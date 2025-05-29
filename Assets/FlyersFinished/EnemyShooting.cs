using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
{
    public GameObject bulletPrefab;        // Prefab for the bullet
    public Transform gunTip;             // Transform representing the tip of the gun
    public Transform player;             // Reference to the player's transform
    public float fireRate = 2f;           // Time between shots.  Higher = more shots per second
    [Range(0, 100)]
    public float accuracyPercent = 80f;         // Accuracy as a percentage (0-100)
    public float accuracyIncreaseRate = 0.1f; // Rate at which accuracy increases per second
    public float detectionDistance = 50f;  // Max distance to detect player
    public LayerMask obstacleLayers;       // Layers that block the line of sight
    private float nextFireTime;         // Time when the next shot can be fired
    private bool canSeePlayer;           // Flag indicating if the player is visible
    public float bulletSpeed = 20f;       // Speed of the bullet
    public LayerMask hitLayers;           // Layers the bullet can collide with
    private float initialAccuracyPercent;
    private float initialFireRate;
    public float coneAngle = 30f;
    public float maxDeviationAngle = 60f;

    void Start()
    {
        nextFireTime = Time.time;
        initialAccuracyPercent = accuracyPercent;
        initialFireRate = fireRate;
        // Find the player.  Important to do this in Start() or Awake()
        //  and keep a reference, rather than searching every frame.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player == null)
        {
            UnityEngine.Debug.LogError("Player not found!  Make sure the player is tagged with 'Player'.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (player == null) return;

        canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            accuracyPercent = Mathf.Clamp(accuracyPercent + accuracyIncreaseRate * Time.deltaTime, 0f, 100f);

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + (1f / fireRate);
            }
        }
        else
        {
            accuracyPercent = initialAccuracyPercent;
            fireRate = initialFireRate; // Reset fireRate when player is not seen
        }
    }

    bool CanSeePlayer()
    {
        // Calculate the direction to the player
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Check if the player is within the detection distance
        if (distanceToPlayer > detectionDistance)
        {
            return false; // Player is too far away
        }

        // Perform a raycast to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, distanceToPlayer, obstacleLayers))
        {
            if (hit.transform != player)
            {
                return false; // Something is blocking the view
            }
        }

        return true; // Player is visible
    }

    void Shoot()
    {
        // Calculate the base direction to the player
        Vector3 directionToPlayer = player.position - gunTip.position;

        Quaternion finalDirection;
        // Apply accuracy: Only deviate if the bullet will not hit the player.
        RaycastHit hit;
        if (Physics.Raycast(gunTip.position, directionToPlayer.normalized, out hit, 100f))
        {
            if (hit.transform != player)
            {
                // Calculate the cone rotation *always*, and then apply deviation.
                Quaternion coneRotation = Quaternion.Euler(
                    UnityEngine.Random.Range(-coneAngle / 2, coneAngle / 2),  // Divide by 2 to center the spread
                    UnityEngine.Random.Range(-coneAngle / 2, coneAngle / 2),
                    0f
                );
                Vector3 deviatedDirection = coneRotation * directionToPlayer;

                // Calculate a point further away from the player
                Vector3 playerDirection = player.position - gunTip.position;
                Vector3 farPoint = player.position + playerDirection.normalized * 100f;

                float randomAngle = UnityEngine.Random.Range(coneAngle, maxDeviationAngle);
                Quaternion furtherRotation = Quaternion.AngleAxis(randomAngle, Vector3.Cross(gunTip.position - player.position, deviatedDirection));
                deviatedDirection = furtherRotation * deviatedDirection;

                finalDirection = Quaternion.LookRotation(deviatedDirection); // Apply cone rotation
            }
            else
            {
                finalDirection = Quaternion.LookRotation(directionToPlayer);
            }
        }
        else
        {
            finalDirection = Quaternion.LookRotation(directionToPlayer);
        }


        // Instantiate the bullet
        GameObject bulletObject = Instantiate(bulletPrefab, gunTip.position, finalDirection);
        Collider bulletCollider = bulletObject.GetComponent<Collider>();
        if (bulletCollider == null)
        {
            UnityEngine.Debug.LogError("Bullet prefab is missing a Collider component!");
        }
        BulletMovement bulletMovement = bulletObject.AddComponent<BulletMovement>();
        bulletMovement.direction = finalDirection * Vector3.forward;  // Use the calculated direction
        bulletMovement.speed = bulletSpeed;
        bulletMovement.hitLayers = hitLayers;
        bulletMovement.playerTransform = player; // Pass the player transform
        bulletMovement.gunTipTransform = gunTip; // Pass the gunTip transform



        // Add this code to check if the bullet will hit the player:
        RaycastHit hitInfo;
        if (Physics.Raycast(gunTip.position, finalDirection * Vector3.forward, out hitInfo, 100f))
        {
            if (hitInfo.transform == player)
            {
                Renderer[] renderers = bulletObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.material.color = Color.red;
                }
            }
        }
        Destroy(bulletObject, 5f);

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }


}


public class BulletMovement : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    public LayerMask hitLayers;
    public Transform playerTransform; // Store the player's transform
    public Transform gunTipTransform; // Store the gunTip's transform

    private bool hasPassedPlayer = false;
    private float distanceToPlayerLastFrame;
    private float maxDistance = 100f; // Add a max distance for the bullet
    private Transform playerCameraTransform; // Store the player's camera transform
    private bool isPlayerTargeted = false; // Flag to track if this bullet is heading towards the player

    void Start()
    {
        if (playerTransform == null || gunTipTransform == null)
        {
            Destroy(gameObject); // Safety check
            return;
        }
        distanceToPlayerLastFrame = Vector3.Distance(transform.position, playerTransform.position);
        // Get the player's camera transform.  Since the player *is* the camera, use the player's transform.
        playerCameraTransform = playerTransform;


        // Check if this bullet is on a path to hit the player in the first 10 units of travel
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, 10f, hitLayers)) //shortened distance
        {
            if (hit.transform == playerTransform)
            {
                isPlayerTargeted = true;
            }
        }
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        // Check for collisions using Raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, Time.deltaTime * speed, hitLayers))
        {
            // Handle collision
            if (hit.transform.gameObject.tag == "Player")
            {
                // Deal damage to the player.
                // You can get the player health script from hit.transform.gameObject.GetComponent<PlayerHealth>();
                // e.g.,  PlayerHealth playerHealth = hit.transform.gameObject.GetComponent<PlayerHealth>();
                UnityEngine.Debug.Log("Hit Player!");
            }
            Destroy(gameObject); // Destroy the bullet on collision
        }

        // Destroy if passed player
        if (playerTransform != null && gunTipTransform != null)
        {
            float distanceToPlayerThisFrame = Vector3.Distance(transform.position, playerTransform.position);

            if (!hasPassedPlayer)
            {
                if (distanceToPlayerThisFrame < distanceToPlayerLastFrame)
                {
                    hasPassedPlayer = true;
                }
            }
            else if (distanceToPlayerThisFrame > distanceToPlayerLastFrame)
            {
                Destroy(gameObject);
            }

            distanceToPlayerLastFrame = distanceToPlayerThisFrame;
        }
        else
        {
            Destroy(gameObject); //Destroy if the player is null
        }

        // Destroy bullet if it travels too far
        if (Vector3.Distance(transform.position, gunTipTransform.position) > maxDistance)
        {
            Destroy(gameObject);
        }

        // Destroy bullet if it gets too close to the player's camera and is not heading towards player.
        if (playerCameraTransform != null)
        {
            if (Vector3.Distance(transform.position, playerCameraTransform.position) < 1.0f && !isPlayerTargeted) // Adjust the distance as needed
            {
                Destroy(gameObject);
            }
        }
    }
}
