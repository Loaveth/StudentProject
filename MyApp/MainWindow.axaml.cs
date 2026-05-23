using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;

namespace MyApp;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboard;
    private readonly ExpenseFormView _expenseForm;
    private readonly RecurringView _recurringView;

    public MainWindow()
    {
        InitializeComponent();

        _dashboard     = new DashboardView();
        _expenseForm   = new ExpenseFormView();
        _recurringView = new RecurringView();

        ContentArea.Content = _dashboard;

        ExpenseStore.Instance.DataChanged += OnDataChanged;
        UpdateMonthLabel();
    }

    private void OnDataChanged()
    {
        _dashboard.Refresh();
        UpdateMonthLabel();
    }

    private void UpdateMonthLabel()
    {
        // Format "2025-05" → "May 2025"
        var key = ExpenseStore.Instance.CurrentMonthKey;
        if (DateTime.TryParseExact(key, "yyyy-MM",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
        {
            CurrentMonthLabel.Text = dt.ToString("MMMM yyyy");
        }
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _dashboard;
        _dashboard.Refresh();
    }

    private void NavAddExpense_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _expenseForm;
    }

    private void NavRecurring_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _recurringView;
        _recurringView.Refresh();
    }

    private void NewMonthBtn_Click(object sender, RoutedEventArgs e)
    {
        ExpenseStore.Instance.StartNewMonth();
        ContentArea.Content = _dashboard;
        _dashboard.Refresh();
    }

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filePath  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"expenses_{timestamp}.csv");
        ExpenseStore.Instance.SaveToCsv(filePath);

        SaveConfirmLabel.IsVisible = true;
        await System.Threading.Tasks.Task.Delay(3000);
        SaveConfirmLabel.IsVisible = false;
    }

    private async void LoadBtn_Click(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Open Expense File",
            AllowMultiple  = false,
            FileTypeFilter = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
        });

        if (files.Count == 0) return;

        ExpenseStore.Instance.LoadFromCsv(files[0].Path.LocalPath);
        ContentArea.Content = _dashboard;
        _dashboard.Refresh();
    }
}