using UnityEngine;

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
    private CharacterMotor motor;

    private float stepTimer;
    private int lastPlayedIndex = -1;
    private bool wasGroundedLastFrame = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        motor = GetComponent<CharacterMotor>();
    }

    void Update()
    {
        if (motor == null || footstepClips == null || footstepClips.Length == 0)
            return;

        bool isGrounded = motor.IsGrounded;
        float horizontalSpeed = new Vector3(motor.CurrentVelocity.x, 0f, motor.CurrentVelocity.z).magnitude;

        // LANDING SOUND
        if (!wasGroundedLastFrame && isGrounded)
        {
            PlayFootstep(horizontalSpeed);
        }

        // REGULAR FOOTSTEPS
        if (isGrounded && horizontalSpeed > minSpeedToStep)
        {
            float dynamicInterval = stepInterval / (1f + horizontalSpeed * velocityStepInfluence);

            stepTimer += Time.deltaTime;
            if (stepTimer >= dynamicInterval)
            {
                PlayFootstep(horizontalSpeed);
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }

        wasGroundedLastFrame = isGrounded;
    }

    private void PlayFootstep(float speed)
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
