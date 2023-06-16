using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Create Snapshot", "Integration", "VmWare", "Snapshots")]
[Writable]
public class CreateSnapshot : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, IDefaultInputMappingStep //, INotifyPropertyChanged
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
            IInputMapping[] inputMappingArray = new IInputMapping[1];
            inputMappingArray[0] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Quiesce File System" };
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "VMID"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Snapshot Name"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Snapshot Description"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "Include Memory State"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(bool)), "Quiesce File System"));
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
        string VmID = data.Data["VMID"] as string;
        string SnapshotName = data.Data["Snapshot Name"] as string;
        string? SnapshotDescription = data.Data["Snapshot Description"] as string;
        bool? Memory = data.Data["Include Memory State"] as bool?;
        bool? Quiesce = data.Data["Quiesce File System"] as bool?;

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
            vmMor.Value = VmID;

            VirtualMachine vm = new VirtualMachine(vimClient, vmMor);
            ManagedObjectReference taskMor = vm.CreateSnapshot_Task(SnapshotName, SnapshotDescription, Memory ?? false, Quiesce ?? false);


            VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
            while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
            {
                //Console.WriteLine(TaskResult.Info.State);
                System.Threading.Thread.Sleep(2000);
                TaskResult.UpdateViewData();
            }

            if (TaskResult.Info.State.ToString() == "error")
            {
                throw new Exception("Failed to create snapshot:" + TaskResult.Info.Error.Fault.ToString() + " - " + TaskResult.Info.Error.LocalizedMessage.ToString());
            }

            vimClient.Logout();
            vimClient.Disconnect();

            return new ResultData("Done");

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
    }
}