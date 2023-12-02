using System.Data;
using System.Diagnostics;
using Lab1.DataLayer;
using Microsoft.Data.SqlClient;

namespace lab3;

public partial class DataGridForm : Form
{
    private readonly SqlConnection _connection;
    private SqlDataAdapter? _adapter;
    private DataTable? _dataTable;
    private readonly BindingSource _dataGridBindingSource;

    public DataGridForm(SqlConnection connection)
    {
        _connection = connection;
        _dataGridBindingSource = new BindingSource();

        InitializeComponent();

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
        };
        Controls.Add(mainPanel);

        ComboBox selectTableComboBox;
        {
            selectTableComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            selectTableComboBox.SelectedIndexChanged += (_, _) =>
            {
                var selectedTable = (string?) selectTableComboBox.SelectedItem;
                Debug.Assert(selectedTable is not null);
                SelectTable(selectedTable);
            };
            mainPanel.Controls.Add(selectTableComboBox);
        }

        {
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = _dataGridBindingSource,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
            };
            dataGridView.CellValueChanged += (_, _) => FlushChangesToDatabase();
            dataGridView.UserDeletedRow += (_, _) => FlushChangesToDatabase();
            dataGridView.UserAddedRow += (_, _) => FlushChangesToDatabase();
            mainPanel.Controls.Add(dataGridView);

            dataGridView.DataSource = _dataGridBindingSource;
        }

        {
            foreach (var tableName in GetTableNames())
            {
                selectTableComboBox.Items.Add(tableName);
            }

            if (selectTableComboBox.Items.Count > 0)
            {
                selectTableComboBox.SelectedIndex = 0;
            }
        }
    }

    private IEnumerable<string> GetTableNames()
    {
        var tableConstraints = TableConstraints.Create();
        tableConstraints.Type = TableType.BaseTable;

        var tablesTable = _connection.GetSchema("Tables", tableConstraints);
        var rows = tablesTable.Rows;
        int rowCount = rows.Count;
        for (int i = 0; i < rowCount; i++)
        {
            var row = rows[i];
            var tableInfo = TableInfoRow.Parse(row);
            var fullyQualifiedName = new FullyQualifiedName(
                tableInfo.Schema,
                tableInfo.Name);
            yield return fullyQualifiedName.ToString();
        }
    }

    private void FlushChangesToDatabase()
    {
        Debug.Assert(_adapter is not null);
        try
        {
            _adapter.Update(_dataTable!);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectTable(string? tableName)
    {
        if (tableName is null)
        {
            _dataGridBindingSource.DataSource = null;
            return;
        }
        _adapter = new SqlDataAdapter($"SELECT * FROM {tableName}", _connection);
        _ = new SqlCommandBuilder(_adapter);
        _dataTable = new DataTable();
        _adapter.Fill(_dataTable);
        _dataGridBindingSource.DataSource = _dataTable;
    }
}
