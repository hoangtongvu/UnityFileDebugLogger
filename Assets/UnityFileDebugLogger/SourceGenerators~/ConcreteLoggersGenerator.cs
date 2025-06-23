using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;

namespace UnityFileDebugLoggerSourceGenerator
{
    [Generator]
    public class ConcreteLoggersGenerator : IIncrementalGenerator
    {
        private const string fileDebugLoggerInterfaceName = "IFileDebugLogger";
        private const string fixedStringNamespace = "Unity.Collections";
        private const string creationMethodsContainerName = "FileDebugLogger";
        private const string packageNamespace = "UnityFileDebugLogger";

        private const string timeDataIdentifier = "Unity.Core.TimeData";

        private const int timePrefixCharCount = 8;
        private const int logIdOffset = 3;
        private const int logTypeOffset = 7;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var loggerProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (syntaxNode, _) => IsConcreteFileDebugLogger(syntaxNode),
                    transform: static (context, _) => GetConcreteLoggerInfo(context));

            context.RegisterSourceOutput(loggerProvider, Generate);

        }

        private static bool IsConcreteFileDebugLogger(SyntaxNode syntaxNode)
        {
            return syntaxNode is StructDeclarationSyntax structDeclaration
                && IsPartialStruct(structDeclaration)
                && ImplementsIFileDebugLogger(structDeclaration);
        }

        private static bool IsPartialStruct(StructDeclarationSyntax structDeclaration)
        {
            return structDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        }

        private static bool ImplementsIFileDebugLogger(StructDeclarationSyntax structDeclaration)
        {
            if (structDeclaration.BaseList == null) return false;

            return structDeclaration.BaseList.Types
                .Any(baseType =>
                {
                    if (baseType.Type is GenericNameSyntax genericName)
                    {
                        return genericName.Identifier.Text == fileDebugLoggerInterfaceName &&
                            genericName.TypeArgumentList.Arguments.Count == 1;
                    }

                    return false;
                });

        }

        private static ConcreteLoggerInfo GetConcreteLoggerInfo(GeneratorSyntaxContext context)
        {
            var structDeclaration = (StructDeclarationSyntax)context.Node;

            string concreteLoggerName = structDeclaration.Identifier.ToString();
            string concreteLoggerNamespace = Utilities.GetNamespace(structDeclaration);

            var genericArguments = ((GenericNameSyntax)structDeclaration.BaseList.Types[0].Type)
                .TypeArgumentList.Arguments;

            return new()
            {
                LoggerName = concreteLoggerName,
                LoggerNamespace = concreteLoggerNamespace,
                FixedStringTypeName = genericArguments[0].ToString(),
            };

        }

        private static void Generate(SourceProductionContext context, ConcreteLoggerInfo concreteLoggerInfo)
        {
            GeneratePartialPartConcreteLogger(in context, in concreteLoggerInfo);
            GenerateCreationMethods(in context, in concreteLoggerInfo);
        }

        private static void GeneratePartialPartConcreteLogger(in SourceProductionContext context, in ConcreteLoggerInfo concreteLoggerInfo)
        {
            string sourceCode = $@"// < auto-generated />

using System.Text;
using Unity.Burst;
using {fixedStringNamespace};

namespace {concreteLoggerInfo.LoggerNamespace}
{{
    [BurstCompile]
    public partial struct {concreteLoggerInfo.LoggerName}
    {{
        private static readonly FixedString32Bytes defaultLogDirectory = ""FileDebugLoggerLogs/"";
        public readonly bool isInitialized;
        public readonly NativeList<double> logTimeInfos;
        public readonly NativeList<{concreteLoggerInfo.FixedStringTypeName}> logs;

        public {concreteLoggerInfo.LoggerName}(int initialCap, Allocator allocator, bool isBurstLogger = false)
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
        public readonly void Log(in {concreteLoggerInfo.FixedStringTypeName} newLog) => this.BaseLog(in newLog, LogType.Log);

        [BurstDiscard]
        public readonly void LogWarning(in {concreteLoggerInfo.FixedStringTypeName} newLog) => this.BaseLog(in newLog, LogType.Warning);

        [BurstDiscard]
        public readonly void LogError(in {concreteLoggerInfo.FixedStringTypeName} newLog) => this.BaseLog(in newLog, LogType.Error);

        [BurstDiscard]
        public readonly void BaseLog(in {concreteLoggerInfo.FixedStringTypeName} newLog, LogType logType)
        {{
            {concreteLoggerInfo.FixedStringTypeName} prefix = $""{{System.DateTime.Now.ToString(""HH:mm:ss""), {timePrefixCharCount}}} | {{this.logs.Length, {logIdOffset}}} | {{logType, {logTypeOffset}}} | "";
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

            context.AddSource($"{concreteLoggerInfo.LoggerName}.g.cs", sourceCode);

            GenerateBurstCompileLoggingMethods(
                in context
                , in concreteLoggerInfo
                , concreteLoggerInfo.FixedStringTypeName);
        }

        private static void GenerateBurstCompileLoggingMethods(
            in SourceProductionContext context
            , in ConcreteLoggerInfo concreteLoggerInfo
            , string fixedStringTypeName)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("// < auto-generated />");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("using Unity.Burst;");
            stringBuilder.AppendLine($"using {fixedStringNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {concreteLoggerInfo.LoggerNamespace}");
            stringBuilder.AppendLine("{");

            stringBuilder.AppendLine($"\tpublic partial struct {concreteLoggerInfo.LoggerName}");
            stringBuilder.AppendLine("\t{");

            stringBuilder.AppendLine(GetBurstCompileLoggingMethodSrc("Log", "Log", fixedStringTypeName));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(GetBurstCompileLoggingMethodSrc("LogWarning", "Warning", fixedStringTypeName));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(GetBurstCompileLoggingMethodSrc("LogError", "Error", fixedStringTypeName));

            stringBuilder.AppendLine("\t}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("}");

            context.AddSource($"{concreteLoggerInfo.LoggerName}.BurstCompilableMethods.g.cs", stringBuilder.ToString());
        }

        private static string GetBurstCompileLoggingMethodSrc(
            string methodName
            , string logTypeIdentifier
            , string fixedStringTypeName)
        {
            string src = $@"        [BurstCompile]
        public readonly void {methodName}(in {timeDataIdentifier} timeData, in {fixedStringTypeName} newLog)
        {{
            FixedString128Bytes prefix = $""{{this.logs.Length, {logIdOffset}}} | {logTypeIdentifier,logTypeOffset} | "";
            prefix.Append(newLog);

            this.logs.Add(prefix);
            this.logTimeInfos.Add(timeData.ElapsedTime);
        }}
";

            return src;
        }

        private static void GenerateCreationMethods(in SourceProductionContext context, in ConcreteLoggerInfo concreteLoggerInfo)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("// < auto-generated />");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"using {fixedStringNamespace};");
            stringBuilder.AppendLine($"using {concreteLoggerInfo.LoggerNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {packageNamespace}");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"\tpublic partial struct {creationMethodsContainerName}");
            stringBuilder.AppendLine("\t{");

            stringBuilder.AppendLine($"\t\tpublic static {concreteLoggerInfo.LoggerName} Create{concreteLoggerInfo.LoggerName}(int initialCap, Allocator allocator, bool isBurstLogger = false) => new(initialCap, allocator, isBurstLogger); \n");

            stringBuilder.AppendLine("\t}");
            stringBuilder.AppendLine("}");

            context.AddSource($"{concreteLoggerInfo.LoggerName}.CreationMethod.g.cs", stringBuilder.ToString());
        }

    }

}
