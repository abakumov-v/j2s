﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Bogus;
using Ether.Outcomes;
using HugeJson2SqlTransformer.Files.Abstract;
using HugeJson2SqlTransformer.Sql.Abstract;
using HugeJson2SqlTransformer.Transformers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace HugeJson2SqlTransformer.Tests.Unit.Transformers
{
    [ExcludeFromCodeCoverage]
    [Trait("Category", "Unit")]
    public class Json2SqlTransformerTests
    {
        private readonly Faker _faker = new Faker();
        private readonly Json2SqlTransformer _testModule;
        private readonly IFileReader _jsonFileReader;
        private readonly string _sourceJsonFilePath;
        private string _targetSqlFilePath;
        private readonly ISqlBuilderDirector _sqlBuilderDirector;
        private readonly string _validJsonContent;
        private readonly ISqlBuilder _sqlBuilder;
        private readonly IFileWriter _sqlFileWriter;

        public Json2SqlTransformerTests()
        {
            _sourceJsonFilePath = "some-file.json";
            _targetSqlFilePath = "target-sql.sql";

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
            _jsonFileReader = Substitute.For<IFileReader>();
            _jsonFileReader.ReadAllTextAsync(_sourceJsonFilePath).Returns(_validJsonContent);

            _sqlBuilder = Substitute.For<ISqlBuilder>();
            _sqlBuilderDirector = Substitute.For<ISqlBuilderDirector>();
            _sqlFileWriter = Substitute.For<IFileWriter>();
            _testModule = new Json2SqlTransformer(_jsonFileReader, _sqlFileWriter, _sqlBuilderDirector, _sqlBuilder);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ExecuteAsync_JsonFilePathIsNullOrEmpty_ReturnFailureWithCorrectMessage(string jsonFilePath)
        {
            // Arrange
            // Act
            var transformResult = await _testModule.ExecuteAsync(jsonFilePath, _targetSqlFilePath);
            // Assert
            Assert.True(transformResult.Failure);
            Assert.Equal("File path is incorrect", transformResult.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_JsonFilePathIsNotNullOrEmpty_ReturnSuccess()
        {
            // Arrange
            // Act
            var transformResult = await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            Assert.True(transformResult.Success);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFilePathIsCorrect_JsonFileWasRead()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            await _jsonFileReader.Received(1).ReadAllTextAsync(_sourceJsonFilePath);
        }

        [Fact]
        public async Task ExecuteAsync_SomeExceptionWasOccuredWhenJsonFileWasRead_ReturnFailureWithCorrectErrorMessage()
        {
            // Arrange
            var errorMessage = "cannot read test JSON file";
            _jsonFileReader.ReadAllTextAsync(Arg.Any<string>()).Throws(new Exception(errorMessage));
            // Act
            var transformResult = await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            Assert.True(transformResult.Failure);
            Assert.Equal(errorMessage, transformResult.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_SqlBuilderDirectorChangeInnerSqlBuilder()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            await _sqlBuilderDirector.Received(1).ChangeBuilder(_sqlBuilder);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_SqlBuilderDirectorWasCalled()
        {
            // Arrange
            // Act
            await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            await _sqlBuilderDirector.Received(1).MakeAsync(Arg.Is<string>(e => e == _validJsonContent));
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_ReturnSuccess()
        {
            // Arrange
            var correctSql = "some SQL statements";
            _sqlBuilderDirector.MakeAsync(_validJsonContent).Returns(correctSql);
            // Act
            var transformResult = await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            Assert.True(transformResult.Success);
        }

        [Fact]
        public async Task ExecuteAsync_JsonFileHasValidContent_SqlStatementWasWroteToFileSuccessfully()
        {
            // Arrange
            var correctSql = "some SQL statements";
            _sqlBuilderDirector.MakeAsync(_validJsonContent).Returns(correctSql);
            // Act
            await _testModule.ExecuteAsync(_sourceJsonFilePath, _targetSqlFilePath);
            // Assert
            await _sqlFileWriter.Received(1).WriteAllTextAsync(_targetSqlFilePath, correctSql);
        }
    }
}