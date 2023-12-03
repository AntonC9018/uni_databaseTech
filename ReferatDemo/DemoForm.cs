using System.ComponentModel;

namespace ReferatDemo;

public enum DockPosition
{
    Top,
    Bottom,
    Left,
    Right,
}

public sealed class NamedImage
{
    public required string Name { get; init; }
    public required Image Image { get; init; }
    public override string ToString() => Name;
}

// A demo form that illustrates the following properties and methods:

// BackgroundColor
// BackgroundImage
// BackgroundImageLayout
// BorderStyle

// BeginEdit
// CancelEdit
// ClearSelection
public sealed partial class DemoForm : Form
{
    private readonly BindingList<RowModel> _rows = new();
    private readonly BindingSource _rowsBindingSource = new();

    public DemoForm()
    {
        var random = new Random();
        foreach (var row in DataGenerationHelper.GenerateRandomData(random, 100))
            _rows.Add(row);
        _rowsBindingSource.DataSource = _rows;

        InitializeComponent();

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
        };
        Controls.Add(tabControl);

        foreach (var (func, name) in new (Action<TabPage>, string)[]
            {
                (InitializeAutoScrollDemo, "AutoScroll"),
                (InitializeAutoSizeDemo, "AutoSize"),
                (InitializeBackgroundDemo, "Background"),
            })
        {
            var tab = new TabPage(name);
            tabControl.TabPages.Add(tab);
            func(tab);
        }
    }

    private void InitializeAutoScrollDemo(TabPage tab)
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 10,
            AutoScroll = true,
        };

        var dockPositionValues = Enum.GetValues<DockPosition>();

        const int squareCount = 100;
        var largeFont = new Font(Font.FontFamily, 24.0f, FontStyle.Bold);
        for (int i = 0; i < squareCount; i++)
        {
            var color = (i % 8) switch
            {
                0 => Color.Pink,
                1 => Color.LightGreen,
                2 => Color.LightBlue,
                3 => Color.LightYellow,
                4 => Color.LightGray,
                5 => Color.Orange,
                6 => Color.Plum,
                7 => Color.Cyan,
                _ => throw new ArgumentOutOfRangeException()
            };
            var box = new Panel
            {
                BackColor = color,
                Height = 200,
                Width = 200,
            };
            body.Controls.Add(box);

            const int sideTextSize = 60;

            foreach (var dockPosition in dockPositionValues)
            {
                var label = new Label
                {
                    Text = dockPosition.ToString(),
                    Width = sideTextSize,
                    Height = sideTextSize,
                    TextAlign = dockPosition switch
                    {
                        DockPosition.Bottom => ContentAlignment.BottomCenter,
                        DockPosition.Top => ContentAlignment.TopCenter,
                        DockPosition.Left => ContentAlignment.MiddleLeft,
                        DockPosition.Right => ContentAlignment.MiddleRight,
                        _ => throw new ArgumentOutOfRangeException(nameof(dockPosition), dockPosition, null),
                    },
                };
                label.Location = dockPosition switch
                {
                    DockPosition.Top => new Point((box.Width - label.Width) / 2, 0),
                    DockPosition.Bottom => new Point((box.Width - label.Width) / 2, box.Height - label.Height),
                    DockPosition.Left => new Point(0, (box.Height - label.Height) / 2),
                    DockPosition.Right => new Point(box.Width - label.Width, (box.Height - label.Height) / 2),
                    _ => throw new ArgumentOutOfRangeException(nameof(dockPosition), dockPosition, null),
                };
                box.Controls.Add(label);
            }

            // Print the index in the center with bold font
            {
                var label = new Label
                {
                    Text = i.ToString(),
                    Font = largeFont,
                    Width = box.Width - sideTextSize * 2,
                    Height = box.Height - sideTextSize * 2,
                    TextAlign = ContentAlignment.MiddleCenter,
                };
                label.Location = new Point(
                    (box.Width - label.Width) / 2,
                    (box.Height - label.Height) / 2);
                box.Controls.Add(label);
            }
        }

        var pageTable = CreateLayout(top =>
        {
            var indexInputControl = new NumericUpDown
            {
                Minimum = 0,
                Maximum = squareCount,
            };
            top.Controls.Add(indexInputControl);

            var autoScrollOffsetCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (var dockPosition in dockPositionValues)
            {
                autoScrollOffsetCombo.Items.Add(dockPosition);
            }
            autoScrollOffsetCombo.SelectedItem = DockPosition.Top;
            top.Controls.Add(autoScrollOffsetCombo);

            var scrollButton = new Button
            {
                Dock = DockStyle.Top,
                Text = "Scroll!",
                Height = 50,
            };
            top.Controls.Add(scrollButton);

            Control? lastControl = null;
            Scroll();

            indexInputControl.ValueChanged += (_, _) => Scroll();
            autoScrollOffsetCombo.SelectedValueChanged += (_, _) => Scroll();
            scrollButton.Click += (_, _) => Scroll();

            void Scroll()
            {
                if (lastControl is not null)
                {
                    lastControl.BackColor = Color.Empty;
                }
                lastControl = null;

                var index = (int) indexInputControl.Value;
                if (index < 0 || index >= squareCount)
                    return;

                var box = body.Controls[index];

                var selectedPosition = (DockPosition) autoScrollOffsetCombo.SelectedItem!;
                var selectedSideControl = box.Controls[(int) selectedPosition];
                var offset = selectedSideControl.Location + selectedSideControl.Size / 2;

                lastControl = selectedSideControl;
                selectedSideControl.BackColor = Color.Red;

                // Has to be negated, no idea why.
                box.AutoScrollOffset = new Point(
                    box.Width - offset.X,
                    box.Height - offset.Y);
                body.ScrollControlIntoView(box);
            }
        }, body);

        tab.Controls.Add(pageTable);
    }

    private void InitializeAutoSizeDemo(TabPage tab)
    {
        var dataGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _rowsBindingSource,
        };

        // Check box to toggle AutoSize
        var pageTable = CreateLayout(topPanel =>
        {
            {
                var autoSizeCheckBox = new CheckBox
                {
                    Text = "AutoSize",
                    Checked = dataGridView.AutoSize,
                };
                autoSizeCheckBox.CheckedChanged += (_, _) =>
                {
                    dataGridView.AutoSize = autoSizeCheckBox.Checked;
                };
                topPanel.Controls.Add(autoSizeCheckBox);
            }

            AutoSizeComboBox(
                mode => dataGridView.AutoSizeColumnsMode = mode,
                DataGridViewAutoSizeColumnsMode.AllCells);
            AutoSizeComboBox(
                mode => dataGridView.AutoSizeRowsMode = mode,
                DataGridViewAutoSizeRowsMode.AllCells);

            void AutoSizeComboBox<T>(Action<T> setter, T defaultValue = default)
                where T : struct, Enum
            {
                var label = new Label
                {
                    Text = typeof(T).Name,
                };
                topPanel.Controls.Add(label);

                var autoSizeOptions = Enum.GetValues<T>();
                var autoSizeOptionsAsObjects = autoSizeOptions.Cast<object>().ToArray();

                var columnsCombo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                columnsCombo.DataSource = autoSizeOptionsAsObjects;
                columnsCombo.SelectedValue = defaultValue;
                setter(defaultValue);
                columnsCombo.SelectedIndexChanged += (source, _) =>
                {
                    var combo = (ComboBox) source!;
                    var item = combo.SelectedItem;
                    setter((T) item!);
                };
                topPanel.Controls.Add(columnsCombo);
            }
        }, dataGridView);

        tab.Controls.Add(pageTable);
    }

    private void InitializeBackgroundDemo(TabPage tab)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
        };

        var pageTable = CreateLayout(topPanel =>
        {
            var colorComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (var color in new[]
            {
                Color.Transparent,
                Color.Red,
                Color.Green,
                Color.Blue,
            })
            {
                colorComboBox.Items.Add(color);
            }
            colorComboBox.SelectedItem = Color.Transparent;
            topPanel.Controls.Add(colorComboBox);

            var imageComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            imageComboBox.Items.Add("");
            foreach (var imageName in new[]
            {
                "geist.jpeg",
                "mossy_stone_bricks.png",
            })
            {
                var imagePath = Path.Combine("Resources", imageName);
                var image = Image.FromFile(imagePath);
                imageComboBox.Items.Add(new NamedImage
                {
                    Name = imageName,
                    Image = image,
                });
            }
            imageComboBox.SelectedItem = null;
            topPanel.Controls.Add(imageComboBox);

            var layoutComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (var layout in Enum.GetValues<ImageLayout>())
            {
                layoutComboBox.Items.Add(layout);
            }
            layoutComboBox.SelectedItem = ImageLayout.None;
            topPanel.Controls.Add(layoutComboBox);

            colorComboBox.SelectedValueChanged += (_, _) =>
            {
                var color = (Color) colorComboBox.SelectedItem;
                panel.BackColor = color;
            };
            imageComboBox.SelectedValueChanged += (_, _) =>
            {
                var image = imageComboBox.SelectedItem as NamedImage;
                panel.BackgroundImage = image?.Image;
            };
            layoutComboBox.SelectedValueChanged += (_, _) =>
            {
                var layout = (ImageLayout) layoutComboBox.SelectedItem!;
                panel.BackgroundImageLayout = layout;
            };
        }, panel);

        tab.Controls.Add(pageTable);
    }

    private TableLayoutPanel CreateLayout(
        Action<TableLayoutPanel> configureTop,
        Control mainControl)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            AutoSize = false,
        };

        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            BackColor = Color.LightBlue,
            ColumnCount = 1,
            AutoSize = true,
        };
        layout.Controls.Add(topPanel);
        configureTop(topPanel);

        layout.Controls.Add(mainControl);

        topPanel.SizeChanged += (_, _) => ResizeSingleColumnTableChildren(topPanel);
        ResizeSingleColumnTableChildren(topPanel);

        static void ResizeSingleColumnTableChildren(TableLayoutPanel topPanel)
        {
            topPanel.SuspendLayout();

            int childWidth = topPanel.ClientSize.Width;
            childWidth -= topPanel.Margin.Horizontal;

            foreach (var control in topPanel.Controls)
            {
                var c = (Control) control;
                c.Width = childWidth;
            }
            topPanel.ResumeLayout();
        }

        return layout;
    }

}
