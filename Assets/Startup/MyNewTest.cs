using System;
using UnityEngine;

public struct TestStruct
{
    public object Item;
    public float R3;
}

public class MyNewTest : MonoBehaviour
{
    private TestStruct _testStruct;

    private void Awake()
    {
        _testStruct = new TestStruct();
        _testStruct.R3 = 1;
        _testStruct.Item = new TestStruct();
    }
}
