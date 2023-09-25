namespace Zitac.VmWare.Steps;
public static class VMwarePropertyLists
{
    public static readonly string[] VirtualMachineProperties = new string[]
    {
        "Name",
        "Config.Template",
        "Config.Uuid",
        "Config.Version",
        "Config.Hardware.MemoryMB",
        "Config.Hardware.NumCPU",
        "Config.Hardware.Device",
        "Config.GuestFullName",
        "Runtime.PowerState",
        "Guest"
    };
    public static readonly string[] ClusterProperties = new string[]
    {
        "Name"
    };

    public static readonly string[] DatacenterProperties = new string[]
    {
        "Name"
    };
    public static readonly string[] DatastoreClusterProperties = new string[]
    {
        "Name",
        "Host",
        "Summary.Capacity",
        "Summary.FreeSpace"

    };
        public static readonly string[] DatastoreProperties = new string[]
    {
        "Name",
        "Host",
        "Summary.Capacity",
        "Summary.FreeSpace"

    };
    public static readonly string[] DistributedVirtualPortgroupProperties = new string[]
    {
        "Name"
    };
    public static readonly string[] NetworkProperties = new string[]
    {
        "Name"
    };
}