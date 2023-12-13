using VMware.Vim;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using System.Text;


namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Run Powershell Script On VM", "Integration", "VmWare", "VM")]
[Writable]
public class RunPowershellScriptOnVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool waitForExecution;

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

    [PropertyClassification(6, "Wait For Execution", new string[] { "Settings" })]
    public bool WaitForExecution
    {
        get { return waitForExecution; }
        set
        {
            waitForExecution = value;
            this.OnPropertyChanged(nameof(WaitForExecution));
            this.OnPropertyChanged("SpecifyTimeout");
            this.OnPropertyChanged("OutcomeScenarios");

        }
    }

    [BooleanPropertyHidden("WaitForExecution", false)]
    [PropertyClassification(7, "Specify Timeout", new string[] { "Settings" })]
    public bool SpecifyTimeout
    {
        get { return specifyTimeout; }
        set
        {
            specifyTimeout = value;
            this.OnPropertyChanged(nameof(SpecifyTimeout));
            this.OnPropertyChanged("MaxTimeout");
            this.OnPropertyChanged("OutcomeScenarios");

        }
    }


    [BooleanPropertyHidden("SpecifyTimeout", false)]
    [BooleanPropertyHidden("WaitForExecution", false)]
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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(Credentials)), "Execution Credentials"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "Script"));
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            if (WaitForExecution)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(Int32), "Return Code")));
            }
            else
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Done"));
            }
            if (WaitForExecution && SpecifyTimeout)
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
        Credentials ExecutionCredentials = data.Data["Execution Credentials"] as Credentials;
        string VmID = data.Data["VMID"] as string;
        string Script = data.Data["Script"] as string;

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
            if (vm.Guest.ToolsVersionStatus == "guestToolsNotInstalled")
            {
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "VMware Tools need to be installed on guest to execute commands"
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
                    (object) "Can only run commands on VM's in a running state"
                }
                });
            }

            NamePasswordAuthentication auth = new NamePasswordAuthentication();
            auth.Username = ExecutionCredentials.Username;
            auth.Password = ExecutionCredentials.Password;
            auth.InteractiveSession = false;

            string powerShellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

            byte[] scriptBytes = Encoding.Unicode.GetBytes(Script);
            string base64EncodedScript = Convert.ToBase64String(scriptBytes);

            GuestProgramSpec spec = new GuestProgramSpec
            {
                ProgramPath = powerShellPath,
                Arguments = "-NonInteractive -NoProfile -EncodedCommand " + base64EncodedScript
            };

            var guestOpMgr = (GuestOperationsManager)vimClient.GetView(vimClient.ServiceContent.GuestOperationsManager, null);
            var processManager = (GuestProcessManager)vimClient.GetView(guestOpMgr.ProcessManager, null);

            // Start the program in the guest OS
            long pid = processManager.StartProgramInGuest(vm.MoRef, auth, spec);

            if (WaitForExecution)
            {
                bool isRunning = true;
                int timeout = 5;
                while (isRunning)
                {
                    long[] pids = new long[] { pid }; // Create an array with the process ID
                    var processes = processManager.ListProcessesInGuest(vm.MoRef, auth, pids);
                    if (processes.Any(p => p.Pid == pid && p.EndTime != null))
                    {
                        isRunning = false;
                                                    vimClient.Logout();
                            vimClient.Disconnect();
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        dictionary.Add("Return Code", (object)processes[0].ExitCode);
                        return new ResultData("Done", (IDictionary<string, object>)dictionary);
                    }
                    else
                    {
                        timeout = timeout + 5;
                        if (specifyTimeout && timeout >= maxTimeout)
                        {
                            vimClient.Logout();
                            vimClient.Disconnect();
                            return new ResultData("Timeout");
                        }
                        Thread.Sleep(5000); // Wait before polling again
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