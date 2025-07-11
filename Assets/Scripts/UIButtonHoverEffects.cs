using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro; // Eðer UI metinleri gösterecekseniz

public class UIButtonHoverEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Transforms")]
    public RectTransform buttonTransform;
    public Image buttonImage;
    public TextMeshProUGUI buttonText;
    public Image iconImage;

    [Header("Hover Settings")]
    public float hoverScale = 1.1f;
    public float duration = 0.2f;
    public Color hoverColor = Color.red;
    public Color textHoverColor = Color.white;
    public Color iconHoverColor = Color.white;

    [Header("Normal Settings")]
    private Vector3 originalScale;
    private Color originalButtonColor;
    private Color originalTextColor;
    private Color originalIconColor;

    void Start()
    {
        // Save original values
        if (buttonTransform == null) buttonTransform = GetComponent<RectTransform>();
        originalScale = buttonTransform.localScale;
        originalButtonColor = buttonImage.color;
        originalTextColor = buttonText.color;
        originalIconColor = iconImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up and change colors
        buttonTransform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutBack);
        buttonImage.DOColor(hoverColor, duration);
        buttonText.DOColor(textHoverColor, duration);
        iconImage.DOColor(iconHoverColor, duration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Revert to original
        buttonTransform.DOScale(originalScale, duration).SetEase(Ease.OutBack);
        buttonImage.DOColor(originalButtonColor, duration);
        buttonText.DOColor(originalTextColor, duration);
        iconImage.DOColor(originalIconColor, duration);
    }
}
