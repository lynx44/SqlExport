using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

        // multiline

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

        [TestMethod]
        public void ReadsIntParameters_WithNoAssignment()
        {
            var parameters = 
                this.sqlParser.ParseVariables("DECLARE @Id AS INT; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@Id", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Int, parameters.First().DbType);
            Assert.AreEqual(null, parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsIntParameters_Multiline()
        {
            var parameters = 
                this.sqlParser.ParseVariables("DECLARE @Id AS INT" + Environment.NewLine + 
                                              "SET @Id = 1" + Environment.NewLine +
                                              "SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@Id", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Int, parameters.First().DbType);
            Assert.AreEqual("1", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsIntParameters_WithoutAs()
        {
            var parameters = 
                this.sqlParser.ParseVariables("DECLARE @Id INT; SET @Id = 1; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@Id", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Int, parameters.First().DbType);
            Assert.AreEqual("1", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsIntParameters_WhenAssignmentUsesDifferentCasing()
        {
            var parameters = 
                this.sqlParser.ParseVariables("DECLARE @ID INT; SET @id = 1; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@ID", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Int, parameters.First().DbType);
            Assert.AreEqual("1", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsVarCharParameters()
        {
            var parameters =
                this.sqlParser.ParseVariables("DECLARE @FirstName Varchar(50); SET @FirstName = 'Bill'; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@FirstName", parameters.First().Name);
            Assert.AreEqual(SqlDbType.VarChar, parameters.First().DbType);
            Assert.AreEqual(50, parameters.First().Size);
            Assert.AreEqual("Bill", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsNVarCharParameters()
        {
            var parameters =
                this.sqlParser.ParseVariables("DECLARE @FirstName nvarchar(50); SET @FirstName = N'Bill'; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@FirstName", parameters.First().Name);
            Assert.AreEqual(SqlDbType.NVarChar, parameters.First().DbType);
            Assert.AreEqual(50, parameters.First().Size);
            Assert.AreEqual("Bill", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsDecimalParameters()
        {
            var parameters =
                this.sqlParser.ParseVariables("DECLARE @Age decimal(20, 2); SET @Age = 20.2; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@Age", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Decimal, parameters.First().DbType);
            Assert.AreEqual(20, parameters.First().Size);
            Assert.AreEqual(2, parameters.First().Precision);
            Assert.AreEqual("20.2", parameters.First().DefaultValue);
        }

        [TestMethod]
        public void ReadsBitParameters()
        {
            var parameters =
                this.sqlParser.ParseVariables("DECLARE @IsHuman BIT; SET @IsHuman = 1; SELECT * FROM Person");

            Assert.AreEqual(1, parameters.Count());
            Assert.AreEqual("@IsHuman", parameters.First().Name);
            Assert.AreEqual(SqlDbType.Bit, parameters.First().DbType);
            Assert.AreEqual("1", parameters.First().DefaultValue);
        }
    }
}
