using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kullanýyorsanýz. Eðer Text kullanýyorsanýz UnityEngine.UI.Text olarak deðiþtirin.
using System.Collections.Generic;

public class FullscreenToggle : MonoBehaviour
{
    public TextMeshProUGUI fullscreenModeText; // Tam ekran modunu gösterecek metin alaný
    public Button leftButton; // Önceki moda git butonu
    public Button rightButton; // Sonraki moda git butonu

    private List<FullScreenMode> availableModes;
    private int currentIndex = 0; // Kullanýcýnýn seçtiði ama henüz uygulamadýðý mod indeksi
    public const string FULLSCREEN_MODE_PREF_KEY = "FullscreenModeIndex"; // PlayerPrefs anahtarý

    void Awake()
    {
        // Null referans kontrolleri
        if (fullscreenModeText == null) { Debug.LogError("Fullscreen Mode Text is not assigned in FullscreenToggle!", this); return; }
        if (leftButton == null) { Debug.LogError("Left Button is not assigned in FullscreenToggle!", this); return; }
        if (rightButton == null) { Debug.LogError("Right Button is not assigned in FullscreenToggle!", this); return; }

        PopulateModesList(); // Mevcut mod seçeneklerini doldur
    }

    void OnEnable()
    {
        // Butonlara listener ekle
        leftButton.onClick.AddListener(PreviousMode);
        rightButton.onClick.AddListener(NextMode);

        //LoadFullscreenMode(); // Script etkinleþtiðinde ayarý yükle ve UI'yý güncelle
        UpdateFullscreenUI(); // UI'yý baþlangýçta güncel tut
    }

    void OnDisable()
    {
        // Script devre dýþý býrakýldýðýnda listener'larý kaldýr
        leftButton.onClick.RemoveListener(PreviousMode);
        rightButton.onClick.RemoveListener(NextMode);
    }

    private void PopulateModesList()
    {
        availableModes = new List<FullScreenMode>
        {
            FullScreenMode.ExclusiveFullScreen, // Özel Tam Ekran (genellikle eski oyunlarda veya yüksek performans istenen yerlerde)
            FullScreenMode.FullScreenWindow,    // Penceresiz Tam Ekran (Borderless Window - modern oyunlarda yaygýn)
            FullScreenMode.Windowed             // Pencereli Mod
        };

        // Eðer sistem mevcut FullScreenMode'u desteklemiyorsa bu listeyi geniþletilebilir
        // Örneðin, sadece FullScreenWindow ve Windowed modlarý yeterliyse:
        // availableModes = new List<FullScreenMode> { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
    }

    private void UpdateFullscreenUI()
    {
        if (availableModes == null || availableModes.Count == 0) return;

        // Geçerli seçili modu metin alanýna yaz
        string modeName = GetModeName(availableModes[currentIndex]);
        fullscreenModeText.text = modeName;

        // Butonlarý etkin/devre dýþý býrakarak sýnýrlarý göster
        leftButton.interactable = currentIndex > 0;
        rightButton.interactable = currentIndex < availableModes.Count - 1;
    }

    private string GetModeName(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return "Fullscreen"; // Ya da "Özel Tam Ekran"
            case FullScreenMode.FullScreenWindow:
                return "Windowed Fullscreen"; // Ya da "Borderless"
            case FullScreenMode.Windowed:
                return "Windowed";
            default:
                return "Null??";
        }
    }

    public void PreviousMode()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateFullscreenUI();
            Debug.Log($"Fullscreen mode pending: {GetModeName(availableModes[currentIndex])}");
        }
    }

    public void NextMode()
    {
        if (currentIndex < availableModes.Count - 1)
        {
            currentIndex++;
            UpdateFullscreenUI();
            Debug.Log($"Fullscreen mode pending: {GetModeName(availableModes[currentIndex])}");
        }
    }

    // SettingsManager tarafýndan çaðrýlacak: Seçilen tam ekran modunu döndürür.
    public FullScreenMode GetPendingFullScreenMode()
    {
        if (availableModes == null || availableModes.Count == 0)
        {
            Debug.LogWarning("Fullscreen mode list is empty. Returning current full screen mode.", this);
            return Screen.fullScreenMode;
        }

        if (currentIndex >= 0 && currentIndex < availableModes.Count)
        {
            return availableModes[currentIndex];
        }
        Debug.LogWarning("Pending fullscreen mode index out of bounds. Returning current full screen mode.", this);
        return Screen.fullScreenMode;
    }

    // --- Kalýcýlýk ve Uygulama Metotlarý ---

    public void LoadFullscreenMode()
    {
        if (availableModes == null || availableModes.Count == 0)
        {
            PopulateModesList(); // Liste henüz yoksa oluþtur
        }

        int savedIndex = PlayerPrefs.GetInt(FULLSCREEN_MODE_PREF_KEY, -1);
        if (savedIndex != -1 && savedIndex < availableModes.Count)
        {
            currentIndex = savedIndex;
        }
        else
        {
            currentIndex = availableModes.FindIndex(mode => mode == Screen.fullScreenMode);
            if (currentIndex == -1) currentIndex = 0;
        }

        UpdateFullscreenUI();
        Debug.Log($"Fullscreen Mode Loaded: {GetModeName(availableModes[currentIndex])}");
    }


    public void ApplyFullscreen()
    {
        // Gerçek tam ekran deðiþikliði SettingsManager tarafýndan çözünürlük ile birlikte koordine edilecek.
        // Bu metod FullscreenToggle'ýn "uygulama" fazýnda olduðunu belirtir.
        Debug.Log("FullscreenToggle.ApplyFullscreen() called. Actual fullscreen change coordinated by SettingsManager.");
    }

    public void SaveFullscreen()
    {
        PlayerPrefs.SetInt(FULLSCREEN_MODE_PREF_KEY, currentIndex);
        Debug.Log($"Fullscreen Mode Saved: {GetModeName(availableModes[currentIndex])}");
    }
}