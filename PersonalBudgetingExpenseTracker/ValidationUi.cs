using System.Drawing;
using System.Windows.Forms;

namespace PersonalBudgetingExpenseTracker
{
    internal static class ValidationUi
    {
        public static void MarkRequired(Control c, bool ok)
        {
            c.BackColor = ok ? SystemColors.Window : Color.MistyRose;
        }
    }
}
