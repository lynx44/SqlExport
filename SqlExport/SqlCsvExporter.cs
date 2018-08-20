using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
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

        public Stream ExportSql(string sqlCommand)
        {
            return ExportSql(sqlCommand, new Dictionary<string, object>());
        }

        public Stream ExportSql(string sqlCommand, Dictionary<string, object> parameters)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            this.sqlConnection.UsingOpenConnection(c =>
            {
                var command = new SqlCommand(sqlCommand, c);
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
    }
}
