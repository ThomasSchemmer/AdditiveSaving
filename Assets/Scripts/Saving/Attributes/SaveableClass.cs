using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveGameManager;

public class SaveableClass : SaveableData
{
    public override List<byte> Write(object Class, string Name)
    {
        return _Write(Class, Name);
    }


    public static new List<byte> _Write(object Class, string Name)
    {
        return Save(Class, Name);
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        int Start = FoundVar.Item3 - GetBaseHeaderOffset();
        int End = FoundVar.Item3 + GetClassHeaderOffset(Data, FoundVar.Item3);
        ReadString(Data, FoundVar.Item3, out string ClassName);
        Type ClassType = Type.GetType(ClassName);

        // todo: support monobehavs with GO generation
        object Instance = Activator.CreateInstance(ClassType);
        LoadTo(Instance, Data, Start, End);
        return Instance;
    }


    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields();
        foreach (var Field in Fields)
        {
            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }

    public static new Type GetTypeFromVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        ReadClassTypeHeader(Data, FoundVar.Item3 - GetBaseHeaderOffset(), out var _, out Type ClassType, out var _);
        return ClassType;
    }


}
