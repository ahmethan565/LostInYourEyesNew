using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class SimpleButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;

    [Header("Hover Ayarları")]
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.15f;

    [Header("Click Ayarları")]
    public float clickScale = 0.9f;
    public float clickDuration = 0.1f;

    [Header("Renk Değişimi")]
    public bool changeColor = false;
    public Graphic targetGraphic; // Image, TextMeshProUGUI, vs.
    public Color hoverColor = Color.grey;
    public float colorDuration = 0.2f;

    private Color originalColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform
            .DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad);

        if (changeColor && targetGraphic != null)
        {
            targetGraphic
                .DOColor(hoverColor, colorDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform
            .DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad);

        if (changeColor && targetGraphic != null)
        {
            targetGraphic
                .DOColor(originalColor, colorDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Sequence clickSeq = DOTween.Sequence();
        clickSeq.Append(rectTransform
            .DOScale(originalScale * clickScale, clickDuration)
            .SetEase(Ease.OutQuad));
        clickSeq.Append(rectTransform
            .DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad));
    }
    private void OnEnable()
    {
        if (rectTransform != null)
            rectTransform.localScale = originalScale;

        if (changeColor && targetGraphic != null)
            targetGraphic.color = originalColor;
    }
    private void OnDisable()
    {
        // DOTween animasyonlarını güvenli şekilde iptal et
        rectTransform?.DOKill();
        targetGraphic?.DOKill();
    }

}
