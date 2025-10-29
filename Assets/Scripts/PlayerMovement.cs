using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3.0f;
    public float runSpeed = 7.0f; // <-- NEW
    public float lookSpeed = 2.0f;
    public float lookXLimit = 80.0f;

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    private Animator animator;

    // --- NEW: For Network Actions ---
    public NetworkClient networkClient; // Assign in Inspector
    private float actionTimer;
    private float actionSendRate = 0.05f; // 20 times per second
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        animator = GetComponentInChildren<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Player Movement ---
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // --- MODIFIED: Check for run key ---
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        float curSpeedX = currentSpeed * Input.GetAxis("Vertical"); // W/S keys
        float curSpeedY = currentSpeed * Input.GetAxis("Horizontal"); // A/D keys
        
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Apply gravity
        if (!controller.isGrounded)
        {
            moveDirection.y -= 9.81f * Time.deltaTime;
        }

        // Move the controller
        controller.Move(moveDirection * Time.deltaTime);

        // --- MODIFIED: UPDATE ANIMATOR ---
        if (animator != null)
        {
            int currentMoveState = 0;
            bool isMoving = (curSpeedX != 0 || curSpeedY != 0);

            if (isMoving)
            {
                currentMoveState = isRunning ? 2 : 1; // 2=run, 1=walk
            }
            
            animator.SetInteger("moveState", currentMoveState);
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
        // 1. Create the action data object
        PlayerAction action = new PlayerAction();
        action.posX = transform.position.x;
        action.posY = transform.position.y;
        action.posZ = transform.position.z;
        action.rotY = transform.eulerAngles.y; 
        
        // --- MODIFIED ---
        action.moveState = animator.GetInteger("moveState");

        // 2. Convert to JSON
        string json = JsonUtility.ToJson(action);

        // 3. Send to network (with a prefix to identify it)
        networkClient.SendActionData("ACTION:" + json);
    }
}