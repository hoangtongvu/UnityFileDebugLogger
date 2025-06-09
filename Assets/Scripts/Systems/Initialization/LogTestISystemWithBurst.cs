using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityFileDebugLogger;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial struct LogTestISystemWithBurst : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            const int loopCount = 5;

            // Note: BurstCompile enabled = No log

            var fileDebugLogger = FileDebugLogger.CreateLogger4096Bytes(10, Allocator.Temp);

            for (int i = 0; i < loopCount; i++)
            {
                fileDebugLogger.Log("This is normal log.");
                fileDebugLogger.LogWarning("This is warning log.");
                fileDebugLogger.LogError("This is error log.");
            }

            fileDebugLogger.Save("TestISystemLogs.csv");

        }

    }

}