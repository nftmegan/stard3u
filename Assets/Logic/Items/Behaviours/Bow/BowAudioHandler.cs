using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BowAudioHandler : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip drawStartClip;
    [SerializeField] private AudioClip drawLoopClip;
    [SerializeField] private AudioClip releaseStringClip; // "Twang"
    [SerializeField] private AudioClip shootClip;         // "Airburst" / Whoosh
    [SerializeField] private AudioClip dryFireClip;       // NEW: Clip for when no arrows
    [SerializeField] private AudioClip cancelDrawClip;    // Optional: Clip for cancelling draw

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float drawStartVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float loopVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float releaseVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float dryFireVolume = 0.9f; // NEW
    [SerializeField] [Range(0f, 1f)] private float cancelVolume = 0.7f;  // Optional

    private AudioSource audioSource;
    private bool isLooping = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false; // Ensure loop is off by default
    }

    public void PlayDrawStart()
    {
        StopLoop(); // Stop any existing loop
        PlayClip(drawStartClip, drawStartVolume, false);
    }

    public void StartLoop()
    {
        // Start loop only if clip exists and not already looping
        if (drawLoopClip != null && !isLooping)
        {
            PlayClip(drawLoopClip, loopVolume, true); // Set loop to true
            isLooping = true;
        }
    }

    public void StopLoop()
    {
        if (isLooping)
        {
            // Only stop if the looping clip is currently assigned and playing
            if (audioSource.clip == drawLoopClip && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            isLooping = false;
            audioSource.loop = false; // Turn loop off
        }
    }

    public void PlayRelease()
    {
        StopLoop(); // Make sure loop stops on release

        // Play the string release sound using PlayOneShot for overlap potential
        if (releaseStringClip != null)
            audioSource.PlayOneShot(releaseStringClip, releaseVolume);

        // Play the arrow whoosh sound using PlayOneShot
        if (shootClip != null)
            audioSource.PlayOneShot(shootClip, shootVolume);
    }

    // --- NEW METHOD ---
    public void PlayDryFire()
    {
        StopLoop(); // Shouldn't be looping, but safety check
        // Play the dry fire sound using PlayOneShot
        if (dryFireClip != null)
            audioSource.PlayOneShot(dryFireClip, dryFireVolume);
    }

    // --- Optional Cancel Sound ---
    public void PlayCancel()
    {
         StopLoop(); // Ensure loop stops if cancelling
         if(cancelDrawClip != null)
            audioSource.PlayOneShot(cancelDrawClip, cancelVolume);
    }


    // Helper method to reduce repetition
    private void PlayClip(AudioClip clip, float volume, bool loop)
    {
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();
    }
}