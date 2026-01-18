namespace TurnFlow;

public enum EventSourceType
{
    Trigger,
    Action
}

public interface IEvent
{
    public void EventExecute(
        ITriggerEngine trigger_engine
    );

    public EventSourceType GetEventSourceType();
    public void SetEventSourceType(EventSourceType event_source_type);
    public bool IsFromSystemTrigger();
    public void SetFromSystemTrigger(bool is_from_system_trigger);
}

public abstract class EventBase : IEvent
{
    private EventSourceType event_source_type = EventSourceType.Action;
    private bool is_from_system_trigger = false;
    public void EventExecute(
        ITriggerEngine trigger_engine
    )
    {
        Execute(trigger_engine);
    }

    public virtual void Execute(
        ITriggerEngine trigger_engine
    )
    {

    }

    public EventSourceType GetEventSourceType()
    {
        return event_source_type;
    }

    public void SetEventSourceType(EventSourceType event_source_type)
    {
        this.event_source_type = event_source_type;
    }

    public bool IsFromSystemTrigger()
    {
        return is_from_system_trigger;
    }

    public void SetFromSystemTrigger(bool is_from_system_trigger)
    {
        this.is_from_system_trigger = is_from_system_trigger;
    }
}



