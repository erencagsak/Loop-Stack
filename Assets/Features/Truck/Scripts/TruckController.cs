using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(TruckGrid))]
[RequireComponent(typeof(TruckAnimator))]
public class TruckController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Bu değer artık otomatik hesaplanıyor (Renk Sayısı x 48)")]
    public int maxCapacity; 

    [Header("Live Stack View (Read Only)")]
    [SerializeField] private List<string> liveStackView = new List<string>();

    [Header("Visuals")]
    public Transform truckCover;
    private bool _isCoverClosed = false;
    public bool IsCoverClosed => _isCoverClosed;

    private TruckGrid _grid;
    private TruckAnimator _animator;
    private ConveyorController _conveyorController;
    private LevelBuilder _builder;
    
    private List<string> _colors;
    private readonly Stack<CubeController> _cubeStack = new Stack<CubeController>();
    private Transform _conveyorCubesContainer;

    private bool _isPoppingSequence = false; 
    private int _incomingCubesCount = 0;

    private static readonly List<TruckController> _activeTrucks = new List<TruckController>();
    public static IReadOnlyList<TruckController> ActiveTrucks => _activeTrucks;

    private void Awake()
    {
        _activeTrucks.Add(this);
        
        _grid = GetComponent<TruckGrid>();
        _animator = GetComponent<TruckAnimator>();

        if (truckCover != null) truckCover.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _activeTrucks.Remove(this);
    }

    public void Initialize(List<string> cubeColors, LevelBuilder levelBuilder, Transform conveyorCubes)
    {
        _colors = cubeColors;
        _builder = levelBuilder;
        _conveyorCubesContainer = conveyorCubes;

        maxCapacity = 192;

        if (conveyorCubes != null)
        {
            _conveyorController = conveyorCubes.parent.GetComponent<ConveyorController>();
        }

        BuildCubes();
    }

    private void BuildCubes()
    {
        liveStackView.Clear();
        if (_colors == null || _colors.Count == 0 || _builder == null) return;

        int cubesPerColorBlock = _grid.GetCubesPerColorBlock();

        for (int i = 0; i < _colors.Count; i++)
        {
            string colorCode = _colors[i];

            for (int j = 0; j < cubesPerColorBlock; j++)
            {
                GameObject spawnedCube = _builder.GetCube(colorCode, out Color cubeColor);
                
                if (spawnedCube.TryGetComponent<CubeController>(out var controller))
                {
                    controller.InitializeColor(colorCode, cubeColor);
                    PushCube(controller, colorCode); 
                }
            }
        }
    }

    public void PushCube(CubeController cubeController, string colorCode = "")
    {
        GameObject cubeObj = cubeController.gameObject;
        
        Vector3 targetPos = _grid.GetCubeTargetPosition(_cubeStack.Count);
        _animator.InstantPlaceCube(cubeObj, targetPos);
        
        if (!string.IsNullOrEmpty(colorCode)) 
            cubeObj.name = $"Cube_{colorCode}_{_cubeStack.Count}";

        if (cubeObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true; 

        _cubeStack.Push(cubeController);
        liveStackView.Add(cubeObj.name);
    }

    public void PopMatchingTopCubes()
    {
        if (_isCoverClosed) return;

        if (_cubeStack.Count == 0 || _isPoppingSequence || _incomingCubesCount > 0) return;

        string targetColor = GetTopCubeColor();

        if (string.IsNullOrEmpty(targetColor)) return;

        _isPoppingSequence = true; 
        int popIndex = 0;

        while (_cubeStack.Count > 0 && GetTopCubeColor() == targetColor)
        {
            PopCubeAnimated(popIndex * _animator.popDelayOffset);
            popIndex++;
        }

        float totalDuration = ((popIndex - 1) * _animator.popDelayOffset) + _animator.jumpDuration;

        ResetPoppingSequenceAsync(totalDuration).Forget();
    }

    private async UniTaskVoid ResetPoppingSequenceAsync(float duration)
    {
        await UniTask.Delay(Mathf.RoundToInt(duration * 1000));
        _isPoppingSequence = false;
    }

    private void PopCubeAnimated(float delay)
    {
        CubeController topCubeController = _cubeStack.Pop();
        GameObject topCubeObj = topCubeController.gameObject;

        topCubeController.originTruck = this;
        topCubeController.popTime = Time.time; 
        
        if (liveStackView.Count > 0) liveStackView.RemoveAt(liveStackView.Count - 1);

        if (topCubeObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true; 

        _animator.AnimatePopCube(topCubeObj, delay, _conveyorCubesContainer, (body) => 
        {
            body.isKinematic = false;

            if (_conveyorController != null) _conveyorController.RegisterCube(body);

            if (!body.GetComponent<CubeCollisionRotation>())
                body.gameObject.AddComponent<CubeCollisionRotation>();
        });
    }

    public string GetTopCubeColor()
    {
        if (_cubeStack.Count == 0) return null;

        return _cubeStack.Peek().colorCode; 
    }

    public void ProcessIncomingCube(Collider other)
    {
        if (other.TryGetComponent<CubeController>(out var incomingCubeController))
        {
            if (incomingCubeController.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
            {
                string incomingColor = incomingCubeController.colorCode; 

                if (string.IsNullOrEmpty(incomingColor)) return;

                if (CanAcceptCube(incomingColor, incomingCubeController))
                {
                    AcceptCubeFromConveyor(incomingCubeController, incomingColor);
                }
            }
        }
    }

    private bool CanAcceptCube(string incomingColor, CubeController incomingCube)
    {
        if (_isCoverClosed) return false;

        if (_isPoppingSequence) return false;

        if (incomingCube.originTruck == this && Time.time - incomingCube.popTime < 3f) return false; 

        if (_cubeStack.Count >= maxCapacity) return false;

        if (_cubeStack.Count == 0) 
        {
            foreach (var truck in ActiveTrucks)
            {
                if (truck != this && truck.GetTopCubeColor() == incomingColor && truck._cubeStack.Count < truck.maxCapacity)
                {
                    return false; 
                }
            }

            return true; 
        }

        return GetTopCubeColor() == incomingColor;
    }

    public bool IsTruckSorted()
    {
        if (_cubeStack.Count == 0) return true;

        string targetColor = GetTopCubeColor();

        foreach (var cube in _cubeStack)
        {
            if (cube.colorCode != targetColor) return false; 
        }

        return true; 
    }

    private void AcceptCubeFromConveyor(CubeController cubeController, string colorCode)
    {
        _incomingCubesCount++;
        GameObject cubeObj = cubeController.gameObject;

        if (cubeObj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (_conveyorController != null) _conveyorController.UnregisterCube(rb);

            if (cubeObj.TryGetComponent<CubeCollisionRotation>(out var collRot))
                Destroy(collRot);

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 targetPos = _grid.GetCubeTargetPosition(_cubeStack.Count);

        _cubeStack.Push(cubeController);
        liveStackView.Add(cubeObj.name);

        _animator.AnimateAcceptCube(cubeObj, targetPos, () => 
        {
            _incomingCubesCount--; 

            if (_cubeStack.Count >= maxCapacity && !_isCoverClosed)
            {
                CloseCover();
            }

            if (LevelRuleManager.Instance != null)
            {
                LevelRuleManager.Instance.UpdateCubeProgress();
                LevelRuleManager.Instance.CheckWinCondition();
            }
        });
    }

    public IEnumerable<CubeController> GetCubesInTruck()
    {
        return _cubeStack;
    }

    private void CloseCover()
    {
        if (truckCover == null || _isCoverClosed) return;

        _isCoverClosed = true;

        Vector3 targetLocalPos = truckCover.localPosition;

        truckCover.localPosition = targetLocalPos + (Vector3.up * 3f);
        truckCover.gameObject.SetActive(true);

        truckCover.DOLocalMoveY(targetLocalPos.y, 1f)
            .SetEase(Ease.OutBounce) 
            .SetLink(truckCover.gameObject)
            .OnComplete(() => 
            {
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayVFX("Confetti", truckCover.position + Vector3.up * 0.5f);

                GameLogger.Log(GameLogger.LogCategory.Gameplay, $"{gameObject.name} cover closed!");
            });
    }
}