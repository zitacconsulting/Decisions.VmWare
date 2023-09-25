using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class DatastoreCluster
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public long Capacity { get; set; }

    [DataMember]
    public long FreeSpace { get; set; }

    public DatastoreCluster() { }

    public DatastoreCluster(VMware.Vim.StoragePod pod)
    {

        this.Name = pod.Name;
        this.ID = pod.MoRef.Value;
        this.Capacity = pod.Summary.Capacity;
        this.FreeSpace = pod.Summary.FreeSpace;
    }
}

