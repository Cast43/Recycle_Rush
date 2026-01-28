using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

//para conter uma lista dos efeitos ja possiudos 
//foi utilizado uma tecnica que cria novos gameObjects relacionados aos efeitos e os seus alvos
//assim os sistemas dos efeitos procuram esses objetos com os efeitos e não os inimigos

public struct EffectPrefab : IBufferElementData
{
    [GhostField]
    public Entity Prefab; // vai apontar para uma Entity Prefab que já tem o PoisonEffect, BurnEffect etc.
    [GhostField]
    public FixedString64Bytes name;
}
public struct EffectTarget : IComponentData
{
    public Entity Value;
    public Entity effectGiver;

}
public struct PoisonEffect : IComponentData
{
    public uint duration;         // Tempo restante de veneno
    public uint dmgInterval;     // A cada quanto tempo toma dano
    public int damagePerTick;
    public uint timeSinceLastTick;
}
//estrutura para sincronizar a duração do veneno no multiplayer
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PoisonDuration : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PoisonDps : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
    public int damagePerTick;
    public uint dmgInterval;     // A cada quanto tempo toma dano
}
//primeiro cria um gameobjet de curse com os stacks com um alvo
//depois é verificado se ja possui stacks(CurseStackEffect)
//se sim adiciona duration e stacks se não adiciona os componentes e buffers no alvo
public struct CurseEffect : IComponentData
{
    public uint duration;         // Tempo restante de veneno
    public uint addAmmount;       //quantidade adicionada nos stacks
    public uint maxStacks;
}
//estrutura para sincronizar a duração do curse no multiplayer
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct CurseDuration : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}
public struct CurseStackEffect : IComponentData
{
    public uint value;
    public uint maxStack;
}
//verifica da posição acertada quais estão próximos
//cria uma duração e pode dar vários danos dependendo do tick de dano
public struct LightningEffect : IComponentData
{
    // public float duration;
    public float radius;
    public Faction target;
    public uint duration;         // Tempo restante de veneno
    public uint dmgInterval;     // A cada quanto tempo toma dano
    public int damagePerTick;   //dos raios
    public uint timeSinceLastTick;
    public Entity particleEffect;
}
public struct LightningVisual : IComponentData
{
    public Entity particleEffect;
}
//estrutura para sincronizar a duração do lightning no multiplayer
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct LightningDuration : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
    public float radius;
    public Faction target;
    public Entity effectGiver;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct LightningDps : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
    public int damagePerTick;
    public uint dmgInterval;     // A cada quanto tempo toma dano
}
