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
using DecisionsFramework;

namespace Zitac.VmWare.Steps;



[AutoRegisterStep("Rename VM", "Integration", "VmWare", "VM")]
[Writable]
public class RenameVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
{

    private static readonly Log log = new Log("VMware Rename VM");

    [WritableValue]
    private bool ignoreSSLErrors;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }
    [WritableValue]
    private bool renameFilesAndFolder;

    [PropertyClassification(0, "Rename File and Folders (Only vCenter)", new string[] { "Settings" })]
    public bool RenameFilesAndFolder
    {
        get { return renameFilesAndFolder; }
        set { renameFilesAndFolder = value; }

    }

    public DataDescription[] InputData
    {
        get
        {

            List<DataDescription> dataDescriptionList = new List<DataDescription>();
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostname"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Credentials)), "Credentials"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "VM ID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "New VM Name"));
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
        string VmId = data.Data["VM ID"] as string;
        string NewVmName = data.Data["New VM Name"] as string;

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


            var vm = vimClient.GetView(vmMor, null) as VirtualMachine;

            if (vm == null)
            {
                vimClient.Logout();
                vimClient.Disconnect();
                throw new Exception("Failed to add Find VM with ID:" + VmId);
            }

            VMware.Vim.Datacenter vmDatacenter = GetDatacenterOfVM(vimClient, vm);

            // Step 1: Rename the VM in the vCenter inventory
            var renameTask = vm.Rename_Task(NewVmName);
            vimClient.WaitForTask(renameTask);
            Console.WriteLine("Renamed");
            if (renameFilesAndFolder)
            {
                log.Info($"Initiating Rename of Files");
                if (vm.Runtime.PowerState != VirtualMachinePowerState.poweredOff)
                {
                    throw new Exception("VM must be powered off to rename the files.");
                }

                // Get current datastore
                ManagedObjectReference currentDatastoreMoref = null;

                foreach (var device in vm.Config.Hardware.Device)
                {
                    if (device is VirtualDisk)
                    {
                        var virtualDisk = (VirtualDisk)device;
                        var backing = virtualDisk.Backing as VirtualDiskFlatVer2BackingInfo;
                        if (backing != null)
                        {
                            // Get the datastore associated with this VMDK
                            var vmdkDatastoreMoref = backing.Datastore;
                            currentDatastoreMoref = (vimClient.GetView(vmdkDatastoreMoref, null) as VMware.Vim.Datastore).MoRef;
                            log.Info($"VMDK Datastore: {currentDatastoreMoref}");
                            break; // Exit after first VMDK, adjust if you need to handle multiple disks.
                        }
                    }
                }

                if (currentDatastoreMoref == null) {
                        throw new Exception("Could not find Datastore of the virtual disk(s)");
                }


                var currentDatastore = vimClient.GetView(currentDatastoreMoref, null) as VMware.Vim.Datastore;
                log.Info($"Current Datastore: {currentDatastore.Name} {currentDatastore.MoRef}");

                // Find usable datastores
                var usableDatastores = FindUsableDatastores(vimClient, vm, currentDatastore, vmDatacenter.MoRef);

                if (!usableDatastores.Any())
                {
                    throw new Exception("No usable datastores found (Move is needed for renaming files)");
                }

                // Try to move the VM to each usable datastore until successful
                bool moveSuccessful = false;
                foreach (var tempDatastore in usableDatastores)
                {
                    log.Info($"Testing Datastore: {tempDatastore.Name} {tempDatastore.MoRef}");
                    // Move VM to temp datastore
                    var relocateSpec = new VirtualMachineRelocateSpec
                    {
                        Datastore = tempDatastore.MoRef
                    };

                    var taskMor = vm.RelocateVM_Task(relocateSpec, VirtualMachineMovePriority.defaultPriority);

                    VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
                    while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
                    {
                        log.Info($"Task state: {TaskResult.Info.State}");
                        System.Threading.Thread.Sleep(2000);
                        TaskResult.UpdateViewData();
                    }


                    if (TaskResult.Info.State == TaskInfoState.success)
                    {
                        log.Info($"VM successfully moved to temporary datastore: {tempDatastore.Name}");
                        moveSuccessful = true;

                        // Move VM back to original datastore
                        relocateSpec.Datastore = currentDatastore.MoRef;
                        taskMor = vm.RelocateVM_Task(relocateSpec, VirtualMachineMovePriority.defaultPriority);
                        TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
                        while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
                        {
                            log.Info($"Task state: {TaskResult.Info.State}");
                            System.Threading.Thread.Sleep(2000);
                            TaskResult.UpdateViewData();
                        }

                        if (TaskResult.Info.State == TaskInfoState.success)
                        {
                            log.Info($"VM successfully moved back to original datastore. {currentDatastore.Name}");
                        }
                        else
                        {
                            throw new Exception($"Failed to move VM back to original datastore. Error: {TaskResult.Info.Error.LocalizedMessage}");
                        }

                        break;
                    }
                    else
                    {
                        throw new Exception("Failed to rename VM file:" + TaskResult.Info.Error.Fault.ToString() + " - " + TaskResult.Info.Error.LocalizedMessage.ToString());
                    }
                }

                if (!moveSuccessful)
                {
                    throw new Exception("Failed to move VM to any of the available datastores.");
                }

                log.Info($"VM renamed to {NewVmName}.");
            }

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
        finally
        {
            vimClient.Logout();
            vimClient.Disconnect();
        }


        return new ResultData("Done");
    }
    static private IEnumerable<VMware.Vim.Datastore> FindUsableDatastores(VimClient vimClient, VirtualMachine vm, VMware.Vim.Datastore currentDatastore, ManagedObjectReference datacenterMoRef)
    {

        // Get current VM size (committed and uncommitted space)
        long vmSize = vm.Summary.Storage.Committed + vm.Summary.Storage.Uncommitted;
        long requiredSpace = (long)(vmSize * 1.1); // 10% extra space

        // Fetch the list of datastores
        var datastores = vimClient.FindEntityViews(typeof(VMware.Vim.Datastore), datacenterMoRef, null, new[] { "name", "summary" });

        // List to store datastores that meet the criteria
        List<VMware.Vim.Datastore> matchingDatastores = new List<VMware.Vim.Datastore>();

        // Iterate through the retrieved datastores
        foreach (VMware.Vim.Datastore datastore in datastores)
        {
            // Check if the datastore is accessible and has enough free space
            if (datastore.Summary.Accessible && datastore.Summary.FreeSpace >= requiredSpace)
            {
                // Ensure we're not selecting the current datastore
                if (datastore.MoRef.Value != currentDatastore.MoRef.Value)
                {
                    // Add to the list of matching datastores
                    matchingDatastores.Add(datastore);
                }
            }
        }

        // Sort the list of matching datastores by free space in descending order
        matchingDatastores.Sort((ds1, ds2) => ds2.Summary.FreeSpace.CompareTo(ds1.Summary.FreeSpace));
        return matchingDatastores;
    }

    private VMware.Vim.Datacenter GetDatacenterOfVM(VimClient vimClient, VirtualMachine vm)
    {
        // Start with the parent of the VM (folder, cluster, etc.)
        ManagedEntity parentEntity = (ManagedEntity)vimClient.GetView(vm.Parent, null);

        // Traverse up the hierarchy until we find a Datacenter
        while (parentEntity != null && !(parentEntity is VMware.Vim.Datacenter))
        {
            parentEntity = (ManagedEntity)vimClient.GetView(parentEntity.Parent, null);
        }

        // Return the Datacenter object, or null if not found
        return parentEntity as VMware.Vim.Datacenter;
    }
}