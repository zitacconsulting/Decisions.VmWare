using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Zitac.VmWare.Steps;

[DataContract]
public class FolderWithPath
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public string? Path { get; set; }

}

