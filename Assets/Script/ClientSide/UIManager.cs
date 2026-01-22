using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // A chave mágica: uma instância estática acessível de qualquer lugar
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI energyPercentageText;
    [SerializeField] private TextMeshProUGUI robotHealthPercentageText;
    [SerializeField] private TextMeshProUGUI treeHealthPercentageText;
    [SerializeField] private Slider experienceBar;

    private void Awake()
    {
        // Garante que só existe um e que é acessível globalmente
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Método público para atualizar o texto
    public void UpdateEnergyPercentage(float value)
    {
        if (energyPercentageText != null)
        {
            // Formata para mostrar como porcentagem (ex: 50%)
            energyPercentageText.text = $"{value:0}%";
        }
    }
    public void UpdateRobotHealthPercentage(int currentValue, int maxValue)
    {
        if (robotHealthPercentageText != null)
        {
            // Formata para mostrar como porcentagem (ex: 50%)
            robotHealthPercentageText.text = $"{currentValue:0}/{maxValue:0}";
        }
    }
    public void UpdateExperienceBar(int value)
    {
        if (experienceBar != null)
        {
            experienceBar.value = value;
        }
    }
    public void UpdateTreeHealthPercentage(float value)
    {
        if (treeHealthPercentageText != null)
        {
            // Formata para mostrar como porcentagem (ex: 50%)
            treeHealthPercentageText.text = $"{value:0}";
        }
    }
}