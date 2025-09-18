using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class VmCDROM
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Type { get; set; }

    [DataMember]
    public string? FileOrDeviceName { get; set; }

    [DataMember]
    public string? DatastoreID { get; set; }

    [DataMember]
    public bool? Connected { get; set; }

    [DataMember]
    public bool? StartConnected { get; set; }

    [DataMember]
    public int? Key { get; set; }

    [DataMember]
    public int? UnitNumber { get; set; }

    [DataMember]
    public int? ControllerKey { get; set; }

    [DataMember]
    public int? ControllerLocation { get; set; }

    public VmCDROM() { }

    public VmCDROM(VirtualCdrom cd)
    {
        this.Name = cd.DeviceInfo.Label;
        if (cd.Backing is VirtualCdromIsoBackingInfo isoBacking)
        {
            this.FileOrDeviceName = isoBacking.FileName;
            this.Type = "ISO";
            if (isoBacking.Datastore is not null)
            {
                this.DatastoreID = isoBacking.Datastore.Value;
            }
        }
        else if (cd.Backing is VirtualCdromAtapiBackingInfo atapiBacking)
        {
            this.Type = "Host Device";
            this.FileOrDeviceName = atapiBacking.DeviceName;
        }
        this.Connected = cd.Connectable.Connected;
        this.StartConnected = cd.Connectable.StartConnected;
        this.Key = cd.Key;
        this.UnitNumber = cd.UnitNumber;
        this.ControllerKey = cd.ControllerKey;
        this.ControllerLocation = cd.UnitNumber;
    }

}

