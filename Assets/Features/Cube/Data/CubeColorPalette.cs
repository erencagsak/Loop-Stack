using System.Collections.Generic;
using UnityEngine;

public enum CubeColor
{
    Unknown = 0,
    Yellow = 1,
    Red = 2,
    Blue = 3,
    Green = 4,
    Purple = 5,
    Orange = 6,
    Pink = 7,
    White = 8,
}

[System.Serializable]
public class CubeColorEntry
{
    [Tooltip("Single-letter code from Excel (Y, R, B, G...)")]
    public string code;
    public CubeColor colorType;
    public Color colorValue;
}

[CreateAssetMenu(fileName = "CubeColorPalette", menuName = "Game/Cube Color Palette")]
public class CubeColorPalette : ScriptableObject
{
    [SerializeField] private List<CubeColorEntry> entries;

    private Dictionary<string, CubeColorEntry> _cache;

    private void OnEnable() => BuildCache();

    private void BuildCache()
    {
        _cache = new Dictionary<string, CubeColorEntry>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.code))
                _cache[entry.code.Trim()] = entry;
        }
    }

    public bool TryGetEntry(string code, out CubeColorEntry entry)
    {
        if (_cache == null) BuildCache();
        return _cache.TryGetValue(code?.Trim() ?? "", out entry);
    }

    public Color GetColor(string code)
    {
        return TryGetEntry(code, out var entry) ? entry.colorValue : Color.magenta;
    }

    public CubeColor GetColorType(string code)
    {
        return TryGetEntry(code, out var entry) ? entry.colorType : CubeColor.Unknown;
    }
}