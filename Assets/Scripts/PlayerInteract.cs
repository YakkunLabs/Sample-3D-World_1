using UnityEngine;
using TMPro; // We need this for the TextMeshPro UI element

public class PlayerInteract : MonoBehaviour
{
    // Assign these in the Inspector
    public GameObject screenUiPanel; 
    public GameObject interactPromptText; // The "Press E" text
    public Transform playerBody;
    
    public float interactDistance = 3f;

    private bool isUiOpen = false;
    private bool canInteract = false; // Is the player looking at the screen?
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        
        // Hide both UI elements at the start
        if (screenUiPanel != null) screenUiPanel.SetActive(false);
        if (interactPromptText != null) interactPromptText.SetActive(false);
    }

    void Update()
    {
        // If the main UI is open, we only check for the "Escape" key
        if (isUiOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleUI(false);
            }
            return; // Stop here if UI is open
        }

        // --- If UI is closed, do the raycast logic ---

        // Cast a ray forward from the camera
        RaycastHit hit;
        if (Physics.Raycast(playerBody.position, transform.forward, out hit, interactDistance))
        {
            // Check if we hit the screen
            if (hit.collider.CompareTag("InteractableScreen"))
            {
                // We are looking at the screen
                if (!canInteract) // Only set active if it's not already
                {
                    interactPromptText.SetActive(true);
                    canInteract = true;
                }

                // Now, check if we press E *while* looking
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ToggleUI(true);
                }
            }
            else
            {
                // We are looking at something else
                HideInteractPrompt();
            }
        }
        else
        {
            // We are looking at nothing (or something too far away)
            HideInteractPrompt();
        }
    }

    void HideInteractPrompt()
    {
        if (canInteract)
        {
            interactPromptText.SetActive(false);
            canInteract = false;
        }
    }

    void ToggleUI(bool show)
    {
        isUiOpen = show;
        screenUiPanel.SetActive(show);

        // When we open the panel, hide the "Press E" prompt
        if (show)
        {
            interactPromptText.SetActive(false);
            canInteract = false; // Ensure this is reset
        }

        // Lock/Unlock cursor and stop/start player look
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerMovement != null) playerMovement.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerMovement != null) playerMovement.enabled = true;
        }
    }
}