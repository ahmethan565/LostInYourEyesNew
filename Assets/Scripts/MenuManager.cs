using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Play butonuna basýlýnca Lobby sahnesini yükleyecek

    [Header("Panels")]
    [SerializeField] private GameObject optionsMenuPanel;

    [Header("Tab Panels")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    private GameObject currentActivePanel;

    [Header("System Infos")]
    [SerializeField] private TextMeshProUGUI displayAdapterText;
    [SerializeField] private TextMeshProUGUI monitorText;

    void Start()
    {
        optionsMenuPanel.SetActive(false);

        // Sistem bilgilerini göster
        if (displayAdapterText != null)
            displayAdapterText.text = SystemInfo.graphicsDeviceName;

        Resolution res = Screen.currentResolution;
        double hz = res.refreshRateRatio.value;
        if (monitorText != null)
            monitorText.text = $"{res.width}x{res.height} @ {(int)hz}Hz";
    }

    private void Update()
    {
        // Ýsteðe baðlý: ESC'ye basýnca kapansýn mý?
        if (Input.GetKeyDown(KeyCode.Escape) && optionsMenuPanel.activeSelf)
        {
            CloseOptions();
        }
    }

    // Ayarlar panelini aç
    public void OpenOptions()
    {
        optionsMenuPanel.SetActive(true);
        ShowPanel(displayPanel); // Varsayýlan olarak Display paneli açýlsýn
        Debug.Log("Ana menüden Ayarlar Açýldý");
    }

    // Ayarlar panelini kapat
    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        Debug.Log("Ana menüden Ayarlar Kapatýldý");
    }

    // Sekmeler (tab'lar)
    public void OnDisplayButton() => ShowPanel(displayPanel);
    public void OnGraphicsButton() => ShowPanel(graphicsPanel);
    public void OnAudioButton() => ShowPanel(audioPanel);
    public void OnControlsButton() => ShowPanel(controlsPanel);

    private void ShowPanel(GameObject panelToShow)
    {
        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        panelToShow.SetActive(true);
        currentActivePanel = panelToShow;
    }


    public void OnPlayButton()
    {
        SceneManager.LoadScene("Lobby");
    }

    // Quit butonuna basýlýnca oyundan çýk
    public void OnQuitButton()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
