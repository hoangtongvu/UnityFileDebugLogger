﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityFileDebugLoggerSourceGenerator
{
    [Generator]
    public class BaseGenerator : ISourceGenerator
    {
        const int timePrefixCharCount = 8;
        const int logIdOffset = 3;
        const int logTypeOffset = 7;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FileDebugLoggerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            const string ERROR_LOG_FILE_PATH = "C:\\Users\\Administrator\\Desktop\\SourceGenErrors.txt";
            const string DEBUG_LOG_FILE_PATH = "C:\\Users\\Administrator\\Desktop\\SourceGenDebugLogs.txt";
            string errorDebugContent = "";

            try
            {
                if (!(context.SyntaxReceiver is FileDebugLoggerSyntaxReceiver receiver))
                    return;

                if (receiver.MainContainerSyntax == null) return;
                if (receiver.ConcreteLoggerSyntaxes.Count == 0) return;

                var compilation = context.Compilation;

                var concreteLoggerNames = new List<string>();
                var concreteLoggerNamespaces = new List<string>();

                // Concrete loggers
                foreach (var structDeclaration in receiver.ConcreteLoggerSyntaxes)
                {
                    string concreteLoggerName = structDeclaration.Identifier.ToString();
                    string concreteLoggerNamespace = this.GetNamespace(structDeclaration);

                    concreteLoggerNames.Add(concreteLoggerName);
                    concreteLoggerNamespaces.Add(concreteLoggerNamespace);

                    var genericArguments = ((GenericNameSyntax)structDeclaration.BaseList.Types[0].Type)
                        .TypeArgumentList.Arguments;

                    SemanticModel semanticModel = compilation.GetSemanticModel(genericArguments[0].SyntaxTree);

                    this.GetNameAndNamespaceOfGenericArgument(semanticModel, genericArguments[0]
                        , out string fixedStringTypeName, out string fixedStringNamespace);

                    this.GeneratePartialPartConcreteLogger(context, concreteLoggerName, concreteLoggerNamespace, fixedStringTypeName, fixedStringNamespace);

                }

                // Main container
                var mainContainerSyntax = receiver.MainContainerSyntax;
                string containerName = mainContainerSyntax.Identifier.ToString();
                string containerNamespace = this.GetNamespace(mainContainerSyntax);

                this.GeneratePartialPartMainContainer(
                    context
                    , containerName
                    , containerNamespace
                    , concreteLoggerNames
                    , concreteLoggerNamespaces);

            }
            catch (Exception e)
            {
                File.AppendAllText(ERROR_LOG_FILE_PATH, $"Source generator error:\n{e}\n");
                File.AppendAllText(ERROR_LOG_FILE_PATH, $"Debug contents:\n{errorDebugContent}\n");
            }
            
        }

        private string GetNamespace(SyntaxNode syntaxNode)
        {
            SyntaxNode parent = syntaxNode.Parent;

            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
                    return namespaceDeclaration.Name.ToString();

                parent = parent.Parent;

            }

            return null;

        }

        private void GetNameAndNamespaceOfGenericArgument(
            SemanticModel semanticModel
            , ExpressionSyntax expressionSyntax
            , out string typeName
            , out string namespaceName)
        {
            ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(expressionSyntax).Type;
            if (typeSymbol != null)
            {
                typeName = typeSymbol.Name;
                namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "(NoNamespace)";
                return;
            }

            throw new System.Exception($"Can not resolve {nameof(ITypeSymbol)} for Generic argument");

        }

        private void GeneratePartialPartConcreteLogger(
            GeneratorExecutionContext context
            , string loggerName
            , string loggerNamespace
            , string fixedStringTypeName
            , string fixedStringNamespace)
        {
            string fixedStringIdentifier = $"{fixedStringNamespace}.{fixedStringTypeName}";
            string timeDataIdentifier = "Unity.Core.TimeData";

            string sourceCode = $@"
using System.Text;
using Unity.Burst;
using Unity.Collections;

namespace {loggerNamespace}
{{
    [BurstCompile]
    public partial struct {loggerName}
    {{
        private static readonly FixedString32Bytes logHeader = ""TimeStamp, Id, LogType, Log"";
        private static readonly FixedString32Bytes defaultLogDirectory = ""FileDebugLoggerLogs/"";
        public readonly bool isInitialized;
        public readonly NativeList<double> logTimeInfos;
        public readonly NativeList<{fixedStringIdentifier}> logs;

        public {loggerName}(int initialCap, Allocator allocator, bool isBurstLogger = false)
        {{
            this.isInitialized = true;
            this.logs = new(initialCap, allocator);
            this.logTimeInfos = isBurstLogger ? new(initialCap, allocator) : new();

        }}

        public readonly void Clear() => this.logs.Clear();

        public readonly void Dispose()
        {{
            this.logs.Dispose();
            this.logTimeInfos.Dispose();
        }}

        [BurstDiscard]
        public readonly void Log(in {fixedStringIdentifier} newLog) => this.BaseLog(in newLog, LogType.Log);

        [BurstDiscard]
        public readonly void LogWarning(in {fixedStringIdentifier} newLog) => this.BaseLog(in newLog, LogType.Warning);

        [BurstDiscard]
        public readonly void LogError(in {fixedStringIdentifier} newLog) => this.BaseLog(in newLog, LogType.Error);

        [BurstDiscard]
        public readonly void BaseLog(in {fixedStringIdentifier} newLog, LogType logType)
        {{
            {fixedStringIdentifier} prefix = $""{{System.DateTime.Now.ToString(""HH:mm:ss""), {timePrefixCharCount}}} | {{this.logs.Length, {logIdOffset}}} | {{logType, {logTypeOffset}}} | "";
            prefix.Append(newLog);

            this.logs.Add(prefix);
        }}

        [BurstDiscard]
        public readonly void Save(in FixedString64Bytes fileName, bool append = false)
        {{
            int length = this.logs.Length;

            StringBuilder stringBuilder = new();

            for (int i = 0; i < length; i++)
            {{
                var log = this.logs[i];
                stringBuilder.AppendLine(log.ToString());
            }}

            FileWriter.Write(defaultLogDirectory + fileName.ToString(), stringBuilder.ToString(), append);

        }}

        [BurstDiscard]
        public readonly void Save(in FixedString64Bytes fileName, in {timeDataIdentifier} finalTimeData, bool append = false)
        {{
            var dateTimeNow = System.DateTime.Now;
            int length = this.logs.Length;

            StringBuilder stringBuilder = new();

            for (int i = 0; i < length; i++)
            {{
                var log = this.logs[i];
                double elapsedSeconds = this.logTimeInfos[i];
                var futureTime = dateTimeNow.AddSeconds(-finalTimeData.ElapsedTime + elapsedSeconds);

                stringBuilder.Append($""{{futureTime.ToString(""HH:mm:ss""), {timePrefixCharCount}}} | "");
                stringBuilder.AppendLine(log.ToString());
            }}

            FileWriter.Write(defaultLogDirectory + fileName.ToString(), stringBuilder.ToString(), append);

        }}

    }}

}}
";

            context.AddSource($"{loggerName}.g.cs", sourceCode);

            this.GenerateBurstCompileLoggingMethods(
                context
                , loggerName
                , loggerNamespace
                , timeDataIdentifier
                , fixedStringIdentifier);

        }

        private void GenerateBurstCompileLoggingMethods(GeneratorExecutionContext context
            , string loggerName
            , string loggerNamespace
            , string timeDataIdentifier
            , string fixedStringIdentifier)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("using Unity.Burst;");
            stringBuilder.AppendLine("using Unity.Collections;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {loggerNamespace}");
            stringBuilder.AppendLine("{");

            stringBuilder.AppendLine($"\tpublic partial struct {loggerName}");
            stringBuilder.AppendLine("\t{");

            stringBuilder.AppendLine(this.GetBurstCompileLoggingMethodSrc("Log", "Log", timeDataIdentifier, fixedStringIdentifier));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(this.GetBurstCompileLoggingMethodSrc("LogWarning", "Warning", timeDataIdentifier, fixedStringIdentifier));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(this.GetBurstCompileLoggingMethodSrc("LogError", "Error", timeDataIdentifier, fixedStringIdentifier));

            stringBuilder.AppendLine("\t}");

            stringBuilder.AppendLine("}");

            context.AddSource($"{loggerName}.burst-methods.g.cs", stringBuilder.ToString());
        }

        private string GetBurstCompileLoggingMethodSrc(
            string methodName
            , string logTypeIdentifier
            , string timeDataIdentifier
            , string fixedStringIdentifier)
        {
            string src = $@"        [BurstCompile]
        public readonly void {methodName}(in {timeDataIdentifier} timeData, in {fixedStringIdentifier} newLog)
        {{
            Unity.Collections.FixedString128Bytes prefix = $""{{this.logs.Length, {logIdOffset}}} | {logTypeIdentifier, logTypeOffset} | "";
            prefix.Append(newLog);

            this.logs.Add(prefix);
            this.logTimeInfos.Add(timeData.ElapsedTime);
        }}
";

            return src;
        }

        private void GeneratePartialPartMainContainer(
            GeneratorExecutionContext context
            , string containerName
            , string containerNamespace
            , List<string> loggerNames
            , List<string> loggerNamespaces)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"using Unity.Collections;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {containerNamespace}");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"\tpublic partial struct {containerName}");
            stringBuilder.AppendLine("\t{");

            int length = loggerNames.Count;
            for (int i = 0; i < length; i++)
            {
                stringBuilder.AppendLine($"\t\tpublic static {loggerNamespaces[i]}.{loggerNames[i]} Create{loggerNames[i]}(int initialCap, Allocator allocator, bool isBurstLogger = false) => new(initialCap, allocator, isBurstLogger); \n");
            }

            stringBuilder.AppendLine("\t}");
            stringBuilder.AppendLine("}");

            context.AddSource($"{containerName}.g.cs", stringBuilder.ToString());

        }

        private void GenerateDebugClass(
            GeneratorExecutionContext context
            , string debugContent)
        {
            string escaped = debugContent
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", "\\n");

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace DarkSun.Debug");
            stringBuilder.AppendLine("{");

            stringBuilder.AppendLine("\tpublic static class Debugger");
            stringBuilder.AppendLine("\t{");

            stringBuilder.Append($"\t\tpublic static string DebugContent = ");
            stringBuilder.Append("\"Error:[");

            stringBuilder.Append(escaped);

            stringBuilder.Append("]\";");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("\t}");

            stringBuilder.AppendLine("}");

            context.AddSource("Debugger.g.cs", stringBuilder.ToString());

        }

    }

}