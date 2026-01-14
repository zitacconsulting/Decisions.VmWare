# Zitac VMware Module (Decisions.VmWare)

> ⚠️ **Important:** Use this module at your own risk. See the **Disclaimer** section below.

## Overview ✅

**Zitac VMware Module** adds a collection of Decisions steps that integrate with VMware vSphere environments. It provides steps to create, manage, and query virtual machines, datastores, networks, snapshots, and more—allowing Decisions flows to interact with vCenter/ESXi programmatically.


## Features 🔧

This module implements a broad set of steps (located under `Integrations/VMware`) including but not limited to:

- VM lifecycle: `CreateVM`, `DeleteVM`, `PowerOnVM`, `PowerOffVM`, `RebootVM`, `Rename VM`
- Snapshots: `CreateSnapshot`, `RemoveSnapshot`, `RevertToSnapshot`, `GetSnapShotsByVM`
- Storage & ISOs: `GetDatastores`, `GetDatastoreClusters`, `UploadISO`, `GetFoldersByDatastore`
- Networking: `GetNetworks`, `ChangeNetwork`, `GetDistributedVirtualPortgroups`
- Hardware: `AddDisk`, `AddCDROM`, `RemoveCDROM`, `DisconnectCDROM`, `UploadISO`
- Inventory queries: `GetAllVMs`, `GetVMByID`, `GetVMByName`, `GetDatacenters`, `GetClusters`, `GetFolderStructureByDatacenter`
- Utilities: `RunPowershellScriptOnVM`, `SetVMNotes`, `GetvCenterHosts`

(See the `Integrations/VMware` folder for the complete list and implementation details.)


## Disclaimer

This module is provided "as is" without warranties of any kind. Use it at your own risk. The authors, maintainers, and contributors disclaim all liability for any direct, indirect, incidental, special, or consequential damages, including data loss or service interruption, arising from the use of this software.

## Requirements

- Decisions Platform 9+
- AWS credentials with access to target Kinesis stream

## Installation

1. Download or compile the module and upload it via Decisions Portal (System > Administration > Features).
2. Configure system and queue settings as needed, then create queues and handlers.