using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using GenericParsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack;
using SqlExport.Extensions;
using SqlExport.Tests.Infrastructure.Extensions;
using SqlExport.Tests.Infrastructure.Helpers;

namespace SqlExport.Tests.Tests
{
    [TestClass]
    public class SqlCsvExporterTests
    {
        private SqlCsvExporter exporter;
        private SqlConnection sqlConnection;

        [TestInitialize]
        public void Setup()
        {
            this.sqlConnection = ConnectionFactory.TestDb;
            this.exporter = new SqlCsvExporter(this.sqlConnection);
        }

        [TestMethod]
        public void BasicSqlToCsvStream()
        {
            var tableName = "Customer_BSTCS";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format("SELECT Id, FirstName, LastName FROM {0}", tableName));
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(2, dataTable.Rows.Count);

                    Assert.AreEqual("1", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Chaz", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bazz", dataTable.Rows[0]["LastName"]);

                    Assert.AreEqual("2", dataTable.Rows[1]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[1]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[1]["LastName"]);
                });
        }

        [TestMethod]
        public void BasicSqlToCsvStream_WithCommas()
        {
            var tableName = "Customer_BSTCS_WC";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Ch,az', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Ba,rk')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format("SELECT Id, FirstName, LastName FROM {0}", tableName));
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(2, dataTable.Rows.Count);

                    Assert.AreEqual("1", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Ch,az", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bazz", dataTable.Rows[0]["LastName"]);

                    Assert.AreEqual("2", dataTable.Rows[1]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[1]["FirstName"]);
                    Assert.AreEqual("Ba,rk", dataTable.Rows[1]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream()
        {
            var tableName = "Customer_PSTCS";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream_StripsDeclaresOnSeparateLine()
        {
            var tableName = "Customer_PSTCS_WDOSL";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "DECLARE @Id AS INT" + Environment.NewLine +
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream_StripsSemicolonDelimitedDeclares()
        {
            var tableName = "Customer_PSTCS_WSDD";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "DECLARE @Id AS INT; " +
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream_StripsSemicolonDelimitedDeclaresOnSeparateLines()
        {
            var tableName = "Customer_PSTCS_WSDDOSL";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "DECLARE @Id AS INT; " + Environment.NewLine +
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream_StripsSetOnSeparateLines()
        {
            var tableName = "Customer_PSTCS_SSOSL";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "DECLARE @Id AS INT" + Environment.NewLine +
                        "SET @Id=1" + Environment.NewLine +
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }

        [TestMethod]
        public void ParameterizedSqlToCsvStream_StripsSemicolonDelimitedSet()
        {
            var tableName = "Customer_PSTCS_SSDS";
            this.sqlConnection.UsingNewTable(
                new TableDefinition(tableName, 
                    "Id INT", 
                    "FirstName VARCHAR(50)", 
                    "LastName VARCHAR(50)"),
                connection =>
                {
                    connection.UsingOpenConnection(c =>
                    {
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(1, 'Chaz', 'Bazz')", tableName));
                        c.ExecuteNonQueries(string.Format(@"INSERT INTO {0} 
                        VALUES(2, 'Mark', 'Bark')", tableName));
                    });

                    var csvStream = this.exporter.ExportSql(string.Format(
                        "DECLARE @Id AS INT;" +
                        "SET @Id=1;" +
                        "SELECT Id, FirstName, LastName FROM {0} " +
                        "WHERE Id=@Id", tableName), new Dictionary<string, object>() { { "@Id", 2 } });
                    var dataTable = csvStream.ReadAsString().CsvToDataTable();

                    Assert.AreEqual(1, dataTable.Rows.Count);

                    Assert.AreEqual("2", dataTable.Rows[0]["Id"]);
                    Assert.AreEqual("Mark", dataTable.Rows[0]["FirstName"]);
                    Assert.AreEqual("Bark", dataTable.Rows[0]["LastName"]);
                });
        }
    }
}
