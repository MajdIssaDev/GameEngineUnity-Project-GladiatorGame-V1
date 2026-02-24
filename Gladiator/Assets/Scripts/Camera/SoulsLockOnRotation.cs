using UnityEngine;
using Unity.Cinemachine;

public class SoulsLockOnRotation : MonoBehaviour
{
    public Transform currentTarget;
    public float rotationSpeed = 10f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    void LateUpdate()
    {
        if (currentTarget == null) return;

        //Use LateUpdate for camera/rotation logic so it happens after all player and enemy movements, preventing jitter
        Vector3 dir = currentTarget.position - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        //Clamp the vertical look angle so the camera doesn't flip upside down if the target gets too high or too low
        Vector3 euler = targetRot.eulerAngles;
        euler.x = ClampAngle(euler.x, minPitch, maxPitch);

        targetRot = Quaternion.Euler(euler);

        //Smoothly rotate towards the target using Slerp instead of snapping instantly, making the lock-on feel natural
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}