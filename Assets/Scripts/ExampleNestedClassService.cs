using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleNestedClassService : ISaveableService
{
    [Serializable]
    public class NestedClass
    {
        [SaveableBaseType]
        public int Var;

        [SaveableList]
        public List<string> Texts = new();
    }

    [SaveableClass]
    public NestedClass NestedTest = new();

    public GameObject GetGameObject()
    {
        return gameObject;
    }


    public override void Reset()
    {
        NestedTest = new();
    }
}
