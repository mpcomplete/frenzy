using Unity.Entities;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  public struct PlayerAbilites : IComponentData {
    public Entity Ability1;
    public Entity Ability2;
    public Entity Ability3;
    public Entity Ability4;
  }
}