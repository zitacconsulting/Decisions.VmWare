using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Zitac.VmWare.Steps;

[AutoRegisterNativeType]
[DataContract]
public class UnitController
{

    public int UnitNumber { get; set; }

    public int ControllerKey { get; set; }

    public bool ControllerFound { get; set; }

    public int BusNumber { get; set; }

}

