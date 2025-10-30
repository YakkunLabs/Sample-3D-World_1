using UnityEngine;

public class DuplicateController : MonoBehaviour
{
    private Animator animator;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public float smoothing = 7.5f; 
    public Vector3 positionOffset = new Vector3(3f, 0, 0f);
    public GameObject swordObject;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        if (swordObject != null)
            swordObject.SetActive(false);
    }

    // This is called by NetworkClient
    public void UpdateState(PlayerAction action)
        {
            Vector3 playerPosition = new Vector3(action.posX, action.posY, action.posZ);
            Quaternion playerRotation = Quaternion.Euler(0, action.rotY, 0);

            Vector3 rotatedOffset = playerRotation * positionOffset;
            targetPosition = playerPosition + rotatedOffset;
            
            targetRotation = playerRotation;
            animator.SetInteger("moveState", action.moveState);

        // --- ADD THIS IF-STATEMENT ---
        if (action.didJump)
        {
            animator.SetTrigger("Jump");
        }
            if (action.didAttack)
        {
            animator.SetTrigger("Attack");
        }

        // Continuously set sword visibility
        if (swordObject != null)
        {
            swordObject.SetActive(action.isEquipped);
        }
        }

    // In Update, we smoothly move to the target position
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothing);
    }
}