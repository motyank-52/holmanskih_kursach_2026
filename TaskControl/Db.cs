using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace TaskControl
{
    internal static class Db
    {
        private const string ConnStr =
            @"Server=(localdb)\MSSQLLocalDB;Database=TaskControlDB;Trusted_Connection=True;";

        public static DataTable Query(string sql, params SqlParameter[] p)
        {
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (p != null && p.Length > 0)
                    cmd.Parameters.AddRange(p);

                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int Exec(string sql, params SqlParameter[] p)
        {
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (p != null && p.Length > 0)
                    cmd.Parameters.AddRange(p);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
