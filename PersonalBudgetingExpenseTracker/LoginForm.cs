using System;
using System.Drawing;
using System.Windows.Forms;

namespace PersonalBudgetingExpenseTracker
{
    public class LoginForm : Form
    {
        private readonly TextBox txtUser = new TextBox { Width = 220 };
        private readonly TextBox txtPass = new TextBox { Width = 220, UseSystemPasswordChar = true };
        private readonly Button btnLogin = new Button { Text = "Login", Width = 220, Height = 32 };
        private readonly Label lblStatus = new Label { AutoSize = true };
        private readonly ErrorProvider error = new ErrorProvider();

        public LoginForm()
        {
            Text = "Personal Budgeting - Login";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(360, 250);

            var title = new Label
            {
                Text = "Login",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            var lblU = new Label { Text = "Username *", AutoSize = true, Location = new Point(20, 60) };
            txtUser.Location = new Point(20, 80);

            var lblP = new Label { Text = "Password *", AutoSize = true, Location = new Point(20, 115) };
            txtPass.Location = new Point(20, 135);

            btnLogin.Location = new Point(20, 175);

            lblStatus.Location = new Point(20, 215);
            lblStatus.ForeColor = Color.Firebrick;

            var hint = new Label
            {
                Text = "Default login: admin / admin123",
                AutoSize = true,
                Location = new Point(20, 215),
                ForeColor = Color.DimGray
            };

            Controls.AddRange(new Control[] { title, lblU, txtUser, lblP, txtPass, btnLogin, lblStatus, hint });

            btnLogin.Click += (s, e) => DoLogin(hint);
            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(hint); };
            AcceptButton = btnLogin;

            txtUser.TextChanged += (s, e) => ValidateInputs(false);
            txtPass.TextChanged += (s, e) => ValidateInputs(false);
        }

        private void DoLogin(Label hint)
        {
            hint.Visible = false;
            if (!ValidateInputs(true)) return;

            bool ok = Db.ValidateLogin(txtUser.Text.Trim(), txtPass.Text);
            if (!ok)
            {
                lblStatus.Text = "Invalid username or password.";
                lblStatus.ForeColor = Color.Firebrick;
                return;
            }

            Hide();
            using (var dash = new DashboardForm(txtUser.Text.Trim()))
            {
                dash.ShowDialog();
            }
            Close();
        }

        private bool ValidateInputs(bool showErrors)
        {
            bool okU = !string.IsNullOrWhiteSpace(txtUser.Text);
            bool okP = !string.IsNullOrWhiteSpace(txtPass.Text);

            ValidationUi.MarkRequired(txtUser, okU);
            ValidationUi.MarkRequired(txtPass, okP);

            if (showErrors)
            {
                error.SetError(txtUser, okU ? "" : "Username is required.");
                error.SetError(txtPass, okP ? "" : "Password is required.");
                lblStatus.Text = "";
            }

            return okU && okP;
        }
    }
}
