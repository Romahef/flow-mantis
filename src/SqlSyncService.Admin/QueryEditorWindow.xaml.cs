using System.Windows;
using SqlSyncService.Config;

namespace SqlSyncService.Admin;

public partial class QueryEditorWindow : Window
{
    public QueryDefinition? Query { get; private set; }

    public QueryEditorWindow(QueryDefinition query)
    {
        InitializeComponent();
        Query = query;
        LoadQuery();
    }

    private void LoadQuery()
    {
        if (Query == null) return;

        QueryNameTextBox.Text = Query.Name;
        SqlTextBox.Text = Query.Sql;
        PaginableCheckBox.IsChecked = Query.Paginable;

        if (Query.PaginationMode == "Token")
        {
            PaginationModeComboBox.SelectedIndex = 1;
        }

        OrderByTextBox.Text = Query.OrderBy;
        KeyColumnsTextBox.Text = string.Join(", ", Query.KeyColumns);

        UpdatePaginationPanels();
    }

    private void PaginableCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePaginationPanels();
    }

    private void PaginationModeComboBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdatePaginationPanels();
    }

    private void UpdatePaginationPanels()
    {
        var isPaginable = PaginableCheckBox.IsChecked == true;
        var isOffset = PaginationModeComboBox.SelectedIndex == 0;

        PaginationModeComboBox.IsEnabled = isPaginable;

        if (isPaginable)
        {
            OffsetPanel.Visibility = isOffset ? Visibility.Visible : Visibility.Collapsed;
            TokenPanel.Visibility = isOffset ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            OffsetPanel.Visibility = Visibility.Collapsed;
            TokenPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(QueryNameTextBox.Text))
        {
            MessageBox.Show("Query name is required", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SqlTextBox.Text))
        {
            MessageBox.Show("SQL query is required", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PaginableCheckBox.IsChecked == true)
        {
            if (PaginationModeComboBox.SelectedIndex == 0 && string.IsNullOrWhiteSpace(OrderByTextBox.Text))
            {
                MessageBox.Show("OrderBy column is required for Offset pagination", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PaginationModeComboBox.SelectedIndex == 1 && string.IsNullOrWhiteSpace(KeyColumnsTextBox.Text))
            {
                MessageBox.Show("Key columns are required for Token pagination", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Save
        if (Query == null)
            Query = new QueryDefinition();

        Query.Name = QueryNameTextBox.Text.Trim();
        Query.Sql = SqlTextBox.Text.Trim();
        Query.Paginable = PaginableCheckBox.IsChecked == true;
        Query.PaginationMode = PaginationModeComboBox.SelectedIndex == 0 ? "Offset" : "Token";
        Query.OrderBy = OrderByTextBox.Text.Trim();
        Query.KeyColumns = KeyColumnsTextBox.Text
            .Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
