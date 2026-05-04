// MigrationHelper.cs
// One-off helper to migrate existing plain-text passwords into secure hashes.
// Usage:
// 1) Set environment variable MIGRATION_CONNECTION_STRING to your DB connection string,
//    or run the tool and paste the connection string when prompted.
// 2) Run the tool (dotnet run within a console app project that includes this file,
//    or compile it as a small console app). It will by default perform a dry-run.
// 3) Follow prompts to ALTER the Users.PassWord column (if needed) and to apply updates.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace RideHailingApi.Tools
{
    public static class MigrationHelper
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("MigrationHelper - migrate plain passwords to salted hashes (bcrypt if available, otherwise PBKDF2 fallback)");
            string? cs = Environment.GetEnvironmentVariable("MIGRATION_CONNECTION_STRING");
            if (string.IsNullOrEmpty(cs))
            {
                Console.Write("Enter connection string: ");
                cs = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(cs))
            {
                Console.WriteLine("No connection string provided. Exiting.");
                return 1;
            }

            using var cn = new SqlConnection(cs);
            await cn.OpenAsync();

            // Optional: check column length and offer ALTER
            Console.WriteLine("Checking Users.PassWord column definition...");
            var colCmd = new SqlCommand(@"
SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PassWord'", cn);
            using var r = await colCmd.ExecuteReaderAsync();
            bool needsAlter = false;
            if (await r.ReadAsync())
            {
                var dtype = r.GetString(0);
                var maxlen = r.IsDBNull(1) ? (int?)null : r.GetInt32(1);
                Console.WriteLine($"PassWord column: {dtype}({(maxlen.HasValue ? maxlen.Value.ToString() : "max")})");
                if (!maxlen.HasValue || maxlen.Value < 200)
                {
                    Console.WriteLine("Recommended: enlarge PassWord column to NVARCHAR(200) to store bcrypt or PBKDF2 hashes.");
                    Console.Write("Do you want to ALTER the column to NVARCHAR(200)? (y/N): ");
                    var alt = Console.ReadLine();
                    if (!string.IsNullOrEmpty(alt) && alt.Trim().ToLowerInvariant().StartsWith("y"))
                    {
                        needsAlter = true;
                    }
                }
            }
            else
            {
                Console.WriteLine("Could not find Users.PassWord column. Make sure table exists and you have proper permissions.");
                return 1;
            }

            if (needsAlter)
            {
                Console.WriteLine("Altering column PassWord to NVARCHAR(200)...");
                var alterCmd = new SqlCommand("ALTER TABLE Users ALTER COLUMN PassWord NVARCHAR(200)", cn);
                try
                {
                    await alterCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Column altered successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to alter column: {ex.Message}");
                    Console.WriteLine("You can run the ALTER manually and re-run this tool.");
                }
            }

            // Collect users
            Console.WriteLine("Fetching users...");
            var selectCmd = new SqlCommand("SELECT UserID, PassWord FROM Users", cn);
            var users = new List<(int Id, string Password)>();
            using (var rdr = await selectCmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync())
                {
                    int id = rdr.GetInt32(0);
                    string pw = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                    users.Add((id, pw));
                }
            }

            Console.WriteLine($"Found {users.Count} users. Scanning for non-hashed passwords...");
            var toUpdate = new List<(int Id, string NewHash)>();

            var bcryptType = TryGetBcryptType();
            bool useBcrypt = bcryptType != null;
            Console.WriteLine(useBcrypt ? "BCrypt.Net detected — will use bcrypt for hashing." : "BCrypt.Net not found — will use PBKDF2 fallback hashing.");

            foreach (var (id, pw) in users)
            {
                if (string.IsNullOrEmpty(pw)) continue;
                if (IsLikelyHashed(pw)) continue;

                string newHash;
                if (useBcrypt)
                {
                    newHash = InvokeBcryptHash(pw, bcryptType!);
                }
                else
                {
                    newHash = PBKDF2_Hash(pw);
                }

                toUpdate.Add((id, newHash));
            }

            Console.WriteLine($"{toUpdate.Count} users to be updated.");
            if (toUpdate.Count == 0)
            {
                Console.WriteLine("Nothing to do. Exiting.");
                return 0;
            }

            Console.Write("Perform update? (this will overwrite PassWord values) (y/N): ");
            var confirm = Console.ReadLine();
            if (string.IsNullOrEmpty(confirm) || !confirm.Trim().ToLowerInvariant().StartsWith("y"))
            {
                Console.WriteLine("Aborted by user.");
                return 0;
            }

            Console.WriteLine("Applying updates...");
            foreach (var (id, newHash) in toUpdate)
            {
                var up = new SqlCommand("UPDATE Users SET PassWord = @pw WHERE UserID = @id", cn);
                up.Parameters.AddWithValue("@pw", newHash);
                up.Parameters.AddWithValue("@id", id);
                try
                {
                    int rows = await up.ExecuteNonQueryAsync();
                    Console.WriteLine(rows == 1 ? $"User {id} updated" : $"User {id} update affected {rows} rows");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to update user {id}: {ex.Message}");
                }
            }

            Console.WriteLine("Done.");
            return 0;
        }

        private static bool IsLikelyHashed(string pw)
        {
            if (string.IsNullOrEmpty(pw)) return false;
            pw = pw.Trim();
            if (pw.StartsWith("$2a$") || pw.StartsWith("$2b$") || pw.StartsWith("$2y$")) return true; // bcrypt
            if (pw.StartsWith("PBKDF2$")) return true; // our fallback format
            // if length looks like a hash
            if (pw.Length > 50) return true;
            return false;
        }

        private static Type? TryGetBcryptType()
        {
            try
            {
                // Try to find BCrypt.Net.BCrypt type in loaded assemblies
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("BCrypt.Net.BCrypt");
                    if (t != null) return t;
                }

                // Try to load assembly by name
                var name = "BCrypt.Net-Next";
                try
                {
                    var asm = Assembly.Load(new AssemblyName("BCrypt.Net-Next"));
                    var t = asm.GetType("BCrypt.Net.BCrypt");
                    if (t != null) return t;
                }
                catch { }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string InvokeBcryptHash(string password, Type bcryptType)
        {
            try
            {
                var method = bcryptType.GetMethod("HashPassword", new[] { typeof(string), typeof(int) })
                             ?? bcryptType.GetMethod("HashPassword", new[] { typeof(string) });
                if (method == null)
                    throw new InvalidOperationException("BCrypt.HashPassword method not found via reflection.");

                object? hashObj;
                if (method.GetParameters().Length == 2)
                {
                    hashObj = method.Invoke(null, new object[] { password, 12 });
                }
                else
                {
                    hashObj = method.Invoke(null, new object[] { password });
                }

                return hashObj?.ToString() ?? PBKDF2_Hash(password);
            }
            catch
            {
                return PBKDF2_Hash(password);
            }
        }

        // PBKDF2 hashing format: PBKDF2$iterations$saltBase64$hashBase64
        private static string PBKDF2_Hash(string password)
        {
            const int iterations = 100_000;
            const int saltSize = 16;
            const int hashSize = 32;
            byte[] salt = new byte[saltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSize);

            string saltB = Convert.ToBase64String(salt);
            string hashB = Convert.ToBase64String(hash);
            return $"PBKDF2${iterations}${saltB}${hashB}";
        }
    }
}
