using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPCHeadLookAt : MonoBehaviour
{
    [SerializeField] private Rig rig;
    [SerializeField] private Transform headLookAtTarget;
    [SerializeField] private float lerpSpeed = 2f;



    [Header("Head Follow Distance")]
    public float followDistance = 10f;

    private bool isLooking;
    private Transform playerTransform;
    private bool canFollowPlayer = false;

    void Update()
    {
        if (playerTransform != null && canFollowPlayer)
        {
            // Check if player is within the following distance
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= followDistance)
            {
                // Smoothly transition to follow the player
                float target = 1f;
                rig.weight = Mathf.Lerp(rig.weight, target, Time.deltaTime * lerpSpeed);
                if (headLookAtTarget != null)
                {
                    headLookAtTarget.position = playerTransform.position + Vector3.up * 1.6f;
                }
            }
            else
            {
                // Stop following if the player is out of range
                StopLooking();
            }
        }
    }

    public void LookAtTransform(Transform t, float yOffset = 1.6f)
    {
        playerTransform = t;
        isLooking = true;  // This should be true during interaction
        if (headLookAtTarget != null && t != null)
            headLookAtTarget.position = t.position + Vector3.up * yOffset;
    }

    public void StopLooking() => isLooking = false;

    public void SetPlayerTransform(Transform t) => playerTransform = t;

    public void EnableFollowing(bool enable) => canFollowPlayer = enable;
}
