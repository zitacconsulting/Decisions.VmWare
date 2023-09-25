using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class Network
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    public Network(){}

    public Network(VMware.Vim.Network network)
    {
                        this.Name = network.Name;
                        this.ID = network.MoRef.Value;
    }
}