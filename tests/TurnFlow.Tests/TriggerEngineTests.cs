
using System.Net;
using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.TriggerEngineTests;


public static class TriggerTypes
{
    public static readonly ITriggerType on_damage = new TriggerType("on_damage");
    public static readonly ITriggerType on_overkill = new TriggerType("on_overkill");
    public static readonly ITriggerType on_turn_end = new TriggerType("on_turn_end");
    public static readonly ITriggerType on_pre_ability = new TriggerType("on_pre_ability", is_system: true);

    public static readonly List<ITriggerType> All = new List<ITriggerType>
    {
        on_damage,
        on_overkill,
        on_turn_end,
        on_pre_ability
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

public class BasicDamageReflectTrigger : TriggerBase
{

    private ITarget owner;
    private int damage;

    public BasicDamageReflectTrigger(ITarget owner, int damage = 3)
    {
        this.Duration = 1;
        this.PermanenceType = TriggerPermanenceType.Permanent;
        this.owner = owner;
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
        EventSourceType event_source_type = trigger_event.GetEventSourceType();
        if (event_source_type == EventSourceType.Action && target == this.owner)
        {
            return true;
        }
        return false;
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
        IEvent fixed_damage_event = new BasicFixedDamageEvent(
            this.owner,
            user,
            damage
        );
        trigger_engine.RegisterEvent(
            fixed_damage_event,
            this
        );
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

public class BasicHealEvent : EventBase
{
    ITarget user;
    ITarget target;

    public BasicHealEvent(
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
        Mechanic.DealHeal(trigger_engine, user, this, target);
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

public class BasicCharacter : ITarget
{
    public string TargetType => "Character";

    public IComponentManager Components { get; }

    public BasicCharacter(string name, int attack = 10, int defense = 5)
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
        IBarComponent mana_bar = cm.GetBar("mana");
        mana_bar.AddBaseFlat(20);

        // -- set component manager --
        Components = cm;
    }

    public IDecision GetQuickDecisions()
    {
        // Implement quick decisions logic here
        return null;
    }

    public IDecision GetDecisions()
    {
        // Implement decisions logic here
        return null;
    }
}


[TestFixture]
public class TriggerEngineTests
{
    [Test]
    public void TestNewActionEvent()
    {
        ITriggerEngine te = new TriggerEngine();

        ITarget t1 = new BasicCharacter("c1", 20, 0);
        ITarget t2 = new BasicCharacter("c2", 0, 5);

        IEvent attack_event = new BasicAttackEvent(t1, t2);

        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        te.RegisterEvent(attack_event, t1);

        StepState st1 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st1.StateType);
        Assert.AreEqual(attack_event, st1.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st2 = te.step();

        Assert.AreEqual(StepStateType.Empty, st2.StateType);
        Assert.IsNull(st2.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(85, health_curr);
        Assert.AreEqual(100, health_total);

    }

    [Test]
    public void TestNewTriggerEvent()
    {
        ITriggerEngine te = new TriggerEngine();

        ITarget t1 = new BasicCharacter("c1", 20, 0);
        ITarget t2 = new BasicCharacter("c2", 0, 5);

        IEvent attack_event = new BasicAttackEvent(t1, t2);

        BasicDamageReflectTrigger reflect_trigger = new BasicDamageReflectTrigger(t2, damage: 3);
        te.RegisterTrigger(
            TriggerTypes.on_damage,
            reflect_trigger,
            t2
        );

        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        te.RegisterEvent(attack_event, t1);

        StepState st1 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st1.StateType);
        Assert.AreEqual(attack_event, st1.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st2 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st2.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st2.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(85, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st3 = te.step();

        Assert.AreEqual(StepStateType.Empty, st3.StateType);
        Assert.IsNull(st3.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(85, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(97, health_curr);
        Assert.AreEqual(100, health_total);
    }

    [Test]
    public void TestActionAndTriggerEventCheck()
    {
        TriggerEngine temp = new TriggerEngine();
        ITriggerEngine te = temp;

        ITarget t1 = new BasicCharacter("c1", 20, 0);
        ITarget t2 = new BasicCharacter("c2", 0, 5);

        IEvent attack_event = new BasicAttackEvent(t1, t2);

        BasicDamageReflectTrigger reflect_trigger_t1 = new BasicDamageReflectTrigger(t1, damage: 2);
        te.RegisterTrigger(
            TriggerTypes.on_damage,
            reflect_trigger_t1,
            t1
        );
        BasicDamageReflectTrigger reflect_trigger_t2 = new BasicDamageReflectTrigger(t2, damage: 3);
        te.RegisterTrigger(
            TriggerTypes.on_damage,
            reflect_trigger_t2,
            t2
        );

        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        te.RegisterEvent(attack_event, t1);

        StepState st1 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st1.StateType);
        Assert.AreEqual(attack_event, st1.Event);

        StepState st2 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st2.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st2.Event);

        StepState st3 = te.step();

        Assert.AreEqual(StepStateType.Empty, st3.StateType);
        Assert.IsNull(st3.Event);

        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(85, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(97, health_curr);
        Assert.AreEqual(100, health_total);
    }

    [Test]
    public void TestDOTStatusTrigger()
    {
        ITriggerEngine te = new TriggerEngine();

        ITarget t1 = new BasicCharacter("c1", 20, 0);
        ITarget t2 = new BasicCharacter("c2", 0, 5);

        IEvent attack_event = new BasicAttackEvent(t1, t2);

        BasicBurnTrigger burn_trigger_t1 = new BasicBurnTrigger(t2, t1, damage: 4);
        te.RegisterTrigger(
            TriggerTypes.on_turn_end,
            burn_trigger_t1,
            t1
        );
        BasicBurnTrigger burn_trigger_t2 = new BasicBurnTrigger(t1, t2, damage: 2);
        te.RegisterTrigger(
            TriggerTypes.on_turn_end,
            burn_trigger_t2,
            t2
        );

        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        te.Trigger(
            TriggerTypes.on_turn_end,
            null,
            null,
            null,
            new Info()
        );

        StepState st1 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st1.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st1.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st2 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st2.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st2.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(98, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st3 = te.step();

        Assert.AreEqual(StepStateType.Empty, st3.StateType);
        Assert.IsNull(st3.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(98, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(96, health_curr);
        Assert.AreEqual(100, health_total);

        te.Trigger(
            TriggerTypes.on_turn_end,
            null,
            null,
            null,
            new Info()
        );

        StepState st4 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st4.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st4.Event);

        StepState st5 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st5.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st5.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(96, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(96, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st6 = te.step();
        Assert.AreEqual(StepStateType.Empty, st6.StateType);
        Assert.IsNull(st6.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(96, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(92, health_curr);
        Assert.AreEqual(100, health_total);

        te.Trigger(
            TriggerTypes.on_turn_end,
            null,
            null,
            null,
            new Info()
        );

        StepState st7 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st7.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st7.Event);

        StepState st8 = te.step();

        Assert.AreEqual(StepStateType.PreEvent, st8.StateType);
        Assert.IsInstanceOf<BasicFixedDamageEvent>(st8.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(94, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(92, health_curr);
        Assert.AreEqual(100, health_total);

        StepState st9 = te.step();

        Assert.AreEqual(StepStateType.Empty, st9.StateType);
        Assert.IsNull(st9.Event);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(94, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(88, health_curr);
        Assert.AreEqual(100, health_total);

        te.Trigger(
            TriggerTypes.on_turn_end,
            null,
            null,
            null,
            new Info()
        );

        StepState st10 = te.step();

        Assert.AreEqual(StepStateType.Empty, st10.StateType);
        Assert.IsNull(st10.Event);

        StepState st11 = te.step();

        Assert.AreEqual(StepStateType.Empty, st11.StateType);
        Assert.IsNull(st11.Event);
    }

    [Test]
    public void TestSystemTrigger()
    {
        ITriggerEngine te = new TriggerEngine();

        ITarget t1 = new BasicCharacter("c1", 20, 0);
        ITarget t2 = new BasicCharacter("c2", 0, 5);

        BasicBurnTrigger burn_trigger_t1 = new BasicBurnTrigger(t2, t1, damage: 4);
        te.RegisterTrigger(
            TriggerTypes.on_pre_ability,
            burn_trigger_t1,
            t1
        );
        BasicBurnTrigger burn_trigger_t2 = new BasicBurnTrigger(t1, t2, damage: 20);
        te.RegisterTrigger(
            TriggerTypes.on_pre_ability,
            burn_trigger_t2,
            t2
        );
        BasicBurnTrigger burn_trigger_t3 = new BasicBurnTrigger(t2, t1, damage: 10);
        te.RegisterTrigger(
            TriggerTypes.on_pre_ability,
            burn_trigger_t3,
            t1
        );

        t1.Components.ResetAllBars();
        t2.Components.ResetAllBars();
        (int health_curr, int health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);

        te.Trigger(
            TriggerTypes.on_pre_ability,
            null,
            null,
            null,
            new Info()
        );

        StepState st1 = te.step();

        Assert.AreEqual(StepStateType.Empty, st1.StateType);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(86, health_curr);
        Assert.AreEqual(100, health_total);
        (health_curr, health_total) = t2.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(80, health_curr);
        Assert.AreEqual(100, health_total);
    }
}