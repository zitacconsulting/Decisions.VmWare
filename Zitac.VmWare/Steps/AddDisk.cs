using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;
using System;
using System.Collections.Generic;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Add Disk", "Integration", "VmWare", "VM")]
[Writable]
public class AddDisk : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }

    public DataDescription[] InputData
    {
        get
        {

            List<DataDescription> dataDescriptionList = new List<DataDescription>();
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostname"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Credentials)), "Credentials"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "VM ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "Disk Size (GB)"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(int), "Key")));
            outcomeScenarioDataList.Add(new OutcomeScenarioData("Error", new DataDescription(typeof(string), "Error Message")));
            return outcomeScenarioDataList.ToArray();
        }
    }

    public ResultData Run(StepStartData data)
    {
        string Hostname = data.Data["Hostname"] as string;
        Credentials Credentials = data.Data["Credentials"] as Credentials;
        string DatacenterId = data.Data["Datacenter ID"] as string;
        string VmId = data.Data["VM ID"] as string;
        int? DiskSize = data.Data["Disk Size (GB)"] as int?;

        int? DiskKey = null;

        // Connect to vSphere server
        var vimClient = new VimClientImpl();
        if (ignoreSSLErrors)
        {
            vimClient.IgnoreServerCertificateErrors = true;
        }
        try
        {
            vimClient.Connect("https://" + Hostname + "/sdk");
            vimClient.Login(Credentials.Username, Credentials.Password);

            ManagedObjectReference vmMor = new ManagedObjectReference();

            vmMor.Type = "VirtualMachine";
            vmMor.Value = VmId;


            var vm = vimClient.GetView(vmMor, VMwarePropertyLists.VirtualMachineProperties) as VirtualMachine;
            if (vm == null)
            {
                vimClient.Logout();
                vimClient.Disconnect();
                throw new Exception("Failed to add Find VM with ID:" + VmId);
            }

            VM VirtualMachine = new VM(vm);

            int controllerKey = 1000; // Assuming SCSI Controller
            int unitNumber = 0;

            UnitController FirstAvail = GetFirstAvailableUnitAndController(controllerKey, unitNumber, vm.Config.Hardware.Device);

            // Create a configuration specification to hold the changes
            VirtualMachineConfigSpec configSpec = new VirtualMachineConfigSpec();

            VirtualDeviceConfigSpec newScsiControllerSpec = new VirtualDeviceConfigSpec();

            if (FirstAvail.ControllerFound == false)
            {
                VirtualSCSIController newScsiController = new VirtualSCSIController();
                newScsiController.BusNumber = FirstAvail.BusNumber;
                newScsiController.Key = FirstAvail.ControllerKey;

                newScsiControllerSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                newScsiControllerSpec.Device = newScsiController;

            }


            // Define new Disk

            var diskSpec = new VirtualDisk();
            diskSpec.CapacityInKB = (long)DiskSize * 1048576;
            diskSpec.UnitNumber = FirstAvail.UnitNumber;
            diskSpec.ControllerKey = FirstAvail.ControllerKey;
            diskSpec.Key = -1;
            diskSpec.Backing = new VirtualDiskFlatVer2BackingInfo
            {
                FileName = "",
                DiskMode = "persistent",
            };

            // Create a device change specification and add the new device
            VirtualDeviceConfigSpec deviceConfigSpec = new VirtualDeviceConfigSpec();
            deviceConfigSpec.FileOperation = VirtualDeviceConfigSpecFileOperation.create;
            deviceConfigSpec.Operation = VirtualDeviceConfigSpecOperation.add;
            deviceConfigSpec.Device = diskSpec;


            if (FirstAvail.ControllerFound == false)
            {
                configSpec.DeviceChange = new VirtualDeviceConfigSpec[] { newScsiControllerSpec, deviceConfigSpec };
            }
            else
            {
                configSpec.DeviceChange = new VirtualDeviceConfigSpec[] { deviceConfigSpec };
            }


            // Start the task to reconfigure the VM
            var taskMor = vm.ReconfigVM_Task(configSpec);

            VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
            while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
            {
                //Console.WriteLine(TaskResult.Info.State);
                System.Threading.Thread.Sleep(2000);
                TaskResult.UpdateViewData();
            }

            if (TaskResult.Info.State.ToString() == "error")
            {
                vimClient.Logout();
                vimClient.Disconnect();
                throw new Exception("Failed to add Disk:" + TaskResult.Info.Error.Fault.ToString() + " - " + TaskResult.Info.Error.LocalizedMessage.ToString());
            }


            vm.UpdateViewData("Config.Hardware.Device");

            foreach (VirtualDevice device in vm.Config.Hardware.Device)
            {
                if (device.ControllerKey == FirstAvail.ControllerKey && device.UnitNumber == FirstAvail.UnitNumber)
                {
                    DiskKey = device.Key;
                }
            }

            // Disconnect from vSphere server
            vimClient.Logout();
            vimClient.Disconnect();

        }
        catch (Exception e)
        {
            string ExceptionMessage = e.ToString();
            return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) ExceptionMessage
                }
                });
        }


        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("Key", (object)DiskKey);
        return new ResultData("Done", (IDictionary<string, object>)dictionary);
    }
    static UnitController GetFirstAvailableUnitAndController(int controllerKey, int unitNumber, VirtualDevice[] Devices)
    {
        bool controllerFound = false;
        int busNumber = 0;
        foreach (VirtualDevice device in Devices)
        {
            if (device.ControllerKey == controllerKey)
            {
                if (device.UnitNumber >= unitNumber)
                {
                    // Find the highest used unit number and add 1 to find the next available unit number.
                    unitNumber = device.UnitNumber.Value + 1;
                }
            }

            if (device is VirtualSCSIController scsiController)
            {
                if (scsiController.Key == controllerKey)
                {
                    controllerFound = true;
                }

                if (scsiController.BusNumber >= busNumber)
                {
                    // Find the highest used bus number and add 1 to find the next available bus number.
                    busNumber = scsiController.BusNumber + 1;
                }
            }

        }

        if (unitNumber > 1)
        {
            controllerKey = controllerKey + 1;
            return GetFirstAvailableUnitAndController(controllerKey, 0, Devices);
        }
        UnitController UnitCont = new UnitController();
        UnitCont.UnitNumber = unitNumber;
        UnitCont.ControllerFound = controllerFound;
        UnitCont.ControllerKey = controllerKey;
        UnitCont.BusNumber = busNumber;

        return UnitCont;
    }
}