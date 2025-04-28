using UnityEngine;

public abstract class ProjectileBehavior : MonoBehaviour
{
    public abstract void Launch(Vector3 direction, float force);
}