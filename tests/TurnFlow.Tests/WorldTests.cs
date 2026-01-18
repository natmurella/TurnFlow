
using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.WorldTests;


public class BasicCharacter : ITarget
{
    public string TargetType => "Character";

    public IComponentManager Components { get; }

    public BasicCharacter(string name, string allegiance = "heroes")
    {
        ComponentManager cm = new ComponentManager();

        // -- define components --
        // details
        cm.CreateString("name", name);
        cm.CreateBubble<string>("allegiance", allegiance);
        // targetting structures
        cm.CreateBubble<string>("targeting_team_type", TargetingTeamType.Allies.ToString());
        cm.CreateBubble<string>("targeting_self_type", TargetingSelfType.Default.ToString());


        // -- set component manager --
        Components = cm;
    }

    public IDecision GetQuickDecisions()
    {
        return null;
    }

    public IDecision GetDecisions()
    {
        return null;
    }
}

public class BasicWorld : World
{
    public BasicWorld() : base()
    {

    }

    public override List<ICondition> DefineConditions()
    {
        return new List<ICondition>
        {
            new TeamTypeCondition("targeting_team_type"),
            new SelfTypeCondition("targeting_self_type"),
            
        };
    }
}

public enum TargetingTeamType
{
    Default,
    Allies,
    Enemies
}

public enum TargetingSelfType
{
    Default,
    ExcludeSelf,
    IncludeSelfOnly
}

public class TeamTypeCondition : ICondition
{
    private string targeting_team_type_id;
    private string allegiance_string_id;

    public TeamTypeCondition(
        string targeting_team_type_id = "targeting_team_type",
        string allegiance_string_id = "allegiance"
    )
    {
        this.targeting_team_type_id = targeting_team_type_id;
        this.allegiance_string_id = allegiance_string_id;
    }

    public bool IsTargetIncluded(ITarget user, ITarget target)
    {
        string targeting_team_type_str = user.Components.GetString(targeting_team_type_id).GetDetail();
        if (!Enum.TryParse(targeting_team_type_str, out TargetingTeamType targeting_team_type))
        {
            throw new ArgumentException($"Invalid targeting team type: {targeting_team_type_str}");
        }

        if (targeting_team_type == TargetingTeamType.Default)
        {
            return true;
        }

        string user_allegiance = user.Components.GetString(allegiance_string_id).GetDetail();
        string target_allegiance = target.Components.GetString(allegiance_string_id).GetDetail();

        if (targeting_team_type == TargetingTeamType.Allies)
        {
            return user_allegiance == target_allegiance;
        }
        else if (targeting_team_type == TargetingTeamType.Enemies)
        {
            return user_allegiance != target_allegiance;
        }

        return false;
    }
}

public class SelfTypeCondition : ICondition
{
    private string targeting_self_type_id;

    public SelfTypeCondition(
        string targeting_self_type_id = "targeting_self_type"
    )
    {
        this.targeting_self_type_id = targeting_self_type_id;
    }

    public bool IsTargetIncluded(ITarget user, ITarget target)
    {
        string targeting_self_type_str = user.Components.GetString(targeting_self_type_id).GetDetail();
        if (!Enum.TryParse(targeting_self_type_str, out TargetingSelfType targeting_self_type))
        {
            throw new ArgumentException($"Invalid targeting self type: {targeting_self_type_str}");
        }

        if (targeting_self_type == TargetingSelfType.Default)
        {
            return true;
        }
        else if (targeting_self_type == TargetingSelfType.ExcludeSelf)
        {
            return user != target;
        }
        else if (targeting_self_type == TargetingSelfType.IncludeSelfOnly)
        {
            return user == target;
        }

        return false;
    }
}


[TestFixture]
public class TurnManagerTests
{
    [Test]
    public void TestBasicWorldTest()
    {
        BasicWorld world = new BasicWorld();

        ITarget c1 = new BasicCharacter("ch1", "heroes");
        ITarget c2 = new BasicCharacter("ch2", "heroes");
        ITarget c3 = new BasicCharacter("ch3", "villains");
        ITarget c4 = new BasicCharacter("ch4", "villains");

        world.AddParticipant(c1);
        world.AddParticipant(c2);
        world.AddParticipant(c3);
        world.AddParticipant(c4);

        IBubbleComponent<string> c1_team_bubble = c1.Components.GetStringBubble("targeting_team_type");
        IBubbleComponent<string> c1_self_bubble = c1.Components.GetStringBubble("targeting_self_type");

        // c1 allies
        c1_team_bubble.AddBubble(TargetingTeamType.Allies.ToString(), c1);
        c1_self_bubble.AddBubble(TargetingSelfType.Default.ToString(), c1);
        IDecisionList<ITarget> dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c1);
        IReadOnlyList<ITarget> availableTargets = dec.GetOptions();
        Assert.AreEqual(2, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c1));
        Assert.IsTrue(availableTargets.Contains(c2));

        // c1 enemies
        c1_team_bubble.AddBubble(TargetingTeamType.Enemies.ToString(), c1);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c1);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(2, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c3));
        Assert.IsTrue(availableTargets.Contains(c4));

        // c1 allies not self
        // c1_team_bubble.RemoveBubble(TargetingTeamType.Enemies.ToString(), c1);
        c1_team_bubble.AddBubble(TargetingTeamType.Allies.ToString(), c1);
        c1_self_bubble.AddBubble(TargetingSelfType.ExcludeSelf.ToString(), c1);
        Assert.AreEqual(TargetingSelfType.ExcludeSelf.ToString(), c1_self_bubble.GetTopBubble());
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c1);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(TargetingTeamType.Allies.ToString(), c1_team_bubble.GetTopBubble());
        Assert.AreEqual(TargetingSelfType.ExcludeSelf.ToString(), c1_self_bubble.GetTopBubble());
        Assert.AreEqual(1, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c2));

        // c1 allies self only
        c1_self_bubble.AddBubble(TargetingSelfType.IncludeSelfOnly.ToString(), c1);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c1);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(1, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c1));

        // c1 all
        c1_team_bubble.AddBubble(TargetingTeamType.Default.ToString(), c1);
        c1_self_bubble.AddBubble(TargetingSelfType.Default.ToString(), c1);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c1);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(4, availableTargets.Count);

        IBubbleComponent<string> c3_team_bubble = c3.Components.GetStringBubble("targeting_team_type");
        IBubbleComponent<string> c3_self_bubble = c3.Components.GetStringBubble("targeting_self_type");

        // c3 allies
        c3_team_bubble.AddBubble(TargetingTeamType.Allies.ToString(), c3);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c3);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(2, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c3));
        Assert.IsTrue(availableTargets.Contains(c4));

        // c3 enemies
        c3_team_bubble.AddBubble(TargetingTeamType.Enemies.ToString(), c3);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c3);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(2, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c1));
        Assert.IsTrue(availableTargets.Contains(c2));

        // c3 allies not self
        c3_team_bubble.RemoveBubble(TargetingTeamType.Enemies.ToString(), c3);
        c3_self_bubble.AddBubble(TargetingSelfType.ExcludeSelf.ToString(), c3);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c3);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(1, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c4));

        // c3 allies self only
        c3_self_bubble.AddBubble(TargetingSelfType.IncludeSelfOnly.ToString(), c3);
        dec = (IDecisionList<ITarget>)world.GetAvailableTargets(c3);
        availableTargets = dec.GetOptions();
        Assert.AreEqual(1, availableTargets.Count);
        Assert.IsTrue(availableTargets.Contains(c3));


    }
}