using System;
using System.Collections;
using System.Collections.Generic;
using static SaveGameManager;
using System.Reflection;

public class SaveableBaseType : SaveableData
{
    public override List<byte> Write(object Obj, string Name)
    {
        return _Write(Obj, Name);
    }


    public static new List<byte> _Write(object Obj, string Name)
    {
        return WriteKnownType(Obj, Name);
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        return ReadType(Data, FoundVar);
    }

    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields();
        foreach (var Field in Fields)
        {
            Type FieldType = Field.FieldType;
            if (!TryGetKnownType(Field.FieldType, out var FoundType))
                continue;

            if (FoundType != VarParams.Item1)
                continue;

            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }

}
