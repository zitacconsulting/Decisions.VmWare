using VMware.Vim;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;


namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Power On VM", "Integration", "VmWare", "VM")]
[Writable]
public class PowerOnVM : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [WritableValue]
    private bool alreadyRunning;

    [WritableValue]
    private bool waitForPowerOn;

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

    [PropertyClassification(0, "Show outcome for 'Already Running'", new string[] { "Settings" })]
    public bool AlreadyRunning
    {
        get { return alreadyRunning; }
        set { alreadyRunning = value; }

    }

    [PropertyClassification(6, "Wait For OS Boot", new string[] { "Settings" })]
    public bool WaitForPowerOn
    {
        get { return waitForPowerOn; }
        set
        {
            waitForPowerOn = value;
            this.OnPropertyChanged(nameof(WaitForPowerOn));
            this.OnPropertyChanged("SpecifyTimeout");

        }
    }

    [BooleanPropertyHidden("WaitForPowerOn", false)]
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
    [BooleanPropertyHidden("WaitForPowerOn", false)]
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
            if (WaitForPowerOn && SpecifyTimeout)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Timeout"));
            }
            if (AlreadyRunning)
            {
                outcomeScenarioDataList.Add(new OutcomeScenarioData("Already Running"));
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

            if (vm == null)
            {
                vimClient.Logout();
                vimClient.Disconnect();
                throw new Exception("Failed to add Find VM with ID:" + VmID);
            }



            if (WaitForPowerOn == true && vm.Guest.ToolsVersionStatus == "guestToolsNotInstalled")
            {
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "VMware Tools need to be installed on guest to allow 'Wait for OS Boot'"
                }
                });
            }
            if (vm.Runtime.PowerState.ToString() == "poweredOn")
            {
                if (AlreadyRunning)
                {
                    return new ResultData("Already Running");
                }
                return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) "VM Already in a running state"
                }
                });
            }
            ManagedObjectReference taskMor = vm.PowerOnVM_Task(null);

            VMware.Vim.Task TaskResult = (VMware.Vim.Task)vimClient.GetView(taskMor, null);
            while ((TaskResult.Info.State.ToString() == "running") || (TaskResult.Info.State.ToString() == "queued"))
            {
                //Console.WriteLine(TaskResult.Info.State);
                System.Threading.Thread.Sleep(2000);
                TaskResult.UpdateViewData();
            }

            if (TaskResult.Info.State.ToString() == "error")
            {
                throw new Exception("Failed to power on VM:" + TaskResult.Info.Error.Fault.ToString() + " - " + TaskResult.Info.Error.LocalizedMessage.ToString());
            }

            if (WaitForPowerOn == true)
            {
                bool isBooted = false;
                int timeout = 5;
                while (!isBooted)
                {
                    System.Threading.Thread.Sleep(5000);  // wait for 5 seconds before next poll

                    // Refresh the VirtualMachine object to get the latest guest info
                    vm.UpdateViewData("Guest");
                    Console.WriteLine(vm.Guest.GuestState);
                    Console.WriteLine(vm.Guest.ToolsStatus);
                    Console.WriteLine(vm.Guest.ToolsRunningStatus);
                    Console.WriteLine(timeout);


                    // Check the guest OS status and tools status
                    if (vm.Guest.GuestState == "running" && vm.Guest.ToolsRunningStatus == "guestToolsRunning")
                    {
                        isBooted = true;
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
            else {

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