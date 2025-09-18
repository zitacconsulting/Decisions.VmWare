using System.Runtime.Serialization;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class Snapshot
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public string? State { get; set; }

    [DataMember]
    public DateTime? CreateTime { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public Snapshot[]? Children { get; set; }

}