using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class Datacenter
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    public Datacenter(){}

    public Datacenter(VMware.Vim.Datacenter datacenter){
        this.Name = datacenter.Name;
        this.ID = datacenter.MoRef.Value;
    }
}

