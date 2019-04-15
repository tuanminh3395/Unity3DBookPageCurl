using UnityEngine.Events;
using UnityEngine.EventSystems;

public static class EventTriggerExtension
{
    public static void AddEntry(this EventTrigger eventTrigger, EventTriggerType triggerType, UnityAction callback)
    {
        var entry = new EventTrigger.Entry {eventID = triggerType};
        entry.callback.AddListener((eventData) => { callback.Invoke(); });
        eventTrigger.triggers.Add(entry);
    }
}