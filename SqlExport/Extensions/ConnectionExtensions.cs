using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlExport.Extensions
{
    static class ConnectionExtensions
    {
        public static void UsingOpenConnection(this SqlConnection connection, Action<SqlConnection> action)
        {
            var originalState = connection.State;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            action(connection);

            if (originalState == ConnectionState.Closed)
            {
                connection.Close();
            }
        }

    }
}
