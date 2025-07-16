using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMaterialListAuthoring : MonoBehaviour
{
    [Tooltip("Liste aqui todos os materiais que irão compor o RenderMeshArray")]
    public Material[] materials;

    [Tooltip("Mesh padrão para todas as instâncias (por exemplo, a do prefab de jogador)")]
    public Mesh mesh;

    public class Baker : Baker<PlayerMaterialListAuthoring>
    {
        public override void Bake(PlayerMaterialListAuthoring auth)
        {
            // Cria entidade singleton para guardar o RenderMeshArray
            var e = GetEntity(TransformUsageFlags.None);

            // Prepare listas para deduplicação
            var mats = new List<Material>(auth.materials);
            var meshes = new List<Mesh> { auth.mesh };

            // Constrói o RenderMeshArray; deduplica internamente
            var rma = RenderMeshArray.CreateWithDeduplication(mats, meshes);

            AddSharedComponentManaged(e, rma);
        }
    }
}
