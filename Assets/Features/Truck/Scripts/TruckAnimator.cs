using UnityEngine;
using DG.Tweening;
using System;

public class TruckAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float jumpPower = 1f;
    public float jumpDuration = 0.1f;
    public float popDelayOffset = 0.05f; 

    [Header("References (Auto-Filled)")]
    public Transform truckCubesContainer;
    public Transform transferPoint; 

    // Cubes position and rotation animation when accepted into the truck
    public void AnimateAcceptCube(GameObject cubeObj, Vector3 targetPos, Action onComplete)
    {
        cubeObj.transform.DOKill();
        
        cubeObj.transform.DOJump(targetPos, jumpPower, 1, jumpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (cubeObj == null) return;

                cubeObj.transform.SetParent(truckCubesContainer, true);
                cubeObj.transform.position = targetPos;
                cubeObj.transform.rotation = transform.rotation;
                onComplete?.Invoke();
            });

        cubeObj.transform.DORotate(transform.eulerAngles, jumpDuration).SetEase(Ease.OutQuad);
    }

    // Cubes position and rotation animation when popping off the conveyor
    public void AnimatePopCube(GameObject topCubeObj, float delay, Transform conveyorContainer, Action<Rigidbody> onComplete)
    {
        if (topCubeObj == null) return;

        if (conveyorContainer != null)
            topCubeObj.transform.SetParent(conveyorContainer, true);
        else
            topCubeObj.transform.SetParent(null);

        topCubeObj.transform.DOKill(); 

        Vector3 baseEndPos = transferPoint != null ? transferPoint.position : transform.position - transform.forward * 3f;

        float randomX = UnityEngine.Random.Range(-0.3f, 0.3f);
        float randomZ = UnityEngine.Random.Range(-0.3f, 0.3f);

        Vector3 finalEndPos = baseEndPos + new Vector3(randomX, 1.5f, randomZ); 

        topCubeObj.transform.DOJump(finalEndPos, jumpPower, 1, jumpDuration)
            .SetDelay(delay) 
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (topCubeObj == null) return;

                if (topCubeObj.TryGetComponent<Rigidbody>(out var body))
                {
                    onComplete?.Invoke(body);
                }
            });

        Vector3 randomTumble = new Vector3(
            UnityEngine.Random.Range(180f, 360f), UnityEngine.Random.Range(180f, 360f), UnityEngine.Random.Range(180f, 360f)
        );

        topCubeObj.transform.DORotate(randomTumble, jumpDuration, RotateMode.LocalAxisAdd)
            .SetDelay(delay) 
            .SetEase(Ease.OutQuad);
    }

    // Instantly place a cube at the target position without animation
    public void InstantPlaceCube(GameObject cubeObj, Vector3 targetPos)
    {
        cubeObj.transform.position = targetPos;
        cubeObj.transform.rotation = transform.rotation;
        cubeObj.transform.SetParent(truckCubesContainer, true);
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        bool changesMade = false;
        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform == null) bodyTransform = transform;

        if (truckCubesContainer == null)
        {
            truckCubesContainer = bodyTransform.Find("Cubes");
            if (truckCubesContainer == null && !Application.isPlaying)
            {
                GameObject cubesObj = new GameObject("Cubes");
                cubesObj.transform.SetParent(bodyTransform, false);
                truckCubesContainer = cubesObj.transform;

                UnityEditor.Undo.RegisterCreatedObjectUndo(cubesObj, "Auto Created Cubes Container");
            }
            changesMade = true;
        }

        if (transferPoint == null)
        {
            transferPoint = transform.Find("TransferPoint");
            if (transferPoint == null && !Application.isPlaying)
            {
                GameObject tpObj = new GameObject("TransferPoint");
                tpObj.transform.SetParent(transform, false);
                tpObj.transform.localPosition = new Vector3(0, 0, -3f); 
                transferPoint = tpObj.transform;
                
                UnityEditor.Undo.RegisterCreatedObjectUndo(tpObj, "Auto Created Transfer Point");
            }
            changesMade = true;
        }

        if (changesMade && truckCubesContainer != null && transferPoint != null)
        {
            GameLogger.Log(GameLogger.LogCategory.Automation, "Truck automation initialized and linked successfully. ✅");
        }
    }
    #endif
}