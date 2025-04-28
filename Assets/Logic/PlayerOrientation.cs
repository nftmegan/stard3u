using UnityEngine;

public interface IAimProvider
{
    Vector3 GetAimHitPoint();
}

public class PlayerOrientation : MonoBehaviour, IAimProvider
{
    public Transform cameraHolder;

    private Quaternion orientation;
    private float yaw;
    private float pitch;

    [Header("Look Settings")]
    public float sensitivity = 2f;
    public float minPitch = -90f;
    public float maxPitch = 90f;

    [Header("Aim Settings")]
    public float maxAimDistance = 100f;

    public Quaternion CurrentOrientation => orientation;
    public float Pitch => pitch;
    public float Yaw => yaw;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = cameraHolder.localEulerAngles.x;
        orientation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Called every frame by PlayerManager with Look input (e.g., mouse or stick).
    /// </summary>
    public void ApplyLookInput(float lookX, float lookY)
    {
        yaw += lookX * sensitivity;
        pitch = Mathf.Clamp(pitch - lookY * sensitivity, minPitch, maxPitch);
        yaw = Mathf.Repeat(yaw, 360f);
        orientation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Returns the world point the camera is currently aiming at (raycast hit or max distance).
    /// </summary>
    public Vector3 GetAimHitPoint()
    {
        Ray ray = GetLookRay();
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
        {
            return hit.point;
        }
        return ray.origin + ray.direction * maxAimDistance;
    }

    /// <summary>
    /// Returns a Ray representing the forward view from the camera.
    /// </summary>
    public Ray GetLookRay()
    {
        Vector3 origin = cameraHolder.position + cameraHolder.forward * 0.5f;
        Vector3 direction = cameraHolder.forward;
        return new Ray(origin, direction);
    }

    /// <summary>
    /// Returns just the forward look direction of the camera.
    /// </summary>
    public Vector3 GetLookDirection()
    {
        return cameraHolder.forward;
    }

    /// <summary>
    /// Returns the raw position the camera is casting from (eye origin).
    /// </summary>
    public Vector3 GetLookOrigin()
    {
        return cameraHolder.position;
    }

    /// <summary>
    /// Returns a direction from a given origin toward the aim point.
    /// </summary>
    public Vector3 GetAimDirection(Vector3 from)
    {
        return (GetAimHitPoint() - from).normalized;
    }
}
