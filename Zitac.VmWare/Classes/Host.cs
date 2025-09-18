using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Service.Debugging.DebugData;
using System.Runtime.Serialization;
using DecisionsFramework.ServiceLayer.Utilities; 

namespace Zitac.VmWare.Steps;
        [AutoRegisterNativeType]
        [DataContract]
        public class Host : IDebuggerJsonProvider
    {
        [DataMember]
        [WritableValue]
        public string? Hostname { get; set; }

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
                   Hostname = this.Hostname,
                   Username = this.Username,
                   Password = "********"
                };
        }
    }