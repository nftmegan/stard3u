using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ArrowAudioHandler : MonoBehaviour
{
    [SerializeField] private AudioClip impactSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayImpactSound()
    {
        if (impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }
}