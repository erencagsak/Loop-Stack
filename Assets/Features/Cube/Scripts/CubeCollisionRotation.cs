using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeCollisionRotation : MonoBehaviour
{
    [Header("Collision Rotation")]
    [Range(0f, 30f)] public float collisionTorqueMultiplier = 8f;
    [Range(0f, 1f)]  public float torqueDuration = 0.3f;

    private Rigidbody _rb;
    private float _torqueTimer;
    private Vector3 _pendingTorque;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 avgNormal = Vector3.zero;
        foreach (ContactPoint contact in collision.contacts)
            avgNormal += contact.normal;

        if (collision.contacts.Length == 0) return;
        avgNormal = (avgNormal / collision.contacts.Length).normalized;

        Vector3 hitDirection = -avgNormal;

        Vector3 localHit = transform.InverseTransformDirection(hitDirection);

        Vector3 torqueAxis = Vector3.Cross(localHit, Vector3.up);

        float impactStrength = collision.relativeVelocity.magnitude;

        _pendingTorque = transform.TransformDirection(torqueAxis) 
            * impactStrength 
            * collisionTorqueMultiplier;
        _torqueTimer = torqueDuration;
    }

    private void FixedUpdate()
    {
        if (_torqueTimer <= 0f) return;

        float fade = _torqueTimer / torqueDuration;
        
        _rb.AddTorque(_pendingTorque * fade, ForceMode.Acceleration);
        
        _torqueTimer -= Time.fixedDeltaTime;
    }
}