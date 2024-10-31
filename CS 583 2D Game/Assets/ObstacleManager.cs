using System.Collections;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject obby1Prefab;
    public GameObject obby2Prefab;
    public GameObject obby3Prefab;
    public GameObject obby4Prefab;
    public GameObject obby5Prefab;
    public GameObject powerUpPrefab;  // New power-up prefab
    public GameObject warningIndicatorPrefab;
    public GameObject ashPrefab;

    // Audio clips for each Obby
    public AudioClip fire1;
    public AudioClip fire2;
    public AudioClip fire3;
    public AudioClip fire4;
    public AudioClip fire5;
    public AudioClip waterSound;  // Sound to play when power-up is collected
    public AudioClip extinguishSound;  // Sound to play when fire is extinguished
    
    private AudioSource audioSource;  // Audio source to play the sound

    public float minSpawnTime = 2f;
    public float maxSpawnTime = 5f;
    public float obbySpeed = 5f;
    public float pendulumSpeed = 2f;  // Speed for Obby5 (pendulum)
    public float pendulumAmplitude = 4f;  // Amplitude (range) of the pendulum motion
    public float warningDuration = 1.5f;
    public float blinkInterval = 0.2f;
    public float obby3IndicatorHeightOffset = 1.5f;
    public float powerUpSpawnRate = 10f;  // Time between power-up spawns
    private float powerUpElapsedTime = 0f;  // Tracks time since last power-up spawn
    private float screenRightEdge;
    private float screenLeftEdge;
    private float screenBottomEdge;
    private float screenTopEdge;
    private GameObject player;
    // Reference to the sky background sprite
    public GameObject skyBackground; 
    private int currentObbies = 1;  // Tracks how many obbies can spawn
    private float elapsedTime = 0f;  // Tracks elapsed time since game started
    private float spawnAccelerationTime = 5f;  // Time interval to increase difficulty

    void Start()
    {
        // Get screen boundaries based on the camera's view
        screenRightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
        screenLeftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        screenBottomEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
        screenTopEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0)).y;

        // Find the player object
        player = GameObject.FindGameObjectWithTag("Player");

        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Start spawning obstacles
        StartCoroutine(SpawnObstacles());
    }

    IEnumerator SpawnObstacles()
    {
        while (true)
        {
            float spawnDelay = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(spawnDelay);

            int obstacleType = Random.Range(1, currentObbies + 1);

            if (obstacleType == 1) SpawnObby1();
            else if (obstacleType == 2) SpawnObby2();
            else if (obstacleType == 3) StartCoroutine(SpawnObbyWithWarning(obby3Prefab, screenBottomEdge, true));
            else if (obstacleType == 4) StartCoroutine(SpawnObbyWithWarning(obby4Prefab, Random.Range(screenBottomEdge, screenTopEdge), false));
            else if (obstacleType == 5) SpawnObby5();  // Pendulum Obby5

            elapsedTime += spawnDelay;

            if (elapsedTime >= spawnAccelerationTime)
            {
                IncreaseDifficulty();
                elapsedTime = 0f;  
            }

            powerUpElapsedTime += spawnDelay;
            if (powerUpElapsedTime >= powerUpSpawnRate)
            {
                SpawnPowerUp();
                powerUpElapsedTime = 0f;
            }
        }
    }
    void IncreaseDifficulty()
    {
        // If fewer than 5 obbies are spawning, introduce a new one
        if (currentObbies < 5)
        {
            currentObbies++;
        }
        else
        {
            // Once all obbies are introduced, increase spawn speed
            minSpawnTime = Mathf.Max(0.5f, minSpawnTime - 0.2f);
            maxSpawnTime = Mathf.Max(1f, maxSpawnTime - 0.2f);
        }
        Debug.Log("Difficulty increased: " + currentObbies + " obbies, spawn time: " + minSpawnTime + " - " + maxSpawnTime);
    }

    // ----- Spawning Methods -----

    void SpawnObby1()
    {
        bool moveLeft = Random.value > 0.5f;

        // Spawn the obstacle at a random height in the lower half of the screen
        float spawnY = Random.Range(screenBottomEdge, screenTopEdge * 0.5f);
        Vector2 spawnPosition = moveLeft ? new Vector2(screenRightEdge + 1f, spawnY) : new Vector2(screenLeftEdge - 1f, spawnY);

        GameObject obby1 = Instantiate(obby1Prefab, spawnPosition, Quaternion.identity);

        Rigidbody2D rb = obby1.GetComponent<Rigidbody2D>();
        rb.velocity = moveLeft ? Vector2.left * obbySpeed : Vector2.right * obbySpeed;

        // Play fire1 sound for Obby1
        PlayFireSound(fire1);

        // Destroy the obstacle after it moves off-screen
        StartCoroutine(DestroyIfOffScreen(obby1));
    }

    void SpawnObby2()
    {
        // Spawn Obby2 (falling obstacle) from above the screen
        float spawnX = Random.Range(screenLeftEdge, screenRightEdge); // Random X position within screen bounds
        float spawnY = screenTopEdge + 1f;  // Spawning above the screen to ensure it drops from the top
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);

        // Instantiate the Obby2 object at the spawn position
        GameObject obby2 = Instantiate(obby2Prefab, spawnPosition, Quaternion.identity);

        // Get the Rigidbody2D component of Obby2
        Rigidbody2D rb = obby2.GetComponent<Rigidbody2D>();

        // Apply a downward velocity so it moves from top to bottom
        rb.velocity = Vector2.down * obbySpeed;

        // Play fire2 sound for Obby2
        PlayFireSound(fire2);

        // Destroy the obstacle after 10 seconds to avoid memory issues (in case it never hits the ground)
        Destroy(obby2, 10f);
    }

    void SpawnObby3()
    {
        float spawnX = Random.Range(screenLeftEdge, screenRightEdge);
        Vector2 spawnPosition = new Vector2(spawnX, Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).y - 1f);

        GameObject obby3 = Instantiate(obby3Prefab, spawnPosition, Quaternion.identity);

        Rigidbody2D rb = obby3.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.up * obbySpeed;

        // Play fire3 sound for Obby3
        PlayFireSound(fire3);

        // Destroy the obstacle after it moves off-screen
        StartCoroutine(DestroyIfOffScreen(obby3));
    }

    void SpawnObby4()
    {
        float spawnX = Random.Range(screenLeftEdge, screenRightEdge);
        float spawnY = Random.Range(screenBottomEdge, screenTopEdge);  // Obby4 can spawn anywhere
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);

        GameObject obby4 = Instantiate(obby4Prefab, spawnPosition, Quaternion.identity);

        StartCoroutine(FollowPlayer(obby4));

        // Play fire4 sound for Obby4
        PlayFireSound(fire4);

        // Destroy Obby4 after it moves off-screen
        StartCoroutine(DestroyIfOffScreen(obby4));
    }
    void SpawnObby5()
    {
        // Set the starting position at the top left of the screen
        Vector2 startPosition = new Vector2(screenLeftEdge + 1f, screenTopEdge - 1f);

        // Instantiate Obby5 at the starting position
        GameObject obby5 = Instantiate(obby5Prefab, startPosition, Quaternion.identity);

        // Start the pendulum motion for Obby5
        StartCoroutine(PendulumMotion(obby5));

        // Play fire5 sound for Obby5
        PlayFireSound(fire5);

        // Destroy after a while to avoid lingering off-screen obstacles
        Destroy(obby5, 15f);  // Adjust if needed
    }

    IEnumerator PendulumMotion(GameObject obby5)
    {
        float time = 0f;

        while (obby5 != null)
        {
            // Pendulum motion using sine wave for smooth back and forth movement
            float x = Mathf.Lerp(screenLeftEdge + 1f, screenRightEdge - 1f, Mathf.Sin(time * pendulumSpeed) * 0.5f + 0.5f);  // Move from left to right
            float y = screenTopEdge - Mathf.Abs(Mathf.Sin(time * pendulumSpeed) * pendulumAmplitude);  // Swing downwards (making a half-circle)
            
            obby5.transform.position = new Vector2(x, y);

            time += Time.deltaTime;

            yield return null;
        }
    }

    void SpawnPowerUp()
    {
        float spawnX = Random.Range(screenLeftEdge, screenRightEdge);
        float spawnY = screenBottomEdge + 1f;
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);

        GameObject powerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);

        // Start the wandering behavior and blinking before despawn
        StartCoroutine(WanderAndBlinkPowerUp(powerUp));

        Destroy(powerUp, 10f);  // Destroy after 10 seconds if not collected
    }

    // Power-Up Wandering and Blinking
    IEnumerator WanderAndBlinkPowerUp(GameObject powerUp)
    {
        Rigidbody2D rb = powerUp.GetComponent<Rigidbody2D>();
        float wanderSpeed = 2f;
        float blinkDuration = 1.5f;
        SpriteRenderer powerUpRenderer = powerUp.GetComponent<SpriteRenderer>();

        while (powerUp != null)
        {
            // Randomly choose direction: left or right
            float direction = Random.value > 0.5f ? 1 : -1;
            rb.velocity = new Vector2(direction * wanderSpeed, rb.velocity.y);

            // Clamp the power-up's X position to stay within screen bounds
            float clampedX = Mathf.Clamp(powerUp.transform.position.x, screenLeftEdge + 0.5f, screenRightEdge - 0.5f);
            powerUp.transform.position = new Vector2(clampedX, powerUp.transform.position.y);

            yield return new WaitForSeconds(1f);

            // Start blinking before despawn
            if (Time.timeSinceLevelLoad > (powerUpSpawnRate - blinkDuration))
            {
                StartCoroutine(BlinkPowerUp(powerUpRenderer));
            }
        }
    }


    // Blinking effect for the power-up before despawning
    IEnumerator BlinkPowerUp(SpriteRenderer powerUpRenderer)
    {
        float blinkTime = 0f;
        while (blinkTime < 1.5f)
        {
            powerUpRenderer.enabled = !powerUpRenderer.enabled;  // Toggle visibility
            yield return new WaitForSeconds(blinkInterval);
            blinkTime += blinkInterval;
        }
    }

    // This method is called when the player collects the power-up
    public void TriggerWaterBlast()
    {
        // Play water sound for the power-up collection
        PlayWaterSound(waterSound);

        // Flash the sky background blue
        StartCoroutine(FlashSkyBackground());

        // Transform fire obstacles to ash and play extinguishing sound
        DestroyAllObstaclesWithAsh();
    }

    // Transform obstacles into ash when water blast happens
    void DestroyAllObstaclesWithAsh()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            // Instantiate ash at the obstacle's position
            Vector2 obstaclePosition = obstacle.transform.position;
            GameObject ash = Instantiate(ashPrefab, obstaclePosition, Quaternion.identity);

            // Play extinguishing sound
            PlayWaterSound(extinguishSound);

            // Destroy the fire obstacle
            Destroy(obstacle);

            // Destroy the ash after a short delay
            Destroy(ash, 2f);
        }
    }

    // ----- Flash Sky Background with Fade -----
    IEnumerator FlashSkyBackground()
    {
        SpriteRenderer skyRenderer = skyBackground.GetComponent<SpriteRenderer>();
        Color originalColor = skyRenderer.color;
        Color targetColor = Color.blue;
        float duration = 1.5f;
        float timeElapsed = 0f;

        // Fade to blue
        while (timeElapsed < duration)
        {
            skyRenderer.color = Color.Lerp(originalColor, targetColor, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        skyRenderer.color = targetColor;  // Ensure it's fully blue

        yield return new WaitForSeconds(0.1f);  // Keep it blue for 0.1 seconds

        // Fade back to the original color
        timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            skyRenderer.color = Color.Lerp(targetColor, originalColor, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        skyRenderer.color = originalColor;  // Reset to the original color
    }

    // Play water-related sound (extinguishing sound or power-up sound)
    void PlayWaterSound(AudioClip sound)
    {
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }
    }

    // ----- Play Fire Sound -----
    void PlayFireSound(AudioClip fireSound)
    {
        if (audioSource != null && fireSound != null)
        {
            // Play the sound assigned to the respective slot
            audioSource.PlayOneShot(fireSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or fireSound is missing!");
        }
    }

    // Coroutine to destroy an obstacle if it moves off-screen
    IEnumerator DestroyIfOffScreen(GameObject obstacle)
    {
        while (obstacle != null)
        {
            Vector2 position = obstacle.transform.position;

            // Check if the obstacle is outside the screen bounds
            if (position.x < screenLeftEdge - 1f || position.x > screenRightEdge + 1f || position.y < screenBottomEdge - 1f || position.y > screenTopEdge + 1f)
            {
                Destroy(obstacle);
                yield break;
            }

            yield return null;
        }
    }

    // ----- Spawn With Warning -----

    IEnumerator SpawnObbyWithWarning(GameObject obbyPrefab, float spawnY, bool isObby3)
    {
        float spawnX = Random.Range(screenLeftEdge, screenRightEdge);
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);

        if (isObby3)
        {
            spawnPosition.y += obby3IndicatorHeightOffset;
        }

        GameObject warningIndicator = Instantiate(warningIndicatorPrefab, spawnPosition, Quaternion.identity);
        SpriteRenderer indicatorRenderer = warningIndicator.GetComponent<SpriteRenderer>();

        float elapsedTime = 0f;
        while (elapsedTime < warningDuration)
        {
            indicatorRenderer.enabled = !indicatorRenderer.enabled;  // Toggle visibility
            yield return new WaitForSeconds(blinkInterval);  // Wait for blink interval
            elapsedTime += blinkInterval;
        }

        Destroy(warningIndicator);

        GameObject obby = Instantiate(obbyPrefab, spawnPosition, Quaternion.identity);

        if (obbyPrefab == obby3Prefab)
        {
            Rigidbody2D rb = obby.GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.up * obbySpeed;
        }
        else if (obbyPrefab == obby4Prefab)
        {
            StartCoroutine(FollowPlayer(obby));
        }

        // Destroy the obstacle after 10 seconds if not already destroyed
        Destroy(obby, 10f);
    }

    // ----- Follow Player -----
    IEnumerator FollowPlayer(GameObject obby4)
    {
        float speed = 3f;
        while (obby4 != null)
        {
            if (player != null)
            {
                Vector2 direction = (player.transform.position - obby4.transform.position).normalized;
                obby4.transform.position += (Vector3)(direction * speed * Time.deltaTime);
            }
            yield return null;
        }
    }
}
