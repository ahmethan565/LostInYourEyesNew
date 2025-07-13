using UnityEngine;

[CreateAssetMenu(fileName = "playerDetector", menuName = "Scriptable Objects/playerDetector")]
public class playerDetector : ScriptableObject
{
    public FPSPlayerController playerController;
    public FPSPlayerControllerSingle playerControllerSingle;
}
