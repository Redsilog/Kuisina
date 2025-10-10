using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.3f;
    public float swaySpeed = 0.6f;
    public float noiseIntensity = 0.2f;
    public float rotationAmount = 0.8f;
    public float depthAmount = 0.15f;

    private Vector3 startPos;
    private Quaternion startRot;
    private float seedX, seedY, seedZ;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
        seedZ = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * swaySpeed;

        float offsetX = (Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f * swayAmount;
        float offsetY = (Mathf.PerlinNoise(seedY, t * 0.8f) - 0.5f) * 2f * swayAmount * 0.7f;
        float offsetZ = (Mathf.PerlinNoise(seedZ, t * 0.6f) - 0.5f) * 2f * depthAmount;

        offsetX += Mathf.Sin(t * 1.7f) * noiseIntensity * 0.3f;
        offsetY += Mathf.Cos(t * 1.1f) * noiseIntensity * 0.2f;

        transform.position = startPos + new Vector3(offsetX, offsetY, offsetZ);

        float rotX = Mathf.Sin(t * 0.7f) * rotationAmount;
        float rotY = Mathf.Cos(t * 0.9f) * rotationAmount;
        transform.rotation = startRot * Quaternion.Euler(rotX, rotY, 0);
    }
}
