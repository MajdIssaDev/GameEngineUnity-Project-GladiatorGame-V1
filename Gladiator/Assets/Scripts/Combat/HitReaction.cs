using UnityEngine;
using System.Collections.Generic;

public class HitReaction : MonoBehaviour
{
    [Header("References")]
    public Animator animator; // --- NEW: Added Animator Reference ---

    [Header("Setup")]
    public List<Transform> bodyParts = new List<Transform>(); 

    [Header("Settings")]
    public float flinchForceForwardBack = 30f; 
    public float flinchForceLeftRight = 20f;   
    
    [Header("Timing")]
    public float impactDuration = 0.15f; 
    public float recoverySpeed = 5f; 

    [Header("VFX")]
    public GameObject bloodVfxPrefab;
    public GameObject sparkVfxPrefab; 
    public GameObject parryVfxPrefab; 
    public bool snapVfxToBone = true;

    [Header("Physics Settings")]
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

    void Start()
    {
        // --- NEW: Auto-grab Animator if not assigned ---
        if (animator == null) animator = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        List<Transform> bonesToRemove = new List<Transform>();

        foreach (var kvp in activeFlinches)
        {
            Transform bone = kvp.Key;
            BoneFlinchState state = kvp.Value;

            if (bone == null) { bonesToRemove.Add(bone); continue; }

            if (state.isImpactPhase)
            {
                state.timer += Time.deltaTime;
                float t = state.timer / impactDuration;
                t = t * t * (3f - 2f * t); 
                state.currentOffset = Quaternion.Lerp(state.startOffset, state.targetOffset, t);
                if (state.timer >= impactDuration) state.isImpactPhase = false;
            }
            else
            {
                state.currentOffset = Quaternion.Lerp(state.currentOffset, Quaternion.identity, Time.deltaTime * recoverySpeed);
                if (Quaternion.Angle(state.currentOffset, Quaternion.identity) < 0.1f)
                    bonesToRemove.Add(bone);
            }
            bone.localRotation = bone.localRotation * state.currentOffset;
        }
        foreach (var b in bonesToRemove) activeFlinches.Remove(b);
    }

    // --- UPDATED VFX METHODS ---

    public void PlayBlockVFX(Vector3 hitPoint, Vector3 attackDirection)
    {
        if (sparkVfxPrefab) 
        {
            Instantiate(sparkVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));
        }

        // --- NEW: ANTI-QUEUING BLOCK ANIMATION LOGIC ---
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we are NOT already playing the block reaction.
            // NOTE: Change "BlockReaction" to the exact name of the state in your Animator!
            if (!stateInfo.IsName("BlockReaction")) 
            {
                animator.ResetTrigger("Blocked"); // Clear phantom triggers
                animator.SetTrigger("Blocked");   // Trigger the hit
            }
        }
    }

    public void PlayParryVFX(Vector3 hitPoint, Vector3 attackDirection)
    {
        if (parryVfxPrefab) 
        {
            Instantiate(parryVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));
        }
        else if (sparkVfxPrefab)
        {
            Instantiate(sparkVfxPrefab, hitPoint, Quaternion.LookRotation(-attackDirection));
        }

        // --- NEW: FORCE PARRY ANIMATION LOGIC ---
        if (animator != null)
        {
            animator.ResetTrigger("Blocked");
            animator.SetTrigger("Blocked");
        }
    }

    // --- STANDARD HIT LOGIC ---
    public void HandleHit(Collider hitCollider, Vector3 hitPoint, Vector3 attackDirection)
    {
        // 1. Identify the Bone
        Transform hitBone = null;
        if (bodyParts.Contains(hitCollider.transform)) hitBone = hitCollider.transform;
        else hitBone = GetClosestBone(hitPoint);

        if (hitBone != null)
        {
            // 2. SPAWN BLOOD VFX
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
        
    }
    
    Quaternion CalculateTargetRotation(Transform bone, Vector3 attackDirection)
    {
        Vector3 impactDir = attackDirection.normalized;
        Vector3 worldRotationAxis = Vector3.Cross(Vector3.up, impactDir);
        if (worldRotationAxis.sqrMagnitude < 0.01f) worldRotationAxis = Vector3.right;
        Vector3 localRotationAxis = bone.InverseTransformDirection(worldRotationAxis);
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