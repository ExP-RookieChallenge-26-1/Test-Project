using UnityEngine;
using UnityEngine.UI;

public class GageFire : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image GageFireImg;
    [SerializeField] private Slider Gage;

    [Header("Sprites")]
    [SerializeField] private Sprite FireStep1Sprite;
    [SerializeField] private Sprite FireStep2Sprite;
    [SerializeField] private Sprite FireStep3Sprite;

    [Header("Shake Settings")]
    [SerializeField] private float shakeStrengthStep2 = 2.0f;
    [SerializeField] private float shakeStrengthStep3 = 5.0f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
    }

    public void Update()
    {
        if (Gage == null || GageFireImg == null) return;

        if (Gage.value >= 0.8f)
        {
            GageFireImg.sprite = FireStep3Sprite;
            ShakeUI(shakeStrengthStep3);
        }
        else if (Gage.value >= 0.5f)
        {
            GageFireImg.sprite = FireStep2Sprite;
            ShakeUI(shakeStrengthStep2);
        }
        else
        {
            GageFireImg.sprite = FireStep1Sprite;
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    private void ShakeUI(float strength)
    {
        Vector2 randomOffset = Random.insideUnitCircle * strength;
        rectTransform.anchoredPosition = originalPosition + randomOffset;
    }
}