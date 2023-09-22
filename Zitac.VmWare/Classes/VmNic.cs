using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class VmNic
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? NetworkName { get; set; }

    [DataMember]
    public string? NetworkID { get; set; }

    [DataMember]
    public bool? Connected { get; set; }

    [DataMember]
    public bool? StartConnected { get; set; }

    [DataMember]
    public string? MacAddress { get; set; }

    public VmNic() { }

    public VmNic(VirtualEthernetCard nic)
    {
        this.Name = nic.DeviceInfo.Label;
        if (nic.Backing is VirtualEthernetCardNetworkBackingInfo networkBacking)
        {
            this.NetworkName = networkBacking.DeviceName.ToString();
            if (networkBacking.Network is not null) {
            this.NetworkID = networkBacking.Network.Value;
            }
        }
        this.Connected = nic.Connectable.Connected;
        this.StartConnected = nic.Connectable.StartConnected;
        this.MacAddress = nic.MacAddress;
    }

}

