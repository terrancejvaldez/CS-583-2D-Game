using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Camera mainCamera;
    public Vector3 originalScale;
    public Vector3 duckScale;

    private float currentTime;
    private float highScore;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // Reference to Rigidbody2D component
        boxCollider = GetComponent<BoxCollider2D>(); // Reference to BoxCollider2D
        mainCamera = Camera.main;  // Get a reference to the main camera

        // Store the original scale of the player sprite
        originalScale = transform.localScale;
        duckScale = new Vector3(originalScale.x, originalScale.y / 2, originalScale.z);

        // Load the high score from PlayerPrefs
        highScore = PlayerPrefs.GetFloat("HighScore", 0f);

        // Reset current time (stopwatch)
        currentTime = 0f;

        // Freeze rotation on the Z-axis to prevent the player from flipping
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Update the stopwatch (time the player is surviving)
        currentTime += Time.deltaTime;

        // Update the UI to display the current score and high score
        scoreText.text = "Score: " + currentTime.ToString("F2") + "s";
        highScoreText.text = "High Score: " + highScore.ToString("F2") + "s";

        // Handle Left-Right movement
        float move = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);

        // Restrict player movement to stay within the screen boundaries
        RestrictMovementToScreen();

        // Handle Jumping (Only when grounded and not pressing 'S')
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !Input.GetKey(KeyCode.S))
        {
            Debug.Log("W key pressed. Attempting to jump.");
            Jump();
        }

        // Handle Ducking (Shrink sprite Y scale when pressing 'S')
        if (Input.GetKey(KeyCode.S))
        {
            Debug.Log("S key pressed. Ducking.");
            Duck();
        }
        else
        {
            StandUp();
        }

        // Reset High Score when pressing 'R'
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHighScore();
        }
    }

    void Jump()
    {
        // Apply jump force
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        Debug.Log("Jump applied with force: " + jumpForce);
    }

    void Duck()
    {
        // Shrink only the sprite's Y scale (BoxCollider2D remains unchanged)
        transform.localScale = duckScale;
        Debug.Log("Ducking: Shrinking player scale to: " + duckScale);
    }

    void StandUp()
    {
        // Reset the sprite scale to its original size (BoxCollider2D remains unchanged)
        transform.localScale = originalScale;
        Debug.Log("Standing up: Resetting player scale to: " + originalScale);
    }

    bool IsGrounded()
    {
        float colliderBottomY = boxCollider.bounds.extents.y + 0.9f;  // Increase distance slightly
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, colliderBottomY, LayerMask.GetMask("Ground"));
        
        // Debugging: Print whether the player is grounded
        if (hit.collider != null)
        {
            Debug.Log("Player is grounded.");
            return true;
        }
        else
        {
            Debug.Log("Player is NOT grounded.");
            return false;
        }
    }

    // Method to restrict player movement within the screen bounds
    void RestrictMovementToScreen()
    {
        float screenLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float screenRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        Vector3 playerPosition = transform.position;
        playerPosition.x = Mathf.Clamp(playerPosition.x, screenLeft + boxCollider.bounds.extents.x, screenRight - boxCollider.bounds.extents.x);
        transform.position = playerPosition;

        Debug.Log("Player position clamped to screen bounds.");
    }

    // Game over logic: Reset the game when player is hit by an obstacle
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            SaveHighScore();
            Debug.Log("Collision with obstacle detected. Game resetting.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);  // Reload the current scene to reset the game
        }
    }

    // Save the high score when the player hits an obstacle
    void SaveHighScore()
    {
        if (currentTime > highScore)
        {
            highScore = currentTime;
            PlayerPrefs.SetFloat("HighScore", highScore);  // Save high score using PlayerPrefs
            PlayerPrefs.Save();  // Make sure the high score is saved

            Debug.Log("New high score saved: " + highScore);
        }
    }

    // Detect when the player collects a power-up
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("PowerUp"))
        {
            // Trigger the water blast in the obstacle manager
            FindObjectOfType<ObstacleManager>().TriggerWaterBlast();

            // Destroy the power-up
            Destroy(other.gameObject);
        }
    }
    void ResetHighScore()
    {
        PlayerPrefs.SetFloat("HighScore", 0f);  
        PlayerPrefs.Save();  
        highScore = 0f;  
        Debug.Log("High score reset.");
    }
}
