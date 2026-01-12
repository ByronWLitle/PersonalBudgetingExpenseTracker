using System;
using System.Windows.Forms;

namespace PersonalBudgetingExpenseTracker
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Db.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
