using NUnit.Framework;
using TurnFlow.Core;

namespace TurnFlow.Core.Tests;

public class TurnCounterTests
{
    [Test]
    public void Advance_IncrementsTurn()
    {
        // Arrange
        var counter = new TurnCounter();

        // Act
        counter.Advance();

        // Assert
        Assert.That(counter.CurrentTurn, Is.EqualTo(1));
    }
}
