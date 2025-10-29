using UnityEngine;

public class DuplicateController : MonoBehaviour
{
    private Animator animator;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public float smoothing = 7.5f; 
    public Vector3 positionOffset = new Vector3(3f, 0, 0f);

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    // This is called by NetworkClient
    public void UpdateState(PlayerAction action)
    {
        Vector3 playerPosition = new Vector3(action.posX, action.posY, action.posZ);
        Quaternion playerRotation = Quaternion.Euler(0, action.rotY, 0);

        Vector3 rotatedOffset = playerRotation * positionOffset;
        targetPosition = playerPosition + rotatedOffset;
        
        targetRotation = playerRotation;

        // --- MODIFIED LINE ---
        animator.SetInteger("moveState", action.moveState);
    }

    // In Update, we smoothly move to the target position
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothing);
    }
}