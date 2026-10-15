using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StunBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text stunText;

    public void UpdateBar(int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        if (fillImage != null)
            fillImage.fillAmount = (float)current / max;

        if (stunText != null)
            stunText.text = current + " / " + max;
    }
    
}