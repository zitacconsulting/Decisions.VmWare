using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get Datastores", "Integration", "VmWare", "Storage")]
[Writable]
public class GetDatastores : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, IDefaultInputMappingStep
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
    public IInputMapping[] DefaultInputs
    {
        get
        {
            IInputMapping[] inputMappingArray = new IInputMapping[1];
            inputMappingArray[0] = (IInputMapping)new IgnoreInputMapping() { InputDataName = "Datacenter ID" };
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datacenter ID"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(Datastore), "Datastores", true)));
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

        List<Datastore> Datastores = new List<Datastore>();

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

            ManagedObjectReference searchRoot = new ManagedObjectReference();

            if (String.IsNullOrEmpty(DatacenterId))
            {
                // Retrieve ServiceContent
                ServiceContent serviceContent = vimClient.ServiceContent;
                searchRoot = serviceContent.RootFolder;

            }
            else
            {
                searchRoot.Type = "Datacenter";
                searchRoot.Value = DatacenterId;
            }


            // Create a filter that only lists the Storagepods with capacity that is not 0.
            NameValueCollection searchfilter = new NameValueCollection();
            searchfilter.Add("Summary.Capacity", "^(?!0$)");


            var dataStores = vimClient.FindEntityViews(typeof(VMware.Vim.Datastore), searchRoot, searchfilter, null);

            if (dataStores != null)
            {
                foreach (VMware.Vim.Datastore evb in dataStores)
                {
                    VMware.Vim.Datastore store = evb as VMware.Vim.Datastore;
                    if (store != null)
                    {

                            Datastore NewDatastore = new Datastore();
                            NewDatastore.Name = store.Name;
                            NewDatastore.ID = store.MoRef.Value;
                            NewDatastore.Capacity = store.Summary.Capacity;
                            NewDatastore.FreeSpace = store.Summary.FreeSpace;
                            Datastores.Add(NewDatastore);
                            Console.WriteLine(store.Name);
                     }
                }
            }
            else
            {
                if (ShowOutcomeforNoResults)
                {
                    // Disconnect from vSphere server
                    vimClient.Logout();
                    vimClient.Disconnect();
                    return new ResultData("No Results");
                }
                //Console.WriteLine("No storage pods found.");
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
        dictionary.Add("Datastores", (object)Datastores.ToArray());
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}