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
    private bool canFollowPlayer;

    void Update()
    {
        bool within = false;
        if (playerTransform != null)
        {
            float d = Vector3.Distance(transform.position, playerTransform.position);
            within = d <= followDistance;
        }

        float target = (isLooking && within && canFollowPlayer) ? 1f : 0f;
        rig.weight = Mathf.Lerp(rig.weight, target, Time.deltaTime * lerpSpeed);

        if (headLookAtTarget != null && playerTransform != null && isLooking && within && canFollowPlayer)
        {
            headLookAtTarget.position = playerTransform.position + Vector3.up * 1.6f;
        }
    }

    public void LookAtTransform(Transform t, float yOffset = 1.6f)
    {
        playerTransform = t;
        isLooking = true;
        if (headLookAtTarget != null && t != null)
            headLookAtTarget.position = t.position + Vector3.up * yOffset;
    }

    public void StopLooking() => isLooking = false;

    public void SetPlayerTransform(Transform t) => playerTransform = t;

    public void EnableFollowing(bool enable) => canFollowPlayer = enable;
}
