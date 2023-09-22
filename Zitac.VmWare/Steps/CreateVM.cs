using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Create VM", "Integration", "VmWare", "VM")]
[Writable]
public class CreateVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, IDefaultInputMappingStep
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }
    public IInputMapping[] DefaultInputs
    {
        get
        {
            IInputMapping[] inputMappingArray = new IInputMapping[5];
            inputMappingArray[0] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Datacenter ID" };
            inputMappingArray[1] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Folder ID" };
            inputMappingArray[2] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "ISO File" };
            inputMappingArray[3] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Network ID" };
            inputMappingArray[4] = (IInputMapping)new ConstantInputMapping() { InputDataName = "Network ID" };
            return inputMappingArray;
        }
    }
    public DataDescription[] InputData
    {
        get
        {

            List<DataDescription> dataDescriptionList = new List<DataDescription>();
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostname"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Credentials)), "Credentials"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "VM Name"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(GuestOS)), "OS Type"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datacenter ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Folder ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datastore ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "ISO File"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Network ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "CPU"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "Memory (GB)"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "Disk Size (GB)"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(VM), "Virtual Machine", false)));
            outcomeScenarioDataList.Add(new OutcomeScenarioData("Error", new DataDescription(typeof(string), "Error Message")));
            return outcomeScenarioDataList.ToArray();
        }
    }

    public ResultData Run(StepStartData data)
    {
        string? Hostname = data.Data["Hostname"] as string;
        Credentials? Credentials = data.Data["Credentials"] as Credentials;
        string? VmName = data.Data["VM Name"] as string;
        string? DatacenterId = data.Data["Datacenter ID"] as string;
        string? FolderId = data.Data["Folder ID"] as string;
        string? DatastoreId = data.Data["Datastore ID"] as string;
        string? ISOFile = data.Data["ISO File"] as string;
        string? NetworkId = data.Data["Network ID"] as string;
        GuestOS OSType = (GuestOS)data.Data["OS Type"];
        int? Cpu = data.Data["CPU"] as int?;
        int? Memory = data.Data["Memory (GB)"] as int?;
        int? DiskSize = data.Data["Disk Size (GB)"] as int?;


        FolderWithPath Folder = new FolderWithPath();

        // Connect to vSphere server
        var vimClient = new VimClientImpl();
        if (ignoreSSLErrors)
        {
            vimClient.IgnoreServerCertificateErrors = true;
        }

        VM NewVM = new VM();

        try
        {
            vimClient.Connect("https://" + Hostname + "/sdk");
            vimClient.Login(Credentials.Username, Credentials.Password);


            // Specify the host
            ServiceContent serviceContent = vimClient.ServiceContent;
            ManagedObjectReference searchRoot = serviceContent.RootFolder;
            var host = (vimClient.FindEntityViews(typeof(HostSystem), searchRoot, null, null))[0];


            if (String.IsNullOrEmpty(DatacenterId))
            {
                // Retrieve ServiceContent
                searchRoot = serviceContent.RootFolder;

            }
            else
            {
                searchRoot.Type = "Datacenter";
                searchRoot.Value = DatacenterId;
            }

            Folder? folder = null;

            if (String.IsNullOrEmpty(FolderId))
            {
                folder = (Folder)vimClient.FindEntityView(typeof(Folder), null, new NameValueCollection { { "name", "vm" } }, null);
            }
            else
            {
                ManagedObjectReference folderMor = new ManagedObjectReference();
                folderMor.Type = "Folder";
                folderMor.Value = FolderId;
                folder = vimClient.GetView(folderMor, null) as VMware.Vim.Folder;
            }

            var resourcePool = (ResourcePool)vimClient.FindEntityView(typeof(ResourcePool), null, new NameValueCollection { { "name", "Resources" } }, null);

            ManagedObjectReference datastoreMor = new ManagedObjectReference();
            datastoreMor.Type = "Datastore";
            datastoreMor.Value = DatastoreId;
            VMware.Vim.Datastore? Datastore = vimClient.GetView(datastoreMor, null) as VMware.Vim.Datastore;

            List<VirtualDeviceConfigSpec> ConfigSpecs = new List<VirtualDeviceConfigSpec>();

            // Build VM
Console.WriteLine("OStype = " + OSType.ToString());
            var vmConfigSpec = new VirtualMachineConfigSpec();
            vmConfigSpec.Name = VmName;
            vmConfigSpec.MemoryMB =  Memory * 1024;
            vmConfigSpec.NumCPUs = Cpu;
            vmConfigSpec.GuestId = OSType.ToString();
            vmConfigSpec.Files = new VirtualMachineFileInfo();
            vmConfigSpec.Files.VmPathName = "[" + Datastore.Name + "]";

            if (DiskSize.HasValue)
            {
                // SCSI Controller settings
                VirtualLsiLogicController scsiController = new VirtualLsiLogicController();
                scsiController.Key = 1000;
                scsiController.BusNumber = 0;
                scsiController.SharedBus = VirtualSCSISharing.noSharing;

                VirtualDeviceConfigSpec scsiControllerSpec = new VirtualDeviceConfigSpec();
                scsiControllerSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                scsiControllerSpec.Device = scsiController;
                ConfigSpecs.Add(scsiControllerSpec);

                // Disk settings
                VirtualDiskFlatVer2BackingInfo diskBackingInfo = new VirtualDiskFlatVer2BackingInfo();
                // Assuming the disk should be created in the same folder as the VM, and it should use the VM's name
                diskBackingInfo.Datastore = Datastore.MoRef;
                diskBackingInfo.FileName = "";  // Empty means it will be in the same folder as the VM
                diskBackingInfo.DiskMode = "persistent";  // The disk will not discard changes upon VM power-off

                // Create a 50GB disk (50 * 1024 * 1024 = 52428800 KB)
                VirtualDisk disk = new VirtualDisk();
                disk.Key = -1; // The key could be set to -1 to indicate this is a new device
                disk.UnitNumber = 0;  // The disk's SCSI ID. Make sure this doesn't conflict with any existing devices
                disk.ControllerKey = 1000;  // Must match the SCSI controller's key
                disk.CapacityInKB = (long)DiskSize * 1048576;
                disk.Backing = diskBackingInfo;

                VirtualDeviceConfigSpec diskSpec = new VirtualDeviceConfigSpec();
                diskSpec.FileOperation = VirtualDeviceConfigSpecFileOperation.create;
                diskSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                diskSpec.Device = disk;
                ConfigSpecs.Add(diskSpec);
            }
            if (ISOFile != null && ISOFile != "")
            {
                // CD Drive settings to mount ISO
                VirtualCdromIsoBackingInfo cdromBacking = new VirtualCdromIsoBackingInfo();
                cdromBacking.FileName = ISOFile;

                VirtualCdrom cdrom = new VirtualCdrom();
                cdrom.Key = 1;
                cdrom.UnitNumber = 0;
                cdrom.ControllerKey = 200;
                cdrom.Backing = cdromBacking;

                VirtualDeviceConfigSpec cdromSpec = new VirtualDeviceConfigSpec();
                cdromSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                cdromSpec.Device = cdrom;
                ConfigSpecs.Add(cdromSpec);
            }

            if (NetworkId != null)
            {
                ManagedObjectReference networkMor = new ManagedObjectReference();
                networkMor.Type = "Network";
                networkMor.Value = NetworkId;
                VMware.Vim.Network? Network = vimClient.GetView(networkMor, null) as VMware.Vim.Network;
                // Create a backing info for the NIC specifying the network it should connect to.
                VirtualEthernetCardNetworkBackingInfo nicBacking = new VirtualEthernetCardNetworkBackingInfo();
                nicBacking.DeviceName = Network.Name;  // Replace with the name of your network.

                // Create the virtual NIC device.
                VirtualVmxnet3 nic = new VirtualVmxnet3();
                nic.Backing = nicBacking;
                nic.Key = 2;  // Usually set to 0 for devices you are adding.
                nic.DeviceInfo = new Description();
                nic.DeviceInfo.Label = "Network Adapter 1";
                nic.DeviceInfo.Summary = Network.Name;  // Replace with the name of your network.

                // Create a VirtualDeviceConfigSpec object for the NIC.
                VirtualDeviceConfigSpec nicSpec = new VirtualDeviceConfigSpec();
                nicSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                nicSpec.Device = nic;
                ConfigSpecs.Add(nicSpec);
            }

            vmConfigSpec.DeviceChange = ConfigSpecs.ToArray();


            // Create the VM

            var task = folder.CreateVM_Task(vmConfigSpec, resourcePool.MoRef, host.MoRef);
            var taskId = task.Value;
            Console.WriteLine($"Created task with ID: {taskId}");

            VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(task, null);
            while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
            {
                Console.WriteLine(TaskResult.Info.State);
                System.Threading.Thread.Sleep(2000);
                TaskResult.UpdateViewData();
            }

            if (TaskResult.Info.State == TaskInfoState.success)
            {
                VirtualMachine vm = (VirtualMachine)vimClient.GetView(TaskResult.Info.Entity, null);
                NewVM = new VM(vm);
            }
            else
            {
                Console.WriteLine($"Failed to create virtual machine with error message: {TaskResult.Info.Error.LocalizedMessage}");
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "Failed to create virtual machine with error message:" + TaskResult.Info.Error.LocalizedMessage
                }
                });
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
        dictionary.Add("Virtual Machine", (object)NewVM);
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}