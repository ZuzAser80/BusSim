using UnityEngine;

public class FollowScript : MonoBehaviour 
{    
    public Vector3 leader;
    public float followSharpness = 0.1f;

    Vector3 _followOffset;

    void Start()
    {
        // Cache the initial offset at time of load/spawn:
        _followOffset = transform.position - leader;
    }

    void LateUpdate () 
    {
        // Apply that offset to get a target position.
        Vector3 targetPosition = leader + _followOffset;

        // Keep our y position unchanged.
        targetPosition.y = transform.position.y;

        // Smooth follow.    
        transform.position += (targetPosition - transform.position) * followSharpness;
    }
}