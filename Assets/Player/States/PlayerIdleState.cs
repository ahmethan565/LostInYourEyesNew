// PlayerIdleState.cs
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm) { }

    public override void Enter()
    {
        Debug.Log("Idle durumuna girildi.");
        // Idle durumuna özgü başlatma işlemleri
    }

    public override void Execute()
    {
        // Yatay hareket inputu kontrolü
        Vector3 horizontalMove = player.GetInputMoveVector(); // Yeni yardımcı metot ile
        if (horizontalMove.magnitude > 0.01f) // Epsilon değeri ile karşılaştırma daha güvenli
        {
            fsm.ChangeState(typeof(PlayerWalkingState));
            return; // Durum değişikliği olduğu için bu fonksiyondan çık
        }

        // Zıplama en yüksek önceliğe sahip olabilir (inputtan bağımsız)
        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            fsm.ChangeState(typeof(PlayerJumpingState));
            return; // Çok önemli! Durum değişti, bu frame'de başka işlem yapma.
        }

        // Çömelme kontrolü
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            fsm.ChangeState(typeof(PlayerCrouchingState));
            return; // Durum değişti, başka işlem yapma.
        }

        // Koşma kontrolü
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (horizontalMove.magnitude > 0.01f) // Koşmak için hareket ediyor olmalı
            {
                fsm.ChangeState(typeof(PlayerRunningState));
                return; // Durum değişti, başka işlem yapma.
            }
        }

        // Yatay hareket (yürüme/idle) - Bu en son kontrol edilmeli
        Vector3 currentHorizontalMove = player.GetInputMoveVector();
        if (currentHorizontalMove.magnitude > 0.01f)
        {
            if (fsm.GetCurrentState().GetType() != typeof(PlayerWalkingState)) // Zaten Walking değilse değiştir
            {
                fsm.ChangeState(typeof(PlayerWalkingState));
                return;
            }
        }
        else // Hareket yoksa Idle'a dön
        {
            if (fsm.GetCurrentState().GetType() != typeof(PlayerIdleState)) // Zaten Idle değilse değiştir
            {
                fsm.ChangeState(typeof(PlayerIdleState));
                return;
            }
        }

        // Yer çekimi ve hareket (eğer yukarıdaki if'lerden hiçbiri return etmediyse)
        player.ApplyGravity();
        player.MoveCharacter();
    }

    public override void Exit()
    {
        Debug.Log("Idle durumundan çıkıldı.");
    }
}
