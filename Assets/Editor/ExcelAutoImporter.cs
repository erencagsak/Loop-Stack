using UnityEditor;
using UnityEngine;

public class ExcelAutoImporter : AssetPostprocessor
{
    // Auto importer for Excel files. When the "Levels.xlsx" file is modified, this method will be triggered automatically.
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool isExcelChanged = false;

        foreach (string path in importedAssets)
        {
            if (path.EndsWith("Levels.xlsx"))
            {
                isExcelChanged = true;
                
                break;
            }
        }

        if (isExcelChanged)
        {
            GameLogger.Log(GameLogger.LogCategory.System, "Changes detected in Levels.xlsx! Data is being converted to ScriptableObject immediately...");
            
            ExcelToLevelConverter.ConvertExcel();
        }
    }
}