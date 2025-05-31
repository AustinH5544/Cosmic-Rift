// BossHP.cs
using System.Diagnostics;
using UnityEngine;

public class BossHP : MonoBehaviour
{
    public int bossHP = 10;
    public float rotationSpeed = 50f; // Degrees per second

    void Update()
    {
        // Rotate the boss around its local X-axis
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    // Call this method to deal damage to the boss
    public void TakeDamage(int damageAmount)
    {
        bossHP -= damageAmount;
        UnityEngine.Debug.Log("Boss HP: " + bossHP);

        if (bossHP <= 0)
        {
            UnityEngine.Debug.Log("Boss Defeated!");
            // Add any boss defeated logic here, e.g., play animation, load new scene, etc.

            // Destroy the parent object
            if (transform.parent != null)
            {
                Destroy(transform.parent.gameObject);
            }
            else
            {
                Destroy(gameObject); // If no parent, destroy this game object
            }
        }
    }
}