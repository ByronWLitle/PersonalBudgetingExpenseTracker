using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;

namespace PersonalBudgetingExpenseTracker
{
    internal static class Db
    {
        private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DbPath = Path.Combine(AppDir, "budget.db");
        private static readonly string ConnStr = "Data Source=" + DbPath + ";Version=3;";

        public static void Initialize()
        {
            Directory.CreateDirectory(AppDir);

            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
  UserId INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  PasswordHash TEXT NOT NULL,
  PasswordSalt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Transactions (
  TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
  Amount REAL NOT NULL,
  Date TEXT NOT NULL,
  Type TEXT NOT NULL CHECK(Type IN ('Income','Expense')),
  Category TEXT NOT NULL,
  Description TEXT NULL
);

CREATE TABLE IF NOT EXISTS Budgets (
  BudgetID INTEGER PRIMARY KEY AUTOINCREMENT,
  Amount REAL NOT NULL,
  Month INTEGER NOT NULL,
  Year INTEGER NOT NULL,
  UNIQUE(Month, Year)
);
";
                    cmd.ExecuteNonQuery();
                }

                // Seed default user if none exist
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users;", conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                        CreateUser(conn, "admin", "admin123");
                }
            }
        }

        public static bool ValidateLogin(string username, string password)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand("SELECT PasswordHash, PasswordSalt FROM Users WHERE Username=@u;", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return false;

                        string hash = r.GetString(0);
                        string salt = r.GetString(1);

                        string candidate = HashPassword(password, salt);
                        return FixedTimeEquals(hash, candidate);
                    }
                }
            }
        }

        private static void CreateUser(SQLiteConnection openConn, string username, string password)
        {
            string salt = CreateSalt();
            string hash = HashPassword(password, salt);

            using (var cmd = new SQLiteCommand("INSERT INTO Users(Username, PasswordHash, PasswordSalt) VALUES(@u,@h,@s);", openConn))
            {
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@s", salt);
                cmd.ExecuteNonQuery();
            }
        }

        public static (double income, double expenses) GetMonthTotals(int month, int year)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();

                string start = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
                string end = new DateTime(year, month, 1).AddMonths(1).ToString("yyyy-MM-dd");

                using (var cmd = new SQLiteCommand(@"
SELECT
  COALESCE(SUM(CASE WHEN Type='Income' THEN Amount ELSE 0 END),0) AS IncomeTotal,
  COALESCE(SUM(CASE WHEN Type='Expense' THEN Amount ELSE 0 END),0) AS ExpenseTotal
FROM Transactions
WHERE Date >= @start AND Date < @end;", conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return (0, 0);
                        return (Convert.ToDouble(r["IncomeTotal"]), Convert.ToDouble(r["ExpenseTotal"]));
                    }
                }
            }
        }

        public static double? GetBudgetForMonth(int month, int year)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Amount FROM Budgets WHERE Month=@m AND Year=@y;", conn))
                {
                    cmd.Parameters.AddWithValue("@m", month);
                    cmd.Parameters.AddWithValue("@y", year);
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value) return null;
                    return Convert.ToDouble(val);
                }
            }
        }

        public static DataTable GetTransactions(int month, int year)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();

                string start = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
                string end = new DateTime(year, month, 1).AddMonths(1).ToString("yyyy-MM-dd");

                using (var cmd = new SQLiteCommand(@"
SELECT TransactionID, Date, Amount, Type, Category, Description
FROM Transactions
WHERE Date >= @start AND Date < @end
ORDER BY Date DESC, TransactionID DESC;", conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static void AddTransaction(DateTime date, double amount, string type, string category, string descriptionOrNull)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
INSERT INTO Transactions(Date, Amount, Type, Category, Description)
VALUES(@d,@a,@t,@c,@desc);", conn))
                {
                    cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@a", amount);
                    cmd.Parameters.AddWithValue("@t", type);
                    cmd.Parameters.AddWithValue("@c", category);
                    if (string.IsNullOrWhiteSpace(descriptionOrNull))
                        cmd.Parameters.AddWithValue("@desc", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@desc", descriptionOrNull.Trim());

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateTransaction(long id, DateTime date, double amount, string type, string category, string descriptionOrNull)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
UPDATE Transactions
SET Date=@d, Amount=@a, Type=@t, Category=@c, Description=@desc
WHERE TransactionID=@id;", conn))
                {
                    cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@a", amount);
                    cmd.Parameters.AddWithValue("@t", type);
                    cmd.Parameters.AddWithValue("@c", category);
                    if (string.IsNullOrWhiteSpace(descriptionOrNull))
                        cmd.Parameters.AddWithValue("@desc", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@desc", descriptionOrNull.Trim());
                    cmd.Parameters.AddWithValue("@id", id);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteTransaction(long id)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Transactions WHERE TransactionID=@id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable GetBudgets()
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
SELECT BudgetID, Month, Year, Amount
FROM Budgets
ORDER BY Year DESC, Month DESC, BudgetID DESC;", conn))
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static void UpsertBudget(int month, int year, double amount)
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
INSERT INTO Budgets(Month, Year, Amount)
VALUES(@m,@y,@a)
ON CONFLICT(Month,Year) DO UPDATE SET Amount=excluded.Amount;", conn))
                {
                    cmd.Parameters.AddWithValue("@m", month);
                    cmd.Parameters.AddWithValue("@y", year);
                    cmd.Parameters.AddWithValue("@a", amount);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static (double spent, double budgeted) GetPerformanceFor(int month, int year)
        {
            var totals = GetMonthTotals(month, year);
            var budget = GetBudgetForMonth(month, year) ?? 0;
            return (totals.expenses, budget);
        }

        private static string CreateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        private static string HashPassword(string password, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }
    }
}
