using VMware.Vim;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;


namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Reboot VM", "Integration", "VmWare", "VM")]
[Writable]
public class RebootVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool waitForReboot;

    [WritableValue]
    private bool specifyTimeout;

    [WritableValue]
    private Int32 maxTimeout;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }

    [PropertyClassification(6, "Wait For Reboot", new string[] { "Settings" })]
    public bool WaitForReboot
    {
        get { return waitForReboot; }
        set
        {
            waitForReboot = value;
            this.OnPropertyChanged(nameof(WaitForReboot));
            this.OnPropertyChanged("SpecifyTimeout");

        }
    }

    [BooleanPropertyHidden("WaitForReboot", false)]
    [PropertyClassification(7, "Specify Timeout", new string[] { "Settings" })]
    public bool SpecifyTimeout
    {
        get { return specifyTimeout; }
        set
        {
            specifyTimeout = value;
            this.OnPropertyChanged(nameof(SpecifyTimeout));
            this.OnPropertyChanged("MaxTimeout");

        }
    }


    [BooleanPropertyHidden("SpecifyTimeout", false)]
    [BooleanPropertyHidden("WaitForReboot", false)]
    [PropertyClassification(8, "Timeout In Sec", new string[] { "Settings" })]
    public Int32 MaxTimeout
    {
        get { return maxTimeout; }
        set
        {
            maxTimeout = value;
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

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done"));
            if (WaitForReboot && SpecifyTimeout)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Timeout"));
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
            vm.RebootGuest();
            if (WaitForReboot == true)
            {
                bool isRebooted = false;
                bool hasShutDown = false;
                int timeout = 5;
                while (!isRebooted)
                {
                    System.Threading.Thread.Sleep(5000);  // wait for 5 seconds before next poll

                    // Refresh the VirtualMachine object to get the latest guest info
                    vm.UpdateViewData("Guest");
                    Console.WriteLine(vm.Guest.GuestState);
                    Console.WriteLine(vm.Guest.ToolsStatus);
                    Console.WriteLine(vm.Guest.ToolsRunningStatus);
                    Console.WriteLine(hasShutDown);
                    Console.WriteLine(timeout);

                    // First check that the VM actually has been turned off
                    if (vm.Guest.ToolsRunningStatus == "guestToolsNotRunning")
                    {
                        hasShutDown = true;
                    }

                    // Check the guest OS status and tools status
                    if (vm.Guest.GuestState == "running" && vm.Guest.ToolsRunningStatus == "guestToolsRunning" && hasShutDown == true)
                    {
                        isRebooted = true;
                    }
                    timeout = timeout + 5;
                    if (specifyTimeout && timeout >= maxTimeout)
                    {
                        vimClient.Logout();
                        vimClient.Disconnect();
                        return new ResultData("Timeout");
                    }
                }
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