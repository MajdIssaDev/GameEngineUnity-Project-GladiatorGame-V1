using UnityEngine;

public class ImpactReceiver : MonoBehaviour
{
    private float mass = 3.0f; // Heavier enemies need more force to move
    private Vector3 impact = Vector3.zero;
    private CharacterController character;

    void Start()
    {
        character = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- FIX: SAFETY CHECK ---
        // If the enemy is dead (controller disabled), stop trying to push it.
        if (character == null || !character.enabled) return;
        // -------------------------

        // Apply the impact force over time
        if (impact.magnitude > 0.2f)
        {
            character.Move(impact * Time.deltaTime);
        }
        
        // Consume the impact energy
        impact = Vector3.Lerp(impact, Vector3.zero, 5 * Time.deltaTime);
    }

    // Call this function from your Sword script
    public void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; // Reflect down force on the ground
        impact += dir.normalized * force / mass;
    }
}