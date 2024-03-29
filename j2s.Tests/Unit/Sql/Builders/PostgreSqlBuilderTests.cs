﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Bogus;
using j2s.Extensions;
using j2s.Sql.Builders;
using j2s.Sql.TableDefinition;
using j2s.Tests.Unit.Fake;
using Xunit;

namespace j2s.Tests.Unit.Sql.Builders
{
    [ExcludeFromCodeCoverage]
    [Trait("Category", "Unit")]
    public class PostgreSqlBuilderTests
    {
        private readonly Faker _faker = new Faker();
        private readonly PostgreSqlBuilder _testModule;
        private readonly List<TableColumn> _tableColumns;
        private readonly string _schema;
        private readonly string _tableName;

        public PostgreSqlBuilderTests()
        {
            _tableColumns = new List<TableColumn>()
            {
                new TableColumn("FirstName", "varchar(100)", required: true),
                new TableColumn("LastName", "varchar(100)", required: true),
                new TableColumn("IsClient", "boolean"),
                new TableColumn("Phone", "varchar(100)"),
            };
            _schema = "dbo";
            _tableName = "tableName";
            _testModule = new PostgreSqlBuilder(_tableColumns);
            _testModule.SetSchema(_schema);
            _testModule.SetTableName(_tableName);
        }

        #region Ctor

        [Fact]
        public void Ctor_TableColumnsIsEmpty()
        {
            // Arrange
            // Act
            var testModule = new PostgreSqlBuilder(null);
            // Assert
            Assert.Empty(testModule.TableColumns);
        }

        [Fact]
        public void Ctor_TableColumnsIsNotEmpty_TableColumnsHasCorrectItems()
        {
            // Arrange
            var correctTableColumns = new List<TableColumn>()
            {
                new TableColumn("FirstName", "varchar(100)", required: true),
                new TableColumn("LastName", "varchar(100)", required: true),
                new TableColumn("IsClient", "boolean"),
                new TableColumn("Phone", "varchar(100)"),
            };
            var testModule = new PostgreSqlBuilder(correctTableColumns);
            // Act
            var tableColumns = testModule.TableColumns;
            // Assert
            Assert.Contains(tableColumns, e => e.Name == "FirstName");
            Assert.Contains(tableColumns, e => e.Name == "LastName");
            Assert.Contains(tableColumns, e => e.Name == "IsClient");
            Assert.Contains(tableColumns, e => e.Name == "Phone");
        }

        #endregion

        #region Method: BuildCreateTable

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BuildCreateTable_ThrowExceptionIfTableSchemaIsNullOrEmpty(string schema)
        {
            // Arrange
            _testModule.SetSchema(schema);

            Action act = () => _testModule.BuildCreateTable();
            // Act
            var ex = Record.Exception(act);
            // Assert
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains(nameof(_testModule.Schema), ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BuildCreateTable_ThrowExceptionIfTableNameIsNullOrEmpty(string tableName)
        {
            // Arrange
            _testModule.SetTableName(tableName);

            Action act = () => _testModule.BuildCreateTable();
            // Act
            var ex = Record.Exception(act);
            // Assert
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains(nameof(_testModule.Table), ex.Message);
        }

        [Fact]
        public void BuildCreateTable_ReturnCorrectSqlStatement()
        {
            // Arrange
            var correctSqlStatement = $@"create table ""{_schema}"".""{_tableName}""(
    ""{_tableColumns[0].Name}"" {_tableColumns[0].Type} not null
    , ""{_tableColumns[1].Name}"" {_tableColumns[1].Type} not null
    , ""{_tableColumns[2].Name}"" {_tableColumns[2].Type}
    , ""{_tableColumns[3].Name}"" {_tableColumns[3].Type}
);"
                .Replace("\r\n", "\n");
            // Act
            var sqlStatement = _testModule.BuildCreateTable();
            // Assert
            Assert.Equal(correctSqlStatement, sqlStatement);
        }

        #endregion

        #region Method: BuildInsert

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BuildInsert_ThrowExceptionIfTableSchemaIsNullOrEmpty(string schema)
        {
            // Arrange
            _testModule.SetSchema(schema);

            var jsonItems = "some-json";
            Action act = () => _testModule.BuildInsert(jsonItems);
            // Act
            var ex = Record.Exception(act);
            // Assert
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains(nameof(_testModule.Schema), ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BuildInsert_ThrowExceptionIfTableNameIsNullOrEmpty(string tableName)
        {
            // Arrange
            _testModule.SetTableName(tableName);

            var jsonItems = "some-json";
            Action act = () => _testModule.BuildInsert(jsonItems);
            // Act
            var ex = Record.Exception(act);
            // Assert
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains(nameof(_testModule.Table), ex.Message);
        }

        [Fact]
        public void BuildInsert_ReturnCorrectSqlStatement()
        {
            // Arrange
            var jsonItems = FakeJson.CreateMultiple(2).AsJsonString().FixSingleQuotes();
            var correctSqlStatement =
                $@"insert into ""{_schema}"".""{_tableName}"" (
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
)
select
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
from json_to_recordset('
{jsonItems}
') as x(
    ""{_tableColumns[0].Name}"" {_tableColumns[0].Type}
    , ""{_tableColumns[1].Name}"" {_tableColumns[1].Type}
    , ""{_tableColumns[2].Name}"" {_tableColumns[2].Type}
    , ""{_tableColumns[3].Name}"" {_tableColumns[3].Type}
);"
                    .Replace("\r\n", "\n");
            // Act
            var sqlStatement = _testModule.BuildInsert(jsonItems);
            // Assert
            Assert.Equal(correctSqlStatement, sqlStatement);
        }

        [Fact]
        public void BuildInsert_JsonItemsHasSingleQuote_ReturnCorrectSqlStatementWithDoubleSingleQuotes()
        {
            // Arrange
            var jsonItems = FakeJson.CreateMultiple(2).AsJsonString().FixSingleQuotes();
            var correctSqlStatement =
                $@"insert into ""{_schema}"".""{_tableName}"" (
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
)
select
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
from json_to_recordset('
{jsonItems.Replace("'", "''")}
') as x(
    ""{_tableColumns[0].Name}"" {_tableColumns[0].Type}
    , ""{_tableColumns[1].Name}"" {_tableColumns[1].Type}
    , ""{_tableColumns[2].Name}"" {_tableColumns[2].Type}
    , ""{_tableColumns[3].Name}"" {_tableColumns[3].Type}
);"
                    .Replace("\r\n", "\n");
            // Act
            var sqlStatement = _testModule.BuildInsert(jsonItems);
            // Assert
            Assert.Equal(correctSqlStatement, sqlStatement);
        }

        [Theory]
        [InlineData(20, 0, 2)]
        [InlineData(20, 2, 5)]
        [InlineData(20, 16, 4)]
        public void BuildInsert_SkipAndLimitIsNotNull_ReturnCorrectCountOfJsonItemsInSqlStatement(int total, int skip,
            int limit)
        {
            // Arrange
            var jsonItemWithNestedObject1 = FakeJson.Create().WithNestedObject();
            var jsonItemWithNestedObject2 = FakeJson.Create().WithNestedObject();
            var jsonItemWithNestedObject3 = FakeJson.Create().WithNestedObject();
            var jsonItems = new List<string>(FakeJson.CreateMultiple(total - 3))
            {
                jsonItemWithNestedObject1, jsonItemWithNestedObject2, jsonItemWithNestedObject3
            };

            var filteredJsonItems = jsonItems.Skip(skip).Take(limit).ToList();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($@"insert into ""{_schema}"".""{_tableName}"" (
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
)
select
    ""{_tableColumns[0].Name}""
    , ""{_tableColumns[1].Name}""
    , ""{_tableColumns[2].Name}""
    , ""{_tableColumns[3].Name}""
from json_to_recordset('
[
");
            for (int i = 0; i < filteredJsonItems.Count; i++)
            {
                stringBuilder.Append($"{filteredJsonItems[i].Replace("'", "''")}");
                if (i < filteredJsonItems.Count - 1)
                {
                    stringBuilder.Append(",\n");
                }
            }

            stringBuilder.Append($@"
]
') as x(
    ""{_tableColumns[0].Name}"" {_tableColumns[0].Type}
    , ""{_tableColumns[1].Name}"" {_tableColumns[1].Type}
    , ""{_tableColumns[2].Name}"" {_tableColumns[2].Type}
    , ""{_tableColumns[3].Name}"" {_tableColumns[3].Type}
);");
            var correctSqlStatement = stringBuilder.ToString().Replace("\r\n", "\n");
            var jsonItemsAsString = jsonItems.AsJsonString();
            // Act
            var sqlStatement = _testModule.BuildInsert(jsonItemsAsString, skip, limit);
            // Assert
            Assert.Equal(correctSqlStatement, sqlStatement);
        }

        #endregion
    }
}