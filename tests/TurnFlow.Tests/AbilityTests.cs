

using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.AbilityTests;

public static class TriggerTypes
{
    public static readonly ITriggerType on_damage = new TriggerType("on_damage");
    public static readonly ITriggerType on_overkill = new TriggerType("on_overkill");
    public static readonly ITriggerType on_turn_end = new TriggerType("on_turn_end");

    public static readonly List<ITriggerType> All = new List<ITriggerType>
    {
        on_damage,
        on_overkill,
        on_turn_end
    };
}


public static class Mechanic
{
    public static void DealDamage(
        ITriggerEngine trigger_engine,
        ITarget user,
        IEvent trigger_event,
        ITarget target
    )
    {

        int attack_value = user.Components.GetStat("attack").GetValue();
        int defense_value = target.Components.GetStat("defense").GetValue();
        int damage = attack_value - defense_value;
        if (damage <= 0)
        {
            damage = 0;
        }
        else
        {

            (int bar_damage, int bar_overkill) = target.Components.GetBar("health").DamageBar(damage);
            IInfo info = new Info();
            info.IntInfo.Add("damage", bar_damage);
            info.IntInfo.Add("overkill", bar_overkill);
            if (bar_damage > 0)
            {
                trigger_engine.Trigger(
                    TriggerTypes.on_damage,
                    user,
                    trigger_event,
                    target,
                    info
                );
            }
            if (bar_overkill > 0)
            {
                trigger_engine.Trigger(
                    TriggerTypes.on_overkill,
                    user,
                    trigger_event,
                    target,
                    info
                );
            }
        }


    }

    public static void DealHeal(
        ITriggerEngine trigger_engine,
        ITarget user,
        IEvent trigger_event,
        ITarget target
    )
    {
        int heal_value = user.Components.GetStat("attack").GetValue();
        int defense_value = target.Components.GetStat("defense").GetValue();
        int heal = heal_value - defense_value;
        if (heal <= 0)
        {
            heal = 0;
        }
        else
        {
            (int bar_heal, int bar_overheal) = target.Components.GetBar("health").HealBar(heal);
        }
    }
    
    public static void DealFixedDamage(
        ITriggerEngine trigger_engine,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        int damage
    )
    {
        if (damage <= 0)
        {
            return;
        }

        (int bar_damage, int bar_overkill) = target.Components.GetBar("health").DamageBar(damage);
        IInfo info = new Info();
        info.IntInfo.Add("damage", bar_damage);
        info.IntInfo.Add("overkill", bar_overkill);
        if (bar_damage > 0)
        {
            trigger_engine.Trigger(
                TriggerTypes.on_damage,
                user,
                trigger_event,
                target,
                info
            );
        }
        if (bar_overkill > 0)
        {
            trigger_engine.Trigger(
                TriggerTypes.on_overkill,
                user,
                trigger_event,
                target,
                info
            );
        }
    }
}

public class BasicAttackEvent : EventBase
{
    ITarget user;
    ITarget target;

    public BasicAttackEvent(
        ITarget user,
        ITarget target
    )
    {
        this.user = user;
        this.target = target;
    }

    public override void Execute(
        ITriggerEngine trigger_engine
    )
    {
        Mechanic.DealDamage(trigger_engine, user, this, target);
    }
}

public class BasicCharacter : ITarget
{
    public string TargetType => "Character";

    public IComponentManager Components { get; }

    public BasicCharacter(string name, int attack = 10, int defense = 5, string allegiance = "heroes")
    {
        ComponentManager cm = new ComponentManager();

        // -- define components --
        // details
        cm.CreateString("name", name);
        // stats
        cm.CreateStat("attack");
        cm.CreateStat("defense");
        // bars
        cm.CreateBar("health");
        cm.CreateBar("mana");
        // bubble details
        cm.CreateBubble<string>("allegiance", allegiance);

        // targetting structures
        cm.CreateBubble<string>("targeting_team_type", TargetingTeamType.Allies.ToString());
        cm.CreateBubble<string>("targeting_self_type", TargetingSelfType.Default.ToString());

        // -- define additional interactions --
        // bars
        cm.AddStatScalersForBarTotal(
            "health",
            new List<string>
            {
                "defense"
            }
        );
        cm.AddStatScalersForBarTotal(
            "mana",
            new List<string>
            {
                "attack"
            }
        );

        // -- apply baselines --
        // stats
        cm.GetStat("attack").AddBuffFlat(attack);
        cm.GetStat("defense").AddBuffFlat(defense);
        // bars
        IBarComponent health_bar = cm.GetBar("health");
        health_bar.AddBaseFlat(100);
        health_bar.AddScalerMult("defense", 500);
        IBarComponent mana_bar = cm.GetBar("mana");
        mana_bar.AddBaseFlat(20);
        mana_bar.AddScalerMult("attack", 200);

        // -- set component manager --
        Components = cm;
    }

    public IDecision GetQuickDecisions()
    {
        // Implementation for quick decisions
        return null; // Placeholder
    }

    public IDecision GetDecisions()
    {
        // Implementation for decisions
        return null; // Placeholder
    }
}

public class AttackingTargetingProfile : TargetingProfile
{
    private string targeting_team_type = TargetingTeamType.Enemies.ToString();
    private string targeting_self_type = TargetingSelfType.ExcludeSelf.ToString();

    public override void ApplyTargettingProfile(ITarget user, int decision_index)
    {
        IBubbleComponent<string> targeting_team_type_bubble = user.Components.GetStringBubble("targeting_team_type");
        IBubbleComponent<string> targeting_self_type_bubble = user.Components.GetStringBubble("targeting_self_type");

        targeting_team_type_bubble.AddBubble(targeting_team_type, this, is_base: true);
        targeting_self_type_bubble.AddBubble(targeting_self_type, this, is_base: true);
    }

    public override void RemoveTargettingProfile(ITarget user, int decision_index)
    {
        IBubbleComponent<string> targeting_team_type_bubble = user.Components.GetStringBubble("targeting_team_type");
        IBubbleComponent<string> targeting_self_type_bubble = user.Components.GetStringBubble("targeting_self_type");

        targeting_team_type_bubble.RemoveBubble(targeting_team_type, this);
        targeting_self_type_bubble.RemoveBubble(targeting_self_type, this);
    }
}

public class AttackAbility : Ability
{
    private ITarget target = null;

    public AttackAbility(ITarget user) : base(user)
    {

    }

    public override void Execute(ITriggerEngine trigger_engine)
    {

    }

    public override (AbilityDecisionType, IDecision) NextDecision(IWorld world)
    {
        if (target == null)
        {
            IDecision dec = world.GetAvailableTargets(user);
            return (AbilityDecisionType.Next, dec);
        }
        return (AbilityDecisionType.Empty, null);
    }

    public override void SaveDecision(IDecision decision)
    {
        if (target == null)
        {
            IDecisionList<ITarget> dec = (IDecisionList<ITarget>)decision;
            ITarget chosen;
            dec.GetChosen(out chosen);
            target = chosen;
        }
    }

    public override ITargetingProfile GetTargetingProfile()
    {
        return new AttackingTargetingProfile();
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
            return target != user;
        }
        else if (targeting_self_type == TargetingSelfType.IncludeSelfOnly)
        {
            return target == user;
        }

        return false;
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
            new TeamTypeCondition(),
            new SelfTypeCondition()
        };
    }
}


[TestFixture]
public class AbilityTests
{
    [Test]
    public void TestBasicAbilityTest()
    {
        BasicWorld w = new BasicWorld();
        ITarget c1 = new BasicCharacter("ch1", 10, 5, "heroes");
        ITarget c2 = new BasicCharacter("ch2", 10, 5, "villains");
        IAbility a = new AttackAbility(c1);

        w.AddParticipant(c1);
        w.AddParticipant(c2);

        (AbilityDecisionType d_t, IDecision dec) = a.NextAbilityDecision(w);

        IDecisionList<ITarget> dec_list = (IDecisionList<ITarget>)dec;
        dec_list.Choose(dec_list.GetOptions()[0]);
        a.SaveDecision(dec);

        (AbilityDecisionType d_t2, IDecision dec2) = a.NextAbilityDecision(w);
        IDecisionList<ITarget> dec_list2 = (IDecisionList<ITarget>)dec;
        Assert.AreEqual(AbilityDecisionType.Next, d_t);
        Assert.AreEqual(1, dec_list2.GetOptions().Count);
        Assert.AreEqual(AbilityDecisionType.Empty, d_t2);

    }

    [Test]
    public void TestAttackingTargetingProfileTest()
    {
        ITarget user = new BasicCharacter("user", 10, 5, "heroes");
        ITargetingProfile profile = new AttackingTargetingProfile();

        profile.ApplyTargettingProfile(user, 0);
        IBubbleComponent<string> targeting_team_type_bubble = user.Components.GetStringBubble("targeting_team_type");
        IBubbleComponent<string> targeting_self_type_bubble = user.Components.GetStringBubble("targeting_self_type");

        Assert.AreEqual(TargetingTeamType.Enemies.ToString(), targeting_team_type_bubble.GetTopBubble());
        Assert.AreEqual(TargetingSelfType.ExcludeSelf.ToString(), targeting_self_type_bubble.GetTopBubble());

        profile.RemoveTargettingProfile(user, 0);

        Assert.AreEqual(TargetingTeamType.Allies.ToString(), targeting_team_type_bubble.GetTopBubble());
        Assert.AreEqual(TargetingSelfType.Default.ToString(), targeting_self_type_bubble.GetTopBubble());

        targeting_team_type_bubble.AddBubble(TargetingTeamType.Default.ToString(), user);
        profile.ApplyTargettingProfile(user, 0);

        Assert.AreEqual(TargetingTeamType.Default.ToString(), targeting_team_type_bubble.GetTopBubble());
    }
}