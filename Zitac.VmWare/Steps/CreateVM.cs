using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;
using System.ComponentModel;
using DecisionsFramework.Data.DataTypes;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Create VM", "Integration", "VmWare", "VM")]
[Writable]
public class CreateVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, INotifyPropertyChanged, IDefaultInputMappingStep
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool storageDRS;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }

    [PropertyClassification(7, "Use Storage DRS", new string[] { "Settings" })]
    public bool StorageDRS
    {
        get { return storageDRS; }
        set
        {
            storageDRS = value;

        }
    }

    public IInputMapping[] DefaultInputs
    {
        get
        {
            IInputMapping[] inputMappingArray = new IInputMapping[13];
            inputMappingArray[0] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Datacenter ID" };
            inputMappingArray[1] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Cluster ID" };
            inputMappingArray[2] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Folder ID" };
            inputMappingArray[3] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "ISO File" };
            inputMappingArray[4] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Network ID" };
            inputMappingArray[5] = (IInputMapping)new ConstantInputMapping() { InputDataName = "OS Type" };
            inputMappingArray[6] = (IInputMapping)new ConstantInputMapping() { InputDataName = "Firmware" };
            inputMappingArray[7] = (IInputMapping)new ConstantInputMapping() { InputDataName = "SCSI Controller" };
            inputMappingArray[8] = (IInputMapping)new ConstantInputMapping() { InputDataName = "Network Adapter Type" };
            inputMappingArray[9] = (IInputMapping)new ConstantInputMapping() { InputDataName = "Synchronize Time with Host", Value = true };
            inputMappingArray[10] = (IInputMapping)new ConstantInputMapping() { InputDataName = "Memory Hot Plug", Value = false };
            inputMappingArray[11] = (IInputMapping)new ConstantInputMapping() { InputDataName = "CPU Hot Plug", Value = false };
            inputMappingArray[12] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Extra Config" };
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Firmware)), "Firmware"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "Secure Boot") { Categories = new string[] { "Advanced" } });
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "Synchronize Time with Host") { Categories = new string[] { "Advanced" } });
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datacenter ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Cluster ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Folder ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datastore ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "ISO File"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Network ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(NetworkAdapterType)), "Network Adapter Type"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "CPU"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "CPU Hot Plug") { Categories = new string[] { "Advanced" } });
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "Memory (GB)"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "Memory Hot Plug") { Categories = new string[] { "Advanced" } });
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(SCSIController)), "SCSI Controller"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(int)), "Disk Size (GB)"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(SimpleKeyValuePair)), "Extra Config", true, true, false) { Categories = new string[] { "Advanced" } });
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
        string? ClusterID = data.Data["Cluster ID"] as string;
        string? FolderId = data.Data["Folder ID"] as string;
        string? DatastoreId = data.Data["Datastore ID"] as string;
        string? ISOFile = data.Data["ISO File"] as string;
        string? NetworkId = data.Data["Network ID"] as string;
        NetworkAdapterType NetworkAdapterType = (NetworkAdapterType)data.Data["Network Adapter Type"];
        GuestOS OSType = (GuestOS)data.Data["OS Type"];
        Firmware Firmware = (Firmware)data.Data["Firmware"];
        bool SecureBoot = data.Data["Secure Boot"] as bool? ?? false;
        bool SyncTime = data.Data["Synchronize Time with Host"] as bool? ?? true;
        int? Cpu = data.Data["CPU"] as int?;
        bool CpuHotPlug = data.Data["CPU Hot Plug"] as bool? ?? true;
        int? Memory = data.Data["Memory (GB)"] as int?;
        bool MemoryHotPlug = data.Data["Memory Hot Plug"] as bool? ?? true;
        int? DiskSize = data.Data["Disk Size (GB)"] as int?;
        SCSIController SCSIController = (SCSIController)data.Data["SCSI Controller"];
        SimpleKeyValuePair[] ExtraConfig = (SimpleKeyValuePair[])data.Data["Extra Config"];



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

            String DatastoreName = null;

            ManagedObjectReference datastoreMor = new ManagedObjectReference();
            if (storageDRS) { datastoreMor.Type = "StoragePod"; }
            else { datastoreMor.Type = "Datastore"; }

            datastoreMor.Value = DatastoreId;
            var DatastoreEntity = vimClient.GetView(datastoreMor, null);

            if (storageDRS)
            {
                var StoragePod = DatastoreEntity as VMware.Vim.StoragePod;
                DatastoreName = StoragePod.Name;
            }
            else
            {
                var DataStore = DatastoreEntity as VMware.Vim.Datastore;
                DatastoreName = DataStore.Name;
            }

            ResourcePool resourcePool = null;
            if (String.IsNullOrEmpty(ClusterID))
            {
                resourcePool = (ResourcePool)vimClient.FindEntityView(typeof(ResourcePool), null, new NameValueCollection { { "name", "Resources" } }, null);
            }
            else
            {
                ManagedObjectReference searchRootCluster = new ManagedObjectReference();
                searchRootCluster.Type = "ClusterComputeResource";
                searchRootCluster.Value = ClusterID;
                resourcePool = (ResourcePool)vimClient.FindEntityView(typeof(ResourcePool), searchRootCluster, new NameValueCollection { { "name", "Resources" } }, null);                
            }

            List<VirtualDeviceConfigSpec> ConfigSpecs = new List<VirtualDeviceConfigSpec>();

            // Build VM
            Console.WriteLine("OStype = " + OSType.ToString());
            var vmConfigSpec = new VirtualMachineConfigSpec();
            vmConfigSpec.Name = VmName;
            vmConfigSpec.MemoryMB = Memory * 1024;
            vmConfigSpec.NumCPUs = Cpu;
            vmConfigSpec.GuestId = OSType.ToString();
            vmConfigSpec.Files = new VirtualMachineFileInfo();
            vmConfigSpec.Files.VmPathName = "[" + DatastoreName + "]";
            vmConfigSpec.CpuHotAddEnabled = CpuHotPlug;
            vmConfigSpec.CpuHotRemoveEnabled = CpuHotPlug;
            vmConfigSpec.MemoryHotAddEnabled = MemoryHotPlug;

            if (ExtraConfig != null)
            {
                List<OptionValue> Options = new List<OptionValue>();
                foreach (SimpleKeyValuePair Config in ExtraConfig)
                {
                    var Option = new OptionValue
                    {
                        Key = Config.Key,
                        Value = Config.Value
                    };
                    Options.Add(Option);
                }
                vmConfigSpec.ExtraConfig = Options.ToArray();
            }


            VirtualMachineBootOptions bootOptions = new VirtualMachineBootOptions();
            bootOptions.EfiSecureBootEnabled = SecureBoot; // Enable/Disable EFI Secure Boot
            vmConfigSpec.BootOptions = bootOptions;

            ToolsConfigInfo toolsOptions = new ToolsConfigInfo();
            toolsOptions.SyncTimeWithHostAllowed = SyncTime; // Enable Disable Sync Time with Host
            vmConfigSpec.Tools = toolsOptions;

            // Define Firmware Type
            vmConfigSpec.Firmware = Firmware.ToString(); ; // Can be "efi" or "bios"

            VirtualDiskFlatVer2BackingInfo diskBackingInfo = new VirtualDiskFlatVer2BackingInfo();

            if (DiskSize.HasValue)
            {
                VirtualSCSIController scsiController = null;
                switch (SCSIController.ToString())
                {
                    case "Paravirtual":
                        scsiController = new ParaVirtualSCSIController();
                        break;
                    case "LSILogic":
                        scsiController = new VirtualLsiLogicController();
                        break;
                    case "LSILogicSAS":
                        scsiController = new VirtualLsiLogicSASController();
                        break;
                    case "VirtualBus":
                        scsiController = new VirtualBusLogicController();
                        break;
                    default:
                        throw new Exception("No SCSI Type selected");
                }

                scsiController.Key = 1000;
                scsiController.BusNumber = 0;
                scsiController.SharedBus = VirtualSCSISharing.noSharing;
                VirtualDeviceConfigSpec scsiControllerSpec = new VirtualDeviceConfigSpec();
                scsiControllerSpec.Operation = VirtualDeviceConfigSpecOperation.add;
                scsiControllerSpec.Device = scsiController;
                ConfigSpecs.Add(scsiControllerSpec);

                // Disk settings

                // Place with VM if it's clustered, use datastore if datastore
                if (!storageDRS)
                {
                    diskBackingInfo.Datastore = datastoreMor;
                }
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
                VirtualEthernetCard nic = new VirtualVmxnet3();
                switch (NetworkAdapterType)
                {
                    case NetworkAdapterType.E1000:
                        nic = new VirtualE1000();
                        break;
                    case NetworkAdapterType.E1000e:
                        nic = new VirtualE1000e();
                        break;
                    case NetworkAdapterType.VMXNET:
                        nic = new VirtualVmxnet();
                        break;
                    case NetworkAdapterType.VMXNET3:
                        nic = new VirtualVmxnet3();
                        break;
                    case NetworkAdapterType.SriovEthernetCard:
                        nic = new VirtualSriovEthernetCard();
                        break;
                    case NetworkAdapterType.Vmxnet3Vrdma:
                        nic = new VirtualVmxnet3Vrdma();
                        break;
                    // VMXNET2 is typically chosen automatically by vSphere and thus not selectable manually
                    default:
                        throw new Exception("No Network Adapter Type selected");
                }

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


            ManagedObjectReference task = new ManagedObjectReference();
            // Create the VM

            if (storageDRS)
            {

                //Create PodDiskLocator
                VMware.Vim.PodDiskLocator pdl = new PodDiskLocator();
                pdl = new PodDiskLocator();
                pdl.DiskId = -1;
                pdl.DiskBackingInfo = diskBackingInfo;

                //Create Pod Config
                VMware.Vim.VmPodConfigForPlacement vpcfp = new VmPodConfigForPlacement();
                vpcfp = new VmPodConfigForPlacement();
                vpcfp.StoragePod = datastoreMor;
                vpcfp.Disk = new[] { pdl };


                //Create Storage DRS Pod Selection Spec
                VMware.Vim.StorageDrsPodSelectionSpec sdps = new StorageDrsPodSelectionSpec();
                sdps.StoragePod = datastoreMor;
                sdps.InitialVmConfig = new[] { vpcfp };

                // Create Storage Placement specStorageDRS
                VMware.Vim.StoragePlacementSpec spconfig = new StoragePlacementSpec();
                spconfig.Type = "create";
                spconfig.ConfigSpec = vmConfigSpec;
                spconfig.Folder = folder.MoRef;
                spconfig.ResourcePool = resourcePool.MoRef;
                spconfig.PodSelectionSpec = sdps;


                StorageResourceManager srm = (StorageResourceManager)vimClient.GetView(vimClient.ServiceContent.StorageResourceManager, null);
                var recommendations = srm.RecommendDatastores(spconfig);

                task = srm.ApplyStorageDrsRecommendation_Task(new[] { recommendations.Recommendations.FirstOrDefault().Key });

            }
            else
            {
                task = folder.CreateVM_Task(vmConfigSpec, resourcePool.MoRef, host.MoRef);
            }
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
                if (storageDRS)
                {
                    ApplyStorageRecommendationResult AsrResult = (ApplyStorageRecommendationResult)TaskResult.Info.Result;

                    VirtualMachine vm = (VirtualMachine)vimClient.GetView(AsrResult.Vm, null);
                    NewVM = new VM(vm);
                }
                else
                {
                    VirtualMachine vm = (VirtualMachine)vimClient.GetView((ManagedObjectReference)TaskResult.Info.Result, VMwarePropertyLists.VirtualMachineProperties) as VirtualMachine;
                    NewVM = new VM((VMware.Vim.VirtualMachine)vm);
                }
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