using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class TabController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static Dictionary<string, List<TabController>> tabGroups = new Dictionary<string, List<TabController>>();

    [Header("UI")]
    public RectTransform backgroundSelected;

    [Header("Ayarlar")]
    public string groupName;
    public float pressedY = -10f;
    public float duration = 0.25f;
    public bool selectAndAutoDeselect = false;
    public bool enableHoverAnimation = false;

    private Vector2 originalPos;
    private bool isSelected;
    private bool isHovering;
    void Awake()
    {
        originalPos = backgroundSelected.anchoredPosition;
        DeselectThis();
    }

    void Start()
    {
        if (backgroundSelected == null)
        {
            Debug.LogError("BackgroundSelected atanmadı!");
            return;
        }

        backgroundSelected.anchoredPosition = originalPos;

        if (!tabGroups.ContainsKey(groupName))
        {
            tabGroups[groupName] = new List<TabController>();
        }
        tabGroups[groupName].Add(this);

        DeselectThis();
    }

    void OnDestroy()
    {
        if (tabGroups.ContainsKey(groupName))
        {
            tabGroups[groupName].Remove(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableHoverAnimation)
            return; // Hover modu aktifse tıklama animasyonu çalışmasın

        if (selectAndAutoDeselect)
        {
            foreach (var tab in tabGroups[groupName])
            {
                if (tab != this)
                    tab.DeselectThis();
            }

            backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration / 2f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    backgroundSelected.DOAnchorPosY(originalPos.y, duration / 2f).SetEase(Ease.InCubic);
                });
        }
        else
        {
            if (isSelected)
            {
                DeselectThis();
            }
            else
            {
                SelectThis();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableHoverAnimation && !isSelected)
        {
            isHovering = true;
            backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration / 2f).SetEase(Ease.OutCubic);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableHoverAnimation && !isSelected && isHovering)
        {
            isHovering = false;
            backgroundSelected.DOAnchorPosY(originalPos.y, duration / 2f).SetEase(Ease.InCubic);
        }
    }

    void SelectThis()
    {
        if (enableHoverAnimation)
            return; // Hover modu aktifse manuel seçim yapılmasın

        isSelected = true;

        foreach (var tab in tabGroups[groupName])
        {
            if (tab != this)
                tab.DeselectThis();
        }

        backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration).SetEase(Ease.OutCubic);
    }

    void DeselectThis()
    {
        isSelected = false;
        isHovering = false;

        backgroundSelected.DOAnchorPosY(originalPos.y, duration).SetEase(Ease.OutCubic);
    }
    private void OnDisable()
    {
        // Panel devre dışı bırakıldığında veya kapatıldığında
        if (backgroundSelected != null)
        {
            // Tüm DOTween animasyonlarını durdur
            backgroundSelected.DOKill(true); // 'true' ile animasyonu öldürür ve mevcut değeri bitirir (hedef değere ayarlar)

            // Pozisyonu doğrudan orijinaline sıfırla
            backgroundSelected.anchoredPosition = originalPos;

            // Seçim ve hover durumlarını sıfırla
            isSelected = false;
            isHovering = false;
        }
    }
}
