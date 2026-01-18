
using System.ComponentModel;
using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.ComponentsTests;


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
        // set strings
        cm.CreateSet<string>("targeting");
        // paired sets string ints
        cm.CreatePairedSet<string, int>("bar_thresh");
        // int bound bubble
        cm.CreateBoundBubble<int>("min_bound", 100, BoundType.Minimum);
        cm.CreateBoundBubble<int>("max_bound", 0, BoundType.Maximum);
        // pair bubble
        cm.CreateBoundPairedBubble<string, int>("targetting_bar_minimum", BoundType.Minimum);

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




[TestFixture]
public class TriggerEngineTests
{
    [Test]
    public void TestStringDetailComponents()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;
        ComponentManager c1 = (ComponentManager)t1.Components;

        // check name and detail
        Assert.AreEqual("c1", t1.Components.GetString("name").GetDetail());
    }

    [Test]
    public void TestStatComponentInitialization()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 10, 5);
        ITarget t1 = ch1;
        ComponentManager c1 = (ComponentManager)t1.Components;

        // check stats
        Assert.AreEqual(10, t1.Components.GetInteger("stat_attack_add_flat").GetValue());
        Assert.AreEqual(0, t1.Components.GetInteger("stat_attack_add_mult").GetValue());

        Assert.AreEqual(10, t1.Components.GetStat("attack").GetValue());

        Assert.AreEqual(5, t1.Components.GetStat("defense").GetValue());
    }

    [Test]
    public void TestPreCalcAndCalcWithBarModifies()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 10, 5);
        ITarget t1 = ch1;
        ComponentManager c1 = (ComponentManager)t1.Components;

        // check health bar
        Assert.AreEqual(100, t1.Components.GetInteger("bar_health_total_base").GetValue());
        (int health_curr, int health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(0, health_curr);
        Assert.AreEqual(125, health_total);

        // check health bar increase base
        t1.Components.GetBar("health").AddBaseFlat(50);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(0, health_curr);
        Assert.AreEqual(175, health_total);

        // check health bar increase defense mult
        t1.Components.GetBar("health").AddScalerMult("defense", 100);
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(0, health_curr);
        Assert.AreEqual(180, health_total);
    }

    [Test]
    public void TestBarResetDefault()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 10, 5);
        ITarget t1 = ch1;
        ComponentManager c1 = (ComponentManager)t1.Components;

        (int health_curr, int health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(0, health_curr);
        Assert.AreEqual(125, health_total);

        // reset bar to default
        t1.Components.GetBar("health").ResetToDefault();
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(125, health_curr);
        Assert.AreEqual(125, health_total);
    }

    [Test]
    public void TestResetAllBarsToDefault()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;
        // ComponentManager c1 = (ComponentManager)t1.Components;

        // check initial bar values
        (int health_curr, int health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(0, health_curr);
        Assert.AreEqual(100, health_total);
        (int mana_curr, int mana_total) = t1.Components.GetBar("mana").GetBarValues();
        Assert.AreEqual(0, mana_curr);
        Assert.AreEqual(20, mana_total);

        // reset all bars
        t1.Components.ResetAllBars();
        (health_curr, health_total) = t1.Components.GetBar("health").GetBarValues();
        Assert.AreEqual(100, health_curr);
        Assert.AreEqual(100, health_total);
        (mana_curr, mana_total) = t1.Components.GetBar("mana").GetBarValues();
        Assert.AreEqual(20, mana_curr);
        Assert.AreEqual(20, mana_total);
    }

    [Test]
    public void TestStringBubbleTests()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0, "heroes");
        ITarget t1 = ch1;
        BasicCharacter ch2 = new BasicCharacter("c2", 0, 0, "villains");
        ITarget t2 = ch2;
        BasicCharacter ch3 = new BasicCharacter("c3", 0, 0, "neutral");
        ITarget t3 = ch3;
        // ComponentManager c1 = (ComponentManager)t1.Components;

        // check allegiance bubble
        IDetailComponent<string> allegianceDetail = t1.Components.GetString("allegiance");
        Assert.AreEqual("heroes", allegianceDetail.GetDetail());

        IBubbleComponent<string> allegianceBubble = t1.Components.GetStringBubble("allegiance");
        Assert.AreEqual("heroes", allegianceBubble.GetTopBubble());

        allegianceBubble.AddBubble("villains", ch2);
        Assert.AreEqual("villains", allegianceBubble.GetTopBubble());

        allegianceBubble.AddBubble("neutral", ch3);
        Assert.AreEqual("neutral", allegianceBubble.GetTopBubble());

        allegianceBubble.RemoveBubble("villains", ch2);
        Assert.AreEqual("neutral", allegianceBubble.GetTopBubble());

        allegianceBubble.RemoveBubble("neutral", ch3);
        Assert.AreEqual("heroes", allegianceBubble.GetTopBubble());
    }

    [Test]
    public void TestStringSetTests()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;
        BasicCharacter ch2 = new BasicCharacter("c2", 0, 0);
        ITarget t2 = ch2;

        ISetComponent<string> targetingSet = t1.Components.GetStringSet("targeting");
        Assert.AreEqual(0, targetingSet.GetItems().Count);

        targetingSet.AddItemSource("c2_1", t2);
        Assert.AreEqual(1, targetingSet.GetItems().Count);

        targetingSet.AddItemSource("c2_2", t2);
        Assert.AreEqual(2, targetingSet.GetItems().Count);

        targetingSet.AddItemSource("c2_1", t2);
        Assert.AreEqual(2, targetingSet.GetItems().Count);

        targetingSet.AddItemSource("c2_1", t1);
        Assert.AreEqual(2, targetingSet.GetItems().Count);

        Assert.IsTrue(targetingSet.ContainsItem("c2_1"));
        Assert.IsTrue(targetingSet.ContainsItem("c2_1"));
        Assert.IsFalse(targetingSet.ContainsItem("c2_3"));

        targetingSet.RemoveItemSource("c2_1", t2);
        Assert.AreEqual(2, targetingSet.GetItems().Count);
        Assert.IsTrue(targetingSet.ContainsItem("c2_1"));

        targetingSet.RemoveItemSource("c2_1", t1);
        Assert.AreEqual(2, targetingSet.GetItems().Count);
        Assert.IsTrue(targetingSet.ContainsItem("c2_1"));

        targetingSet.RemoveItemSource("c2_1", t2);
        Assert.AreEqual(1, targetingSet.GetItems().Count);
        Assert.IsFalse(targetingSet.ContainsItem("c2_1"));

        targetingSet.RemoveItemSource("c2_2", t2);
        Assert.AreEqual(0, targetingSet.GetItems().Count);
        Assert.IsFalse(targetingSet.ContainsItem("c2_2"));
    }

    [Test]
    public void TestStringIntPairedSetTests()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;
        BasicCharacter ch2 = new BasicCharacter("c2", 0, 0);
        ITarget t2 = ch2;

        IPairedSetComponent<string, int> barThreshSet = t1.Components.GetStringIntPairedSet("bar_thresh");
        Assert.AreEqual(0, barThreshSet.GetPairedItems().Count);

        barThreshSet.AddPairedItem("c2_1", 10, t2);
        Assert.AreEqual(1, barThreshSet.GetPairedItems().Count);
        Assert.IsTrue(barThreshSet.ContainsPairedItem("c2_1", 10));
        Assert.IsFalse(barThreshSet.ContainsPairedItem("c2_1", 20));

        barThreshSet.AddPairedItem("c2_2", 20, t2);
        Assert.AreEqual(2, barThreshSet.GetPairedItems().Count);
        Assert.IsTrue(barThreshSet.ContainsPairedItem("c2_2", 20));

        barThreshSet.AddPairedItem("c2_2", 10, t2);
        Assert.AreEqual(2, barThreshSet.GetPairedItems().Count);
        Assert.AreEqual(2, barThreshSet.GetPairedItems()["c2_2"].Count);

        barThreshSet.AddPairedItem("c2_2", 10, t2);
        Assert.AreEqual(2, barThreshSet.GetPairedItems().Count);
        Assert.IsTrue(barThreshSet.ContainsPairedItem("c2_2", 10));
        Assert.AreEqual(2, barThreshSet.GetPairedItems()["c2_2"].Count);

        barThreshSet.RemovePairedItem("c2_1", 10, t2);
        foreach (var item in barThreshSet.GetPairedItems())
        {
            Console.WriteLine($"{item.Key}: {string.Join(", ", item.Value)}");
        }
        Assert.AreEqual(1, barThreshSet.GetPairedItems().Count);
        Assert.IsFalse(barThreshSet.ContainsPairedItem("c2_1", 10));

        barThreshSet.RemovePairedItem("c2_2", 10, t2);
        Assert.AreEqual(1, barThreshSet.GetPairedItems().Count);
        Assert.IsTrue(barThreshSet.ContainsPairedItem("c2_2", 10));
        Assert.AreEqual(2, barThreshSet.GetPairedItems()["c2_2"].Count);

        barThreshSet.RemovePairedItem("c2_2", 20, t2);
        Assert.AreEqual(1, barThreshSet.GetPairedItems().Count);
        Assert.IsFalse(barThreshSet.ContainsPairedItem("c2_2", 20));
        Assert.AreEqual(1, barThreshSet.GetPairedItems()["c2_2"].Count);

    }

    [Test]
    public void TestIntBoundBubbleTests()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;

        IBubbleComponent<int> minBoundBubble = t1.Components.GetIntegerBubble("min_bound");
        Assert.AreEqual(100, minBoundBubble.GetTopBubble());

        minBoundBubble.AddBubble(150, t1);
        Assert.AreEqual(100, minBoundBubble.GetTopBubble());

        minBoundBubble.AddBubble(50, t1);
        Assert.AreEqual(50, minBoundBubble.GetTopBubble());

        minBoundBubble.AddBubble(150, t1);
        Assert.AreEqual(50, minBoundBubble.GetTopBubble());

        IBubbleComponent<int> maxBoundBubble = t1.Components.GetIntegerBubble("max_bound");
        Assert.AreEqual(0, maxBoundBubble.GetTopBubble());

        maxBoundBubble.AddBubble(-50, t1);
        Assert.AreEqual(0, maxBoundBubble.GetTopBubble());

        maxBoundBubble.AddBubble(50, t1);
        Assert.AreEqual(50, maxBoundBubble.GetTopBubble());

        maxBoundBubble.AddBubble(100, t1);
        Assert.AreEqual(100, maxBoundBubble.GetTopBubble());

        maxBoundBubble.RemoveBubble(50, t1);
        Assert.AreEqual(100, maxBoundBubble.GetTopBubble());

        maxBoundBubble.RemoveBubble(100, t1);
        Assert.AreEqual(0, maxBoundBubble.GetTopBubble());
    }

    [Test]
    public void TestPairedBoundBubbleTests()
    {
        BasicCharacter ch1 = new BasicCharacter("c1", 0, 0);
        ITarget t1 = ch1;

        IPairedBubbleComponent<string, int> pairBubble = t1.Components.GetStringIntegerPairedBubble("targetting_bar_minimum");
        Assert.AreEqual(0, pairBubble.GetPairedBubbles().Count);

        pairBubble.AddPairedBubble("health", 10, t1);
        Assert.AreEqual(1, pairBubble.GetPairedBubbles().Count);
        Assert.IsTrue(pairBubble.ContainsPairedBubble("health", 10));
        Assert.AreEqual(10, pairBubble.GetPairedBubbles()["health"]);

        pairBubble.AddPairedBubble("health", 20, t1);
        Assert.AreEqual(1, pairBubble.GetPairedBubbles().Count);
        Assert.IsTrue(pairBubble.ContainsPairedBubble("health", 20));
        Assert.AreEqual(10, pairBubble.GetPairedBubbles()["health"]);

        pairBubble.AddPairedBubble("health", 5, t1);
        Assert.AreEqual(1, pairBubble.GetPairedBubbles().Count);
        Assert.IsTrue(pairBubble.ContainsPairedBubble("health", 5));
        Assert.AreEqual(5, pairBubble.GetPairedBubbles()["health"]);

        pairBubble.AddPairedBubble("mana", 15, t1);
        Assert.AreEqual(2, pairBubble.GetPairedBubbles().Count);
        Assert.IsTrue(pairBubble.ContainsPairedBubble("mana", 15));
        Assert.AreEqual(15, pairBubble.GetPairedBubbles()["mana"]);

        pairBubble.RemovePairedBubble("health", 10, t1);
        Assert.AreEqual(2, pairBubble.GetPairedBubbles().Count);
        Assert.IsFalse(pairBubble.ContainsPairedBubble("health", 10));
        Assert.AreEqual(5, pairBubble.GetPairedBubbles()["health"]);

        pairBubble.RemovePairedBubble("health", 5, t1);
        Assert.AreEqual(2, pairBubble.GetPairedBubbles().Count);
        Assert.IsFalse(pairBubble.ContainsPairedBubble("health", 5));
        Assert.IsTrue(pairBubble.ContainsPairedBubble("health", 20));
        Assert.AreEqual(20, pairBubble.GetPairedBubbles()["health"]);
    }
}