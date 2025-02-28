using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PersonSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PersonSpawnerComponent>();
        RequireForUpdate<RandomComponent>();
    }
    
    protected override void OnUpdate()
    {
        EntityQuery peopleEntityQuery = EntityManager.CreateEntityQuery(typeof(PeopleTag));
        PersonSpawnerComponent peopleSpawnerComponent = SystemAPI.GetSingleton<PersonSpawnerComponent>();

        if (peopleEntityQuery.CalculateEntityCount() < peopleSpawnerComponent.MaxNbPeople)
        {
            RefRW<RandomComponent> randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
            EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            // Prefab
            EntityQuery prefabQuery = EntityManager.CreateEntityQuery(typeof(PersonPrefab));
            NativeArray<PersonPrefab> prefabs = prefabQuery.ToComponentDataArray<PersonPrefab>(Allocator.Temp);

            int randomPrefabIndex = randomComponent.ValueRW.Random.NextInt(0, prefabs.Length);
            Entity prefab = prefabs[randomPrefabIndex].Value;
            Entity spawnedEntity = entityCommandBuffer.Instantiate(prefab);

            // Spawn zone
            EntityQuery spawnQuery = EntityManager.CreateEntityQuery(typeof(PersonZone));
            NativeArray<PersonZone> spawnZones = spawnQuery.ToComponentDataArray<PersonZone>(Allocator.Temp);

            int randomZoneIndex = randomComponent.ValueRW.Random.NextInt(0, spawnZones.Length);
            PersonZone zone = spawnZones[randomZoneIndex];

            // Start position
            float3 startPosition = GetSpawnPosition(zone, randomComponent);

            // Set Start position
            entityCommandBuffer.SetComponent(spawnedEntity, new LocalTransform
            {
                Position = startPosition,
                Rotation = quaternion.identity,
                Scale = 1
            });

            // Change the speed of the person
            entityCommandBuffer.SetComponent(spawnedEntity, new Speed
            {
                Value = randomComponent.ValueRW.Random.NextFloat(peopleSpawnerComponent.PeopleMinSpeed, peopleSpawnerComponent.PeopleMaxSpeed)
            });
          
            // Change destination
            entityCommandBuffer.SetComponent(spawnedEntity, new TargetPosition
            {
                Value = startPosition + new float3(0.1f, 0, 0.1f)
            });

            // Set the zone in which it should stay
            entityCommandBuffer.SetComponent(spawnedEntity, new MovementZoneIndex
            {
                MovementIndex = randomZoneIndex
            });
            
            prefabs.Dispose();
            spawnZones.Dispose();
        }
    }

    private float3 GetSpawnPosition(PersonZone zone, RefRW<RandomComponent> randomComponent)
    {
        float startPosX = randomComponent.ValueRW.Random.NextFloat(zone.SpawnCenterZone.x - (zone.SizeXZone / 2), zone.SpawnCenterZone.x + (zone.SizeXZone / 2));
        float startPosZ = randomComponent.ValueRW.Random.NextFloat(zone.SpawnCenterZone.z - (zone.SizeZZone / 2), zone.SpawnCenterZone.z + (zone.SizeZZone / 2));

        return new float3(startPosX, 0, startPosZ);
    }
}
