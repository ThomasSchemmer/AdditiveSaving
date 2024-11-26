using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExampleSimpleService : ISaveableService
{
    /*
    [Flags]
    public enum FlagEnum : uint
    {
        None = 0,
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
        D = 1 << 3,
    }

    [SaveableDictionary]
    public SerializedDictionary<FlagEnum, List<int>> TestDic = new();
    */

    [SaveableArray]
    public int[] TestArray;

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public override void Reset()
    {
        TestArray = new int[0];
    }
}
