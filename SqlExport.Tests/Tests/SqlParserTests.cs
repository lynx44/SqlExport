using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlExport.Tests.Tests
{
    [TestClass]
    public class SqlParserTests
    {
        private SqlParser sqlParser;

        [TestInitialize]
        public void Setup()
        {
            this.sqlParser = new SqlParser();
        }

        // different param name casing
        // using AS or not

        [TestMethod]
        public void ReadsIntParameters()
        {
            var parameters = 
                this.sqlParser.ParseVariables("DECLARE @Id AS INT; SET @Id = 1; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@Id", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Int, parameters.First().DbType);
            Assert.AreEqual("1", parameters.First().DefaultValue);
        }
    }
}
