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

[AutoRegisterStep("Change Network", "Integration", "VmWare", "VM")]
[Writable]
public class ChangeNetwork : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Network ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "NIC Key"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done"));
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
        string NetworkID = data.Data["Network ID"] as string;
        int? NicKey = data.Data["NIC Key"] as int?;


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


            // Create a configuration specification to hold the changes
            VirtualMachineConfigSpec configSpec = new VirtualMachineConfigSpec();

            bool nicFound = false;

            foreach (var device in vm.Config.Hardware.Device)
            {
                if (device.Key == NicKey && device is VirtualEthernetCard nic)
                {
                    nicFound = true;

                    if (NetworkID.StartsWith("dvportgroup"))
                    {
                        // Setting up for a distributed port group


                        ManagedObjectReference networkMor = new ManagedObjectReference();

                        networkMor.Type = "DistributedVirtualPortgroup";
                        networkMor.Value = NetworkID;
                        var network = vimClient.GetView(networkMor, VMwarePropertyLists.DistributedVirtualPortgroupProperties) as VMware.Vim.DistributedVirtualPortgroup;

                        if (network == null)
                        {
                            vimClient.Logout();
                            vimClient.Disconnect();
                            throw new Exception("Failed to add Find Portgroup with ID:" + NetworkID);
                        }

                        var Netswitch = vimClient.GetView(network.Config.DistributedVirtualSwitch, VMwarePropertyLists.VmwareDistributedVirtualSwitchProperties) as VMware.Vim.VmwareDistributedVirtualSwitch;

                        if (Netswitch == null)
                        {
                            vimClient.Logout();
                            vimClient.Disconnect();
                            throw new Exception("Failed to Find Switch for Portgroup with ID:" + NetworkID);
                        }


                        var dvsPortConnection = new DistributedVirtualSwitchPortConnection
                        {
                            PortgroupKey = NetworkID,
                            SwitchUuid = Netswitch.Uuid
                        };

                        nic.Backing = new VirtualEthernetCardDistributedVirtualPortBackingInfo
                        {
                            Port = dvsPortConnection
                        };
                        // Create a device spec for the NIC
                        var deviceSpec = new VirtualDeviceConfigSpec
                        {
                            Device = nic,
                            Operation = VirtualDeviceConfigSpecOperation.edit
                        };

                        configSpec.DeviceChange = new VirtualDeviceConfigSpec[] { deviceSpec };
                        break;
                    }
                    else
                    {
                        // Setting up for a standard network
                        ManagedObjectReference networkMor = new ManagedObjectReference();

                        networkMor.Type = "Network";
                        networkMor.Value = NetworkID;
                        var network = vimClient.GetView(networkMor, VMwarePropertyLists.NetworkProperties) as VMware.Vim.Network;

                        if (network == null)
                        {
                            vimClient.Logout();
                            vimClient.Disconnect();
                            throw new Exception("Failed to add Find Network with ID:" + NetworkID);
                        }
                        // Create a new backing info with the provided network ID
                        var newNetwork = new ManagedObjectReference { Type = "Network", Value = NetworkID };
                        var networkBacking = new VirtualEthernetCardNetworkBackingInfo { Network = networkMor, DeviceName = network.Name };

                        // Assign the new backing to the NIC
                        nic.Backing = networkBacking;

                        // Create a device spec for the NIC
                        var deviceSpec = new VirtualDeviceConfigSpec
                        {
                            Device = nic,
                            Operation = VirtualDeviceConfigSpecOperation.edit
                        };

                        configSpec.DeviceChange = new VirtualDeviceConfigSpec[] { deviceSpec };
                        break;
                    }
                }
            }


            if (!nicFound)
            {
                throw new Exception("Failed to add Find NIC with Key:" + NicKey);
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
                throw new Exception("Failed to change network:" + TaskResult.Info.Error.Fault.ToString() + " - " + TaskResult.Info.Error.LocalizedMessage.ToString());
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


        return new ResultData("Done");
    }
}