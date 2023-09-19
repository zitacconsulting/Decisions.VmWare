using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class Cluster
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

}

