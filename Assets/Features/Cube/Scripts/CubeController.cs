using UnityEngine;
using System.Collections.Generic;

public class CubeController : MonoBehaviour
{
    public static readonly List<CubeController> AllCubes = new List<CubeController>();

    [Header("Color Settings")]
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    public string colorCode { get; private set; }

    public TruckController originTruck; 
    public float popTime = 0.2f;

    void Awake()
    {
        AllCubes.Add(this);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    void OnDestroy()
    {
        AllCubes.Remove(this);
    }

    // This method initializes the color of the cube based on the provided color code and color value.
    public void InitializeColor(string code, Color colorValue)
    {
        if (_renderer == null) return;

        colorCode = code;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(ColorProperty, colorValue);
        _renderer.SetPropertyBlock(_propBlock);
    }
}