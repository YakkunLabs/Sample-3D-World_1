using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.0f;
    public float runSpeed = 7.0f;
    public float jumpForce = 8.0f; 
    public float gravity = 20.0f;

    [Header("Camera")]
    public float lookSpeed = 2.0f;
    public float lookXLimit = 80.0f;
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float fovSmoothSpeed = 10f;
    public GameObject crosshairUI;

    [Header("Network & Weapons")]
    public NetworkClient networkClient; 
    public GameObject swordObject;
    public GameObject weapon2Object; // Pistol
    public GameObject bulletPrefab; 
    public Transform pistolMuzzle; 
    
    // Private Components
    private CharacterController controller;
    private Camera playerCamera;
    private Animator animator;

    // Private State
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private int equippedWeaponState = 0; // 0=none, 1=sword, 2=pistol
    private int justAttackedType = 0;    // 0=none, 1=sword, 2=pistol
    private bool justJumped = false;
    private float actionTimer;
    private float actionSendRate = 0.05f;
    private Vector3 lastFireDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>(); // This should already be here
        animator = GetComponentInChildren<Animator>();

        // Set default camera FOV
        playerCamera.fieldOfView = normalFOV; 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) 
        crosshairUI.SetActive(false); 
        
        UpdateWeaponVisibility(0); 
    }

    void Update()
    {
        HandleMovement();
        HandleWeaponInput();
        HandleAiming();
        HandleNetworkSend();
        HandleCameraLook();
    }

    void HandleMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        float curSpeedX = currentSpeed * Input.GetAxis("Vertical");
        float curSpeedY = currentSpeed * Input.GetAxis("Horizontal");
        float moveDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (controller.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
                animator.SetTrigger("Jump");
                justJumped = true;
            }
            else
            {
                moveDirection.y = 0;
            }
        }
        else
        {
            moveDirection.y = moveDirectionY;
            moveDirection.y -= gravity * Time.deltaTime;
        }
        controller.Move(moveDirection * Time.deltaTime);

        // --- UPDATE ANIMATOR ---
        if (animator != null)
        {
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
            animator.SetInteger("equippedWeapon", equippedWeaponState);
        }
    }

    void HandleWeaponInput()
    {
        // Equip Sword (Key 1)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            equippedWeaponState = (equippedWeaponState == 1) ? 0 : 1;
            UpdateWeaponVisibility(equippedWeaponState);

            if (crosshairUI != null)
            crosshairUI.SetActive(equippedWeaponState > 0);
        }
        // Equip Pistol (Key 2)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            equippedWeaponState = (equippedWeaponState == 2) ? 0 : 2;
            UpdateWeaponVisibility(equippedWeaponState);

            if (crosshairUI != null)
            crosshairUI.SetActive(equippedWeaponState > 0);
        }
        // Attack (Left Mouse)
        if (Input.GetMouseButtonDown(0))
        {
            if (equippedWeaponState == 1) // Sword
            {
                animator.SetTrigger("Attack_Sword");
                justAttackedType = 1;
            }
            else if (equippedWeaponState == 2) // Pistol
            {
                animator.SetTrigger("Attack_Weapon2");
                justAttackedType = 2;

                // --- NEW: Calculate True Aim Direction ---

                // 1. Find the target
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen
                RaycastHit hit;
                Vector3 targetPoint;
                if (Physics.Raycast(ray, out hit, 1000)) // 1000 is max distance
                {
                    // We hit something (a wall, another player, etc.)
                    targetPoint = hit.point;
                }
                else
                {
                    // We hit nothing (the sky)
                    targetPoint = ray.GetPoint(100); // 100m away
                }

                // 2. Find the direction from the muzzle to that target
                Vector3 direction = (targetPoint - pistolMuzzle.position).normalized;

                // 3. Spawn the bullet with the correct rotation
                Instantiate(bulletPrefab, pistolMuzzle.position, Quaternion.LookRotation(direction));

                // 4. Save this direction to send to the duplicate
                lastFireDirection = direction;
                // ------------------------------------------
            }
        }
    }

    void HandleAiming()
    {
        // Check for right-click hold
        bool isAimingInput = Input.GetMouseButton(1); 
        // We can only aim if the pistol is out
        bool canAim = (equippedWeaponState == 2); 
        bool isCurrentlyAiming = isAimingInput && canAim;

        // Tell the animator
        animator.SetBool("isAiming", isCurrentlyAiming);

        // Zoom the camera
        if (isCurrentlyAiming)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, aimFOV, Time.deltaTime * fovSmoothSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * fovSmoothSpeed);
        }
    }
    // ----------------------

    void HandleNetworkSend()
    {
        actionTimer += Time.deltaTime;
        if (actionTimer >= actionSendRate)
        {
            actionTimer = 0f;
            SendActionData();
        }
    }

    void HandleCameraLook()
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    void UpdateWeaponVisibility(int state)
    {
        if (swordObject != null)
            swordObject.SetActive(state == 1);
        if (weapon2Object != null)
            weapon2Object.SetActive(state == 2);
    }

    void SendActionData()
    {
        PlayerAction action = new PlayerAction();
        action.posX = transform.position.x;
        action.posY = transform.position.y;
        action.posZ = transform.position.z;
        action.rotY = transform.eulerAngles.y; 
        action.moveState = animator.GetInteger("moveState");
        action.didJump = justJumped;
        action.equippedWeapon = equippedWeaponState;
        action.attackType = justAttackedType;
        action.isAiming = animator.GetBool("isAiming");

        if (justAttackedType == 2)
        {
            action.fireDirection = lastFireDirection;
        }

        // Reset one-shot events
        justJumped = false; 
        justAttackedType = 0; 
        
        string json = JsonUtility.ToJson(action);
        networkClient.SendActionData("ACTION:" + json);
    }
}