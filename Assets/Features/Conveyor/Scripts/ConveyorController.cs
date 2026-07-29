using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;

public class ConveyorController : MonoBehaviour
{
    [Header("Conveyor Settings")]
    [Range(1f, 20f)] public float maxSpeed = 10f;          
    [Range(1f, 50f)] public float conveyorForce = 8f;    
    [Range(0f, 30f)] public float rollForce = 4f;        

    [Header("Chaos & Spreading for cubes moving")]
    [Range(1f, 5f)] public float beltWidth = 3f;     
    [Range(0f, 50f)] public float lateralForce = 18.5f;  
    [Range(0.1f, 10f)] public float weaveSpeed = 0.9f;   

    private const float SpeedVariationRange = 0.1f;
    private const float BeltWidthDivider = 2.2f;

    private SplineComputer _splineComputer;
    private SplineSample _reusableSample = new SplineSample();

    private struct ConveyorCubeData
    {
        public Rigidbody Rb;
        public float UniqueMultiplier;
        public float WeaveOffset;
    }

    private readonly List<ConveyorCubeData> _activeCubes = new List<ConveyorCubeData>();

    private void Awake()
    {
        _splineComputer = GetComponent<SplineComputer>();
    }

    // Registers a cube to be controlled by the conveyor system
    public void RegisterCube(Rigidbody rb)
    {
        if (rb == null) return;

        float idOffset = rb.gameObject.GetInstanceID();
        float multiplier = 1f + (Mathf.Abs(idOffset) % 5 * SpeedVariationRange);

        _activeCubes.Add(new ConveyorCubeData
        {
            Rb = rb,
            UniqueMultiplier = multiplier,
            WeaveOffset = idOffset
        });
    }
    // Unregisters a cube from the conveyor system
    public void UnregisterCube(Rigidbody rb)
    {
        for (int i = _activeCubes.Count - 1; i >= 0; i--)
        {
            if (_activeCubes[i].Rb == rb)
            {
                _activeCubes.RemoveAt(i);
                break;
            }
        }
    }

    // LOOP
    private void FixedUpdate()
    {
        if (_splineComputer == null || _activeCubes.Count == 0) return;

        float currentTime = Time.time;

        for (int i = _activeCubes.Count - 1; i >= 0; i--)
        {
            ConveyorCubeData cubeData = _activeCubes[i];

            if (cubeData.Rb == null || !cubeData.Rb.gameObject.activeInHierarchy)
            {
                _activeCubes.RemoveAt(i);
                continue;
            }

            if (cubeData.Rb.isKinematic) continue;

            _splineComputer.Project(cubeData.Rb.position, ref _reusableSample);

            float currentForwardSpeed = Vector3.Dot(cubeData.Rb.velocity, _reusableSample.forward);

            if (currentForwardSpeed < maxSpeed)
            {
                cubeData.Rb.AddForce(_reusableSample.forward * conveyorForce * cubeData.UniqueMultiplier, ForceMode.Acceleration);
            }

            float randomWeave = Mathf.Sin(currentTime * weaveSpeed + cubeData.WeaveOffset); 
            float targetLateralOffset = randomWeave * (beltWidth / BeltWidthDivider); 

            Vector3 targetPosOnBelt = _reusableSample.position + (_reusableSample.right * targetLateralOffset);
            
            Vector3 dirToTarget = targetPosOnBelt - cubeData.Rb.position;
            dirToTarget.y = 0f; 
            
            cubeData.Rb.AddForce(dirToTarget * lateralForce, ForceMode.Acceleration);

            Vector3 chaoticRollAxis = Vector3.Lerp(_reusableSample.right, cubeData.Rb.transform.up, 0.1f).normalized; 
            cubeData.Rb.AddTorque(chaoticRollAxis * rollForce, ForceMode.Acceleration);
        }
    }
}