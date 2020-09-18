using Unity.Entities;
using UnityEngine;

namespace ECSFrenzy {
  [RequiresEntityConversion]
  public class CooldownWithStatusAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
    public float Duration;
    public float TimeRemaining;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
      var initialStatus = TimeRemaining > 0 ? SharedCooldownStatus.Active : SharedCooldownStatus.Elapsed;

      dstManager.AddSharedComponentData<SharedCooldownStatus>(entity, initialStatus);
      dstManager.AddComponentData<Cooldown>(entity, new Cooldown { Duration = Duration, TimeRemaining = TimeRemaining });
    }
  }
}