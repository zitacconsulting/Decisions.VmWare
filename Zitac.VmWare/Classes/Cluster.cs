using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class Cluster
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    public Cluster(){}
    public Cluster(VMware.Vim.ClusterComputeResource cluster)
    {
        this.Name = cluster.Name;
        this.ID = cluster.MoRef.Value;
    }

}

