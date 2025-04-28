using UnityEngine;

public class HoleDecalSpawner : MonoBehaviour
{
    [SerializeField] private GameObject decalPrefab;
    [SerializeField] private float decalOffset = 0.01f;
    [SerializeField] private float decalLifetime = 1000f;
    [SerializeField] private bool randomizeRotation = true;

    /// <summary>
    /// Spawns a decal at the hit point with proper orientation and optional random roll.
    /// </summary>
    /// <param name="position">World hit point</param>
    /// <param name="normal">Surface normal at the hit point</param>
    /// <param name="parent">Optional transform to parent the decal to</param>
    public void SpawnDecal(Vector3 position, Vector3 normal, Transform parent = null)
    {
        if (decalPrefab == null) return;

        Quaternion baseRotation = Quaternion.LookRotation(-normal);

        if (randomizeRotation)
        {
            Quaternion randomRoll = Quaternion.AngleAxis(Random.Range(0f, 360f), normal);
            baseRotation = randomRoll * baseRotation;
        }

        Vector3 decalPosition = position + normal * decalOffset;
        GameObject decalInstance = Instantiate(decalPrefab, decalPosition, baseRotation);

        if (parent != null)
        {
            decalInstance.transform.SetParent(parent);
        }

        Destroy(decalInstance, decalLifetime);
    }
}