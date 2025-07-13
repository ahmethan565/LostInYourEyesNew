using UnityEngine;
using System.Collections;

public class DoorLift : MonoBehaviour
{
    public float liftHeight = 3f;
    public float liftSpeed = 2f;
    public float autoCloseDelay = 3f; // kaç saniye sonra kapansın

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isOpen = false;
    private bool isMoving = false;

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + new Vector3(0, liftHeight, 0);
    }

    public void ToggleDoor()
    {
        if (!isMoving)
        {
            StartCoroutine(OpenAndMaybeAutoClose());
        }
    }

    private IEnumerator OpenAndMaybeAutoClose()
    {
        yield return StartCoroutine(MoveDoor(isOpen ? targetPosition : initialPosition, isOpen ? initialPosition : targetPosition));
        isOpen = !isOpen;

        // Eğer yeni durum açık ise belirli bir süre sonra kapat
        if (isOpen)
        {
            yield return new WaitForSeconds(autoCloseDelay);
            StartCoroutine(MoveDoor(targetPosition, initialPosition));
            isOpen = false;
        }
    }

    private IEnumerator MoveDoor(Vector3 fromPos, Vector3 toPos)
    {
        isMoving = true;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * liftSpeed;
            transform.position = Vector3.Lerp(fromPos, toPos, elapsed);
            yield return null;
        }

        transform.position = toPos;
        isMoving = false;
    }
}
