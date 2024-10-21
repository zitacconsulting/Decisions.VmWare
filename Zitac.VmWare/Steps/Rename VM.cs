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
    private bool renameFilesAndFolder = true;

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
        string DatacenterId = data.Data["Datacenter ID"] as string;
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

            // Step 1: Rename the VM in the vCenter inventory
            var renameTask = vm.Rename_Task(NewVmName);
            vimClient.WaitForTask(renameTask);
            Console.WriteLine("Renamed");
            if (renameFilesAndFolder)
            {

                if (vm.Runtime.PowerState != VirtualMachinePowerState.poweredOff)
                {
                    throw new Exception("VM must be powered off to rename the files.");
                }

                // Get current datastore
                var currentDatastoreMoref = vm.Datastore[0];
                var currentDatastore = vimClient.GetView(currentDatastoreMoref, null) as VMware.Vim.Datastore;

                // Find usable datastores
                var usableDatastores = FindUsableDatastores(vimClient, vm, currentDatastore);

                if (!usableDatastores.Any())
                {
                    throw new Exception("No usable datastores found (Move is needed for renaming files)");
                }


                // Try to move the VM to each usable datastore until successful
                bool moveSuccessful = false;
                foreach (var tempDatastore in usableDatastores)
                {
                    try
                    {
                        // Move VM to temp datastore
                        var relocateSpec = new VirtualMachineRelocateSpec
                        {
                            Datastore = tempDatastore.MoRef
                        };

                        var taskMor = vm.RelocateVM_Task(relocateSpec, VirtualMachineMovePriority.defaultPriority);

                        VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
                        while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
                        {
                            Console.WriteLine(TaskResult.Info.State);
                            System.Threading.Thread.Sleep(2000);
                            TaskResult.UpdateViewData();
                        }
                        Console.WriteLine(TaskResult.Info.State);



                        if (TaskResult.Info.State == TaskInfoState.success)
                        {
                            Console.WriteLine($"VM successfully moved to temporary datastore: {tempDatastore.Name}");
                            moveSuccessful = true;

                            // Move VM back to original datastore
                            relocateSpec.Datastore = currentDatastore.MoRef;
                            TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
                            while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
                            {
                                Console.WriteLine(TaskResult.Info.State);
                                System.Threading.Thread.Sleep(2000);
                                TaskResult.UpdateViewData();
                            }
                            Console.WriteLine(TaskResult.Info.State);

                            if (TaskResult.Info.State == TaskInfoState.success)
                            {
                                Console.WriteLine("VM successfully moved back to original datastore.");
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
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to move VM to datastore {tempDatastore.Name}. Error: {ex.Message}");
                    }
                }

                if (!moveSuccessful)
                {
                    throw new Exception("Failed to move VM to any of the available datastores.");
                }

                Console.WriteLine($"VM renamed to {NewVmName}.");
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
    static IEnumerable<VMware.Vim.Datastore> FindUsableDatastores(VimClient vimClient, VirtualMachine vm, VMware.Vim.Datastore currentDatastore)
    {
        var datastores = vimClient.FindEntityViews(typeof(VMware.Vim.Datastore), null, null, new[] { "name", "summary" });

        // Get the size of the VM
        long vmSize = vm.Summary.Storage.Committed + vm.Summary.Storage.Uncommitted;

        return datastores
            .Cast<VMware.Vim.Datastore>()
            .Where(ds =>
            {
                var summary = ds.Summary as DatastoreSummary;
                return summary != null &&
                       summary.Accessible &&
                       summary.FreeSpace > vmSize * 1.1 && // Ensure 10% extra space
                       ds.MoRef != currentDatastore.MoRef; // Exclude the current datastore
            })
            .OrderByDescending(ds => ((DatastoreSummary)ds.Summary).FreeSpace); // Sort by free space, most to least
    }
}