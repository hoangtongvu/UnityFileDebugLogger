using Unity.Collections;

namespace UnityFileDebugLogger.ConcreteLoggers
{
    public partial struct Logger128Bytes : IFileDebugLogger<FixedString128Bytes> { }
      
}

//using System.Text;
//using Unity.Burst;
//using Unity.Collections;

//namespace UnityFileDebugLogger
//{
//    [MainFileDebugLoggerContainer]
//    public struct FileDebugLogger
//    {
//        public static Logger128Bytes CreateLogger128Bytes(int initialCap, Allocator allocator) => new(initialCap, allocator);

//        public partial struct Logger128Bytes : IFileDebugLogger<FixedString128Bytes>
//        {
//            private static readonly FixedString32Bytes logHeader = "TimeStamp, Id, LogType, Log";
//            private static readonly FixedString32Bytes defaultLogDirectory = "FileDebugLoggerLogs/";
//            public readonly NativeList<FixedString128Bytes> logs;

//            public Logger128Bytes(int initialCap, Allocator allocator) => this.logs = new(initialCap, allocator);

//            public readonly void Dispose() => this.logs.Dispose();

//            [BurstDiscard]
//            public readonly void Log(in FixedString128Bytes newLog) => this.BaseLog(in newLog, LogType.Log);

//            [BurstDiscard]
//            public readonly void LogWarning(in FixedString128Bytes newLog) => this.BaseLog(in newLog, LogType.Warning);

//            [BurstDiscard]
//            public readonly void LogError(in FixedString128Bytes newLog) => this.BaseLog(in newLog, LogType.Error);

//            private readonly void BaseLog(in FixedString128Bytes newLog, LogType logType)
//            {
//                FixedString128Bytes prefix = $"{System.DateTime.Now}, {this.logs.Length}, {logType}, ";
//                prefix.Append(newLog);

//                this.logs.Add(prefix);
//            }

//            [BurstDiscard]
//            public readonly void Save(in FixedString64Bytes fileName, bool append = false)
//            {
//                int length = this.logs.Length;

//                StringBuilder stringBuilder = new();
//                stringBuilder.AppendLine(logHeader.ToString());

//                for (int i = 0; i < length; i++)
//                {
//                    var log = this.logs[i];
//                    stringBuilder.AppendLine(log.ToString());
//                }

//                FileWriter.Write(defaultLogDirectory + fileName.ToString(), stringBuilder.ToString(), append);

//            }

//        }

//    }

//}
