using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack;
using SqlExport.Extensions;

namespace SqlExport
{
    public class SqlCsvExporter
    {
        private readonly SqlConnection sqlConnection;

        public SqlCsvExporter(SqlConnection sqlConnection)
        {
            this.sqlConnection = sqlConnection;
        }

        public Stream ExportSql(string commandText)
        {
            return ExportSql(commandText, new Dictionary<string, object>());
        }

        public Stream ExportSql(string commandText, Dictionary<string, object> parameters)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            this.sqlConnection.UsingOpenConnection(c =>
            {
                commandText = CleanSqlCmdSyntax(commandText);
                var command = new SqlCommand(commandText, c);
                foreach (var parameterPair in parameters)
                {
                    command.Parameters.AddWithValue(parameterPair.Key, parameterPair.Value);
                }

                using(var sqlDataReader = command.ExecuteReader())
                {
                    for (int i = 0; i < sqlDataReader.FieldCount; i++)
                    {
                        streamWriter.Write(sqlDataReader.GetName(i).ToCsvField());

                        if (i != sqlDataReader.FieldCount - 1)
                        {
                            streamWriter.Write(",");
                        }
                        else
                        {
                            streamWriter.Write("\n");
                        }
                    }

                    while(sqlDataReader.Read())
                    for (int i = 0; i < sqlDataReader.FieldCount; i++)
                    {
                        var field = sqlDataReader[i].ToCsvField();
                        streamWriter.Write(field);
                        if (i != sqlDataReader.FieldCount - 1)
                        {
                            streamWriter.Write(",");
                        }
                        else
                        {
                            streamWriter.Write("\n");
                        }

                        streamWriter.Flush();
                    }
                }
            });

            return stream;
        }

        private static string CleanSqlCmdSyntax(string commandText)
        {
            commandText = Regex.Replace(commandText, "(DECLARE\\s+@(.)*?;)", string.Empty);
            commandText = Regex.Replace(commandText, "(DECLARE\\s+@(.)*$)", string.Empty, RegexOptions.Multiline);
            commandText = Regex.Replace(commandText, "(SET\\s+@(.)*?;)", string.Empty, RegexOptions.Multiline);
            commandText = Regex.Replace(commandText, "(SET\\s+@(.)*$)", string.Empty, RegexOptions.Multiline);
            return commandText;
        }
    }
}
