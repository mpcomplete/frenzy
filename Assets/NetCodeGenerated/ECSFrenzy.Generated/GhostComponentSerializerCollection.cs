//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using Unity.Entities;
using Unity.NetCode;
using ECSFrenzy.Generated;

namespace ECSFrenzy.Generated
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    public class GhostComponentSerializerRegistrationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            ghostCollectionSystem.AddSerializer(BaseGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(ChanneledBeamAbilityGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(CooldownGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(CooldownStatusGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(FireballAbilityGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(HeadingGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(MoveSpeedGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(PlayerStateGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(TeamGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(TriggerGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(TurnSpeedGhostComponentSerializer.State);
        }

        protected override void OnUpdate()
        {
            var parentGroup = World.GetExistingSystem<InitializationSystemGroup>();
            if (parentGroup != null)
            {
                parentGroup.RemoveSystemFromUpdateList(this);
            }
        }
    }
}