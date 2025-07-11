using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Animator buttonAnimator;
    private bool isHovered = false; // Hover durumunu takip etmek için

    private void Start()
    {
        buttonAnimator = GetComponent<Animator>();
        if (buttonAnimator == null)
        {
            Debug.LogError($"{gameObject.name} üzerinde Animator bileşeni bulunamadı!");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovered) // Eğer zaten hover durumundaysa tekrar tetikleme
        {
            buttonAnimator.SetBool("IsHovered", true);
            buttonAnimator.SetBool("IsNormal", false);
            isHovered = true; // Hover durumunu işaretle
            Debug.Log($"{gameObject.name}: Hover animasyonu tetiklendi");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHovered) // Eğer zaten normal durumundaysa tekrar tetikleme
        {
            buttonAnimator.SetBool("IsHovered", false);
            buttonAnimator.SetBool("IsNormal", true);
            isHovered = false; // Normal durumu işaretle
            Debug.Log($"{gameObject.name}: Normal animasyonu tetiklendi");
        }
    }
}
