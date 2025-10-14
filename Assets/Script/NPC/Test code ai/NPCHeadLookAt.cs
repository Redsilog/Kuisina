using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPCHeadLookAt : MonoBehaviour
{
    [SerializeField] private Rig rig;                    // your head-look rig
    [SerializeField] private Transform headLookAtTarget; // the target Transform the rig uses
    [SerializeField] private float lerpSpeed = 2f;

    private bool isLooking;

    void Update()
    {
        float target = isLooking ? 1f : 0f;
        rig.weight = Mathf.Lerp(rig.weight, target, Time.deltaTime * lerpSpeed);
    }

    public void LookAtPosition(Vector3 worldPos)
    {
        isLooking = true;
        if (headLookAtTarget != null) headLookAtTarget.position = worldPos;
    }

    public void LookAtTransform(Transform t, float yOffset = 1.6f)
    {
        if (t == null) return;
        LookAtPosition(t.position + Vector3.up * yOffset);
    }

    public void StopLooking()
    {
        isLooking = false;
    }
}
