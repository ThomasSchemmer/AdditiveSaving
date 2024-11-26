using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleInheritance : ExampleService
{
    [SaveableBaseType]
    public string TestString;

    public override void Reset()
    {
        base.Reset();
        TestString = string.Empty;
    }
}
