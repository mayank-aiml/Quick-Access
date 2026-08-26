using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using QuickAccessHub.Models;

namespace QuickAccessHub.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuickAccessHub");

            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            _dbPath = Path.Combine(appDataDir, "quickaccess.db");
            _connectionString = $"Data Source={_dbPath}";

            InitializeDatabase();
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        private void InitializeDatabase()
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT UNIQUE NOT NULL,
                    DisplayOrder INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Items (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Path TEXT,
                    Url TEXT,
                    CategoryId INTEGER,
                    CreatedAt TEXT,
                    UpdatedAt TEXT,
                    FOREIGN KEY(CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
            ";
            cmd.ExecuteNonQuery();

            // Seed default categories if empty
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Categories;";
            long count = (long)(countCmd.ExecuteScalar() ?? 0);

            if (count == 0)
            {
                string[] defaultCategories = { "General", "Projects", "College", "Work", "Websites", "Other" };
                for (int i = 0; i < defaultCategories.Length; i++)
                {
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO Categories (Name, DisplayOrder) VALUES (@name, @order);";
                    insertCmd.Parameters.AddWithValue("@name", defaultCategories[i]);
                    insertCmd.Parameters.AddWithValue("@order", i + 1);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public List<Category> GetCategories()
        {
            var list = new List<Category>();
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, DisplayOrder FROM Categories ORDER BY DisplayOrder ASC, Name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Category
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    DisplayOrder = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                });
            }

            return list;
        }

        public long AddCategory(string name)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories (Name, DisplayOrder) VALUES (@name, 99); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", name);
            return (long)(cmd.ExecuteScalar() ?? 0);
        }

        public bool DeleteCategory(long id)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Categories WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<QuickItem> GetItems(string? searchQuery = null, long? categoryId = null)
        {
            var list = new List<QuickItem>();
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            var sql = @"
                SELECT i.Id, i.Name, i.Type, i.Path, i.Url, i.CategoryId, COALESCE(c.Name, 'General') as CategoryName, i.CreatedAt, i.UpdatedAt
                FROM Items i
                LEFT JOIN Categories c ON i.CategoryId = c.Id
                WHERE 1=1
            ";

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                sql += " AND i.CategoryId = @categoryId";
                cmd.Parameters.AddWithValue("@categoryId", categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                sql += " AND (i.Name LIKE @query OR i.Path LIKE @query OR i.Url LIKE @query)";
                cmd.Parameters.AddWithValue("@query", $"%{searchQuery.Trim()}%");
            }

            sql += " ORDER BY i.CreatedAt DESC;";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = new QuickItem
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    Path = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CategoryId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
                    CategoryName = reader.GetString(6),
                    CreatedAt = DateTime.TryParse(reader.IsDBNull(7) ? null : reader.GetString(7), out var ca) ? ca : DateTime.Now,
                    UpdatedAt = DateTime.TryParse(reader.IsDBNull(8) ? null : reader.GetString(8), out var ua) ? ua : DateTime.Now
                };

                // Check missing status on disk
                if (item.Type == "File" && !string.IsNullOrEmpty(item.Path))
                {
                    item.IsMissing = !File.Exists(item.Path);
                }
                else if (item.Type == "Folder" && !string.IsNullOrEmpty(item.Path))
                {
                    item.IsMissing = !Directory.Exists(item.Path);
                }

                list.Add(item);
            }

            return list;
        }

        public long AddItem(QuickItem item)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Items (Name, Type, Path, Url, CategoryId, CreatedAt, UpdatedAt)
                VALUES (@name, @type, @path, @url, @categoryId, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@type", item.Type);
            cmd.Parameters.AddWithValue("@path", (object?)item.Path ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@url", (object?)item.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@categoryId", (object?)item.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("o"));

            return (long)(cmd.ExecuteScalar() ?? 0);
        }

        public bool UpdateItem(QuickItem item)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Items
                SET Name = @name, Type = @type, Path = @path, Url = @url, CategoryId = @categoryId, UpdatedAt = @updatedAt
                WHERE Id = @id;
            ";

            cmd.Parameters.AddWithValue("@id", item.Id);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@type", item.Type);
            cmd.Parameters.AddWithValue("@path", (object?)item.Path ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@url", (object?)item.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@categoryId", (object?)item.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("o"));

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteItem(long id)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Items WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public string? GetSetting(string key, string? defaultValue = null)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? result.ToString() : defaultValue;
        }

        public void SaveSetting(string key, string value)
        {
            using var connection = GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Settings (Key, Value) VALUES (@key, @value)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            ";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }
    }
}
