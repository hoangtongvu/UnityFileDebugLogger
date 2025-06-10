using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityFileDebugLogger;
using UnityFileDebugLogger.ConcreteLoggers;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class FixedStepInitializationSystemGroup : ComponentSystemGroup
    {
        public FixedStepInitializationSystemGroup()
        {
            this.RateManager = new RateUtils.VariableRateManager(50); // This SystemGroup will be updated once every 0.05s.
        }
    }

    [UpdateInGroup(typeof(FixedStepInitializationSystemGroup))]
    [BurstCompile]
    public partial struct LogTestISystemWithBurst : ISystem, ISystemStartStop
    {
        private Logger128Bytes fileDebugLogger;
        private float timeCounterSeconds;
        private float timeLimitSeconds;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.fileDebugLogger = FileDebugLogger.CreateLogger128Bytes(10, Allocator.Persistent, true);
            this.timeCounterSeconds = 0f;
            this.timeLimitSeconds = 2f;
        }

        public void OnStartRunning(ref SystemState state)
        {
        }

        public void OnStopRunning(ref SystemState state)
        {
            this.fileDebugLogger.Save("TestISystemLogs.csv", in SystemAPI.Time);
            this.fileDebugLogger.Dispose();

            FixedString128Bytes msg = $"TestISystemLogs.csv created, final ElapsedTime: {SystemAPI.Time.ElapsedTime}";
            UnityEngine.Debug.Log(msg);

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Simulate time delay
            this.timeCounterSeconds += SystemAPI.Time.DeltaTime;
            var timeData = SystemAPI.Time;

            this.fileDebugLogger.Log(in timeData, $"This is a normal log before {this.timeLimitSeconds}s.");

            if (this.timeCounterSeconds < this.timeLimitSeconds) return;

            state.Enabled = false;

            this.fileDebugLogger.Log(in timeData, $"This is a normal log after {this.timeLimitSeconds}s.");
            this.fileDebugLogger.LogWarning(in timeData, $"This is a warning log after {this.timeLimitSeconds}s.");
            this.fileDebugLogger.LogError(in timeData, $"This is an error log after {this.timeLimitSeconds}s.");

        }

    }

}