using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Models
{
    public static class DatabaseHelper
    {
        public static string MySqlConnStr =>
            ConfigurationManager.ConnectionStrings["MySqlConn"].ConnectionString;

        public static MySqlConnection GetMySqlConnection()
        {
            MySqlConnection conn = new MySqlConnection(MySqlConnStr);
            conn.Open();
            return conn;
        }
    }
}
