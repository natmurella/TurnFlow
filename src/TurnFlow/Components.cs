using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;

namespace TurnFlow;

public enum BarResetDefaultType
{
    Empty,
    Full
}

public interface IComponentManager
{
    public IIntegerComponent GetInteger(string component_name);
    public ICostComponent<int> GetCost(string component_name);
    public IDetailComponent<string> GetString(string component_name);
    public IStatComponent GetStat(string component_name);
    public IBarComponent GetBar(string component_name);
    public IBubbleComponent<string> GetStringBubble(string component_name);
    public IBubbleComponent<int> GetIntegerBubble(string component_name);
    public IPairedBubbleComponent<string, int> GetStringIntegerPairedBubble(string component_name);
    public ISetComponent<string> GetStringSet(string component_name);
    public IPairedSetComponent<string, int> GetStringIntPairedSet(string component_name);

    public void ResetAllBars();
}

public class SharedComponentState
{
    public bool state { get; set; }
    public string name { get; set; }

    // holds the method to invoke when State is true
    public Action on_true { get; }

    public SharedComponentState(string sh_name, bool initialState, Action onTrue)
    {
        name = sh_name;
        state = initialState;
        on_true = onTrue ?? throw new ArgumentNullException(nameof(onTrue));
    }

    public void CheckAndInvoke()
    {
        if (state)
        {
            state = false;
            on_true();
        }
    }
}

public class ComponentManager: IComponentManager
{
    // base dictionaries
    private Dictionary<string, IIntegerComponent> integer_components;
    private Dictionary<string, IStringComponent> string_components;

    // complex dictionaries
    private Dictionary<string, ICostComponent<int>> integer_cost_components;
    private Dictionary<string, IDetailComponent<int>> integer_detail_components;
    private Dictionary<string, IDetailComponent<string>> string_detail_components;
    private Dictionary<string, IStatComponent> stat_components;
    private Dictionary<string, IBarComponent> bar_components;
    private Dictionary<string, IPreCalcComponent> pre_calc_components;
    private Dictionary<string, ICalcComponent> calc_components;
    private Dictionary<string, IBubbleComponent<string>> string_bubble_components;
    private Dictionary<string, IBubbleComponent<int>> integer_bubble_components;
    private Dictionary<string, IPairedBubbleComponent<string, int>> string_integer_pair_bubble_components;
    private Dictionary<string, ISetComponent<string>> string_set_components;
    private Dictionary<string, IPairedSetComponent<string, int>> string_int_paired_set_components;

    // trackers
    private HashSet<string> stats_used;
    private HashSet<string> bars_used;

    // shared component states
    private SharedComponentState precalc_must_recalc;
    private SharedComponentState calc_must_recalc;

    

    public ComponentManager()
    {
        integer_components = new Dictionary<string, IIntegerComponent>();
        string_components = new Dictionary<string, IStringComponent>();

        integer_cost_components = new Dictionary<string, ICostComponent<int>>();
        integer_detail_components = new Dictionary<string, IDetailComponent<int>>();
        string_detail_components = new Dictionary<string, IDetailComponent<string>>();
        stat_components = new Dictionary<string, IStatComponent>();
        bar_components = new Dictionary<string, IBarComponent>();
        pre_calc_components = new Dictionary<string, IPreCalcComponent>();
        calc_components = new Dictionary<string, ICalcComponent>();
        string_bubble_components = new Dictionary<string, IBubbleComponent<string>>();
        integer_bubble_components = new Dictionary<string, IBubbleComponent<int>>();
        string_integer_pair_bubble_components = new Dictionary<string, IPairedBubbleComponent<string, int>>();
        string_set_components = new Dictionary<string, ISetComponent<string>>();
        string_int_paired_set_components = new Dictionary<string, IPairedSetComponent<string, int>>();

        stats_used = new HashSet<string>();
        bars_used = new HashSet<string>();

        precalc_must_recalc = new SharedComponentState("precalc", true, CalculatePreCalcComponents);
        calc_must_recalc = new SharedComponentState("calc", true, CalculateCalcComponents);
    }

    public IIntegerComponent GetInteger(string component_name)
    {
        if (integer_components.TryGetValue(component_name, out IIntegerComponent? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"Integer component '{component_name}' does not exist.");
    }

    public ICostComponent<int> GetCost(string component_name)
    {
        if (integer_cost_components.TryGetValue(component_name, out ICostComponent<int>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"Integer cost component '{component_name}' does not exist.");
    }

    public IDetailComponent<string> GetString(string component_name)
    {
        if (string_detail_components.TryGetValue(component_name, out IDetailComponent<string>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String detail component '{component_name}' does not exist.");
    }

    public IStatComponent GetStat(string component_name)
    {
        if (stat_components.TryGetValue(component_name, out IStatComponent? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"Stat component '{component_name}' does not exist.");
    }

    public IBarComponent GetBar(string component_name)
    {
        if (bar_components.TryGetValue(component_name, out IBarComponent? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"Bar component '{component_name}' does not exist.");
    }

    public IBubbleComponent<string> GetStringBubble(string component_name)
    {
        if (string_bubble_components.TryGetValue(component_name, out IBubbleComponent<string>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String bubble component '{component_name}' does not exist.");
    }

    public IBubbleComponent<int> GetIntegerBubble(string component_name)
    {
        if (integer_bubble_components.TryGetValue(component_name, out IBubbleComponent<int>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"Integer bubble component '{component_name}' does not exist.");
    }

    public IPairedBubbleComponent<string, int> GetStringIntegerPairedBubble(string component_name)
    {
        if (string_integer_pair_bubble_components.TryGetValue(component_name, out IPairedBubbleComponent<string, int>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String-integer paired bubble component '{component_name}' does not exist.");
    }

    public ISetComponent<string> GetStringSet(string component_name)
    {
        if (string_set_components.TryGetValue(component_name, out ISetComponent<string>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String set component '{component_name}' does not exist.");
    }

    public IPairedSetComponent<string, int> GetStringIntPairedSet(string component_name)
    {
        if (string_int_paired_set_components.TryGetValue(component_name, out IPairedSetComponent<string, int>? component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String-int paired set component '{component_name}' does not exist.");
    }

    public void ResetAllBars()
    {
        foreach (var bar in bar_components.Values)
        {
            bar.ResetToDefault();
        }
    }


    public void PrintComponents()
    {
        Console.WriteLine("Integer Components:");
        foreach (var kvp in integer_components)
        {
            IntegerComponent kvp_int = (IntegerComponent)kvp.Value;
            string mod_states = "";
            foreach (var state in kvp_int.StatesModifiedOnChange)
            {
                mod_states += $"{state.name},";
            }
            string get_states = "";
            foreach (var state in kvp_int.StatesInvokedOnGet)
            {
                get_states += $"{state.name},";
            }
            Console.WriteLine($"  {kvp.Key}: mod: [{mod_states}], get: [{get_states}]");
        }
    }

    private void CalculatePreCalcComponents()
    {
        foreach (IPreCalcComponent pre_calc_component in pre_calc_components.Values)
        {
            pre_calc_component.Calculate();
        }
    }

    private void CalculateCalcComponents()
    {
        foreach (ICalcComponent calc_component in calc_components.Values)
        {
            calc_component.Calculate();
        }
    }

    private IBubbleComponent<string> GetStringBubbleComponent(
        string component_name
    )
    {
        IBubbleComponent<string>? component = null;
        if (string_bubble_components.TryGetValue(component_name, out component) && component != null)
        {
            return component;
        }
        throw new System.Exception($"String bubble component '{component_name}' does not exist.");
    }

    
    

    public void CreateString(string component_name, string default_value = "unknown")
    {
        if (string_components.ContainsKey(component_name))
        {
            throw new System.Exception($"String component '{component_name}' already exists.");
        }

        StringComponent string_component = new StringComponent(default_value);
        IDetailComponent<string> detail_component = string_component;

        string_components.Add(component_name, string_component);
        string_detail_components.Add(component_name, detail_component);
    }

    public void CreateStat(
        string stat_name
    )
    {

        if (stats_used.Contains(stat_name))
        {
            throw new System.Exception($"Stat component '{stat_name}' already exists.");
        }

        string stat_prefix = "stat_";
        string stat_calced_value_name = stat_prefix + stat_name;
        string stat_add_flat_name = stat_prefix + stat_name + "_add_flat";
        string stat_add_mult_name = stat_prefix + stat_name + "_add_mult";
        string stat_sub_flat_name = stat_prefix + stat_name + "_sub_flat";
        string stat_sub_mult_name = stat_prefix + stat_name + "_sub_mult";

        IntegerComponent stat_calced_value = new IntegerComponent(0);
        IntegerComponent stat_add_flat = new IntegerComponent(0);
        IntegerComponent stat_add_mult = new IntegerComponent(0);
        IntegerComponent stat_sub_flat = new IntegerComponent(0);
        IntegerComponent stat_sub_mult = new IntegerComponent(0);

        stat_calced_value.AddStateInvokedOnGet(precalc_must_recalc);
        stat_add_flat.AddStateModifiedOnChange(precalc_must_recalc);
        stat_add_mult.AddStateModifiedOnChange(precalc_must_recalc);
        stat_sub_flat.AddStateModifiedOnChange(precalc_must_recalc);
        stat_sub_mult.AddStateModifiedOnChange(precalc_must_recalc);

        StatComponent stat_component = new StatComponent(
            stat_calced_value,
            stat_add_flat,
            stat_add_mult,
            stat_sub_flat,
            stat_sub_mult
        );

        integer_components.Add(stat_calced_value_name, stat_calced_value);
        integer_components.Add(stat_add_flat_name, stat_add_flat);
        integer_components.Add(stat_add_mult_name, stat_add_mult);
        integer_components.Add(stat_sub_flat_name, stat_sub_flat);
        integer_components.Add(stat_sub_mult_name, stat_sub_mult);

        stat_components.Add(stat_name, stat_component);

        pre_calc_components.Add(stat_name, stat_component);

        stats_used.Add(stat_name);
    }

    public void CreateBar(
        string bar_name,
        BarResetDefaultType reset_type = BarResetDefaultType.Full
    )
    {

        if (bars_used.Contains(bar_name))
        {
            throw new System.Exception($"Bar component '{bar_name}' already exists.");
        }

        string bar_prefix = "bar_";
        string bar_current_name = bar_prefix + bar_name + "_current";
        string bar_total_name = bar_prefix + bar_name + "_total";
        string bar_total_base_name = bar_prefix + bar_name + "_total_base";

        IntegerComponent bar_current = new IntegerComponent(0);
        IntegerComponent bar_total = new IntegerComponent(0);
        IntegerComponent bar_total_base = new IntegerComponent(0);

        bar_total.AddStateInvokedOnGet(calc_must_recalc);
        bar_total_base.AddStateModifiedOnChange(calc_must_recalc);

        BarComponent bar_component = new BarComponent(
            bar_current,
            bar_total,
            bar_total_base,
            reset_type
        );

        integer_components.Add(bar_current_name, bar_current);
        integer_components.Add(bar_total_name, bar_total);
        integer_components.Add(bar_total_base_name, bar_total_base);

        bar_components.Add(bar_name, bar_component);

        integer_cost_components.Add(bar_name, bar_component);

        calc_components.Add(bar_name, bar_component);

        bars_used.Add(bar_name);
    }

    // public void CreateStringBubbleComponent(
    //     string component_name,
    //     string default_value,
    //     bool smart = false
    // )
    // {
    //     if (string_bubble_components.ContainsKey(component_name))
    //     {
    //         if (smart)
    //         {
    //             return;
    //         }
    //         throw new System.Exception($"String bubble component '{component_name}' already exists.");
    //     }

    //     StringBubbleComponent bubble_component = new StringBubbleComponent(default_value);
    //     string_bubble_components[component_name] = bubble_component;
    // }
    
    public void CreateBubble<T>(
        string component_name,
        T default_value
    )
    {
        if (string_bubble_components.ContainsKey(component_name))
        {
            throw new System.Exception($"Bubble component '{component_name}' already exists.");
        }

        if (typeof(T) == typeof(string))
        {
            string default_value_str = default_value as string ?? "unknown";
            IBubbleComponent<string> bubble_component = new StringBubbleComponent(default_value_str);
            string_bubble_components.Add(component_name, bubble_component);
            IDetailComponent<string> detail_component = (IDetailComponent<string>)bubble_component;
            string_detail_components.Add(component_name, detail_component);
        }
        else
        {
            throw new System.Exception($"Bubble component type '{typeof(T)}' is not supported.");
        }
    }

    public void CreateBoundBubble<T>(
        string component_name,
        T default_value,
        BoundType bound_type = BoundType.Minimum
    )
    {
        if (integer_bubble_components.ContainsKey(component_name))
        {
            throw new System.Exception($"Bound bubble component '{component_name}' already exists.");
        }

        if (typeof(T) == typeof(int))
        {
            int default_value_int = Convert.ToInt32(default_value);
            IBubbleComponent<int> bubble_component = new IntegerBoundBubbleComponent(default_value_int, bound_type);
            integer_bubble_components.Add(component_name, bubble_component);
            IDetailComponent<int> detail_component = (IDetailComponent<int>)bubble_component;
            integer_detail_components.Add(component_name, detail_component);
        }
        else
        {
            throw new System.Exception($"Bound bubble component type '{typeof(T)}' is not supported.");
        }
    }

    public void CreateBoundPairedBubble<T, U>(
        string component_name,
        BoundType bound_type
    )
    {
        if (string_integer_pair_bubble_components.ContainsKey(component_name))
        {
            throw new System.Exception($"Bound paired bubble component '{component_name}' already exists.");
        }

        if (typeof(T) == typeof(string) && typeof(U) == typeof(int))
        {
            IPairedBubbleComponent<string, int> bubble_component = new StringIntegerBoundPairedComponent(bound_type);
            string_integer_pair_bubble_components.Add(component_name, bubble_component);
        }
        else
        {
            throw new System.Exception($"Bound paired bubble component types '{typeof(T)}' and '{typeof(U)}' are not supported.");
        }
    }

    public void CreateSet<T>(
        string component_name
    )
    {


        if (typeof(T) == typeof(string))
        {
            if (string_set_components.ContainsKey(component_name))
            {
                throw new System.Exception($"Set component '{component_name}' already exists.");
            }

            ISetComponent<string> set_component = new StringSetComponent();
            string_set_components.Add(component_name, set_component);
        }
        else
        {
            throw new System.Exception($"Set component type '{typeof(T)}' is not supported.");
        }
    }

    public void CreatePairedSet<T, U>(
        string component_name
    )
    {
        if (typeof(T) == typeof(string) && typeof(U) == typeof(int))
        {
            if (string_int_paired_set_components.ContainsKey(component_name))
            {
                throw new System.Exception($"String-int paired set component '{component_name}' already exists.");
            }

            IPairedSetComponent<string, int> paired_set_component = new StringIntPairedSetComponent();
            string_int_paired_set_components.Add(component_name, paired_set_component);
        }
        else
        {
            throw new System.Exception($"Paired set component types '{typeof(T)}' and '{typeof(U)}' are not supported.");
        }
    }

    public void AddStatScalersForBarTotal(
        string bar_name,
        List<string> stat_names
    )
    {
        if (!bars_used.Contains(bar_name))
        {
            throw new System.Exception($"Bar component '{bar_name}' does not exist.");
        }

        string bar_prefix = "bar_";
        string stat_prefix = "stat_";

        IBarComponent bar_component = GetBar(bar_name);
        BarComponent bar = (BarComponent)bar_component;

        foreach (string stat_name in stat_names)
        {


            string stat_integer_name;
            if (stats_used.Contains(stat_name))
            {
                stat_integer_name = stat_prefix + stat_name;
            }
            else
            {
                stat_integer_name = stat_name;
            }
            string bar_total_stat_mult_name = bar_prefix + bar_name + "_total_" + stat_name + "_mult";

            IIntegerComponent stat_component = GetInteger(stat_integer_name);

            IntegerComponent stat_integer = (IntegerComponent)stat_component;
            IntegerComponent bar_total_stat_mult = new IntegerComponent(0);

            stat_integer.AddStateModifiedOnChange(calc_must_recalc);
            bar_total_stat_mult.AddStateModifiedOnChange(calc_must_recalc);

            integer_components.Add(bar_total_stat_mult_name, bar_total_stat_mult);

            bar.AddScaler(stat_name, stat_integer, bar_total_stat_mult);
        }
    }
}

public interface IIntegerComponent
{
    public int GetValue();
    public bool AddToValue(int value);
    public bool CanSubtractFromValue(int value);
    public bool SubFromValue(int value);
}

public interface IStringComponent
{
    public string GetValue();
    public void SetValue(string value);
}


public interface ICostComponent<T>
{
    public bool CanPayCost(
        T cost_value
    );

    public void PayCost(
        T cost_value
    );
}

public interface IDetailComponent<T>
{
    public void SetDetail(T value);
    public T GetDetail();
}

public interface IStatComponent
{
    // add change
    public void AddBuffFlat(int value);
    public void AddBuffMult(int value);
    public void AddDebuffFlat(int value);
    public void AddDebuffMult(int value);
    // remove change
    public void RemoveBuffFlat(int value);
    public void RemoveBuffMult(int value);
    public void RemoveDebuffFlat(int value);
    public void RemoveDebuffMult(int value);
    // get stat values
    public (int flat_value, int mult_value) GetStatParts();
    public int GetValue();
    // state change
    public void AddStateModifiedOnChange(SharedComponentState state);
}

public interface IBarComponent
{
    public (int bar_damage, int bar_overkill) DamageBar(int damage);
    public (int bar_heal, int bar_overheal) HealBar(int heal);
    public (int current, int total) GetBarValues();
    public void AddBaseFlat(int value);
    public void RemoveBaseFlat(int value);
    public void AddScalerMult(string stat_name, int mult_value);
    public void RemoveScalerMult(string stat_name, int mult_value);
    public void ResetToDefault();

}

public interface IPreCalcComponent
{
    public void Calculate();
    public int GetValue();
    public void AddStateModifiedOnChange(SharedComponentState state);
}

public interface ICalcComponent
{
    public void Calculate();
}

public interface IBubbleComponent<T>
{
    public void AddBubble(T value, object source, bool is_base = false);
    public bool RemoveBubble(T value, object source);
    public T GetTopBubble();
}

public interface IPairedBubbleComponent<T, U>
{
    public void AddPairedBubble(T value, U paired_value, object source);
    public bool RemovePairedBubble(T value, U paired_value, object source);
    public ReadOnlyDictionary<T, U> GetPairedBubbles();
    public bool ContainsPairedBubble(T value, U paired_value);
}

public interface ISetComponent<T>
{
    public void AddItemSource(T value, object source);
    public bool RemoveItemSource(T value, object source);
    public IReadOnlyCollection<T> GetItems();
    public bool ContainsItem(T value);
}

public interface IPairedSetComponent<T, U>
{
    public void AddPairedItem(T value, U paired_value, object source);
    public bool RemovePairedItem(T value, U paired_value, object source);
    public ReadOnlyDictionary<T, IReadOnlySet<U>> GetPairedItems();
    public bool ContainsPairedItem(T value, U paired_value);
}


public class IntegerComponent : ICostComponent<int>, IDetailComponent<int>, IIntegerComponent
{
    private int Value;
    public HashSet<SharedComponentState> StatesModifiedOnChange;
    public HashSet<SharedComponentState> StatesInvokedOnGet;

    public IntegerComponent(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Value cannot be negative");
        }

        Value = value;
        this.StatesModifiedOnChange = new HashSet<SharedComponentState>();
        this.StatesInvokedOnGet = new HashSet<SharedComponentState>();
    }

    public void AddStateModifiedOnChange(SharedComponentState state)
    {
        StatesModifiedOnChange.Add(state);
    }

    public void AddStateInvokedOnGet(SharedComponentState state)
    {
        StatesInvokedOnGet.Add(state);
    }

    public int GetValue()
    {
        foreach (var state in StatesInvokedOnGet)
        {
            state.CheckAndInvoke();
        }
        return Value;
    }

    public bool AddToValue(int value)
    {
        if (value < 0)
        {
            return false;
        }

        Value += value;

        foreach (var state in StatesModifiedOnChange)
        {
            state.state = true;
        }

        return true;
    }

    public bool CanSubtractFromValue(int value)
    {
        if (value < 0 || Value < value)
        {
            return false;
        }

        return true;
    }

    public bool SubFromValue(int value)
    {
        if (!CanSubtractFromValue(value))
        {
            return false;
        }

        Value -= value;

        foreach (var state in StatesModifiedOnChange)
        {
            state.state = true;
        }

        return true;
    }

    // cost interface methods

    public bool CanPayCost(int cost_value)
    {
        return CanSubtractFromValue(cost_value);
    }

    public void PayCost(int cost_value)
    {
        if (!CanPayCost(cost_value))
        {
            throw new System.Exception("Cannot pay cost");
        }

        SubFromValue(cost_value);
    }

    // detail interface methods
    public void SetDetail(int value)
    {
        Value = value;

        foreach (var state in StatesModifiedOnChange)
        {
            state.state = true;
        }
    }

    public int GetDetail()
    {
        foreach (var state in StatesInvokedOnGet)
        {
            state.CheckAndInvoke();
        }
        return Value;
    }
}

public class StringComponent : IDetailComponent<string>, IStringComponent
{

    private string Value { get; set; }
    public HashSet<SharedComponentState> StatesModifiedOnChange;
    public HashSet<SharedComponentState> StatesInvokedOnGet;

    public StringComponent(string value)
    {
        Value = value;
        this.StatesModifiedOnChange = new HashSet<SharedComponentState>();
        this.StatesInvokedOnGet = new HashSet<SharedComponentState>();
    }

    public void AddStateModifiedOnChange(SharedComponentState state)
    {
        StatesModifiedOnChange.Add(state);
    }

    public void AddStateInvokedOnGet(SharedComponentState state)
    {
        StatesInvokedOnGet.Add(state);
    }

    public string GetValue()
    {
        foreach (var state in StatesInvokedOnGet)
        {
            state.CheckAndInvoke();
        }
        return Value;
    }

    public void SetValue(string value)
    {
        Value = value;

        foreach (var state in StatesModifiedOnChange)
        {
            state.state = true;
        }
    }

    public void SetDetail(string value)
    {
        SetValue(value);
    }

    public string GetDetail()
    {
        return GetValue();
    }
}


public class StatComponent : IStatComponent, IPreCalcComponent
{
    IntegerComponent stat_calced_value;
    IntegerComponent stat_add_flat;
    IntegerComponent stat_add_mult;
    IntegerComponent stat_sub_flat;
    IntegerComponent stat_sub_mult;

    public StatComponent(
        IntegerComponent stat_calced_value,
        IntegerComponent stat_add_flat,
        IntegerComponent stat_add_mult,
        IntegerComponent stat_sub_flat,
        IntegerComponent stat_sub_mult
    )
    {
        this.stat_calced_value = stat_calced_value;
        this.stat_add_flat = stat_add_flat;
        this.stat_add_mult = stat_add_mult;
        this.stat_sub_flat = stat_sub_flat;
        this.stat_sub_mult = stat_sub_mult;
    }

    public void AddBuffFlat(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Buff value cannot be negative");
        }

        stat_add_flat.AddToValue(value);
    }

    public void RemoveBuffFlat(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Buff value cannot be negative");
        }

        stat_add_flat.SubFromValue(value);
    }

    public void AddBuffMult(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Buff value cannot be negative");
        }

        stat_add_mult.AddToValue(value);
    }

    public void RemoveBuffMult(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Buff value cannot be negative");
        }

        stat_add_mult.SubFromValue(value);
    }

    public void AddDebuffFlat(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Debuff value cannot be negative");
        }

        stat_sub_flat.AddToValue(value);
    }

    public void RemoveDebuffFlat(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Debuff value cannot be negative");
        }

        stat_sub_flat.SubFromValue(value);
    }

    public void AddDebuffMult(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Debuff value cannot be negative");
        }

        stat_sub_mult.AddToValue(value);
    }

    public void RemoveDebuffMult(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Debuff value cannot be negative");
        }

        stat_sub_mult.SubFromValue(value);
    }

    public (int flat_value, int mult_value) GetStatParts()
    {
        int flat_value = stat_add_flat.GetValue() - stat_sub_flat.GetValue();

        int mult_value = stat_add_mult.GetValue() - stat_sub_mult.GetValue();

        return (flat_value, mult_value);
    }

    private int GetStatValue()
    {
        var (flat_value, mult_value) = GetStatParts();

        if (flat_value <= 0)
        {
            return 0;
        }

        int mult_val = 100 + mult_value;

        if (mult_val <= 0)
        {
            return 0;
        }

        float temp_value = flat_value * mult_val / 100f;
        int final_value = (int)temp_value;

        return final_value;
    }

    public void AddStateModifiedOnChange(SharedComponentState state)
    {
        stat_add_flat.AddStateModifiedOnChange(state);
        stat_add_mult.AddStateModifiedOnChange(state);
        stat_sub_flat.AddStateModifiedOnChange(state);
        stat_sub_mult.AddStateModifiedOnChange(state);
    }

    public void Calculate()
    {
        int final_value = GetStatValue();
        stat_calced_value.SetDetail(final_value);
    }

    public int GetValue()
    {
        return stat_calced_value.GetValue();
    }
}


public class BarComponent : IBarComponent, ICostComponent<int>, ICalcComponent
{
    IntegerComponent bar_current;
    IntegerComponent bar_total;
    IntegerComponent bar_total_base;
    BarResetDefaultType reset_type;
    Dictionary<string, (IntegerComponent, IntegerComponent)> bar_scalers;

    public BarComponent(
        IntegerComponent bar_current,
        IntegerComponent bar_total,
        IntegerComponent bar_total_base,
        BarResetDefaultType reset_type = BarResetDefaultType.Full
    )
    {
        this.bar_current = bar_current;
        this.bar_total = bar_total;
        this.bar_total_base = bar_total_base;
        this.bar_scalers = new Dictionary<string, (IntegerComponent, IntegerComponent)>();
        this.reset_type = reset_type;
    }

    public void AddScaler(string stat_name, IntegerComponent target_resource, IntegerComponent resource_mult)
    {
        bar_scalers.Add(stat_name, (target_resource, resource_mult));
    }

    public (int bar_damage, int bar_overkill) DamageBar(int damage)
    {
        if (damage < 0)
        {
            throw new System.Exception("Damage cannot be negative");
        }

        int overkill = 0;
        int bar_damage = 0;

        if (bar_current.GetValue() >= damage)
        {
            bar_current.SubFromValue(damage);
            bar_damage = damage;
        }
        else
        {
            overkill = damage - bar_current.GetValue();
            bar_damage = bar_current.GetValue();
            bar_current.SetDetail(0);
        }

        return (bar_damage, overkill);
    }

    public (int bar_heal, int bar_overheal) HealBar(int heal)
    {
        if (heal < 0)
        {
            throw new System.Exception("Heal cannot be negative");
        }

        int overheal = 0;
        int bar_heal = 0;

        if (bar_current.GetValue() + heal <= bar_total.GetValue())
        {
            bar_current.AddToValue(heal);
            bar_heal = heal;
        }
        else
        {
            overheal = bar_current.GetValue() + heal - bar_total.GetValue();
            bar_heal = bar_total.GetValue() - bar_current.GetValue();
            bar_current.SetDetail(bar_total.GetValue());
        }

        return (bar_heal, overheal);
    }

    public void AddBaseFlat(int value)
    {
        if (value < 0)
        {
            throw new System.Exception("Base flat value cannot be negative");
        }

        bar_total_base.AddToValue(value);
    }

    public void RemoveBaseFlat(int value)
    {
        if (value < 0 || !bar_total_base.CanSubtractFromValue(value))
        {
            throw new System.Exception("Base flat value cannot be negative");
        }

        bar_total_base.SubFromValue(value);
    }

    public void AddScalerMult(string stat_name, int mult_value)
    {
        if (mult_value < 0)
        {
            throw new System.Exception("Scaler multiplier cannot be negative");
        }

        if (!bar_scalers.ContainsKey(stat_name))
        {
            throw new System.Exception($"Scaler for stat '{stat_name}' does not exist.");
        }

        (IntegerComponent _, IntegerComponent resource_mult) = bar_scalers[stat_name];
        resource_mult.AddToValue(mult_value);
    }

    public void RemoveScalerMult(string stat_name, int mult_value)
    {
        if (mult_value < 0 || !bar_scalers.ContainsKey(stat_name))
        {
            throw new System.Exception("Scaler multiplier cannot be negative or does not exist");
        }

        (IntegerComponent _, IntegerComponent resource_mult) = bar_scalers[stat_name];
        if (!resource_mult.CanSubtractFromValue(mult_value))
        {
            throw new System.Exception("Cannot remove scaler multiplier, not enough value");
        }
        resource_mult.SubFromValue(mult_value);
    }

    public (int current, int total) GetBarValues()
    {
        return (bar_current.GetValue(), bar_total.GetValue());
    }

    public bool CanPayCost(int cost_value)
    {
        return bar_current.CanSubtractFromValue(cost_value);
    }

    public void PayCost(int cost_value)
    {
        if (!CanPayCost(cost_value))
        {
            throw new System.Exception("Cannot pay cost");
        }

        bar_current.SubFromValue(cost_value);
    }

    public void Calculate()
    {
        int total_value = bar_total_base.GetValue();

        foreach ((IntegerComponent target_resource, IntegerComponent resource_mult) in bar_scalers.Values)
        {
            int precalc_value = target_resource.GetValue();
            int mult_value = resource_mult.GetValue();
            float temp_value = precalc_value * mult_value / 100f;
            int final_value = (int)temp_value;
            total_value += final_value;
        }

        bar_total.SetDetail(total_value);
    }

    public void ResetToDefault()
    {
        if (reset_type == BarResetDefaultType.Empty)
        {
            bar_current.SetDetail(0);
        }
        else if (reset_type == BarResetDefaultType.Full)
        {
            bar_current.SetDetail(bar_total.GetValue());
        }
        else
        {
            throw new System.Exception("Invalid reset type");
        }
    }
}

public class BarTotalComponent : ICalcComponent
{

    IntegerComponent bar_total;
    IntegerComponent bar_total_base;
    List<(IPreCalcComponent, IntegerComponent)> bar_scale_precal_values;

    public BarTotalComponent(
        IntegerComponent bar_total,
        IntegerComponent bar_total_base,
        List<(IPreCalcComponent, IntegerComponent)> bar_scale_precal_values
    )
    {
        this.bar_total = bar_total;
        this.bar_total_base = bar_total_base;
        this.bar_scale_precal_values = bar_scale_precal_values;
    }

    public void Calculate()
    {
        int total_value = bar_total_base.GetValue();

        foreach ((IPreCalcComponent pre_calc_component, IntegerComponent mult) in bar_scale_precal_values)
        {
            int precalc_value = pre_calc_component.GetValue();
            int mult_value = mult.GetValue();
            float temp_value = precalc_value * mult_value / 100f;
            int final_value = (int)temp_value;
            total_value += final_value;
        }

        bar_total.SetDetail(total_value);
    }
}



public class StringBubbleComponent : IBubbleComponent<string>, IDetailComponent<string>
{
    private class ComponentItem
    {
        public string value;
        public object source;
        public long recency;
    }

    private List<ComponentItem> bubbles;
    private long item_counter;
    private string default_value;
    private HashSet<SharedComponentState> StatesModifiedOnChange;
    private HashSet<SharedComponentState> StatesInvokedOnGet;

    public StringBubbleComponent(string default_value)
    {
        bubbles = new List<ComponentItem>();
        item_counter = 0;
        this.default_value = default_value;

        this.StatesModifiedOnChange = new HashSet<SharedComponentState>();
        this.StatesInvokedOnGet = new HashSet<SharedComponentState>();
    }

    public void AddStateModifiedOnChange(SharedComponentState state)
    {
        StatesModifiedOnChange.Add(state);
    }

    public void AddStateInvokedOnGet(SharedComponentState state)
    {
        StatesInvokedOnGet.Add(state);
    }

    private void SortBubbles()
    {
        string prev_top = GetMostRecentBubble();
        this.bubbles = this.bubbles.OrderByDescending(x => x.recency).ToList();
        // bubbles.Sort((a, b) => b.recency.CompareTo(a.recency));
        if (prev_top != GetMostRecentBubble())
        {
            foreach (var state in StatesModifiedOnChange)
            {
                state.state = true;
            }
        }
    }

    public void AddBubble(string value, object source, bool is_base = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new System.Exception("Bubble value cannot be null or empty");
        }

        item_counter++;
        long recency_value = is_base ? -item_counter : item_counter;
        bubbles.Add(new ComponentItem
        {
            value = value,
            source = source,
            recency = recency_value
        });
        SortBubbles();
    }

    public bool RemoveBubble(string value, object source)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new System.Exception("Bubble value cannot be null or empty");
        }

        var item = bubbles.FirstOrDefault(x => x.value.Equals(value) && x.source.Equals(source));
        if (item != null)
        {
            bubbles.Remove(item);
            SortBubbles();
            return true;
        }

        return false;
    }

    private string GetMostRecentBubble()
    {
        if (bubbles.Count == 0)
        {
            return default_value;
        }

        return bubbles[0].value;
    }

    public string GetTopBubble()
    {
        foreach (var state in StatesInvokedOnGet)
        {
            state.CheckAndInvoke();
        }
        return GetMostRecentBubble();
    }

    public void SetDetail(string value)
    {
        throw new System.Exception("Cannot set detail on a bubble component");
    }

    public string GetDetail()
    {
        return GetTopBubble();
    }
}

public enum BoundType
{
    Maximum,
    Minimum
}

public class IntegerBoundBubbleComponent : IBubbleComponent<int>, IDetailComponent<int>
{

    private class Counter
    {
        public int count;
        public Counter()
        {
            count = 0;
        }
    }

    private SortedDictionary<int, Dictionary<object, Counter>> sorted_dict;
    private BoundType bound_type;
    private int default_value;

    public IntegerBoundBubbleComponent(int default_value, BoundType bound_type)
    {
        this.sorted_dict = new SortedDictionary<int, Dictionary<object, Counter>>();
        this.bound_type = bound_type;
        this.default_value = default_value;
    }

    public void AddBubble(int value, object source, bool is_base = false)
    {
        Dictionary<object, Counter> sources = sorted_dict.GetValueOrDefault(value) ?? (sorted_dict[value] = new Dictionary<object, Counter>());
        Counter counter = sources.GetValueOrDefault(source) ?? (sources[source] = new Counter());
        counter.count++;
    }

    public bool RemoveBubble(int value, object source)
    {
        if (sorted_dict.TryGetValue(value, out Dictionary<object, Counter> sources) && sources.TryGetValue(source, out Counter counter))
        {
            counter.count--;
            if (counter.count < 1)
            {
                sources.Remove(source);
            }
            if (sources.Count == 0)
            {
                sorted_dict.Remove(value);
            }
            return true;
        }
        return false;
    }

    public int GetTopBubble()
    {
        if (sorted_dict.Count == 0)
        {
            return default_value;
        }

        if (bound_type == BoundType.Maximum)
        {
            int val = sorted_dict.Last().Key;
            return val > this.default_value ? val : this.default_value;
        }
        else if (bound_type == BoundType.Minimum)
        {
            int val = sorted_dict.First().Key;
            return val < this.default_value ? val : this.default_value;
        }

        return default_value;
    }

    public void SetDetail(int value)
    {
        throw new System.Exception("Cannot set detail on a bound bubble component");
    }

    public int GetDetail()
    {
        return GetTopBubble();
    }

}

public class StringIntegerBoundPairedComponent : IPairedBubbleComponent<string, int>
{
    private class Counter
    {
        public int count;
        public Counter()
        {
            count = 0;
        }
    }
    private Dictionary<string, SortedDictionary<int, Dictionary<object, Counter>>> sources_dict;
    private BoundType bound_type;
    private Dictionary<string, int> bound_values;
    private ReadOnlyDictionary<string, int> bound_values_readonly;

    public StringIntegerBoundPairedComponent(BoundType bound_type)
    {
        this.sources_dict = new Dictionary<string, SortedDictionary<int, Dictionary<object, Counter>>>();
        this.bound_type = bound_type;
        this.bound_values = new Dictionary<string, int>();
        this.bound_values_readonly = new ReadOnlyDictionary<string, int>(bound_values);
    }

    public void AddPairedBubble(string value, int paired_value, object source)
    {
        SortedDictionary<int, Dictionary<object, Counter>> paired_sources = sources_dict.GetValueOrDefault(value) ?? (sources_dict[value] = new SortedDictionary<int, Dictionary<object, Counter>>());
        Dictionary<object, Counter> paired_values = paired_sources.GetValueOrDefault(paired_value) ?? (paired_sources[paired_value] = new Dictionary<object, Counter>());
        Counter counter = paired_values.GetValueOrDefault(source) ?? (paired_values[source] = new Counter());
        counter.count++;

        UpdateTargetBoundValue(value);
    }

    public bool RemovePairedBubble(string value, int paired_value, object source)
    {
        if (sources_dict.TryGetValue(value, out SortedDictionary<int, Dictionary<object, Counter>> paired_sources) &&
            paired_sources.TryGetValue(paired_value, out Dictionary<object, Counter> paired_values) &&
            paired_values.TryGetValue(source, out Counter counter))
        {
            counter.count--;
            if (counter.count < 1)
            {
                paired_values.Remove(source);
            }
            if (paired_values.Count == 0)
            {
                paired_sources.Remove(paired_value);
            }
            if (paired_sources.Count == 0)
            {
                sources_dict.Remove(value);
                bound_values.Remove(value);
            }
            else
            {
                UpdateTargetBoundValue(value);
            }
            return true;
        }
        return false;
    }

    public ReadOnlyDictionary<string, int> GetPairedBubbles()
    {
        return bound_values_readonly;
    }

    public bool ContainsPairedBubble(string value, int paired_value)
    {
        if (sources_dict.TryGetValue(value, out SortedDictionary<int, Dictionary<object, Counter>> paired_sources))
        {
            return paired_sources.ContainsKey(paired_value);
        }
        return false;
    }

    private void UpdateTargetBoundValue(string value)
    {
        SortedDictionary<int, Dictionary<object, Counter>> target_sorted_dict = sources_dict[value];
        int val;
        if (this.bound_type == BoundType.Maximum)
        {
            val = target_sorted_dict.Last().Key;
        }
        else
        {
            val = target_sorted_dict.First().Key;
        }

        bound_values[value] = val;
    }


}


public class StringSetComponent : ISetComponent<string>
{
    private class Counter
    {
        public int count;
        public Counter()
        {
            count = 0;
        }
    }
    private HashSet<string> items;
    private Dictionary<string, Dictionary<object, Counter>> item_sources;

    public StringSetComponent()
    {
        items = new HashSet<string>();
        item_sources = new Dictionary<string, Dictionary<object, Counter>>();
    }

    public void AddItemSource(string value, object source)
    {
        Dictionary<object, Counter> sources = item_sources.GetValueOrDefault(value) ?? (item_sources[value] = new Dictionary<object, Counter>());
        Counter counter = sources.GetValueOrDefault(source) ?? (sources[source] = new Counter());
        counter.count++;

        items.Add(value);
    }

    public bool RemoveItemSource(string value, object source)
    {
        if (item_sources.TryGetValue(value, out Dictionary<object, Counter> sources) && sources.TryGetValue(source, out Counter counter))
        {
            counter.count--;
            if (counter.count < 1)
            {
                sources.Remove(source);
            }
            if (sources.Count == 0)
            {
                item_sources.Remove(value);
                items.Remove(value);
            }

            return true;
        }
        return false;
    }

    public IReadOnlyCollection<string> GetItems()
    {
        return items;
    }

    public bool ContainsItem(string value)
    {
        return items.Contains(value);
    }
}

public class StringIntPairedSetComponent : IPairedSetComponent<string, int>
{
    private class Counter
    {
        public int count;
        public Counter()
        {
            count = 0;
        }
    }
    private Dictionary<string, Dictionary<int, Dictionary<object, Counter>>> paired_items;
    private Dictionary<string, HashSet<int>> paired_items_sets;
    private Dictionary<string, IReadOnlySet<int>> paired_items_readonly_sets;
    private ReadOnlyDictionary<string, IReadOnlySet<int>> paired_items_readonly_dict;

    public StringIntPairedSetComponent()
    {
        paired_items = new Dictionary<string, Dictionary<int, Dictionary<object, Counter>>>();
        paired_items_sets = new Dictionary<string, HashSet<int>>();
        paired_items_readonly_sets = new Dictionary<string, IReadOnlySet<int>>();
        paired_items_readonly_dict = new ReadOnlyDictionary<string, IReadOnlySet<int>>(paired_items_readonly_sets);
    }

    public void AddPairedItem(string value, int paired_value, object source)
    {
        Dictionary<int, Dictionary<object, Counter>> sources = paired_items.GetValueOrDefault(value) ?? (paired_items[value] = new Dictionary<int, Dictionary<object, Counter>>());
        Dictionary<object, Counter> paired_values = sources.GetValueOrDefault(paired_value) ?? (sources[paired_value] = new Dictionary<object, Counter>());
        Counter counter = paired_values.GetValueOrDefault(source) ?? (paired_values[source] = new Counter());
        counter.count++;

        HashSet<int> paired_set = paired_items_sets.GetValueOrDefault(value);
        if (paired_set == null)
        {
            paired_set = new HashSet<int>();
            paired_items_sets[value] = paired_set;
            paired_items_readonly_sets[value] = paired_set;
        }
        paired_set.Add(paired_value);
    }

    public bool RemovePairedItem(string value, int paired_value, object source)
    {
        if (paired_items.TryGetValue(value, out Dictionary<int, Dictionary<object, Counter>> sources) &&
            sources.TryGetValue(paired_value, out Dictionary<object, Counter> paired_values) &&
            paired_values.TryGetValue(source, out Counter counter))
        {
            counter.count--;
            if (counter.count < 1)
            {
                paired_values.Remove(source);
            }
            if (paired_values.Count == 0)
            {
                sources.Remove(paired_value);
                paired_items_sets[value].Remove(paired_value);
            }
            if (sources.Count == 0)
            {
                paired_items.Remove(value);
                paired_items_sets.Remove(value);
                paired_items_readonly_sets.Remove(value);
            }
            return true;
        }
        return false;
    }

    public ReadOnlyDictionary<string, IReadOnlySet<int>> GetPairedItems()
    {
        return paired_items_readonly_dict;
    }

    public bool ContainsPairedItem(string value, int paired_value)
    {
        return paired_items_sets.TryGetValue(value, out HashSet<int> paired_set) && paired_set.Contains(paired_value);
    }
}