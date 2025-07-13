using ExitGames.Client.Photon.StructWrapping;
using Photon.Realtime;
using UnityEngine;

public class organCapsuleTriggerScript : MonoBehaviour
{
    public Canvas organCanvas;
    private bool organPlayed = false;

    public Player player;
    private float playerSpeed = 10;
    public playerDetector playerDetector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && organPlayed == false)
        {
            Instantiate(organCanvas);
            organPlayed = true;

            if (other.GetComponent<FPSPlayerController>() != null)
            {
                other.GetComponent<FPSPlayerController>().moveSpeed = 0;
                playerSpeed = other.GetComponent<FPSPlayerController>().moveSpeed;
                Debug.Log(playerSpeed);
                playerDetector.playerController = other.GetComponent<FPSPlayerController>();
            }
            if (other.GetComponent<FPSPlayerControllerSingle>() != null)
            {
                other.GetComponent<FPSPlayerControllerSingle>().moveSpeed = 0;
                playerSpeed = other.GetComponent<FPSPlayerControllerSingle>().moveSpeed;
                playerDetector.playerControllerSingle = other.GetComponent<FPSPlayerControllerSingle>();
                Debug.Log(playerDetector.playerControllerSingle);
            }
        }
    }
}
