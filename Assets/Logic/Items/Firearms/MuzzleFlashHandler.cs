using System.Collections;
using UnityEngine;

public class MuzzleFlashHandler : MonoBehaviour
{
    [Header("Muzzle Flash Settings")]
    [Tooltip("The ParticleSystem to play for the muzzle flash.")]
    [SerializeField] private ParticleSystem muzzleParticles;

    /// <summary>
    /// Call this to play the muzzle flash particles.
    /// </summary>
    public void Muzzle()
    {
        if (muzzleParticles == null)
        {
            Debug.LogWarning("MuzzleFlashController: No ParticleSystem assigned.");
            return;
        }
        muzzleParticles.Play();
    }
}
