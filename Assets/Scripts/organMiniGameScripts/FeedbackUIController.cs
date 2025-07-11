using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackUIController : MonoBehaviour
{
    public static FeedbackUIController Instance;
    
    public Image feedbackImage;
    public TMP_Text feedbackText;

    void Awake()
    {
        Instance = this;
    }

    public void ShowFeedback(Color color, KeyType key)
    {
        color.a = 1f;
        feedbackImage.color = color;
        feedbackText.text = GetKeyDisplay(key);
    }

    public string GetKeyDisplay(KeyType key)
    {
        return key switch
        {
            KeyType.Left => "◄",
            KeyType.Up => "▲",
            KeyType.Right => "►",
            KeyType.Down => "▼",
            _ => key.ToString()
        };
    }
}
