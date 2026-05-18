using UnityEngine;

[CreateAssetMenu(fileName = "NewEventUIInfo", menuName = "Rougue/Event UI Info")]
public class EventUIInfoSO : ScriptableObject
{
    public EventType eventType;
    public string eventName;
    
    [TextArea]
    public string eventDescription;
}