using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Shakepayrollsystem.Data
{
    public class DatabaseHelper
    {
        private string connectionString;
        public int rowAffected = 0;

        public DatabaseHelper(string server_address, string database, string username, string password)
        {
            // Removed SslMode=none and added AllowUserVariables for better compatibility
            connectionString = $"Server={server_address};Database={database};Uid={username};Pwd={password};Port=3306;AllowUserVariables=true;DefaultCommandTimeout=30;";
        }

        // Default constructor with your database settings
        public DatabaseHelper()
        {
            // Removed SslMode=none - not needed for local XAMPP
            connectionString = $"Server=localhost;Database=cs311-finalproj;Uid=louise;Pwd=legaspi;Port=3306;AllowUserVariables=true;DefaultCommandTimeout=30;";
        }

        // Get DataTable from SELECT query
        public DataTable GetData(string sql)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Parameterized SELECT query
        public DataTable GetData(string sql, Dictionary<string, object> parameters)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // Execute INSERT/UPDATE/DELETE
        public void ExecuteQuery(string sql)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    rowAffected = cmd.ExecuteNonQuery();
                }
            }
        }

        // Parameterized INSERT/UPDATE/DELETE
        public void ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    foreach (var p in parameters)
                        cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                    rowAffected = cmd.ExecuteNonQuery();
                }
            }
        }

        // Execute scalar (returns single value)
        public object ExecuteScalar(string query)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        // Parameterized Execute scalar
        public object ExecuteScalar(string query, Dictionary<string, object> parameters)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}