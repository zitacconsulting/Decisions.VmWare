using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class DistributedVirtualPortgroup
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    public DistributedVirtualPortgroup() { }

    public DistributedVirtualPortgroup(VMware.Vim.DistributedVirtualPortgroup distributedVirtualPortgroup)
    {
        this.Name = distributedVirtualPortgroup.Name;
        this.ID = distributedVirtualPortgroup.MoRef.Value;
    }

}

