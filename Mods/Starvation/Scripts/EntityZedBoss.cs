using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public class EntityZedBoss : EntityZombie
{
    private float meshScale = 1;
    private string habilityBuff = "";
    private string habilitySound = "";
    private int spawnPause = 60;
    private int spawnToDelay = 3;
    private int timesSpawned = 0;
    private int habilityDelay = 20;
    private int habilityChance = 20;
    private HabilityType bossHability = HabilityType.None;
    private string spawnGroup = "";
    private int numToSpawn = 0;
    private int checkArea = 5; // small area by default
    private DateTime nextHability = DateTime.MaxValue;
    private bool debug = false;
    private enum HabilityType
    {
        None,
        Regen,
        Spawn,
        Rez
    }

    public EntityZedBoss() : base()
    {

    }

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        EntityClass entityClass = EntityClass.list[_entityClass];
        if (entityClass.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = entityClass.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            float minScale = 1;
            float maxScale = 1;

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }

            meshScale = UnityEngine.Random.Range(minScale, maxScale);
            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
        }
        if (entityClass.Properties.Values.ContainsKey("Hability"))
        {
            string auxHab = entityClass.Properties.Values["Hability"];
            if (auxHab == "Regen") bossHability = HabilityType.Regen;
            else if (auxHab == "Spawn") bossHability = HabilityType.Spawn;
            else if (auxHab == "Rez") bossHability = HabilityType.Rez;
        }
        if (entityClass.Properties.Values.ContainsKey("Group"))
        {
            spawnGroup = entityClass.Properties.Values["Group"];
        }
        if (entityClass.Properties.Values.ContainsKey("Area"))
        {
            int.TryParse(entityClass.Properties.Values["Area"], out checkArea);
        }
        if (entityClass.Properties.Values.ContainsKey("Number"))
        {
            int.TryParse(entityClass.Properties.Values["Number"], out numToSpawn);
        }
        if (entityClass.Properties.Values.ContainsKey("HabilityChance"))
        {
            int.TryParse(entityClass.Properties.Values["HabilityChance"], out habilityChance);
        }
        if (entityClass.Properties.Values.ContainsKey("HabilityBuff"))
        {
            habilityBuff = entityClass.Properties.Values["HabilityBuff"];
        }
        if (entityClass.Properties.Values.ContainsKey("HabilitySound"))
        {
            habilitySound = entityClass.Properties.Values["HabilitySound"];
        }
        nextHability = DateTime.Now.AddSeconds(5);
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();
        if (world.IsRemote()) return;
        if (!IsAlive() || IsDead()) return;
        if (bossHability==HabilityType.None) return;    
        // only does the habilities if he is alert or approaching a player
        if (!IsAlert && !ApproachingPlayer) return;
        if (IsBreakingBlocks || IsBreakingDoors || Climbing) return;
        if (DateTime.Now <= nextHability) return;
        nextHability = DateTime.Now.AddSeconds(5); // at least 5 seconds between "tries"
        //chance of doing hability move
        if (rand.Next(1, 101) < habilityChance)
        {
           debugHelper.doDebug("Doing hability", debug);
            nextHability = DateTime.Now.AddSeconds(habilityDelay); // delay between hability
            if (bossHability == HabilityType.Regen)
            {
                if (Health >= GetMaxHealth()) return;
                debugHelper.doDebug("Recovering CURRENT HP = " + Health, debug);
                if (habilitySound != "") Audio.Manager.BroadcastPlay((this as Entity), habilitySound);
                // apply regeneration buff
                MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction(habilityBuff);
                multiBuffClassAction.Execute(this.entityId, (EntityAlive)this,
                    false,
                    EnumBodyPartHit.Torso, (string)null);
            }
            else if (bossHability == HabilityType.Spawn)
            {
                timesSpawned++;
                if (timesSpawned >= spawnToDelay)
                {
                    timesSpawned = 0;
                    nextHability = DateTime.Now.AddSeconds(spawnPause); // pauses for a longer period then usual
                }
                if (habilitySound != "") Audio.Manager.BroadcastPlay((this as Entity), habilitySound);
                MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction(habilityBuff);
                multiBuffClassAction.Execute(this.entityId, (EntityAlive)this,
                    false,
                    EnumBodyPartHit.Torso, (string)null);
                SpawnZedGroup();
            }
            else if (bossHability == HabilityType.Rez)
            {
                // look for a dead zed in the area, and rez it.
                if (habilitySound != "") Audio.Manager.BroadcastPlay((this as Entity), habilitySound);
                MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction(habilityBuff);
                multiBuffClassAction.Execute(this.entityId, (EntityAlive)this,
                    false,
                    EnumBodyPartHit.Torso, (string)null);
                using (
                        List<Entity>.Enumerator enumerator =
                            this.world.GetEntitiesInBounds(typeof(EntityZombie),
                                BoundsUtils.BoundsForMinMax(this.position.x - GetSeeDistance(), this.position.y - GetSeeDistance(),
                                    this.position.z - GetSeeDistance(), this.position.x + GetSeeDistance(),
                                    this.position.y + GetSeeDistance(),
                                    this.position.z + GetSeeDistance())).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        EntityZombie _other = enumerator.Current as EntityZombie;
                        //if (_other.IsDead() && !(_other is EntityAnimalClown))
                        if (_other.IsDead())
                        {
                            if (!(_other is EntityBipedZCop) && !(_other is EntityZombieDog) &&
                                !(_other is EntityDogMorte))
                            {
                                if ((_other is EntityAnimalClown) || (_other is EntityZedBoss))
                                {
                                    debugHelper.doDebug("Rezzing a custom zed!", debug);
                                    // spawn a new one on its place, and applies the buff
                                    
                                    Entity spawnEntity = EntityFactory.CreateEntity(_other.entityClass, _other.position);
                                    spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
                                    _other.MarkToUnload();
                                    GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);                                    
                                    return;
                                }
                                else
                                {
                                    debugHelper.doDebug("Rezzing a zed!", debug);
                                    _other.SetAlive();
                                    return;
                                }
                                //multiBuffClassAction = MultiBuffClassAction.NewAction("ZedRez");
                                //multiBuffClassAction.Execute(this.entityId, (EntityAlive)_other,
                                //    false,
                                //    EnumBodyPartHit.Torso, (string)null);                                
                            }
                        }
                    }
                }
            }
        }
        else Debug.Log("Hability didn't trigger");
    }

    private void SpawnZedGroup()
    {
        // spawns a EntityGroup of zeds
        try
        {
            if (spawnGroup != "" && numToSpawn > 0 && checkArea > 0)
            {
                int x;
                int y;
                int z;
                int i = 0;
                while (i < numToSpawn)
                {
                    if (GameManager.Instance.World.FindRandomSpawnPointNearPosition(
                        this.position, 15, out x, out y, out z,
                        new Vector3(checkArea, checkArea, checkArea), true, false))
                    {
                        int entityID = EntityGroups.GetRandomFromGroup(spawnGroup);
                        Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                            new Vector3((float) x, (float) y, (float) z));
                        spawnEntity.SetSpawnerSource(EnumSpawnerSource.Unknown);
                        GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                    }
                    i++;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("SURVIVOR - error spawning: " + ex.ToString());
        }
    }

    protected override void Awake()
    {
        base.Awake();
        
    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {
        
        int ret = base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
        return ret;
    }
    public override bool IsImmuneToLegDamage
    {
        get { return false; }
    }
    protected override DamageResponse damageEntityLocal(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {

        this.Health -= _strength;
        DamageResponse ret =  base.damageEntityLocal(_damageSource, _strength, _criticalHit, impulseScale);
     //   Debug.Log("Response: " + ret.ToString() + " strength: " + _strength + " type: " + _damageSource.GetName().ToString() + " health: " + this.Health);
        return ret;

    }
    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.45f, 0.45f, 1f);
    }
}