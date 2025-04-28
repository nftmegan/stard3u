using UnityEngine;

public interface IAimProvider
{
    Vector3 GetAimHitPoint();
}

public class PlayerLook : MonoBehaviour, IAimProvider
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Look Settings")]
    public float sensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Aim Settings")]
    public float maxAimDistance = 100f;

    private float yaw;
    private float pitch;

    public Quaternion Orientation => Quaternion.Euler(0f, yaw, 0f);
    public float Pitch => pitch;
    public float Yaw => yaw;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = cameraPivot.localEulerAngles.x;
    }

    public void HandleInput(IPlayerInput input)
    {
        if (input == null) return;
        ApplyLookInput(input.LookAxisX, input.LookAxisY);
    }

    private void ApplyLookInput(float lookX, float lookY)
    {
        yaw += lookX * sensitivity;
        pitch = Mathf.Clamp(pitch - lookY * sensitivity, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public Vector3 GetAimHitPoint()
    {
        Ray ray = GetLookRay();
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
        {
            return hit.point;
        }
        return ray.origin + ray.direction * maxAimDistance;
    }

    public Ray GetLookRay()
    {
        Vector3 origin = cameraPivot.position + cameraPivot.forward * 0.5f;
        Vector3 direction = cameraPivot.forward;
        return new Ray(origin, direction);
    }

    public Vector3 GetLookDirection()
    {
        return cameraPivot.forward;
    }

    public Vector3 GetLookOrigin()
    {
        return cameraPivot.position;
    }

    public Vector3 GetAimDirection(Vector3 from)
    {
        return (GetAimHitPoint() - from).normalized;
    }
}