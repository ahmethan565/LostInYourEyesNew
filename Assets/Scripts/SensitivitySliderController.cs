using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class SensitivitySliderController : MonoBehaviour
{
    // Inspector'da atanacak bileþenler
    [Header("UI References")]
    [Tooltip("Bu slider'ýn baðlý olduðu UI Slider bileþeni.")]
    [SerializeField] private Slider uiSlider;
    [Tooltip("Hassasiyet deðerini gösteren TextMeshPro metni.")]
    [SerializeField] private TextMeshProUGUI valueText;
    [Tooltip("Dolu kýsmýn görseli (Image bileþenine ihtiyacýnýz var).")]
    [SerializeField] private Image fillImage; // *** YENÝ EKLENDÝ ***

    [Header("Slider Settings")]
    [Tooltip("Slider'ýn göstereceði minimum hassasiyet deðeri.")]
    [SerializeField] private float minSensitivity = 1f;
    [Tooltip("Slider'ýn göstereceði maksimum hassasiyet deðeri.")]
    [SerializeField] private float maxSensitivity = 10f;
    [Tooltip("Varsayýlan hassasiyet deðeri.")]
    [SerializeField] private float defaultSensitivity = 5f;

    [Header("Persistence")]
    [Tooltip("Hassasiyet deðerini kaydetmek ve yüklemek için PlayerPrefs anahtarý.")]
    [SerializeField] private string playerPrefsKey = "MasterSensitivity";

    [Header("Debugging")]
    [Tooltip("Bu, slider'ýn güncel deðerini Inspector'da görmenizi saðlar.")]
    [SerializeField]
    private float _currentSliderValue;

    // Hassasiyet deðeri deðiþtiðinde diðer scriptleri bilgilendirmek için Event
    public UnityEvent<float> OnSensitivityChanged;

    // Start metodu burada boþ býrakýlabilir veya baþlangýçta bir iþlem yapýyorsa kullanýlabilir.
    // Ancak, LoadSensitivity zaten OnEnable'da çaðrýldýðý için burada bir þeye gerek yok.
    private void Start()
    {
        // UpdateSliderVisual(defaultSensitivity); // Bu satýr LoadSensitivity içinde zaten hallediliyor
    }

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

        // FillImage referansýný da Awake'de kontrol et ve otomatik bulmaya çalýþ
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


        uiSlider.minValue = minSensitivity;
        uiSlider.maxValue = maxSensitivity;

        if (OnSensitivityChanged == null)
        {
            OnSensitivityChanged = new UnityEvent<float>();
        }
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

        LoadSensitivity();
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
        SaveSensitivity(newValue);
        OnSensitivityChanged.Invoke(newValue);
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
            // Slider'ýn deðerini (minSensitivity-maxSensitivity aralýðýnda) 0-1 aralýðýna dönüþtür
            float fillAmount = Mathf.InverseLerp(minSensitivity, maxSensitivity, currentValue);
            fillImage.fillAmount = fillAmount; // Görüntünün ne kadarýnýn dolu olacaðýný ayarla
        }
        else
        {
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - fillImage is NULL. Fill visual won't work.");
        }
    }

    // --- Kaydetme/Yükleme Metotlarý ---
    private void LoadSensitivity()
    {
        float loadedValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultSensitivity);

        if (uiSlider != null)
        {
            // Yüklenen deðeri slider'a ayarla.
            // uiSlider.value'yu set etmek OnSliderValueChanged'ý tetikleyecektir.
            // Bu, _currentSliderValue'nun ve görselin de güncellenmesini saðlar.
            uiSlider.value = loadedValue;
        }
        else
        {
            Debug.LogError($"LoadSensitivity: {gameObject.name} - UI Slider is NULL, cannot set value during load.");
        }
    }

    private void SaveSensitivity(float valueToSave)
    {
        PlayerPrefs.SetFloat(playerPrefsKey, valueToSave);
        PlayerPrefs.Save();
    }
}