using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using lab4.EFCore;
using Lab4.EFCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace lab4;

public sealed partial class DataGridForm : Form
{
    private readonly MyDbContext _dbContext;
    private readonly BindingSource _dataGridBindingSource;
    private IList? _data;

    public DataGridForm(MyDbContext dbContext)
    {
        _dbContext = dbContext;
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
            var tables = _dbContext.Model
                .GetEntityTypes()
                .Where(x => x.ClrType.Namespace == typeof(Category).Namespace)
                .Select(x => x.GetTableName())
                .Where(x => x is not null)
                .ToList();
            selectTableComboBox.DataSource = tables;

            SelectTable(tables.FirstOrDefault());
        }
    }


    private void FlushChangesToDatabase()
    {
        Debug.Assert(_data is not null);
        try
        {
            _dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static readonly MethodInfo _GetDataMethod = typeof(DataGridForm)
        .GetMethod(nameof(QueryAllRowsInTable), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private List<T> QueryAllRowsInTable<T>(string name)
        where T : class
    {
        var query = _dbContext.Set<T>(name);
        return query.ToList();
    }

    private void SelectTable(string? tableName)
    {
        if (tableName is null)
        {
            _dataGridBindingSource.DataSource = null;
            return;
        }

        var model = _dbContext.Model;
        var entityType = model
            .GetEntityTypes()
            .FirstOrDefault(x => x.GetTableName() == tableName);
        Debug.Assert(entityType is not null);
        _dbContext.ChangeTracker.Clear();

        _data = (IList) _GetDataMethod
            .MakeGenericMethod(entityType.ClrType)
            .Invoke(this, new object?[] { entityType.Name })!;

        _dataGridBindingSource.DataSource = _data;
    }
}
