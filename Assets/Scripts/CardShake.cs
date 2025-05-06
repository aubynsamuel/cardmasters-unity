using UnityEngine;

public class CardShake : MonoBehaviour
{
    [Header("Overall Shake Settings")]
    public float shakeDuration = 0.2f;

    [Header("Axis Magnitudes")]
    [Tooltip("How far (in local units) the card moves side to side.")]
    public float horizontalMagnitude = 0.2f;
    [Tooltip("How far (in local units) the card moves up and down.")]
    public float verticalMagnitude = 0.02f;

    [Header("Speed Controls")]
    [Tooltip("How many jitters per second.")]
    public float shakeFrequency = 25f;

    // Internal
    private Vector3 originalPosition;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private float nextJitterTime = 0f;

    // Hook this from your game logic when the card is in a "success" state
    public bool isSuccess = false;

    void OnMouseDown()
    {
        if (!isSuccess && !isShaking)
        {
            originalPosition = transform.localPosition;
            isShaking = true;
            shakeTimer = 0f;
            nextJitterTime = 0f;
        }
    }

    void Update()
    {
        if (!isShaking) return;

        shakeTimer += Time.deltaTime;
        if (shakeTimer >= shakeDuration)
        {
            // End shake
            isShaking = false;
            transform.localPosition = originalPosition;
            return;
        }

        // Only jitter position at the set frequency
        if (shakeTimer >= nextJitterTime)
        {
            // schedule next jitter
            nextJitterTime += 1f / shakeFrequency;

            // random horizontal and tiny vertical offset
            float xOffset = Random.Range(-horizontalMagnitude, horizontalMagnitude);
            float yOffset = Random.Range(-verticalMagnitude, verticalMagnitude);

            transform.localPosition = originalPosition + new Vector3(xOffset, yOffset, 0f);
        }
    }
}