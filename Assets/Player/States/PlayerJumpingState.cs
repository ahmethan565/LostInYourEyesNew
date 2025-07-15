// PlayerJumpingState.cs
using UnityEngine;

public class PlayerJumpingState : PlayerState
{
    public PlayerJumpingState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm) { }

    public override void Enter()
    {
        Debug.Log("Jumping durumuna girildi.");
        player.HandleJumpInput(); // Zıplama gücünü uygula
        // Zıplama animasyonunu başlat
        // player.animator.SetTrigger("Jump");
        // player.animator.SetBool("IsGrounded", false);
    }

    public override void Execute()
    {
        // Yer çekimi her zaman uygulanmalı
        player.ApplyGravity();
        player.MoveCharacter();

        // Zıplama tamamlandı mı? Yere değdi mi ve dikey hız sıfıra yakın mı?
        if (player.isGrounded && player.playerVelocity.y < 0.1f) // 0.1f gibi küçük bir değer daha kararlı olabilir
        {
            // Yere indikten sonra hangi duruma geçeceğine karar ver
            Vector3 horizontalMove = player.GetInputMoveVector();
            // 1. Çömelerek İniş
            if (Input.GetKeyDown(KeyCode.LeftControl)) // Tuşa basılı tutuyorsa
            {
                fsm.ChangeState(typeof(PlayerCrouchingState));
                return;
            }
            // 2. Koşarak İniş
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (horizontalMove.magnitude > 0.01f)
                {
                    fsm.ChangeState(typeof(PlayerRunningState));
                    return;
                }
            }
            // 3. Yürüyerek İniş
            else if (horizontalMove.magnitude > 0.01f)
            {
                fsm.ChangeState(typeof(PlayerWalkingState));
                return;
            }
            // 4. Idle İniş (Hareket yoksa)
            else
            {
                fsm.ChangeState(typeof(PlayerIdleState));
                return;
            }
            player.isSprinting = false;

        }
    }

    public override void Exit()
    {
        Debug.Log("Jumping durumundan çıkıldı.");
        // player.animator.SetBool("IsGrounded", true);
    }
}