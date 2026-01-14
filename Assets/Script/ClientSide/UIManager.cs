using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // A chave mágica: uma instância estática acessível de qualquer lugar
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI energyPercentageText;
    [SerializeField] private TextMeshProUGUI healthPercentageText;

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
    public void UpdateHealthPercentage(float value)
    {
        if (healthPercentageText != null)
        {
            // Formata para mostrar como porcentagem (ex: 50%)
            healthPercentageText.text = $"{value:0}";
        }
    }
}