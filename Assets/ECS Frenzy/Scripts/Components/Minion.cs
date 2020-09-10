using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Minion : IComponentData {
  }

  [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
  public class MinionSystem : ComponentSystem {
    protected override void OnUpdate() {
      Entities.ForEach((ref Minion minion, ref Translation translate, ref Rotation rot) => {
        const float speed = 5;
        Vector3 velocity = new Vector3(0, 0, 1) * Time.DeltaTime * speed;
        Quaternion q = rot.Value;
        Vector3 move = q * velocity;
        translate.Value += (float3)move;
      });
    }
  }
}
