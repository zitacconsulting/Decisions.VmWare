namespace Zitac.VmWare.Steps;

public static class VMwarePropertyLists
{
    public static readonly string[] VirtualMachineProperties = new string[]
    {
        "Name",
        "Config.Annotation",
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
    public static readonly string[] VirtualMachineBaseProperties = new string[]
    {
        "Name",
        "Guest.HostName"
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
        //"Host",
        "Summary.Capacity",
        "Summary.FreeSpace",
        "ChildEntity",
        "PodStorageDrsEntry.StorageDrsConfig.PodConfig"

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
        "Name",
        "Config.DistributedVirtualSwitch"
    };
    public static readonly string[] NetworkProperties = new string[]
    {
        "Name"
    };

    public static readonly string[] VmwareDistributedVirtualSwitchProperties = new string[]
    {
        "Name",
        "Uuid"

    };

    public static readonly string[] HostSystemProperties = new string[]
    {
    "Name",
    "Runtime.ConnectionState",
    "Runtime.InMaintenanceMode",
    "Runtime.PowerState",
    "Config.Product.Version",
    "Config.Product.Build",
    "Hardware.CpuInfo.NumCpuCores",
    "Hardware.CpuInfo.NumCpuThreads",
    "Hardware.MemorySize",
    "Parent"
    };
}