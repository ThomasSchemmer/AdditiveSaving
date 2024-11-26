using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveGameManager;

public abstract class ISaveableService : MonoBehaviour
{
    public abstract void Reset();

    public List<byte> Save(SaveGameType SGType, string Name)
    {
        List<byte> InnerData = SaveableData.Save(this, Name);
        List<byte> Data = WriteTypeHeader(this, Name, SGType, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(this, Name, VariableType.WrapperEnd));

        return Data;
    }
    public void LoadFrom(byte[] Data, int MinIndex, int MaxIndex)
    {
        SaveableData.LoadTo(this, Data, MinIndex, MaxIndex);
    }

    private List<byte> WriteTypeHeader(object Obj, string Name, SaveGameType Type, int InnerLength)
    {
        /*
         * Var:    Type | Hash | SaveGameType | InnerLen    
         * #Byte:    1  | 4    | 1            | 4  
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.WrapperStart);
        Header.Add((byte)Type);
        Header.AddRange(SaveableData.ToBytes(InnerLength));
        return Header;
    }

    protected List<byte> WriteTypeHeader(object Obj, string Name, VariableType Type)
    {
        int Hash = Name.GetHashCode();
        List<byte> Bytes = new();
        Bytes.Add((byte)Type);
        Bytes.AddRange(SaveableData.ToBytes(Hash));
        return Bytes;
    }

    public static int ReadWrapperTypeHeader(byte[] Data, int Index, out int Hash, out SaveGameType Type, out int InnerLength)
    {
        Index = SaveableData.ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.WrapperStart)
        {
            throw new Exception("Expected a wrapper start, but found " + VarType + " instead!");
        }
        Index = SaveableData.ReadInt(Data, Index, out Hash);
        Index = SaveableData.ReadByte(Data, Index, out byte bType);
        Index = SaveableData.ReadInt(Data, Index, out InnerLength);
        Type = (SaveGameType)bType;
        return Index;
    }

}
