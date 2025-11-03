using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float lifetime = 3f;
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Make the bullet fly forward
        rb.linearVelocity = transform.forward * speed;

        // Destroy the bullet after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    // This runs when the bullet hits another object (if it's a trigger)
    void OnTriggerEnter(Collider other)
    {
        // Check if we hit a destructible target
        if (other.CompareTag("Destructible"))
        {
            DestructibleTarget target = other.GetComponent<DestructibleTarget>();
            if (target != null)
            {
                target.TakeDamage(10f); // Pistol deals 10 damage
            }
        }

        // Don't hit the player who fired it
        if (other.CompareTag("Player"))
        {
            return;
        }

        // Destroy the bullet when it hits anything
        Destroy(gameObject);
    }
}