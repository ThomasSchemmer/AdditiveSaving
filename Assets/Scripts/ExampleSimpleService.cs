using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExampleSimpleService : ISaveableService
{
    [SaveableClass]
    public ExampleScriptable Scriptable;

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public override void Reset()
    {
        Scriptable = null;
    }

}
