using UnityEngine;

public class SingerBehaviour : MonoBehaviour
{
    private GameObject owner;
    private float followDistance;
    private float floatHeight;
    private float followSpeed = 3f;
    private float rotateSpeed = 2f;

    public void Initialize(GameObject caster, float distance, float height)
    {
        owner = caster;
        followDistance = distance;
        floatHeight = height;
    }

    void Update()
    {
        if (owner == null) return;

        FollowOwner();
        FloatEffect();
        LookAtOwner();
    }

    private void FollowOwner()
    {
        // Calculate target position behind the player
        Vector3 targetPosition = owner.transform.position - owner.transform.forward * followDistance;
        targetPosition.y = owner.transform.position.y + floatHeight;

        // Smoothly move to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
    }

    private void FloatEffect()
    {
        // Add bobbing effect
        float bobOffset = Mathf.Sin(Time.time * 2f) * 0.15f;
        transform.position += new Vector3(0, bobOffset * Time.deltaTime, 0);
    }

    private void LookAtOwner()
    {
        // Slowly rotate towards the owner
        Vector3 directionToOwner = owner.transform.position - transform.position;
        directionToOwner.y = 0;

        if (directionToOwner != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToOwner);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }

        // Add gentle spin effect
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
    }
}