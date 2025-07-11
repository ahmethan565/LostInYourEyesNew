using Photon.Realtime;
using Unity.VisualScripting;
using UnityEngine;

public class organCapsuleTrigger : MonoBehaviour
{
    public Canvas organCanvas;

    public Player player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Instantiate(organCanvas);
        }
    }
}
