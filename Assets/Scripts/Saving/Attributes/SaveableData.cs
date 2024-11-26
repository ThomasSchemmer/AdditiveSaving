using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static SaveGameManager;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class SaveableData : Attribute {

    protected static Dictionary<Type, VariableType> TypeMap = new()
    {
        { typeof(byte), VariableType.Byte },
        { typeof(int), VariableType.Int },
        { typeof(uint), VariableType.Uint },
        { typeof(float), VariableType.Double },
        { typeof(double), VariableType.Double },
        { typeof(string), VariableType.String },
        { typeof(Vector3), VariableType.Vector3 },
    }; 
    
    protected static Dictionary<VariableType, Type> ClassMap = new()
    {
        {VariableType.Byte, typeof(SaveableBaseType)},
        {VariableType.Int, typeof(SaveableBaseType)},
        {VariableType.Uint, typeof(SaveableBaseType)},
        {VariableType.Double, typeof(SaveableBaseType)},
        {VariableType.String, typeof(SaveableBaseType)},
        {VariableType.Vector3, typeof(SaveableBaseType)},
        {VariableType.ClassStart, typeof(SaveableClass)},
        {VariableType.ClassEnd, null},
        {VariableType.ListStart, typeof(SaveableList)},
        {VariableType.ListEnd, null},
        {VariableType.ArrayStart, typeof(SaveableArray)},
        {VariableType.ArrayEnd, null},
        // wrapper are handled indirectly in SaveableService
        {VariableType.WrapperStart, null},
        {VariableType.WrapperEnd, null},
        {VariableType.EnumStart, typeof(SaveableEnum)},
        {VariableType.EnumEnd, null},
        {VariableType.DictionaryStart, typeof(SaveableDictionary)},
        {VariableType.DictionaryEnd, null},
    };

    // runtime only!
    protected static Dictionary<VariableType, MethodInfo> _ReadVarMap = new();

    //**************************** Saving **************************************************************

    public abstract List<byte> Write(object Obj, string Name);

    public static List<byte> _Write(object Obj, string Name)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }

    public static List<byte> Save(object Obj, string Name)
    {
        FieldInfo[] Infos = Obj.GetType().GetFields();

        List<byte> InnerData = new();

        bool bHasSaved = false;
        foreach (FieldInfo Info in Infos)
        {
            if (!TryGetSaveable(Info, null, out var FoundSaveable))
                continue;

            // use non-static call to get the correct method overwrite
            InnerData.AddRange(FoundSaveable.Write(Info.GetValue(Obj), Info.Name));
            bHasSaved = true;
        }

        if (!bHasSaved)
        {
            throw new Exception("Nothing saved for " + Obj.ToString() + " - are you missing a type mapping?");
        }

        List<byte> Data = WriteClassTypeHeader(Obj, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(Obj, Name, VariableType.ClassEnd));

        return Data;
    }


    public static List<byte> WriteKnownType(object Value, string Name)
    {
        Type Type = Value.GetType();
        if (!TryGetKnownType(Type, out var FoundVarType))
            return new();

        switch (FoundVarType)
        {
            case VariableType.Byte: return WriteByte(Value, Name);
            case VariableType.Int: return WriteInt(Value, Name);
            case VariableType.Uint: return WriteUInt(Value, Name);
            case VariableType.Double: return WriteDouble(Value, Name);
            case VariableType.String: return WriteString(Value, Name);
            case VariableType.Vector3: return WriteVector(Value, Name);
            //c# has problems detecting subclasses in generic types
            case VariableType.EnumStart: return SaveableEnum._Write(Value, Name);
            case VariableType.ListStart: return SaveableList._Write(Value, Name);
            case VariableType.ArrayStart: return SaveableArray._Write(Value, Name);
            case VariableType.DictionaryStart: return SaveableDictionary._Write(Value, Name);
            default:
                throw new Exception("Missing type registry for known type");
        }
    }

    public static Type GetTypeFromVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        if (TypeMap.Values.Contains(FoundVar.Item1))
        {
            return TypeMap.Where(Pair => Pair.Value == FoundVar.Item1).FirstOrDefault().Key;
        }
        switch (FoundVar.Item1) 
        {
            case VariableType.ClassStart: return SaveableClass.GetTypeFromVar(Data, FoundVar);
            case VariableType.EnumStart: return SaveableEnum.GetTypeFromVar(Data, FoundVar);
            case VariableType.ListStart: return SaveableList.GetTypeFromVar(Data, FoundVar);
            case VariableType.ArrayStart: return SaveableArray.GetTypeFromVar(Data, FoundVar);
            //case VariableType.DictionaryStart: return SaveableDictionary._Write(Value, Name);
            default:
                throw new Exception("Missing type registry for known type");
        }
    }

    //**************************** Loading **************************************************************

    protected static object ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        VariableType VarType = FoundVar.Item1;
        if (!_ReadVarMap.ContainsKey(VarType))
        {
            Type FoundClassType = ClassMap[VarType];
            if (FoundClassType == null)
                return null;

            // use reflection to get the static overload
            MethodInfo Method = FoundClassType.GetMethod("_ReadVar");
            if (Method == null)
                throw new NotImplementedException("Every SaveableData class needs to overwrite this!");

            _ReadVarMap.Add(VarType, Method);
        }

        MethodInfo ReadVarMethod = _ReadVarMap[VarType];
        object Variable = ReadVarMethod.Invoke(null, new object[] { Data, FoundVar });
        return Variable;
    }

    /** Reads the variable provided with the meta data according to its subclasses definition */
    public static object _ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }

    public static FieldInfo GetMatch(object Target, Tuple<VariableType, int, int> FoundVar)
    {
        Type FoundClassType = ClassMap[FoundVar.Item1];
        if (FoundClassType == null)
            return null;

        // use reflection to get the static overload
        // (has to be static since we dont have the actual field and its restrictions yet)
        MethodInfo GetMatchMethod = FoundClassType.GetMethod("_GetMatch");
        if (GetMatchMethod == null)
            throw new NotImplementedException("Every SaveableData class needs to overwrite this!");

        FieldInfo FoundField = GetMatchMethod.Invoke(Target, new object[] { Target, FoundVar }) as FieldInfo;
        return FoundField;
    }

    /** Returns the field matching the variable meta data */
    public static FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }

    /** Tries to load the passed in data into the target's best fitting variable */
    public static void LoadTo(object Target, byte[] Data, int MinIndex, int MaxIndex)
    {
        int Index = ReadClassTypeHeader(Data, MinIndex, out int BeginHash, out Type ClassType, out int _);
        if (Target.GetType() != ClassType)
            return;

        IterateData(Data, Index, MaxIndex, out var FoundVars);

        foreach (var FoundVar in FoundVars)
        {
            Type FoundClassType = ClassMap[FoundVar.Item1];
            if (FoundClassType == null)
                continue;

            FieldInfo FoundField = GetMatch(Target, FoundVar);
            // eg the new version doesn't have the var anymore
            if (FoundField == null)
                continue;

            object LoadedObject = ReadVar(Data, FoundVar);
            FoundField.SetValue(Target, LoadedObject);
        }
    }

    public static void IterateData(byte[] Data, int Index, int MaxIndex, out List<Tuple<VariableType, int, int>> FoundVars)
    {
        FoundVars = new();
        while (Index < MaxIndex)
        {
            // shallow search only, list/classes will be filled later!
            Index = ReadSaveTypeHeader(Data, Index, out var VarTuple);
            FoundVars.Add(VarTuple);
        }
    }

    private static int ReadSaveTypeHeader(byte[] Data, int Index, out Tuple<VariableType, int, int> FoundVar)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType Type = (VariableType)bVarType;
        Index = ReadInt(Data, Index, out int Hash);
        FoundVar = new(Type, Hash, Index);

        int VarOffset;
        switch (Type)
        {
            case VariableType.Byte: VarOffset = GetByteVarOffset(); break;
            case VariableType.Int: VarOffset = GetIntVarOffset(); break;
            case VariableType.Uint: VarOffset = GetUIntVarOffset(); break;
            case VariableType.Double: VarOffset = GetDoubleVarOffset(); break;
            case VariableType.String: VarOffset = GetStringVarOffset(Data, Index); break;
            case VariableType.Vector3: VarOffset = GetVectorVarOffset(); break;
            case VariableType.ClassStart: VarOffset = GetClassHeaderOffset(Data, Index); break;
            case VariableType.ListStart: VarOffset = GetListHeaderOffset(Data, Index); break;
            case VariableType.ArrayStart: VarOffset = GetArrayHeaderOffset(Data, Index); break;
            case VariableType.WrapperStart: VarOffset = GetWrapperHeaderOffset(Data, Index); break;
            case VariableType.EnumStart: VarOffset = GetEnumHeaderOffset(Data, Index); break;
            case VariableType.DictionaryStart: VarOffset = GetDictionaryHeaderOffset(Data, Index); break;
            case VariableType.ListEnd: VarOffset = 0; break;
            case VariableType.ClassEnd: VarOffset = 0; break;
            case VariableType.ArrayEnd: VarOffset = 0; break;
            case VariableType.WrapperEnd: VarOffset = 0; break;
            case VariableType.EnumEnd: VarOffset = 0; break;
            case VariableType.DictionaryEnd: VarOffset = 0; break;
            default:
                throw new Exception("Should not reach here - are you missing a value type?");
        }
        return Index + VarOffset;
    }
    //**************************** Utility **************************************************************

    protected static bool TryGetKnownType(Type Type, out VariableType VariableType)
    {
        if (TypeMap.ContainsKey(Type))
        {
            VariableType = TypeMap[Type];
            return true;
        }
        // c# is weird for saving enums, converts it into a basetype?
        if (Type.IsEnum)
        {
            VariableType = VariableType.EnumStart;
            return true;
        }
        if (Type.IsArray)
        {
            VariableType = VariableType.ArrayStart;
            return true;
        }
        if (IsGenericList(Type))
        {
            VariableType = VariableType.ListStart;
            return true;
        }
        if (IsGenericDictionary(Type))
        {
            VariableType = VariableType.DictionaryStart;
            return true;
        }
        VariableType = default;
        return false;
    }



    protected static object ReadType(byte[] Data, Tuple<VariableType, int, int> Variable)
    {
        return ReadValue(Data, Variable.Item3, Variable.Item1);
    }

    protected static object ReadValue(byte[] Data, int Index, Type Type)
    {
        if (TryGetKnownType(Type, out var FoundType))
            return null;

        return ReadValue(Data, Index, FoundType);
    }

    protected static object ReadValue(byte[] Data, int Index, VariableType Type)
    {
        switch (Type)
        {
            case VariableType.Byte: ReadByte(Data, Index, out byte bValue); return bValue;
            case VariableType.Int: ReadInt(Data, Index, out int iValue); return iValue;
            case VariableType.Uint: ReadUInt(Data, Index, out uint uValue); return uValue;
            case VariableType.Double: ReadDouble(Data, Index, out double dValue); return dValue;
            case VariableType.String: ReadString(Data, Index, out string sValue); return sValue;
            case VariableType.Vector3: ReadVector(Data, Index, out Vector3 vValue); return vValue;
        }
        return null;
    }

    public static int ReadByte(byte[] Data, int Index, out byte Value)
    {
        Value = Data[Index];
        return Index + sizeof(byte);
    }

    public static int ReadInt(byte[] Data, int Index, out int Value)
    {
        Value = BitConverter.ToInt32(Data, Index);
        return Index + sizeof(int);
    }

    public static int ReadUInt(byte[] Data, int Index, out uint Value)
    {
        Value = BitConverter.ToUInt32(Data, Index);
        return Index + sizeof(uint);
    }

    public static int ReadDouble(byte[] Data, int Index, out double Value)
    {
        Value = BitConverter.ToDouble(Data, Index);
        return Index + sizeof(double);
    }

    public static int ReadString(byte[] Data, int Index, out string Value)
    {
        Index = ReadInt(Data, Index, out int Length);
        Value = Encoding.UTF8.GetString(Data, Index, Length);
        return Index + Length;
    }

    public static int ReadVector(byte[] Data, int Index, out Vector3 Value)
    {
        Index = ReadDouble(Data, Index, out double x);
        Index = ReadDouble(Data, Index, out double y);
        Index = ReadDouble(Data, Index, out double z);
        Value = new((float)x, (float)y, (float)z);
        return Index + sizeof(double) * 3;
    }

    protected static int GetIntVarOffset()
    {
        return sizeof(int);
    }

    protected static int GetUIntVarOffset()
    {
        return sizeof(uint);
    }

    protected static int GetByteVarOffset()
    {
        return sizeof(byte);
    }

    protected static int GetDoubleVarOffset()
    {
        return sizeof(double);
    }

    protected static int GetStringVarOffset(byte[] Data, int Index)
    {
        ReadInt(Data, Index, out int Length);
        return sizeof(int) + Length;
    }

    protected static int GetVectorVarOffset()
    {
        return sizeof(double) * 3;
    }

    public static int ReadClassTypeHeader(byte[] Data, int Index, out int Hash, out Type ClassType, out int InnerLength)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.ClassStart)
        {
            throw new Exception("Expected a class start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out Hash);
        Index = ReadString(Data, Index, out string AssemblyName);
        ClassType = Type.GetType(AssemblyName);
        Index = ReadInt(Data, Index, out InnerLength);
        return Index;
    }

    public static int GetBaseHeaderOffset()
    {
        return sizeof(int) + sizeof(byte);
    }

    protected static int GetClassHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        ReadInt(Data, Index, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    protected static int GetListHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        ReadInt(Data, Index, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    protected static int GetArrayHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        Index = ReadByte(Data, Index, out var DimSize);
        Index += DimSize * sizeof(byte);
        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int) + DimSize * sizeof(byte) + sizeof(byte) + AssemblyNameOffset;
    }

    protected static int GetWrapperHeaderOffset(byte[] Data, int Index)
    {
        Index = ReadByte(Data, Index, out var _);
        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int) + sizeof(byte);
    }

    protected static int GetEnumHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        ReadInt(Data, Index + AssemblyNameOffset, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    protected static int GetDictionaryHeaderOffset(byte[] Data, int Index)
    {
        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int);
        return 0;
    }

    protected static bool IsGenericList(Type Type)
    {
        return (Type.IsGenericType && (Type.GetGenericTypeDefinition() == typeof(List<>)));
    }

    protected static bool IsGenericDictionary(Type Type)
    {
        return Type.IsGenericType && 
            (Type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
            Type.GetGenericTypeDefinition() == typeof(SerializedDictionary<,>));
    }

    protected static bool IsArray(Type Type)
    {
        return Type.IsArray;
    }

    protected static bool IsEndType(VariableType Type)
    {
        return Type == VariableType.WrapperEnd || Type == VariableType.ClassEnd || Type == VariableType.ListEnd ||
            Type == VariableType.ArrayEnd;
    }

    public static List<byte> ToBytes(int Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(uint Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(double Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(string Value)
    {
        return Encoding.UTF8.GetBytes(Value).ToList();
    }

    protected static List<byte> WriteInt(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Int);

        int iValue = (int)Value;
        Bytes.AddRange(ToBytes(iValue));
        return Bytes;
    }

    protected static List<byte> WriteUInt(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Uint);

        uint iValue = (uint)Value;
        Bytes.AddRange(ToBytes(iValue));
        return Bytes;
    }

    protected static List<byte> WriteByte(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Byte);

        Bytes.Add((byte)Value);
        return Bytes;
    }

    protected static List<byte> WriteString(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.String);

        string Text = (string)Value;
        Bytes.AddRange(ToBytes(Text.Length));
        Bytes.AddRange(ToBytes(Text));
        return Bytes;
    }

    protected static List<byte> WriteVector(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Vector3);

        Vector3 vValue = (Vector3)Value;
        Bytes.AddRange(ToBytes(vValue.x));
        Bytes.AddRange(ToBytes(vValue.y));
        Bytes.AddRange(ToBytes(vValue.z));
        return Bytes;
    }

    protected static List<byte> WriteDouble(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Double);

        Bytes.AddRange(ToBytes((double)Value));
        return Bytes;
    }

    protected static List<byte> WriteTypeHeader(object Obj, string Name, VariableType Type)
    {
        int Hash = Name.GetHashCode();
        List<byte> Bytes = new();
        Bytes.Add((byte)Type);
        Bytes.AddRange(ToBytes(Hash));
        return Bytes;
    }

    protected static List<byte> WriteClassTypeHeader(object Obj, string Name, int InnerLength)
    {
        /*
         * Var:    Type | Hash  | ClassNameLen  | ClassName | InnerLen    
         * #Byte:    1  | 4     | 4             | 0..X      | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.ClassStart);
        string AssemblyName = Obj.GetType().AssemblyQualifiedName;
        Header.AddRange(ToBytes(AssemblyName.Length));
        Header.AddRange(ToBytes(AssemblyName));
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    protected static bool TryGetSaveable(FieldInfo Info, Type TargetType, out SaveableData FoundSaveable)
    {
        object[] attrs = Info.GetCustomAttributes(true);
        foreach (object attr in attrs)
        {
            SaveableData SaveableAttr = attr as SaveableData;
            if (SaveableAttr == null)
                continue;

            if (TargetType != null && TargetType != SaveableAttr.GetType())
                continue;

            FoundSaveable = SaveableAttr;
            return true;
        }

        FoundSaveable = default;
        return false;
    }

    protected static bool TryGetSaveable(FieldInfo[] Infos, Type SaveableType, out SaveableData FoundSaveable)
    {
        foreach (var Info in Infos)
        {
            if (!TryGetSaveable(Info, SaveableType, out FoundSaveable))
                continue;

            return true;
        }

        FoundSaveable = default;
        return false;
    }

}
