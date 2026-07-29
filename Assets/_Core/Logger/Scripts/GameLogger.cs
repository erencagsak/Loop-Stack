#define ENABLE_LOGS

using UnityEngine;
using System.Diagnostics;

public static class GameLogger
{
    public enum LogCategory
    {
        Automation,
        UI,
        Gameplay,
        Physics,
        System
    }

    private static LoggerSettings _settings;

    private static void LoadSettings()
    {
        if (_settings == null)
        {
            _settings = Resources.Load<LoggerSettings>("LoggerSettings");
            
            if (_settings == null)
            {
                UnityEngine.Debug.LogWarning("LoggerSettings file not found in the Resources folder! All logs will be enabled by default.");
            }
        }
    }

    private static bool ShouldLog(LogCategory category)
    {
        LoadSettings();

        if (_settings == null) return true; 

        if (!_settings.masterEnable) return false;

        return category switch
        {
            LogCategory.Automation => _settings.showAutomation,
            LogCategory.UI => _settings.showUI,
            LogCategory.Gameplay => _settings.showGameplay,
            LogCategory.Physics => _settings.showPhysics,
            LogCategory.System => _settings.showSystem,
            _ => true
        };
    }

    [Conditional("ENABLE_LOGS")]
    public static void Log(LogCategory category, string message)
    {
        if (!ShouldLog(category)) return;

        string prefix = GetPrefix(category);

        UnityEngine.Debug.Log($"{prefix} {message}");
    }

    [Conditional("ENABLE_LOGS")]
    public static void LogWarning(LogCategory category, string message)
    {
        if (!ShouldLog(category)) return;

        string prefix = GetPrefix(category);
        UnityEngine.Debug.LogWarning($"{prefix} {message}");
    }

    [Conditional("ENABLE_LOGS")]
    public static void LogError(LogCategory category, string message)
    {
        if (!ShouldLog(category)) return;

        string prefix = GetPrefix(category);
        UnityEngine.Debug.LogError($"{prefix} {message}");
    }

    private static string GetPrefix(LogCategory category)
    {
        return category switch
        {
            LogCategory.Automation => "<color=#00FF00>[Auto-Setup]</color>",
            LogCategory.UI         => "<color=#00FFFF>[UI System]</color>",
            LogCategory.Gameplay   => "<color=#FFA500>[Gameplay]</color>",
            LogCategory.Physics    => "<color=#FF00FF>[Physics]</color>",
            LogCategory.System     => "<color=#FFFFFF>[System]</color>",
            _                      => "[Log]"
        };
    }
}