using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using static SaveGameManager;

public class SaveableList : SaveableData
{
    public override List<byte> Write(object List, string Name)
    {
        return _Write(List, Name);
    }

    public static new List<byte> _Write(object List, string Name)
    {
        List<byte> InnerData = new();

        Type ListType = List.GetType();
        int ListCount = (int)ListType.GetProperty("Count").GetValue(List);
        MethodInfo GetItemMethod = ListType.GetMethod("get_Item");

        for (int i = 0; i < ListCount; i++)
        {
            object Item = GetItemMethod.Invoke(List, new object[] { i });
            List<byte> ItemData;
            if (TryGetKnownType(Item.GetType(), out var _))
            {
                ItemData = WriteKnownType(Item, "" + i);
            }
            else
            {
                ItemData = Save(Item, "" + i);
            }
            InnerData.AddRange(ItemData);
        }

        List<byte> Data = WriteTypeHeader(List, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(List, Name, VariableType.ListEnd));
        return Data;
    }

    private static List<byte> WriteTypeHeader(object Obj, string Name, int InnerLength)
    {
        if (Obj is not IList List)
            return new();

        /*
         * Var:    Type | Hash | GenericNameLength | GenericName | InnerLen    
         * #Byte:    1  | 4    | 4                 | 0..x        | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.ListStart);
        string AssemblyName = List.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
        Header.AddRange(ToBytes(AssemblyName.Length));
        Header.AddRange(ToBytes(AssemblyName));
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedList)
    {
        IList List = Activator.CreateInstance(GetTypeFromVar(Data, LoadedList)) as IList;

        // skip length info
        int StartIndex = LoadedList.Item3 + sizeof(int);
        int EndIndex = StartIndex + GetListHeaderOffset(Data, LoadedList.Item3);
        IterateData(Data, StartIndex, EndIndex, out var FoundListVars);

        for (int i = 0; i < FoundListVars.Count; i++)
        {
            object FoundListElement = ReadVar(Data, FoundListVars[i]);
            if (FoundListElement == null)
                continue;

            List.Add(FoundListElement);
        }
        return List;
    }

    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields();
        foreach (var Field in Fields)
        {
            if (!IsGenericList(Field.FieldType))
                continue;

            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }

    public static new Type GetTypeFromVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        ReadListTypeHeader(Data, FoundVar.Item3 - GetBaseHeaderOffset(), out var _, out Type GenericType, out var _);

        Type ListType = typeof(List<>);
        ListType = ListType.MakeGenericType(GenericType);
        return ListType;
    }

    public static int ReadListTypeHeader(byte[] Data, int Index, out int Hash, out Type GenericType, out int InnerLength)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.ListStart)
        {
            throw new Exception("Expected a list start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out Hash);
        Index = ReadString(Data, Index, out string AssemblyName);
        GenericType = Type.GetType(AssemblyName);
        Index = ReadInt(Data, Index, out InnerLength);
        return Index;
    }
}
