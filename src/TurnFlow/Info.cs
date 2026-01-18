namespace TurnFlow;

public enum TriggerGroupType
{
    System,
    Default
}

public interface IInfo
{
    public void SetTriggerGroup(TriggerGroupType group);
    public TriggerGroupType GetTriggerGroup();
    public Dictionary<string, string> StringInfo { get; set; }
    public Dictionary<string, int> IntInfo { get; set; }
}

public class Info : IInfo
{
    private TriggerGroupType triggerGroup;
    public Dictionary<string, string> StringInfo { get; set; }
    public Dictionary<string, int> IntInfo { get; set; }

    public Info()
    {
        triggerGroup = TriggerGroupType.Default;
        StringInfo = new Dictionary<string, string>();
        IntInfo = new Dictionary<string, int>();
    }

    public void SetTriggerGroup(TriggerGroupType group)
    {
        triggerGroup = group;
    }

    public TriggerGroupType GetTriggerGroup()
    {
        return triggerGroup;
    }
    

}