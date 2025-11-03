using UnityEngine;

public class DuplicateController : MonoBehaviour
{
    private Animator animator;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public float smoothing = 7.5f;
    public Vector3 positionOffset = new Vector3(3f, 0, 0f);
    public float meleeRange = 2f; 
    public float swordDamage = 25f; 
    public float axeDamage = 35f;

    public GameObject swordObject;
    public GameObject weapon2Object; // Pistol
    public LaserSight pistolLaser;
    public GameObject bulletPrefab;
    public Transform pistolMuzzle;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        UpdateWeaponVisibility(0);
    }

    void UpdateWeaponVisibility(int state)
    {
        if (swordObject != null)
            swordObject.SetActive(state == 1);
        if (weapon2Object != null)
            weapon2Object.SetActive(state == 2);
        
        if (pistolLaser != null)
    {
        pistolLaser.enabled = (state == 2);
        pistolLaser.GetComponent<LineRenderer>().enabled = (state == 2);
    }
    }

    // This is called by NetworkClient
    public void UpdateState(PlayerAction action)
    {
        // Position and Rotation
        Vector3 playerPosition = new Vector3(action.posX, action.posY, action.posZ);
        Quaternion playerRotation = Quaternion.Euler(0, action.rotY, 0);
        Vector3 rotatedOffset = playerRotation * positionOffset;
        targetPosition = playerPosition + rotatedOffset;
        targetRotation = playerRotation;

        // Movement States
        animator.SetFloat("moveX", action.moveX);
        animator.SetFloat("moveY", action.moveY);
        animator.SetInteger("equippedWeapon", action.equippedWeapon);
        animator.SetBool("isAiming", action.isAiming);

        // Jump Event
        if (action.didJump)
        {
            animator.SetTrigger("Jump");
        }
        
        // Attack Event
        if (action.attackType == 1)
        {
            animator.SetTrigger("Attack_Sword");
            DoMeleeAttackCheck(swordDamage);
        }
        else if (action.attackType == 2) // Pistol
        {
            animator.SetTrigger("Attack_Weapon2");

            // --- MODIFIED LOGIC ---
            if (bulletPrefab != null && pistolMuzzle != null)
            {
                // Use the fire direction from the network message
                Quaternion fireRotation = Quaternion.LookRotation(action.fireDirection);
                Instantiate(bulletPrefab, pistolMuzzle.position, fireRotation);
            }
        }

        // Continuously set weapon visibility
        UpdateWeaponVisibility(action.equippedWeapon);
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothing);
    }
    // This function performs a melee attack for the duplicate
    void DoMeleeAttackCheck(float damage)
    {
        // Cast a ray from the duplicate's body, in the direction it's facing
        Vector3 castOrigin = transform.position + Vector3.up; // Start from chest height

        RaycastHit hit;
        if (Physics.Raycast(castOrigin, transform.forward, out hit, meleeRange))
        {
            if (hit.collider.CompareTag("Destructible"))
            {
                DestructibleTarget target = hit.collider.GetComponent<DestructibleTarget>();
                if (target != null)
                {
                    target.TakeDamage(damage);
                }
            }
        }
    }
}