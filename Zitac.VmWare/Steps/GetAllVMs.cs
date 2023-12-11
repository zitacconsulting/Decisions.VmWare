using VMware.Vim;
using System.Collections.Specialized;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Get All VMs", "Integration", "VmWare", "VM")]
[Writable]
public class GetAllVMs : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, IDefaultInputMappingStep
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool getBaseInfo;

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

    [PropertyClassification(0, "Get Only Base Information", new string[] { "Settings" })]
    public bool GetBaseInfo
    {
        get { return getBaseInfo; }
        set
        {
            getBaseInfo = value;
            this.OnPropertyChanged("OutcomeScenarios");
        }

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
            if (multipleServers)
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostnames", true, false, false));
            }
            else
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Hostname"));
            }
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

            if (getBaseInfo)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(VMBase), "VMs", true)));
            }
            else
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(VM), "VMs", true)));
            }
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
        List<string> Servers = new List<string>();
        if (multipleServers) {
            Servers.AddRange(data.Data["Hostnames"] as string[]);
        }
        else {
        Servers.Add(data.Data["Hostname"] as string);
        }
        Credentials Credentials = data.Data["Credentials"] as Credentials;
        string DatacenterId = data.Data["Datacenter ID"] as string;


        List<VM> VMs = new List<VM>();
        List<VMBase> BaseVMs = new List<VMBase>();

        // Connect to vSphere server
        var vimClient = new VimClientImpl();
        if (ignoreSSLErrors)
        {
            vimClient.IgnoreServerCertificateErrors = true;
        }
        try
        {
            foreach (string Server in Servers) {
            vimClient.Connect("https://" + Server + "/sdk");
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

            string[] Properties;
            if (getBaseInfo)
            {
                Properties = VMwarePropertyLists.VirtualMachineBaseProperties;
            }
            else
            {
                Properties = VMwarePropertyLists.VirtualMachineProperties;
            }

            List<EntityViewBase> vms = vimClient.FindEntityViews(typeof(VMware.Vim.VirtualMachine), searchRoot, null, Properties);

            // Disconnect from vSphere server
            vimClient.Logout();
            vimClient.Disconnect();

            if (vms != null)
            {
                foreach (EntityViewBase evb in vms)
                {
                    VMware.Vim.VirtualMachine vm = evb as VMware.Vim.VirtualMachine;
                    if (getBaseInfo == false && vm != null && vm.Config != null)
                    {
                        VM NewVm = new VM(vm);
                        VMs.Add(NewVm);
                    }
                    if (getBaseInfo && vm != null)
                    {
                        Console.WriteLine(vm.Name);
                        VMBase NewVM = new VMBase(vm, Server);
                        BaseVMs.Add(NewVM);
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
            
                if (ShowOutcomeforNoResults && BaseVMs.Count == 0 && VMs.Count == 0)
                {
                    return new ResultData("No Results");
                }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        if (getBaseInfo)
        {
            dictionary.Add("VMs", (object)BaseVMs.ToArray());
        }
        else
        {
            dictionary.Add("VMs", (object)VMs.ToArray());
        }
        return new ResultData("Done", (IDictionary<string, object>)dictionary);


    }
}