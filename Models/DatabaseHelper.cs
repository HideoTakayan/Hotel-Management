using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Models
{
    public static class DatabaseHelper
    {
        public static string MySqlConnStr =>
            ConfigurationManager.ConnectionStrings["MySqlConn"].ConnectionString;

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(MySqlConnStr);
        }

        /// <summary>
        /// Lấy ShiftId của ca trực hiện tại (hoặc tạo mới ca trực hôm nay nếu chưa có).
        /// </summary>
        public static int GetOrCreateCurrentShiftId(MySqlConnection conn, MySqlTransaction trans = null)
        {
            int shiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;

            string queryShift = @"SELECT ShiftId FROM Shifts 
                                  ORDER BY 
                                      (CASE 
                                          WHEN WorkDate = CURDATE() AND ShiftType = @ShiftType THEN 1
                                          WHEN WorkDate = CURDATE() THEN 2
                                          ELSE 3
                                      END) ASC,
                                      WorkDate DESC, 
                                      ShiftType DESC 
                                  LIMIT 1";
            using (var cmdShift = new MySqlCommand(queryShift, conn, trans))
            {
                cmdShift.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = shiftType;
                var res = cmdShift.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return Convert.ToInt32(res);
                }
            }

            int emp1 = 1, emp2 = 1;
            string queryUsers = "SELECT Id FROM Users ORDER BY Role DESC, Id ASC LIMIT 2";
            using (var cmdUser = new MySqlCommand(queryUsers, conn, trans))
            using (var reader = cmdUser.ExecuteReader())
            {
                if (reader.Read()) emp1 = reader.GetInt32("Id");
                if (reader.Read()) emp2 = reader.GetInt32("Id");
                else emp2 = emp1;
            }

            string insertShift = @"INSERT INTO Shifts (WorkDate, ShiftType, Employee1_Id, Employee2_Id) 
                                   VALUES (CURDATE(), @ShiftType, @Emp1, @Emp2)";
            using (var cmdInsert = new MySqlCommand(insertShift, conn, trans))
            {
                cmdInsert.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = shiftType;
                cmdInsert.Parameters.Add("@Emp1", MySqlDbType.Int32).Value = emp1;
                cmdInsert.Parameters.Add("@Emp2", MySqlDbType.Int32).Value = emp2;
                cmdInsert.ExecuteNonQuery();
                return (int)cmdInsert.LastInsertedId;
            }
        }
    }
}
