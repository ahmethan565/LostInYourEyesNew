using UnityEngine;
using TMPro;
using DG.Tweening;

public class InteractionUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactionText;

    private Tween currentTween;
    private string currentMessage = "";

    public void Show(string message)
    {
        if (message == currentMessage) return;
        currentMessage = message;

        interactionText.text = message;
        interactionText.DOKill();
        interactionText.alpha = 0;
        interactionText.transform.localScale = Vector3.one * 0.8f;

        Sequence seq = DOTween.Sequence();
        seq.Append(interactionText.DOFade(1, 0.2f));
        seq.Join(interactionText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        currentTween = seq;
    }

    public void Hide()
    {
        if (currentMessage == "") return;
        currentMessage = "";

        interactionText.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(interactionText.DOFade(0, 0.2f));
        seq.Join(interactionText.transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack));
        currentTween = seq;
    }
}
