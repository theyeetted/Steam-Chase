using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text HealthText;

    public void UpdateBar(int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        if (fillImage != null)
            fillImage.fillAmount = (float)current / max;

        if (HealthText != null)
            HealthText.text = current + " / " + max;
    }
}
