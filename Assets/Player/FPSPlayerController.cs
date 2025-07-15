using UnityEngine;
using Photon.Pun;

public class FPSPlayerController : MonoBehaviourPun, IPunObservable
{

    public string currentState;
    public bool isMovementFrozen = false;

    [Header("Speed Settings")]
    public float baseWalkSpeed = 5f;
    public float baseSprintSpeed = 8f;

    public float currentSpeed { get; private set; }

    [Header("Hareket Ayarları")]
    public bool isSprinting = false;
    public float jumpForce = 8f;
    public float gravityValue = -20f;
    [Tooltip("Yüksekliği bu değerden daha az olan eğimlerde karakter yukarı doğru hareket edebilir.")]
    public float maxSlopeAngle = 45f; // Karakterin çıkabileceği maksimum eğim açısı
    [Tooltip("Havada hareket ederken inputun ne kadar etkili olacağını belirler. 0 = hiç kontrol yok, 1 = tam kontrol.")]
    [Range(0f, 1f)]
    public float airControlFactor = 0.5f; // Havada hareket kontrolü çarpanı

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
    public bool isSlidingSlope { get; private set; } // Dik eğimde kayma bayrağı, FSM'ye açık

    // Ağ üzerinden gelen veriler
    private float network_xRotation;
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    // Slope Projection için
    private Vector3 _hitNormal; // Son çarpma normali

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Step offset'in ayarı doğrudan CharacterController component'i üzerindedir ve FSM gerektirmez.
        // controller.stepOffset = 0.3f; // İhtiyacınıza göre bu değeri ayarlayın

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
            playerFSM.AddState(new PlayerRunningState(this, playerFSM));
            playerFSM.AddState(new PlayerCrouchingState(this, playerFSM));
            playerFSM.AddState(new PlayerSlidingState(this, playerFSM));

            playerFSM.ChangeState(typeof(PlayerIdleState)); // Başlangıç durumu
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            isGrounded = controller.isGrounded;
            currentState = playerFSM.GetCurrentState().ToString();

            bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

            if (!menuIsOpen)
            {
                playerFSM.Update();
                HandleLocalPlayerMouseLook();
            }
            else
            {
                ApplyGravity();
                MoveCharacter();
            }

            // ✅ Her frame hız mantığını uygula
            UpdateMoveSpeed();
        }
        else
        {
            SmoothRemotePlayerData();
        }
    }
    private void UpdateMoveSpeed()
    {
        float targetSpeed = baseWalkSpeed;

        // Eğer sprint ediyorsa sprint hızını kullan
        if (isSprinting)
        {
            targetSpeed = baseSprintSpeed;
        }

        // Çömeliyorsa hız çarpanı uygula
        if (currentState.Contains("Crouching"))
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        currentSpeed = targetSpeed;
    }



    // Karakterin bir objeye çarptığında çağrılır (Slope Projection için)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        _hitNormal = hit.normal;
        // Eğer yere çarpıyorsak ve eğim açısı çok dik ise kayma bayrağını ayarla
        if (isGrounded)
        {
            float angle = Vector3.Angle(Vector3.up, _hitNormal);
            isSlidingSlope = angle > maxSlopeAngle;
        }
        else
        {
            isSlidingSlope = false;
        }
    }

    // --- FSM Tarafından Erişilecek Yardımcı Metotlar ---

    public Vector3 GetInputMoveVector()
    {
        bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

        if (menuIsOpen || isMovementFrozen)
        {
            return Vector3.zero; // Menü veya puzzle ekranı açıksa input yok
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.magnitude > 1f) move.Normalize();

        if (!isGrounded)
            return move * airControlFactor;

        return move;
    }

    public void HandleJumpInput()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    public void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0)
        {
            // Yere yapışma: Karakter yere değdiğinde hafifçe aşağı doğru bir kuvvet uygular
            // -0.5f gibi sabit, küçük bir negatif değer genellikle yeterlidir.
            playerVelocity.y = -0.5f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
    }

    public void ApplyHorizontalMovement(Vector3 horizontalMove)
    {
        // Bu metot artık doğrudan MoveCharacter tarafından ele alınıyor,
        // ancak farklı FSM durumları özel yatay hareket uygulamak isterse kullanılabilir.
    }

    public void MoveCharacter()
    {
        // GetInputMoveVector() şimdi sadece yönü döndürüyor, bu yüzden burada moveSpeed ile çarpıyoruz.
        Vector3 horizontalMovement = GetInputMoveVector() * currentSpeed;

        // FSM durumları özel hızları (örn. sprintSpeed, crouchSpeedMultiplier)
        // horizontalMovement üzerinde değiştirmelidir.
        // Örneğin, PlayerRunningState içinde: horizontalMovement = GetInputMoveVector() * sprintSpeed;

        Vector3 totalMove = horizontalMovement;

        // Eğimde hareket (Slope Projection)
        // Eğer kaymıyorsak ve yere basıyorsak eğim projeksiyonunu uygula
        if (isGrounded && !isSlidingSlope)
        {
            totalMove = ProjectMoveOnSlope(horizontalMovement, _hitNormal);
        }

        totalMove += new Vector3(0, playerVelocity.y, 0);

        if (controller != null && controller.enabled)
        {
            controller.Move(totalMove * Time.deltaTime);
        }
    }

    // Eğim üzerinde hareketi yansıtan metod
    private Vector3 ProjectMoveOnSlope(Vector3 moveVector, Vector3 slopeNormal)
    {
        // Hareket vektörünü eğim normaline dik olacak şekilde yansıtır.
        // Burada .normalized * moveVector.magnitude kısmını kaldırıyoruz.
        // ProjectOnPlane zaten doğru uzunlukta bir vektör döndürmelidir.
        return Vector3.ProjectOnPlane(moveVector, slopeNormal);
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
            stream.SendNext(isSlidingSlope); // isSlidingSlope bayrağını ağ üzerinden gönder
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            network_xRotation = (float)stream.ReceiveNext();
            isSlidingSlope = (bool)stream.ReceiveNext(); // isSlidingSlope bayrağını ağ üzerinden al
        }
    }
}