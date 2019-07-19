﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HugeJson2SqlTransformer.Files.Abstract;
using HugeJson2SqlTransformer.Files.Readers.Json;
using HugeJson2SqlTransformer.Files.Writers;
using HugeJson2SqlTransformer.Sql;
using HugeJson2SqlTransformer.Sql.Builders;
using HugeJson2SqlTransformer.Sql.TableDefinition;
using HugeJson2SqlTransformer.Transformers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HugeJson2SqlTransformer
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        static Dictionary<string, string> AvailableSourceJsonFileTypes = new Dictionary<string, string>()
        {
            {"mongodbcompass", "MongoDB Compass export file"}
        };

        static Dictionary<string, string> AvailableSqlTypes = new Dictionary<string, string>()
        {
            {"postgres", "PostgreSQL"}
        };

        static async Task Main(string[] args)
        {
            try
            {
                var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;

                Console.WriteLine("j2s - JSON to SQL files transformer.");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("\t--source-json=<path>               Full path to source *.json file");
                Console.WriteLine("\t--source-json-type=<type>          One of available source JSON file types");
                Console.WriteLine("\t--target-sql=<sql>                 One of available target SQL databases");
                Console.WriteLine("\t--table-name=<name>                Name of SQL table");
                Console.WriteLine("\t--schema=<schema_name>             Name of SQL table schema");
                Console.WriteLine("\t--limit-inserts=<number>           Max rows in INSERT statement per 1 *.sql file");
                Console.WriteLine("\t--columns-definition-file=<path>   Full path to *.json file for SQL table columns definition");
                Console.WriteLine();

                var jsonSourceFilePath = GetArgumentValue("--source-json", args);
                var jsonSourceFileType = GetArgumentValue("--source-json-type", args);
                if (!AvailableSourceJsonFileTypes.ContainsKey(jsonSourceFileType))
                {
                    var errorBuilder = new StringBuilder();
                    errorBuilder.Append("Sorry, but now supports only next source json file types:");
                    foreach (var item in AvailableSourceJsonFileTypes)
                    {
                        errorBuilder.Append($"\n  * {item.Key} - {item.Value};");
                    }

                    throw new ArgumentException(errorBuilder.ToString());
                }

                var targetSqlType = GetArgumentValue("--target-sql", args);
                if (!AvailableSqlTypes.ContainsKey(targetSqlType))
                {
                    var errorBuilder = new StringBuilder();
                    errorBuilder.Append("Sorry, but now supports only next SQL databases:");
                    foreach (var item in AvailableSqlTypes)
                    {
                        errorBuilder.Append($"\n  * {item.Key} - {item.Value};");
                    }

                    throw new ArgumentException(errorBuilder.ToString());
                }

                var tableName = GetArgumentValue("--table-name", args);
                var schema = GetArgumentValue("--schema", args);
                var limitInserts = Convert.ToInt32(GetArgumentValue("--limit-inserts", args));
                var columnsDefinitionFilePath = GetArgumentValue("--columns-definition-file", args);
                var tableColumns = ParseTableDeColumnsDefinition(columnsDefinitionFilePath);

                var jsonFileReader = new MongoDbCompass_1_17_0_JsonFileReader();
                var fileWriter = new FileWriter();
                var sqlBuilder = new PostgreSqlBuilder(tableColumns);

                var transformer =
                    new Json2SqlTransformer(jsonFileReader, fileWriter, sqlBuilder);

                var transformOptions = new Json2SqlTransformOptions()
                {
                    SourceJsonFilePath = jsonSourceFilePath,
                    TableName = tableName,
                    TableSchema = schema,
                    MaxLinesPer1InsertValuesSqlFile = limitInserts
                };

                var transformResult = await transformer.ExecuteAsync(transformOptions);
                if (transformResult.Success)
                {
                    Console.WriteLine("Transform success!");
                }
                else
                {
                    Error(transformResult.ToString());
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }

            Console.WriteLine("\nPress any key for exit...");
            Console.ReadLine();
        }

        private static void Error(string message)
        {
            Console.WriteLine("--------");
            Console.WriteLine($"Transformation was failed with ERROR:\n{message}");
            Console.WriteLine("--------");
        }

        private static string GetArgumentValue(string argumentName, string[] args)
        {
            var argument = args.FirstOrDefault(e => e.StartsWith(argumentName));
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            var keyValueSeparator = '=';
            var keyValueSeparatorPosition = argument.IndexOf(keyValueSeparator);
            return argument.Substring(keyValueSeparatorPosition + 1);
        }

        private static List<TableColumn> ParseTableDeColumnsDefinition(string file)
        {
            var jsonContent = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<List<TableColumn>>(jsonContent);
        }
    }
}
