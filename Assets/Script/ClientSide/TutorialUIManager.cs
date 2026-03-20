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
        // Busca o mundo do cliente para ler os dados sincronizados
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                var em = world.EntityManager;

                // Cria a query para ler o estado da partida
                var matchQuery = em.CreateEntityQuery(typeof(MatchStateComponent));

                if (matchQuery.HasSingleton<MatchStateComponent>())
                {
                    var matchState = matchQuery.GetSingleton<MatchStateComponent>();

                    // 1. LIGA OU DESLIGA A HUD DEPENDENDO DO ESTADO
                    if (matchState.CurrentState == MatchState.Tutorial)
                    {
                        if (!tutorialHUDPanel.activeSelf)
                            tutorialHUDPanel.SetActive(true);

                        // 2. ATUALIZA O TEXTO DO OBJETIVO (Opcional, mas recomendado)
                        UpdateTutorialObjective(em);
                    }
                    else
                    {
                        if (tutorialHUDPanel.activeSelf)
                            tutorialHUDPanel.SetActive(false);
                    }
                }
                break; // Achou o mundo do cliente, pode parar o loop
            }
        }
    }

    // Método auxiliar para ler o progresso do jogador local
    private void UpdateTutorialObjective(EntityManager em)
    {
        // Pega o progresso apenas do avatar que pertence ao jogador desta máquina
        var progressQuery = em.CreateEntityQuery(typeof(TutorialProgress), typeof(GhostOwnerIsLocal));

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