using System.Net;
using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get All VMs", "Integration", "VmWare", "Virtual Machine")]
[Writable]
public class GetAllVMs : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
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
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(String), "Virtual Machines", true)));
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


        // Connect to vSphere server
        var vimClient = new VimClientImpl();
        if (ignoreSSLErrors) {
            vimClient.IgnoreServerCertificateErrors = true;
        }
        vimClient.Connect("https://" + Hostname + "/sdk");
        vimClient.Login(Credentials.Username, Credentials.Password);

        // List VMs on server
        NameValueCollection filter = new NameValueCollection();
        var vmList = vimClient.FindEntityViews(typeof(VirtualMachine), null, filter, null);

 //       List<String> VMs = new List<String>();
 //       foreach (VirtualMachine vm in vmList)
 //       {
            //Console.WriteLine(vm.);
 //           Console.WriteLine(vm.Name);
 //           VMs.Add(vm.Name);
 //       }
 List<string> VMs = new List<string> { "Alice", "Bob", "Charlie" };
        
        // Disconnect from vSphere server
        vimClient.Logout();
        vimClient.Disconnect();

                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("Virtual Machines", (object)VMs.ToArray());
                return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}