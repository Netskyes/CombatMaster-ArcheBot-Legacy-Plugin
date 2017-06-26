using System;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace CombatMaster
{
    using Data;

    public class SQLiteClient
    {
        private SQLiteConnection conn;

        public SQLiteClient(string dbName)
        {
            string path = Path.Combine(Paths.Plugin, dbName);
            bool isTableRequired = false;


            if (!File.Exists(path))
            {
                SQLiteConnection.CreateFile(path);
                isTableRequired = true;
            }


            conn = new SQLiteConnection(string.Format("Data Source={0};Version=3", path));
            conn.Open();

            
            if (isTableRequired)
            {
                string query1 = "CREATE TABLE prices (id INTEGER PRIMARY KEY AUTOINCREMENT, item_name VARCHAR NOT NULL UNIQUE, item_price INTEGER NOT NULL);",
                       query2 = "CREATE TABLE history (id INTEGER PRIMARY KEY AUTOINCREMENT, item_name VARCHAR NOT NULL, item_price INTEGER NOT NULL, date_time DATETIME NOT NULL);";

                ExecuteCommand(Command(query1));
                ExecuteCommand(Command(query2));
            }
        }

        public SQLiteCommand Command(string query, SQLiteParameter[] queryParams = null)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = query;

            if (queryParams != null)
            {
                command.Parameters.AddRange(queryParams);
            }
            
            return command;
        }

        public int ExecuteCommand(SQLiteCommand command)
        {
            return command.ExecuteNonQuery();
        }

        public SQLiteDataReader ExecuteReader(SQLiteCommand command)
        {
            return command.ExecuteReader();
        }

        public int FetchNumRows(SQLiteCommand command)
        {
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
