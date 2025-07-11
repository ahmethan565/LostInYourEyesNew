// PlayerRunningState.cs
using UnityEngine;

public class PlayerRunningState : PlayerState
{
    private float originalMoveSpeed; // Koşma öncesi hızı saklamak için
    private float sprintDuration = 0f;
    private float slideTriggerTime = 1.0f; // Örn: 1 saniye sonra slide açılabilir

    public PlayerRunningState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm)
    {
        sprintDuration = player.sprintDuration;
        slideTriggerTime = player.slideTriggerTime; // Örn: 1 saniye sonra slide açılabilir
    }

    public override void Enter()
    {
        Debug.Log("Running durumuna girildi.");
        // Koşma hızını ayarla (Örn: moveSpeed'in 1.5 katı)
        originalMoveSpeed = player.moveSpeed; // Mevcut hızı kaydet
        player.moveSpeed = player.sprintSpeed; // Koşma hızına ayarla

        // Animasyon tetikleyici: player.animator.SetBool("IsRunning", true);
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
            {
                fsm.ChangeState(typeof(PlayerSlidingState));
            }
            else
            {
                fsm.ChangeState(typeof(PlayerCrouchingState));
            }
            return;
        }



        // 3. Koşma Tuşu Bırakıldıysa veya Hareket Durduysa
        Vector3 horizontalMove = player.GetInputMoveVector();

        if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) // Koşma tuşu bırakıldıysa
        {
            if (horizontalMove.magnitude > 0.01f)
            {
                fsm.ChangeState(typeof(PlayerWalkingState)); // Yürüme tuşu basılıysa yürüme
            }
            else
            {
                fsm.ChangeState(typeof(PlayerIdleState)); // Yoksa idle
            }
            return;
        }
        // 4. Hareket Yoksa Idle'a Dön (koşma tuşu basılı olsa bile hareket yoksa)
        if (horizontalMove.magnitude < 0.01f)
        {
            fsm.ChangeState(typeof(PlayerIdleState));
            return;
        }

        // Yer çekimi ve karakter hareketini uygula
        player.ApplyGravity();
        player.MoveCharacter(); // MoveCharacter() metodu player.moveSpeed'i kullanacağı için bu ayar otomatik yansır
    }

    public override void Exit()
    {
        Debug.Log("Running durumundan çıkıldı.");
        sprintDuration = 0f;
        player.moveSpeed = originalMoveSpeed; // Hızı eski haline getir
        // Animasyon tetikleyici: player.animator.SetBool("IsRunning", false);
    }
}
