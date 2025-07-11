using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class PortraitHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Settings")]
    [Tooltip("Hover animasyonunu uygulamak istediğin RectTransform. Boşsa bu objenin RectTransform'u kullanılır.")]
    public RectTransform targetTransform;

    [Header("Hover Settings")]
    public bool enableHover = true;           // Hover aktif mi?
    public float hoverScale = 1.1f;           // Hover olduğunda scale
    public float animationDuration = 0.2f;    // Animasyon süresi
    public Ease animationEase = Ease.OutQuad; // Ease tipi

    private Vector3 originalScale;
    private Tween currentTween;

    void Awake()
    {
        // Eğer hedef belirtilmediyse kendi RectTransform'unu kullan
        if (targetTransform == null)
        {
            targetTransform = GetComponent<RectTransform>();
        }
        originalScale = targetTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHover) return;

        currentTween?.Kill();

        currentTween = targetTransform.DOScale(originalScale * hoverScale, animationDuration)
            .SetEase(animationEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHover) return;

        currentTween?.Kill();

        currentTween = targetTransform.DOScale(originalScale, animationDuration)
            .SetEase(animationEase);
    }
}
