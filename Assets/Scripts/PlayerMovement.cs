using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3.0f;
    public float runSpeed = 7.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 80.0f;
    public float jumpForce = 8.0f; 
    public float gravity = 20.0f;

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    private Animator animator;
    public NetworkClient networkClient;
    private float actionTimer;
    private float actionSendRate = 0.05f;

    private bool justJumped = false; 
    public GameObject swordObject; 
    private bool isSwordEquipped = false;
    private bool justAttacked = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        animator = GetComponentInChildren<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (swordObject != null)
            swordObject.SetActive(false);
    }

    void Update()
    {
        // --- Player Movement ---
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // Get Horizontal/Vertical input
        float curSpeedX = currentSpeed * Input.GetAxis("Vertical");
        float curSpeedY = currentSpeed * Input.GetAxis("Horizontal");
        
        // --- MODIFIED: Gravity and Jump Logic ---
        // We preserve the Y-axis speed from last frame
        float moveDirectionY = moveDirection.y;
        
        // Set X and Z movement
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Check if we are on the ground
        if (controller.isGrounded)
        {
            // If grounded, check for jump button
            // "Jump" is Spacebar by default
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce; // Apply jump force
                animator.SetTrigger("Jump"); // Tell animator
                justJumped = true; // Tell network
            }
            else
            {
                // If we are grounded and NOT jumping, Y speed is 0
                moveDirection.y = 0;
            }
        }
        else
        {
            // If in the air, restore Y speed and apply gravity
            moveDirection.y = moveDirectionY;
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        controller.Move(moveDirection * Time.deltaTime);
        // ------------------------------------------

        // --- UPDATE ANIMATOR ---
        if (animator != null)
        {
            // Only set walking/running if on the ground
            if (controller.isGrounded)
            {
                int currentMoveState = 0;
                bool isMoving = (curSpeedX != 0 || curSpeedY != 0);

                if (isMoving)
                {
                    currentMoveState = isRunning ? 2 : 1;
                }

                animator.SetInteger("moveState", currentMoveState);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isSwordEquipped = !isSwordEquipped; // Toggle
            swordObject.SetActive(isSwordEquipped);
        }

        // Attack (Left Mouse)
        if (Input.GetMouseButtonDown(0) && isSwordEquipped) // 0 is Left Click
        {
            animator.SetTrigger("Attack");
            justAttacked = true; // Tell network
        }


        // --- Send Action Data over Network ---
        actionTimer += Time.deltaTime;
        if (actionTimer >= actionSendRate)
        {
            actionTimer = 0f;
            SendActionData();
        }

        // --- Camera Rotation ---
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    void SendActionData()
    {
        PlayerAction action = new PlayerAction();
        action.posX = transform.position.x;
        action.posY = transform.position.y;
        action.posZ = transform.position.z;
        action.rotY = transform.eulerAngles.y; 
        action.moveState = animator.GetInteger("moveState");

        //  Send the jump event ---
        action.didJump = justJumped;

        action.isEquipped = isSwordEquipped;
        action.didAttack = justAttacked;

        // Reset the event so we only send it once
        justJumped = false; 
        justAttacked = false;
        // -------------------------------

        string json = JsonUtility.ToJson(action);
        networkClient.SendActionData("ACTION:" + json);
    }
}