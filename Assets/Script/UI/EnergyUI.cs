using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text EnergyText;

    public void UpdateEnergyText(int currentEnergy)
    {
        EnergyText.text = currentEnergy.ToString();
    }
}
