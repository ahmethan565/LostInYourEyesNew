// PlayerWalkingState.cs
using UnityEngine;

public class PlayerWalkingState : PlayerState
{
    public PlayerWalkingState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm) { }

    public override void Enter()
    {
        Debug.Log("Walking durumuna girildi.");
        player.isSprinting = false;

        // Yürüme animasyonunu başlat (eğer varsa)
        // player.animator.SetBool("IsWalking", true);
        // player.animator.SetBool("IsIdle", false);
    }

    public override void Execute()
    {
        // ÖNEMLİ: Geçiş koşullarının sırası önemlidir!
        // Genellikle spesifik durumlar (zıplama, koşma, çömelme) önce kontrol edilir.

        // 1. Zıplama Geçişi (En Yüksek Öncelik)
        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            fsm.ChangeState(typeof(PlayerJumpingState));
            return;
        }

        // 2. Çömelme Geçişi
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            fsm.ChangeState(typeof(PlayerCrouchingState));
            return;
        }

        // 3. Koşma Geçişi
        // Hem hareket inputu olmalı hem de koşma tuşuna basılı olmalı
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            Vector3 horizontalMove = player.GetInputMoveVector();
            if (horizontalMove.magnitude > 0.01f) // Hala ileri/yanlara hareket ediyor olmalı
            {
                fsm.ChangeState(typeof(PlayerRunningState));
                return;
            }
        }

        // 4. Idle Geçişi (Hareket Durduğunda)
        Vector3 currentHorizontalMove = player.GetInputMoveVector();
        if (currentHorizontalMove.magnitude < 0.01f)
        {
            fsm.ChangeState(typeof(PlayerIdleState));
            return;
        }

        // Fiziksel Hareket (Eğer durum değişmediyse)
        player.ApplyGravity();
        player.MoveCharacter();
    }

    public override void Exit()
    {
        Debug.Log("Walking durumundan çıkıldı.");
        // Yürüme animasyonunu durdur (eğer varsa)
        // player.animator.SetBool("IsWalking", false);
    }
}