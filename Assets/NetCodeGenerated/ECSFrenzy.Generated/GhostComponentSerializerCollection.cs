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
            ghostCollectionSystem.AddSerializer(HeadingGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(MoveSpeedGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(TurnSpeedGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(ECSFrenzyBaseGhostComponentSerializer.State);
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