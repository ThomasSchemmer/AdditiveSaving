using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
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
        int Index = ReadHeader(Data, Start, out int _, out Type ClassType, out int InnerLength);

        object Instance = CreateInstance(ClassType);
        ScriptableObject ScriptInstance = Instance as ScriptableObject;
        string path = AssetDatabase.GetAssetPath(ScriptInstance);
        LoadTo(Instance, Data, Start, Index + InnerLength);
        return Instance;
    }

    private static object CreateInstance(Type ClassType)
    {
        if (ClassType.IsSubclassOf(typeof(MonoBehaviour))){
            GameObject Obj = new();
            return Obj.AddComponent(ClassType);
        }
        if (ClassType.IsSubclassOf(typeof(ScriptableObject)))
        {
            return ScriptableObject.CreateInstance(ClassType);
        }
        // must be a regular class then

        return Activator.CreateInstance(ClassType);
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

    public static int GetHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        ReadInt(Data, Index, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    public static List<byte> WriteHeader(object Obj, string Name, int InnerLength)
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

    public static int ReadHeader(byte[] Data, int Index, out int Hash, out Type ClassType, out int InnerLength)
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
}
