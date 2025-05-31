// DamageBossOnDestroy.cs (Connects to the modified ChildRespawner)
using System.Diagnostics;
using UnityEngine;

public class DamageBossOnDestroy : MonoBehaviour
{
    private BossHP bossHP;
    private ChildRespawner childRespawner;

    void Start()
    {
        GameObject bossObject = GameObject.FindWithTag("Boss");
        if (bossObject != null)
        {
            bossHP = bossObject.GetComponent<BossHP>();
            if (bossHP == null)
            {
               UnityEngine.Debug.LogError("BossHP script not found on the Boss object.");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Boss object with 'Boss' tag not found in the scene.");
        }

        childRespawner = GetComponentInParent<ChildRespawner>();
        if (childRespawner == null)
        {
            UnityEngine.Debug.LogError("ChildRespawner script not found on the parent of " + gameObject.name + ". Ensure this object is a child of a GameObject with ChildRespawner.");
        }
    }

    void OnDestroy()
    {
        if (bossHP != null)
        {
            bossHP.TakeDamage(1);
        }
        else
        {
            UnityEngine.Debug.LogWarning("BossHP not found. Cannot deal damage.");
        }

        if (childRespawner != null)
        {
            // Call the new method to initiate the respawn cycle
            childRespawner.InitiateRespawnCycle();
        }
        else
        {
            UnityEngine.Debug.LogWarning("ChildRespawner not found. Cannot initiate respawn.");
        }
    }
}