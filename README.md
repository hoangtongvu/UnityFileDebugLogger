# UnityFileDebugLogger

## Introduction

**UnityFileDebugLogger** is a simple Unity logger that logs messages to CSV-formatted file.

## Installation

To install, paste the following URL into Unity's **Package Manager**:

1. Open **Package Manager**.
2. Click the **+** button.
3. Select **"Add package from git URL..."**.
4. Enter the following url:
```bash
https://github.com/hoangtongvu/UnityFileDebugLogger.git?path=/Assets/Scripts/com.darksun.unity-file-debug-logger
```

## How to use

See example usage in:
- [LogTestSystemBase](Assets/Scripts/Systems/Initialization/LogTestSystemBase.cs)
- [LogTestISystemWithBurst](Assets/Scripts/Systems/Initialization/LogTestISystemWithBurst.cs)

### 1. Create a Logger
Choose your approriate `FixedString` size and create the logger:

```cs
var fileDebugLogger = FileDebugLogger.CreateLogger4096Bytes(initialCap, allocator);
```

**Parameters**:
- `initialCap`: Initial capacity of the internal `NativeList`. Each log entry adds to this list.
- `allocator`: The Unity `Allocator` used for the internal `NativeList`.

Supported options:
- `CreateLogger128Bytes()` for `FixedString128Bytes` logger
- `CreateLogger512Bytes()` for `FixedString512Bytes` logger
- `CreateLogger4096Bytes()` for `FixedString4096Bytes` logger


### 2. Add logs

You can save logs to the internal `NativeList` using the following methods:

```cs
fileDebugLogger.Log("This is normal log.");
fileDebugLogger.LogWarning("This is warning log.");
fileDebugLogger.LogError("This is error log.");
```

### 3. Finally, save the log

```cs
fileDebugLogger.Save("logfileName.csv");
```

The log will be saved to the `FileDebugLoggerLogs` folder:
```bash
%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProjectName>\FileDebugLoggerLogs\
```

**Note**: You can use any file extension, but logs will always follow this CSV structure:
```pgsql
TimeStamp, Id, LogType, Log
```

## Important Notes

- This plug-in **is not Burst Compilable** due to `I/O operations`.
- It is **safe** to place log code inside `[BurstCompile]` methods, but the log file will only written when **Burst Compilation is disabled**.