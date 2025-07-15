using UnityEngine;

public class organCapsuleTriggerScript : MonoBehaviour
{
    public Canvas organCanvas;
    private bool organPlayed = false;

    public playerDetector playerDetector; // Oyuncuyu tespit eden script

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && organPlayed == false)
        {
            Instantiate(organCanvas);
            organPlayed = true;

            // FPSPlayerController mı yoksa FPSPlayerControllerSingle mı?
            var controller = other.GetComponent<FPSPlayerController>();
            if (controller != null)
            {
                controller.isMovementFrozen = true; // 🔑 input'u dondur
                playerDetector.playerController = controller;
                Debug.Log("Movement frozen for multiplayer controller");
                return; // Bitti
            }

            var controllerSingle = other.GetComponent<FPSPlayerControllerSingle>();
            if (controllerSingle != null)
            {
                controllerSingle.moveSpeed = 0f; // 🔑 input'u dondur
                playerDetector.playerControllerSingle = controllerSingle;
                Debug.Log("Movement frozen for single player controller");
                return;
            }
        }
    }
}
