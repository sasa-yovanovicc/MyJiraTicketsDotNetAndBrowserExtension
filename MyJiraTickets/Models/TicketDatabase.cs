using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyJiraTickets.Models
{
    public static class TicketDatabase
    {
        // Load database path from settings, fallback to default location
        private static readonly string DatabasePath = GetDatabasePath();
        
        private static string GetDatabasePath()
        {
            try
            {
                var settings = AppSettings.Load("appsettings.json");
                var path = settings.DatabasePath;
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                return path;
            }
            catch
            {
                // Fallback to default location
                return @"D:\MyApps\sqlitedb\tickets.db";
            }
        }
        
        static TicketDatabase()
        {
            InitializeDatabase();
        }
        
        private static void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tickets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL,
                    URL TEXT NOT NULL,
                    Summary TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Type TEXT DEFAULT 'Task',
                    Priority TEXT DEFAULT 'Medium',
                    CreatedDate TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
            
            // Add missing columns if they don't exist
            try
            {
                var alterCommand1 = connection.CreateCommand();
                alterCommand1.CommandText = "ALTER TABLE Tickets ADD COLUMN Type TEXT DEFAULT 'Task'";
                alterCommand1.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists, ignore
            }
            
            try
            {
                var alterCommand2 = connection.CreateCommand();
                alterCommand2.CommandText = "ALTER TABLE Tickets ADD COLUMN Priority TEXT DEFAULT 'Medium'";
                alterCommand2.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists, ignore
            }
        }
        
        public static List<Ticket> LoadTickets()
        {
            var tickets = new List<Ticket>();
            
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Key, URL, Summary, Status, Type, Priority FROM Tickets ORDER BY Id DESC";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tickets.Add(new Ticket
                {
                    Key = reader.GetString(0),
                    Url = reader.GetString(1), 
                    Summary = reader.GetString(2),
                    Status = reader.GetString(3),
                    Type = reader.IsDBNull(4) ? "Task" : reader.GetString(4),
                    Priority = reader.IsDBNull(5) ? "Medium" : reader.GetString(5)
                });
            }
            
            return tickets;
        }
        
        public static void SaveTickets(List<Ticket> tickets)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            // Clear existing tickets
            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM Tickets";
            deleteCommand.ExecuteNonQuery();
            
            // Insert all tickets
            foreach (var ticket in tickets)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Tickets (Key, URL, Summary, Status, Type, Priority, CreatedDate) 
                    VALUES (@key, @url, @summary, @status, @type, @priority, @date)";
                
                insertCommand.Parameters.AddWithValue("@key", ticket.Key);
                insertCommand.Parameters.AddWithValue("@url", ticket.Url);
                insertCommand.Parameters.AddWithValue("@summary", ticket.Summary);
                insertCommand.Parameters.AddWithValue("@status", ticket.Status);
                insertCommand.Parameters.AddWithValue("@type", ticket.Type);
                insertCommand.Parameters.AddWithValue("@priority", ticket.Priority);
                insertCommand.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                insertCommand.ExecuteNonQuery();
            }
        }
        
        public static void AddTicket(Ticket ticket)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tickets (Key, URL, Summary, Status, Type, Priority, CreatedDate) 
                VALUES (@key, @url, @summary, @status, @type, @priority, @date)";
            
            command.Parameters.AddWithValue("@key", ticket.Key);
            command.Parameters.AddWithValue("@url", ticket.Url);
            command.Parameters.AddWithValue("@summary", ticket.Summary);
            command.Parameters.AddWithValue("@status", ticket.Status);
            command.Parameters.AddWithValue("@type", ticket.Type);
            command.Parameters.AddWithValue("@priority", ticket.Priority);
            command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            command.ExecuteNonQuery();
        }
        
        public static void UpdateTicketStatus(string key, string newStatus)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Tickets SET Status = @status WHERE Key = @key";
            
            command.Parameters.AddWithValue("@status", newStatus);
            command.Parameters.AddWithValue("@key", key);
            
            command.ExecuteNonQuery();
        }
        
        public static void UpdateTicket(Ticket ticket)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Tickets 
                SET URL = @url, Summary = @summary, Status = @status, Type = @type, Priority = @priority 
                WHERE Key = @key";
            
            command.Parameters.AddWithValue("@key", ticket.Key);
            command.Parameters.AddWithValue("@url", ticket.Url);
            command.Parameters.AddWithValue("@summary", ticket.Summary);
            command.Parameters.AddWithValue("@status", ticket.Status);
            command.Parameters.AddWithValue("@type", ticket.Type);
            command.Parameters.AddWithValue("@priority", ticket.Priority);
            
            command.ExecuteNonQuery();
        }
        
        public static void DeleteTicket(string key)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Tickets WHERE Key = @key";
            command.Parameters.AddWithValue("@key", key);
            
            command.ExecuteNonQuery();
        }
    }
}
