using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraResolutionFix : MonoBehaviour
{
    [Header("Reference Resolution")]
    public float targetWidth = 1080f;
    public float targetHeight = 1920f;

    private Camera cam;
    private float defaultFov;
    private float defaultOrthoSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        
        defaultFov = cam.fieldOfView;
        defaultOrthoSize = cam.orthographicSize;
        
        FixCamera();
    }

    public void FixCamera()
    {
        float targetAspect = targetWidth / targetHeight;
        
        float currentAspect = (float)Screen.width / (float)Screen.height;

        if (currentAspect < targetAspect)
        {
            float aspectMultiplier = targetAspect / currentAspect;

            if (cam.orthographic)
            {
                cam.orthographicSize = defaultOrthoSize * aspectMultiplier;
            }
            else
            {
                cam.fieldOfView = Mathf.Atan(Mathf.Tan(defaultFov * Mathf.Deg2Rad * 0.5f) * aspectMultiplier) * Mathf.Rad2Deg * 2f;
            }
        }
        else
        {
            if (cam.orthographic)
                cam.orthographicSize = defaultOrthoSize;
            else
                cam.fieldOfView = defaultFov;
        }
    }

    #if UNITY_EDITOR
        void Update()
        {
            FixCamera();
        }
    #endif
}