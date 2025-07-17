using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Note : MonoBehaviour
{
    public KeyType assignedKey;
    public float speed = 400f;

    private float missThresholdY;
    public float missDetectFloat;

    [Header("missPoint")]
    public int missPoint = -5;

    private bool hasBeenMissed = false;

    void Start()
    {
        Transform column = transform.parent;
        Transform hitZone = column.Find("HitZone");

        if (hitZone != null)
        {
            float hitY = hitZone.position.y;
            missThresholdY = hitY - missDetectFloat;
        }
        else
        {
            Debug.LogWarning("HitZone not found: " + column.name);
            missThresholdY = -100f;
        }
    }

    void Update()
    {
        if (this == null || !gameObject.activeSelf || hasBeenMissed) return;

        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < missThresholdY)
        {
            HandleMiss();
        }
    }

    private void HandleMiss()
    {
        if (hasBeenMissed) return;
        hasBeenMissed = true;

        NoteSpawnerUI.Instance.AddPoints(missPoint);
        NoteSpawnerUI.Instance?.ShakeScreen(0.3f, 7f);

        FeedbackUIController.Instance?.ShowFeedback(Color.red, assignedKey);
        NoteSpawnerUI.Instance.PlayEffectOnImages(false);

        ComboManager.Instance?.MissCombo(); // Combo sıfırla

        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white;

            Sequence seq = DOTween.Sequence();

            seq.Append(img.DOColor(Color.red, 0.15f).SetEase(Ease.OutQuad));
            seq.Join(transform.DORotate(new Vector3(0, 0, -15f), 0.25f).SetEase(Ease.OutBack));

            // Burada ufak bir shake ekliyoruz, hem pozisyon hem rotasyon
            seq.Append(transform.DOShakePosition(0.3f, strength: new Vector3(5f, 2f, 0f), vibrato: 10, randomness: 90));
            seq.Join(transform.DOShakeRotation(0.3f, strength: 10f, vibrato: 10));


            seq.Append(transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack));
            seq.Append(transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack));
            seq.Join(img.DOFade(0, 0.4f).SetEase(Ease.InQuad));
            seq.Join(transform.DORotate(Vector3.zero, 0.4f).SetEase(Ease.InBack));
            seq.OnComplete(() => Destroy(gameObject));
            seq.SetLink(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void HandleHitEffect()
    {
        if (this == null || !gameObject.activeSelf || hasBeenMissed) return;
        hasBeenMissed = true;

        //NoteSpawnerUI.Instance.PlayEffectOnImages(true);
        ComboManager.Instance?.AddCombo();

        Image img = GetComponent<Image>();
        if (img == null)
        {
            Destroy(gameObject);
            return;
        }

        Color originalColor = img.color;
        Vector3 originalScale = transform.localScale;

        Sequence seq = DOTween.Sequence();

        //seq.Append(img.DOColor(Color.green, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale * 1.4f, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DORotate(new Vector3(0, 0, 10f), 0.2f).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(originalScale * 0.9f, 0.15f).SetEase(Ease.InBack));
        seq.Join(transform.DORotate(new Vector3(0, 0, -8f), 0.15f).SetEase(Ease.InBack));
        seq.Append(transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutBack));
        seq.Join(transform.DORotate(Vector3.zero, 0.15f).SetEase(Ease.OutBack));
        seq.Append(img.DOColor(originalColor, 0.3f).SetEase(Ease.InOutSine));
        seq.Join(img.DOFade(0, 0.3f));
        seq.OnComplete(() => Destroy(gameObject));
        seq.SetLink(gameObject);
    }
}
