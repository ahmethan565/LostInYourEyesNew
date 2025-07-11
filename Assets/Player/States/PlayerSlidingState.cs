using UnityEngine;

public class PlayerSlidingState : PlayerState
{
    private float slideSpeed;
    private float currentSpeed;
    private float slideTimer;
    private Vector3 slideDirection;

    private readonly float maxSlideSpeed = 40f;
    private readonly float baseSlideSpeed = 15;
    private readonly float flatSlideDuration = 0.8f;
    private readonly float slopeAcceleration = 12f;
    private readonly float slopeDetectionRange = 1.5f;
    private readonly float minSlideEndSpeed = 1.5f;
    private readonly float minSlideAngle = 5f;
    private readonly float maxSlideAngle = 45f;

    public PlayerSlidingState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm)
    {

    }

    public override void Enter()
    {
        Debug.Log("Sliding durumuna girildi.");

        slideTimer = flatSlideDuration;
        slideDirection = player.transform.forward;
        currentSpeed = baseSlideSpeed;
    }

    public override void Execute()
    {
        // Zemin eğimini kontrol et
        bool onSlope = IsOnSlope(out RaycastHit hit, out float angle);

        if (onSlope)
        {
            float slopeFactor = Mathf.InverseLerp(minSlideAngle, maxSlideAngle, angle);
            currentSpeed += slopeFactor * slopeAcceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, baseSlideSpeed, maxSlideSpeed);
        }
        else
        {
            // Düz zemin: sabit hızda kay, sonra yavaşla
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, 6f * Time.deltaTime);
            slideTimer -= Time.deltaTime;
        }

        // Kayma hareketi
        Vector3 motion = slideDirection * currentSpeed;
        motion.y = player.playerVelocity.y;
        player.controller.Move(motion * Time.deltaTime);

        // Yer çekimi uygula
        player.ApplyGravity();

        // Bitirme koşulları
        if (!onSlope && (slideTimer <= 0f || currentSpeed <= minSlideEndSpeed || !player.isGrounded))
        {
            fsm.ChangeState(typeof(PlayerIdleState));
        }

        // Slide sırasında Jump yapılırsa?
        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            fsm.ChangeState(typeof(PlayerJumpingState));
        }
    }

    public override void Exit()
    {
        Debug.Log("Sliding durumundan çıkıldı.");
        // Gerekirse animasyon sıfırlama yapılabilir
    }

    private bool IsOnSlope(out RaycastHit hit, out float slopeAngle)
    {
        hit = default;
        slopeAngle = 0f;

        if (Physics.Raycast(player.transform.position, Vector3.down, out hit, slopeDetectionRange))
        {
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            return slopeAngle >= minSlideAngle && slopeAngle <= maxSlideAngle;
        }

        return false;
    }

    private Vector3 CalculateSlopeDirection(Vector3 normal)
    {
        // Yokuş aşağı yönü, yerçekiminin yüzeye izdüşümü
        Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        return slopeDir;
    }

}
