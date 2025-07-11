using UnityEngine;
using Photon.Pun;

public class FPSPlayerController : MonoBehaviourPun, IPunObservable
{

    public string currentState;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravityValue = -20f;
    public float sprintSpeed;

    [Header("Çömelme Ayarları")]
    public float crouchHeight = 1.0f; // Çömelme yüksekliği
    public float crouchSpeedMultiplier = 0.5f; // Çömelirken hız çarpanı
    public float crouchCameraOffset = -0.5f; // Kameranın ne kadar aşağı ineceği

    [Header("Sprint Ayarları")]
    public float sprintDuration = 0f;
    public float slideTriggerTime = 1.0f; // Örn: 1 saniye sonra slide açılabilir

    [Header("Kamera Ayarları")]
    public float mouseSensitivity = 100f;
    public Transform cameraRoot;
    public bool clampVerticalRotation = true;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Ağ Ayarları")]
    public float remoteSmoothFactor = 15f;

    public CharacterController controller;
    public Vector3 playerVelocity; // Hem input kaynaklı hem yer çekimi kaynaklı dikey hızı içerir
    private float xRotation = 0f;

    // FSM ile ilgili eklemeler
    private PlayerFSM playerFSM;
    public bool isGrounded; // Yerel oyuncu için yere değme durumu

    // Ağ üzerinden gelen veriler
    private float network_xRotation;
    // network_isGrounded artık doğrudan kullanılmayacak, SmoothRemotePlayerData'da dolaylı etki edecek
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (!photonView.IsMine)
        {
            if (controller != null)
                controller.enabled = false;
            SetupRemotePlayerCamera();
        }
    }

    void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 20;

        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetupLocalPlayerCamera();

            // FSM'yi başlat
            playerFSM = new PlayerFSM();
            playerFSM.AddState(new PlayerIdleState(this, playerFSM));
            playerFSM.AddState(new PlayerWalkingState(this, playerFSM));
            playerFSM.AddState(new PlayerJumpingState(this, playerFSM));
            playerFSM.AddState(new PlayerRunningState(this, playerFSM)); // YENİ
            playerFSM.AddState(new PlayerCrouchingState(this, playerFSM)); // YENİ
            playerFSM.AddState(new PlayerSlidingState(this, playerFSM)); // YENİ

            playerFSM.ChangeState(typeof(PlayerIdleState)); // Başlangıç durumu
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            // Yere değme durumunu her zaman güncelle
            isGrounded = controller.isGrounded;
            currentState = playerFSM.GetCurrentState().ToString();
            // Menü açık mı kontrol et
            bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

            if (!menuIsOpen)
            {
                playerFSM.Update(); // FSM'nin mevcut durumunu güncelle
                HandleLocalPlayerMouseLook(); // Fare ile bakışı FSM'den bağımsız tutabiliriz
            }
            else
            {
                // Menü açıksa hareket ve zıplama inputu işlenmez, FSM güncellenmez.
                // Karakterin yer çekimini uygulamaya devam etmesi gerekebilir.
                ApplyGravity();
                MoveCharacter(); // Sadece yer çekimi etkisiyle hareket
            }
        }
        else // Remote oyuncu ise
        {
            SmoothRemotePlayerData();
        }
    }

    // --- FSM Tarafından Erişilecek Yardımcı Metotlar ---

    // Sadece yatay/dikey input vektörünü döndüren yardımcı metot (public)
    public Vector3 GetInputMoveVector()
    {
        // Menü açık mı kontrol et
        bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

        // Eğer menü açıksa, hareket inputu alma
        if (menuIsOpen)
        {
            return Vector3.zero;
        }

        // Menü kapalıysa normal input alımına devam et
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.magnitude > 1f) move.Normalize();
        return move * moveSpeed;
    }
    // Sadece zıplama gücünü uygulayan yardımcı metot (public)
    public void HandleJumpInput()
    {
        if (isGrounded) // Sadece yere değiyorsa zıplasın (durum zaten kontrol etmeli ama burada da emin olalım)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    // Yer çekimini uygulayan yardımcı metot (public)
    public void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f; // Yere değer değmez dikey hızı sıfırla
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
    }

    // Yatay hareketi uygulayan yardımcı metot (public)
    public void ApplyHorizontalMovement(Vector3 horizontalMove)
    {
        // FSM durumları tarafından çağrılırken yatay hareket doğrudan CharacterController'a uygulanacak
        // playerVelocity.x ve .z'yi kullanmak yerine doğrudan Move metodu içinde ele alınabilir.
        // Ancak daha temiz bir ayrım için, FSM'de sadece input alınır, burada doğrudan uygulanır.
        // Burada playerVelocity.x/z'yi güncellemiyoruz, çünkü totalMove'da birleştirilecek.
        // Bu fonksiyon sadece hareket vektörünü hesaplar.
    }

    // Karakteri gerçekten hareket ettiren yardımcı metot (public)
    public void MoveCharacter()
    {
        Vector3 horizontalMovement = GetInputMoveVector(); // Her kare yeniden hesapla
        Vector3 totalMove = horizontalMovement + new Vector3(0, playerVelocity.y, 0);

        if (controller != null && controller.enabled)
        {
            controller.Move(totalMove * Time.deltaTime);
        }
    }


    // --- Mevcut Diğer Metotlar (Aynı Kalır) ---
    void SetupLocalPlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al == null) al = cam.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }
            else Debug.LogWarning("cameraRoot altında kamera bulunamadı! Yerel kamera ayarlanamadı.");
        }
        else Debug.LogWarning("cameraRoot atanmamış! Yerel kamera ayarlanamadı.");
    }

    void SetupRemotePlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.gameObject.SetActive(false);
            AudioListener al = cameraRoot.GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;
        }
        else Debug.LogWarning("cameraRoot atanmamış! Remote kamera ayarlanamadı.");
    }

    void HandleLocalPlayerMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;

        if (clampVerticalRotation)
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        else Debug.LogWarning("cameraRoot atanmamış! Dikey bakış yapılamadı.");
    }

    void SmoothRemotePlayerData()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * remoteSmoothFactor);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * remoteSmoothFactor);

        if (cameraRoot != null)
        {
            Quaternion currentRot = cameraRoot.localRotation;
            Quaternion targetRot = Quaternion.Euler(network_xRotation, 0f, 0f);
            cameraRoot.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * remoteSmoothFactor);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(xRotation);
            // playerVelocity.y'yi de göndermek isteyebilirsiniz, özellikle zıplama animasyonları için
            // stream.SendNext(playerVelocity.y);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            network_xRotation = (float)stream.ReceiveNext();
            // Eğer gönderiyorsanız:
            // playerVelocity.y = (float)stream.ReceiveNext();
        }
    }
}