using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    [Header("Combo Settings")]
    public float comboTimeout = 2f;               // Combo devam etmezse süre dolunca sıfırlanır
    public int[] comboMilestones = { 5, 10, 20, 50 }; // Efekt tetiklenecek değerler

    [Header("Effect Settings")]
    public Transform[] effectPositions;           // Efektin gösterileceği pozisyonlar

    [Header("Debug")]
    public int currentCombo = 0;
    private float comboTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (currentCombo > 0)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer >= comboTimeout)
                MissCombo();
        }
    }

    /// <summary>
    /// Doğru notaya basıldığında çağrılır. Combo'yu artırır ve milestone efektlerini tetikler.
    /// </summary>
    public void AddCombo()
    {
        currentCombo++;
        comboTimer = 0f;

        if (ShouldTriggerEffect(currentCombo))
        {
            PlayComboEffect();
        }

        // İstersen buraya skor/puan eklemesi gibi başka işlevler de eklenebilir
    }

    /// <summary>
    /// Yanlış nota basıldığında çağrılır. Combo'yu sıfırlar.
    /// </summary>
    public void MissCombo()
    {
        ResetCombo();
        StopComboEffects();
    }

    /// <summary>
    /// Combo’yu sıfırlar.
    /// </summary>
    public void ResetCombo()
    {
        currentCombo = 0;
        comboTimer = 0;
    }

    /// <summary>
    /// Combo milestone’a ulaşıldığında efekt tetiklenip tetiklenmeyeceğini kontrol eder.
    /// </summary>
    private bool ShouldTriggerEffect(int comboValue)
    {
        foreach (int milestone in comboMilestones)
        {
            if (comboValue == milestone)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Combo milestone efekti oynatır.
    /// </summary>
    private void PlayComboEffect()
    {
        foreach (var pos in effectPositions)
        {
            if (pos == null) continue;

            var fxPlayer = pos.GetComponent<ComboVFXPlayer>();
            if (fxPlayer != null)
            {
                fxPlayer.Play();  // Play ile aktif edilir ve animasyon başlar
            }
        }
    }

    public void StopComboEffects()
    {
        foreach (var pos in effectPositions)
        {
            if (pos == null) continue;

            var fxPlayer = pos.GetComponent<ComboVFXPlayer>();
            fxPlayer?.Stop();
        }
    }

}
