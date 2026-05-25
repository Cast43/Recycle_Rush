using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;

public class EventUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject eventHUDPanel; // Painel principal do evento na HUD
    [SerializeField] private TextMeshProUGUI timeText; // Texto do cronômetro
    [SerializeField] private TextMeshProUGUI eventNameText;
    [SerializeField] private UnityEngine.UI.Slider eventProgressSlider;
    [SerializeField] private TextMeshProUGUI eventProgressText;

    [Header("Event Texts Config")]
    [SerializeField] private EventUIInfoSO[] eventUIInfos;

    private void Start()
    {
        // Garante que a HUD comece desligada
        if (eventHUDPanel != null)
            eventHUDPanel.SetActive(false);
    }

    private void Update()
    {
        EntityManager? em = null;

        // Busca o mundo do cliente para pegar a simulação local sincronizada
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                em = world.EntityManager;
                break;
            }
        }

        // Se não achou mundo de cliente, tenta usar o padrão (útil para testes em Single Player)
        if (em == null && World.DefaultGameObjectInjectionWorld != null)
        {
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        if (em == null) return;

        // Busca se existe algum evento ativo rolando na partida
        var eventQuery = em.Value.CreateEntityQuery(typeof(EventObjective), typeof(EventActiveTag));

        if (!eventQuery.IsEmptyIgnoreFilter)
        {
            // Liga o painel na tela
            if (eventHUDPanel != null && !eventHUDPanel.activeSelf)
                eventHUDPanel.SetActive(true);

            var eventObjective = eventQuery.GetSingleton<EventObjective>();

            if (eventNameText != null) 
            {
                eventNameText.text = "Evento Ativo"; // Valor padrão
                foreach (var info in eventUIInfos)
                {
                    if (info != null && info.eventType == eventObjective.Type)
                    {
                        eventNameText.text = info.eventName;
                        break;
                    }
                }
            }

            if (eventProgressSlider != null)
            {
                eventProgressSlider.maxValue = eventObjective.TargetValue;
                eventProgressSlider.value = eventObjective.Progress;
            }

            if (eventProgressText != null)
            {
                eventProgressText.text = "Complete o objetivo do evento!"; // Valor padrão
                foreach (var info in eventUIInfos)
                {
                    if (info != null && info.eventType == eventObjective.Type)
                    {
                        eventProgressText.text = info.eventDescription;
                        break;
                    }
                }
            }

            if (eventObjective.TimeLimit > 0 && timeText != null)
            {
                // Formata os segundos soltos em formato MM:SS
                int min = Mathf.FloorToInt(eventObjective.TimeRemaining / 60);
                int sec = Mathf.FloorToInt(eventObjective.TimeRemaining % 60);
                
                timeText.text = string.Format("{0:00}:{1:00}", min, sec);
                
                // Efeito visual nos últimos 10 segundos (Fica vermelho e dá uma leve piscada)
                if (eventObjective.TimeRemaining <= 10f)
                {
                    timeText.color = Color.red;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
        }
        else
        {
            // Se não tem nenhum evento ativo, esconde o painel do evento da tela
            if (eventHUDPanel != null && eventHUDPanel.activeSelf)
                eventHUDPanel.SetActive(false);
        }
    }
}