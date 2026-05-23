using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MyApp;

public enum ExpenseFrequency
{
    Daily,
    Weekly,
    Monthly,
    OneTime
}

// A one-off expense for a specific month
public class Expense
{
    public string Name { get; set; } = "Unnamed";
    public decimal Amount { get; set; }
    public ExpenseFrequency Frequency { get; set; }
    public string Category { get; set; } = "Other";

    public decimal MonthlyAmount => Frequency switch
    {
        ExpenseFrequency.Daily   => Amount * 30,
        ExpenseFrequency.Weekly  => Amount * 4.33m,
        ExpenseFrequency.Monthly => Amount,
        ExpenseFrequency.OneTime => Amount,
        _                        => Amount
    };
}

// A recurring template — set up once, appears every month
public class RecurringExpense
{
    public string Name { get; set; } = "Unnamed";
    public decimal Amount { get; set; }
    public ExpenseFrequency Frequency { get; set; }
    public string Category { get; set; } = "Other";

    public decimal MonthlyAmount => Frequency switch
    {
        ExpenseFrequency.Daily   => Amount * 30,
        ExpenseFrequency.Weekly  => Amount * 4.33m,
        ExpenseFrequency.Monthly => Amount,
        ExpenseFrequency.OneTime => Amount,
        _                        => Amount
    };
}

// A recurring expense as it appears in a specific month, with an on/off toggle
public class MonthlyRecurringEntry
{
    public RecurringExpense Template { get; set; } = null!;
    public bool IsIncluded { get; set; } = true;
}

// All data for one month
public class MonthData
{
    public string MonthKey { get; set; } = "";          // e.g. "2025-05"
    public List<Expense> OneOffExpenses { get; set; } = new();
    public List<MonthlyRecurringEntry> RecurringEntries { get; set; } = new();

    public decimal TotalMonthlyExpenses()
    {
        decimal total = 0;
        foreach (var e in OneOffExpenses)
            total += e.MonthlyAmount;
        foreach (var r in RecurringEntries)
            if (r.IsIncluded) total += r.Template.MonthlyAmount;
        return total;
    }
}

public class ExpenseStore
{
    private static ExpenseStore? _instance;
    public static ExpenseStore Instance => _instance ??= new ExpenseStore();

    // Recurring templates — persist across all months
    public List<RecurringExpense> RecurringExpenses { get; } = new();

    // Per-month data keyed by "yyyy-MM"
    public Dictionary<string, MonthData> MonthHistory { get; } = new();

    // Currently viewed month
    public string CurrentMonthKey { get; private set; } = DateTime.Now.ToString("yyyy-MM");

    public decimal MonthlyIncome { get; set; } = 0;
    public decimal SavingsGoal { get; set; } = 0;

    public event Action? DataChanged;

    // ── Current month helpers ─────────────────────────────────────────────────

    public MonthData GetOrCreateMonth(string key)
    {
        if (!MonthHistory.ContainsKey(key))
        {
            var month = new MonthData { MonthKey = key };
            
            // Only populate recurring entries if this is the current month or a future month
            // For past months, they should have been created when they were current
            var monthDate = DateTime.ParseExact(key, "yyyy-MM", CultureInfo.InvariantCulture);
            var currentDate = DateTime.ParseExact(CurrentMonthKey, "yyyy-MM", CultureInfo.InvariantCulture);
            
            if (monthDate >= currentDate)
            {
                // Current or future month: populate with current recurring templates
                foreach (var r in RecurringExpenses)
                    month.RecurringEntries.Add(new MonthlyRecurringEntry { Template = r, IsIncluded = true });
            }
            // For past months, leave RecurringEntries empty (they should have been saved)
            
            MonthHistory[key] = month;
        }
        return MonthHistory[key];
    }

    public MonthData CurrentMonth => GetOrCreateMonth(CurrentMonthKey);

    public void SwitchToMonth(string key)
    {
        CurrentMonthKey = key;
        DataChanged?.Invoke();
    }

    // Advance to next month, carrying recurring templates forward
    public void StartNewMonth()
    {
        var next = DateTime.ParseExact(CurrentMonthKey, "yyyy-MM", CultureInfo.InvariantCulture)
                           .AddMonths(1)
                           .ToString("yyyy-MM");
        CurrentMonthKey = next;
        GetOrCreateMonth(next); // creates with current recurring templates
        DataChanged?.Invoke();
    }

    // ── One-off expenses ──────────────────────────────────────────────────────

    public void AddExpense(Expense expense)
    {
        CurrentMonth.OneOffExpenses.Add(expense);
        DataChanged?.Invoke();
    }

    public void RemoveExpense(Expense expense)
    {
        CurrentMonth.OneOffExpenses.Remove(expense);
        DataChanged?.Invoke();
    }

    // ── Recurring templates ───────────────────────────────────────────────────

    public void AddRecurring(RecurringExpense r)
    {
        RecurringExpenses.Add(r);
        // Add to all existing months that don't already have it
        foreach (var month in MonthHistory.Values)
        {
            bool alreadyThere = month.RecurringEntries.Any(e => e.Template == r);
            if (!alreadyThere)
                month.RecurringEntries.Add(new MonthlyRecurringEntry { Template = r, IsIncluded = true });
        }
        DataChanged?.Invoke();
    }

    public void RemoveRecurring(RecurringExpense r)
    {
        RecurringExpenses.Remove(r);
        foreach (var month in MonthHistory.Values)
            month.RecurringEntries.RemoveAll(e => e.Template == r);
        DataChanged?.Invoke();
    }

    public void NotifyChanged() => DataChanged?.Invoke();

    public decimal TotalMonthlyExpenses() => CurrentMonth.TotalMonthlyExpenses();
    public decimal MonthlyRemaining() => MonthlyIncome - TotalMonthlyExpenses();

    public List<string> AllMonthKeys() => MonthHistory.Keys.OrderByDescending(k => k).ToList();

    // ── CSV Save / Load ───────────────────────────────────────────────────────

    public void SaveToCsv(string filePath)
    {
        var lines = new List<string>();

        // Settings
        lines.Add("[Settings]");
        lines.Add($"MonthlyIncome;{MonthlyIncome.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"SavingsGoal;{SavingsGoal.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"CurrentMonth;{CurrentMonthKey}");

        // Recurring templates
        lines.Add("[Recurring]");
        foreach (var r in RecurringExpenses)
            lines.Add($"{r.Name};{r.Amount.ToString(CultureInfo.InvariantCulture)};{r.Frequency};{r.Category}");

        // Monthly data
        foreach (var kv in MonthHistory)
        {
            lines.Add($"[Month:{kv.Key}]");
            foreach (var e in kv.Value.OneOffExpenses)
                lines.Add($"OneOff;{e.Name};{e.Amount.ToString(CultureInfo.InvariantCulture)};{e.Frequency};{e.Category}");
            foreach (var entry in kv.Value.RecurringEntries)
                lines.Add($"Recurring;{entry.Template.Name};{(entry.IsIncluded ? "1" : "0")}");
        }

        File.WriteAllLines(filePath, lines);
    }

    public void LoadFromCsv(string filePath)
    {
        if (!File.Exists(filePath)) return;

        RecurringExpenses.Clear();
        MonthHistory.Clear();

        var lines = File.ReadAllLines(filePath);
        string section = "";
        MonthData? currentMonthData = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("[Month:"))
            {
                string key = line.Substring(7, line.Length - 8);
                currentMonthData = new MonthData { MonthKey = key };
                MonthHistory[key] = currentMonthData;
                section = "month";
                continue;
            }
            if (line == "[Settings]") { section = "settings"; continue; }
            if (line == "[Recurring]") { section = "recurring"; currentMonthData = null; continue; }

            var parts = line.Split(';');

            if (section == "settings" && parts.Length == 2)
            {
                if (parts[0] == "MonthlyIncome" && decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal inc))
                    MonthlyIncome = inc;
                if (parts[0] == "SavingsGoal" && decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal goal))
                    SavingsGoal = goal;
                if (parts[0] == "CurrentMonth")
                    CurrentMonthKey = parts[1];
            }
            else if (section == "recurring" && parts.Length == 4)
            {
                if (!decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amt)) continue;
                if (!Enum.TryParse<ExpenseFrequency>(parts[2], out var freq)) continue;
                RecurringExpenses.Add(new RecurringExpense { Name = parts[0], Amount = amt, Frequency = freq, Category = parts[3] });
            }
            else if (section == "month" && currentMonthData != null)
            {
                if (parts[0] == "OneOff" && parts.Length == 5)
                {
                    if (!decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amt)) continue;
                    if (!Enum.TryParse<ExpenseFrequency>(parts[3], out var freq)) continue;
                    currentMonthData.OneOffExpenses.Add(new Expense { Name = parts[1], Amount = amt, Frequency = freq, Category = parts[4] });
                }
                else if (parts[0] == "Recurring" && parts.Length == 3)
                {
                    var template = RecurringExpenses.FirstOrDefault(r => r.Name == parts[1]);
                    if (template != null)
                        currentMonthData.RecurringEntries.Add(new MonthlyRecurringEntry { Template = template, IsIncluded = parts[2] == "1" });
                }
            }
        }

        DataChanged?.Invoke();
    }
}