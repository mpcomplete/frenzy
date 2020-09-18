using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSFrenzy {
  [DisallowMultipleComponent]
  [RequiresEntityConversion]
  public class CooldownAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
    public float Duration;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
      dstManager.AddComponentData<Cooldown>(entity, new Cooldown { Duration = Duration, TimeRemaining = 0 });
      dstManager.AddSharedComponentData<SharedCooldownStatus>(entity, SharedCooldownStatus.Elapsed);
    }
  }
}