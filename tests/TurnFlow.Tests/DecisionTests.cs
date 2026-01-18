using System.ComponentModel;
using NUnit.Framework;
using TurnFlow;

namespace TurnFlow.DecisionTests;


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
        health_bar.AddScalerMult("defense", 500);
        IBarComponent mana_bar = cm.GetBar("mana");
        mana_bar.AddBaseFlat(20);
        mana_bar.AddScalerMult("attack", 200);

        // -- set component manager --
        Components = cm;
    }

    public IDecision GetQuickDecisions()
    {
        // For simplicity, returning null here.
        // In a real implementation, this would return a decision relevant to the character.
        return null;
    }

    public IDecision GetDecisions()
    {
        // For simplicity, returning null here.
        // In a real implementation, this would return a decision relevant to the character.
        return null;
    }
}



[TestFixture]
public class TurnManagerTests
{
    [Test]
    public void TestStringListDecisionTest()
    {
        IDecisionList<string> d1 = new ListDecision<string>(
            new List<string>
            {
                "option1",
                "option2",
                "option3"
            }
        );

        Assert.IsTrue(d1.GetOptions().Count == 3);
        Assert.IsTrue(d1.GetOptions()[0] == "option1");
        Assert.IsTrue(d1.GetOptions()[1] == "option2");
        Assert.IsTrue(d1.GetOptions()[2] == "option3");

        string empty_chosen;
        bool found_chosen = d1.GetChosen(out empty_chosen);
        Assert.IsFalse(found_chosen);
        Assert.IsTrue(empty_chosen == null);

        bool was_chosen = d1.Choose("option2");
        Assert.IsTrue(d1.HasChosen);
        Assert.IsTrue(was_chosen);

        was_chosen = d1.Choose("option4");
        Assert.IsFalse(was_chosen);

        string chosen;
        found_chosen = d1.GetChosen(out chosen);
        Assert.IsTrue(found_chosen);
        Assert.IsTrue(chosen == "option2");
    }

    [Test]
    public void TestITargetListDecisionTest()
    {
        ITarget target1 = new BasicCharacter("target1");
        ITarget target2 = new BasicCharacter("target2");
        ITarget target3 = new BasicCharacter("target3");

        IDecisionList<ITarget> d1 = new ListDecision<ITarget>(
            new List<ITarget>
            {
                target1,
                target2,
                target3
            }
        );

        Assert.IsTrue(d1.GetOptions().Count == 3);
        Assert.IsTrue(d1.GetOptions()[0] == target1);
        Assert.IsTrue(d1.GetOptions()[1] == target2);
        Assert.IsTrue(d1.GetOptions()[2] == target3);

        ITarget empty_chosen;
        bool found_chosen = d1.GetChosen(out empty_chosen);
        Assert.IsFalse(found_chosen);
        Assert.IsTrue(empty_chosen == null);

        bool was_chosen = d1.Choose(target2);
        Assert.IsTrue(d1.HasChosen);
        Assert.IsTrue(was_chosen);

        was_chosen = d1.Choose(new BasicCharacter("target4"));
        Assert.IsFalse(was_chosen);

        ITarget chosen;
        found_chosen = d1.GetChosen(out chosen);
        Assert.IsTrue(found_chosen);
        Assert.IsTrue(chosen == target2);
    }
}