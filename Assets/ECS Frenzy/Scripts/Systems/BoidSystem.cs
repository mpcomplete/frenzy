using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
public class BoidSystem : SystemBase {
  public struct Neighbor {
    public Entity entity;
    public float3 position;
    public float3 heading;
  }

  public static int3 HashPosition(in float3 position, in float radius) {
    return int3(position / radius);
  }

  public static int PopulateNeighbors(
  Entity entity, 
  in float3 position, 
  in float maxRadius,
  in float maxRadiusSquared,
  in int maxNeighbors,
  in NativeMultiHashMap<int3, Neighbor> spatialHashMap, 
  NativeArray<Neighbor> neighbors) {
    int index = 0;
    int3 bi = HashPosition(position, maxRadius);

    for (int i = bi.x - 1; i < bi.x + 1; i++) {
      for (int j = bi.y - 1; j < bi.y + 1; j++) {
        for (int k = bi.z - 1; k < bi.z + 1; k++) {
          if (spatialHashMap.TryGetFirstValue(int3(i,j,k), out Neighbor neighbor, out NativeMultiHashMapIterator<int3> it)) {
            do {
              if (neighbor.entity != entity && distancesq(position, neighbor.position) < maxRadiusSquared)
              {
                neighbors[index++] = neighbor;
                if (index >= maxNeighbors) {
                  return index;
                }
              }
            } while (spatialHashMap.TryGetNextValue(out neighbor, ref it));
          }
        }
      }
    }
    return index;
  }

  public static float3 Alignment(in int neighborCount, in NativeArray<Neighbor> neighbors) {
    float3 alignment = float3(0,0,0);

    if (neighborCount == 0)
      return alignment;

    for (int i = 0; i < neighborCount; i++) {
      alignment += neighbors[i].heading;
    }
    alignment /= neighborCount;
    alignment = normalizesafe(alignment);
    return alignment;
  }

  public static float3 Cohesion(in int neighborCount, in NativeArray<Neighbor> neighbors, in float3 position) {
    float3 cohesion = float3(0,0,0);

    if (neighborCount == 0)
      return cohesion;

    for (int i = 0; i < neighborCount; i++) {
      cohesion += neighbors[i].position;
    }
    cohesion /= neighborCount;
    cohesion = normalizesafe(cohesion - position);
    return cohesion;
  }

  public static float3 Separation(in int neighborCount, in NativeArray<Neighbor> neighbors, in float3 position) {
    float3 separation = float3(0,0,0);

    if (neighborCount == 0)
      return separation;

    for (int i = 0; i < neighborCount; i++) {
      separation += neighbors[i].position - position;
    }
    separation /= neighborCount;
    separation *= -1;
    separation = normalizesafe(separation);
    return separation;
  }

  public static void RenderNeighbors(
  in int neighborCount, 
  in NativeArray<Neighbor> neighbors, 
  in float3 position) {
    for (int i = 0; i < neighborCount; i++) {
      Debug.DrawLine(position, neighbors[i].position, Color.green);
    }
  }

  protected override void OnUpdate() {
    const int SPATIAL_INDEX_CAPACITY = 1000000;
    const float MAX_RADIUS = 3f;
    const float MAX_RADIUS_SQ = MAX_RADIUS * MAX_RADIUS;
    const int MAX_NEIGHBORS = 32;
    const float ALIGNMENT_FACTOR = 1/8f;
    const float COHESION_FACTOR = 1/200f;
    const float SEPARATION_FACTOR = 1/10f;
    const float ATTRACTION_FACTOR = 1/1000f;

    float dt = Time.DeltaTime;
    NativeMultiHashMap<int3, Neighbor> spatialHashMap = new NativeMultiHashMap<int3, Neighbor>(SPATIAL_INDEX_CAPACITY, Allocator.TempJob);
    NativeMultiHashMap<int3, Neighbor>.ParallelWriter spatialHashMapParallelWriter = spatialHashMap.AsParallelWriter();

    Entities
    .WithName("Hash_position")
    .WithBurst()
    .WithDisposeOnCompletion(spatialHashMapParallelWriter)
    .WithAll<Boid>()
    .ForEach((Entity entity, in Translation translation, in Heading heading) => {
      Neighbor neighbor = new Neighbor {
        entity = entity,
        position = translation.Value,
        heading = heading.Value
      };
      spatialHashMapParallelWriter.Add(HashPosition(translation.Value, MAX_RADIUS), neighbor);
    }).ScheduleParallel();

    Entities
    .WithName("Steer")
    .WithBurst()
    .WithReadOnly(spatialHashMap)
    .WithDisposeOnCompletion(spatialHashMap)
    .WithAll<Boid>()
    .ForEach((Entity entity, ref Heading heading, in Translation translation) => {
      NativeArray<Neighbor> neighbors = new NativeArray<Neighbor>(MAX_NEIGHBORS, Allocator.Temp);
      int3 key = HashPosition(translation.Value, MAX_RADIUS);
      int neighborCount = PopulateNeighbors(entity, translation.Value, MAX_RADIUS, MAX_RADIUS_SQ, MAX_NEIGHBORS, spatialHashMap, neighbors);
      float3 alignment = Alignment(neighborCount, neighbors) * ALIGNMENT_FACTOR;
      float3 cohesion = Cohesion(neighborCount, neighbors, translation.Value) * COHESION_FACTOR;
      float3 separation = Separation(neighborCount, neighbors, translation.Value) * SEPARATION_FACTOR;
      float3 attraction = -translation.Value * ATTRACTION_FACTOR;
      float3 correction = alignment + cohesion + separation + attraction;
      float3 newHeading = normalizesafe(heading.Value + correction);

      heading.Value = newHeading;
      neighbors.Dispose();
    }).ScheduleParallel();

    Entities
    .WithName("Orient_to_heading")
    .WithBurst()
    .WithAll<Boid>()
    .ForEach((ref Rotation rotation, in Heading heading) => {
      rotation.Value = Quaternion.LookRotation(heading.Value, float3(0, 1, 0));
    }).ScheduleParallel();

    Entities
    .WithName("Move_forward")
    .WithBurst()
    .WithAll<Boid>()
    .ForEach((ref Translation translation, in Heading heading, in MoveSpeed moveSpeed) => {
      translation.Value += dt * moveSpeed.Value * heading.Value;
    }).ScheduleParallel();
  }
}