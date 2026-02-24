using UnityEngine;

public class ImpactReceiver : MonoBehaviour
{
    public float mass = 3.0f; 
    public float damping = 5f; //Controls the friction/drag that slows the character down after getting hit
    
    private Vector3 impact = Vector3.zero;
    private CharacterController character;

    void Start()
    {
        character = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (character == null || !character.enabled) return;

        //SqrMagnitude is computationally cheaper than .magnitude 
        if (impact.sqrMagnitude > 0.04f) 
        {
            //1. Calculate the intended move for this frame
            Vector3 intendedMove = impact * Time.deltaTime;

            //Clamp the knockback distance per frame to the character's radius
            float maxStep = character.radius * 0.9f; 
            if (intendedMove.magnitude > maxStep)
            {
                intendedMove = intendedMove.normalized * maxStep;
            }

            //3. Move the character
            character.Move(intendedMove);
        }
        
        //Apply drag/friction to smoothly bleed off the knockback velocity over time; pushback feels weighty and natural
        impact = Vector3.Lerp(impact, Vector3.zero, damping * Time.deltaTime);
    }

    public void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; 
        
        //Add the new force to the existing impact so heavier enemies are naturally pushed back
        impact += dir * (force / mass);
    }
}