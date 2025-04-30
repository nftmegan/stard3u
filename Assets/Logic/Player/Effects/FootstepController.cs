using UnityEngine;
using KinematicCharacterController;

[RequireComponent(typeof(AudioSource))]
public class FootstepController : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip[] footstepClips;

    [Header("Step Timing")]
    public float stepInterval = 0.5f;
    public float velocityStepInfluence = 0.3f;
    public float minSpeedToStep = 0.1f;

    private AudioSource audioSource;
    private MyCharacterController characterController;

    private float stepTimer;
    private int lastPlayedIndex = -1;
    private bool wasGroundedLastFrame = true;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<MyCharacterController>();
    }

    private void Update()
    {
        if (characterController == null || footstepClips == null || footstepClips.Length == 0)
            return;

        bool isGrounded = characterController.Motor.GroundingStatus.IsStableOnGround;
        Vector3 velocity = characterController.Velocity;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        // LANDING SOUND
        if (!wasGroundedLastFrame && isGrounded)
        {
            PlayFootstep();
        }

        // REGULAR FOOTSTEPS
        if (isGrounded && horizontalSpeed > minSpeedToStep)
        {
            float dynamicInterval = stepInterval / (1f + horizontalSpeed * velocityStepInfluence);

            stepTimer += Time.deltaTime;
            if (stepTimer >= dynamicInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }

        wasGroundedLastFrame = isGrounded;
    }

    private void PlayFootstep()
    {
        if (footstepClips.Length == 0)
            return;

        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        } while (index == lastPlayedIndex && footstepClips.Length > 1);

        lastPlayedIndex = index;

        audioSource.PlayOneShot(footstepClips[index]);
    }
}