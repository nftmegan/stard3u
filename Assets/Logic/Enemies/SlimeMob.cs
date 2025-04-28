using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
}

[RequireComponent(typeof(Rigidbody))]
public class SlimeMob : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float jumpCooldown = 2f;
    [SerializeField] private float jumpHorizontalForce = 1.5f;
    [SerializeField] private float jumpRandomness = 1f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.1f;

    private Rigidbody rb;
    private float nextJumpTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (Time.time >= nextJumpTime && IsGrounded())
        {
            Jump();
            nextJumpTime = Time.time + jumpCooldown + Random.Range(-jumpRandomness, jumpRandomness);
        }
    }

    private void Jump()
    {
        // Random horizontal direction
        Vector3 horizontalDir = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        Vector3 jumpVector = horizontalDir * jumpHorizontalForce + Vector3.up * jumpForce;

        // Rotate slime to face jump direction
        if (horizontalDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalDir);
            transform.rotation = targetRotation;
        }

        rb.AddForce(jumpVector, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up, Vector3.down, groundCheckDistance);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        currentHealth -= amount;
        Debug.Log($"Slime took {amount} damage. Remaining HP: {currentHealth}");

        // Optional: play hit animation, spawn particles

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Slime died!");
        Destroy(gameObject);
    }
}