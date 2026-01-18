

namespace TurnFlow;


public interface IWorld
{
    public void AddParticipant(ITarget participant, int position);
    public IDecision GetAvailableTargets(ITarget user);
}

public abstract class World : IWorld
{
    private HashSet<ITarget> participants;
    private List<ICondition> conditions;

    public World()
    {
        participants = new HashSet<ITarget>();
        conditions = DefineConditions();
    }

    public abstract List<ICondition> DefineConditions();

    public virtual void AddParticipant(ITarget participant, int position = 0)
    {
        participants.Add(participant);
    }

    public IDecision GetAvailableTargets(ITarget user)
    {
        List<ITarget> available_targets = new List<ITarget>();

        foreach (ITarget target in participants)
        {
            bool is_included = true;
            foreach (ICondition condition in conditions)
            {
                if (!condition.IsTargetIncluded(user, target))
                {
                    is_included = false;
                    break;
                }
            }

            if (is_included)
            {
                available_targets.Add(target);
            }
        }

        IDecision dec = new ListDecision<ITarget>(available_targets);

        return dec;
    }
}

public interface ICondition
{
    public bool IsTargetIncluded(ITarget user, ITarget target);
}
