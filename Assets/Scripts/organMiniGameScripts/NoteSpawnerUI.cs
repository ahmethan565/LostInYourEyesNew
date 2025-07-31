using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using DG.Tweening;
using System.Collections.Generic;

public class NoteSpawnerUI : MonoBehaviourPunCallbacks
{
    public static NoteSpawnerUI Instance;

    [Header("A")]
    public GameObject notePrefab;
    public Transform[] columns;
    public organCapsuleTrigger triggerCapsule;

    public string[] keysTexts = { "W", "A", "S", "D", "\u2190", "\u2191", "\u2192", "\u2193" };

    [Header("B")]
    public float spawnInterval;
    public bool isInvoking;
    public float waitTime;

    private float points;

    [Header("C")]
    public Note noteScript;

    public TMP_Text pointsText;

    private bool FPointsBool = false;
    private bool SPointsBool = false;
    private bool TPointsBool = false;
    private bool FoPointsBool = false;
    private bool escapeMenuOpen = false;
    private float beforeEscInterval;

    [Header("D")]
    public GameObject youWon;

    private Canvas canvas;

    [Header("E")]
    public GameObject Coin1;
    public GameObject Coin2;
    public GameObject Coin3;
    public GameObject Coin4;
    public Image fillingImage;

    public GameObject fullPointsPanel;
    public GameObject escapeMenuOrgan;
    public playerDetector playerDetector;

    [Header("Animation Settings")]
    public float coinAnimationDuration = 0.8f;
    public float fillAnimationDuration = 0.5f;
    public float pointsTextAnimationDuration = 0.3f;
    public float badgeScalePunch = 1.2f;

    [Header("Advanced Animation Settings")]
    public float coinBounceStrength = 1.3f;
    public float coinRotationAmount = 360f;
    public float fillEaseAmount = 0.8f;
    public float shakeStrength = 25f;
    public float shakeDuration = 0.4f;

    [Header("Effect Images")]
    public List<Image> effectImages; // Inspector'da atayacağın Image'lar

    // Animation tracking
    private float previousFillAmount = 0f;
    private Tween currentFillTween;

    public RectTransform canvasRectTransform;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        noteScript = GetComponent<Note>();
        StartSpawn();
        points = 0;
        canvas = GetComponentInParent<Canvas>();
        PhotonNetwork.AutomaticallySyncScene = true;

        // Initialize coins with zero scale for animations
        InitializeCoins();

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("organGameManager", Vector3.zero, Quaternion.identity);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            openEscMenu();
        }
    }

    public void AddPoints(float amount)
    {
        points += amount;
        if (points <= 0)
        {
            points = 0;
        }
        UpdateScoreUI();

        if (points >= 30 && !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Reached400"))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { "Reached400", true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        StartCoroutine(UpdateSpawnInterval());
    }

    /// <summary>
    /// Initialize coins with zero scale for entry animations
    /// </summary>
    private void InitializeCoins()
    {
        if (Coin1 != null) Coin1.transform.localScale = Vector3.zero;
        if (Coin2 != null) Coin2.transform.localScale = Vector3.zero;
        if (Coin3 != null) Coin3.transform.localScale = Vector3.zero;
        if (Coin4 != null) Coin4.transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Animate coin appearance with bounce and rotation
    /// </summary>
    /// <param name="coin">Coin GameObject to animate</param>
    private void AnimateCoinAppearance(GameObject coin)
    {
        if (coin == null) return;

        coin.SetActive(true);
        coin.transform.localScale = Vector3.zero;

        // Create sequence for coin animation
        Sequence coinSequence = DOTween.Sequence();

        // Scale bounce animation
        coinSequence.Append(coin.transform.DOScale(Vector3.one * coinBounceStrength, coinAnimationDuration * 0.6f)
            .SetEase(Ease.OutBack));
        coinSequence.Append(coin.transform.DOScale(Vector3.one, coinAnimationDuration * 0.4f)
            .SetEase(Ease.InOutQuad));

        // Rotation animation (parallel to scale)
        coin.transform.DORotate(new Vector3(0, 0, coinRotationAmount), coinAnimationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuart);

        // Add shake effect to the screen when coin appears
        ShakeScreen(shakeDuration * 0.5f, shakeStrength * 0.5f);
    }

    /// <summary>
    /// Animate coin disappearance
    /// </summary>
    /// <param name="coin">Coin GameObject to animate</param>
    private void AnimateCoinDisappearance(GameObject coin, System.Action onComplete = null)
    {
        if (coin == null)
        {
            onComplete?.Invoke();
            return;
        }

        Sequence coinSequence = DOTween.Sequence();

        // Scale down with rotation
        coinSequence.Append(coin.transform.DOScale(Vector3.zero, coinAnimationDuration * 0.7f)
            .SetEase(Ease.InBack));

        // Fade out if it has CanvasGroup
        CanvasGroup canvasGroup = coin.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            coinSequence.Join(canvasGroup.DOFade(0f, coinAnimationDuration * 0.7f));
        }

        coinSequence.OnComplete(() =>
        {
            coin.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Smooth fill animation for progress bar
    /// </summary>
    /// <param name="targetFillAmount">Target fill amount (0-1)</param>
    private void AnimateFillAmount(float targetFillAmount)
    {
        if (fillingImage == null) return;

        // Kill previous fill animation
        if (currentFillTween != null && currentFillTween.IsActive())
        {
            currentFillTween.Kill();
        }

        float currentFill = fillingImage.fillAmount;
        previousFillAmount = currentFill;

        // Create smooth fill animation
        currentFillTween = fillingImage.DOFillAmount(targetFillAmount, fillAnimationDuration)
            .SetEase(Ease.OutQuart)
            .OnUpdate(() =>
            {
                // Add subtle glow effect during fill
                if (fillingImage.fillAmount > previousFillAmount)
                {
                    // Positive progress - green tint
                    fillingImage.color = Color.Lerp(Color.white, Color.green, 0.2f);
                }
            })
            .OnComplete(() =>
            {
                // Reset color after animation
                fillingImage.DOColor(Color.white, 0.2f);
            });
    }

    /// <summary>
    /// Animate points text with punch scale effect
    /// </summary>
    private void AnimatePointsText()
    {
        if (pointsText == null) return;

        // Kill any existing animation
        pointsText.transform.DOKill();

        // Punch scale animation
        pointsText.transform.DOPunchScale(Vector3.one * 0.3f, pointsTextAnimationDuration, 10, 1f)
            .SetEase(Ease.OutQuart);

        // Color flash animation
        /*
        Color originalColor = pointsText.color;
        pointsText.DOColor(Color.yellow, pointsTextAnimationDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => pointsText.color = originalColor);
            */
    }

    /// <summary>
    /// Doğru/yanlış durumuna göre renk efekti uygular.
    /// </summary>
    /// <param name="isCorrect">Doğru ise true, yanlış ise false</param>
    public void PlayEffectOnImages(bool isCorrect)
    {
        Color effectColor = isCorrect ? Color.green : Color.red;
        float duration = 0.4f;

        foreach (var img in effectImages)
        {
            if (img == null) continue;

            // Renk animasyonunu yap, önce efektColor sonra eski renk (Yoyo)
            img.DOColor(effectColor, duration / 2).SetLoops(2, LoopType.Yoyo);
        }
    }

    IEnumerator UpdateSpawnInterval()
    {
        if (points >= 50 && FPointsBool == false)
        {
            // Animate coin appearance instead of just setting active
            AnimateCoinAppearance(Coin1);
            spawnInterval = 0.8f;
            RestartSpawn();
            FPointsBool = true;
            yield return new WaitForSeconds(waitTime);
        }
        else if (points >= 100 && SPointsBool == false)
        {
            // Animate coin appearance instead of just setting active
            AnimateCoinAppearance(Coin2);
            spawnInterval = 0.7f;
            RestartSpawn();
            SPointsBool = true;
            yield return new WaitForSeconds(waitTime);
        }
        else if ((points == 200 && TPointsBool == false) || (points == 205 && TPointsBool == false))
        {
            // Animate coin appearance instead of just setting active
            AnimateCoinAppearance(Coin3);
            spawnInterval = 0.6f;
            RestartSpawn();
            TPointsBool = true;
            yield return new WaitForSeconds(waitTime);
        }
        else if (points >= 400)
        {
            FullPointsFunction();
        }
    }

    void UpdateScoreUI()
    {
        if (pointsText != null)
        {
            pointsText.text = points + "X";
            // Animate points text
            AnimatePointsText();
        }

        if (fillingImage != null)
        {
            float targetFillAmount = 0f;

            if (0 <= points && points <= 55)
            {
                targetFillAmount = points / 285.7f;
            }
            else if (56 <= points && points <= 105)
            {
                targetFillAmount = points / 250f;
            }
            else if (106 <= points && points <= 205)
            {
                targetFillAmount = points / 303f;
            }
            else if (206 <= points)
            {
                targetFillAmount = points / 400f;
            }

            // Animate fill amount instead of setting directly
            AnimateFillAmount(targetFillAmount);
        }
    }

    public void ShakeScreen(float duration = 0.3f, float strength = 15f)
    {
        if (canvasRectTransform != null)
        {
            canvasRectTransform.DOShakePosition(duration, new Vector3(strength, strength * 0.7f, 0f), vibrato: 20, randomness: 90);
        }
    }

    void SpawnNote()
    {
        int columnIndex = Random.Range(0, columns.Length);
        int keyIndex = Random.Range(0, keysTexts.Length);

        GameObject newNote = Instantiate(notePrefab, columns[columnIndex]);

        newNote.GetComponentInChildren<TMP_Text>().text = keysTexts[keyIndex];
        newNote.GetComponent<Note>().assignedKey = (KeyType)keyIndex;

        newNote.transform.localPosition = new Vector3(0, 400f, 0);
    }

    void StartSpawn()
    {
        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);
        isInvoking = true;
    }

    void RestartSpawn()
    {
        if (isInvoking)
        {
            CancelInvoke(nameof(SpawnNote));
        }

        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);
        isInvoking = true;
    }

    void DestroyAllWithTag()
    {
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag("Note");

        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }
    }

    void FullPointsFunction()
    {
        if (!FoPointsBool)
        {
            fullPointsPanel.SetActive(true);
            spawnInterval = 60;
            DestroyAllWithTag();
            RestartSpawn();
            FoPointsBool = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Animate the final coin appearance with extra effects
            AnimateCoinAppearance(Coin4);

            // Animate you won panel with scale effect
            youWon.SetActive(true);
            youWon.transform.localScale = Vector3.zero;
            youWon.transform.DOScale(Vector3.one, 1f)
                .SetEase(Ease.OutElastic)
                .SetDelay(0.5f);

            // Extra celebration shake
            ShakeScreen(1f, shakeStrength * 1.5f);
        }
    }

    public void resumeAfterFullPoints()
    {
        fullPointsPanel.SetActive(false);
        spawnInterval = 0.6f;
        RestartSpawn();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void quitAfterFullPoints()
    {
        Destroy(canvas);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (playerDetector.playerController == null)
        {
            playerDetector.playerControllerSingle.moveSpeed = 15;
        }
        else
        {
            playerDetector.playerController.isMovementFrozen = false;
        }
    }

    public void openEscMenu()
    {
        beforeEscInterval = spawnInterval;
        spawnInterval = 60;
        RestartSpawn();
        DestroyAllWithTag();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        escapeMenuOrgan.SetActive(true);
        escapeMenuOpen = true;
    }

    public void resumeEscapeMenu()
    {
        escapeMenuOrgan.SetActive(false);
        spawnInterval = beforeEscInterval;
        RestartSpawn();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
