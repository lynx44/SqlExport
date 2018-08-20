using System.Configuration;
using System.Data.SqlClient;

namespace SqlExport.Tests.Infrastructure.Helpers
{
    public static class ConnectionFactory
    {
        public static SqlConnection TestDb
        {
            get
            {
                return new SqlConnection(ConfigurationManager.
                    ConnectionStrings["SqlExport.Tests.Properties.Settings.TestDbConnectionString"].ConnectionString);
            }
        }
    }
}
