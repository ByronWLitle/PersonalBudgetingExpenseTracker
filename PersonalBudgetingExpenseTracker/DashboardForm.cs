using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PersonalBudgetingExpenseTracker
{
    public class DashboardForm : Form
    {
        private readonly string _username;

        private readonly Label lblIncome = new Label { AutoSize = true };
        private readonly Label lblExpenses = new Label { AutoSize = true };
        private readonly Label lblBudget = new Label { AutoSize = true };
        private readonly Label lblNet = new Label { AutoSize = true };

        private readonly Chart chart = new Chart { Dock = DockStyle.Fill };

        private readonly Button btnTransactions = new Button { Text = "Transactions", Height = 34, Width = 120 };
        private readonly Button btnBudgets = new Button { Text = "Budgets", Height = 34, Width = 120 };
        private readonly Button btnRefresh = new Button { Text = "Refresh", Height = 34, Width = 120 };
        private readonly Button btnLogout = new Button { Text = "Logout", Height = 34, Width = 120 };

        public DashboardForm(string username)
        {
            _username = username;

            Text = "Personal Budgeting - Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 550);

            var top = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(12) };
            var title = new Label
            {
                Text = $"Dashboard (Current Month) — {_username}",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 8)
            };

            var stats = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            lblIncome.Font = new Font(Font.FontFamily, 10, FontStyle.Regular);
            lblExpenses.Font = new Font(Font.FontFamily, 10, FontStyle.Regular);
            lblBudget.Font = new Font(Font.FontFamily, 10, FontStyle.Regular);
            lblNet.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);

            stats.Controls.AddRange(new Control[] { lblIncome, Spacer(), lblBudget, Spacer(), lblExpenses, Spacer(), lblNet });

            top.Controls.Add(title);
            top.Controls.Add(stats);

            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 160,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(12),
                WrapContents = false
            };
            right.Controls.AddRange(new Control[] { btnTransactions, btnBudgets, btnRefresh, btnLogout });

            var chartHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            chartHost.Controls.Add(chart);

            Controls.Add(chartHost);
            Controls.Add(right);
            Controls.Add(top);

            btnTransactions.Click += (s, e) =>
            {
                using (var f = new TransactionsForm())
                {
                    f.ShowDialog();
                }
                RefreshDashboard();
            };

            btnBudgets.Click += (s, e) =>
            {
                using (var f = new BudgetsForm())
                {
                    f.ShowDialog();
                }
                RefreshDashboard();
            };

            btnRefresh.Click += (s, e) => RefreshDashboard();
            btnLogout.Click += (s, e) => Close();

            BuildChart();
            RefreshDashboard();
        }

        private static Control Spacer() => new Label { Text = "   |   ", AutoSize = true, ForeColor = Color.Gray };

        private void BuildChart()
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Legends.Clear();

            var area = new ChartArea("main");
            area.AxisX.Interval = 1;
            area.AxisY.Title = "Amount";
            chart.ChartAreas.Add(area);

            var series = new Series("Income vs Expenses");
            series.ChartType = SeriesChartType.Column;
            series.Points.AddXY("Income", 0);
            series.Points.AddXY("Expenses", 0);
            chart.Series.Add(series);

            chart.Titles.Clear();
            chart.Titles.Add("Current Month Summary");
        }

        private void RefreshDashboard()
        {
            var now = DateTime.Now;
            var totals = Db.GetMonthTotals(now.Month, now.Year);
            var budget = Db.GetBudgetForMonth(now.Month, now.Year);

            double income = totals.income;
            double expenses = totals.expenses;

            lblIncome.Text = $"Income: {income:C}";
            lblExpenses.Text = $"Expenses: {expenses:C}";
            lblBudget.Text = $"Budget Goal: {(budget.HasValue ? budget.Value.ToString("C") : "Not set")}";
            lblNet.Text = $"Net: {(income - expenses):C}";

            var s = chart.Series[0];
            s.Points[0].YValues[0] = income;
            s.Points[1].YValues[0] = expenses;
        }
    }
}
