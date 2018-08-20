using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlExport.Extensions
{
    public static class StreamExtensions
    {
        public static string ReadAsString(this Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(stream, System.Text.Encoding.UTF8);
            return streamReader.ReadToEnd();
        }
    }
}
