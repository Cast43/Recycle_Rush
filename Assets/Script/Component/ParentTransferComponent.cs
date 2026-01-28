using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

// Componente para sincronizar quem deve ser o pai
public struct VisualParentLink : IComponentData
{
    [GhostField] public Entity ParentEntity;
}