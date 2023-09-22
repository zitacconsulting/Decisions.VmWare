using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class VM
{

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public string? OS { get; set; }

    [DataMember]
    public bool? Template { get; set; }

    [DataMember]
    public string? UUID { get; set; }

    [DataMember]
    public string? Version { get; set; }

    [DataMember]
    public int? MemoryMB { get; set; }

    [DataMember]
    public int? CPU { get; set; }

    [DataMember]
    public VmGuestTools? GuestTools { get; set; }    

    [DataMember]
    public VmDisk[] Disks { get; set; }

    [DataMember]
    public VmCDROM[] CDROMs { get; set; }

    [DataMember]
    public VmNic[] NICs { get; set; }

    public VM() { }

    public VM(VirtualMachine machine)
    {
        this.Name = machine.Name;
        this.ID = machine.MoRef.Value;
        this.OS = machine.Config.GuestFullName;
        this.Template = machine.Config.Template;
        this.UUID = machine.Config.Uuid;
        this.Version = machine.Config.Version;
        this.MemoryMB = machine.Config.Hardware.MemoryMB;
        this.CPU = machine.Config.Hardware.NumCPU;
        this.GuestTools = new VmGuestTools(machine.Guest);

        List<VmDisk> Disks = new List<VmDisk>();
        List<VmCDROM> CDROMs = new List<VmCDROM>();
        List<VmNic> NICs = new List<VmNic>();

        foreach (var device in machine.Config.Hardware.Device)
        {
            switch (device)
            {
                case VirtualDisk virtualDisk:
                    Disks.Add(new VmDisk(virtualDisk));
                    break;

                case VirtualEthernetCard virtualEthernetCard:
                    NICs.Add(new VmNic(virtualEthernetCard));
                    break;

                case VirtualCdrom virtualCdrom:
                    CDROMs.Add(new VmCDROM(virtualCdrom));
                    break;
            }
        }

        this.Disks = Disks.ToArray();
        this.NICs = NICs.ToArray();
        this.CDROMs = CDROMs.ToArray();


    }
}

