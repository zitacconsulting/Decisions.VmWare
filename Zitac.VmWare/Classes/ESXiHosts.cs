using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VMware.Vim;

namespace Zitac.VmWare.Steps;

[DataContract]
public class ESXiHost
{
    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? ID { get; set; }

    [DataMember]
    public string? ConnectionState { get; set; }

    [DataMember]
    public bool? InMaintenanceMode { get; set; }

    [DataMember]
    public string? PowerState { get; set; }

    [DataMember]
    public string? Version { get; set; }
    
    [DataMember]
    public string? Build { get; set; }
    
    [DataMember]
    public int? CpuCores { get; set; }
    
    [DataMember]
    public int? CpuThreads { get; set; }
    
    [DataMember]
    public long? MemorySize { get; set; }
    
    [DataMember]
    public string? ClusterID { get; set; }
    
    [DataMember]
    public string? ClusterName { get; set; }
    
    public ESXiHost() { }

    public ESXiHost(HostSystem host)
    {
        this.Name = host.Name;
        this.ID = host.MoRef.Value;
        this.ConnectionState = host.Runtime.ConnectionState.ToString();
        this.InMaintenanceMode = host.Runtime.InMaintenanceMode;
        this.PowerState = host.Runtime.PowerState.ToString();
        this.Version = host.Config?.Product?.Version;
        this.Build = host.Config?.Product?.Build;
        
        // CPU and Memory information
        if (host.Hardware?.CpuInfo != null)
        {
            this.CpuCores = host.Hardware.CpuInfo.NumCpuCores;
            this.CpuThreads = host.Hardware.CpuInfo.NumCpuThreads;
        }
        
        this.MemorySize = host.Hardware?.MemorySize;

        
        // Cluster information
        if (host.Parent != null)
        {
            this.ClusterID = host.Parent.Value;
            // Note: To get the cluster name, we would need to query the ClusterComputeResource object
            // This would require additional API calls which is not done here for simplicity
        }
    }
}