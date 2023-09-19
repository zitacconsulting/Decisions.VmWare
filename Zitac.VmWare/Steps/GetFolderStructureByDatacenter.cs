using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get Folder Structure By Datacenter", "Integration", "VmWare", "Folder")]
[Writable]
public class GetFolderStructureByDatacenter : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
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

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(FolderWithPath), "Folders", true)));
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


        List<FolderWithPath> Folders = new List<FolderWithPath>();

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

            ManagedObjectReference DatacenterMoref = new ManagedObjectReference();
            DatacenterMoref.Type = "Datacenter";
            DatacenterMoref.Value = DatacenterId;


            VMware.Vim.Datacenter Datacenter = vimClient.GetView(DatacenterMoref, null) as VMware.Vim.Datacenter;
            if (Datacenter != null)
            {
                // Initialize folder list
                List<FolderWithPath> folderList = new List<FolderWithPath>();

                // List folders under vmFolder in the Datacenter
                folderList = ListVmFolders(vimClient, Datacenter.VmFolder, folderList, "");

                Folders = folderList.OrderBy(o => o.Path).ToList();

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
    static List<FolderWithPath> ListVmFolders(VimClient vimClient, ManagedObjectReference folderMoRef, List<FolderWithPath> folderList, string currentPath = "")
    {
        Folder folder = (Folder)vimClient.GetView(folderMoRef, new string[] { "childEntity" });

        foreach (var childEntity in folder.ChildEntity)
        {
            if (childEntity.Type == "Folder")
            {
                Folder childFolder = (Folder)vimClient.GetView(childEntity, new string[] { "name" });

                FolderWithPath NewFolder = new FolderWithPath();
                NewFolder.Name = childFolder.Name;
                NewFolder.ID = childFolder.MoRef.Value;
                NewFolder.Path = string.IsNullOrEmpty(currentPath) ? childFolder.Name : currentPath + "/" + childFolder.Name;
                folderList.Add(NewFolder);

                // Recursively list nested folders
                ListVmFolders(vimClient, childEntity, folderList, NewFolder.Path);
            }
        }
        return folderList;
    }
}