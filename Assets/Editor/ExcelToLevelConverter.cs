using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ExcelDataReader;
using System.Data;
using System.Linq;

public class ExcelToLevelConverter : EditorWindow
{
    private const string SavePath = "Assets/Features/Level/Resources/Levels";

    [MenuItem("Tools/Convert Excel to Level Data")]
    public static void ConvertExcel()
    {
        string excelPath = Path.Combine(Application.streamingAssetsPath, "Levels.xlsx");

        if (!File.Exists(excelPath))
        {
            GameLogger.LogError(GameLogger.LogCategory.System, $"Excel file not found: {excelPath}");

            return;
        }

        if (!AssetDatabase.IsValidFolder(SavePath))
            Directory.CreateDirectory(SavePath);

        using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });

        List<LevelData> parsedLevels = ParseExcelToSO(dataSet);

        foreach (var level in parsedLevels)
        {
            string assetPath = $"{SavePath}/Level_{level.LevelNumber}.asset";
            LevelData existingAsset = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);

            if (existingAsset != null)
            {
                EditorUtility.CopySerialized(level, existingAsset);
                EditorUtility.SetDirty(existingAsset);
            }
            else
            {
                AssetDatabase.CreateAsset(level, assetPath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameLogger.Log(GameLogger.LogCategory.System, $"OPERATION COMPLETED! {parsedLevels.Count} levels created as SO.");
    }

    private static List<LevelData> ParseExcelToSO(DataSet dataSet)
    {
        var resultList = new List<LevelData>();
        var splineTable = dataSet.Tables["Splines"];
        var carrierTable = dataSet.Tables["Carriers"];

        var splineHeaderRow = splineTable.Rows[1];
        var colToSplineIndex = new Dictionary<int, int>();
        for (int c = 1; c < splineTable.Columns.Count; c++)
        {
            if (int.TryParse(splineHeaderRow[c]?.ToString().Trim(), out int idx)) colToSplineIndex[c] = idx;
        }

        for (int r = 2; r < splineTable.Rows.Count; r++)
        {
            string col0 = splineTable.Rows[r][0]?.ToString().Trim();
            if (!string.IsNullOrEmpty(col0) && float.TryParse(col0.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float lvlNum))
            {
                LevelData newLevel = CreateInstance<LevelData>();

                newLevel.name = $"Level_{Mathf.RoundToInt(lvlNum)}";

                newLevel.LevelNumber = Mathf.RoundToInt(lvlNum);

                resultList.Add(newLevel);
            }

            var current = resultList.LastOrDefault();
            if (current == null) continue;

            for (int c = 1; c < splineTable.Columns.Count; c++)
            {
                string cell = splineTable.Rows[r][c]?.ToString().Trim();

                if (string.IsNullOrEmpty(cell) || cell == "-") continue;

                if (colToSplineIndex.TryGetValue(c, out int splineIdx) && int.TryParse(cell, out int splineNum))
                    current.Splines.Add(new SplineData { SplineIndex = splineNum, Row = r, Col = c });
                else
                {
                    string normalized = cell.Replace("\n", " ").Replace("\r", "");
                    string[] parts = normalized.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    string name = parts[0].ToUpper();
                    
                    if (name.Length == 1 && char.IsLetter(name[0]))
                    {
                        Vector3 finalRotation = Vector3.zero;

                        if (parts.Length > 1)
                        {
                            if (parts.Length >= 4 && 
                                float.TryParse(parts[1], out float x) && 
                                float.TryParse(parts[2], out float y) && 
                                float.TryParse(parts[3], out float z))
                            {
                                finalRotation = new Vector3(x, y, z);
                            }
                            else if (float.TryParse(parts[1], out float yRot))
                            {
                                finalRotation = new Vector3(0, yRot, 0);
                            }
                        }

                        current.Trucks.Add(new TruckData 
                        { 
                            TruckName = name, 
                            Rotation = finalRotation, 
                            Row = r, 
                            Col = c 
                        });
                    }
                }
            }
        }

        var headerRow = carrierTable.Rows[1];
        LevelData targetLevel = null;

        for (int r = 2; r < carrierTable.Rows.Count; r++)
        {
            string col0 = carrierTable.Rows[r][0]?.ToString().Trim();

            if (!string.IsNullOrEmpty(col0) && float.TryParse(col0.Replace(",", "."), out float lvl))
                targetLevel = resultList.Find(l => l.LevelNumber == Mathf.RoundToInt(lvl));

            if (targetLevel == null) continue;

            for (int c = 1; c < carrierTable.Columns.Count; c++)
            {
                string truckName = headerRow[c]?.ToString().Trim();
                string colorCode = carrierTable.Rows[r][c]?.ToString().Trim();

                if (string.IsNullOrEmpty(colorCode) || colorCode == "-") continue;

                var truck = targetLevel.Trucks.Find(t => t.TruckName == truckName);

                if (truck != null) { truck.CubeColors ??= new List<string>(); truck.CubeColors.Insert(0, colorCode); }
            }
        }

        return resultList;
    }
}