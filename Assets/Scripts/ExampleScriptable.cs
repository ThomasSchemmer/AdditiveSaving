using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Custom/ExampleSO", order = 1)]
public class ExampleScriptable : ScriptableObject
{
    [SaveableList]
    public List<string> TestList = new();
}
