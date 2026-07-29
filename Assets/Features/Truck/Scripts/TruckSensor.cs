using UnityEngine;

public class TruckSensor : MonoBehaviour
{
    private TruckController parentTruck;

    private void Awake()
    {
        parentTruck = GetComponentInParent<TruckController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        GameLogger.Log(GameLogger.LogCategory.Physics, $"Sensor triggered by: {other.name}");

        if (parentTruck != null)
        {
            parentTruck.ProcessIncomingCube(other);
        }
    }
}