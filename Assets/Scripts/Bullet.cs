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
        rb.velocity = transform.forward * speed;

        // Destroy the bullet after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    // This runs when the bullet hits another object (if it's a trigger)
    void OnTriggerEnter(Collider other)
    {
        // Don't hit the player who fired it
        if (other.CompareTag("Player"))
        {
            return;
        }

        // For now, just destroy the bullet when it hits anything
        Destroy(gameObject);
    }
}