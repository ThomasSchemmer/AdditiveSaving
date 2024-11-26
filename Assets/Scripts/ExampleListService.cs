using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleListService : ISaveableService
{
    [SaveableList]
    public List<int> TestList = new() {};

    public GameObject GetGameObject()
    {
        return gameObject;
    }
    public override void Reset()
    {
        TestList = new();
    }
}
