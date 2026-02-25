using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class GameStartAuthoring : MonoBehaviour
{
    public GameObject playerPrefabA;
    public GameObject playerPrefabB;
    public GameObject playerPrefabC;
    public GameObject playerPrefabD;
    public float timeToNextEnemy;
    public int startWave;
    public class Baker : Baker<GameStartAuthoring>
    {
        public override void Bake(GameStartAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GameConfig
            {
                playerPrefabA = GetEntity(authoring.playerPrefabA, TransformUsageFlags.None),
                playerPrefabB = GetEntity(authoring.playerPrefabB, TransformUsageFlags.None),
                playerPrefabC = GetEntity(authoring.playerPrefabC, TransformUsageFlags.None),
                playerPrefabD = GetEntity(authoring.playerPrefabD, TransformUsageFlags.None),
                timeToNextEnemy = authoring.timeToNextEnemy,
                startWave = authoring.startWave,
            });
        }
    }
}
