using System.Collections.Generic;
using UnityEngine;

public class LevelRuleManager : MonoBehaviour
{
    public static LevelRuleManager Instance { get; private set; }

    [Header("Rule Settings")]
    [Tooltip("A Truck should be filled with this many cubes to count as a unit.")]
    [SerializeField] private int cubesPerTarget = 192;

    private bool _isLevelFinished = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        GameManager.OnLevelLoaded += ResetRules;
    }

    private void OnDisable()
    {
        GameManager.OnLevelLoaded -= ResetRules;
    }

    private void ResetRules(int obj)
    {
        _isLevelFinished = false;
    }

    public void UpdateCubeProgress()
    {
        if (_isLevelFinished) return;

        int totalCubes = CubeController.AllCubes.Count;
        int sortedCubes = 0;

        foreach (CubeController cube in CubeController.AllCubes)
        {
            if (cube.TryGetComponent<Rigidbody>(out Rigidbody rb) && rb.isKinematic) 
            {
                sortedCubes++;
            }
        }

        int currentUnits = sortedCubes / cubesPerTarget;
        int totalUnits = totalCubes / cubesPerTarget;

        if (sortedCubes == totalCubes && totalCubes > 0)
        {
            currentUnits = 0;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCubeProgressText(currentUnits, totalUnits);
        }
    }
    
    public void CheckWinCondition()
    {
        if (_isLevelFinished) return;

        foreach (CubeController cube in CubeController.AllCubes)
        {
            if (cube.TryGetComponent(out Rigidbody rb) && !rb.isKinematic) return;
        }

        Dictionary<string, TruckController> colorTruckMap = new Dictionary<string, TruckController>();

        foreach (TruckController truck in TruckController.ActiveTrucks)
        {
            foreach (CubeController cube in truck.GetCubesInTruck())
            {
                string color = cube.colorCode;

                if (string.IsNullOrEmpty(color)) continue;

                if (!colorTruckMap.ContainsKey(color))
                {
                    colorTruckMap[color] = truck;
                }
                else if (colorTruckMap[color] != truck)
                {
                    return; 
                }
            }
        }

        _isLevelFinished = true;
        
        GameManager.Instance.TriggerWinCondition();
    }
}