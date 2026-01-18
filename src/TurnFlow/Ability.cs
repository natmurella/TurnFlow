
namespace TurnFlow;

public enum AbilityDecisionType
{
    Next,
    Empty
}


public interface IAbility
{
    public void Execute(ITriggerEngine trigger_engine);
    public (AbilityDecisionType, IDecision) NextAbilityDecision(IWorld world);
    public void SaveDecision(IDecision decision);
    public void ResetDecisions();
}

public abstract class Ability : IAbility
{

    protected ITarget user;
    private List<ITarget> targets;
    private ITargetingProfile profile;
    protected int decision_index;


    public Ability(ITarget user)
    {
        this.user = user;
        targets = new List<ITarget>();
        decision_index = 0;
        profile = GetTargetingProfile();
    }

    public void ExecuteAbility(ITriggerEngine trigger_engine)
    {
        Execute(trigger_engine);
        ResetDecisions();
    }

    public virtual void ResetDecisions()
    {

    }


    public virtual void Execute(ITriggerEngine trigger_engine)
    {
        // Default implementation does nothing
    }

    public (AbilityDecisionType, IDecision) NextAbilityDecision(IWorld world)
    {
        profile.ApplyTargettingProfile(user, decision_index);
        (AbilityDecisionType, IDecision) decision_output = NextDecision(world);
        profile.RemoveTargettingProfile(user, decision_index);
        UpdateDecisionIndex();
        return decision_output;
    }

    public virtual (AbilityDecisionType, IDecision) NextDecision(IWorld world)
    {
        return (AbilityDecisionType.Empty, null);
    }

    public virtual void UpdateDecisionIndex()
    {

    }

    public virtual void SaveDecision(IDecision decision)
    {

    }

    public abstract ITargetingProfile GetTargetingProfile();
}

public interface ITargetingProfile
{
    public void ApplyTargettingProfile(ITarget user, int decision_index);
    public void RemoveTargettingProfile(ITarget user, int decision_index);
}

public abstract class TargetingProfile : ITargetingProfile
{
    public abstract void ApplyTargettingProfile(ITarget user, int decision_index);
    public abstract void RemoveTargettingProfile(ITarget user, int decision_index);
}