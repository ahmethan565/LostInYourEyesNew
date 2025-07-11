using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ResolutionSelector : MonoBehaviour
{
    public TextMeshProUGUI resolutionText;
    public Button previousResolutionButton;
    public Button nextResolutionButton;

    private List<Resolution> uniqueResolutions; // Baþlangýçta null olarak kalabilir
    private int pendingResolutionIndex = 0;
    public const string RESOLUTION_PREF_KEY = "ResolutionIndex";

    [Header("Resolution Filtering")]
    [Tooltip("Hedeflenen en boy oraný (örn. 16:9 için 1.777f, 16:10 için 1.6f)")]
    public float targetAspectRatio = 16f / 9f;
    [Tooltip("En boy oraný için kabul edilebilir sapma (örn. 0.01f)")]
    public float aspectRatioTolerance = 0.01f;
    [Tooltip("Minimum geniþlik çözünürlüðü")]
    public int minWidth = 1280;
    [Tooltip("Minimum yükseklik çözünürlüðü")]
    public int minHeight = 720;

    [Header("Custom Resolutions (Bypass Filtering)")]
    [Tooltip("Filtrelemeye tabi tutulmadan eklenecek özel çözünürlükler (geniþlik x yükseklik)")]
    public List<Vector2Int> customResolutions = new List<Vector2Int>();


    void Awake()
    {
        if (resolutionText == null) { Debug.LogError("Resolution Text is not assigned in ResolutionSelector!", this); return; }
        if (previousResolutionButton == null) { Debug.LogError("Previous Resolution Button is not assigned in ResolutionSelector!", this); return; }
        if (nextResolutionButton == null) { Debug.LogError("Next Resolution Button is not assigned in ResolutionSelector!", this); return; }

        // PopulateResolutionList(); // Awake'den kaldýrdýk, çünkü LoadResolution/GetPendingResolution içinde kontrol edeceðiz
        // Ama yine de bu scriptin UI'ý göstermesi için baþlatýlmasý lazým.
        // Eðer bu objenin her zaman aktif olacaðýný biliyorsanýz, Awake'de kalmasý sorun deðil.
        // Güvenli olmasý için, ilk ihtiyaç duyulduðunda doldurulmasýný saðlayacaðýz.
    }

    void OnEnable()
    {
        previousResolutionButton.onClick.AddListener(PreviousResolution);
        nextResolutionButton.onClick.AddListener(NextResolution);

        // Buradan LoadResolution çaðrýlýyor, o yüzden LoadResolution içinde doldurma kontrolü önemli
        // LoadResolution(); // SettingsManager'dan çaðrýldýðýnda çalýþacak, bu kýsmý SettingsManager yönetiyor.
        // UpdateResolutionUI(); // UI'ý baþlangýçta güncel tutmak için yine de çaðýrabiliriz, ama LoadResolution da bunu yapýyor.
    }

    void OnDisable()
    {
        previousResolutionButton.onClick.RemoveListener(PreviousResolution);
        nextResolutionButton.onClick.RemoveListener(NextResolution);
    }

    private void PopulateResolutionList()
    {
        uniqueResolutions = Screen.resolutions
            .Where(res => {
                float currentAspectRatio = (float)res.width / res.height;
                bool meetsAspectRatio = Mathf.Abs(currentAspectRatio - targetAspectRatio) < aspectRatioTolerance;
                bool meetsMinDimensions = res.width >= minWidth && res.height >= minHeight;
                return meetsAspectRatio && meetsMinDimensions;
            })
            .GroupBy(res => new { res.width, res.height })
            .Select(group => group.OrderByDescending(res => res.refreshRateRatio).First())
            .OrderBy(res => res.width * res.height)
            .ToList();

        // Custom resolution'larý ekle (filtrelemeden)
        foreach (var vec in customResolutions)
        {
            if (!uniqueResolutions.Any(r => r.width == vec.x && r.height == vec.y))
            {
                Resolution custom = new Resolution
                {
                    width = vec.x,
                    height = vec.y,
                    refreshRate = 60 // varsayýlan bir deðer, deðiþtirilebilir
                };
                uniqueResolutions.Add(custom);
            }
        }

        // Listeyi yeniden sýrala
        uniqueResolutions = uniqueResolutions
            .OrderBy(res => res.width * res.height)
            .ToList();

        if (uniqueResolutions.Count == 0)
        {
            Debug.LogError("No unique resolutions found after filtering! Check your filter settings. Adding current screen resolution as fallback.", this);
            uniqueResolutions.Add(Screen.currentResolution);
        }
    }

    private void UpdateResolutionUI()
    {
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            resolutionText.text = "N/A"; // Çözünürlük bulunamazsa
            previousResolutionButton.interactable = false;
            nextResolutionButton.interactable = false;
            return;
        }

        Resolution selectedRes = uniqueResolutions[pendingResolutionIndex];
        resolutionText.text = $"{selectedRes.width}x{selectedRes.height}";

        previousResolutionButton.interactable = pendingResolutionIndex > 0;
        nextResolutionButton.interactable = pendingResolutionIndex < uniqueResolutions.Count - 1;
    }

    public void PreviousResolution()
    {
        // uniqueResolutions'ýn dolu olduðundan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) { PopulateResolutionList(); }
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) return; // Hala boþsa iþlem yapma

        if (pendingResolutionIndex > 0)
        {
            pendingResolutionIndex--;
            UpdateResolutionUI();
            Debug.Log($"Resolution pending: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
        }
    }

    public void NextResolution()
    {
        // uniqueResolutions'ýn dolu olduðundan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) { PopulateResolutionList(); }
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) return; // Hala boþsa iþlem yapma

        if (pendingResolutionIndex < uniqueResolutions.Count - 1)
        {
            pendingResolutionIndex++;
            UpdateResolutionUI();
            Debug.Log($"Resolution pending: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
        }
    }

    // SettingsManager tarafýndan çaðrýlacak: Seçilen çözünürlüðü döndürür.
    public Resolution GetPendingResolution()
    {
        // uniqueResolutions'ýn dolu olduðundan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            PopulateResolutionList();
            if (uniqueResolutions == null || uniqueResolutions.Count == 0)
            {
                Debug.LogWarning("Resolution list is empty even after attempting to populate. Returning current screen resolution.", this);
                return Screen.currentResolution;
            }
        }

        if (pendingResolutionIndex >= 0 && pendingResolutionIndex < uniqueResolutions.Count)
        {
            return uniqueResolutions[pendingResolutionIndex];
        }
        Debug.LogWarning("Pending resolution index out of bounds. Returning current screen resolution.", this);
        return Screen.currentResolution;
    }

    // --- Kalýcýlýk ve Uygulama Metotlarý ---

    public void LoadResolution()
    {
        // uniqueResolutions'ýn dolu olduðundan emin ol <-- BURADAKÝ DEÐÝÞÝKLÝK KRÝTÝK
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            PopulateResolutionList();
            if (uniqueResolutions == null || uniqueResolutions.Count == 0)
            {
                Debug.LogError("Failed to populate uniqueResolutions list. Cannot load resolution.", this);
                return; // Listeyi dolduramazsak ilerleyemeyiz.
            }
        }

        // Kaydedilmiþ çözünürlük indeksini yükle. Yoksa varsayýlan olarak mevcut ekran çözünürlüðünü bul.
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, -1);
        // Satýr 117'deki hata, uniqueResolutions.Count'a eriþmeden önce uniqueResolutions'ýn null olmamasýný garanti etmediðimiz içindi.
        if (savedIndex != -1 && savedIndex < uniqueResolutions.Count)
        {
            pendingResolutionIndex = savedIndex;
        }
        else
        {
            // Kayýtlý ayar yoksa, mevcut sistem çözünürlüðünün indeksini bul
            pendingResolutionIndex = uniqueResolutions.FindIndex(res =>
                res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height);
            if (pendingResolutionIndex == -1) pendingResolutionIndex = 0;
        }
        UpdateResolutionUI();
        Debug.Log($"Resolution Loaded: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
    }

    public void ApplyResolution()
    {
        Debug.Log("ResolutionSelector.ApplyResolution() called. Actual resolution change coordinated by SettingsManager.");
    }

    public void SaveResolution()
    {
        PlayerPrefs.SetInt(RESOLUTION_PREF_KEY, pendingResolutionIndex);
        Debug.Log($"Resolution Saved: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
    }
}