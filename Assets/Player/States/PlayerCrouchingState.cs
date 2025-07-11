// PlayerCrouchingState.cs
using UnityEngine;

public class PlayerCrouchingState : PlayerState
{
    private float originalMoveSpeed;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private float originalCameraYPosition; // Kameranın y pozisyonunu tutmak için

    // Bu değerler FPSPlayerController'dan alınacağı için burada [Header] veya public'e gerek yok
    private float crouchHeight;
    private float crouchSpeedMultiplier;
    private float crouchCameraOffset;

    public PlayerCrouchingState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm)
    {
        // FPSPlayerController'dan ayarları al
        this.crouchHeight = player.crouchHeight;
        this.crouchSpeedMultiplier = player.crouchSpeedMultiplier;
        this.crouchCameraOffset = player.crouchCameraOffset;
    }

    public override void Enter()
    {
        Debug.Log("Crouching durumuna girildi.");

        originalMoveSpeed = player.moveSpeed;
        originalControllerHeight = player.controller.height; // CharacterController'ın yüksekliğini al
        originalControllerCenter = player.controller.center;
        originalCameraYPosition = player.cameraRoot.localPosition.y; // Kameranın mevcut Y pozisyonunu al

        // Çömelme hızını ve yüksekliğini ayarla
        player.moveSpeed *= crouchSpeedMultiplier;
        player.controller.height = crouchHeight;
        player.controller.center = Vector3.up * (crouchHeight / 2f); // Merkez noktasını da ayarla

        // Kamerayı aşağı indir
        if (player.cameraRoot != null)
        {
            player.cameraRoot.localPosition = new Vector3(
                player.cameraRoot.localPosition.x,
                originalCameraYPosition + crouchCameraOffset, // Mevcut konumdan offset kadar aşağı
                player.cameraRoot.localPosition.z
            );
        }

        // Animasyon tetikleyici: player.animator.SetBool("IsCrouching", true);
    }

    public override void Execute()
    {
        // 1. Ayağa Kalkma Geçişi (Çömelme tuşu bırakılırsa)
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            // Ayağa kalkmadan önce yukarıda bir engel olup olmadığını kontrol etmek iyi bir fikirdir.
            // Raycast veya OverlapSphere kullanarak bu kontrolü yapabilirsiniz.
            // Örnek (basit raycast):
            // if (!Physics.Raycast(player.transform.position, Vector3.up, player.originalControllerHeight - crouchHeight + 0.1f)) {
            //     fsm.ChangeState(typeof(PlayerIdleState)); // veya Walking/Running
            //     return;
            // }

            // Şimdilik engelin olmadığını varsayarak doğrudan geçiş yapalım:
            Vector3 horizontalMove = player.GetInputMoveVector();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                 if (horizontalMove.magnitude > 0.01f) fsm.ChangeState(typeof(PlayerRunningState));
                 else fsm.ChangeState(typeof(PlayerIdleState)); // Koşma tuşu basılı ama hareket yok
            }
            else if (horizontalMove.magnitude > 0.01f)
            {
                fsm.ChangeState(typeof(PlayerWalkingState));
            }
            else
            {
                fsm.ChangeState(typeof(PlayerIdleState));
            }
            return;
        }

        // 2. Zıplama Geçişi (Çömelirken Zıplama)
        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            fsm.ChangeState(typeof(PlayerJumpingState));
            return;
        }

        // Fiziksel Hareket (Çömelirken hareket)
        player.ApplyGravity();
        player.MoveCharacter(); // MoveCharacter() player.moveSpeed'i kullanacak, bu zaten çarpanla ayarlandı
    }

    public override void Exit()
    {
        Debug.Log("Crouching durumundan çıkıldı.");
        player.moveSpeed = originalMoveSpeed; // Hızı eski haline getir

        // Karakter kontrol yüksekliğini ve merkezini eski haline getir
        player.controller.height = originalControllerHeight;
        player.controller.center = originalControllerCenter;

        // Kamerayı eski yüksekliğine getir
        if (player.cameraRoot != null)
        {
            player.cameraRoot.localPosition = new Vector3(
                player.cameraRoot.localPosition.x,
                originalCameraYPosition,
                player.cameraRoot.localPosition.z
            );
        }

        // Animasyon tetikleyici: player.animator.SetBool("IsCrouching", false);
    }
}