using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BowAudioHandler : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip drawStartClip;
    [SerializeField] private AudioClip drawLoopClip;
    [SerializeField] private AudioClip releaseStringClip; // "Twang"
    [SerializeField] private AudioClip shootClip;         // "Airburst"

    [Header("Volumes")]
    [SerializeField] private float drawStartVolume = 1f;
    [SerializeField] private float loopVolume = 0.8f;
    [SerializeField] private float releaseVolume = 1f;
    [SerializeField] private float shootVolume = 1f;

    private AudioSource audioSource;
    private bool isLooping = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayDrawStart()
    {
        StopLoop();
        if (drawStartClip == null) return;

        audioSource.clip = drawStartClip;
        audioSource.volume = drawStartVolume;
        audioSource.loop = false;
        audioSource.Play();
    }

    public void StartLoop()
    {
        if (drawLoopClip == null || isLooping) return;

        audioSource.clip = drawLoopClip;
        audioSource.volume = loopVolume;
        audioSource.loop = true;
        audioSource.Play();
        isLooping = true;
    }

    public void StopLoop()
    {
        if (!isLooping) return;

        audioSource.Stop();
        isLooping = false;
    }

    public void PlayRelease()
    {
        StopLoop();

        if (releaseStringClip != null)
            audioSource.PlayOneShot(releaseStringClip, releaseVolume);

        if (shootClip != null)
            audioSource.PlayOneShot(shootClip, shootVolume);
    }
}