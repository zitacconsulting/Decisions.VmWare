using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class Network
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Type { get; set; }

    [DataMember]
    public string? ID { get; set; }

    public Network(){}

    public Network(VMware.Vim.Network network)
    {
                        this.Name = network.Name;
                        this.ID = network.MoRef.Value;
                        if(network.GetType().ToString() == "VMware.Vim.DistributedVirtualPortgroup") {
                            this.Type = "Portgroup";
                        }
                        else if(network.GetType().ToString() == "VMware.Vim.Network")
                        {
                            this.Type = "Network";
                        }
    }
}