namespace TurnFlow.Core
{
    public sealed class TurnCounter
    {
        public int CurrentTurn { get; private set; }

        public void Advance()
        {
            CurrentTurn++;
        }
    }
}