// PlayerRunningState.cs
using UnityEngine;

public class PlayerRunningState : PlayerState
{
    private float sprintDuration = 0f;
    private float slideTriggerTime = 1.0f;

    public PlayerRunningState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm)
    {
        slideTriggerTime = player.slideTriggerTime;
    }

    public override void Enter()
    {
        //Debug.Log("Running durumuna girildi.");

        // ✅ Artık hız elle ayarlanmaz, FSM flag belirler
        player.isSprinting = true;

        sprintDuration = 0f;

        // player.animator.SetBool("IsRunning", true);
    }

    public override void Execute()
    {
        sprintDuration += Time.deltaTime;

        // 1. Zıplama Geçişi
        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            fsm.ChangeState(typeof(PlayerJumpingState));
            return;
        }

        // 2. Çömelme Geçişi
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (sprintDuration >= slideTriggerTime)
                fsm.ChangeState(typeof(PlayerSlidingState));
            else
                fsm.ChangeState(typeof(PlayerCrouchingState));
            return;
        }

        // 3. Koşma Tuşu Bırakıldıysa veya Hareket Durduysa
        Vector3 horizontalMove = player.GetInputMoveVector();

        if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            if (horizontalMove.magnitude > 0.01f)
                fsm.ChangeState(typeof(PlayerWalkingState));
            else
                fsm.ChangeState(typeof(PlayerIdleState));
            return;
        }

        if (horizontalMove.magnitude < 0.01f)
        {
            fsm.ChangeState(typeof(PlayerIdleState));
            return;
        }

        player.ApplyGravity();
        player.MoveCharacter();
    }

    public override void Exit()
    {
        //Debug.Log("Running durumundan çıkıldı.");
        sprintDuration = 0f;

        // player.animator.SetBool("IsRunning", false);
    }
}
