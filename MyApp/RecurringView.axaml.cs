using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace MyApp;

public partial class RecurringView : UserControl
{
    private readonly ExpenseStore _store = ExpenseStore.Instance;

    public RecurringView()
    {
        InitializeComponent();
        _store.DataChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        RecurringList.Children.Clear();

        if (_store.RecurringExpenses.Count == 0)
        {
            RecurringList.Children.Add(new TextBlock
            {
                Text       = "No recurring expenses yet.",
                Foreground = Brushes.Gray,
                FontSize   = 12
            });
            return;
        }

        foreach (var r in _store.RecurringExpenses)
            RecurringList.Children.Add(BuildRow(r));
    }

    private void AddRecurring_Click(object? sender, RoutedEventArgs e)
    {
        RecurringValidation.IsVisible = false;

        string name = RecurringNameInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            RecurringValidation.Text      = "Please enter a name.";
            RecurringValidation.IsVisible = true;
            return;
        }

        if (!decimal.TryParse(RecurringAmountInput.Text, out decimal amount) || amount <= 0)
        {
            RecurringValidation.Text      = "Please enter a valid amount greater than 0.";
            RecurringValidation.IsVisible = true;
            return;
        }

        var frequency = RecurringFrequencyPicker.SelectedIndex switch
        {
            0 => ExpenseFrequency.Monthly,
            1 => ExpenseFrequency.Weekly,
            2 => ExpenseFrequency.Daily,
            _ => ExpenseFrequency.Monthly
        };

        var category = (RecurringCategoryPicker.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";

        _store.AddRecurring(new RecurringExpense
        {
            Name      = name,
            Amount    = amount,
            Frequency = frequency,
            Category  = category
        });

        RecurringNameInput.Text   = "";
        RecurringAmountInput.Text = "";
        RecurringFrequencyPicker.SelectedIndex = 0;
        RecurringCategoryPicker.SelectedIndex  = 0;
    }

    private StackPanel BuildRow(RecurringExpense r)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing     = 8
        };

        row.Children.Add(new TextBlock
        {
            Text              = CategoryHelper.GetEmoji(r.Category),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize          = 14
        });

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = r.Name, FontWeight = FontWeight.SemiBold });
        info.Children.Add(new TextBlock
        {
            Text       = $"{r.Frequency} · ${r.Amount:F2}  (≈ ${r.MonthlyAmount:F2}/mo)",
            FontSize   = 11,
            Foreground = Brushes.Gray
        });
        row.Children.Add(info);

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
        del.Click += (_, _) => _store.RemoveRecurring(r);
        row.Children.Add(del);

        return row;
    }
}
