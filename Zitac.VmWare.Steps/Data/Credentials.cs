using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Service.Debugging.DebugData;
using System;
using System.Collections.Generic;

namespace Zitac.VmWare.Steps;

        public class Credentials : IDebuggerJsonProvider
    {
        [WritableValue]
        public string? Username { get; set; }

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