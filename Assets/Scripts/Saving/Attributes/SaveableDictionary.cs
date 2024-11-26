using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveGameManager;

public class SaveableDictionary : SaveableData
{
    public override List<byte> Write(object DicValue, string Name)
    {
        return _Write(DicValue, Name);
    }


    public static new List<byte> _Write(object DicValue, string Name)
    {
        List<byte> InnerData = new();
        Type Type = DicValue.GetType();

        int i = 0;
        IDictionary Dictionary = DicValue as IDictionary;
        foreach (var Key in Dictionary.Keys)
        {
            List<byte> KeyData;
            if (TryGetKnownType(Key.GetType(), out var FoundVarType))
            {
                KeyData = WriteKnownType(Key, "" + i);
            }
            else
            {
                KeyData = Save(Key, "" + i);
            }

            object Value = Dictionary[Key];
            List<byte> ValueData;
            if (TryGetKnownType(Value.GetType(), out var _))
            {
                ValueData = WriteKnownType(Value, "" + i);
            }
            else
            {
                ValueData = Save(Value, "" + i);
            }

            InnerData.AddRange(KeyData);
            InnerData.AddRange(ValueData);
            i++;
        }

        List<byte> Data = WriteDicTypeHeader(DicValue, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(DicValue, Name, VariableType.DictionaryEnd));
        return Data;
    }

    private static List<byte> WriteDicTypeHeader(object Obj, string Name, int InnerLength)
    {
        /*
         * Var:    Type | Hash  | InnerLen    
         * #Byte:    1  | 4     | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.DictionaryStart);
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedEnum)
    {
        // skip header info
        int Start = LoadedEnum.Item3 + sizeof(int);
        int End = LoadedEnum.Item3 + GetDictionaryHeaderOffset(Data, LoadedEnum.Item3);

        IterateData(Data, Start, End, out var FoundVars);
        List<Tuple<object, object>> GenericList = new();

        for (int i = 0; i < FoundVars.Count; i += 2)
        {
            if (IsEndType(FoundVars[i].Item1) || IsEndType(FoundVars[i + 1].Item1))
                continue;

            object FoundKey = ReadVar(Data, FoundVars[i]);
            object FoundValue = ReadVar(Data, FoundVars[2]);
            GenericList.Add(new(FoundKey, FoundValue));
        }

        return GenericList;
    }

    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields();
        foreach (var Field in Fields)
        {
            if (!IsGenericDictionary(Field.FieldType))
                continue;

            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }

    public static new object _ConvertGeneric(FieldInfo Field, object Variable)
    {
        List<Tuple<object, object>> GenericList = Variable as List<Tuple<object, object>>;
        // since we only have a list of tuples we need to infer the types from the field
        // and then convert to it

        var Types = Field.FieldType.GetGenericArguments();
        Type DictType = typeof(SerializedDictionary<,>);
        DictType = DictType.MakeGenericType(Types);
        IDictionary Dict = Activator.CreateInstance(DictType) as IDictionary;

        foreach (var Tuple in GenericList)
        {
            Dict.Add(Tuple.Item1, Tuple.Item2);
        }
        //var property = Type.GetProperty("Item");

        //var value = property.GetValue(DicValue, new[] { key });
        return Dict;
    }
}
