using System.Net;
using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get StoragePods by Datacenter", "Integration", "VmWare", "Storage")]
[Writable]
public class GetStoragePodsByDatacenter : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datacenter ID"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(StoragePod), "StoragePods", true)));
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
        string DatacenterId = data.Data["Datacenter ID"] as string;

        // Build a Moref with the provided DatacenterID
        var datacenterRef = new ManagedObjectReference
        {
            Type = "Datacenter",
            Value = DatacenterId
        };

        List<StoragePod> StoragePods = new List<StoragePod>();

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

            // Get the Datacenter by the provided ID
            Datacenter Datacenter = (Datacenter)vimClient.GetView(datacenterRef, null);

            // Create a filter that only lists the Storagepods with capacity that is not 0.
            NameValueCollection searchfilter = new NameValueCollection();
            searchfilter.Add("Summary.Capacity", "^(?!0$)");


            var storagePods = vimClient.FindEntityViews(typeof(VMware.Vim.StoragePod), datacenterRef, searchfilter, null);

            if (storagePods != null)
            {
                foreach (VMware.Vim.StoragePod sp in storagePods)
                {
                    StoragePod NewPod = new StoragePod();
                    NewPod.Name = sp.Name;
                    NewPod.ID = sp.MoRef.Value;
                    NewPod.Capacity = sp.Summary.Capacity;
                    NewPod.FreeSpace = sp.Summary.FreeSpace;
                    StoragePods.Add(NewPod);
                }
            }
            else
            {
                Console.WriteLine("No storage pods found.");
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
        dictionary.Add("Virtual Machines", (object)StoragePods.ToArray());
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}