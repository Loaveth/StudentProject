using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MyApp;

public partial class DashboardView : UserControl
{
    private readonly ExpenseStore _store = ExpenseStore.Instance;

    public DashboardView()
    {
        InitializeComponent();
        
        // Initialize input fields with current values
        IncomeInput.Text = _store.MonthlyIncome > 0 ? _store.MonthlyIncome.ToString() : "";
        SavingsGoalInput.Text = _store.SavingsGoal > 0 ? _store.SavingsGoal.ToString() : "";
        
        Refresh();
    }

    public void Refresh()
    {
        UpdateMonthLabel();
        UpdateCards();
        UpdateBudgetBar();
        UpdateRecurringToggles();
        UpdateCategoryChart();
        UpdateFrequencyChart();
        UpdateSavings();
    }

    // ── Income & Savings Goal ─────────────────────────────────────────────────

    private void IncomeInput_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (decimal.TryParse(IncomeInput.Text, out decimal income) && income >= 0)
        {
            _store.MonthlyIncome = income;
            _store.NotifyChanged();
        }
    }

    private void SavingsGoalInput_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (decimal.TryParse(SavingsGoalInput.Text, out decimal goal) && goal >= 0)
        {
            _store.SavingsGoal = goal;
            _store.NotifyChanged();
        }
    }

    // ── Month navigation ──────────────────────────────────────────────────────

    private void UpdateMonthLabel()
    {
        var key = _store.CurrentMonthKey;
        if (DateTime.TryParseExact(key, "yyyy-MM", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dt))
        {
            CurrentMonthLabel.Text = dt.ToString("MMMM yyyy");
        }
    }

    private void PrevMonth_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var current = DateTime.ParseExact(_store.CurrentMonthKey, "yyyy-MM", CultureInfo.InvariantCulture);
        var prev = current.AddMonths(-1).ToString("yyyy-MM");
        _store.SwitchToMonth(prev);
        Refresh();
    }

    private void NextMonth_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var current = DateTime.ParseExact(_store.CurrentMonthKey, "yyyy-MM", CultureInfo.InvariantCulture);
        var next = current.AddMonths(1).ToString("yyyy-MM");
        _store.SwitchToMonth(next);
        Refresh();
    }

    // ── Summary cards ─────────────────────────────────────────────────────────

    private void UpdateCards()
    {
        decimal income    = _store.MonthlyIncome;
        decimal expenses  = _store.TotalMonthlyExpenses();
        decimal remaining = _store.MonthlyRemaining();

        IncomeCard.Text    = $"${income:F2}";
        ExpensesCard.Text  = $"${expenses:F2}";
        RemainingCard.Text = $"${remaining:F2}";
        RemainingCard.Foreground = remaining >= 0
            ? new SolidColorBrush(Color.Parse("#90ee90"))
            : new SolidColorBrush(Color.Parse("#ff6b6b"));
    }

    // ── Budget bar ────────────────────────────────────────────────────────────

    private void UpdateBudgetBar()
    {
        decimal income   = _store.MonthlyIncome;
        decimal expenses = _store.TotalMonthlyExpenses();

        if (income <= 0)
        {
            BudgetBar.Width     = 0;
            BudgetBarLabel.Text = "Set your monthly income in Add Expense.";
            return;
        }

        double pct    = Math.Min((double)(expenses / income), 1.0);
        int    pctInt = (int)(pct * 100);

        double containerWidth = (BudgetBar.Parent as Border)?.Bounds.Width ?? 500;
        BudgetBar.Width = Math.Max(2, pct * containerWidth);
        BudgetBarLabel.Text = $"{pctInt}% of income spent  (${expenses:F2} of ${income:F2})";
        BudgetBar.Background = pct >= 1.0
            ? Brushes.Red
            : pct >= 0.8
                ? Brushes.Orange
                : new SolidColorBrush(Color.Parse("#191970"));
    }

    // ── Recurring toggles ─────────────────────────────────────────────────────

    private void UpdateRecurringToggles()
    {
        RecurringToggles.Children.Clear();
        var month = _store.CurrentMonth;

        if (month.RecurringEntries.Count == 0)
        {
            RecurringToggles.Children.Add(new TextBlock
            {
                Text       = "No recurring expenses set up yet. Go to 🔁 Recurring to add some.",
                Foreground = Brushes.Gray,
                FontSize   = 12,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
            return;
        }

        foreach (var entry in month.RecurringEntries)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var cb = new CheckBox { IsChecked = entry.IsIncluded };
            cb.IsCheckedChanged += (_, _) =>
            {
                entry.IsIncluded = cb.IsChecked == true;
                _store.NotifyChanged();
            };

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text       = $"{CategoryHelper.GetEmoji(entry.Template.Category)} {entry.Template.Name}",
                FontWeight = FontWeight.SemiBold
            });
            info.Children.Add(new TextBlock
            {
                Text       = $"{entry.Template.Frequency} · ${entry.Template.Amount:F2}  (≈ ${entry.Template.MonthlyAmount:F2}/mo)",
                FontSize   = 11,
                Foreground = Brushes.Gray
            });

            row.Children.Add(cb);
            row.Children.Add(info);
            RecurringToggles.Children.Add(row);
        }
    }

    // ── Category chart ────────────────────────────────────────────────────────

    private void UpdateCategoryChart()
    {
        CategoryChart.Children.Clear();
        var month = _store.CurrentMonth;

        var totals = new Dictionary<string, decimal>();

        foreach (var e in month.OneOffExpenses)
        {
            if (!totals.ContainsKey(e.Category)) totals[e.Category] = 0;
            totals[e.Category] += e.MonthlyAmount;
        }
        foreach (var r in month.RecurringEntries)
        {
            if (!r.IsIncluded) continue;
            if (!totals.ContainsKey(r.Template.Category)) totals[r.Template.Category] = 0;
            totals[r.Template.Category] += r.Template.MonthlyAmount;
        }

        if (totals.Count == 0)
        {
            CategoryChart.Children.Add(new TextBlock { Text = "No expenses this month.", Foreground = Brushes.Gray, FontSize = 12 });
            return;
        }

        decimal max = 0;
        foreach (var v in totals.Values) if (v > max) max = v;

        foreach (var kv in totals)
        {
            var brush = CategoryHelper.GetColor(kv.Key);
            CategoryChart.Children.Add(BuildBar(kv.Key, kv.Value, max, brush));
        }
    }

    // ── Frequency chart ───────────────────────────────────────────────────────

    private void UpdateFrequencyChart()
    {
        FrequencyChart.Children.Clear();
        var month = _store.CurrentMonth;

        var totals = new Dictionary<string, decimal>
        {
            ["Daily"] = 0, ["Weekly"] = 0, ["Monthly"] = 0, ["One-time"] = 0
        };

        foreach (var e in month.OneOffExpenses)
        {
            string key = FreqKey(e.Frequency);
            totals[key] += e.MonthlyAmount;
        }
        foreach (var r in month.RecurringEntries)
        {
            if (!r.IsIncluded) continue;
            string key = FreqKey(r.Template.Frequency);
            totals[key] += r.Template.MonthlyAmount;
        }

        decimal max = 0;
        foreach (var v in totals.Values) if (v > max) max = v;

        foreach (var kv in totals)
        {
            if (kv.Value == 0) continue;
            FrequencyChart.Children.Add(BuildBar(kv.Key, kv.Value, max,
                new SolidColorBrush(Color.Parse("#191970"))));
        }

        if (FrequencyChart.Children.Count == 0)
            FrequencyChart.Children.Add(new TextBlock { Text = "No expenses this month.", Foreground = Brushes.Gray, FontSize = 12 });
    }

    private static string FreqKey(ExpenseFrequency f) => f switch
    {
        ExpenseFrequency.Daily   => "Daily",
        ExpenseFrequency.Weekly  => "Weekly",
        ExpenseFrequency.Monthly => "Monthly",
        _                        => "One-time"
    };

    // ── Shared bar builder ────────────────────────────────────────────────────

    private static StackPanel BuildBar(string label, decimal value, decimal max, IBrush color)
    {
        var row = new StackPanel { Spacing = 4 };

        var valueBlock = new TextBlock { Text = $"${value:F2}/mo", FontSize = 12, Foreground = Brushes.Gray };
        var labelBlock = new TextBlock { Text = label, FontSize = 12 };
        var header     = new DockPanel();
        DockPanel.SetDock(valueBlock, Dock.Right);
        header.Children.Add(valueBlock);
        header.Children.Add(labelBlock);
        row.Children.Add(header);

        double pct = max > 0 ? (double)(value / max) : 0;
        var fill = new Border
        {
            Background          = color,
            CornerRadius        = new Avalonia.CornerRadius(3),
            Height              = 14,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width               = Math.Max(2, pct * 350)
        };
        row.Children.Add(new Border
        {
            Background   = new SolidColorBrush(Color.Parse("#dde")),
            CornerRadius = new Avalonia.CornerRadius(3),
            Height       = 14,
            Child        = fill
        });
        return row;
    }

    // ── Savings calculator ────────────────────────────────────────────────────

    private void UpdateSavings()
    {
        decimal income    = _store.MonthlyIncome;
        decimal expenses  = _store.TotalMonthlyExpenses();
        decimal remaining = income - expenses;
        decimal goal      = _store.SavingsGoal;

        if (income <= 0)
        {
            SavingsLine1.Text = "Set your monthly income in the Add Expense screen.";
            SavingsLine2.Text = "";
            SavingsLine3.Text = "";
            return;
        }

        SavingsLine1.Text = $"After all expenses you have ${remaining:F2} available per month.";

        if (goal <= 0)
        {
            SavingsLine2.Text = "Set a savings goal in the Add Expense screen to see projections.";
            SavingsLine3.Text = "";
            return;
        }

        SavingsLine2.Text = $"Savings goal: ${goal:F2}";

        if (remaining <= 0)
        {
            SavingsLine3.Text       = "⚠️ Expenses exceed income — no savings possible at current spending.";
            SavingsLine3.Foreground = Brushes.Red;
            return;
        }

        int fullMonths = (int)Math.Ceiling(goal / remaining);
        SavingsLine3.Text       = $"✅ Saving all remaining income: goal reached in ~{fullMonths} month(s).";
        SavingsLine3.Foreground = Brushes.DarkGreen;
    }
}