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

}

