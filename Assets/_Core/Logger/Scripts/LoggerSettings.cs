using UnityEngine;

[CreateAssetMenu(fileName = "LoggerSettings", menuName = "Settings/Logger Settings")]
public class LoggerSettings : ScriptableObject
{
    [Header("Master Switch")]
    [Tooltip("When disabled, ALL logs will be silenced.")]
    public bool masterEnable = true;

    [Header("Category Filters")]
    public bool showSystem = true;
    public bool showGameplay = true;
    public bool showAutomation = true;
    public bool showUI = true;
    public bool showPhysics = false;
}