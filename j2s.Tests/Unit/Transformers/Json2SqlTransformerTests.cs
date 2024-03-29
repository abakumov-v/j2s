﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Bogus;
using j2s.Extensions;
using j2s.Files.Abstract;
using j2s.Sql.Abstract;
using j2s.Tests.Unit.Fake;
using j2s.Transformers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace j2s.Tests.Unit.Transformers
{
    [ExcludeFromCodeCoverage]
    [Trait("Category", "Unit")]
    public class Json2SqlTransformerTests
    {
        private readonly Faker _faker = new Faker();
        private readonly Json2SqlTransformer _testModule;
        private readonly IFileReader _fileReader;
        private readonly string _sourceJsonFileName;
        private readonly string _sourceJsonFilePath;
        private readonly Json2SqlTransformOptions _transformOptions;
        private readonly string _validJsonContent;
        private readonly ISqlBuilder _sqlBuilder;
        private readonly IFileWriter _fileWriter;
        private readonly string _sourceDirectory;

        public Json2SqlTransformerTests()
        {
            _sourceDirectory = "folder";
            _sourceJsonFileName = "some-file";
            _sourceJsonFilePath = $@"{_sourceDirectory}\{_sourceJsonFileName}.json";
            _transformOptions = new Json2SqlTransformOptions()
            {
                SourceJsonFilePath = _sourceJsonFilePath,
            };

            _validJsonContent = @"
[
    {
	    ""firstName"": ""James"",
        ""lastName"": ""Bond"",
        ""isClient"": false,
        ""email"": ""james-bond@example.com""
    },
    {
	    ""firstName"": ""John"",
        ""lastName"": ""Doe"",
        ""isClient"": true,
        ""email"": ""john-doe@example.com""
    }
]
";
            _fileReader = Substitute.For<IFileReader>();
            _fileReader.ReadAllTextAsync(_sourceJsonFilePath).Returns(_validJsonContent);

            _sqlBuilder = Substitute.For<ISqlBuilder>();
            _fileWriter = Substitute.For<IFileWriter>();
            _testModule = new Json2SqlTransformer(_fileReader, _fileWriter, _sqlBuilder);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ExecuteAsync_SourceJsonFilePathIsNullOrEmpty_ReturnFailureWithCorrectMessage(
            string sourceJsonFilePath)
        {
            // Arrange
            _transformOptions.SourceJsonFilePath = sourceJsonFilePath;
            // Act
            var transformResult = await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            Assert.True(transformResult.Failure);
            Assert.Equal("Source file path is incorrect", transformResult.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_JsonFilePathIsNotNullOrEmpty_ReturnSuccess()
        {
            // Arrange
            // Act
            var transformResult = await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            Assert.True(transformResult.Success);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFilePathIsCorrect_JsonFileWasRead()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            await _fileReader.Received(1).ReadAllTextAsync(_sourceJsonFilePath);
        }

        [Fact]
        public async Task ExecuteAsync_SomeExceptionWasOccuredWhenJsonFileWasRead_ReturnFailureWithCorrectErrorMessage()
        {
            // Arrange
            var errorMessage = "cannot read test JSON file";
            _fileReader.ReadAllTextAsync(Arg.Any<string>()).Throws(new Exception(errorMessage));
            // Act
            var transformResult = await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            Assert.True(transformResult.Failure);
            Assert.Equal(errorMessage, transformResult.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_BuildCreateTableWasCalled()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).BuildCreateTable();
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_BuildInsertWasCalled()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).BuildInsert(_validJsonContent);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_ReturnSuccess()
        {
            // Arrange
            var correctCreateTableStatement = "create table SQL";
            var correctInsertStatement = "insert to SQL table";

            _sqlBuilder.BuildCreateTable().Returns(correctCreateTableStatement);
            _sqlBuilder.BuildInsert(_validJsonContent).Returns(correctInsertStatement);
            // Act
            var transformResult = await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            Assert.True(transformResult.Success);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_DatabaseSchemaWasSetUpForSqlBuilder()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).SetSchema(_transformOptions.TableSchema);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_DatabaseTableNameWasSetUpForSqlBuilder()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).SetTableName(_transformOptions.TableName);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_CreateTableAndInsertStatementsWasBuiltSeparately()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).BuildCreateTable();
            _sqlBuilder.Received(1).BuildInsert(_validJsonContent);
        }

        [Fact]
        public async Task ExecuteAsync_FileWithSqlStatementOfCreateTableWasCreated()
        {
            // Arrange
            var correctFileName = $"001-create-table-{_transformOptions.TableSchema}_{_transformOptions.TableName}.sql";
            var correctFilePath = Path.Combine(_sourceDirectory, _sourceJsonFileName, correctFileName);
            var correctCreateTableSqlStatement = "create table SQL statement";
            _sqlBuilder.BuildCreateTable().Returns(correctCreateTableSqlStatement);
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            await _fileWriter.Received(1).WriteAllTextAsync(correctFilePath, correctCreateTableSqlStatement);
        }

        [Fact]
        public async Task
            ExecuteAsync_MaxLinesPer1InsertValuesSqlFileIsNullAsDefault_FileWithSqlStatementOfInsertValuesWasCreatedAtOnce()
        {
            // Arrange
            var correctFileName =
                $"002-insert-values-into-{_transformOptions.TableSchema}_{_transformOptions.TableName}.sql";
            var correctFilePath = Path.Combine(_sourceDirectory, _sourceJsonFileName, correctFileName);
            var correctInsertValuesSqlStatement = "insert values SQL statement";
            _sqlBuilder.BuildInsert(_validJsonContent).Returns(correctInsertValuesSqlStatement);
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            await _fileWriter.Received(1).WriteAllTextAsync(correctFilePath, correctInsertValuesSqlStatement);
        }

        [Theory]
        [InlineData(2, 7, 4)]
        [InlineData(2, 8, 4)]
        [InlineData(2, 3, 2)]
        [InlineData(2, 1, 1)]
        public async Task
            ExecuteAsync_SetMaxLinesPer1InsertValuesSqlFile_FileWithSqlStatementOfInsertValuesWasCreatedCorrectTimes(
                int maxLinesPer1InsertValuesSqlFile, int jsonItemsCount, int correctInsertValuesSqlFilesCount)
        {
            // Arrange
            var jsonContent = FakeJson.CreateMultiple(jsonItemsCount).AsJsonString();
            _fileReader.ReadAllTextAsync(_transformOptions.SourceJsonFilePath).Returns(jsonContent);
            var correctInsertSqlStatement = "some insert values";
            _sqlBuilder.BuildInsert(jsonContent, Arg.Any<int>(), Arg.Any<int>())
                .Returns(correctInsertSqlStatement);

            _transformOptions.MaxLinesPer1InsertValuesSqlFile = maxLinesPer1InsertValuesSqlFile;
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            await _fileWriter.Received(correctInsertValuesSqlFilesCount)
                .WriteAllTextAsync(Arg.Any<string>(), correctInsertSqlStatement);
        }

        [Fact]
        public async Task ExecuteAsync_JsonContentHasItemsWithNestedObjects_SqlStatementOfInsertValuesWasCreatedCorrectTimes()
        {
            // Arrange
            var maxLinesPer1InsertValuesSqlFile = 2;
            var jsonItem1 = FakeJson.Create();
            var jsonItem2 = FakeJson.Create();
            var jsonItem3 = $@"{{
	""firstName"": ""{_faker.Person.FirstName}"",
    ""lastName"": ""{_faker.Person.LastName}"",
    ""isClient"": {_faker.Random.Bool()},
    ""emails"": [{{ ""item"": ""{_faker.Person.Email}"" }},{{ ""item"": ""{_faker.Person.Email}"" }}]
}}";
            var jsonItem4 = $@"{{
	""firstName"": ""{_faker.Person.FirstName}"",
    ""lastName"": ""{_faker.Person.LastName}"",
    ""isClient"": {_faker.Random.Bool()},
    ""emails"": [{{ ""item"": ""{_faker.Person.Email}"" }},{{ ""item"": ""{_faker.Person.Email}"" }}]
}}";
            var jsonItem5 = $@"{{
	""firstName"": ""{_faker.Person.FirstName}"",
    ""lastName"": ""{_faker.Person.LastName}"",
    ""isClient"": {_faker.Random.Bool()},
    ""emails"": [{{ ""item"": ""{_faker.Person.Email}"" }},{{ ""item"": ""{_faker.Person.Email}"" }}]
}}";
            var jsonContent = new List<string>()
            {
                jsonItem1, jsonItem2, jsonItem3, jsonItem4, jsonItem5
            }.AsJsonString();
            _fileReader.ReadAllTextAsync(_transformOptions.SourceJsonFilePath).Returns(jsonContent);

            _transformOptions.MaxLinesPer1InsertValuesSqlFile = maxLinesPer1InsertValuesSqlFile;
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(3).BuildInsert(jsonContent, skip: Arg.Any<int>(), limit: maxLinesPer1InsertValuesSqlFile);
        }

        [Fact]
        public async Task
            ExecuteAsync_SetMaxLinesPer1InsertValuesSqlFile_SqlStatementOfInsertValuesWasCreatedCorrectTimes()
        {
            // Arrange
            var maxLinesPer1InsertValuesSqlFile = 2;
            var jsonItemsCount = 7;
            var jsonContent = FakeJson.CreateMultiple(jsonItemsCount).AsJsonString();
            _fileReader.ReadAllTextAsync(_transformOptions.SourceJsonFilePath).Returns(jsonContent);

            _transformOptions.MaxLinesPer1InsertValuesSqlFile = maxLinesPer1InsertValuesSqlFile;
            // Act
            await _testModule.ExecuteAsync(_transformOptions);
            // Assert
            _sqlBuilder.Received(1).BuildInsert(jsonContent, skip: 0, limit: maxLinesPer1InsertValuesSqlFile);
            _sqlBuilder.Received(1).BuildInsert(jsonContent, skip: 2, limit: maxLinesPer1InsertValuesSqlFile);
            _sqlBuilder.Received(1).BuildInsert(jsonContent, skip: 4, limit: maxLinesPer1InsertValuesSqlFile);
            _sqlBuilder.Received(1).BuildInsert(jsonContent, skip: 6, limit: maxLinesPer1InsertValuesSqlFile);
        }
    }
}