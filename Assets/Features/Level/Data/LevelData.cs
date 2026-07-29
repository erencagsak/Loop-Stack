using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TruckData
{
    public string TruckName;
    public Vector3 Rotation;
    public int Row;
    public int Col;
    public List<string> CubeColors = new List<string>();
}

[Serializable]
public class SplineData
{
    public int SplineIndex;
    public int Row;
    public int Col;
}

public class LevelData : ScriptableObject
{
    [Header("Level Information")]
    public int LevelNumber;

    [Header("Level Content")]
    public List<SplineData> Splines = new List<SplineData>();
    public List<TruckData> Trucks = new List<TruckData>();
}