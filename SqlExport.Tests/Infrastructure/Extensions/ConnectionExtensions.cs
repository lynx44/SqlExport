using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace SqlExport.Tests.Infrastructure.Extensions
{
    public static class ConnectionExtensions
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

        public static void UsingNewTable(this SqlConnection connection, TableDefinition tableDefinition, Action<SqlConnection> action)
        {
            var tableName = tableDefinition.TableName;
            connection.UsingOpenConnection(c =>
            {
                var commandBuilder = new StringBuilder();
                commandBuilder.AppendLine(string.Format(@"CREATE TABLE {0} (",
                    tableName));
                commandBuilder.AppendLine(
                    string.Join(Environment.NewLine + ",", tableDefinition.ColumnDefinitions));

                commandBuilder.AppendLine(")");
                var sqlCommand = new SqlCommand(commandBuilder.ToString(), c);
                sqlCommand.ExecuteNonQuery();

                try
                {
                    action(connection);
                }
                finally
                {
                    var dropTableCommand = new SqlCommand(string.Format("DROP TABLE {0}", tableName), c);
                    dropTableCommand.ExecuteNonQuery();
                }
            });
        }

        public static void ExecuteNonQueries(this SqlConnection connection, params string[] queries)
        {
            foreach (var query in queries)
            {
                var sqlCommand = new SqlCommand(query, connection);
                sqlCommand.ExecuteNonQuery();
            }
        }
    }

    public class TableDefinition
    {
        public string TableName { get; set; }
        public IEnumerable<string> ColumnDefinitions { get; set; }
        public TableDefinition(string tableName, params string[] columnDefinitions)
        {
            this.TableName = tableName;
            this.ColumnDefinitions = columnDefinitions;
        }
    }
}
