using UnityEngine;

public class CreepyEye : MonoBehaviour
{
    [Header("Target Eye Object")]
    public Transform eyeObject;

    [Header("Pupil Object (scale will change)")]
    public Transform pupilTransform;

    [Header("Twitch Settings")]
    public float twitchIntensity = 10f;
    public float twitchSpeed = 1f;
    public bool usePerlinNoise = true;

    [Header("Freeze Settings")]
    public float freezeInterval = 6f;
    public float freezeDuration = 1f;
    private bool isFrozen = false;
    private float freezeTimer = 0f;
    private float freezeEndTime = 0f;

    [Header("Snap Settings")]
    public float snapInterval = 8f;
    public float snapRotationAmount = 90f;
    private float snapTimer = 0f;
    private Quaternion? snapTargetRotation = null;

    [Header("Pupil Settings")]
    public Vector2 pupilScaleRange = new Vector2(0.5f, 1.1f);
    public float pupilLerpSpeed = 4f;
    public float chancePerSecond = 0.05f; // 5% şansla hareket eder
    private float randomPupilTimer = 0f;
    private Vector3 targetPupilScale;

    private float seedX;
    private float seedY;

    void Start()
    {
        if (eyeObject == null)
            eyeObject = transform;

        if (pupilTransform == null)
            Debug.LogWarning("Pupil Transform not assigned.");

        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
        targetPupilScale = pupilTransform != null ? pupilTransform.localScale : Vector3.one;
    }

    void Update()
    {
        freezeTimer += Time.deltaTime;
        snapTimer += Time.deltaTime;

        if (!isFrozen && freezeTimer >= freezeInterval)
        {
            isFrozen = true;
            freezeEndTime = Time.time + freezeDuration;
            freezeTimer = 0f;

            // 🎯 Donma sonrası pupil küçülebilir
            if (pupilTransform != null)
                SetPupilScale(pupilScaleRange.x);
        }

        if (isFrozen && Time.time >= freezeEndTime)
        {
            isFrozen = false;
        }

        if (isFrozen)
            return;

        if (snapTargetRotation.HasValue)
        {
            eyeObject.localRotation = Quaternion.Slerp(
                eyeObject.localRotation,
                snapTargetRotation.Value,
                Time.deltaTime * 5f
            );

            if (Quaternion.Angle(eyeObject.localRotation, snapTargetRotation.Value) < 1f)
            {
                snapTargetRotation = null;
            }
        }
        else if (snapTimer >= snapInterval)
        {
            float snapYaw = Random.Range(-snapRotationAmount, snapRotationAmount);
            float snapPitch = Random.Range(-snapRotationAmount, snapRotationAmount);
            snapTargetRotation = Quaternion.Euler(snapPitch, snapYaw, 0f);
            snapTimer = 0f;

            // ⚡ Snap anında büyüt
            if (pupilTransform != null)
                SetPupilScale(pupilScaleRange.y);
        }
        else
        {
            Vector3 targetEuler = Vector3.zero;

            if (usePerlinNoise)
            {
                float noiseX = Mathf.PerlinNoise(seedX, Time.time * twitchSpeed) - 0.5f;
                float noiseY = Mathf.PerlinNoise(seedY, Time.time * twitchSpeed) - 0.5f;
                targetEuler = new Vector3(noiseY, noiseX, 0f) * twitchIntensity;
            }
            else
            {
                float sinX = Mathf.Sin(Time.time * twitchSpeed);
                float cosY = Mathf.Cos(Time.time * twitchSpeed);
                targetEuler = new Vector3(cosY, sinX, 0f) * twitchIntensity;
            }

            Quaternion targetRotation = Quaternion.Euler(targetEuler);
            eyeObject.localRotation = Quaternion.Slerp(eyeObject.localRotation, targetRotation, Time.deltaTime * 3f);
        }

        HandlePupilDilation();
    }

    void HandlePupilDilation()
    {
        if (pupilTransform == null) return;

        // 🎲 Bazen rastgele deforme
        if (Random.value < chancePerSecond * Time.deltaTime)
        {
            float randomScale = Random.Range(pupilScaleRange.x, pupilScaleRange.y);
            SetPupilScale(randomScale);
        }

        // Her karede yumuşak geçiş
        pupilTransform.localScale = Vector3.Lerp(
            pupilTransform.localScale,
            targetPupilScale,
            Time.deltaTime * pupilLerpSpeed
        );
    }

    void SetPupilScale(float scale)
    {
        if (pupilTransform == null) return;

        targetPupilScale = new Vector3(scale, scale, scale);
    }
}
