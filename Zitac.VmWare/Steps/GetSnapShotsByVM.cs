using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get Snapshots by VM", "Integration", "VmWare", "Snapshots")]
[Writable]
public class GetSnapshotsByVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool showOutcomeforNoResults;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }
    [PropertyClassification(1, "Show Outcome for No Results", new string[] { "Outcomes" })]
    public bool ShowOutcomeforNoResults
    {
        get { return showOutcomeforNoResults; }
        set
        {
            showOutcomeforNoResults = value;
            this.OnPropertyChanged("OutcomeScenarios");
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
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(Snapshot), "Snapshots", true)));
            if (ShowOutcomeforNoResults)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("No Results"));
            }
            outcomeScenarioDataList.Add(new OutcomeScenarioData("Error", new DataDescription(typeof(string), "Error Message")));
            return outcomeScenarioDataList.ToArray();
        }
    }

    public ResultData Run(StepStartData data)
    {
        string Hostname = data.Data["Hostname"] as string;
        Credentials Credentials = data.Data["Credentials"] as Credentials;
        string VmID = data.Data["VMID"] as string;

        ManagedObjectReference mor = new ManagedObjectReference();
        mor.Type = "VirtualMachine";
        mor.Value = VmID;

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

            var vm = (VirtualMachine)vimClient.GetView(mor, null);

            vimClient.Logout();
            vimClient.Disconnect();

            List<Snapshot> SnapshotList = new List<Snapshot>();

            if (vm.Snapshot != null)
            {
                SnapshotList = ProcessSnapshotTree(vm.Snapshot.RootSnapshotList);
            }
            else
            {
                if (ShowOutcomeforNoResults)
                {
                    return new ResultData("No Results");
                }
                Console.WriteLine("No snapshots found for VM: " + vm.Name);
            }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("Snapshots", (object)SnapshotList.ToArray());
        return new ResultData("Done", (IDictionary<string, object>)dictionary);

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

            static List<Snapshot> ProcessSnapshotTree(VirtualMachineSnapshotTree[] snapshotTree)
            {
                List<Snapshot> ReturnList = new List<Snapshot>();
                foreach (var snapshot in snapshotTree)
                {
                    Snapshot SnapshotToAdd = new Snapshot();
                    SnapshotToAdd.Name = snapshot.Name;
                    SnapshotToAdd.ID = snapshot.Snapshot.Value;
                    SnapshotToAdd.State = snapshot.State.ToString();
                    SnapshotToAdd.Description = snapshot.Description;
                    SnapshotToAdd.CreateTime = snapshot.CreateTime;

                    Console.WriteLine("Snapshot: " + snapshot.Name);
                    if (snapshot.ChildSnapshotList != null)
                    {
                        SnapshotToAdd.Children = ProcessSnapshotTree(snapshot.ChildSnapshotList).ToArray();
                    }
                    ReturnList.Add(SnapshotToAdd);
                }
                return ReturnList;
            }
}