using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientUpgradeWatcherSystem : SystemBase
{
    private int lastFilaServidor = 0;
    private int pendingRpcs = 0;
    private bool wasUiActive = false;

    protected override void OnUpdate()
    {
        if (AddUpgradesUIManager.Instance == null) return;

        bool isUiActive = AddUpgradesUIManager.Instance.gameObject.activeSelf;

        // Se a UI estava aberta e agora fechou, assumimos que o jogador enviou um RPC de escolha
        if (wasUiActive && !isUiActive)
        {
            pendingRpcs++;
        }
        wasUiActive = isUiActive;

        foreach (var (pendingBuffer, ghost) in SystemAPI.Query<DynamicBuffer<UpgradesPending>, RefRO<GhostOwnerIsLocal>>().WithAll<PlayerInput>())
        {
            int atualFilaServidor = pendingBuffer.Length;

            // Se a fila zerou no servidor, podemos resetar a contagem local de segurança
            if (atualFilaServidor == 0)
            {
                pendingRpcs = 0;
            }
            // Se o servidor processou escolhas (a fila diminuiu), descontamos dos nossos RPCs pendentes
            else if (atualFilaServidor < lastFilaServidor)
            {
                int consumidos = lastFilaServidor - atualFilaServidor;
                pendingRpcs -= consumidos;
                if (pendingRpcs < 0) pendingRpcs = 0;
            }
            
            lastFilaServidor = atualFilaServidor;

            int efetivamentePendentes = atualFilaServidor - pendingRpcs;

            // Se o jogador ainda tem upgrades para responder e a UI está fechada
            if (efetivamentePendentes > 0 && !isUiActive)
            {
                // Garante que não vamos ler fora dos limites do array do buffer
                int indexToRead = pendingRpcs;
                if (indexToRead >= atualFilaServidor) indexToRead = atualFilaServidor - 1;
                if (indexToRead < 0) indexToRead = 0;

                UpgradeAperance currentLevel = pendingBuffer[indexToRead].upgradeLevel;

                AddUpgradesUIManager.Instance.ShowUpgrades(currentLevel);
                
                // Atualizamos para não tentar abrir múltiplas vezes no mesmo frame
                wasUiActive = true; 
            }
        }
    }
}