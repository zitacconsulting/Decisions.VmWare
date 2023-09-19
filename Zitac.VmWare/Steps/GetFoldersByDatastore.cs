using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get Folders By Datastore", "Integration", "VmWare", "Storage")]
[Writable]
public class GetFoldersByDatastore : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Datastore ID"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(string), "Folders", true)));
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
        string DatastoreId = data.Data["Datastore ID"] as string;


        List<string> Folders = new List<string>();

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

            ManagedObjectReference DatastoreMoref = new ManagedObjectReference();
            DatastoreMoref.Type = "Datastore";
            DatastoreMoref.Value = DatastoreId;


            VMware.Vim.Datastore Datastore = vimClient.GetView(DatastoreMoref, null) as VMware.Vim.Datastore;
            if (Datastore != null)
            {
                ManagedObjectReference hostMor = Datastore.Host[0].Key;
                HostSystem host = (HostSystem)vimClient.GetView(hostMor, null);


                ManagedObjectReference dsBrowser = host.DatastoreBrowser;
                HostDatastoreBrowser hostDatastoreBrowser = (HostDatastoreBrowser)vimClient.GetView(dsBrowser, null);

                HostDatastoreBrowserSearchSpec searchSpec = new HostDatastoreBrowserSearchSpec();
                searchSpec.MatchPattern = new string[] { "*" };
                string searchPath = $"[{Datastore.Name}] /";

                HostDatastoreBrowserSearchResults searchResults = hostDatastoreBrowser.SearchDatastore(searchPath, searchSpec);

                foreach (var fileInfo in searchResults.File)
                {
                    if (fileInfo is FolderFileInfo folderFileInfo)
                    {
                        Folders.Add(folderFileInfo.Path);
                    }
                }
                if (Folders.Count == 0 && ShowOutcomeforNoResults) {
                    return new ResultData("No Results");
                }

            }
            else
            {
                if (ShowOutcomeforNoResults)
                {
                    return new ResultData("No Results");
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
        dictionary.Add("Folders", (object)Folders.ToArray());
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}