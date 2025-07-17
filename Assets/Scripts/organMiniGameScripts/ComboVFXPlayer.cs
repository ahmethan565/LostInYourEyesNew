using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ComboVFXPlayer : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 12f;
    public bool loop = false;

    private Image img;
    private float timer;
    private int currentFrame;

    private Sequence effectSequence;

    void Awake()
    {
        img = GetComponent<Image>();
        gameObject.SetActive(false);
        enabled = false;
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = 0;
        timer = 0f;
        img.sprite = frames[0];
        gameObject.SetActive(true);
        enabled = true;

        // Başlangıçta şeffaf yap
        Color c = img.color;
        c.a = 0f;
        img.color = c;

        // Fade in 0.3 saniyede
        img.DOFade(1f, 0.3f).SetUpdate(true);

        // Hafif titreşim
        transform.DOShakePosition(0.2f, 5f, vibrato: 10, randomness: 45).SetUpdate(true);
    }


    public void Stop()
    {
        effectSequence?.Kill();
        ShakeScreen(0.2f, 4f);
        // Fade out yap, sonra kapat
        img.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() =>
        {
            enabled = false;
            gameObject.SetActive(false);
            currentFrame = 0;
            timer = 0f;
        });
    }

    public void ShakeScreen(float duration = 0.3f, float strength = 15f)
    {

        this.transform.DOShakePosition(0.5f, new Vector3(30f, 20f, 0f), vibrato: 20, randomness: 90);

    }
    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    Stop();
                    return;
                }
            }
            img.sprite = frames[currentFrame];
        }
    }

}
