using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class VmDisk
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public long? CapacityInBytes { get; set; }
    
    [DataMember]
    public int? Key { get; set; }

    [DataMember]
    public string? Type { get; set; }

    [DataMember]
    public string? DiskMode { get; set; }

    [DataMember]
    public string? DiskFile { get; set; }

    [DataMember]
    public string? DatastoreID { get; set; }

    [DataMember]
    public int? Controller { get; set; }

    [DataMember]
    public int? ControllerLocation { get; set; }





    public VmDisk() { }

    public VmDisk(VirtualDisk disk)
    {
        this.Name = disk.DeviceInfo.Label;
        this.CapacityInBytes = disk.CapacityInBytes;
        this.Key = disk.Key;
        this.Controller = disk.ControllerKey;
        this.ControllerLocation = disk.UnitNumber;

        if (disk.Backing is VirtualDiskFlatVer2BackingInfo diskBacking)
        {
            this.DiskMode = diskBacking.DiskMode;
            this.DiskFile = diskBacking.FileName;
            if (diskBacking.ThinProvisioned is true) {
                this.Type = "Thin provisioned";
            }
            else if(diskBacking.EagerlyScrub is true){
                this.Type = "Thick provisioned, eagerly zeroed";
            }
            else {
                this.Type = "Thick provisioned, lazily zeroed";
            }
 
            if (diskBacking.Datastore is not null)
            {
                this.DatastoreID = diskBacking.Datastore.Value;
            }
        }

    }
}

