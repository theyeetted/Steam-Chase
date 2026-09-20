using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Steamchasing.Cards
{
    public class StatusIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stackText;

        public void Setup(Sprite icon, int stack)
        {
            iconImage.sprite = icon;

            if (stack > 0)
            {
                stackText.text = stack.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }
    }
}