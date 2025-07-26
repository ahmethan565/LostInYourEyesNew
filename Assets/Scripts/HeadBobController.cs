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
    [Tooltip("Minimum vertical speed to trigger an increased landing bob.")]
    [SerializeField] private float minFallSpeedForImpact = 5f;
    [Tooltip("Multiplier for additional landing bob amplitude based on fall speed.")]
    [SerializeField] private float fallImpactMultiplier = 0.01f; // Adjust this for more/less impact

    [Header("Head Bob Settings - Jump Kick")]
    [Tooltip("Amplitude of the head bob when starting a jump.")]
    [SerializeField] private float jumpKickAmplitude = 0.03f;
    [Tooltip("Duration of the jump kick bob.")]
    [SerializeField] private float jumpKickDuration = 0.15f;

    [Header("Head Bob Settings - In Air (Subtle Wobble)")]
    [Tooltip("Amplitude of head bobbing when in air.")]
    [SerializeField] private float airBobAmplitude = 0.005f; // Very subtle
    [Tooltip("Frequency of head bobbing when in air.")]
    [SerializeField] private float airBobFrequency = 2f; // Slow frequency

    [Header("Smoothness Settings")]
    [Tooltip("How smoothly the head bob interpolates between states (e.g., stopping, starting, changing speed).")]
    [SerializeField] private float smoothTime = 0.1f;

    [Header("External Control Flags")]
    [Tooltip("Set this to true when the player is aiming down sights.")]
    public bool isAiming = false;
    // isRunning is now determined internally by speed thresholds, so it can be private.
    // If you still need external control, keep it public.
    // public bool isRunning = false; 

    // Private variables for head bob logic
    private float _timer;
    private Vector3 _targetCameraLocalPosition;
    private Vector3 _currentCameraLocalVelocity;
    private bool _wasGrounded;
    private float _jumpLandTimer;
    private float _jumpKickTimer; // New timer for jump kick
    private float _currentDynamicLandAmplitude; // To store dynamic land amplitude

    // Event for footstep sounds (optional)
    public delegate void FootstepEventHandler();
    public static event FootstepEventHandler OnFootstep;

    [Header("Speed Thresholds")]
    [Tooltip("Minimum speed to be considered walking.")]
    [SerializeField] private float walkSpeedThreshold = 2f;
    [Tooltip("Minimum speed to be considered running.")]
    [SerializeField] private float runSpeedThreshold = 3.0f;

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
            Debug.LogError("PlayerHeadBob: CharacterController is not assigned! Head bobbing cannot determine speed.", this);
            enabled = false;
            return;
        }

        initialCameraLocalPosition = cameraTransform.localPosition;
        _targetCameraLocalPosition = initialCameraLocalPosition;
        _wasGrounded = characterController.isGrounded;
        _jumpLandTimer = 0;
        _jumpKickTimer = 0;
        _currentDynamicLandAmplitude = jumpLandBobAmplitude; // Initialize
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleJumpAndLandingStates(); // Renamed and updated for better clarity
        ApplyHeadBob();
    }

    private void ApplyHeadBob()
    {
        // 1. Aiming takes highest priority - no bobbing
        if (isAiming)
        {
            _targetCameraLocalPosition = initialCameraLocalPosition;
            cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
            _timer = 0; // Reset timer when aiming
            return;
        }

        float speed = GetCurrentSpeed();
        bool isCurrentlyGrounded = characterController.isGrounded;

        // Reset target position to base
        _targetCameraLocalPosition = initialCameraLocalPosition;

        // 2. Apply Jump Kick effect if active
        if (_jumpKickTimer > 0)
        {
            float kickBobProgress = 1 - (_jumpKickTimer / jumpKickDuration);
            // Uses a sine curve for a smooth up-down motion, peaking in the middle.
            float kickBobOffset = Mathf.Sin(kickBobProgress * Mathf.PI) * jumpKickAmplitude;
            _targetCameraLocalPosition.y += kickBobOffset; // Move camera up
        }
        // 3. Apply Jump Land effect if active (adjusts Y position downwards)
        else if (_jumpLandTimer > 0) // Only apply land bob if kick bob isn't active
        {
            float landBobProgress = 1 - (_jumpLandTimer / jumpLandBobDuration);
            float landBobOffset = Mathf.Sin(landBobProgress * Mathf.PI) * _currentDynamicLandAmplitude;
            _targetCameraLocalPosition.y -= landBobOffset; // Move camera down
        }
        // 4. Apply Walking/Running Bob or In-Air Wobble or Idle
        else if (isCurrentlyGrounded) // On the ground
        {
            bool isCurrentlyRunning = (speed >= runSpeedThreshold);
            bool isCurrentlyWalking = (speed >= walkSpeedThreshold && speed < runSpeedThreshold);

            if (isCurrentlyWalking || isCurrentlyRunning) // Player is moving on ground
            {
                float currentAmplitude = isCurrentlyRunning ? runBobAmplitude : walkBobAmplitude;
                float currentFrequency = isCurrentlyRunning ? runBobFrequency : walkBobFrequency;

                _timer += Time.deltaTime * currentFrequency;

                float xBob = Mathf.Cos(_timer) * currentAmplitude;
                float yBob = Mathf.Sin(_timer * 2) * currentAmplitude * 0.8f;

                _targetCameraLocalPosition = initialCameraLocalPosition + new Vector3(xBob, yBob, 0f);

                // Optional: Footstep sound integration
                // This condition makes sure it triggers roughly twice per full bob cycle.
                if (Mathf.Abs(Mathf.Cos(_timer)) > 0.99f && Mathf.Abs(Mathf.Cos(_timer - Time.deltaTime * currentFrequency)) < 0.99f) // Triggers near peaks
                {
                    OnFootstep?.Invoke();
                }
            }
            else // Player is idle on ground (speed < walkSpeedThreshold)
            {
                _timer = 0; // Reset timer when idle
                // _targetCameraLocalPosition is already initialCameraLocalPosition from above.
                // Velocity will be smoothed to zero by SmoothDamp.
            }
        }
        else // Player is in the air (not grounded, and not in jump kick/land animation)
        {
            _timer += Time.deltaTime * airBobFrequency; // Use a separate timer for air bob
            float xBobAir = Mathf.Cos(_timer * 0.5f) * airBobAmplitude;
            float yBobAir = Mathf.Sin(_timer) * airBobAmplitude * 0.5f;

            _targetCameraLocalPosition = initialCameraLocalPosition + new Vector3(xBobAir, yBobAir, 0f);
        }

        // Apply the calculated target position smoothly
        cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
    }

    private float GetCurrentSpeed()
    {
        if (characterController != null)
        {
            // Only consider horizontal velocity for speed determination
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
            return horizontalVelocity.magnitude;
        }
        return 0f;
    }

    private void HandleJumpAndLandingStates()
    {
        if (characterController == null) return;

        bool isCurrentlyGrounded = characterController.isGrounded;

        // Check for landing
        if (!_wasGrounded && isCurrentlyGrounded)
        {
            _jumpLandTimer = jumpLandBobDuration;
            // Calculate dynamic landing amplitude based on vertical fall speed
            float fallSpeed = Mathf.Abs(characterController.velocity.y);
            _currentDynamicLandAmplitude = jumpLandBobAmplitude + Mathf.Max(0, (fallSpeed - minFallSpeedForImpact) * fallImpactMultiplier);
        }

        // Check for jump kick (when transitioning from grounded to not grounded)
        // Only trigger if character is actually moving upwards or has just left the ground with some velocity
        if (_wasGrounded && !isCurrentlyGrounded && characterController.velocity.y > 0.1f) // Ensure it's an actual jump
        {
            _jumpKickTimer = jumpKickDuration;
        }

        // Update timers
        if (_jumpLandTimer > 0)
        {
            _jumpLandTimer -= Time.deltaTime;
        }
        if (_jumpKickTimer > 0)
        {
            _jumpKickTimer -= Time.deltaTime;
        }

        _wasGrounded = isCurrentlyGrounded;
    }

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
        _jumpKickTimer = 0; // Reset jump kick timer too
        _currentDynamicLandAmplitude = jumpLandBobAmplitude; // Reset dynamic amplitude
    }
}