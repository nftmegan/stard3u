using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BulletAudioHandler : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip impactClip;       // "Bullet impact sound"

    [Header("Volume Settings")]
    [SerializeField] private float impactVolume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // Play impact sound when the bullet hits something
    public void PlayImpactSound()
    {
        if (impactClip != null)
        {
            audioSource.PlayOneShot(impactClip, impactVolume);
        }
    }
}