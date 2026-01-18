namespace TurnFlow;

public interface ITarget
{
    public string TargetType { get; }

    public IComponentManager Components { get; }

    // public IDecisionAgent DecisionAgent { get; }

    public IDecision GetQuickDecisions();
    public IDecision GetDecisions();
}