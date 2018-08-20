using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericParsing;

namespace SqlExport.Tests.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static DataTable CsvToDataTable(this string csv)
        {
            DataTable table;
            using (var parser = new GenericParserAdapter(new StringReader(csv)))
            {
                parser.FirstRowHasHeader = true;
                table = parser.GetDataTable();
            }

            return table;
        }
    }
}
