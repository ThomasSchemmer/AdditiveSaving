using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ExampleService : ISaveableService
{

    [Flags]
    public enum FlagEnum : uint
    {
        Zero = 0,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Four = 1 << 3,
    }

    [SaveableEnum]
    public FlagEnum Test;

    [SaveableBaseType]
    public int TestVar;

    [SaveableArray]
    public int[,] Data = new int[,]{
        {0, 1, 2, 3, 4 },
        {1, 2, 3, 4, 5 },
        {3, 4, 5, 6, 7 },
    };


    [SaveableClass]
    public ExampleData TestData = new();

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public override void Reset()
    {
        TestVar = -1;
        TestData = ExampleData.CreateEmpty();
        Test = FlagEnum.Zero;
        Data = new int[0, 0];
    }
}

[Serializable]
public class ExampleData
{
    [SaveableBaseType]
    public string Text;
    [SaveableBaseType]
    public Vector3 Vector;
    [SaveableList]
    public List<int> ListInt;
    [SaveableList]
    public List<ExampleListItem> ListItems;

    public static ExampleData CreateEmpty()
    {
        ExampleData Data = new();
        Data.Text = "";
        Data.Vector = Vector3.one * -1;
        Data.ListInt = new();
        Data.ListItems = new();
        return Data;
    }
}

[Serializable]
public class ExampleListItem
{
    [SaveableBaseType]
    public double Value = 0;

    public ExampleListItem(double Value)
    {
        this.Value = Value;
    }

    public ExampleListItem() { }
}
