using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class Datastore
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public long Capacity { get; set; }

    [DataMember]
    public long FreeSpace { get; set; }

    public Datastore() { }

    public Datastore(VMware.Vim.Datastore datastore)
    {
        this.Name = datastore.Name;
        this.ID = datastore.MoRef.Value;
        this.Capacity = datastore.Summary.Capacity;
        this.FreeSpace = datastore.Summary.FreeSpace;
    }
}

