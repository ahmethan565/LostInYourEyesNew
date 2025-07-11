using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class VolumeSliderController : MonoBehaviour
{
    // Inspector'da atanacak bileþenler
    [Header("UI References")]
    [Tooltip("Bu slider'ýn baðlý olduðu UI Slider bileþeni.")]
    [SerializeField] private Slider uiSlider;
    [Tooltip("Ses seviyesini gösteren TextMeshPro metni.")]
    [SerializeField] private TextMeshProUGUI valueText;

    // Fill görselinin RectTransform'u yerine, Image bileþenine ihtiyacýmýz var.
    [Tooltip("Dolu kýsmýn görseli (fillImageRect yerine Image bileþenine ihtiyacýnýz var).")]
    [SerializeField] private Image fillImage; // *** BURASI DEÐÝÞTÝ ***

    [Header("Audio References")]
    [Tooltip("Bu slider'ýn kontrol edeceði AudioSource.")]
    [SerializeField] private AudioSource targetAudioSource;
    [Tooltip("Bu slider'ýn kontrol edeceði AudioMixerGroup parametresi.")]
    [SerializeField] private AudioMixerGroup targetAudioMixerGroup;
    [Tooltip("AudioMixer'daki ses parametresinin adý (örneðin 'MasterVolume').")]
    [SerializeField] private string mixerParameterName = "MasterVolume";

    [Header("Slider Settings")]
    [Tooltip("Slider'ýn göstereceði minimum ses deðeri (genellikle 0).")]
    [SerializeField] private float minVolume = 0f;
    [Tooltip("Slider'ýn göstereceði maksimum ses deðeri (genellikle 100).")]
    [SerializeField] private float maxVolume = 100f;

    // maxFillWidth artýk gerekli deðil, çünkü Image.fillAmount kullanacaðýz.
    // [Tooltip("Görsel dolgunun maksimum geniþliði (varsayýlan fillImage'in boyutu kullanýlabilir).")]
    // [SerializeField] private float maxFillWidth = 300f; 

    [Header("Persistence")]
    [Tooltip("Ses seviyesi deðerini kaydetmek ve yüklemek için PlayerPrefs anahtarý.")]
    [SerializeField] private string playerPrefsKey = "MasterVolume";
    [Tooltip("Varsayýlan ses seviyesi deðeri.")]
    [SerializeField] private float defaultVolume = 75f;

    [Header("Debugging")]
    [Tooltip("Bu, slider'ýn güncel deðerini Inspector'da görmenizi saðlar.")]
    [SerializeField]
    private float _currentSliderValue;

    // --- Baþlangýç ve Olay Atamalarý ---
    private void Awake()
    {
        if (uiSlider == null)
        {
            uiSlider = GetComponent<Slider>();
            if (uiSlider == null)
            {
                Debug.LogError($"Awake: {gameObject.name} - UI Slider not found on this GameObject or not assigned in Inspector! Please assign the Slider component.");
                return;
            }
        }

        // FillImage referansýný da Awake'de kontrol edebiliriz
        if (fillImage == null)
        {
            // Genellikle slider'ýn altýndaki "Fill" objesinde Image bileþeni bulunur
            Transform fillTransform = uiSlider.fillRect;
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
            }
            if (fillImage == null)
            {
                Debug.LogWarning($"Awake: {gameObject.name} - Fill Image component not found or assigned! Visual fill won't work correctly.");
            }
        }


        uiSlider.minValue = minVolume;
        uiSlider.maxValue = maxVolume;
    }

    private void OnEnable()
    {
        if (uiSlider != null)
        {
            uiSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        else
        {
            Debug.LogError($"OnEnable: {gameObject.name} - UI Slider is NULL, cannot add listener!");
            return;
        }

        LoadVolume();
    }

    private void OnDisable()
    {
        if (uiSlider != null)
        {
            uiSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    // --- Olay Dinleyici Metotlarý ---
    private void OnSliderValueChanged(float newValue)
    {
        _currentSliderValue = newValue;

        UpdateSliderVisual(newValue);
        SaveVolume(newValue); // Ses seviyesini kaydet

        float audioVolume = newValue / maxVolume;

        if (targetAudioSource != null)
        {
            targetAudioSource.volume = audioVolume;
        }
        else if (targetAudioMixerGroup != null)
        {
            float dbValue = Mathf.Log10(audioVolume) * 20;
            if (audioVolume <= 0.0001f)
            {
                dbValue = -80f;
            }
            targetAudioMixerGroup.audioMixer.SetFloat(mixerParameterName, dbValue);
        }
        else
        {
            Debug.LogWarning($"OnSliderValueChanged: {gameObject.name} - No AudioSource or AudioMixerGroup assigned. Slider only updates visuals and saves preference.");
        }
    }

    // --- Görsel Güncelleme Metodu ---
    private void UpdateSliderVisual(float currentValue)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(currentValue).ToString();
        }
        else
        {
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - valueText is NULL. Cannot update text.");
        }

        // *** BURASI DEÐÝÞTÝ: fillImage'in fillAmount'unu kullanýyoruz ***
        if (fillImage != null)
        {
            // Slider'ýn 0-100 deðerini 0-1 aralýðýna dönüþtür
            float fillAmount = Mathf.Clamp01(currentValue / maxVolume);
            fillImage.fillAmount = fillAmount; // Görüntünün ne kadarýnýn dolu olacaðýný ayarla
        }
        else
        {
            // fillImage atandýðýndan emin olmak için bir uyarý ekleyebiliriz
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - fillImage is NULL. Fill visual won't work.");
        }
    }

    // --- Kaydetme/Yükleme Metotlarý ---
    private void LoadVolume()
    {
        float loadedValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultVolume);

        if (targetAudioSource != null)
        {
            if (!PlayerPrefs.HasKey(playerPrefsKey))
            {
                loadedValue = targetAudioSource.volume * maxVolume;
            }
        }
        else if (targetAudioMixerGroup != null)
        {
            float mixerDbValue;
            if (targetAudioMixerGroup.audioMixer.GetFloat(mixerParameterName, out mixerDbValue))
            {
                if (!PlayerPrefs.HasKey(playerPrefsKey))
                {
                    float normalizedValue = Mathf.Pow(10, mixerDbValue / 20);
                    loadedValue = normalizedValue * maxVolume;
                    if (mixerDbValue <= -79f)
                    {
                        loadedValue = minVolume;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"LoadVolume: {gameObject.name} - AudioMixer parameter '{mixerParameterName}' not found. Using PlayerPrefs or default.");
            }
        }
        else
        {
            // Hiçbir ses kaynaðý baðlý deðilse, sadece PlayerPrefs veya varsayýlan deðeri kullan
        }

        if (uiSlider != null)
        {
            uiSlider.value = loadedValue;
        }
        else
        {
            Debug.LogError($"LoadVolume: {gameObject.name} - UI Slider is NULL, cannot set value during load.");
        }
    }

    private void SaveVolume(float valueToSave)
    {
        PlayerPrefs.SetFloat(playerPrefsKey, valueToSave);
        PlayerPrefs.Save();
    }
}