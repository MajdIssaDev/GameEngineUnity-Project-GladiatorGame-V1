using UnityEngine;
using System.Collections.Generic;

public class HitReaction : MonoBehaviour
{
    [Header("Setup")]
    public List<Transform> bodyParts = new List<Transform>(); 

    [Header("Settings")]
    public bool isBlocking = false; 
    
    // Split forces so you can tune them (Side hits usually need less movement to look good)
    public float flinchForceForwardBack = 30f; 
    public float flinchForceLeftRight = 20f;   
    
    public float recoverySpeed = 10f;

    [Header("VFX")]
    public GameObject bloodVfxPrefab;
    public GameObject sparkVfxPrefab;

    // Internal tracking
    private Transform lastHitBone;
    private Quaternion currentFlinchRotation = Quaternion.identity;

    void LateUpdate()
    {
        // 1. Smoothly return rotation to zero
        currentFlinchRotation = Quaternion.Lerp(currentFlinchRotation, Quaternion.identity, Time.deltaTime * recoverySpeed);

        // 2. Apply the rotation to the bone
        if (lastHitBone != null && Quaternion.Angle(currentFlinchRotation, Quaternion.identity) > 0.1f)
        {
            lastHitBone.localRotation = lastHitBone.localRotation * currentFlinchRotation;
        }
    }

    public bool HandleHit(Vector3 hitPoint, Vector3 attackDirection)
    {
        if (isBlocking)
        {
            // Blocked
            if (sparkVfxPrefab) 
                Instantiate(sparkVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));
            return true;
        }
        else
        {
            // Hit
            if (bloodVfxPrefab) 
                Instantiate(bloodVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));

            Transform closestBone = GetClosestBone(hitPoint);
            
            if (closestBone != null)
            {
                lastHitBone = closestBone;
                CalculateDirectionalFlinch(attackDirection);
            }

            return false;
        }
    }

    void CalculateDirectionalFlinch(Vector3 attackDirection)
    {
        // 1. Convert Global Attack Direction to Local Direction relative to the victim
        // This tells us if the hit is coming from their Left, Right, Front, or Back
        Vector3 localAttackDir = transform.InverseTransformDirection(attackDirection);
        
        float xRot = 0; // Pitch (Forward/Back)
        float zRot = 0; // Roll (Left/Right)

        // --- FRONT / BACK LOGIC (X Axis) ---
        if (localAttackDir.z > 0) 
        {
            // Attack traveling same direction as player = Hit from BEHIND
            // Bend Forward (+X)
            xRot = flinchForceForwardBack; 
        }
        else 
        {
            // Attack traveling opposite to player = Hit from FRONT
            // Bend Backward (-X)
            xRot = -flinchForceForwardBack; 
        }

        // --- LEFT / RIGHT LOGIC (Z Axis) ---
        if (localAttackDir.x > 0)
        {
            // Attack coming from Right (+X) -> Push to Left
            // Bend Left (+Z)
            zRot = flinchForceLeftRight;
        }
        else
        {
            // Attack coming from Left (-X) -> Push to Right
            // Bend Right (-Z)
            zRot = -flinchForceLeftRight;
        }

        // Apply both rotations
        currentFlinchRotation = Quaternion.Euler(xRot, 0, zRot);
    }

    Transform GetClosestBone(Vector3 hitPoint)
    {
        Transform bestBone = null;
        float closestDistance = float.MaxValue;

        foreach (Transform bone in bodyParts)
        {
            if (bone == null) continue;
            float dist = Vector3.Distance(hitPoint, bone.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestBone = bone;
            }
        }
        return bestBone;
    }
}