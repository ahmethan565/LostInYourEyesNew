#pragma warning disable CS0414 // Kullanýlmayan alanlar için uyarýyý kapat

using UnityEngine;

/// <summary>
/// Tüm sahnelerde geçerli, profesyonel FPS sabitleme sistemi.
/// Editörde ve build sýrasýnda farklý limitler tanýmlanabilir.
/// </summary>
public class FPSLimiter : MonoBehaviour
{
    public static FPSLimiter Instance { get; private set; }

    [Header("FPS Ayarlarý")]
    [SerializeField] private int editorTargetFPS = 60;
    [SerializeField] private int buildTargetFPS = 60;
    [SerializeField] private bool limitWhenInBackground = true;
    [SerializeField] private int backgroundTargetFPS = 15;

    private void Awake()
    {
        // Singleton kontrolü
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyFPSLimit();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (limitWhenInBackground)
            ApplyFPSLimit();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (limitWhenInBackground)
            ApplyFPSLimit();
    }

    private void ApplyFPSLimit()
    {
        if (!Application.isPlaying) return;

#if UNITY_EDITOR
        Application.targetFrameRate = editorTargetFPS;
#else
        if (Application.isFocused)
            Application.targetFrameRate = buildTargetFPS;
        else
            Application.targetFrameRate = limitWhenInBackground ? backgroundTargetFPS : buildTargetFPS;
#endif

        if (QualitySettings.vSyncCount != 0)
        {
            Debug.LogWarning("VSync aktif. FPS limiti geçerli olmayabilir. VSync ayarýný kontrol edin.");
        }
    }
}

#pragma warning restore CS0414 // Uyarýyý tekrar aç
