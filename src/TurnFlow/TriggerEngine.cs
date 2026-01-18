

namespace TurnFlow;

public interface ITriggerEngine
{
    public void RegisterTrigger(
        ITriggerType trigger_type,
        ITrigger trigger,
        ITarget target
    );

    public void RegisterEvent(
        IEvent e,
        Object obj
    );

    public void Trigger(
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    );
    
    public StepState step();
}


public enum StepStateType
{
    Empty,
    PreEvent,
}

public class StepState
{
    public StepStateType StateType { get; }
    public IEvent Event { get; }

    public StepState(
        StepStateType state_type,
        IEvent e
    )
    {
        StateType = state_type;
        Event = e;
    }
}


public class TriggerEngine : ITriggerEngine
{
    private Dictionary<ITriggerType, Dictionary<ITarget, List<ITrigger>>> trigger_list;
    private Stack<IEvent> event_stack;
    private bool is_event_seen;

    public TriggerEngine()
    {
        trigger_list = new Dictionary<ITriggerType, Dictionary<ITarget, List<ITrigger>>>();
        event_stack = new Stack<IEvent>();
        is_event_seen = false;
    }

    public StepState step()
    {
        if (event_stack.Count == 0)
        {
            return new StepState(StepStateType.Empty, null);
        }

        if (!is_event_seen)
        {
            is_event_seen = true;
            IEvent ev = event_stack.Peek();
            if (!ev.IsFromSystemTrigger())
            {
                return new StepState(StepStateType.PreEvent, ev);
            }
        }

        IEvent e = event_stack.Pop();
        e.EventExecute(this);

        is_event_seen = false;

        if (event_stack.Count == 0)
        {
            return new StepState(StepStateType.Empty, null);
        }
        else
        {
            is_event_seen = true;
            IEvent ev = event_stack.Peek();
            if (!ev.IsFromSystemTrigger())
            {
                return new StepState(StepStateType.PreEvent, ev);
            }
            else
            {
                return step();
            }
        }
    }

    public void Trigger(
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget? target,
        IInfo info
    )
    {
        if (!trigger_list.ContainsKey(trigger_type))
        {
            return;
        }

        if (target != null && !trigger_list[trigger_type].ContainsKey(target))
        {
            return;
        }

        if (target != null)
        {
            var target_trigger_list = trigger_list[trigger_type][target];
            List<ITrigger> to_keep = new List<ITrigger>();

            foreach (ITrigger trigger in target_trigger_list)
            {
                trigger.TriggerActivate(
                    this,
                    trigger_type: trigger_type,
                    user: user,
                    trigger_event: trigger_event,
                    target: target,
                    info: info
                );

                if (!trigger.IsDurationZero())
                {
                    to_keep.Add(trigger);
                }
            }

            if (to_keep.Count == 0)
            {
                trigger_list[trigger_type].Remove(target);
                if (trigger_list[trigger_type].Count == 0)
                {
                    trigger_list.Remove(trigger_type);
                }
            }
            else
            {
                trigger_list[trigger_type][target] = to_keep;
            }
        }
        else
        {
            List<ITarget> keys = new List<ITarget>(trigger_list[trigger_type].Keys);
            foreach (ITarget t in keys)
            {
                var target_trigger_list = trigger_list[trigger_type][t];
                List<ITrigger> to_keep = new List<ITrigger>();

                foreach (ITrigger trigger in target_trigger_list)
                {
                    trigger.TriggerActivate(
                        this,
                        trigger_type: trigger_type,
                        user: user,
                        trigger_event: trigger_event,
                        target: target,
                        info: info
                    );

                    if (!trigger.IsDurationZero())
                    {
                        to_keep.Add(trigger);
                    }
                }

                if (to_keep.Count == 0)
                {
                    trigger_list[trigger_type].Remove(t);
                    if (trigger_list[trigger_type].Count == 0)
                    {
                        trigger_list.Remove(trigger_type);
                    }
                }
                else
                {
                    trigger_list[trigger_type][t] = to_keep;
                }
            }
        }
    }

    public void RegisterTrigger(
        ITriggerType trigger_type,
        ITrigger trigger,
        ITarget target
    )
    {
        if (!trigger_list.ContainsKey(trigger_type))
        {
            trigger_list[trigger_type] = new Dictionary<ITarget, List<ITrigger>>();
        }

        if (!trigger_list[trigger_type].ContainsKey(target))
        {
            trigger_list[trigger_type][target] = new List<ITrigger>();
        }

        trigger_list[trigger_type][target].Add(trigger);
    }

    public void RegisterEvent(
        IEvent e,
        Object obj
    )
    {
        (bool is_event_from_trigger, ITriggerType? itt) = IsEventFromTrigger(obj);
        if (is_event_from_trigger)
        {
            e.SetEventSourceType(EventSourceType.Trigger);
            if (itt != null && itt.IsSystem())
            {
                e.SetFromSystemTrigger(true);
            }
        }
        else
        {
            e.SetEventSourceType(EventSourceType.Action);
        }

        event_stack.Push(e);
        is_event_seen = false;
    }

    private (bool, ITriggerType?) IsEventFromTrigger(
        Object obj
    )
    {
        bool is_event_from_trigger = obj is ITrigger;
        if (is_event_from_trigger)
        {
            ITrigger trigger = (ITrigger)obj;
            return (is_event_from_trigger, trigger.GetFiredTriggerType());
        }
        return (is_event_from_trigger, null);
    }
}


public interface ITriggerType
{
    public string GetName();
    public bool IsSystem();
}

public class TriggerType : ITriggerType
{
    private string name;
    private bool is_system;

    public TriggerType(
        string name,
        bool is_system = false
    )
    {
        this.name = name;
        this.is_system = is_system;
    }

    public string GetName()
    {
        return name;
    }

    public bool IsSystem()
    {
        return is_system;
    }
}