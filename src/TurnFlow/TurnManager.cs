namespace TurnFlow;


public abstract class TurnManager
{
    private enum TurnStateType
    {
        GameStart,
        StartTurn,
        MidTurn,
        EndTurn,

    }

    private enum IntraTurnStateType
    {
        Enter,
        Resolving,
        Exit,
    }

    protected ITriggerEngine trigger_engine;
    protected IWorld world;

    private ITriggerType on_turn_start_trigger_type;
    private ITriggerType on_turn_end_trigger_type;
    private ITriggerType on_game_start_trigger_type;
    private ITriggerType on_game_end_trigger_type;

    private string turn_time_cost;

    private List<ITarget> participants;

    public ITarget current_turn_target;
    public IDecision current_target_decision;
    private TurnStateType turn_state;
    private IntraTurnStateType intra_turn_state;
    private List<ITarget> waiting_for_quick_turn;

    public TurnManager(
        ITriggerEngine trigger_engine,
        IWorld world,
        ITriggerType on_turn_start_trigger_type,
        ITriggerType on_turn_end_trigger_type,
        ITriggerType on_game_start_trigger_type,
        ITriggerType on_game_end_trigger_type,
        string turn_time_cost = "time"
    )
    {
        this.trigger_engine = trigger_engine;
        this.world = world;
        this.on_turn_start_trigger_type = on_turn_start_trigger_type;
        this.on_turn_end_trigger_type = on_turn_end_trigger_type;
        this.on_game_start_trigger_type = on_game_start_trigger_type;
        this.on_game_end_trigger_type = on_game_end_trigger_type;
        this.turn_time_cost = turn_time_cost;

        participants = new List<ITarget>();
        waiting_for_quick_turn = new List<ITarget>();
    }

    public IDecision Step()
    {
        return (turn_state, intra_turn_state) switch
        {
            (_, IntraTurnStateType.Resolving) => Resolve(),

            (TurnStateType.GameStart, IntraTurnStateType.Enter) => GameStartStepEnter(),
            (TurnStateType.GameStart, IntraTurnStateType.Exit) => GameStartStepExit(),

            (TurnStateType.StartTurn, IntraTurnStateType.Enter) => StartTurnStepEnter(),
            (TurnStateType.StartTurn, IntraTurnStateType.Exit) => StartTurnStepExit(),

            (TurnStateType.MidTurn, IntraTurnStateType.Enter) => MidTurnStepEnter(),
            (TurnStateType.MidTurn, IntraTurnStateType.Exit) => MidTurnStepExit(),

            (TurnStateType.EndTurn, IntraTurnStateType.Enter) => EndTurnStepEnter(),
            (TurnStateType.EndTurn, IntraTurnStateType.Exit) => EndTurnStepExit(),


            _ => throw new System.Exception("Invalid turn state")
        };

    }

    private IDecision GameStartStepEnter()
    {
        Console.WriteLine("Game Start Step Enter");
        
        trigger_engine.Trigger(
            on_game_start_trigger_type,
            null,
            null,
            null,
            new Info()
        );

        intra_turn_state = IntraTurnStateType.Resolving;
        return Step();
    }

    private IDecision GameStartStepExit()
    {
        Console.WriteLine("Game Start Step Exit");

        turn_state = TurnStateType.StartTurn;
        intra_turn_state = IntraTurnStateType.Enter;

        return Step();
    }

    private IDecision StartTurnStepEnter()
    {
        Console.WriteLine("Start Turn Step Enter");

        GetAndSwapCurrentTurnTarget();

        trigger_engine.Trigger(
            on_turn_start_trigger_type,
            null,
            null,
            current_turn_target,
            new Info()
        );

        intra_turn_state = IntraTurnStateType.Resolving;
        return Step();
    }

    private IDecision StartTurnStepExit()
    {
        Console.WriteLine("Start Turn Step Exit");

        turn_state = TurnStateType.MidTurn;
        intra_turn_state = IntraTurnStateType.Enter;
        return Step();
    }

    private IDecision MidTurnStepEnter()
    {
        Console.WriteLine("Mid Turn Step Enter");

        return take_turn();
    }

    private IDecision MidTurnStepExit()
    {
        Console.WriteLine("Mid Turn Step Exit");

        turn_state = TurnStateType.EndTurn;
        intra_turn_state = IntraTurnStateType.Enter;
        return Step();
    }

    private IDecision EndTurnStepEnter()
    {
        Console.WriteLine("End Turn Step Enter");

        trigger_engine.Trigger(
            on_turn_end_trigger_type,
            null,
            null,
            current_turn_target,
            new Info()
        );

        intra_turn_state = IntraTurnStateType.Resolving;
        return Step();
    }

    private IDecision EndTurnStepExit()
    {
        Console.WriteLine("End Turn Step Exit");

        current_turn_target = null;
        turn_state = TurnStateType.StartTurn;
        intra_turn_state = IntraTurnStateType.Enter;
        return Step();
    }

    private IDecision Resolve()
    {
        Console.WriteLine("Resolving");

        ExecuteCurrentDecision();

        if (waiting_for_quick_turn.Count > 0)
        {
            ITarget next_quick = waiting_for_quick_turn[0];
            waiting_for_quick_turn.RemoveAt(0);
            current_target_decision = next_quick.GetQuickDecisions();
            return current_target_decision;
        }

        StepState step_state = trigger_engine.step();

        if (step_state.StateType == StepStateType.PreEvent)
        {
            foreach (ITarget target in participants)
            {
                waiting_for_quick_turn.Append(target);
            }
            return Step();
        }

        intra_turn_state = IntraTurnStateType.Exit;
        return Step();
    }

    private void ExecuteCurrentDecision()
    {
        if (current_target_decision != null)
        {
            // current_target_decision.Execute();
            current_target_decision = null;
            waiting_for_quick_turn.Clear();
        }
    }

    private void GetAndSwapCurrentTurnTarget()
    {
        if (current_turn_target != null)
        {
            participants.Append(current_turn_target);
            current_turn_target = null;
        }

        GetNextTurn();
    }

    private void GetNextTurn()
    {
        int next_turn_index = GetNextTurnIndex();
        if (next_turn_index >= 0)
        {
            current_turn_target = participants[next_turn_index];
            participants.RemoveAt(next_turn_index);
            return;
        }

        while (next_turn_index < 0)
        {
            TurnDamage();
            next_turn_index = GetNextTurnIndex();
        }

        current_turn_target = participants[next_turn_index];
        participants.RemoveAt(next_turn_index);
    }

    private int GetNextTurnIndex()
    {
        if (participants.Count == 0)
        {
            return -1; // No participants
        }

        for (int i = 0; i < participants.Count; i++)
        {
            (int curr, int tot) = participants[i].Components.GetBar(turn_time_cost).GetBarValues();
            if (curr <= 0)
            {
                return i;
            }
        }

        return -1;
    }

    private void TurnDamage()
    {
        foreach (ITarget target in participants)
        {
            IBarComponent bar = target.Components.GetBar(turn_time_cost);
            _ = bar.DamageBar(1);
        }

        bool evs_empty = false;
        while (!evs_empty)
        {
            StepState step = trigger_engine.step();
            if (step.StateType == StepStateType.Empty)
            {
                evs_empty = true;
            }
        }
    }

    public virtual void ResetTurn(ITarget target)
    {
        target.Components.GetBar(turn_time_cost).ResetToDefault();
    }





    public virtual IDecision take_turn()
    {
        end_turn();
        return Step();
    }

    public void AddParticipant(ITarget target, int position = 0)
    {
        participants.Add(target);
        world.AddParticipant(target, position);
    }

    protected void end_turn()
    {
        intra_turn_state = IntraTurnStateType.Exit;
    }

    
}