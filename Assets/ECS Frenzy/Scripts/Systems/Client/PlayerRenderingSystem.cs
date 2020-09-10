using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
public class PlayerRenderingSystem : ComponentSystem {
  const int MAX_PLAYERS = 4;

  Dictionary<Entity, RenderedPlayer> entityToRendererMap = new Dictionary<Entity, RenderedPlayer>(MAX_PLAYERS);

  protected override void OnUpdate() {
    // get all the player entities found on this frame
    EntityQuery playersQuery = Entities.WithAll<NetworkPlayer, Translation, Rotation, MoveSpeed>().ToEntityQuery();
    using (NativeArray<Entity> playerEntities = playersQuery.ToEntityArray(Allocator.TempJob))
    using (NativeArray<Translation> translations = playersQuery.ToComponentDataArray<Translation>(Allocator.Temp))
    using (NativeArray<Rotation> rotations = playersQuery.ToComponentDataArray<Rotation>(Allocator.Temp))
    using (NativeArray<MoveSpeed> moveSpeeds = playersQuery.ToComponentDataArray<MoveSpeed>(Allocator.Temp))
    using (NativeList<Entity> expiredEntities = new NativeList<Entity>(Allocator.Temp)) {
      // check for entities that do not have a rendered player associated with them and instantiate one
      for (int i = 0; i < playerEntities.Length; i++) {
        Entity e = playerEntities[i];

        if (!entityToRendererMap.ContainsKey(playerEntities[i])) {
          RenderedPlayer rp = RenderedPlayer.Instantiate(SystemConfig.Instance.RenderedPlayerPrefab);

          entityToRendererMap.Add(e, rp);
        }
      }

      // check for entities stored in the dictionary that no longer exist and destroy them
      foreach (var existingEntity in entityToRendererMap.Keys) {
        if (!playerEntities.Contains(existingEntity)) {
            expiredEntities.Add(existingEntity);
        }
      }

      foreach (var expiredEntity in expiredEntities) {
        if (entityToRendererMap.TryGetValue(expiredEntity, out RenderedPlayer expiredRenderedPlayer)) {
          entityToRendererMap.Remove(expiredEntity);
          RenderedPlayer.Destroy(expiredRenderedPlayer);
        }
      }

      // Update the transforms of all rendered players that are still alive
      for (int i = 0; i < playerEntities.Length; i++) {
        Entity e = playerEntities[i];

        if (entityToRendererMap.TryGetValue(e, out RenderedPlayer rp)) {
          float3 position = translations[i].Value;
          quaternion rotation = rotations[i].Value;
          float moveSpeed = moveSpeeds[i].Value;

          rp.transform.SetPositionAndRotation(position, rotation);
          rp.Animator.SetFloat("Speed", moveSpeed);
        } else {
          UnityEngine.Debug.LogError($"Tried to find RenderedPlayer for Entity: {e} but did not exist. This should not happen.");
        }
      }
    }
  }
}