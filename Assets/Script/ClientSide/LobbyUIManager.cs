using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playersStatusText;

    // Referência opcional para esconder o painel quando o jogo começar
    [SerializeField] private GameObject lobbyPanel;

    private void Update()
    {
        // Precisamos vasculhar os mundos para achar o ClientWorld
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                var em = world.EntityManager;

                // Cria uma query para buscar os componentes de estado
                var lobbyQuery = em.CreateEntityQuery(typeof(LobbyStateComponent));
                var matchQuery = em.CreateEntityQuery(typeof(MatchStateComponent));

                // Garante que a entidade já foi criada e sincronizada pela rede
                if (lobbyQuery.HasSingleton<LobbyStateComponent>() && matchQuery.HasSingleton<MatchStateComponent>())
                {
                    var lobbyState = lobbyQuery.GetSingleton<LobbyStateComponent>();
                    var matchState = matchQuery.GetSingleton<MatchStateComponent>();

                    // Atualiza o texto da UI
                    playersStatusText.text = $"Jogadores Conectados: {lobbyState.ConnectedPlayers} \n" +
                                             $"Prontos: {lobbyState.ReadyPlayers} / {lobbyState.ConnectedPlayers}";

                    // Se o estado mudou para Tutorial ou Playing, podemos desativar a tela de Lobby
                    if (matchState.CurrentState != MatchState.WaitingForPlayers)
                    {
                        if (lobbyPanel.activeSelf) lobbyPanel.SetActive(false);
                    }
                }

                break; // Achou o ClientWorld, não precisa olhar os outros
            }
        }
    }
}