using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class VMBase
{

    [DataMember]
    public string? Name { get; set; }
    
    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public string? Hostname { get; set; }

    [DataMember]
    public string? VMHost { get; set; }

    public VMBase() { }

    public VMBase(VirtualMachine machine, string VMHost)
    {
        this.Name = machine.Name;
        this.ID = machine.MoRef.Value;
        if(machine.Guest != null) {
        this.Hostname = machine.Guest.HostName;
        }
        this.VMHost = VMHost;
    }
}

