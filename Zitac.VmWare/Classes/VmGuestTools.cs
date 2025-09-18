using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class VmGuestTools
{

    [DataMember]
    public string? ToolsStatus { get; set; }
    
    [DataMember]
    public string? ToolsRunningStatus { get; set; }

    [DataMember]
    public string? OSFullName { get; set; }

    [DataMember]
    public string? Hostname { get; set; }

    [DataMember]
    public string? IpAddress { get; set; }


    public VmGuestTools() { }

    public VmGuestTools(GuestInfo info)
    {
        this.ToolsStatus = info.ToolsStatus.ToString();
        this.ToolsRunningStatus = info.ToolsRunningStatus;
        this.OSFullName = info.GuestFullName;
        this.Hostname = info.HostName;
        this.IpAddress = info.IpAddress;

    }
}

