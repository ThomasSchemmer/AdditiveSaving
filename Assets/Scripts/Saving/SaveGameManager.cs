using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    public enum VariableType : uint
    {
        None = 0,
        Byte,
        Int, 
        Uint,
        Double,
        String,
        Vector3,
        ClassStart,
        ClassEnd,
        ListStart,
        ListEnd,
        ArrayStart,
        ArrayEnd,
        WrapperStart,
        WrapperEnd,
        EnumStart,
        EnumEnd,
        DictionaryStart,
        DictionaryEnd,
    }

    public enum SaveGameType
    {
        Map,
        Relics,
        Buildings
    }

    public SerializedDictionary<SaveGameType, GameObject> Saveables = new();

    
    public void Start()
    {
        byte[] Data = Save();
        Load(Data);
    }

    //**************************** Main Functions **************************************************************
    
    public byte[] Save()
    {
        List<byte> Data = new List<byte>();
        foreach (var Tuple in Saveables)
        {
            ISaveableService Service = Tuple.Value.GetComponent<ISaveableService>();
            Data.AddRange(Service.Save(Tuple.Key, Service.gameObject.name));
            Service.Reset();
        }

        return Data.ToArray();
    }

    public void Load(byte[] Data)
    {
        SaveableData.IterateData(Data, 0, Data.Length, out var FoundSaveables);

        foreach (var FoundSaveable in FoundSaveables)
        {
            if (FoundSaveable.Item1 != VariableType.WrapperStart)
                continue;

            int Index = FoundSaveable.Item3 - SaveableData.GetBaseHeaderOffset();
            ISaveableService.ReadWrapperTypeHeader(Data, Index, out int _, out SaveGameType Type, out int InnerLength);
            if (!Saveables.ContainsKey(Type))
                continue;

            // skip savegametype and inner length
            int Start = FoundSaveable.Item3 + sizeof(byte) + sizeof(int);
            ISaveableService Service = Saveables[Type].GetComponent<ISaveableService>();
            Service.LoadFrom(Data, Start, Start + InnerLength);
        }
    }

}

