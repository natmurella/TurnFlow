using System.ComponentModel;
using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.TurnManagerTests;

public static class TriggerTypes
{
    public static readonly ITriggerType on_damage = new TriggerType("on_damage");
    public static readonly ITriggerType on_overkill = new TriggerType("on_overkill");
    public static readonly ITriggerType on_turn_start = new TriggerType("on_turn_start");
    public static readonly ITriggerType on_turn_end = new TriggerType("on_turn_end");
    public static readonly ITriggerType on_game_start = new TriggerType("on_game_start");
    public static readonly ITriggerType on_game_end = new TriggerType("on_game_end");
    public static readonly ITriggerType on_pre_ability_target = new TriggerType("on_pre_ability_target", is_system: true);
    public static readonly ITriggerType on_post_ability_target = new TriggerType("on_post_ability_target", is_system: true);
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



public class BasicCharacter : ITarget
{
    public string TargetType => "Character";

    public IComponentManager Components { get; }

    public List<IAbility> abilities;

    public BasicCharacter(string name, string allegiance = "good")
    {
        ComponentManager cm = new ComponentManager();
        abilities = new List<IAbility>
        {
            new AttackAbility(this),
            new AttackAbility(this)
        };

        // -- define components --
        // details
        cm.CreateString("name", name);
        // stats
        cm.CreateStat("attack");
        cm.CreateStat("defense");
        // bars
        cm.CreateBar("health");
        cm.CreateBar("mana");
        cm.CreateBar("time");
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
        cm.GetStat("attack").AddBuffFlat(5);
        cm.GetStat("defense").AddBuffFlat(0);
        // bars
        IBarComponent health_bar = cm.GetBar("health");
        health_bar.AddBaseFlat(100);
        health_bar.AddScalerMult("defense", 500);
        IBarComponent mana_bar = cm.GetBar("mana");
        mana_bar.AddBaseFlat(20);
        mana_bar.AddScalerMult("attack", 200);
        IBarComponent time_bar = cm.GetBar("time");
        time_bar.AddBaseFlat(10);

        // -- set component manager --
        Components = cm;
    }

    public IDecision GetQuickDecisions()
    {
        return new ListDecision<IAbility>(new List<IAbility>());
    }

    public IDecision GetDecisions()
    {
        IDecisionList<IAbility> decision_list = new ListDecision<IAbility>(this.abilities);
        return decision_list;
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

public class BasicBurnTrigger : TriggerBase
{
    private ITarget owner;
    private ITarget holder;
    private int damage;

    public BasicBurnTrigger(ITarget owner, ITarget holder, int damage = 2)
    {
        this.Duration = 3;
        this.PermanenceType = TriggerPermanenceType.Temporary;
        this.owner = owner;
        this.holder = holder;
        this.damage = damage;
    }

    public override bool CanActivate(
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    )
    {
        // if (target != holder)
        // {
        //     return false;
        // }
        return true;
    }

    public override void Activate(
        ITriggerEngine trigger_engine,
        ITriggerType trigger_type,
        ITarget user,
        IEvent trigger_event,
        ITarget target,
        IInfo info
    )
    {
        Console.WriteLine($"Burn Trigger Activated: {owner.Components.GetString("name").GetDetail()} -> {holder.Components.GetString("name").GetDetail()} for {damage} damage.");

        IEvent fixed_damage_event = new BasicFixedDamageEvent(
            this.owner,
            this.holder,
            damage
        );
        trigger_engine.RegisterEvent(
            fixed_damage_event,
            this
        );
    }
}

public class BasicFixedDamageEvent : EventBase
{
    ITarget user;
    ITarget target;
    int damage;

    public BasicFixedDamageEvent(
        ITarget user,
        ITarget target,
        int damage
    )
    {
        this.user = user;
        this.target = target;
        this.damage = damage;
    }

    public override void Execute(
        ITriggerEngine trigger_engine
    )
    {
        Mechanic.DealFixedDamage(trigger_engine, user, this, target, damage);
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

public class BasicTurnManager : TurnManager
{

    private enum BasicTurnState
    {
        ChooseAbility,
        ChooseFirstTarget,
        ChooseNextTarget,
        ExecuteAbility,
    }

    private BasicTurnState basic_turn_state;
    private bool decision_made;

    private IDecision? current_decision;
    private IAbility? current_ability;

    public BasicTurnManager(
        ITriggerEngine trigger_engine,
        IWorld world
    ) : base(
        trigger_engine,
        world,
        TriggerTypes.on_turn_start,
        TriggerTypes.on_turn_end,
        TriggerTypes.on_game_start,
        TriggerTypes.on_game_end,
        "time"
    )
    {
        this.basic_turn_state = BasicTurnState.ChooseAbility;
        this.decision_made = false;

        this.current_decision = null;
        this.current_ability = null;
    }

    public override IDecision take_turn()
    {
        if (!decision_made)
        {
            return make_decision();
        }
        else
        {
            decision_made = false;
            end_turn();
            return Step();
        }
    }

    public IDecision make_decision()
    {
        if (this.basic_turn_state == BasicTurnState.ChooseAbility)
        {
            ListDecision<ITarget> decision = new ListDecision<ITarget>(new List<ITarget>());
            this.current_decision = decision;
            this.basic_turn_state = BasicTurnState.ChooseFirstTarget;
            return decision;
        }
        else if (this.basic_turn_state == BasicTurnState.ChooseFirstTarget)
        {
            ListDecision<IAbility> ability_dec = (ListDecision<IAbility>)this.current_decision;
            ability_dec.GetChosen(out this.current_ability);

            (AbilityDecisionType adt, IDecision d) = this.current_ability.NextAbilityDecision(this.world);
            if (adt == AbilityDecisionType.Next)
            {
                this.current_decision = d;
                this.basic_turn_state = BasicTurnState.ChooseNextTarget;
                return d;
            }
            else if (adt == AbilityDecisionType.Empty)
            {
                this.basic_turn_state = BasicTurnState.ExecuteAbility;
                this.current_decision = null;
                return Step();
            }
            else
            {
                throw new InvalidEnumArgumentException($"Invalid ability decision type: {adt}");
            }

        }
        else if (this.basic_turn_state == BasicTurnState.ChooseNextTarget)
        {
            this.current_ability.SaveDecision(this.current_decision);
            (AbilityDecisionType adt, IDecision d) = this.current_ability.NextAbilityDecision(this.world);
            if (adt == AbilityDecisionType.Next)
            {
                this.current_decision = d;
                return d;
            }
            else if (adt == AbilityDecisionType.Empty)
            {
                this.basic_turn_state = BasicTurnState.ExecuteAbility;
                this.current_decision = null;
                return Step();
            }
            else
            {
                throw new InvalidEnumArgumentException($"Invalid ability decision type: {adt}");
            }
        }
        else if (this.basic_turn_state == BasicTurnState.ExecuteAbility)
        {
            this.current_ability.Execute(this.trigger_engine);
            decision_made = true;
            return Step();
        }
        else
        {
            throw new InvalidEnumArgumentException($"Invalid turn state: {this.basic_turn_state}");
        }
    }
}





[TestFixture]
public class TurnManagerTests
{
    [Test]
    public void TestBasicTurnManagerTest()
    {
        ITriggerEngine te = new TriggerEngine();
        IWorld world = new BasicWorld();
        BasicTurnManager tm = new BasicTurnManager(
            te,
            world
        );

        ITarget t1 = new BasicCharacter("ch1", "good");
        ITarget t2 = new BasicCharacter("ch2", "bad");

        BasicBurnTrigger burn_trigger_1 = new BasicBurnTrigger(t1, t2, damage: 1);
        te.RegisterTrigger(
            TriggerTypes.on_game_start,
            burn_trigger_1,
            t2
        );
        BasicBurnTrigger burn_trigger_2 = new BasicBurnTrigger(t1, t2, damage: 2);
        te.RegisterTrigger(
            TriggerTypes.on_turn_start,
            burn_trigger_2,
            t2
        );
        BasicBurnTrigger burn_trigger_3 = new BasicBurnTrigger(t1, t2, damage: 3);
        te.RegisterTrigger(
            TriggerTypes.on_turn_end,
            burn_trigger_3,
            t2
        );

        BasicBurnTrigger burn_trigger_4 = new BasicBurnTrigger(t2, t1, damage: 4);
        te.RegisterTrigger(
            TriggerTypes.on_game_start,
            burn_trigger_4,
            t1
        );
        BasicBurnTrigger burn_trigger_5 = new BasicBurnTrigger(t2, t1, damage: 5);
        te.RegisterTrigger(
            TriggerTypes.on_turn_start,
            burn_trigger_5,
            t1
        );
        BasicBurnTrigger burn_trigger_6 = new BasicBurnTrigger(t2, t1, damage: 6);
        te.RegisterTrigger(
            TriggerTypes.on_turn_end,
            burn_trigger_6,
            t1
        );



        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        tm.AddParticipant(t1);
        tm.AddParticipant(t2);

        IDecision d1 = tm.Step();
        Assert.AreEqual(t1, tm.current_turn_target);
        Assert.IsInstanceOf<IDecisionList<ITarget>>(d1);
        IDecisionList<ITarget> decision_list = (IDecisionList<ITarget>)d1;
        Assert.AreEqual(0, decision_list.GetOptions().Count);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(91, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(99, health_curr);
        Assert.AreEqual(100, health_total);
    }
}