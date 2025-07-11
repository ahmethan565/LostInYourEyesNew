using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    // Inspector'dan atayacaðýnýz ayar scriptlerinin referanslarý.
    public ResolutionSelector resolutionSelector;
    public FullscreenToggle fullscreenToggle; // << BURASI DEÐÝÞMEDÝ >>
    //public GraphicsQualitySelector graphicsQualitySelector;

    public Button applyButton; // UI'daki "Apply" butonu

    void Awake()
    {
        // Önemli: Scriptlerin atanýp atanmadýðýný kontrol et.
        if (resolutionSelector == null) Debug.LogError("ResolutionSelector not assigned in SettingsManager!", this);
        if (fullscreenToggle == null) Debug.LogError("FullscreenToggle not assigned in SettingsManager!", this);
        //if (graphicsQualitySelector == null) Debug.LogError("GraphicsQualitySelector not assigned in SettingsManager!", this);
    }

    void OnEnable()
    {
        // Apply butonuna listener ekle. Birden fazla eklenmemesi için OnEnable/OnDisable kullanýlýyor.
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyAllSettings);
        }
        else
        {
            Debug.LogError("Apply Button is not assigned in SettingsManager! Please assign it in the Inspector.", this);
        }

        // Ayar menüsü açýldýðýnda (script etkinleþtiðinde) tüm ayarlarý yükle
        LoadAllSettings();
    }

    void OnDisable()
    {
        // Script devre dýþý býrakýldýðýnda listener'ý kaldýr
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyAllSettings);
        }
    }

    void OnApplicationQuit()
    {
        // Uygulama kapanýrken olasý son deðiþikliklerin kaydedildiðinden emin ol
        PlayerPrefs.Save();
    }

    // Tüm ayarlarý PlayerPrefs'ten yükler ve ilgili UI elemanlarýný günceller.
    public void LoadAllSettings()
    {
        Debug.Log("Loading all settings...");
        if (resolutionSelector != null) resolutionSelector.LoadResolution();
        if (fullscreenToggle != null) fullscreenToggle.LoadFullscreenMode(); // << BURASI DEÐÝÞTÝ >>
        //if (graphicsQualitySelector != null) graphicsQualitySelector.LoadQuality();
        Debug.Log("All settings loaded!");
    }

    // Tüm bekleyen ayarlarý oyuna uygular.
    public void ApplyAllSettings()
    {
        Debug.Log("Applying all settings...");

        // --- Görüntü Ayarlarýný Uygula (Çözünürlük ve Tam Ekran Birlikte) ---
        if (resolutionSelector != null && fullscreenToggle != null)
        {
            Resolution pendingRes = resolutionSelector.GetPendingResolution();
            FullScreenMode pendingMode = fullscreenToggle.GetPendingFullScreenMode(); // << BURASI DEÐÝÞTÝ >>

            Screen.SetResolution(pendingRes.width, pendingRes.height, pendingMode); // << BURASI DEÐÝÞTÝ >>
            Debug.Log($"Applied Screen Resolution: {pendingRes.width}x{pendingRes.height} in {pendingMode} mode.");
        }
        else
        {
            Debug.LogWarning("ResolutionSelector or FullscreenToggle not assigned. Screen resolution might not be applied correctly.", this);
            // Fallback: Yalnýzca biri atanmýþsa veya ikisi de atanmamýþsa, mevcut ayarlarý koruyarak uygula.
            if (resolutionSelector != null) resolutionSelector.ApplyResolution(); // ApplyResolution þimdilik boþ, ama belki gelecekte ek lojik olur.
            if (fullscreenToggle != null) fullscreenToggle.ApplyFullscreen(); // Ayný þekilde.
        }
        /*
        // --- Grafik Kalitesi Ayarlarýný Uygula ---
        if (graphicsQualitySelector != null)
        {
            graphicsQualitySelector.ApplyQuality();
        }
        */
        // Tüm ayarlar uygulandýktan sonra kaydet
        SaveAllSettings();

        Debug.Log("All settings applied successfully!");
    }

    // Tüm ayarlarý PlayerPrefs'e kaydeder.
    public void SaveAllSettings()
    {
        Debug.Log("Saving all settings...");
        if (resolutionSelector != null) resolutionSelector.SaveResolution();
        if (fullscreenToggle != null) fullscreenToggle.SaveFullscreen(); // << BURASI DEÐÝÞMEDÝ >>
        //if (graphicsQualitySelector != null) graphicsQualitySelector.SaveQuality();

        PlayerPrefs.Save(); // Tüm PlayerPrefs deðiþikliklerini diske yaz
        Debug.Log("All settings saved successfully!");
    }

    // Ýsteðe baðlý: Ayarlarý varsayýlanlara sýfýrlar.
    public void ResetToDefaults()
    {
        Debug.Log("Resetting all settings to defaults...");

        // PlayerPrefs'ten ilgili anahtarlarý sil
        PlayerPrefs.DeleteKey(ResolutionSelector.RESOLUTION_PREF_KEY);
        PlayerPrefs.DeleteKey(FullscreenToggle.FULLSCREEN_MODE_PREF_KEY); // << BURASI DEÐÝÞTÝ >>
        //PlayerPrefs.DeleteKey(GraphicsQualitySelector.QUALITY_PREF_KEY);
        PlayerPrefs.Save(); // Deðiþiklikleri hemen diske yaz

        // Ayarlarý yeniden yükle (bu, UI'yý varsayýlanlara geri döndürür)
        LoadAllSettings();

        // Ýsteðe baðlý: Varsayýlanlarý hemen oyuna uygula
        // ApplyAllSettings(); // Eðer sýfýrlama sonrasý hemen uygulansýn istenirse etkinleþtirin.

        Debug.Log("All settings reset to defaults!");
    }
}