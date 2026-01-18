using UnityEngine;
using System.Collections.Generic;

public class HitReaction : MonoBehaviour
{
    [Header("Setup")]
    public List<Transform> bodyParts = new List<Transform>(); 

    [Header("Settings")]
    public bool isBlocking = false; 
    public float flinchForceForwardBack = 30f; 
    public float flinchForceLeftRight = 20f;   
    
    [Header("Timing")]
    public float impactDuration = 0.15f; 
    public float recoverySpeed = 5f; 

    [Header("VFX")]
    public GameObject bloodVfxPrefab;
    public GameObject sparkVfxPrefab;
    public bool snapVfxToBone = true;

    [Header("Physics Settings")]
    [Tooltip("How many degrees to bend.")]
    public float flinchAngle = 45f;
    
    // Internal Data
    private class BoneFlinchState
    {
        public Quaternion currentOffset = Quaternion.identity; 
        public Quaternion targetOffset = Quaternion.identity;  
        public Quaternion startOffset = Quaternion.identity;   
        public float timer = 0f;
        public bool isImpactPhase = false;
    }

    private Dictionary<Transform, BoneFlinchState> activeFlinches = new Dictionary<Transform, BoneFlinchState>();

    void LateUpdate()
    {
        List<Transform> bonesToRemove = new List<Transform>();

        foreach (var kvp in activeFlinches)
        {
            Transform bone = kvp.Key;
            BoneFlinchState state = kvp.Value;

            if (bone == null) { bonesToRemove.Add(bone); continue; }

            // 1. Calculate Rotation
            if (state.isImpactPhase)
            {
                state.timer += Time.deltaTime;
                float t = state.timer / impactDuration;
                t = t * t * (3f - 2f * t); // Smooth step
                state.currentOffset = Quaternion.Lerp(state.startOffset, state.targetOffset, t);

                if (state.timer >= impactDuration) state.isImpactPhase = false;
            }
            else
            {
                state.currentOffset = Quaternion.Lerp(state.currentOffset, Quaternion.identity, Time.deltaTime * recoverySpeed);
                if (Quaternion.Angle(state.currentOffset, Quaternion.identity) < 0.1f)
                    bonesToRemove.Add(bone);
            }

            // 2. Apply Rotation
            bone.localRotation = bone.localRotation * state.currentOffset;
        }

        foreach (var b in bonesToRemove) activeFlinches.Remove(b);
    }

    // --- THIS IS THE FIX ---
    // We added 'Collider hitCollider' as the first argument
    public bool HandleHit(Collider hitCollider, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (isBlocking)
        {
            if (sparkVfxPrefab) Instantiate(sparkVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));
            return true;
        }

        // 1. Identify the Bone
        Transform hitBone = null;

        // A. Direct Match (Best for Hurtboxes)
        // If the collider we hit IS in our list of body parts, use it directly.
        if (bodyParts.Contains(hitCollider.transform))
        {
            hitBone = hitCollider.transform;
        }
        else
        {
            // B. Fallback (If we somehow hit a generic box, find closest bone)
            hitBone = GetClosestBone(hitPoint);
        }

        if (hitBone != null)
        {
            // 2. SPAWN VFX
            if (bloodVfxPrefab)
            {
                Vector3 spawnPos = hitPoint;
                Transform spawnParent = null;

                if (snapVfxToBone)
                {
                    spawnPos = hitBone.position;
                    spawnParent = hitBone;
                }

                GameObject vfx = Instantiate(bloodVfxPrefab, spawnPos, Quaternion.LookRotation(-attackDirection));
                if (spawnParent != null) vfx.transform.SetParent(spawnParent);
                Destroy(vfx, 1.0f);
            }

            // 3. FLINCH LOGIC
            if (!activeFlinches.ContainsKey(hitBone))
            {
                activeFlinches.Add(hitBone, new BoneFlinchState());
            }

            BoneFlinchState state = activeFlinches[hitBone];
            
            state.startOffset = state.currentOffset; 
            state.targetOffset = CalculateTargetRotation(hitBone, attackDirection);
            state.timer = 0f;
            state.isImpactPhase = true;
        }

        return false;
    }
    
    Quaternion CalculateTargetRotation(Transform bone, Vector3 attackDirection)
    {
        // 1. Force Direction: The direction the blow is travelling (World Space)
        Vector3 impactDir = attackDirection.normalized;

        // 2. Rotation Axis: Calculate the "Hinge" to rotate around (World Space)
        // We use the Cross Product of World-Up and the Impact Direction.
        // Example: If Impact is Forward (Z), Axis becomes Right (X).
        Vector3 worldRotationAxis = Vector3.Cross(Vector3.up, impactDir);

        // Safety: If hit strictly from above/below, use Right axis to prevent errors
        if (worldRotationAxis.sqrMagnitude < 0.01f) worldRotationAxis = Vector3.right;

        // 3. Convert that World Axis into the Bone's Local Space
        // This makes it work regardless of how the bone is currently twisted by animation
        Vector3 localRotationAxis = bone.InverseTransformDirection(worldRotationAxis);

        // 4. Create the rotation
        // We apply the angle around that specific calculated hinge
        return Quaternion.AngleAxis(flinchAngle, localRotationAxis);
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