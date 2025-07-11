using UnityEngine;

public class catacombPuzzleChecker : MonoBehaviour
{
    public static catacombPuzzleChecker Instance;

    private Texture[] correctSequence;

    void Awake()
    {
        Instance = this;
    }

    public void SetCorrectSymbols(Texture[] sequence)
    {
        correctSequence = sequence;
        foreach (var Texture in correctSequence)
        {
            Debug.Log(Texture.name);
        }
    }

    public bool Check(Texture[] userInput)
    {
        if (userInput.Length != correctSequence.Length) return false;

        for (int i = 0; i < 3; i++)
        {
            if (userInput[i] != correctSequence[i])
            {
                return false;
            }
        }

        return true;
    }
}
