using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FirearmAudioHandler : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip fireClip;          // "Gunshot"
    [SerializeField] private AudioClip reloadClip;        // "Reload"
    [SerializeField] private AudioClip dryFireClip;       // "Click" when firing with no ammo

    [Header("Volumes")]
    [SerializeField] private float fireVolume = 1f;
    [SerializeField] private float reloadVolume = 1f;
    [SerializeField] private float dryFireVolume = 0.8f;

    private AudioSource audioSource;
    private bool isReloading = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Play fire sound (when the weapon shoots)
    public void PlayFire()
    {
        if (fireClip == null) return;

        audioSource.PlayOneShot(fireClip, fireVolume);
    }

    // Play reload sound (when the weapon reloads)
    public void PlayReload()
    {
        if (reloadClip == null || isReloading) return;

        isReloading = true;
        audioSource.PlayOneShot(reloadClip, reloadVolume);
    }

    // Play dry fire sound (when there’s no ammo)
    public void PlayDryFire()
    {
        if (dryFireClip == null) return;

        audioSource.PlayOneShot(dryFireClip, dryFireVolume);
    }

    // New method: Play the shoot sound
    public void PlayShootSound()
    {
        PlayFire(); // Simply calls the PlayFire method to play the shooting sound
    }

    // Reset reloading state when reload is done
    public void OnReloadComplete()
    {
        isReloading = false;
    }
}
