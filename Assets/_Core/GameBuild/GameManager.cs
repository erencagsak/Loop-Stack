using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action OnGameStarted;
    public static event Action<int> OnLevelLoaded;
    public static event Action OnLevelCompleted;

    [Header("Level Settings")]
    [Tooltip("The maximum Loop Level Index")]
    [SerializeField] private int maxLevelIndex = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ApplyApplicationSettings();
    }

    private void ApplyApplicationSettings()
    {
        SetTargetFrameRate();
        
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    private void SetTargetFrameRate()
    {
        QualitySettings.vSyncCount = 0;

        #if UNITY_EDITOR
                Application.targetFrameRate = 120;
        #else
                Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        #endif
    }

    public void StartGame()
    {
        OnGameStarted?.Invoke();

        LoadCurrentLevel();
    }

    public void LoadNextLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentLevel++;
        
        if (currentLevel > maxLevelIndex) currentLevel = 1;

        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.Save();

        GameLogger.Log(GameLogger.LogCategory.System, $"SAVE: Next level ({currentLevel}) saved.");
        
        LoadCurrentLevel();
    }

    private void LoadCurrentLevel()
    {
        int targetLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        if (LevelBuilder.Instance != null)
        {
            LevelBuilder.Instance.LoadLevelAsync(targetLevel).Forget();
            GameLogger.Log(GameLogger.LogCategory.System, $"[Transition] Loading Level {targetLevel}.");
            
            OnLevelLoaded?.Invoke(targetLevel);
        }
        else
        {
            GameLogger.LogError(GameLogger.LogCategory.System, "LevelBuilder not found!");
        }
    }

    public void TriggerWinCondition()
    {
        GameLogger.Log(GameLogger.LogCategory.Gameplay, "💎 CONGRATULATIONS! LEVEL COMPLETED! 💎");
        
        OnLevelCompleted?.Invoke();
    }
}