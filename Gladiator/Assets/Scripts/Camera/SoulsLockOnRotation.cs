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

        Vector3 dir = currentTarget.position - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        Vector3 euler = targetRot.eulerAngles;
        euler.x = ClampAngle(euler.x, minPitch, maxPitch);

        targetRot = Quaternion.Euler(euler);

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