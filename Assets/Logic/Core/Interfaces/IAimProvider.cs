using UnityEngine;

public interface IAimProvider
{
    Vector3 GetAimHitPoint();
    Ray GetLookRay(); // Added GetLookRay here for consistency
    Transform GetLookTransform(); // Might be useful for weapons
}