using Photon.Realtime;
using UnityEngine;

public class organCapsuleTriggerScript : MonoBehaviour
{
    public Canvas organCanvas;
    private bool organPlayed = false;

    public Player player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && organPlayed == false)
        {
            Instantiate(organCanvas);
            organPlayed = true;
        }
    }
}
