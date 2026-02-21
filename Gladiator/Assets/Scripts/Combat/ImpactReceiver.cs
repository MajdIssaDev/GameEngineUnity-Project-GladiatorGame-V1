using UnityEngine;

public class ImpactReceiver : MonoBehaviour
{
    public float mass = 3.0f; 
    public float damping = 5f; // How fast the knockback wears off
    
    private Vector3 impact = Vector3.zero;
    private CharacterController character;

    void Start()
    {
        character = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (character == null || !character.enabled) return;

        if (impact.sqrMagnitude > 0.04f) // slightly cheaper than .magnitude
        {
            // 1. Calculate the intended move for this frame
            Vector3 intendedMove = impact * Time.deltaTime;

            // 2. ANTI-TUNNELING FIX: Clamp the movement step
            // Never let the character move further in one frame than its own radius
            float maxStep = character.radius * 0.9f; 
            if (intendedMove.magnitude > maxStep)
            {
                intendedMove = intendedMove.normalized * maxStep;
            }

            // 3. Move the character
            character.Move(intendedMove);
        }
        
        // 4. MATH FIX: Better friction
        // Lerp with Time.deltaTime isn't perfectly smooth across varying framerates.
        // This is a more consistent way to bleed off velocity.
        impact = Vector3.Lerp(impact, Vector3.zero, damping * Time.deltaTime);
    }

    public void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; 
        
        // Add the new force to the existing impact
        impact += dir * (force / mass);
    }
}