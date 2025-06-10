using Unity.Collections;
using Unity.Entities;
using UnityFileDebugLogger;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LogTestSystemBase : SystemBase
    {
        protected override void OnCreate()
        {
        }

        protected override void OnStopRunning()
        {
            FixedString128Bytes msg = $"TestSystemBaseLogs.csv created, final ElapsedTime: {SystemAPI.Time.ElapsedTime}";
            UnityEngine.Debug.Log(msg);
        }

        protected override void OnUpdate()
        {
            this.Enabled = false;
            const int loopCount = 5;

            var fileDebugLogger = FileDebugLogger.CreateLogger4096Bytes(10, Allocator.Temp);

            for (int i = 0; i < loopCount; i++)
            {
                fileDebugLogger.Log("This is normal log.");
                fileDebugLogger.LogWarning("This is warning log.");
                fileDebugLogger.LogError("This is error log.");
            }

            fileDebugLogger.Save("TestSystemBaseLogs.csv");

        }

    }

}