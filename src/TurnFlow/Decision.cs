namespace TurnFlow;

public interface IDecision
{

}

public interface IDecision<T> : IDecision
{
    public bool Choose(T selection);
    public bool GetChosen(out T chosen);
    public bool HasChosen { get; }
}

public interface IDecisionList<T> : IDecision<T>
{
    public IReadOnlyList<T> GetOptions();
}

public class ListDecision<T> : IDecisionList<T>
{
private IReadOnlyList<T> Options;
    private T chosen;
    public bool HasChosen { get; private set; }

    public ListDecision(IReadOnlyList<T> options)
    {
        Options = options;
        HasChosen = false;
    }
    
    public IReadOnlyList<T> GetOptions()
    {
        return Options;
    }
    

    public bool Choose(T selection)
    {
        if (Options.Contains(selection))
        {
            chosen = selection;
            HasChosen = true;
            return true;
        }
        return false;
    }

    public bool GetChosen(out T chosen)
    {
        if (HasChosen)
        {
            chosen = this.chosen;
            return true;
        }
        chosen = default;
        return false;
    }

}