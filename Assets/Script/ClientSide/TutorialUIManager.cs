using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;

public class TutorialUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject tutorialHUDPanel; // O painel principal da HUD do tutorial
    [SerializeField] private TextMeshProUGUI objectiveText; // O texto que diz o que o jogador deve fazer
    [SerializeField] private string[] stepTexts; // O texto que diz o que o jogador deve fazer

    private void Start()
    {
        // Garante que a HUD comece desligada
        tutorialHUDPanel.SetActive(false);
    }

    private void Update()
    {
        EntityManager? em = null;

        // Busca o mundo do cliente (se estiver usando Netcode local)
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                em = world.EntityManager;
                break; // Achou o mundo do cliente, pode parar o loop
            }
        }

        // Se não achou mundo de cliente, tenta usar o mundo padrão (Single-Player puro)
        if (em == null && World.DefaultGameObjectInjectionWorld != null)
        {
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
    }

    // Método auxiliar para ler o progresso do jogador local
    private void UpdateTutorialObjective(EntityManager em)
    {
        // Pega o progresso de qualquer entidade que tenha TutorialProgress na cena
        var progressQuery = em.CreateEntityQuery(typeof(TutorialProgress));

        if (progressQuery.HasSingleton<TutorialProgress>())
        {
            var progress = progressQuery.GetSingleton<TutorialProgress>();

            // Verifica se o passo atual está dentro dos limites do array
            if (progress.CurrentStep >= 0 && progress.CurrentStep < stepTexts.Length)
            {
                // Pega o texto diretamente do array configurado no Inspector
                objectiveText.text = stepTexts[progress.CurrentStep];
            }
            else if (progress.IsCompleted)
            {
                // Opcional: Mensagem final caso haja um delay antes de mudar para o estado "Playing"
                objectiveText.text = "Tutorial Concluído! Aguardando outros jogadores...";
            }
        }
    }
}