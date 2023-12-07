using VMware.Vim;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;


namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Power Off VM", "Integration", "VmWare", "VM")]
[Writable]
public class PowerOffVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool waitForPowerOff;

    [WritableValue]
    private bool hardPowerOff;


    [WritableValue]
    private bool specifyTimeout;

    [WritableValue]
    private bool notRunning;

    [WritableValue]
    private Int32 maxTimeout;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

    }

    [PropertyClassification(0, "Show outcome for 'Not Running'", new string[] { "Settings" })]
    public bool NotRunning
    {
        get { return notRunning; }
        set { notRunning = value; }

    }

    [PropertyClassification(6, "Wait For Power Off", new string[] { "Settings" })]
    public bool WaitForPowerOff
    {
        get { return waitForPowerOff; }
        set
        {
            waitForPowerOff = value;
            this.OnPropertyChanged(nameof(WaitForPowerOff));
            this.OnPropertyChanged("SpecifyTimeout");

        }
    }

    [PropertyClassification(0, "Perform Hard PowerOff", new string[] { "Settings" })]
    public bool HardPowerOff
    {
        get { return hardPowerOff; }
        set { hardPowerOff = value; }

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
            if (WaitForPowerOff && SpecifyTimeout)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Timeout"));
            }
            if (NotRunning)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Not Running"));
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

            var vm = vimClient.GetView(vmMor, VMwarePropertyLists.VirtualMachineProperties) as VirtualMachine;
            if (HardPowerOff == false && vm.Guest.ToolsVersionStatus == "guestToolsNotInstalled")
            {
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "VMware Tools need to be installed on guest to perform graceful power off"
                }
                });
            }
            if (vm.Runtime.PowerState.ToString() != "poweredOn")
            {
                if (NotRunning)
                {
                    return new ResultData("Not Running");
                }
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "Can only power off VM's in a running state"
                }
                });
            }
            if (HardPowerOff)
            {

                ManagedObjectReference taskMor = vm.PowerOffVM_Task();
            }
            else
            {
                vm.ShutdownGuest();
            }

            if (WaitForPowerOff == true)
            {
                bool hasShutDown = false;
                int timeout = 5;
                while (!hasShutDown)
                {
                    System.Threading.Thread.Sleep(3000);  // wait for 3 seconds before next poll

                    // Refresh the VirtualMachine object to get the latest guest info
                    vm.UpdateViewData("Runtime");

                    // First check that the VM actually has been turned off
                    if (vm.Runtime.PowerState.ToString() == "poweredOff")
                    {
                        hasShutDown = true;
                    }
                    else
                    {
                        timeout = timeout + 3;
                        if (specifyTimeout && timeout >= maxTimeout)
                        {
                            vimClient.Logout();
                            vimClient.Disconnect();
                            return new ResultData("Timeout");
                        }
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