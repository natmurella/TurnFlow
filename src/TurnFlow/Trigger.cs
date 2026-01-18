namespace TurnFlow;

public interface ITrigger
{
    public void TriggerActivate(
        ITriggerEngine trigger_engine,
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    );

    public bool IsDurationZero();
    public ITriggerType GetFiredTriggerType();
}


public abstract class TriggerBase : ITrigger
{
    public int Duration = 1;
    public TriggerPermanenceType PermanenceType = TriggerPermanenceType.Temporary;
    private ITriggerType fired_trigger_type = null;

    public void TriggerActivate(
        ITriggerEngine trigger_engine,
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    )
    {
        bool can_activate = CanActivate(
            trigger_type,
            user,
            trigger_event,
            target,
            info
        );
        if (!IsDurationZero() && can_activate)
        {
            fired_trigger_type = trigger_type;
            Activate(
                trigger_engine,
                trigger_type,
                user,
                trigger_event,
                target,
                info
            );
            fired_trigger_type = null;

            if (PermanenceType == TriggerPermanenceType.Temporary)
            {
                Duration--;
            }
        }
    }

    public bool IsDurationZero()
    {
        return Duration <= 0;
    }

    public ITriggerType GetFiredTriggerType()
    {
        return fired_trigger_type;
    }

    public virtual void Activate(
        ITriggerEngine trigger_engine,
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    )
    {

    }
    
    public virtual bool CanActivate(
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    )
    {
        return true;
    }
}