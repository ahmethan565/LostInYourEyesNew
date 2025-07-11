using Unity.VisualScripting;
using UnityEngine;

public class doorCheckerBlue : MonoBehaviour
{

    public FPSPlayerControllerSingle bluePlayer;
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<hostPlayerU>() != null)
        {
            Debug.Log("BLUEPLAYER ENTERED;");
        }
        else
        {
            Debug.Log("Yanlış oyuncu");
        }
    }
}
