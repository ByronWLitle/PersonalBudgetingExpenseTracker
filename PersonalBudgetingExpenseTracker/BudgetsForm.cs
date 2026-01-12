using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PersonalBudgetingExpenseTracker
{
    public class BudgetsForm : Form
    {
        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        private readonly ComboBox cboMonth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        private readonly NumericUpDown nudYear = new NumericUpDown { Minimum = 2000, Maximum = 2100, Width = 90 };
        private readonly TextBox txtAmount = new TextBox { Width = 120 };
        private readonly Button btnSave = new Button { Text = "Add / Update", Width = 120 };
        private readonly Label lblPerf = new Label { AutoSize = true };
        private readonly ErrorProvider error = new ErrorProvider();

        public BudgetsForm()
        {
            Text = "Budgets";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 600);

            for (int m = 1; m <= 12; m++)
                cboMonth.Items.Add(new DateTime(2000, m, 1).ToString("MMMM"));

            var now = DateTime.Now;
            cboMonth.SelectedIndex = now.Month - 1;
            nudYear.Value = now.Year;

            var top = new Panel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(12) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            row.Controls.AddRange(new Control[] {
                new Label{Text="Month *", AutoSize=true, Padding=new Padding(0,7,0,0)}, cboMonth,
                new Label{Text="Year *", AutoSize=true, Padding=new Padding(0,7,0,0)}, nudYear,
                new Label{Text="Amount *", AutoSize=true, Padding=new Padding(0,7,0,0)}, txtAmount,
                btnSave
            });

            lblPerf.Location = new Point(12, 52);
            lblPerf.ForeColor = Color.DimGray;

            top.Controls.Add(row);
            top.Controls.Add(lblPerf);

            Controls.Add(grid);
            Controls.Add(top);

            btnSave.Click += (s, e) => SaveBudget();
            grid.SelectionChanged += (s, e) => PopulateFromSelection();

            txtAmount.TextChanged += (s, e) => ValidateInputs(false);
            cboMonth.SelectedIndexChanged += (s, e) => UpdatePerformanceLabel();
            nudYear.ValueChanged += (s, e) => UpdatePerformanceLabel();

            LoadGrid();
            UpdatePerformanceLabel();
        }

        private (int month, int year) SelectedPeriod() => (cboMonth.SelectedIndex + 1, (int)nudYear.Value);

        private void LoadGrid()
        {
            DataTable dt = Db.GetBudgets();
            grid.DataSource = dt;

            if (grid.Columns.Count > 0)
            {
                grid.Columns["BudgetID"].Width = 90;
                grid.Columns["Month"].Width = 80;
                grid.Columns["Year"].Width = 80;
                grid.Columns["Amount"].Width = 120;
                grid.Columns["Amount"].DefaultCellStyle.Format = "C";
            }
        }

        private void PopulateFromSelection()
        {
            if (grid.CurrentRow == null) return;
            var drv = grid.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int month = Convert.ToInt32(drv["Month"]);
            int year = Convert.ToInt32(drv["Year"]);
            double amt = Convert.ToDouble(drv["Amount"]);

            cboMonth.SelectedIndex = month - 1;
            nudYear.Value = year;
            txtAmount.Text = amt.ToString("0.##");

            UpdatePerformanceLabel();
        }

        private bool ValidateInputs(bool showErrors)
        {
            bool okAmount = double.TryParse(txtAmount.Text, out double amt) && amt > 0;
            ValidationUi.MarkRequired(txtAmount, okAmount);

            if (showErrors)
                error.SetError(txtAmount, okAmount ? "" : "Budget amount must be numeric and > 0.");

            return okAmount;
        }

        private void SaveBudget()
        {
            if (!ValidateInputs(true)) return;

            var p = SelectedPeriod();
            double amt = double.Parse(txtAmount.Text);

            Db.UpsertBudget(p.month, p.year, amt);
            LoadGrid();
            UpdatePerformanceLabel();
        }

        private void UpdatePerformanceLabel()
        {
            var p = SelectedPeriod();
            var perf = Db.GetPerformanceFor(p.month, p.year);
            double spent = perf.spent;
            double budgeted = perf.budgeted;
            double diff = budgeted - spent;

            lblPerf.Text = $"Performance for {new DateTime(p.year, p.month, 1):MMMM yyyy}: Spent {spent:C} vs Budgeted {budgeted:C} (Difference: {diff:C})";
        }
    }
}
