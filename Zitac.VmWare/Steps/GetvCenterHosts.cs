using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get vCenter Hosts", "Integration", "VmWare", "vCenter")]
[Writable]
public class GetvCenterHosts : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool multipleServers;

    [WritableValue]
    private bool showOutcomeforNoResults;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }


    [PropertyClassification(0, "Provide Multiple Hosts", new string[] { "Settings" })]
    public bool MultipleServers
    {
        get { return multipleServers; }
        set
        {
            multipleServers = value;
            this.OnPropertyChanged("InputData");
        }

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
            if (multipleServers)
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Host)), "Hostnames", true, false, false));
            }
            else
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Credentials)), "Credentials"));
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostname"));
            }
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(ESXiHost), "Hosts", true)));

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

        Credentials Credentials = data.Data["Credentials"] as Credentials;
        List<Host> Servers = new List<Host>();
        if (multipleServers)
        {
            Servers.AddRange(data.Data["Hostnames"] as Host[]);
        }
        else
        {
            Host SingleHost = new Host();
            SingleHost.Hostname = data.Data["Hostname"] as string;
            SingleHost.Username = Credentials.Username;
            SingleHost.Password = Credentials.Password;
            Servers.Add(SingleHost);
        }

        List<ESXiHost> Hosts = new List<ESXiHost>();

        // Connect to vSphere server
        var vimClient = new VimClientImpl();
        if (ignoreSSLErrors)
        {
            vimClient.IgnoreServerCertificateErrors = true;
        }
        try
        {
            foreach (Host Server in Servers)
            {
                vimClient.Connect("https://" + Server.Hostname + "/sdk");
                vimClient.Login(Server.Username, Server.Password);

                ManagedObjectReference searchRoot = new ManagedObjectReference();


                // Retrieve ServiceContent
                ServiceContent serviceContent = vimClient.ServiceContent;
                searchRoot = serviceContent.RootFolder;


                List<EntityViewBase> hosts = vimClient.FindEntityViews(typeof(VMware.Vim.HostSystem), searchRoot, null, VMwarePropertyLists.HostSystemProperties);

                // Disconnect from vSphere server
                vimClient.Logout();
                vimClient.Disconnect();

                if (hosts != null)
                {
                    foreach (EntityViewBase evb in hosts)
                    {
                        VMware.Vim.HostSystem hostSystem = evb as VMware.Vim.HostSystem;
                        if (hostSystem != null)
                        {
                            ESXiHost NewHost = new ESXiHost(hostSystem);
                            Hosts.Add(NewHost);
                        }
                    }
                }
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

        if (ShowOutcomeforNoResults && Hosts.Count == 0)
        {
            return new ResultData("No Results");
        }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("Hosts", (object)Hosts.ToArray());
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}