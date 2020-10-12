//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using System;
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

namespace ECSFrenzy.Generated
{
    [BurstCompile]
    public struct ChanneledBeamAbilityGhostComponentSerializer
    {
        static ChanneledBeamAbilityGhostComponentSerializer()
        {
            State = new GhostComponentSerializer.State
            {
                GhostFieldsHash = 14767913548786401661,
                ExcludeFromComponentCollectionHash = 0,
                ComponentType = ComponentType.ReadWrite<ChanneledBeamAbility>(),
                ComponentSize = UnsafeUtility.SizeOf<ChanneledBeamAbility>(),
                SnapshotSize = UnsafeUtility.SizeOf<Snapshot>(),
                ChangeMaskBits = ChangeMaskBits,
                SendMask = GhostComponentSerializer.SendMask.Predicted,
                SendForChildEntities = 1,
                CopyToSnapshot =
                    new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyToSnapshot),
                CopyFromSnapshot =
                    new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyFromSnapshot),
                RestoreFromBackup =
                    new PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate>(RestoreFromBackup),
                PredictDelta = new PortableFunctionPointer<GhostComponentSerializer.PredictDeltaDelegate>(PredictDelta),
                CalculateChangeMask =
                    new PortableFunctionPointer<GhostComponentSerializer.CalculateChangeMaskDelegate>(
                        CalculateChangeMask),
                Serialize = new PortableFunctionPointer<GhostComponentSerializer.SerializeDelegate>(Serialize),
                Deserialize = new PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate>(Deserialize),
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                ReportPredictionErrors = new PortableFunctionPointer<GhostComponentSerializer.ReportPredictionErrorsDelegate>(ReportPredictionErrors),
                #endif
            };
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            State.NumPredictionErrorNames = GetPredictionErrorNames(ref State.PredictionErrorNames);
            #endif
        }
        public static readonly GhostComponentSerializer.State State;
        public struct Snapshot
        {
            public int ChanneledBeam;
            public uint ChanneledBeamSpawnTick;
        }
        public const int ChangeMaskBits = 1;
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyToSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData, snapshotOffset + snapshotStride*i);
                ref var component = ref GhostComponentSerializer.TypeCast<ChanneledBeamAbility>(componentData, componentStride*i);
                ref var serializerState = ref GhostComponentSerializer.TypeCast<GhostSerializerState>(stateData, 0);
                snapshot.ChanneledBeam = 0;
                snapshot.ChanneledBeamSpawnTick = 0;
                if (serializerState.GhostFromEntity.HasComponent(component.ChanneledBeam))
                {
                    var ghostComponent = serializerState.GhostFromEntity[component.ChanneledBeam];
                    snapshot.ChanneledBeam = ghostComponent.ghostId;
                    snapshot.ChanneledBeamSpawnTick = ghostComponent.spawnTick;
                }
            }
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyFromSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshotInterpolationData = ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(snapshotData, snapshotStride*i);
                ref var snapshotBefore = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotBefore, snapshotOffset);
                ref var snapshotAfter = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotAfter, snapshotOffset);
                float snapshotInterpolationFactor = snapshotInterpolationData.InterpolationFactor;
                ref var component = ref GhostComponentSerializer.TypeCast<ChanneledBeamAbility>(componentData, componentStride*i);
                var deserializerState = GhostComponentSerializer.TypeCast<GhostDeserializerState>(stateData, 0);
                deserializerState.SnapshotTick = snapshotInterpolationData.Tick;
                component.ChanneledBeam = Entity.Null;
                if (snapshotBefore.ChanneledBeam != 0)
                {
                    if (deserializerState.GhostMap.TryGetValue(new SpawnedGhost{ghostId = snapshotBefore.ChanneledBeam, spawnTick = snapshotBefore.ChanneledBeamSpawnTick}, out var ghostEnt))
                        component.ChanneledBeam = ghostEnt;
                }
            }
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        private static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<ChanneledBeamAbility>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<ChanneledBeamAbility>(backupData, 0);
            component.ChanneledBeam = backup.ChanneledBeam;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        private static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline1 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline1Data);
            ref var baseline2 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline2Data);
            snapshot.ChanneledBeam = predictor.PredictInt(snapshot.ChanneledBeam, baseline1.ChanneledBeam, baseline2.ChanneledBeam);
            snapshot.ChanneledBeamSpawnTick = (uint)predictor.PredictInt((int)snapshot.ChanneledBeamSpawnTick, (int)baseline1.ChanneledBeamSpawnTick, (int)baseline2.ChanneledBeam);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CalculateChangeMaskDelegate))]
        private static void CalculateChangeMask(IntPtr snapshotData, IntPtr baselineData, IntPtr bits, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask;
            changeMask = (snapshot.ChanneledBeam != baseline.ChanneledBeam || snapshot.ChanneledBeamSpawnTick != baseline.ChanneledBeamSpawnTick) ? 1u : 0;
            GhostComponentSerializer.CopyToChangeMask(bits, changeMask, startOffset, 1);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        private static void Serialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamWriter writer, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
            {
                writer.WritePackedIntDelta(snapshot.ChanneledBeam, baseline.ChanneledBeam, compressionModel);
                writer.WritePackedUIntDelta(snapshot.ChanneledBeamSpawnTick, baseline.ChanneledBeamSpawnTick, compressionModel);
            }
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        private static void Deserialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
            {
                snapshot.ChanneledBeam = reader.ReadPackedIntDelta(baseline.ChanneledBeam, compressionModel);
                snapshot.ChanneledBeamSpawnTick = reader.ReadPackedUIntDelta(baseline.ChanneledBeamSpawnTick, compressionModel);
            }
            else
            {
                snapshot.ChanneledBeam = baseline.ChanneledBeam;
                snapshot.ChanneledBeamSpawnTick = baseline.ChanneledBeamSpawnTick;
            }
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        private static void ReportPredictionErrors(IntPtr componentData, IntPtr backupData, ref UnsafeList<float> errors)
        {
        }
        private static int GetPredictionErrorNames(ref FixedString512 names)
        {
            int nameCount = 0;
            return nameCount;
        }
        #endif
    }
}