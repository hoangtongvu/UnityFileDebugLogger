# UnityFileDebugLogger

## Introduction

**UnityFileDebugLogger** is a simple Unity logger that logs messages to Table-formatted txt file.

## Installation

To install, paste the following URL into Unity's **Package Manager**:

1. Open **Package Manager**.
2. Click the **+** button.
3. Select **"Add package from git URL..."**.
4. Enter the following URL:
```bash
https://github.com/hoangtongvu/UnityFileDebugLogger.git?path=/Assets/UnityFileDebugLogger
```

## How to use

<details>
  <summary>General logging usecase</summary>

See [LogTestSystemBase](Assets/Scripts/Systems/Initialization/LogTestSystemBase.cs) for full details.

### 1. Create a Logger
Choose your appropriate `FixedString` size and create the logger:

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
fileDebugLogger.Log("This is a normal log.");
fileDebugLogger.LogWarning("This is a warning log.");
fileDebugLogger.LogError("This is an error log.");
```

### 3. Finally, save the log

```cs
fileDebugLogger.Save("logfileName.txt");
```

The log will be saved to the `FileDebugLoggerLogs` folder:
```bash
%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProjectName>\FileDebugLoggerLogs\
```

**Note**: You can use any file extension, but logs will always follow this structure:
```pgsql
TimeStamp | Id | LogType | Log
```

</details>

<details>
  <summary>BurstCompile logging usecase</summary>
  
See [LogTestISystemWithBurst example](Assets/Scripts/Systems/Initialization/LogTestISystemWithBurst.cs) for full details.

### 1. Create a logger

```cs
[BurstCompile]
public partial struct LogTestISystemWithBurst : ISystem, ISystemStartStop
{
    private Logger128Bytes fileDebugLogger;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        this.fileDebugLogger = FileDebugLogger.CreateLogger128Bytes(10, Allocator.Persistent, true);
    }
}
```

### 2. Add logs

```cs
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    this.fileDebugLogger.Log(in timeData, $"This is a normal log.");
}
```

### 3. Finally, save the log

`Save()` is normally placed in `OnStopRunning()` or `OnDestroy()` without `[BurstCompile]` because it involves `I/O operations` and `System.DateTime.Now` access.

```cs
public void OnStopRunning(ref SystemState state)
{
    this.fileDebugLogger.Save("TestISystemLogs.txt", in SystemAPI.Time);
    this.fileDebugLogger.Dispose();
}
```

</details>