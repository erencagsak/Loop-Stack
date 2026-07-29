using UnityEngine;

public class TruckGrid : MonoBehaviour
{
    [Header("Bed Settings")]
    public Vector3 truckBedOffset = new Vector3(0, 0.8f, 0.34f);
    [Space(10)]
    public float cubeXOffset = 0.27f;
    public float cubeYOffset = 0.38f;
    public float cubeZOffset = 0.9f;
    
    [Header("Direction Settings")]
    public bool buildRightToLeft = true; 
    public bool reverseZDirection = false; 

    [Header("Single Color Block Dimensions")]
    [Tooltip("X Axis - Width (Columns)")]
    public int gridX = 4;
    [Tooltip("Y Axis - Height (Upward Layers)")]
    public int gridY = 4;
    [Tooltip("Z Axis - Depth (The row occupied by each color in the image)")]
    public int gridZ = 3;

    [Header("Gizmo Debug")]
    public bool showGizmos = true;
    [Tooltip("Approximately how many colors should be shown side by side when drawing Gizmos in the scene?")]
    public int previewColorCount = 2;
    public GameObject cubePrefab;

    public int GetCubesPerColorBlock()
    {
        return gridX * gridY * gridZ;
    }

    // Duvar duvar dizilim mantığı
    public Vector3 GetCubeTargetPosition(int stackIndex)
    {
        int cubesPerWall = gridX * gridY;
        
        int z = stackIndex / cubesPerWall;
        int indexInWall = stackIndex % cubesPerWall;

        int y = indexInWall / gridX;
        int x = indexInWall % gridX;

        float startX = (gridX - 1) * cubeXOffset / 2f;
        float localX = buildRightToLeft ? startX - (x * cubeXOffset) : -startX + (x * cubeXOffset);

        float localY = y * cubeYOffset;

        float zDir = reverseZDirection ? -1f : 1f;
        float localZ = z * cubeZOffset * zDir; 

        Vector3 localPosInRoot = truckBedOffset + new Vector3(localX, localY, localZ);

        return transform.TransformPoint(localPosInRoot);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 cubeSize = cubePrefab != null 
            ? cubePrefab.transform.localScale 
            : Vector3.one * 0.5f;

        int totalCubes = GetCubesPerColorBlock() * previewColorCount;

        for (int i = 0; i < totalCubes; i++)
        {
            Vector3 worldPos = GetCubeTargetPosition(i);

            // Her renk bloğunu farklı göster
            int colorIndex = i / GetCubesPerColorBlock();
            Gizmos.color = (colorIndex % 2 == 0) ? Color.cyan : Color.yellow;

            Gizmos.matrix = Matrix4x4.TRS(worldPos, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, cubeSize);
        }
    }
}