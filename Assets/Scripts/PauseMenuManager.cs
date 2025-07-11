using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    // Singleton Instance
    private static PauseMenuManager _instance;
    public static PauseMenuManager Instance // Global erişim noktası
    {
        get
        {
            // Eğer instance yoksa bulmaya çalış
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PauseMenuManager>();

                // Hala yoksa hata ver (sahneye eklenmemiş demektir)
                if (_instance == null)
                {
                    Debug.LogError("PauseMenuManager instance sahnede bulunamadı!");
                }
            }
            return _instance;
        }
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainEscPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private GameObject UiBackground;

    [Header("Tab Panels")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    private GameObject currentActivePanel; // SerializeField olmasına gerek yok

    [Header("SystemInfos")]
    [SerializeField] private TextMeshProUGUI displayAdapterText;
    [SerializeField] private TextMeshProUGUI monitorText;

    // MENÜ DURUMU ARTIK PUBLIC
    public bool isMenuOpen = false;

    void Awake()
    {
        // Singleton kurulumu: Sadece bir instance olmasına izin ver
        if (_instance != null && _instance != this)
        {
            // Zaten bir instance varsa bu yenisini yok et
            Destroy(this.gameObject);
        }
        else
        {
            // Bu ilk instance ise onu _instance olarak ayarla
            _instance = this;
            // İsteğe bağlı: Sahneler arası geçişte yok olmaması için
            // DontDestroyOnLoad(gameObject);
        }
    }


    void Start()
    {
        // Başlangıçta tüm menü panellerinin kapalı olduğundan emin olalım
        UiBackground.SetActive(false);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false);
        quitConfirmationPanel.SetActive(false);

        // Ekran kartı adı
        string gpuName = SystemInfo.graphicsDeviceName;
        if (displayAdapterText != null) displayAdapterText.text = gpuName;

        // Monitör bilgisi (çözünürlük + tazeleme oranı)
        Resolution res = Screen.currentResolution;
        // RefreshRateRatio kullanmak daha doğru, eski refreshRate int
        double hz = res.refreshRateRatio.value;

        string monitorInfo = $"{res.width}x{res.height} @ {(int)hz}Hz";  // örnek çıktı: 1920x1080 @ 144Hz, int'e çevirince küsuratı atarız
        if (monitorText != null) monitorText.text = monitorInfo;
    }

    void Update()
    {
        // Sadece yerel oyuncu ESC menüsünü açabilmeli.
        // Bu script sahnede her zaman olduğu için IsMine kontrolünü burada yapamayız.
        // ESC tuşuna basıldığında menüyü aç/kapa logic'i her zaman çalışmalı,
        // ancak karakterin hareketinin durdurulması FPSPlayerController içinde IsMine kontrolü ile yapılacak.
        // Eğer menü açma/kapama sadece yerel oyuncu için olmalıysa, bu Update metodunun
        // FPSPlayerController'daki IsMine bloğuna taşınması daha mantıklı olur.
        // Ama menü yönetimi UI scriptinde kalabilir, sadece ToggleMainEscMenu metodu çağrılır.

        // ESC tuşuna basıldığında menü navigasyonu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitConfirmationPanel.activeSelf) // Önce onay paneli açık mı?
            {
                CancelQuit(); // Onay panelini kapat
            }
            else if (optionsMenuPanel.activeSelf) // Seçenekler paneli açık mı?
            {
                HideOptionsPanelAndShowMainEsc(); // Seçenekleri kapat, ana menüye dön
            }
            else // Hiçbiri açık değilse ana menüyü aç/kapa
            {
                // ESC tuşuna basıldığında ana menüyü sadece yerel oyuncu açmalı/kapamalı.
                // Bu kontrolün FPSPlayerController'da yapılıp buradaki ToggleMainEscMenu() metodunun çağrılması daha temiz olurdu.
                // Ancak mevcut kod yapına uyum sağlamak için buraya bir örnek ekleyelim,
                // fakat bu scriptin sahnede sadece 1 tane olduğundan ve
                // ESC basıldığında sadece o bilgisayarda çalıştığından emin olunmalı.
                // Multiplayer'da bu, her oyuncunun kendi bilgisayarında kendi menüsünü açması için doğru yerdir.
                ToggleMainEscMenu();
            }
        }
    }

    public void ToggleMainEscMenu()
    {
        // isMenuOpen değişkenini değiştir
        isMenuOpen = !isMenuOpen;

        // Panelleri durumuna göre aktif/deaktif et
        mainEscPanel.SetActive(isMenuOpen);
        UiBackground.SetActive(isMenuOpen);

        // Fare imlecini yönet
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None; // İmleci serbest bırak
            Cursor.visible = true; // İmleci görünür yap
            Debug.Log("ESC Menü Açıldı");
            // Time.timeScale'i 0 yapmak genellikle singleplayer oyunlarda yapılır.
            // Multiplayer'da timeScale'i değiştirmek genellikle sorun yaratır.
            // Eğer multiplayer'da duraklatma (pause) yapmak istiyorsanız,
            // bu durumu ağ üzerinden senkronize etmeniz ve oyuncu inputunu/mantığını
            // duraklatma durumuna göre yönetmeniz gerekir.
            // Şimdilik Time.timeScale dokunmuyoruz.
        }
        else
        {
            // Menü kapandığında imleci tekrar kilitle (FPS oyunları için)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("ESC Menü Kapandı");
            // Time.timeScale = 1f; // Eğer Time.timeScale 0 yapıldıysa geri al
        }
    }

    // --- MainEscPanel Buton Fonksiyonları ---
    public void ResumeGame()
    {
        // Oyunu devam ettirme logic'i (menüyü kapatır)
        isMenuOpen = false; // Durumu güncelle
        UiBackground.SetActive(isMenuOpen);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false); // Her ihtimale karşı diğer panelleri de kapat
        quitConfirmationPanel.SetActive(false);

        // İmleci tekrar kilitle (FPS oyunları için)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Eğer timeScale değiştirildiyse geri al
        // Time.timeScale = 1f;

        Debug.Log("Oyun Devam Ediyor");
    }

    public void GoToThisScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }
    public void ShowOptionsPanel()
    {
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
        ShowPanel(displayPanel); // Seçenekler açıldığında Display paneli açık olsun
        //Debug.Log("Seçenekler Açıldı");
    }

    public void ShowQuitConfirmationPanel()
    {
        quitConfirmationPanel.SetActive(true);
        // İsteğe bağlı: Ana menüyü arkada bırakabilir veya kapatabilirsiniz.
        // mainEscPanel.SetActive(false);
        Debug.Log("Çıkış Onayı Açıldı");
    }


    // --- OptionsMenuPanel Tab Fonksiyonları ---
    public void OnDisplayButton() => ShowPanel(displayPanel);
    public void OnGraphicsButton() => ShowPanel(graphicsPanel);
    public void OnAudioButton() => ShowPanel(audioPanel);
    public void OnControlsButton() => ShowPanel(controlsPanel);

    // Genel panel gösterme fonksiyonu (tekrarı önler)
    public void ShowPanel(GameObject panelToShow)
    {
        // Aktif paneli kapat
        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        // Yeni paneli aç
        panelToShow.SetActive(true);
        currentActivePanel = panelToShow; // Aktif paneli güncelle
        //Debug.Log($"Panel Değişti: {panelToShow.name}");
    }

    // --- OptionsMenuPanel Geri Fonksiyonu ---
    public void HideOptionsPanelAndShowMainEsc()
    {
        optionsMenuPanel.SetActive(false); // Seçenekleri kapat
        // currentActivePanel = null; // İsteğe bağlı: Seçeneklerden çıkınca aktif panel referansını temizle
        if (mainEscPanel != null)
        {
            mainEscPanel.SetActive(true); // Ana menüyü göster
            Debug.Log("Seçeneklerden Geri Dönüldü");
        }
        else
        {
            Debug.Log("Anamenüye Dönüldü");
        }
        // isMenuOpen durumu zaten true olmalı bu noktada
    }


    // --- QuitConfirmationPanel Buton Fonksiyonları ---
    public void ConfirmQuitGame()
    {
        Debug.Log("Oyundan çıkılıyor...");

        // ÖNEMLİ: Eğer online bir oyunsa, önce sunucudan düzgün bir şekilde ayrılın!
        // PhotonNetwork.LeaveRoom(); // veya uygun Photon metodu

        Application.Quit(); // Uygulamayı kapatır

        // Editörde çalışırken oyunu durdurmak için (build alınca bu kısım çalışmaz)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CancelQuit()
    {
        quitConfirmationPanel.SetActive(false); // Çıkış onay panelini kapat
        // İsteğe bağlı: Ana menüyü gizlediyseniz tekrar gösterin
        // mainEscPanel.SetActive(true);
        Debug.Log("Çıkış İptal Edildi");
        // isMenuOpen durumu zaten true olmalı bu noktada
    }
}