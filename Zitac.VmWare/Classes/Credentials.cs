using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Service.Debugging.DebugData;
using System.Runtime.Serialization;


namespace Zitac.VmWare.Steps;

        [DataContract]
        public class Credentials : IDebuggerJsonProvider
    {
        [DataMember]
        [WritableValue]
        public string? Username { get; set; }

        [DataMember]
        [WritableValue]
        [PasswordText]
        public string? Password { get; set; }

        public object GetJsonDebugView()
        {
                return new
                {
                   Username = this.Username,
                   Password = "********"
                };
        }
    }