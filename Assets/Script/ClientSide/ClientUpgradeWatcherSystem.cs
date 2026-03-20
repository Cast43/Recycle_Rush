using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientUpgradeWatcherSystem : SystemBase
{
    // Livro caixa local: quantas vezes já abrimos a tela para os upgrades atuais
    private int upgradesProcessadosLocalmente = 0;

    protected override void OnUpdate()
    {
        if (AddUpgradesUIManager.Instance == null) return;

        foreach (var (pendingBuffer, ghost) in SystemAPI.Query<DynamicBuffer<UpgradesPending>, RefRO<GhostOwnerIsLocal>>().WithAll<PlayerInput>())
        {
            int tamanhoFilaServidor = pendingBuffer.Length;

            // 1. O servidor processou nosso clique e descontou da fila principal.
            // Sincronizamos nossa contagem local para baixo para acompanhar o servidor.
            if (tamanhoFilaServidor < upgradesProcessadosLocalmente)
            {
                upgradesProcessadosLocalmente = tamanhoFilaServidor;
            }

            // 2. Se a fila do servidor tem mais itens do que nós já processamos,
            // significa que ganhamos um nível novo (ou acumulamos).
            if (tamanhoFilaServidor > upgradesProcessadosLocalmente && !AddUpgradesUIManager.Instance.gameObject.activeSelf)
            {
                // Lê o nível do upgrade (buscando no índice correto caso o jogador upe 2x de uma vez)
                UpgradeLevel currentLevel = pendingBuffer[upgradesProcessadosLocalmente].upgradeLevel;

                AddUpgradesUIManager.Instance.ShowUpgrades(currentLevel);

                // Marcamos que a tela foi aberta para este ponto específico.
                // A tela não vai tentar abrir de novo no frame seguinte enquanto espera a internet!
                upgradesProcessadosLocalmente++;
            }
        }
    }
}