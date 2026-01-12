using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PersonalBudgetingExpenseTracker
{
    public class TransactionsForm : Form
    {
        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        private readonly ComboBox cboMonth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        private readonly NumericUpDown nudYear = new NumericUpDown { Minimum = 2000, Maximum = 2100, Width = 90 };
        private readonly Button btnLoad = new Button { Text = "Load", Width = 80 };

        private readonly DateTimePicker dtpDate = new DateTimePicker { Width = 170 };
        private readonly TextBox txtAmount = new TextBox { Width = 120 };
        private readonly ComboBox cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly TextBox txtCategory = new TextBox { Width = 160 };
        private readonly TextBox txtDesc = new TextBox { Width = 240 };

        private readonly Button btnAdd = new Button { Text = "Add", Width = 90 };
        private readonly Button btnUpdate = new Button { Text = "Update", Width = 90 };
        private readonly Button btnDelete = new Button { Text = "Delete", Width = 90 };

        private readonly ErrorProvider error = new ErrorProvider();

        public TransactionsForm()
        {
            Text = "Transactions";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 600);

            cboType.Items.AddRange(new object[] { "Income", "Expense" });
            cboType.SelectedIndex = 0;

            for (int m = 1; m <= 12; m++)
                cboMonth.Items.Add(new DateTime(2000, m, 1).ToString("MMMM"));

            var now = DateTime.Now;
            cboMonth.SelectedIndex = now.Month - 1;
            nudYear.Value = now.Year;

            var top = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(12) };

            var filter = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            filter.Controls.AddRange(new Control[] {
                new Label{Text="Month:", AutoSize=true, Padding=new Padding(0,7,0,0)}, cboMonth,
                new Label{Text="Year:", AutoSize=true, Padding=new Padding(0,7,0,0)}, nudYear,
                btnLoad
            });

            var editor = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, WrapContents = false };
            editor.Controls.AddRange(new Control[] {
                new Label{Text="Date *", AutoSize=true, Padding=new Padding(0,7,0,0)}, dtpDate,
                new Label{Text="Amount *", AutoSize=true, Padding=new Padding(0,7,0,0)}, txtAmount,
                new Label{Text="Type *", AutoSize=true, Padding=new Padding(0,7,0,0)}, cboType,
                new Label{Text="Category *", AutoSize=true, Padding=new Padding(0,7,0,0)}, txtCategory,
                new Label{Text="Description", AutoSize=true, Padding=new Padding(0,7,0,0)}, txtDesc,
                btnAdd, btnUpdate, btnDelete
            });

            top.Controls.Add(filter);
            top.Controls.Add(editor);

            Controls.Add(grid);
            Controls.Add(top);

            btnLoad.Click += (s, e) => LoadGrid();
            btnAdd.Click += (s, e) => AddTransaction();
            btnUpdate.Click += (s, e) => UpdateTransaction();
            btnDelete.Click += (s, e) => DeleteTransaction();

            grid.SelectionChanged += (s, e) => PopulateFromSelection();

            txtAmount.TextChanged += (s, e) => ValidateInputs(false);
            txtCategory.TextChanged += (s, e) => ValidateInputs(false);

            LoadGrid();
        }

        private (int month, int year) SelectedPeriod() => (cboMonth.SelectedIndex + 1, (int)nudYear.Value);

        private void LoadGrid()
        {
            var p = SelectedPeriod();
            DataTable dt = Db.GetTransactions(p.month, p.year);
            grid.DataSource = dt;

            if (grid.Columns.Count > 0)
            {
                grid.Columns["TransactionID"].Width = 90;
                grid.Columns["Date"].Width = 110;
                grid.Columns["Amount"].Width = 90;
                grid.Columns["Type"].Width = 90;
                grid.Columns["Category"].Width = 120;
                grid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void PopulateFromSelection()
        {
            if (grid.CurrentRow == null) return;
            var drv = grid.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            dtpDate.Value = DateTime.Parse(drv["Date"].ToString());
            txtAmount.Text = Convert.ToDouble(drv["Amount"]).ToString("0.##");
            cboType.SelectedItem = drv["Type"].ToString();
            txtCategory.Text = drv["Category"].ToString();
            txtDesc.Text = drv["Description"] == DBNull.Value ? "" : drv["Description"].ToString();
        }

        private bool ValidateInputs(bool showErrors)
        {
            bool okAmount = double.TryParse(txtAmount.Text, out double amt) && amt > 0;
            bool okCat = !string.IsNullOrWhiteSpace(txtCategory.Text);

            ValidationUi.MarkRequired(txtAmount, okAmount);
            ValidationUi.MarkRequired(txtCategory, okCat);

            if (showErrors)
            {
                error.SetError(txtAmount, okAmount ? "" : "Amount must be numeric and > 0.");
                error.SetError(txtCategory, okCat ? "" : "Category is required.");
            }

            return okAmount && okCat;
        }

        private void AddTransaction()
        {
            if (!ValidateInputs(true)) return;

            double amt = double.Parse(txtAmount.Text);
            Db.AddTransaction(dtpDate.Value.Date, amt, cboType.SelectedItem.ToString(), txtCategory.Text.Trim(), txtDesc.Text);
            LoadGrid();
        }

        private void UpdateTransaction()
        {
            if (grid.CurrentRow == null) return;
            var drv = grid.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;
            if (!ValidateInputs(true)) return;

            long id = Convert.ToInt64(drv["TransactionID"]);
            double amt = double.Parse(txtAmount.Text);

            Db.UpdateTransaction(id, dtpDate.Value.Date, amt, cboType.SelectedItem.ToString(), txtCategory.Text.Trim(), txtDesc.Text);
            LoadGrid();
        }

        private void DeleteTransaction()
        {
            if (grid.CurrentRow == null) return;
            var drv = grid.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            long id = Convert.ToInt64(drv["TransactionID"]);
            var res = MessageBox.Show($"Delete transaction #{id}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            Db.DeleteTransaction(id);
            LoadGrid();
        }
    }
}
