using UnityEngine;
using Photon.Pun;

public class PlayerHeadBob : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    [Tooltip("The camera transform to apply head bobbing to. This should be a child of your player character.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("The initial local position of the camera. This is used as the base for bobbing.")]
    [SerializeField] private Vector3 initialCameraLocalPosition;

    [Tooltip("Reference to the CharacterController. If you're using a different controller, you'll need to adapt the GetCurrentSpeed method.")]
    [SerializeField] private CharacterController characterController;

    [Header("Head Bob Settings - Walk")]
    [Tooltip("Amplitude of head bobbing when walking (how much the camera moves).")]
    [SerializeField] private float walkBobAmplitude = 0.015f;
    [Tooltip("Frequency of head bobbing when walking (how fast the camera moves).")]
    [SerializeField] private float walkBobFrequency = 8f;

    [Header("Head Bob Settings - Run")]
    [Tooltip("Amplitude of head bobbing when running.")]
    [SerializeField] private float runBobAmplitude = 0.03f;
    [Tooltip("Frequency of head bobbing when running.")]
    [SerializeField] private float runBobFrequency = 12f;

    [Header("Head Bob Settings - Jump Land")]
    [Tooltip("Amplitude of the head bob when landing from a jump.")]
    [SerializeField] private float jumpLandBobAmplitude = 0.05f;
    [Tooltip("Duration of the jump land bob.")]
    [SerializeField] private float jumpLandBobDuration = 0.2f;

    [Header("Smoothness Settings")]
    [Tooltip("How smoothly the head bob interpolates between states (e.g., stopping, starting, changing speed).")]
    [SerializeField] private float smoothTime = 0.1f;

    [Header("External Control Flags")]
    [Tooltip("Set this to true when the player is aiming down sights.")]
    public bool isAiming = false;
    [Tooltip("Set this to true when the player is running.")]
    public bool isRunning = false;

    // Private variables for head bob logic
    private float _timer;
    private Vector3 _targetCameraLocalPosition;
    private Vector3 _currentCameraLocalVelocity;
    private bool _wasGrounded;
    private float _jumpLandTimer;

    // Event for footstep sounds (optional)
    public delegate void FootstepEventHandler();
    public static event FootstepEventHandler OnFootstep;
    [Header("Speed Thresholds")]
    [Tooltip("Minimum speed to be considered walking.")]
    [SerializeField] private float walkSpeedThreshold = 2f; // Yürüme hızı eşiği
    [Tooltip("Minimum speed to be considered running.")]
    [SerializeField] private float runSpeedThreshold = 3.0f;  // Koşma hızı eşiği (karakter hızınıza göre ayarlayın)

    // isRunning ve isAiming'i artık public olarak dışarıdan set etmenize gerek kalmayacak,
    // ancak hala dışarıdan kontrol etmek isterseniz public kalabilirler.
    // Şimdilik onları private yapıp internal olarak kontrol edelim:
    // public bool isAiming = false; // Dışarıdan kontrol edilecekse public kalsın
    // public bool isRunning = false; // Bu artık internal olarak belirlenecek.

    // Eğer isAiming'i hareket betiğinizden ayarlıyorsanız, public olarak kalmalı.
    [Tooltip("Set this to true when the player is aiming down sights.")]

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("PlayerHeadBob: Camera Transform is not assigned! Head bobbing will not work.", this);
            enabled = false;
            return;
        }

        if (characterController == null)
        {
            // Debug.LogWarning("PlayerHeadBob: CharacterController is not assigned. Ensure GetCurrentSpeed method is adapted for your custom controller.", this);
            // Karakter kontrolcüsü yoksa GetCurrentSpeed'in 0 döneceğini varsayalım
            // Bu uyarıyı kaldırabilir veya başka bir mesaj verebiliriz.
            Debug.LogError("PlayerHeadBob: CharacterController is not assigned! Head bobbing cannot determine speed.", this);
            enabled = false; // Karakter hızını alamıyorsak kafa sallama çalışmaz.
            return;
        }

        // Start'ta her zaman pozisyonu güncelle:
        initialCameraLocalPosition = cameraTransform.localPosition;

        _targetCameraLocalPosition = initialCameraLocalPosition;
        _wasGrounded = characterController.isGrounded;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleJumpLandingBob();
        ApplyHeadBob();
    }

    private void ApplyHeadBob()
    {
        if (isAiming)
        {
            _targetCameraLocalPosition = initialCameraLocalPosition;
            cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
            _timer = 0;
            return;
        }

        float speed = GetCurrentSpeed();

        // Koşma durumunu hız eşiklerine göre belirle
        bool isCurrentlyRunning = (speed >= runSpeedThreshold);
        bool isCurrentlyWalking = (speed >= walkSpeedThreshold && speed < runSpeedThreshold);

        if (isCurrentlyWalking || isCurrentlyRunning) // Player is moving
        {
            float currentAmplitude = isCurrentlyRunning ? runBobAmplitude : walkBobAmplitude;
            float currentFrequency = isCurrentlyRunning ? runBobFrequency : walkBobFrequency;

            _timer += Time.deltaTime * currentFrequency;

            float xBob = Mathf.Cos(_timer) * currentAmplitude;
            float yBob = Mathf.Sin(_timer * 2) * currentAmplitude * 0.8f;

            _targetCameraLocalPosition = initialCameraLocalPosition + new Vector3(xBob, yBob, 0f);

            // Optional: Footstep sound integration
            if (Mathf.Abs(_timer % (Mathf.PI * 2)) < (currentFrequency * Time.deltaTime) * 0.5f)
            {
                OnFootstep?.Invoke();
            }
        }
        else // Player is idle (speed < walkSpeedThreshold)
        {
            _targetCameraLocalPosition = initialCameraLocalPosition;
            _timer = 0;
            _currentCameraLocalVelocity = Vector3.zero; // Bu satır titremeyi önler
        }

        if (_jumpLandTimer > 0)
        {
            float landBobProgress = 1 - (_jumpLandTimer / jumpLandBobDuration);
            float landBobOffset = Mathf.Sin(landBobProgress * Mathf.PI) * jumpLandBobAmplitude;
            _targetCameraLocalPosition.y -= landBobOffset;
        }

        cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
    }

    private float GetCurrentSpeed()
    {
        if (characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
            return horizontalVelocity.magnitude;
        }
        return 0f;
    }

    private void HandleJumpLandingBob()
    {
        if (characterController == null) return;

        bool isCurrentlyGrounded = characterController.isGrounded;

        if (!_wasGrounded && isCurrentlyGrounded)
        {
            _jumpLandTimer = jumpLandBobDuration;
        }

        if (_jumpLandTimer > 0)
        {
            _jumpLandTimer -= Time.deltaTime;
        }

        _wasGrounded = isCurrentlyGrounded;
    }

    // isRunning'i artık doğrudan bu script belirlediği için bu metot gereksiz hale gelebilir.
    // Ancak dışarıdan kontrol etmeniz gerekiyorsa yine de kalabilir.
    // Eğer yalnızca hız eşikleriyle belirlenecekse, bu metodu kaldırın.
    // public void SetRunning(bool running)
    // {
    //     // isRunning = running; // Artık dışarıdan set edilmesine gerek yok
    // }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public void ResetBobbing()
    {
        _timer = 0;
        _targetCameraLocalPosition = initialCameraLocalPosition;
        cameraTransform.localPosition = initialCameraLocalPosition;
        _currentCameraLocalVelocity = Vector3.zero;
        _jumpLandTimer = 0;
    }
}
