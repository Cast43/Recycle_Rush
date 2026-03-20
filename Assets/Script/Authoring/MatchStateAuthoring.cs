using Unity.Entities;
using UnityEngine;

public class MatchStateAuthoring : MonoBehaviour
{
    class Baker : Baker<MatchStateAuthoring>
    {
        public override void Bake(MatchStateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // Adiciona o estado da partida
            AddComponent(entity, new MatchStateComponent { CurrentState = MatchState.WaitingForPlayers });

            // Adiciona os contadores do lobby zerados
            AddComponent(entity, new LobbyStateComponent { ConnectedPlayers = 0, ReadyPlayers = 0 });
        }
    }
}