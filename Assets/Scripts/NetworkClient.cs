using UnityEngine;
using UnityEngine.UI; // For the Button
using TMPro; // For InputField and Text
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks; // For asynchronous tasks

public class NetworkClient : MonoBehaviour
{
    // --- Assign these in the Inspector ---
    public TMP_InputField messageInput;
    public Button sendButton;
    public TMP_Text responseText;
    // ------------------------------------

    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;

    // --- NEW: For Action Data ---
    public DuplicateController duplicateController; // Assign in Inspector
    private volatile PlayerAction actionToApply = null;
    // ----------------------------

    // A thread-safe way to pass the received message back to the main thread
    private volatile string messageToDisplay = null;

    // Try to connect as soon as the game starts
    async void Start()
    {
        // Add a listener to the button so it calls our function when clicked
        sendButton.onClick.AddListener(OnSendButtonClick);

        await ConnectToServer();
    }

    async Task ConnectToServer()
    {
        try
        {
            // Create a new client and connect to the Rust server
            client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 8080); // 127.0.0.1 means "this computer"

            if (client.Connected)
            {
                stream = client.GetStream();
                isConnected = true;
                Debug.Log("Connected to Rust server!");
                responseText.text = "Connected!";

                // Start a background task to listen for data
                // We don't 'await' this, so it runs in the background
                _ = ListenForData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
            responseText.text = "Error: Failed to connect to server.";
        }
    }

    // This runs on a background thread
    private async Task ListenForData()
    {
        byte[] buffer = new byte[4096]; // Increased buffer size

        try
        {
            while (isConnected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    isConnected = false;
                    messageToDisplay = "Server disconnected.";
                    break;
                }

                string receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // --- NEW: Check message type ---
                if (receivedMessage.StartsWith("ACTION:"))
                {
                    // This is an action. Get the JSON part.
                    string json = receivedMessage.Substring(7);
                    // Parse it and store it for the main thread
                    actionToApply = JsonUtility.FromJson<PlayerAction>(json);
                }
                else
                {
                    // This is a chat message
                    messageToDisplay = $"Server Echo: {receivedMessage}";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Network read error: {e.Message}");
            messageToDisplay = "Connection lost.";
            isConnected = false;
        }
        finally
        {
            client.Close();
        }
    }

    // This is called when the UI Send Button is clicked
    private void OnSendButtonClick()
    {
        if (!isConnected)
        {
            responseText.text = "Not connected to server.";
            return;
        }

        string message = messageInput.text;
        if (string.IsNullOrEmpty(message)) return;

        SendMessageToServer(message); // Just call the renamed function
        messageInput.text = ""; // Clear the input field
    }

    // This runs every frame on the main Unity thread
    void Update()
    {
        // --- This part is NEW ---
        if (actionToApply != null)
        {
            duplicateController.UpdateState(actionToApply);
            actionToApply = null; // We've used it, so clear it
        }
        // -------------------------

        // This part was already here
        if (messageToDisplay != null)
        {
            responseText.text = messageToDisplay;
            messageToDisplay = null;
        }
    }

    // Clean up when the game closes
    void OnApplicationQuit()
    {
        if (client != null)
        {
            isConnected = false;
            stream?.Close();
            client.Close();
        }
    }

    private async void SendMessageToServer(string message)
    {
        if (!isConnected || stream == null) return;

        try
        {
            // Convert our string message to a byte array
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            // Send the data over the stream
            await stream.WriteAsync(data, 0, data.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send data: {e.Message}");
        }
    }
                public void SendActionData(string message)
        {
            SendMessageToServer(message);
        }
}
