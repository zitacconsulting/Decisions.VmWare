# Zitac VMware Module (Decisions.VmWare)

> ⚠️ **Important:** Use this module at your own risk. See the **Disclaimer** section below.

## Overview

**Zitac VMware Module** is a comprehensive integration module for the Decisions no-code automation platform that enables interaction with VMware vSphere environments. It provides workflow steps to create, manage, and query virtual machines, datastores, networks, snapshots, and infrastructure components through vCenter/ESXi.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Available Steps](#available-steps)
- [Supported Configurations](#supported-configurations)
- [Usage Examples](#usage-examples)
- [Configuration Options](#configuration-options)
- [Building from Source](#building-from-source)
- [Troubleshooting](#troubleshooting)
- [License](#license)
- [Disclaimer](#disclaimer)

## Features

This module provides 32 Decisions workflow steps organized under `Integration/VmWare` category:

### Virtual Machine Lifecycle
- `CreateVM` - Create new virtual machines with full hardware configuration
- `DeleteVM` - Remove virtual machines
- `PowerOnVM` - Power on virtual machines
- `PowerOffVM` - Power off virtual machines
- `RebootVM` - Restart virtual machines
- `Rename VM` - Rename existing virtual machines

### Snapshot Management
- `CreateSnapshot` - Create VM snapshots
- `RemoveSnapshot` - Delete VM snapshots
- `RevertToSnapshot` - Restore VM to a snapshot state
- `GetSnapShotsByVM` - List all snapshots for a VM

### Storage Operations
- `GetDatastores` - Retrieve available datastores
- `GetDatastoreClusters` - List datastore clusters
- `GetFoldersByDatastore` - Browse datastore folder structure
- `UploadISO` - Upload ISO files to datastores
- `AddDisk` - Add virtual disks to VMs

### Networking
- `GetNetworks` - List available networks
- `GetDistributedVirtualPortgroups` - Retrieve distributed virtual port groups
- `ChangeNetwork` - Modify VM network connections

### Hardware Management
- `AddCDROM` - Add CD/DVD drives to VMs
- `RemoveCDROM` - Remove CD/DVD drives from VMs
- `DisconnectCDROM` - Disconnect CD/DVD drives
- `AddDisk` - Add additional disks to VMs

### Inventory & Discovery
- `GetAllVMs` - Retrieve all virtual machines
- `GetVMByID` - Get VM details by managed object ID
- `GetVMByName` - Find VM by name
- `GetDatacenters` - List all datacenters
- `GetDatacenterById` - Get specific datacenter details
- `GetClusters` - Retrieve compute clusters
- `GetvCenterHosts` - List ESXi hosts
- `GetFolderByID` - Get folder details
- `GetFolderStructureByDatacenter` - Browse datacenter folder hierarchy

### Utilities
- `RunPowershellScriptOnVM` - Execute PowerShell scripts on VMs (requires VMware Tools)
- `SetVMNotes` - Update VM annotations/notes

## Requirements

### Platform Requirements
- **Decisions Platform**: Version 9.0 or higher
- **.NET Runtime**: .NET 9.0
- **VMware Environment**: vCenter Server or ESXi 6.5+

### VMware Permissions
The vCenter/ESXi user account requires appropriate permissions for intended operations:
- Virtual machine management (create, modify, delete)
- Datastore access (read/write for ISO uploads)
- Network configuration
- Snapshot operations
- Resource pool and cluster access

### Dependencies
- VMware.Vim API libraries
- VMware.System.Private.ServiceModel
- VMware.Binding.WsTrust
- VMware.Binding.Wcf
- VimService

## Installation

### Option 1: Install Pre-built Module
1. Download the compiled module (`.zip` file)
2. Log into Decisions Portal
3. Navigate to **System > Administration > Features**
4. Click **Install Module**
5. Upload the module file
6. Restart the Decisions service if prompted

### Option 2: Build and Install
See [Building from Source](#building-from-source) section below.

### Post-Installation
After installation, the VMware steps will be available in the Flow Designer under:
```
Integration > VmWare > [Category]
```

## Available Steps

All 32 steps are organized into logical categories within the Decisions Flow Designer. Each step includes:
- Input validation
- Error handling with detailed error messages
- Support for SSL certificate validation options


## Usage Examples

### Example 1: Create a Virtual Machine

```
Step Input:
- Hostname: "vcenter.example.com"
- Credentials: Username/Password
- VM Name: "WebServer01"
- Datacenter ID: "datacenter-2"
- Cluster ID: "domain-c7"
- Datastore ID: "datastore-10"
- Network ID: "network-15"
- OS Type: "ubuntu64Guest"
- Firmware: "efi"
- CPU: 4
- Memory (GB): 8
- Disk Size (GB): 100
- SCSI Controller: "Paravirtual"
- Network Adapter Type: "VMXNET3"

Output:
- Virtual Machine object with all details
```

### Example 2: Snapshot Workflow

1. **CreateSnapshot** - Create snapshot before maintenance
2. Perform maintenance operations
3. **GetSnapShotsByVM** - Verify snapshot exists
4. If issues occur: **RevertToSnapshot**
5. If successful: **RemoveSnapshot** after validation

### Example 3: Bulk VM Information Retrieval

1. **GetAllVMs** - Retrieve all VMs
2. Loop through results
3. **GetVMByID** - Get detailed information for each
4. Export to database or generate report

## Configuration Options

### SSL Certificate Validation
Many steps include an **"Ignore SSL Errors"** setting for environments with self-signed certificates:
- **Enabled (true)**: Skip SSL certificate validation (not recommended for production)
- **Disabled (false)**: Enforce SSL certificate validation (default)

### Storage DRS Support
The `CreateVM` step supports Storage DRS (Datastore Clusters):
- **Use Storage DRS**: Enable to deploy VMs to datastore clusters with automatic datastore selection
- Automatically applies storage placement recommendations

### Advanced VM Settings
When creating VMs, advanced options include:
- **CPU Hot Plug**: Enable/disable CPU hot-add capability
- **Memory Hot Plug**: Enable/disable memory hot-add capability
- **Synchronize Time with Host**: Auto-sync VM time with ESXi host
- **Secure Boot**: Enable EFI secure boot (requires EFI firmware)
- **Extra Config**: Key-value pairs for advanced VM configuration parameters

## Building from Source

### Prerequisites
- .NET 9.0 SDK
- CreateDecisionsModule Global Tool (installed automatically during build)
- VMware vSphere API DLL files (included in project)

### Build Steps

The module uses the Decisions `CreateDecisionsModule` tool to package the module according to the configuration in `Module.Build.json`.

#### On Linux/macOS:
```bash
chmod +x build_module.sh
./build_module.sh
```

#### On Windows (PowerShell):
```powershell
.\build_module.ps1
```

#### Manual Build:
If you prefer to build manually:
```bash
# 1. Publish the project
dotnet publish ./Zitac.VmWare/Zitac.VmWare.Steps.csproj --self-contained false --output ./Zitac.VmWare/bin -c Debug

# 2. Install/Update CreateDecisionsModule tool
dotnet tool update --global CreateDecisionsModule-GlobalTool

# 3. Create the module package
CreateDecisionsModule -buildmodule Zitac.VMware -output "." -buildfile Module.Build.json
```

### Build Output
The build process creates a `Zitac.VMware.zip` file in the root directory containing:
- Compiled module DLL (Zitac.VmWare.Steps.dll)
- VMware API dependencies:
  - VMware.Vim.dll
  - VMware.System.Private.ServiceModel.dll
  - VMware.Binding.WsTrust.dll
  - VMware.Binding.Wcf.dll
  - VimService.dll
- Module icon (vmware.svg)
- Module metadata

This ZIP file can be uploaded directly to Decisions via **System > Administration > Features**.

## Troubleshooting

### SSL Certificate Errors
**Problem**: "The remote certificate is invalid according to the validation procedure"

**Solution**:
- Enable "Ignore SSL Errors" setting on the step (development only)
- Install proper SSL certificates on vCenter (production)
- Import vCenter certificate to Decisions server trusted root

### Authentication Failures
**Problem**: "Login failed" or "Access denied"

**Solution**:
- Verify username/password credentials
- Check account is not locked or expired
- Ensure account has required vCenter permissions
- Use username format: `user@domain` or `domain\user`

### Insufficient Permissions
**Problem**: "Permission to perform this operation was denied"

**Solution**:
- Review vCenter role assignments
- Grant necessary privileges for VM operations
- Check datacenter/cluster/resource pool permissions

### VM Creation Fails with Storage DRS
**Problem**: "No datastore recommendations available"

**Solution**:
- Verify Storage DRS is enabled on the datastore cluster
- Check datastore cluster has sufficient free space
- Ensure datastores in cluster are accessible to target host

### VMware Tools Required
**Problem**: "RunPowershellScriptOnVM" fails

**Solution**:
- Install VMware Tools in the guest VM
- Ensure VMware Tools is running
- Verify guest credentials are correct

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

Copyright (c) 2022-2026 Zitac Consulting AB

## Disclaimer

This module is provided "as is" without warranties of any kind. Use it at your own risk. The authors, maintainers, and contributors disclaim all liability for any direct, indirect, incidental, special, or consequential damages, including data loss or service interruption, arising from the use of this software.

**Important Notes:**
- Always test in a non-production environment first
- Ensure proper backups before performing destructive operations
- Review VMware and Decisions documentation for best practices
- This module is not officially supported by VMware or Decisions
