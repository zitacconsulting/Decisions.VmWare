using VMware.Vim;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping.InputImpl;

namespace Zitac.VmWare.Steps;

[AutoRegisterStep("Upload ISO", "Integration", "VmWare", "Storage")]
[Writable]
public class UploadISO : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer, IDefaultInputMappingStep
{
    [WritableValue]
    private bool ignoreSSLErrors;

    [PropertyClassification(0, "Ignore SSL Errors", new string[] { "Settings" })]
    public bool IgnoreSSLErrors
    {
        get { return ignoreSSLErrors; }
        set { ignoreSSLErrors = value; }

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
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "File Path"));
            dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(byte)), "File Content", true, false, false));

            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done"));
            outcomeScenarioDataList.Add(new OutcomeScenarioData("Error", new DataDescription(typeof(string), "Error Message")));
            return outcomeScenarioDataList.ToArray();
        }
    }

    public ResultData Run(StepStartData data)
    {
        string? Hostname = data.Data["Hostname"] as string;
        Credentials? Credentials = data.Data["Credentials"] as Credentials;
        string? DatacenterId = data.Data["Datacenter ID"] as string;
        string? FilePath = data.Data["File Path"] as string;
        byte[]? FileContent = data.Data["File Content"] as byte[];

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

        if (!FilePath.StartsWith("["))
        {
            throw new ArgumentException("Incorrect syntax of file path. File Path should be provided [Datastore] /path/file.iso");
        }

        int startIndex = FilePath.IndexOf('[') + 1;
        int endIndex = FilePath.IndexOf(']', startIndex);
        
        if (startIndex < 1 || endIndex < 0 || endIndex <= startIndex)
        {
            throw new ArgumentException("The path does not contain a valid datastore name. File Path should be provided [Datastore] /path/file.iso");
        }
        
        string datastoreName = FilePath.Substring(startIndex, endIndex - startIndex).Trim();
        string Path = FilePath.Substring(endIndex+1).TrimStart();

            ManagedObjectReference DatacenterMoref = new ManagedObjectReference();
            DatacenterMoref.Type = "Datacenter";
            DatacenterMoref.Value = DatacenterId;

            VMware.Vim.Datacenter Datacenter = vimClient.GetView(DatacenterMoref, null) as VMware.Vim.Datacenter;

            if (Datacenter == null)
            {
                throw new Exception("Could not find Datacenter with id" + DatacenterId);
            }


            string uploadUrl = "https://" + Hostname + "/folder/" + Path + "?dcPath=" + Datacenter.Name + "&dsName=" + datastoreName;
            Console.WriteLine(uploadUrl);

            UploadData(FileContent, uploadUrl, Credentials.Username, Credentials.Password, ignoreSSLErrors);


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


        return new ResultData("Done");


    }
    private static void UploadData(byte[] data, string uploadUrl, string Username, string Password, bool IgnoreSSLErrors)
    {
        var handler = new HttpClientHandler();
        if (IgnoreSSLErrors)
        {
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        };

        HttpClient client = new HttpClient(handler);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{Username}:{Password}")));

        using (ByteArrayContent content = new ByteArrayContent(data))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = client.PutAsync(uploadUrl, content).Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to upload. Status code: {response.StatusCode}");
            }
        }
    }
}