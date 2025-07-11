using UnityEngine;

public class FPSPlayerControllerSingle : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravityValue = -20f;

    [Header("Kamera Ayarları")]
    public float mouseSensitivity = 100f;
    public Transform cameraRoot;
    public bool clampVerticalRotation = true;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EnableCamera();
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -0.5f;

        playerVelocity.y += gravityValue * Time.deltaTime;

        Vector3 move = GetInputMoveVector();
        HandleJumpInput(isGrounded);
        HandleMouseLook();

        Vector3 totalMove = move + new Vector3(0, playerVelocity.y, 0);
        controller.Move(totalMove * Time.deltaTime);
    }

    private Vector3 GetInputMoveVector()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.magnitude > 1f) move.Normalize();
        return move * moveSpeed;
    }

    private void HandleJumpInput(bool isGrounded)
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;

        if (clampVerticalRotation)
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void EnableCamera()
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
        }
    }
}
