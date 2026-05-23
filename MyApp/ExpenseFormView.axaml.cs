using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace MyApp;

public partial class ExpenseFormView : UserControl
{
    private readonly ExpenseStore _store = ExpenseStore.Instance;

    public ExpenseFormView()
    {
        InitializeComponent();

        _store.DataChanged += RebuildList;
        RebuildList();
    }

    // ── Add expense ───────────────────────────────────────────────────────────

    private void AddExpense_Click(object? sender, RoutedEventArgs e)
    {
        ValidationMessage.IsVisible = false;

        string name = ExpenseNameInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter an expense name.");
            return;
        }

        if (!decimal.TryParse(ExpenseAmountInput.Text, out decimal amount) || amount <= 0)
        {
            ShowError("Please enter a valid amount greater than 0.");
            return;
        }

        var frequency = FrequencyPicker.SelectedIndex switch
        {
            0 => ExpenseFrequency.Monthly,
            1 => ExpenseFrequency.Weekly,
            2 => ExpenseFrequency.Daily,
            3 => ExpenseFrequency.OneTime,
            _ => ExpenseFrequency.Monthly
        };

        var category = (CategoryPicker.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";

        _store.AddExpense(new Expense
        {
            Name      = name,
            Amount    = amount,
            Frequency = frequency,
            Category  = category
        });

        ExpenseNameInput.Text         = "";
        ExpenseAmountInput.Text       = "";
        FrequencyPicker.SelectedIndex = 0;
        CategoryPicker.SelectedIndex  = 0;
    }

    // ── List ──────────────────────────────────────────────────────────────────

    private void RebuildList()
    {
        ExpensesList.Children.Clear();

        foreach (var expense in _store.CurrentMonth.OneOffExpenses)
            ExpensesList.Children.Add(BuildExpenseRow(expense));

        UpdateSummary();
    }

    private StackPanel BuildExpenseRow(Expense expense)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        row.Children.Add(new TextBlock
        {
            Text              = CategoryEmoji(expense.Category),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize          = 14
        });

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = expense.Name, FontWeight = FontWeight.SemiBold });
        info.Children.Add(new TextBlock
        {
            Text       = $"{expense.Frequency} · ${expense.Amount:F2}  (≈ ${expense.MonthlyAmount:F2}/mo)",
            FontSize   = 11,
            Foreground = Brushes.Gray
        });
        row.Children.Add(info);

        row.Children.Add(new Border { HorizontalAlignment = HorizontalAlignment.Stretch });

        var del = new Button
        {
            Content                    = "✕",
            Width                      = 24,
            Height                     = 24,
            Padding                    = new Avalonia.Thickness(0),
            VerticalAlignment          = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Center,
            Background                 = Brushes.Transparent,
            Foreground                 = Brushes.Gray
        };
        del.Click += (_, _) => _store.RemoveExpense(expense);
        row.Children.Add(del);

        return row;
    }

    private void UpdateSummary()
    {
        decimal total     = _store.TotalMonthlyExpenses();
        decimal remaining = _store.MonthlyRemaining();

        TotalLabel.Text            = $"Total monthly expenses: ${total:F2}";
        RemainLabel.Text           = $"Remaining: ${remaining:F2}";
        RemainLabel.Foreground     = remaining >= 0 ? Brushes.DarkGreen : Brushes.Red;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowError(string msg)
    {
        ValidationMessage.Text      = msg;
        ValidationMessage.IsVisible = true;
    }

    private static string CategoryEmoji(string category) => category switch
    {
        "Housing"       => "🏠",
        "Food"          => "🍔",
        "Transport"     => "🚗",
        "Health"        => "❤️",
        "Entertainment" => "🎬",
        "Subscriptions" => "📱",
        _               => "📦"
    };
}