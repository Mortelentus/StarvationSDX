using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

public class SurvivorScript : MonoBehaviour
{
    // for the survivor just runs periodically to check for food/sleep
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    private int checkInterval = 30;
    private int checkArea = 10;
    private string foodLow = "";
    private string foodMed = "";
    private string foodHigh = "";
    private string foodContainer = "";
    DateTime nextSleepCheck = DateTime.MinValue;
    DateTime nextFoodCheck = DateTime.MinValue;
    List<Vector3i> foodContainers = new List<Vector3i>();
    private int sleepFood = 0;
    private SurvivorHelper svHelper = new SurvivorHelper();
    private DateTime nextRandomSound = DateTime.MinValue;
    private BlockValue oldBlockValue = BlockValue.Air;
    private bool debug = false;

    // initialization
    void Start()
    {
        // TODO: emit noise heat map - the more survivors, the bigger the noise. 
    }

    // initializes some variable like block position, world, chunk
    public void init(WorldBase _world, Vector3i _blockPos, int _cIdx, int _checkInterval, int _checkArea,
        string _foodContainer, string _foodLow, string _foodMed, string _foodHigh)
    {
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        if (_checkInterval > 0) checkInterval = _checkInterval;
        if (_checkArea > 0) checkArea = _checkArea;
        foodLow = _foodLow;
        foodMed = _foodMed;
        foodHigh = _foodHigh;
        foodContainer = _foodContainer;
        System.Random _rndSound = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
        nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
    }

    // periodic function
    void Update()
    {
        // looks for food on a interval
        // check time of day to sleep (they will sleep from 22:00 - 07:00 randomly)
        // may fall a sleep or wakeup at any time between those hours.
        string pos = "0";
        try
        {
            if (world != null)
            {
                BlockValue blockValue = world.GetBlock(cIdx, blockPos);
                System.Random _rnd = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                pos = "1";
                // META2:
                //  bit 0 -> sleeping
                // META1: character type:
                // value = x where x is the asset transform to activate
                // activate the correct object and deactivates all others
                Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(true);
                pos = "2";
                if (componentsInChildren == null)
                {
                    debugHelper.doDebug("SurvivorScript: Imporssible to find any transforms", true);
                }
                else
                {
                    foreach (Transform tra in componentsInChildren)
                    {
                        if (tra.name.StartsWith("char"))
                        {
                            if (tra.name == string.Format("char{0}", blockValue.meta.ToString()))
                            {
                                tra.gameObject.SetActive(true);
                            }
                            else tra.gameObject.SetActive(false);
                        }
                    }
                    if (!GameManager.IsDedicatedServer)
                    {
                        // force animation
                        if (svHelper.IsSleeping(blockValue.meta2))
                        {
                            if (svHelper.IsSleeping(oldBlockValue.meta2))
                                svHelper.ForceState(gameObject, blockPos, cIdx, "sleeping", "Sleep", "");
                            else
                            {
                                // play transition
                                svHelper.SleepChar(componentsInChildren, pos, blockValue, blockPos, cIdx, world);
                            }
                        }
                        else if (!svHelper.IsSleeping(blockValue.meta2))
                        {
                            if (!svHelper.IsSleeping(oldBlockValue.meta2))
                                svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "WakeUp", "");
                            else
                            {
                                // play transition
                                svHelper.WakeupChar(componentsInChildren, pos, blockValue, blockPos, cIdx, world);
                            }
                        }
                    }
                    pos = "3";
                    if (DateTime.Now > nextSleepCheck)
                    {
                        debugHelper.doDebug("SurvivorScript: CHECK meta2: " + blockValue.meta2, debug);
                        nextSleepCheck = DateTime.Now.AddSeconds(_rnd.Next(7, 30));
                            // from 1 to 30 seconds, just so that not all change at the same time
                        int sleepProb = 0;
                        int wakeProb = 0;
                        // this will only run on the server
                        // the server decides if it is supposed to be sleeping or not
                        if (!world.IsRemote())
                        {
                            #region sleep check;                        

                            if (!GameManager.Instance.World.IsDaytime())
                            {
                                if (!svHelper.IsSleeping(blockValue.meta2))
                                    sleepProb = 80; // 80% chance of sleeping during the night
                                else
                                    wakeProb = 5; // 5% chance of waking up during the night                            
                            }
                            else if (GameManager.Instance.World.IsDaytime() && svHelper.IsSleeping(blockValue.meta2))
                            {
                                if (!svHelper.IsSleeping(blockValue.meta2))
                                    sleepProb = 5; // 5% chance of sleeping during the day
                                else wakeProb = 80; // 80% chance of waking up during the day
                            }
                            int rndCheck = _rnd.Next(1, 101);
                            if (rndCheck <= sleepProb && sleepProb > 0)
                            {
                                pos = "4";
                                // fall asleep if not sleeping
                                pos = svHelper.SleepChar(componentsInChildren, pos, blockValue, blockPos, cIdx, world);
                                nextSleepCheck = DateTime.Now.AddSeconds(30);
                            }
                            else if (rndCheck <= wakeProb && wakeProb > 0)
                            {
                                pos = "5";
                                // wakeup if sleeping
                                pos = svHelper.WakeupChar(componentsInChildren, pos, blockValue, blockPos, cIdx, world);
                                nextSleepCheck = DateTime.Now.AddSeconds(30);
                            }

                            #endregion;
                        }
                        if (!GameManager.IsDedicatedServer)
                        {
                            if (svHelper.IsSleeping(blockValue.meta2))
                            {
                                string sound = "sleep_male_sound";
                                if (blockValue.meta > 1) sound = "sleep_female_sound";
                                debugHelper.doDebug(string.Format("SurvivorScript: PLAY SOUND {0}", sound), debug);
                                //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                                Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                            }
                        }
                    }
                }
                // this will only run on the server
                // the server decides if he needs food or not
                if (!world.IsRemote())
                {
                    #region food check;

                    if (DateTime.Now > nextFoodCheck)
                    {
                        nextFoodCheck = DateTime.Now.AddSeconds(checkInterval);
                        int currentDmg = blockValue.damage;
                        int maxDmg = Block.list[blockValue.type].MaxDamage;
                        if (svHelper.IsSleeping(blockValue.meta2))
                        {
                            // if the survivor is sleeping it will reduce hp each 5 checks, but only by 1
                            sleepFood++;
                            if (sleepFood > 10)
                            {
                                debugHelper.doDebug(string.Format("SLEEPING FOR TOO LONG"), debug);
                                svHelper.DmgBlock(maxDmg, blockValue, 1, world, blockPos, cIdx);
                            }
                            else debugHelper.doDebug(string.Format("SLEEPING SAINLY - NO HUNGER"), debug);
                        }
                        else
                        {
                            sleepFood = 0;
                            float hpPerc = 0;
                            if (currentDmg > 0)
                                hpPerc = (float)currentDmg / (float)maxDmg * 100;
                            int hpAdd = 0;
                            string[] foodItems = null;
                            // check what is the type of food to check
                            // For example: if a survivor is BADLY hurt he will ONLY accept high food tiers!
                            if (hpPerc <= 10)
                            {
                                debugHelper.doDebug(string.Format("LOW tier FOOD"), debug);
                                // more then 90%HP - low tier  
                                if (foodLow != "")
                                {
                                    foodItems = foodLow.Split(',');
                                }
                            }
                            else if (hpPerc <= 70)
                            {
                                debugHelper.doDebug(string.Format("MEDIUM tier FOOD"), debug);
                                // more then 30%HP - medium tier
                                if (foodMed != "")
                                {
                                    foodItems = foodMed.Split(',');
                                }
                            }
                            else
                            {
                                debugHelper.doDebug(string.Format("HIGH tier FOOD"), debug);
                                // less then 30%HP - high tier 
                                if (foodHigh != "")
                                {
                                    foodItems = foodHigh.Split(',');
                                }
                            }
                            if (foodItems.Length > 1 && foodContainer != "")
                            {
                                if (int.TryParse(foodItems[0], out hpAdd))
                                {
                                    bool couldEat = false;
                                    // look for containers in area
                                    foodContainers = svHelper.GetContainers(foodContainer, world, blockPos, cIdx,
                                        checkArea);
                                    //GetFoodContainers();
                                    if (foodContainers.Count > 0)
                                    {
                                        // search of 1 valid food item
                                        if (svHelper.EatFood(foodItems, foodContainers, world, cIdx))
                                        {
                                            // if exists, consumes it and recover ammount of hp
                                            debugHelper.doDebug(string.Format("EAT FOOD AND RECOVERS {0}", hpAdd), debug);
                                            svHelper.DmgBlock(maxDmg, blockValue, -hpAdd, world, blockPos, cIdx);
                                            couldEat = true;
                                        }
                                    }
                                    // if doesn't exist looses 2hp -> this means that, as long as there is valid food, they will easly recover.                        
                                    if (!couldEat)
                                    {
                                        debugHelper.doDebug(string.Format("NO FOOD AVAILABLE"), debug);
                                        svHelper.DmgBlock(maxDmg, blockValue, 2, world, blockPos, cIdx);
                                    }
                                }
                                else debugHelper.doDebug(string.Format("No hp increment configured"), debug);
                            }
                            else debugHelper.doDebug(string.Format("No food tier configured"), debug);
                        }
                    }

                    #endregion;
                }
                if (!GameManager.IsDedicatedServer)
                {
                    if (!svHelper.IsSleeping(blockValue.meta2) && DateTime.Now > nextRandomSound)
                    {
                        // this should be sent to all clients, or let each client play their own sounds... screw it, i'll do the last one for now.
                        // random sound                                     
                        // next random sound will be in random time too
                        System.Random _rndSound = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                        // there are 4 different sound: 1 - burp, 2 - cough, 3 - hickup, 4 - fart
                        int soundT = _rndSound.Next(0, 100);
                        string sound = "";
                        if (soundT < 10)
                        {
                            sound = "burp_sound";
                        }
                        else if (soundT < 30)
                        {
                            sound = "fart_sound";
                        }
                        else if (soundT < 60)
                        {
                            sound = "hickup_sound";
                        }
                        else if (soundT < 100)
                        {
                            sound = "cough_male_sound";
                            if (blockValue.meta > 1) sound = "cough_female_sound";
                        }
                        debugHelper.doDebug(string.Format("SurvivorScript: PLAY SOUND {0}", sound), debug);
                        //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                        Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                        nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
                    }
                }
                oldBlockValue = blockValue;
            }
        }
        catch (Exception ex)
        {
            debugHelper.doDebug(string.Format("SurvivorScript: ERROR UPDATE: {0} (pos={1}", ex.Message, pos), true);
        }
    }    
}

public class TowerScript : MonoBehaviour
{
    private float BlinkSpeed = 1.0F;
    private float LightOnIntensity = 1.0F;
    private float LightOffIntensity = 0.0F;
    private float SecondsPassed = 0.0F;
    private int spawnInterval = 30;
    private int spawnChance = 50;
    private int spawnArea = 50;
    private string entityGroup = "";
    private int maxSpawn = 3;
    private bool isPowered = true;
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    DateTime nextCheck = DateTime.MinValue;
    private DateTime nextSpawn = DateTime.MinValue;
    Light[] luz = null;
    bool hasPower = false;
    private bool debug = false;

    void Start()
    {
        luz = gameObject.GetComponentsInChildren<Light>(true);
    }
    public void init(WorldBase _world, Vector3i _blockPos, int _cIdx, int _spawnInterval, int _spawnChance, int _spawnArea, int _maxSpawn, bool _isPowered, string _entityGroup)
    {
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        entityGroup = _entityGroup;
        if (_spawnInterval > 0) spawnInterval = _spawnInterval;
        if (_spawnChance > 0) spawnChance = _spawnChance;
        if (_spawnArea > 0) spawnArea = _spawnArea;
        if (_maxSpawn > 0) maxSpawn = _maxSpawn;
        isPowered = _isPowered;
        if (!isPowered) hasPower = true; // the light ALWAYS blinks and it ALWAYS spawns
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
    }
    void Update()
    {
        if (world != null)
        {
            // check for power      
            if (true)
            {
                try
                {
                    if (DateTime.Now >= nextCheck && isPowered)
                    {
                        nextCheck = DateTime.Now.AddSeconds(5);
                        BlockValue _blockValue = world.GetBlock(cIdx, blockPos);
                        if (Findorigin(world, cIdx, _blockValue, blockPos, blockPos, 1, "Electric"))
                        {
                            hasPower = true;
                        }
                        else hasPower = false;
                    }
                }
                catch (Exception ex)
                {
                    debugHelper.doDebug(string.Format("TowerScript: ERROR TOWER: {0}", ex.Message), true);
                }
                // light blink - just for show... ;)
                SecondsPassed += Time.deltaTime;
                if (luz != null && hasPower)
                {
                    if (luz.Length > 0)
                    {
                        if (luz[0].intensity == LightOnIntensity && SecondsPassed >= BlinkSpeed)
                        {
                            luz[0].intensity = LightOffIntensity;
                            SecondsPassed = 0.0F;
                        }
                        else if (luz[0].intensity == LightOffIntensity && SecondsPassed >= BlinkSpeed)
                        {
                            luz[0].intensity = LightOnIntensity;
                            SecondsPassed = 0.0F;
                        }
                    }
                    else debugHelper.doDebug("TowerScript: No light exists", true);
                }
                else if (luz != null)
                {
                    if (luz.Length > 0)
                    {
                        luz[0].intensity = LightOffIntensity;
                        SecondsPassed = 0.0F;
                    }
                }
                else debugHelper.doDebug("TowerScript:No light exists", true);
                // this will only run on the server            
                if (!world.IsRemote())
                {
                    #region Spawn Check - SERVER ONLY;

                    try
                    {
                        if (hasPower && DateTime.Now >= nextSpawn)
                        {
                            nextSpawn = DateTime.Now.AddSeconds(spawnInterval);
                            System.Random _rnd = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                            int rndCheck = _rnd.Next(1, 101);
                            debugHelper.doDebug(
                                string.Format("TowerScript: TRY SPAWN with spawnChance={0} and randomCheck={1}",
                                    spawnChance,
                                    rndCheck), debug);
                            if (rndCheck <= spawnChance && spawnChance > 0)
                            {
                                debugHelper.doDebug("TowerScript: Spawning", debug);
                                // look for a place to spawn the survivor.
                                // basically will look for a place where i COULD spawn an entity
                                // and spawn the survivor block there
                                // i'll have to check for the number of those blocks nearby.

                                // check the maximum number of spawns in the spawn area
                                if (CheckMaxSurvivors())
                                {
                                    // prepares spawn
                                    int x = 0;
                                    int y = 0;
                                    int z = 0;
                                    GameManager.Instance.World.FindRandomSpawnPointNearPosition(blockPos.ToVector3(), 15,
                                        out x,
                                        out y,
                                        out z, new Vector3(spawnArea, spawnArea, spawnArea), true, true);
                                    Vector3i spotToPlace = new Vector3i(x, y, z);
                                    //BlockValue offBlock = Block.GetBlockValue("survivor");
                                    //Vector3i spotToPlace = GetSpotToPlaceBlock(offBlock, world, blockPos, cIdx, spawnArea);
                                    //if (world.IsOpenSkyAbove(cIdx, x, y + 1, z))
                                    //for (int i = -2; i <= 2; i++)
                                    if (spotToPlace != Vector3i.zero)
                                    {
                                        {
                                            // spawn random entity from a survivor group!
                                            int entityID = EntityGroups.GetRandomFromGroup(entityGroup);
                                            Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                                                new Vector3((float) x, (float) y, (float) z));
                                            spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner, cIdx,
                                                entityGroup);
                                            GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);

                                            //WorldRayHitInfo worldRayHitInfo = new WorldRayHitInfo();
                                            ////worldRayHitInfo.hit.blockPos = new Vector3i(x, y + i, z);
                                            //worldRayHitInfo.hit.blockPos = spotToPlace;
                                            //worldRayHitInfo.bHitValid = true;
                                            //BlockPlacement.Result _bpResult =
                                            //    Block.list[offBlock.type].BlockPlacementHelper.OnPlaceBlock(world, offBlock,
                                            //        worldRayHitInfo.hit, blockPos.ToVector3());
                                            //Block.list[offBlock.type].OnBlockPlaceBefore(world, ref _bpResult, null,
                                            //    new System.Random());
                                            //if (Block.list[offBlock.type].CanPlaceBlockAt(world, cIdx, _bpResult.blockPos,
                                            //    _bpResult.blockValue))
                                            //{
                                            //    // spawn the survivor block on this new position    
                                            //    Block.list[offBlock.type].PlaceBlock(world, _bpResult, null);
                                            //    //break;
                                            //}
                                            //else
                                            //    debugHelper.doDebug("TowerScript: CANNOT PLACE BLOCK AT DEFINED POSITION" +
                                            //                        _bpResult.blockPos.ToString(), debug);
                                        }
                                    }
                                    else
                                        debugHelper.doDebug(
                                            "TowerScript: NO SUITABLE PLACE TO SPAWN A SURVIVOR WAS FOUND",
                                            debug);
                                }
                                else debugHelper.doDebug("TowerScript: Impossible to spawn more survivors", debug);
                            }
                            else debugHelper.doDebug("TowerScript: Spawning failed", debug);
                        }
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug(string.Format("TowerScript: ERROR TOWER: {0}", ex.Message), true);
                    }

                    #endregion;
                }
            }
        }
    }
    // find a suitable place to spawn the survivor
    public Vector3i GetSpotToPlaceBlock(BlockValue blockToPlace, WorldBase world, Vector3i blockPos, int cIdx, int checkArea)
    {
        List<Vector3i> availableSpots = new List<Vector3i>();
        System.Random _rnd = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        Vector3i position = Vector3i.zero;
        for (int i = blockPos.x - checkArea; i <= (blockPos.x + checkArea); i++)
        {
            for (int j = blockPos.z - checkArea; j <= (blockPos.z + checkArea); j++)
            {
                for (int k = blockPos.y - checkArea; k <= (blockPos.y + checkArea); k++)
                {
                    // need to have open sky above, and at least 2 blocks free to the sides
                    position = Vector3i.zero;
                    BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
                    BlockValue blockTop = world.GetBlock(cIdx, new Vector3i(i, k + 1, j));
                    BlockValue blockTop1 = world.GetBlock(cIdx, new Vector3i(i, k + 2, j)); // i'm checking this 2 extra blocks, just in case I wanna remove the openskyabove
                    BlockValue blockTop2 = world.GetBlock(cIdx, new Vector3i(i, k + 3, j));
                    // cannot place on TOP of another survivor
                    if (blockTop.type == BlockValue.Air.type && blockTop1.type == BlockValue.Air.type && blockTop2.type == BlockValue.Air.type &&
                        block.type != BlockValue.Air.type && world.IsOpenSkyAbove(cIdx, i, k, j) && block.type != blockToPlace.type)
                    {                       
                        if (Block.list[blockToPlace.type].CanPlaceBlockAt(world, cIdx, (new Vector3i(i, k + 1, j)),
                            blockToPlace))
                        {
                            position = (new Vector3i(i, k + 1, j));
                            //debugHelper.doDebug(
                            //    string.Format("TowerScript: FOUND A POSITION DOING ADITIONAL CHECKINGS"), debug);
                            if (position != Vector3i.zero)
                            {
                                // check if it has "support" all around the place to spawn
                                // just to make sure
                                for (int i1 = position.x - 1; i1 <= (position.x + 1); i1++)
                                {
                                    for (int j1 = position.z - 1; j1 <= (position.z + 1); j1++)
                                    {
                                        block = world.GetBlock(cIdx, new Vector3i(i1, k, j1));
                                        if (block.type == BlockValue.Air.type ||
                                            Block.list[block.type].blockMaterial.IsGroundCover)
                                        {
                                            //debugHelper.doDebug(
                                            //    string.Format("TowerScript: NOT ENOUGH 'FLOOR' around the spot"), debug);
                                            position = Vector3i.zero;
                                            break;
                                        }
                                    }
                                    if (position == Vector3i.zero) break;
                                }
                            }
                        }                                                
                    }
                    if (position != Vector3i.zero)
                    {
                        availableSpots.Add(position);
                        position = Vector3i.zero;
                    }
                }
            }
        }
        position = Vector3i.zero;
        if (availableSpots.Count > 0)
        {
            debugHelper.doDebug(
                                string.Format("TowerScript: There are {0} valid spots available to spawn survivors", availableSpots.Count), debug);
            // get a random spot.
            position = availableSpots[_rnd.Next(0, availableSpots.Count - 1)];
        }
        return position;
    }

    // a boiler just needs to be turned on, there's no power consumption
    private bool Findorigin(WorldBase _world, int _cIdx, BlockValue _blockValue, Vector3i _blockPos, Vector3i _blockPosOrigin, int level, string powerType)
    {
        // now, it will check the parent directly... If no parent is found, the line to the origin is broken
        // and energy is NOT bidirectional - so even if there are acumulator further in line
        // power for this object is null
        // should be fast to calculate like this
        bool result = false;
        //RetryLabel:
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockPos).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        //DisplayChatAreaText(string.Format("CHECKING PARENT OF BLOCK {0}", blockname));
        if (_blockValue.meta2 > 0)
        {
            // check if parent exists. If it does, follows that line to the source
            // 1 - UP; 2 - DOWN; 3 - LEFT; 4 - RIGHT; 5 - FORWARD; 6 - BACK
            Vector3i posCheck = _blockPos;
            if (_blockValue.meta2 == 1)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z); // parent is up
            else if (_blockValue.meta2 == 2)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z); // parent is down
            else if (_blockValue.meta2 == 3)
                posCheck = new Vector3i(_blockPos.x - 1, _blockPos.y, _blockPos.z); // parent is left
            else if (_blockValue.meta2 == 4)
                posCheck = new Vector3i(_blockPos.x + 1, _blockPos.y, _blockPos.z); // parent is right
            else if (_blockValue.meta2 == 5)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z + 1); // parent is forward
            else if (_blockValue.meta2 == 6)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z - 1); // parent is back
            result = CheckBoiler(level, _world, _cIdx,
                     posCheck, _blockPos);
        }
        else
        {
            // This SHOULD not happen            
            //DisplayToolTipText(string.Format("NO PARENT DEFINED FOR {0} at level {1}", blockname, level));
        }

        return result;
    }

    #region Check Generator;   
    private bool CheckBoiler(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin)
    {
        int maxLevel = 10;
        int maxValves = 10;
        bool result = false;
        if (level > maxLevel)
        {
            //DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
            return result; // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using heat acumulators
        }                
        string blockname = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type].GetBlockName();
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        if (blockAux is BlockGenerator)
        {
            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockGenerator.IsOn(blockAuxValue.meta2))
            {
                //DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED ON"));
                return true;
            }
            else
            {
                //DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED OFF"));
                return false; // boiler is not on
            }
        }
        else if (blockAux is BlockValve)
        {            
            // needs to verify the valve powerType, to make sure.
            if ((blockAux as BlockValve).GetPowerType() != "Electric") return false;            
            // asks valve for power, instead of going all the way to the generator
            // it will go as deep as 10 valves
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, 1, maxValves))
            {
                //DisplayChatAreaText(string.Format("FOUND A VALVE WITH POWER"));
                return true; // available power
            }
            else
            {
                //DisplayChatAreaText(string.Format("FOUND A VALVE WITHOUT POWER"));
                return false; // no power available
            }
        }
        else if (blockAux is BlockPowerLine)
        {
            if ((blockAux as BlockPowerLine).GetPowerType() != "Electric") return false;
            // check one more level
            level = level + 1;
            // check parent of current posistion
            BlockValue currentValue = _world.GetBlock(_cIdx, _blockCheck);
            result = Findorigin(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, "Electric");
        }
        else
        {
            return false; // no more line
        }
        return result;
    }
    #endregion;

    private bool CheckMaxSurvivors()
    {
        int numBlocks = 0;
        int checkArea = spawnArea*2; // double the spawnArea, since they wander off a bit
        using (List<Entity>.Enumerator enumerator =
            GameManager.Instance.World.GetEntitiesInBounds(typeof (EntitySurvivorMod),
                BoundsUtils.BoundsForMinMax(blockPos.x - checkArea, blockPos.y - checkArea,
                    blockPos.z - checkArea, blockPos.x + checkArea,
                    blockPos.y + checkArea,
                    blockPos.z + checkArea)).GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                numBlocks++;
                if (numBlocks >= maxSpawn)
                {
                    return false; // cannot spawn more blocks
                }
            }
        }
        return true;


        //BlockValue survivorBlock = Block.GetBlockValue("survivor");
        //for (int i = blockPos.x - spawnArea; i <= (blockPos.x + spawnArea); i++)
        //{
        //    for (int j = blockPos.z - spawnArea; j <= (blockPos.z + spawnArea); j++)
        //    {
        //        for (int k = blockPos.y - spawnArea; k <= (blockPos.y + spawnArea); k++)
        //        {
        //            BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
        //            if (block.type == survivorBlock.type)
        //            {
        //                numBlocks++;
        //                if (numBlocks >= maxSpawn)
        //                {
        //                    return false; // cannot spawn more blocks
        //                }
        //            }
        //        }
        //    }
        //}
        //return true;
    }
}

// Opens and manipulates crafter know receipts
// probably text only, screw it...
public class CrafterInvScript : MonoBehaviour
{
    private CrafterInvScript script;
    private Rect guiAreaRect = new Rect(0, 0, 0, 0);
    private Rect infoAreaRect = new Rect(0, 0, 0, 0);
    private bool openGUI = false;
    private TileEntitySecureLootContainer inv;
    EntityAlive player = null;
    int numButtons = 0;
    private string lowTier = "";
    private string medTier = "";
    private string highTier = "";
    private string expertTier = "";
    private string craftArea = "";
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    private bool debug = false;
    private SurvivorHelper srcHelper = new SurvivorHelper();
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color titleColor = Color.red;
    Color buttonColor = Color.green;
    public Vector2 scrollPosition = Vector2.zero;

    void Start()
    {
        // TODO: build the window based on its content
        // TODO: allow the user to select an object to start "crafting" it
    }

    public void initialize(TileEntitySecureLootContainer _inv, EntityAlive _player, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        inv = _inv;
        player = _player;
        GetNumButtons();        
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTier"))
            lowTier = Block.list[blockValue.type].Properties.Values["LowTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTier"))
            medTier = Block.list[blockValue.type].Properties.Values["MedTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTier"))
            highTier = Block.list[blockValue.type].Properties.Values["HighTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTier"))
            expertTier = Block.list[blockValue.type].Properties.Values["ExpertTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftArea"))
            craftArea = Block.list[blockValue.type].Properties.Values["CraftArea"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        debugHelper.doDebug(string.Format("CrafterInvScript: Initializing custom INV with numButtons = {0}, cursor = {1}", numButtons,
            Cursor.lockState), debug);                
        openGUI = true;
    }

    private void GetNumButtons()
    {
        numButtons = 0;
        if (inv != null)
        {
            ItemStack[] items = inv.GetItems();
            foreach (ItemStack item in items)
            {
                if (item != null)
                {
                    if (item.count > 0)
                    {
                        numButtons++;
                    }
                }
            }
        }
        numButtons = numButtons + 6; // teach button, exit button, job label and Title        
        guiAreaRect = new Rect(10, 10, 250, 60 * numButtons);
        infoAreaRect = new Rect(300, 10, 250, 60 * numButtons);
    }

    public void OnGUI()
    {
        //debugHelper.doDebug("ONGui");
        if (openGUI && inv != null)
        {
            // GUISTYLES **************
            GUIStyle buttonSkin = new GUIStyle(GUI.skin.button);
            GUIStyle boxSkin = new GUIStyle(GUI.skin.box);
            GUIStyle titleSkin = new GUIStyle(GUI.skin.box);
            GUIStyle noJobSkin = new GUIStyle(GUI.skin.box);
            GUIStyle recTitleSkin = new GUIStyle(GUI.skin.box);
            buttonSkin.fontSize = 14;
            titleSkin.fontSize = 17;
            titleSkin.fontStyle = FontStyle.Bold;
            titleSkin.normal.textColor = Color.cyan;
            titleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.red);
            boxSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            boxSkin.normal.textColor = Color.white;
            boxSkin.fontSize = 14;
            boxSkin.fontStyle = FontStyle.Bold;
            noJobSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            noJobSkin.normal.textColor = Color.yellow;
            noJobSkin.fontSize = 15;
            noJobSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.fontSize = 16;
            recTitleSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.normal.textColor = Color.cyan;
            recTitleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.green);
            // GUISTYLES **************
            GetNumButtons();
            (player as EntityPlayerLocal).SetControllable(false);
            (player as EntityPlayerLocal).bIntroAnimActive = true;
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
            // get current blockvalue
            BlockValue blockValue = world.GetBlock(cIdx, blockPos);
            inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it
            Recipe receipe = null;
            #region Job area;
            //begin guilayout area            
            //GUILayout.BeginArea(guiAreaRect);
            //scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 280, 500), scrollPosition, new Rect(0, 0, 250, 50 * numButtons));
            // up to 5 buttons
            scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 280, 500), scrollPosition, new Rect(0, 0, 250, 50 * numButtons), false, true);
            //begin vertical group. This means that everything under this will go from up to down
            //GUILayout.BeginVertical();
            //loop throught the list of available known "receipts"
            GUI.color = Color.magenta;
            GUILayout.Box(string.Format("Receipts Managment (lvl: {0})", blockValue.meta), titleSkin, GUILayout.Width(250),
                GUILayout.Height(50));
            GUI.color = colorNatural;
            ItemStack[] items = inv.GetItems();
            #region Teach button;
            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Teach Held Item", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                // tries to "learn" the equiped weapon
                ItemStack heldItem = (player as EntityPlayerLocal).inventory.holdingItemStack;
                string heldName = ItemClass.GetForId(heldItem.itemValue.type).Name;
                //if (lowTier.Contains(heldName) || (medTier.Contains(heldName) && blockValue.meta>=35) || (highTier.Contains(heldName) && blockValue.meta >= 70) || (expertTier.Contains(heldName) && blockValue.meta >= 100))
                if (lowTier.Contains(heldName) || (medTier.Contains(heldName) && blockValue.meta >= 6) || (highTier.Contains(heldName) && blockValue.meta >= 12) || (expertTier.Contains(heldName) && blockValue.meta >= 100))
                {
                    // first checks if he already knows it
                    bool alreadyKnown = false;
                    foreach (ItemStack item in items)
                    {
                        if (item.itemValue.type == heldItem.itemValue.type && item.count > 0)
                        {
                            alreadyKnown = true;
                            break;
                        }
                    }
                    if (!alreadyKnown)
                    {
                        int f = 0;
                        bool foundSlot = false;
                        foreach (ItemStack item in items)
                        {
                            bool freeStack = false;
                            if (item == null) freeStack = true;
                            else if (item.count == 0) freeStack = true;
                            if (f > 0 && freeStack)
                            {
                                foundSlot = true;
                                System.Random _rnd = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                                decimal modifier = 1;
                                if (medTier.Contains(heldName)) modifier = 0.9m;
                                else if (highTier.Contains(heldName)) modifier = 0.7m;
                                else if (expertTier.Contains(heldName)) modifier = 0.6m;
                                decimal skillLevel = (decimal)blockValue.meta;
                                if (skillLevel == 0) skillLevel = 1;
                                skillLevel = (100 * skillLevel) / 15;
                                decimal chance = ((skillLevel / 100) * modifier) * 100;
                                if (chance < 35) chance = 35;
                                int rool = _rnd.Next(1, 100);
                                debugHelper.doDebug(string.Format("CrafterInvScript: Chance={0}, Roll={1}", chance, rool), debug);
                                if ((decimal)rool <= chance)
                                {
                                    ItemValue test = new ItemValue(heldItem.itemValue.type, 100,
                                        100, heldItem.itemValue.Parts.Length > 0);
                                    ItemStack learnItem = new ItemStack(test, 1);
                                    // always starts at low quality
                                    // quality will improve with number of items crafted 
                                    // the higher the tier, the harder it is to disassemble it                          
                                    inv.UpdateSlot(f, learnItem);
                                    inv.SetModified();
                                    GameManager.Instance.ShowTooltip(
                                        string.Format("Hmm this is interesting. I now know how to make a {0}",
                                            ItemClass.GetForId(heldItem.itemValue.type).localizedName));
                                }
                                else
                                {
                                    GameManager.Instance.ShowTooltip(
                                        string.Format(
                                            "Fuck, sorry mate. I broke your {0}! Maybe if you get me another one...",
                                            ItemClass.GetForId(heldItem.itemValue.type).localizedName));
                                }
                                (player as EntityPlayerLocal).inventory.DecHoldingItem(1);
                                break;
                            }
                            f++;
                        }
                        if (!foundSlot) GameManager.Instance.ShowTooltip("I'm good, but I aint that good! I can't learn anything else...");
                    }
                    else GameManager.Instance.ShowTooltip("What the hell? Zeds ate your brain or something? I already know how to make that!");
                }
                else if (lowTier.Contains(heldName) || (medTier.Contains(heldName)) || (highTier.Contains(heldName)) || (expertTier.Contains(heldName)))
                    GameManager.Instance.ShowTooltip("Shit dude, are you crazy? I can't do that yet!");
                else GameManager.Instance.ShowTooltip("What the hell do you want me to do with that?");
            }
            #endregion;
            #region Close Button;
            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Close Crafter", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                //if we clicked the button it will but that weapon to our selected(equipped) weapon
                openGUI = false;
                // Kills the scripts after it updates the inventory
                //inv.SetModified();
                KillScript();
            }
            #endregion;                        
            int i = 0;
            foreach (ItemStack item in items)
            {
                //check if we find something
                bool buttonShown = false;
                if (item != null)
                {
                    if (item.count > 0)
                    {                        
                        buttonShown = true;
                        //Do a gui button for every item we found on our weapon list. And make them draw their weaponLogos.    
                        //try to find the block first
                        string msg = "";
                        Block blockAux = Block.list[item.itemValue.ToBlockValue().type];
                        string localizedName = ItemClass.GetForId(item.itemValue.type).localizedName;
                        string itemName = ItemClass.GetForId(item.itemValue.type).Name;
                        if (blockAux != null && item.itemValue.ToBlockValue().type != BlockValue.Air.type)
                        {
                            localizedName = blockAux.GetLocalizedBlockName();
                            itemName = blockAux.GetBlockName();
                        }
                        if (item.itemValue.HasQuality)
                            msg = string.Format("{0} (lvl {1})",
                                localizedName, item.itemValue.Quality);
                        else msg = localizedName;
                        GUI.contentColor = Color.white;
                        if (i == 0)
                        {                            
                            GUI.contentColor = Color.yellow;
                            if (item.itemValue.HasQuality)
                                msg = string.Format("Current Job: {0} (lvl {1})",
                                    localizedName, item.itemValue.Quality);
                            else msg = string.Format("Current Job: {0}",
                                    localizedName);
                            #region find receipt;
                            Recipe[] recipes = CraftingManager.GetAllRecipes(itemName);
                            if (recipes != null)
                            {
                                if (recipes.Length > 0)
                                {                                    
                                    //bool receiptFound = false;
                                    foreach (Recipe rec in recipes)
                                    {
                                        if (rec.craftingArea == craftArea || rec.craftingArea == "" || craftArea == "")
                                        {
                                            receipe = rec;
                                            break;
                                        }
                                    }
                                }
                            }
                            #endregion;
                        }
                        // if it can already be done shows button, otherwise shows box
                        bool canMake = false;
                        if (lowTier.Contains(itemName) || (medTier.Contains(itemName) && blockValue.meta >= 6) || (highTier.Contains(itemName) && blockValue.meta >= 12) || (expertTier.Contains(itemName) && blockValue.meta >= 100))
                            canMake = true;
                        if (canMake)
                        {
                            GUI.backgroundColor = Color.blue;
                            if (GUILayout.Button(msg, buttonSkin,
                                GUILayout.Width(250),
                                GUILayout.Height(50)))
                            {
                                //selectedItem = item;
                                if (i == 0)
                                {
                                    // clear slot 0
                                    item.Clear();
                                    inv.SetModified();
                                }
                                else
                                {
                                    // set slot 0
                                    inv.UpdateSlot(0, item.Clone());
                                    inv.SetModified();
                                }
                            }
                        }
                        else
                        {
                            //GUI.color = colorNatural;
                            GUI.backgroundColor = originalColor;
                            GUI.contentColor = Color.white;
                            GUILayout.Box(msg, boxSkin, GUILayout.Width(250),
                                GUILayout.Height(50));
                        }
                    }
                }
                //GUI.contentColor = Color.yellow;
                if (!buttonShown && i==0) GUILayout.Box("No job selected", noJobSkin, GUILayout.Width(250),
                            GUILayout.Height(50));                
                i++;
            }
            GUI.EndScrollView();            
            //We need to close vertical gpr and gui area group.            
            //GUILayout.EndVertical();
            //GUILayout.EndArea();
            #endregion;

            if (receipe != null)
            {
                #region Information Area;

                GUILayout.BeginArea(infoAreaRect);
                //begin vertical group. This means that everything under this will go from up to down
                GUILayout.BeginVertical();
                //GUI.contentColor = Color.cyan;
                GUI.backgroundColor = Color.grey;
                GUILayout.Box(string.Format("Ingredients List for Current Job"), recTitleSkin, GUILayout.Width(250),
                    GUILayout.Height(50));
                GUI.backgroundColor = originalColor;
                GUI.contentColor = Color.white;
                foreach (ItemStack item in receipe.ingredients)
                {
                    ItemClass aux = ItemClass.GetForId(item.itemValue.type);
                    string blkIng = "";
                    if (aux != null)
                    {
                        if (aux.IsBlock())
                        {
                            int type = Block.GetBlockValue(aux.GetItemName()).type;
                            blkIng = Block.list[type].GetLocalizedBlockName();
                        }
                        else blkIng = aux.GetLocalizedItemName();
                    }
                    else blkIng = "Unknown Ingredient";
                    blkIng = string.Format("{0}x{1}", item.count, blkIng);
                    GUI.backgroundColor = originalColor;
                    GUILayout.Box(blkIng, boxSkin, GUILayout.Width(250),
                        GUILayout.Height(25));
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();

                #endregion;
            }
            else
            {
                #region Test Area;

                GUILayout.BeginArea(infoAreaRect);
                //begin vertical group. This means that everything under this will go from up to down
                GUILayout.BeginVertical();
                //GUI.contentColor = Color.cyan;
                GUI.backgroundColor = Color.grey;
                GUILayout.Box(string.Format("No aditional information"), recTitleSkin, GUILayout.Width(250),
                    GUILayout.Height(50));
                GUI.backgroundColor = originalColor;
                GUI.contentColor = Color.white;                
                GUILayout.EndVertical();
                GUILayout.EndArea();

                #endregion;
            }
        }
        else debugHelper.doDebug(string.Format("CrafterInvScript: openGUI={0} or inventory is null", openGUI.ToString()), debug);
    }

    public void KillScript()
    {
        (player as EntityPlayerLocal).bIntroAnimActive = false;
        (player as EntityPlayerLocal).SetControllable(true);
        GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
        script = gameObject.GetComponent<CrafterInvScript>();
        if (script != null)
        {
            Destroy(this);
        }
        else
        {
            //Debug.Log("FindChildTele script not found");
        }
    }
}

// class that allows the crafter to make whatever he was asked to make
// he will check if he has any assigned "job"
// he will check for parts
// if any part is missing, production will stop
public class CrafterWorkScript : MonoBehaviour
{
    private DateTime nextJobCheck = DateTime.MinValue;
    private DateTime craftEnd = DateTime.MinValue;
    private int craftCheckInterval = 10;
    private int CraftCheckArea = 2;
    private TileEntitySecureLootContainer inv;
    private string matContainer = ""; // containers to look for raw materials
    private string itemContainer = ""; // containers to put final products in
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    List<Vector3i> matContainers = new List<Vector3i>();
    List<Vector3i> itemContainers = new List<Vector3i>();
    private SurvivorHelper svHelper = new SurvivorHelper();
    //private bool crafting = false;
    private ItemStack itemToCraft;
    private Recipe craftRecipe;
    // sleep and food parameters
    private int checkInterval = 30;
    private int checkArea = 10;
    private string foodLow = "";
    private string foodMed = "";
    private string foodHigh = "";
    private string foodContainer = "";
    private string craftArea = "";
    DateTime nextSleepCheck = DateTime.MinValue;
    DateTime nextFoodCheck = DateTime.MinValue;
    List<Vector3i> foodContainers = new List<Vector3i>();
    private int sleepFood = 0;
    // heatmap
    private DateTime nextHeatCheck = DateTime.Now;
    private float HeatMapStrength = 0.0F;
    private ulong HeatMapWorldTime = 0;
    private int HeatInterval = 60;
    private string requirement = "Any";
    EnumAIDirectorChunkEvent heatType = EnumAIDirectorChunkEvent.Sound;
    // random sounds
    private DateTime nextRandomSound = DateTime.MinValue;
    private string workSound = "";
    private BlockValue oldBlockValue = BlockValue.Air;
    private bool debug = false;

    void Start()
    {
    }

    public void initialize(TileEntitySecureLootContainer _inv, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        inv = _inv;        
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        BlockValue blockValue = _world.GetBlock(cIdx, blockPos);
        //// set crafting to FALSE ALWAYS.
        //if (!world.IsRemote())
        //{
        //    debugHelper.doDebug(string.Format("CrafterWorkScript: SET CRAFTING TO FALSE"));
        //    blockValue.meta2 = (byte)(blockValue.meta2 & ~(1 << 2));
        //    world.SetBlockRPC(cIdx, blockPos, blockValue);
        //}
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MatContainer"))
            matContainer = Block.list[blockValue.type].Properties.Values["MatContainer"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ItemContainer"))
            itemContainer = Block.list[blockValue.type].Properties.Values["ItemContainer"];
        CraftCheckArea = 2;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftCheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CraftCheckArea"], out CraftCheckArea) == false) CraftCheckArea = 2;
        }
        checkInterval = 30;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckInterval"], out checkInterval) == false) checkInterval = 30;
        }
        checkArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        foodLow = "";
        foodMed = "";
        foodHigh = "";
        foodContainer = "";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodLow"))
            foodLow = Block.list[blockValue.type].Properties.Values["FoodLow"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodMedium"))
            foodMed = Block.list[blockValue.type].Properties.Values["FoodMedium"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodHigh"))
            foodHigh = Block.list[blockValue.type].Properties.Values["FoodHigh"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodContainer"))
            foodContainer = Block.list[blockValue.type].Properties.Values["FoodContainer"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftArea"))
            craftArea = Block.list[blockValue.type].Properties.Values["CraftArea"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatStrength"))
            HeatMapStrength = Utils.ParseFloat(Block.list[blockValue.type].Properties.Values["HeatStrength"]);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatTime"))
            HeatMapWorldTime = ulong.Parse(Block.list[blockValue.type].Properties.Values["HeatTime"]) * 10UL;
        HeatInterval = 120;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["HeatInterval"], out HeatInterval) == false) HeatInterval = 120;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatType"))
        {
            if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Sound") heatType = EnumAIDirectorChunkEvent.Sound;
            else if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Smell") heatType = EnumAIDirectorChunkEvent.Smell;
        }
        requirement = "Any";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Requires"))
            requirement = Block.list[blockValue.type].Properties.Values["Requires"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("WorkSound"))
            workSound = Block.list[blockValue.type].Properties.Values["WorkSound"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        System.Random _rndGen = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        if (!svHelper.IsFirstRun(blockValue.meta2) && !_world.IsRemote())
        {
            // set bit to 1, since i wont do this never again
            // only server, host or local computer will do this
            blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 3));
            _world.SetBlockRPC(cIdx, blockPos, blockValue);
            debugHelper.doDebug(
                    string.Format("CrafterWorkScript: Try to assign default receipts"), debug);
            #region Assigns autolearn and default receipts, up to 5
            if (_inv != null)
            {
                int learnedNumber = 0;
                int maxToLearn = 5;
                string autoLearn = "";                
                string lowTier = "";
                string medTier = "";
                string highTier = "";
                string expertTier = "";
                int lowTierDF = 0;
                int medTierDF = 0;
                int highTierDF = 0;
                int expertTierDF = 0;
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTier"))
                    lowTier = Block.list[blockValue.type].Properties.Values["LowTier"];
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTier"))
                    medTier = Block.list[blockValue.type].Properties.Values["MedTier"];
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTier"))
                    highTier = Block.list[blockValue.type].Properties.Values["HighTier"];
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTier"))
                    expertTier = Block.list[blockValue.type].Properties.Values["ExpertTier"];
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("AutoLearn"))
                    autoLearn = Block.list[blockValue.type].Properties.Values["AutoLearn"];
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTierDF"))
                {
                    if (int.TryParse(Block.list[blockValue.type].Properties.Values["LowTierDF"], out lowTierDF) == false) lowTierDF = 0;
                }
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTierDF"))
                {
                    if (int.TryParse(Block.list[blockValue.type].Properties.Values["MedTierDF"], out medTierDF) == false) medTierDF = 0;
                }
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTierDF"))
                {
                    if (int.TryParse(Block.list[blockValue.type].Properties.Values["HighTierDF"], out highTierDF) == false) highTierDF = 0;
                }
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTierDF"))
                {
                    if (int.TryParse(Block.list[blockValue.type].Properties.Values["ExpertTierDF"], out expertTierDF) == false) expertTierDF = 0;
                }                
                if (autoLearn != "")
                {
                    string[] availableToLearn = autoLearn.Split(',');
                    if (availableToLearn.Length > 0)
                    {
                        foreach (string tStr in availableToLearn)
                        {
                            AddDefaultRec(_world, tStr);
                        }
                    }
                }
                if (lowTierDF > 0 && lowTier != "")
                {
                    _rndGen = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    lowTierDF = _rndGen.Next(0, lowTierDF);
                    if (lowTierDF > 0)
                    {
                        // assign this receipts, let's start choosing
                        string[] availableToLearn = lowTier.Split(',');
                        if (availableToLearn.Length > 0)
                        {
                            if (availableToLearn.Length < lowTierDF) lowTierDF = availableToLearn.Length;
                            List<string> lst = new List<string>();
                            foreach (string tStr in availableToLearn)
                            {
                                if (!autoLearn.Contains(tStr))
                                    lst.Add(tStr);
                            }
                            while (lowTierDF > 0 && lst.Count > 0)
                            {
                                int index = _rndGen.Next(0, lst.Count - 1);
                                string learnNow = lst[index];
                                AddDefaultRec(_world, learnNow);
                                lst.RemoveAt(index);
                                lowTierDF--;
                            }
                        }
                    }
                }
                if (medTierDF > 0 && medTier != "")
                {
                    _rndGen = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    medTierDF = _rndGen.Next(0, medTierDF);
                    if (medTierDF > 0)
                    {
                        // assign this receipts, let's start choosing
                        string[] availableToLearn = medTier.Split(',');
                        if (availableToLearn.Length > 0)
                        {
                            if (availableToLearn.Length < medTierDF) medTierDF = availableToLearn.Length;
                            List<string> lst = new List<string>();
                            foreach (string tStr in availableToLearn)
                            {
                                if (!autoLearn.Contains(tStr))
                                    lst.Add(tStr);
                            }
                            while (medTierDF > 0 && lst.Count > 0)
                            {
                                int index = _rndGen.Next(0, lst.Count - 1);
                                string learnNow = lst[index];
                                AddDefaultRec(_world, learnNow);
                                lst.RemoveAt(index);
                                medTierDF--;
                            }
                        }
                    }
                }
                if (highTierDF > 0 && highTier != "")
                {
                    _rndGen = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    highTierDF = _rndGen.Next(0, highTierDF);
                    if (highTierDF > 0)
                    {
                        // assign this receipts, let's start choosing
                        string[] availableToLearn = highTier.Split(',');
                        if (availableToLearn.Length > 0)
                        {
                            if (availableToLearn.Length < highTierDF) highTierDF = availableToLearn.Length;
                            List<string> lst = new List<string>();
                            foreach (string tStr in availableToLearn)
                            {
                                if (!autoLearn.Contains(tStr))
                                    lst.Add(tStr);
                            }
                            while (highTierDF > 0 && lst.Count > 0)
                            {
                                int index = _rndGen.Next(0, lst.Count - 1);
                                string learnNow = lst[index];
                                AddDefaultRec(_world, learnNow);
                                lst.RemoveAt(index);
                                highTierDF--;
                            }
                        }
                    }
                }
                if (expertTierDF > 0 && expertTier != "")
                {
                    _rndGen = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    expertTierDF = _rndGen.Next(0, expertTierDF);
                    if (expertTierDF > 0)
                    {
                        // assign this receipts, let's start choosing
                        string[] availableToLearn = expertTier.Split(',');
                        if (availableToLearn.Length > 0)
                        {
                            if (availableToLearn.Length < expertTierDF) expertTierDF = availableToLearn.Length;
                            List<string> lst = new List<string>();
                            foreach (string tStr in availableToLearn)
                            {
                                lst.Add(tStr);
                            }
                            while (expertTierDF > 0 && lst.Count > 0)
                            {
                                int index = _rndGen.Next(0, lst.Count - 1);
                                string learnNow = lst[index];
                                AddDefaultRec(_world, learnNow);
                                lst.RemoveAt(index);
                                expertTierDF--;
                            }
                        }
                    }
                }                
            }
            #endregion;
        }        
        nextRandomSound = DateTime.Now.AddSeconds(_rndGen.Next(30, 120));
        world = _world;
    }

    private void AddDefaultRec(WorldBase _world, string learnNow)
    {
        inv = _world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer;
        ItemStack[] items = inv.GetItems();
        int f = 0;
        bool foundSlot = false;
        foreach (ItemStack item in items)
        {
            bool freeStack = false;
            if (item == null) freeStack = true;
            else if (item.count == 0) freeStack = true;
            if (f > 0 && freeStack)
            {
                foundSlot = true;
                ItemStack itemToLearn = null;
                ItemValue test = null;
                ItemStack learnItem = null;
                // if its a block
                BlockValue blockAux = Block.GetBlockValue(learnNow);
                if (blockAux.type == BlockValue.Air.type)
                {
                    debugHelper.doDebug(
                        string.Format("CrafterWorkScript: {0} is A ITEM", learnNow), debug);
                    itemToLearn =
                        new ItemStack(new ItemValue(ItemClass.GetItem(learnNow).type, false), 1);
                    test = new ItemValue(itemToLearn.itemValue.type, 100,
                        100, itemToLearn.itemValue.Parts.Length > 0);
                    learnItem = new ItemStack(test, 1);
                }
                else
                {
                    debugHelper.doDebug(
                        string.Format("CrafterWorkScript: {0} is A BLOCK", learnNow), debug);
                    itemToLearn =
                        new ItemStack(blockAux.ToItemValue(), 1);
                    learnItem = itemToLearn.Clone();
                }
                inv.UpdateSlot(f, learnItem);
                inv.SetModified();
                debugHelper.doDebug(
                    string.Format("CrafterWorkScript: Default learned {0}", learnNow), debug);
                break;
            }
            f++;
        }
    }

    void Update()
    {
        string pos = "0";
        if (world != null)
        {            
            BlockValue blockValue = world.GetBlock(cIdx, blockPos);
            inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it
            System.Random _rnd = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            if (!GameManager.IsDedicatedServer)
            {
                // force animation
                if (svHelper.IsSleeping(blockValue.meta2))
                {
                    if (svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "sleeping", "Sleep", "");
                    else
                    {
                        // play transition
                        svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
                else if (svHelper.IsCrafting(blockValue.meta2))
                {
                    svHelper.ForceState(gameObject, blockPos, cIdx, "working", "isWorking", workSound);
                }
                else if (!svHelper.IsSleeping(blockValue.meta2))
                {
                    if (!svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "WakeUp", "");
                    else
                    {
                        // play transition
                        svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
            }
            if (DateTime.Now > nextSleepCheck && !svHelper.IsCrafting(blockValue.meta2))
            {
                debugHelper.doDebug("CrafterWorkScript: CRAFTER CHECK meta2: " + blockValue.meta2, debug);
                nextSleepCheck = DateTime.Now.AddSeconds(_rnd.Next(7, 30)); // from 1 to 30 seconds, just so that not all change at the same time
                int sleepProb = 0;
                int wakeProb = 0;
                // this will only run on the server
                if (!world.IsRemote())
                {
                    #region sleep check;
                    if (!GameManager.Instance.World.IsDaytime())
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 80; // 80% chance of sleeping during the night
                        else wakeProb = 5; // 5% chance of waking up during the night                            
                    }
                    else if (GameManager.Instance.World.IsDaytime() && svHelper.IsSleeping(blockValue.meta2))
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 5; // 5% chance of sleeping during the day
                        else wakeProb = 80; // 80% chance of waking up during the day
                    }
                    int rndCheck = _rnd.Next(1, 101);
                    if (rndCheck <= sleepProb && sleepProb > 0)
                    {
                        pos = "4";
                        // fall asleep if not sleeping
                        pos = svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }
                    else if (rndCheck <= wakeProb && wakeProb > 0)
                    {
                        pos = "5";
                        // wakeup if sleeping
                        pos = svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }
                    #endregion;
                }
                if (!GameManager.IsDedicatedServer)
                {
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        string sound = "sleep_male_sound";
                        if (requirement != "Male") sound = "sleep_female_sound";
                        debugHelper.doDebug(string.Format("CrafterWorkScript: PLAY SOUND {0}", sound), debug);
                        //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                        Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    }
                }
            }
            if (!world.IsRemote())
            {
                if (DateTime.Now > nextFoodCheck)
                {
                    #region food check;
                    nextFoodCheck = DateTime.Now.AddSeconds(checkInterval);
                    int currentDmg = blockValue.damage;
                    int maxDmg = Block.list[blockValue.type].MaxDamage;
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        // if the survivor is sleeping it will reduce hp each 5 checks, but only by 1
                        sleepFood++;
                        if (sleepFood > 10)
                        {
                            debugHelper.doDebug(string.Format("CrafterWorkScript: SLEEPING FOR TOO LONG"), debug);
                            svHelper.DmgBlock(maxDmg, blockValue, 1, world, blockPos, cIdx);
                        }
                        else debugHelper.doDebug(string.Format("CrafterWorkScript: SLEEPING SAINLY - NO HUNGER"), debug);
                    }
                    else
                    {
                        sleepFood = 0;
                        float hpPerc = 0;
                        if (currentDmg > 0)
                            hpPerc = (float)currentDmg / (float)maxDmg * 100;
                        int hpAdd = 0;
                        string[] foodItems = null;
                        // check what is the type of food to check
                        // For example: if a survivor is BADLY hurt he will ONLY accept high food tiers!
                        if (hpPerc <= 10)
                        {
                            debugHelper.doDebug(string.Format("CrafterWorkScript: LOW tier FOOD"), debug);
                            // more then 90%HP - low tier  
                            if (foodLow != "")
                            {
                                foodItems = foodLow.Split(',');
                            }
                        }
                        else if (hpPerc <= 70)
                        {
                            debugHelper.doDebug(string.Format("CrafterWorkScript: MEDIUM tier FOOD"), debug);
                            // more then 30%HP - medium tier
                            if (foodMed != "")
                            {
                                foodItems = foodMed.Split(',');
                            }
                        }
                        else
                        {
                            debugHelper.doDebug(string.Format("CrafterWorkScript: HIGH tier FOOD"), debug);
                            // less then 30%HP - high tier 
                            if (foodHigh != "")
                            {
                                foodItems = foodHigh.Split(',');
                            }
                        }
                        if (foodItems.Length > 1 && foodContainer != "")
                        {
                            if (int.TryParse(foodItems[0], out hpAdd))
                            {
                                bool couldEat = false;
                                // look for containers in area
                                foodContainers = svHelper.GetContainers(foodContainer, world, blockPos, cIdx, checkArea);
                                //GetFoodContainers();
                                if (foodContainers.Count > 0)
                                {
                                    // search of 1 valid food item
                                    if (svHelper.EatFood(foodItems, foodContainers, world, cIdx))
                                    {
                                        // if exists, consumes it and recover ammount of hp
                                        debugHelper.doDebug(string.Format("CrafterWorkScript: EAT FOOD AND RECOVERS {0}", hpAdd), debug);
                                        svHelper.DmgBlock(maxDmg, blockValue, -hpAdd, world, blockPos, cIdx);
                                        couldEat = true;
                                    }
                                }
                                // if doesn't exist looses 2hp -> this means that, as long as there is valid food, they will easly recover.                        
                                if (!couldEat)
                                {
                                    debugHelper.doDebug(string.Format("CrafterWorkScript: NO FOOD AVAILABLE"), debug);
                                    svHelper.DmgBlock(maxDmg, blockValue, 2, world, blockPos, cIdx);
                                }
                            }
                            else debugHelper.doDebug(string.Format("CrafterWorkScript: No hp increment configured"), true);
                        }
                        else debugHelper.doDebug(string.Format("CrafterWorkScript: No food tier configured"), true);
                    }
                    #endregion;
                }
                if (!svHelper.IsSleeping(blockValue.meta2))
                {
                    #region Crafting Task;

                    if (inv != null)
                    {
                        if (DateTime.Now >= nextJobCheck)
                        {
                            nextJobCheck = DateTime.Now.AddSeconds(craftCheckInterval);
                            if (svHelper.IsCrafting(blockValue.meta2))
                            {
                                debugHelper.doDebug(string.Format("CrafterWorkScript: IS CRAFTING UNTIL {0}",
                                    craftEnd.ToString("yyyy-MM-dd HH:mm:ss")), debug);
                                if (DateTime.Now >= craftEnd)
                                {
                                    debugHelper.doDebug(string.Format("CrafterWorkScript: FINISHED CRAFTING"), debug);
                                    // check for item containers
                                    itemContainers = svHelper.GetContainers(itemContainer, world, blockPos, cIdx,
                                        CraftCheckArea);
                                    debugHelper.doDebug(string.Format("CrafterWorkScript: FINISHED CRAFTING (1)"), debug);
                                    if (itemContainers.Count > 0)
                                    {
                                        debugHelper.doDebug(string.Format("CrafterWorkScript: FINISHED CRAFTING (2)"), debug);
                                        if (svHelper.craftItem(matContainers, itemContainers, itemToCraft, craftRecipe,
                                            world, cIdx))
                                        {
                                            debugHelper.doDebug(string.Format(
                                                "CrafterWorkScript: Finished crafting {0}",
                                                ItemClass.GetForId(itemToCraft.itemValue.type).localizedName), debug);
                                            System.Random _rndSkill =
                                                new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                                            if (_rndSkill.Next(0, 100) < 10)
                                            {
                                                // general skill increase
                                                if (blockValue.meta < 15)
                                                    blockValue.meta++;
                                            }
                                            world.SetBlockRPC(cIdx, blockPos, blockValue);
                                            if (itemToCraft.itemValue.HasQuality)
                                            {
                                                _rndSkill = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                                                // weapon skill increase ranges from 5 to 10
                                                // this means a minimum of 50 weapons and a max of 100 to reach level 600
                                                int skillIncrease = _rndSkill.Next(5, 10);
                                                // weapon skill inscrease (both on job slot and weapon slot)
                                                ItemStack[] items = inv.GetItems();
                                                if (items.Length > 0)
                                                {
                                                    int f = 0;
                                                    foreach (ItemStack item in items)
                                                    {
                                                        if (item.itemValue.type == itemToCraft.itemValue.type)
                                                        {
                                                            // create a new quality item and replaces it
                                                            int newQuality = item.itemValue.Quality + skillIncrease;
                                                            if (newQuality > 600) newQuality = 600;
                                                            ItemValue test = new ItemValue(item.itemValue.type,
                                                                newQuality, newQuality,
                                                                item.itemValue.Parts.Length > 0);
                                                            ItemStack learnItem = new ItemStack(test, 1);
                                                            inv.UpdateSlot(f, learnItem);
                                                            inv.SetModified();
                                                        }
                                                        f++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                            debugHelper.doDebug(
                                                string.Format("CrafterWorkScript: Something went wrong crafting"), debug);
                                    }
                                    //crafting = false;
                                    debugHelper.doDebug(string.Format("CrafterWorkScript: CHANGES BIT 2 TO STOP CRAFTING"), debug);
                                    blockValue.meta2 = (byte)(blockValue.meta2 & ~(1 << 2));
                                    world.SetBlockRPC(cIdx, blockPos, blockValue);                                    
                                }
                            }
                            else
                            {
                                // check for material containers
                                matContainers = svHelper.GetContainers(matContainer, world, blockPos, cIdx, CraftCheckArea);
                                if (matContainers.Count > 0)
                                {
                                    // check for item containers
                                    itemContainers = svHelper.GetContainers(itemContainer, world, blockPos, cIdx,
                                        CraftCheckArea);
                                }
                                if (matContainers.Count > 0 && itemContainer.Length > 0)
                                {
                                    ItemStack[] items = inv.GetItems();
                                    if (items.Length > 0)
                                    {
                                        debugHelper.doDebug("CrafterWorkScript: There are item in crafter inv", debug);
                                        // if the first position in the inv has an item, that's the assigned job
                                        if (items[0] != null)
                                        {
                                            debugHelper.doDebug("CrafterWorkScript: There is item0", debug);
                                            if (items[0].count > 0)
                                            {
                                                Block blockAux = Block.list[items[0].itemValue.ToBlockValue().type];
                                                string itemName = ItemClass.GetForId(items[0].itemValue.type).Name;
                                                string itemLocalizedName = ItemClass.GetForId(items[0].itemValue.type).localizedName;
                                                if (blockAux != null && items[0].itemValue.ToBlockValue().type != BlockValue.Air.type)
                                                {
                                                    itemName = blockAux.GetBlockName();
                                                    itemLocalizedName = blockAux.GetLocalizedBlockName();
                                                }
                                                Recipe[] recipes =
                                                    CraftingManager.GetAllRecipes(itemName);
                                                if (recipes != null)
                                                {
                                                    if (recipes.Length > 0)
                                                    {
                                                        debugHelper.doDebug("CrafterWorkScript: There are receipts", debug);
                                                        bool receiptFound = false;
                                                        foreach (Recipe rec in recipes)
                                                        {
                                                            debugHelper.doDebug(
                                                                "CrafterWorkScript: CRAFTAREA = " + rec.craftingArea,
                                                                debug);
                                                            if (rec.craftingArea == craftArea || rec.craftingArea == "" || craftArea == "")
                                                            {
                                                                debugHelper.doDebug("CrafterWorkScript: Found valid receipt", debug);
                                                                receiptFound = true;
                                                                bool itemMissing = false;
                                                                // go through the ingredients list and see if all are available
                                                                foreach (ItemStack ing in rec.ingredients)
                                                                {
                                                                    // if any of them is NOT available, he will have to wait
                                                                    if (
                                                                        !svHelper.checkIngredient(matContainers, ing, world,
                                                                            cIdx, false))
                                                                    {
                                                                        debugHelper.doDebug(
                                                                            string.Format("CrafterWorkScript: Missing {0}",
                                                                                ItemClass.GetForId(ing.itemValue.type)
                                                                                    .localizedName), debug);
                                                                        itemMissing = true;
                                                                        break;
                                                                    }
                                                                }
                                                                // if all exist, try to craft the end product   
                                                                if (!itemMissing)
                                                                {
                                                                    // will start to craft
                                                                    debugHelper.doDebug(string.Format(
                                                                        "CrafterWorkScript: Building {0} in {1}s",
                                                                        ItemClass.GetForId(items[0].itemValue.type)
                                                                            .localizedName,
                                                                        rec.craftingTime), debug);
                                                                    //crafting = true;
                                                                    blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 2));
                                                                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                                                                    itemToCraft = items[0].Clone();
                                                                    craftRecipe = rec;
                                                                    craftEnd =
                                                                        DateTime.Now.AddSeconds((double) rec.craftingTime);
                                                                }
                                                                else
                                                                    debugHelper.doDebug(
                                                                        string.Format(
                                                                            "CrafterWorkScript: There are missing ingredients to make {0}",
                                                                            itemLocalizedName), debug);
                                                                break;
                                                            }
                                                        }
                                                        if (!receiptFound)
                                                            debugHelper.doDebug(string.Format("CrafterWorkScript: No {1} receipts for {0}",
                                                                itemLocalizedName, craftArea), debug);
                                                    }
                                                    else
                                                        debugHelper.doDebug(string.Format("CrafterWorkScript: No known receipts for {0}",
                                                            itemLocalizedName), debug);
                                                }
                                                else
                                                    debugHelper.doDebug(string.Format("CrafterWorkScript: No receipts for {0}",
                                                        itemLocalizedName), debug);
                                            }
                                            else debugHelper.doDebug("CrafterWorkScript: No job defined(1)", debug);
                                        }
                                        else debugHelper.doDebug("CrafterWorkScript: No job defined", debug);
                                    }
                                    else debugHelper.doDebug("CrafterWorkScript: No known receipts", debug);
                                }
                                else debugHelper.doDebug("CrafterWorkScript: There are no Material containers or item containers", debug);
                            }
                        }
                    }
                    else debugHelper.doDebug("CrafterWorkScript: Crafter has no inventory", true);

                    #endregion;
                }
                // only emits heatmap if working.
                if (DateTime.Now > nextHeatCheck && svHelper.IsCrafting(blockValue.meta2))
                {
                    nextHeatCheck = DateTime.Now.AddSeconds(HeatInterval);
                    if (GameManager.Instance.World.aiDirector != null)
                    {
                        if (HeatMapStrength > 0 && HeatMapWorldTime > 0)
                        {
                            GameManager.Instance.World.aiDirector.NotifyActivity(
                                heatType, blockPos, HeatMapStrength,
                                HeatMapWorldTime);
                        }
                    }
                }
            }
            // random sound
            if (!GameManager.IsDedicatedServer)
            {
                if (!svHelper.IsSleeping(blockValue.meta2) && DateTime.Now > nextRandomSound &&
                    !svHelper.IsCrafting(blockValue.meta2))
                {
                    // next random sound will be in random time too
                    System.Random _rndSound = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                    // there are 4 different sound: 1 - burp, 2 - cough, 3 - hickup, 4 - fart
                    int soundT = _rndSound.Next(0, 100);
                    string sound = "";
                    if (soundT < 10)
                    {
                        sound = "burp_sound";
                    }
                    else if (soundT < 30)
                    {
                        sound = "fart_sound";
                    }
                    else if (soundT < 60)
                    {
                        sound = "hickup_sound";
                    }
                    else if (soundT < 100)
                    {
                        sound = "cough_male_sound";
                        if (requirement != "Male") sound = "cough_female_sound";
                    }
                    debugHelper.doDebug(string.Format("CrafterWorkScript: PLAY SOUND {0}", sound), debug);
                    //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                    Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
                }
            }
            oldBlockValue = blockValue;
        }
    }
}

public class FarmerInvScript : MonoBehaviour
{
    private FarmerInvScript script;
    private Rect guiAreaRect = new Rect(0, 0, 0, 0);
    private Rect infoAreaRect = new Rect(0, 0, 0, 0);
    private bool openGUI = false;
    private TileEntitySecureLootContainer inv;
    EntityAlive player = null;
    int numButtons = 0;
    private string lowTier = "";
    private string medTier = "";
    private string highTier = "";
    private string expertTier = "";
    private string lowTierSeeds = "";
    private string medTierSeeds = "";
    private string highTierSeeds = "";
    private string expertTierSeeds = "";
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    private bool debug = false;
    private SurvivorHelper srcHelper = new SurvivorHelper();
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color titleColor = Color.red;
    Color buttonColor = Color.green;

    void Start()
    {
        // TODO: build the window based on its content
        // TODO: allow the user to select an object to start "crafting" it
    }

    public void initialize(TileEntitySecureLootContainer _inv, EntityAlive _player, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        inv = _inv;
        player = _player;
        GetNumButtons();        
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTier"))
            lowTier = Block.list[blockValue.type].Properties.Values["LowTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTier"))
            medTier = Block.list[blockValue.type].Properties.Values["MedTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTier"))
            highTier = Block.list[blockValue.type].Properties.Values["HighTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTier"))
            expertTier = Block.list[blockValue.type].Properties.Values["ExpertTier"];

        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTierSeeds"))
            lowTierSeeds = Block.list[blockValue.type].Properties.Values["LowTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTierSeeds"))
            medTierSeeds = Block.list[blockValue.type].Properties.Values["MedTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTierSeeds"))
            highTierSeeds = Block.list[blockValue.type].Properties.Values["HighTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTierSeeds"))
            expertTierSeeds = Block.list[blockValue.type].Properties.Values["ExpertTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        debugHelper.doDebug(string.Format("FarmerInvScript: Initializing custom INV with numButtons = {0}, cursor = {1}", numButtons,
            Cursor.lockState), debug);
        openGUI = true;
    }

    private void GetNumButtons()
    {
        numButtons = 7; // teach button, exit button, job label and Title
        guiAreaRect = new Rect(10, 10, 250, 60 * numButtons);
        infoAreaRect = new Rect(270, 10, 250, 60 * numButtons);
    }

    public void OnGUI()
    {
        //debugHelper.doDebug("ONGui");
        if (openGUI && inv != null)
        {
            // GUISTYLES **************
            GUIStyle buttonSkin = new GUIStyle(GUI.skin.button);
            GUIStyle boxSkin = new GUIStyle(GUI.skin.box);
            GUIStyle titleSkin = new GUIStyle(GUI.skin.box);
            GUIStyle noJobSkin = new GUIStyle(GUI.skin.box);
            GUIStyle recTitleSkin = new GUIStyle(GUI.skin.box);
            buttonSkin.fontSize = 14;
            titleSkin.fontSize = 17;
            titleSkin.fontStyle = FontStyle.Bold;
            titleSkin.normal.textColor = Color.cyan;
            titleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.red);
            boxSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            boxSkin.normal.textColor = Color.white;
            boxSkin.fontSize = 14;
            boxSkin.fontStyle = FontStyle.Bold;
            noJobSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            noJobSkin.normal.textColor = Color.yellow;
            noJobSkin.fontSize = 15;
            noJobSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.fontSize = 16;
            recTitleSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.normal.textColor = Color.cyan;
            recTitleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.green);
            // GUISTYLES **************
            GetNumButtons();
            (player as EntityPlayerLocal).SetControllable(false);
            (player as EntityPlayerLocal).bIntroAnimActive = true;
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
            // get current blockvalue
            BlockValue blockValue = world.GetBlock(cIdx, blockPos);
            inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it
            #region Button Area;
            //begin guilayout area
            GUILayout.BeginArea(guiAreaRect);
            //begin vertical group. This means that everything under this will go from up to down
            GUILayout.BeginVertical();
            //loop throught the list of available known "receipts"
            GUI.color = Color.magenta;
            GUILayout.Box(string.Format("Job Managment (lvl: {0})", blockValue.meta), titleSkin, GUILayout.Width(250),
                GUILayout.Height(50));
            GUI.color = colorNatural;
            ItemStack[] items = inv.GetItems();
            int i = 0;
            string tierPlants = "";
            string tierSeeds = "";
            foreach (ItemStack item in items)
            {
                //check if we find something
                if (item != null)
                {
                    if (i == 0 && item.IsEmpty())
                    {
                        // adds a piece of stone to the slot 0
                        // will be used to basically store what tier is active
                        // ugly but effective and avoids me custom files
                        debugHelper.doDebug("Creating new 'job object'", debug);
                        ItemStack rock = new ItemStack(new ItemValue(ItemClass.GetItem("stone").type, false), 1);
                        inv.UpdateSlot(0, rock);
                        inv.SetModified();
                    }
                    if (item.count > 0)
                    {
                        //Do a gui button for every item we found on our weapon list. And make them draw their weaponLogos.                            
                        GUI.contentColor = Color.white;
                        if (i == 0)
                        {                            
                            GUI.contentColor = Color.yellow;
                            string msg = "";
                            // get stone block from item stack                            
                            if (item.itemValue.Meta == 0)
                            {
                                GUILayout.Box("No job selected", noJobSkin, GUILayout.Width(250),
                              GUILayout.Height(50));
                            }
                            else if (item.itemValue.Meta == 1)
                            {
                                msg = "Working Low Tier Plants";
                                tierPlants = lowTier;
                                tierSeeds = lowTierSeeds;
                            }
                            else if (item.itemValue.Meta == 2)
                            {
                                msg = "Working Medium Tier Plants";
                                tierPlants = medTier;
                                tierSeeds = medTierSeeds;
                            }
                            else if (item.itemValue.Meta == 3)
                            {
                                msg = "Working High Tier Plants";
                                tierPlants = highTier;
                                tierSeeds = highTierSeeds;
                            }
                            else if (item.itemValue.Meta == 4)
                            {
                                msg = "Working Expert Tier Plants";
                                tierPlants = expertTier;
                                tierSeeds = expertTierSeeds;
                            }
                            if (msg != "")
                            {
                                GUI.backgroundColor = Color.blue;
                                if (GUILayout.Button(msg, buttonSkin, GUILayout.Width(250),
                                    GUILayout.Height(50)))
                                {
                                    item.itemValue.Meta = 0;
                                    inv.SetModified();
                                }
                            }
                            GUI.backgroundColor = Color.blue;
                            GUI.contentColor = Color.white;
                            if (GUILayout.Button("Low Tier Plants", buttonSkin, GUILayout.Width(250),
                                GUILayout.Height(50)))
                            {
                                item.itemValue.Meta = 1;
                                inv.SetModified();
                            }
                            if (blockValue.meta >= 6)
                            {
                                GUI.backgroundColor = Color.blue;
                                GUI.contentColor = Color.white;
                                if (GUILayout.Button("Medium Tier Plants", buttonSkin, GUILayout.Width(250),
                                GUILayout.Height(50)))
                                {
                                    item.itemValue.Meta = 2;
                                    inv.SetModified();
                                }
                            }
                            else
                            {
                                GUI.backgroundColor = originalColor;
                                //GUI.contentColor = Color.grey;
                                GUILayout.Box("Medium Tier Plants", boxSkin, GUILayout.Width(250),
                                             GUILayout.Height(50));
                            }
                            if (blockValue.meta >= 12)
                            {
                                GUI.backgroundColor = Color.blue;
                                //GUI.contentColor = Color.white;
                                if (GUILayout.Button("High Tier Plants", buttonSkin, GUILayout.Width(250),
                                GUILayout.Height(50)))
                                {
                                    item.itemValue.Meta = 3;
                                    inv.SetModified();
                                }
                            }
                            else
                            {
                                GUI.backgroundColor = originalColor;
                                //GUI.contentColor = Color.grey;
                                GUILayout.Box("High Tier Plants", boxSkin, GUILayout.Width(250),
                                             GUILayout.Height(50));
                            }
                            if (blockValue.meta >= 15)
                            {
                                GUI.backgroundColor = Color.blue;
                                GUI.contentColor = Color.white;
                                if (GUILayout.Button("Expert Tier Plants", buttonSkin, GUILayout.Width(250),
                                GUILayout.Height(50)))
                                {
                                    item.itemValue.Meta = 4;
                                    inv.SetModified();
                                }
                            }
                            else
                            {
                                GUI.backgroundColor = originalColor;
                                //GUI.contentColor = Color.grey;
                                GUILayout.Box("Expert Tier Plants", boxSkin, GUILayout.Width(250),
                                             GUILayout.Height(50));
                            }
                            break;
                        }                        
                    }
                }
                i++;
            }
            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Close Farmer", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                //if we clicked the button it will but that weapon to our selected(equipped) weapon
                openGUI = false;
                // Kills the scripts after it updates the inventory
                //inv.SetModified();
                KillScript();
            }
            //We need to close vertical gpr and gui area group.
            GUILayout.EndVertical();
            GUILayout.EndArea();
            #endregion;
            if (tierPlants != "" || tierSeeds != "")
            {
                #region Information Area;

                GUILayout.BeginArea(infoAreaRect);
                //begin vertical group. This means that everything under this will go from up to down
                GUILayout.BeginVertical();
                if (tierPlants != "")
                {
                    GUI.backgroundColor = Color.grey;
                    GUI.contentColor = Color.cyan;
                    GUILayout.Box(string.Format("Plants List"), recTitleSkin, GUILayout.Width(250),
                        GUILayout.Height(50));
                    GUI.backgroundColor = originalColor;
                    string[] plants = null;
                    plants = tierPlants.Split(',');
                    GUI.contentColor = Color.white;
                    foreach (string plant in plants)
                    {
                        BlockValue plantBlock = Block.GetBlockValue(plant);
                        ItemClass aux = ItemClass.GetForId(plantBlock.type);
                        string blkPlant = "";
                        if (aux != null)
                        {
                            if (aux.IsBlock())
                            {
                                int type = Block.GetBlockValue(aux.GetItemName()).type;
                                blkPlant = Block.list[type].GetLocalizedBlockName();
                            }
                            else blkPlant = aux.GetLocalizedItemName();
                        }
                        else blkPlant = plant;
                        GUILayout.Box(blkPlant, boxSkin, GUILayout.Width(250),
                            GUILayout.Height(25));
                    }
                }
                if (tierSeeds != "")
                {
                    string[] seeds = null;
                    seeds = tierSeeds.Split(',');
                    GUI.contentColor = Color.cyan;
                    GUILayout.Box(string.Format("Seeds List"), noJobSkin, GUILayout.Width(250),
                        GUILayout.Height(50));
                    GUI.contentColor = Color.white;
                    foreach (string seed in seeds)
                    {
                        ItemClass aux = ItemClass.GetItemClass(seed);
                        string blkSeed = "";
                        if (aux != null)
                        {
                            if (aux.IsBlock())
                            {
                                int type = Block.GetBlockValue(aux.GetItemName()).type;
                                blkSeed = Block.list[type].GetLocalizedBlockName();
                            }
                            else blkSeed = aux.GetLocalizedItemName();
                        }
                        else blkSeed = seed;
                        GUILayout.Box(blkSeed, boxSkin, GUILayout.Width(250),
                            GUILayout.Height(25));
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();

                #endregion;
            }
        }
        else debugHelper.doDebug(string.Format("FarmerInvScript: openGUI={0} or inventory is null", openGUI.ToString()), debug);
    }

    public void KillScript()
    {
        (player as EntityPlayerLocal).bIntroAnimActive = false;
        (player as EntityPlayerLocal).SetControllable(true);
        GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
        script = gameObject.GetComponent<FarmerInvScript>();
        if (script != null)
        {
            Destroy(this);
        }
        else
        {
            //Debug.Log("FindChildTele script not found");
        }
    }
}

public class FarmerWorkScript : MonoBehaviour
{
    private DateTime nextJobCheck = DateTime.MinValue;
    private DateTime craftEnd = DateTime.MinValue;
    private int craftCheckInterval = 10;
    private int CraftCheckArea = 2;
    private int WorkArea = 5;
    private int WorkInterval = 30;
    private TileEntitySecureLootContainer inv;
    private string matContainer = ""; // containers to look for raw materials
    private string itemContainer = ""; // containers to put final products in
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    List<Vector3i> matContainers = new List<Vector3i>();
    List<Vector3i> itemContainers = new List<Vector3i>();
    private SurvivorHelper svHelper = new SurvivorHelper();
    // farming stuff
    private string lowTier = "";
    private string medTier = "";
    private string highTier = "";
    private string expertTier = "";
    private string lowTierSeeds = "";
    private string medTierSeeds = "";
    private string highTierSeeds = "";
    private string expertTierSeeds = "";
    // sleep and food parameters
    private int checkInterval = 30;
    private int checkArea = 10;
    private string foodLow = "";
    private string foodMed = "";
    private string foodHigh = "";
    private string foodContainer = "";
    private string craftArea = "";
    DateTime nextSleepCheck = DateTime.MinValue;
    DateTime nextFoodCheck = DateTime.MinValue;
    List<Vector3i> foodContainers = new List<Vector3i>();
    private int sleepFood = 0;
    // heatmap
    private DateTime nextHeatCheck = DateTime.Now;
    private float HeatMapStrength = 0.0F;
    private ulong HeatMapWorldTime = 0;
    private int HeatInterval = 60;
    private string requirement = "Any";
    EnumAIDirectorChunkEvent heatType = EnumAIDirectorChunkEvent.Sound;
    // random sounds
    private DateTime nextRandomSound = DateTime.MinValue;
    private string workSound = "";
    private BlockValue oldBlockValue = BlockValue.Air;
    private bool debug = false;

    void Start()
    {
    }

    public void initialize(TileEntitySecureLootContainer _inv, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        inv = _inv;
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        // farming stuff
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTier"))
            lowTier = Block.list[blockValue.type].Properties.Values["LowTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTier"))
            medTier = Block.list[blockValue.type].Properties.Values["MedTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTier"))
            highTier = Block.list[blockValue.type].Properties.Values["HighTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTier"))
            expertTier = Block.list[blockValue.type].Properties.Values["ExpertTier"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LowTierSeeds"))
            lowTierSeeds = Block.list[blockValue.type].Properties.Values["LowTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MedTierSeeds"))
            medTierSeeds = Block.list[blockValue.type].Properties.Values["MedTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HighTierSeeds"))
            highTierSeeds = Block.list[blockValue.type].Properties.Values["HighTierSeeds"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ExpertTierSeeds"))
            expertTierSeeds = Block.list[blockValue.type].Properties.Values["ExpertTierSeeds"];
        WorkArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("WorkArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["WorkArea"], out WorkArea) == false) WorkArea = 10;
        }
        WorkInterval = 30;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("WorkInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["WorkInterval"], out WorkInterval) == false) WorkInterval = 30;
        }
        if (WorkInterval < 30) WorkInterval = 30;
        // containers
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MatContainer"))
            matContainer = Block.list[blockValue.type].Properties.Values["MatContainer"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("ItemContainer"))
            itemContainer = Block.list[blockValue.type].Properties.Values["ItemContainer"];
        CraftCheckArea = 2;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftCheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CraftCheckArea"], out CraftCheckArea) == false) CraftCheckArea = 2;
        }
        // food
        checkInterval = 30;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckInterval"], out checkInterval) == false) checkInterval = 30;
        }
        checkArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        foodLow = "";
        foodMed = "";
        foodHigh = "";
        foodContainer = "";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodLow"))
            foodLow = Block.list[blockValue.type].Properties.Values["FoodLow"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodMedium"))
            foodMed = Block.list[blockValue.type].Properties.Values["FoodMedium"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodHigh"))
            foodHigh = Block.list[blockValue.type].Properties.Values["FoodHigh"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodContainer"))
            foodContainer = Block.list[blockValue.type].Properties.Values["FoodContainer"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftArea"))
            craftArea = Block.list[blockValue.type].Properties.Values["CraftArea"];
        // HeatMap
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatStrength"))
            HeatMapStrength = Utils.ParseFloat(Block.list[blockValue.type].Properties.Values["HeatStrength"]);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatTime"))
            HeatMapWorldTime = ulong.Parse(Block.list[blockValue.type].Properties.Values["HeatTime"]) * 10UL;
        HeatInterval = 120;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["HeatInterval"], out HeatInterval) == false) HeatInterval = 120;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatType"))
        {
            if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Sound") heatType = EnumAIDirectorChunkEvent.Sound;
            else if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Smell") heatType = EnumAIDirectorChunkEvent.Smell;
        }
        requirement = "Any";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Requires"))
            requirement = Block.list[blockValue.type].Properties.Values["Requires"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("WorkSound"))
            workSound = Block.list[blockValue.type].Properties.Values["WorkSound"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        System.Random _rndSound = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
    }

    void Update()
    {
        string pos = "0";
        if (world != null)
        {
            BlockValue blockValue = world.GetBlock(cIdx, blockPos);
            inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it
            System.Random _rnd = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            if (!GameManager.IsDedicatedServer)
            {
                // force animation
                if (svHelper.IsSleeping(blockValue.meta2))
                {
                    if (svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "sleeping", "Sleep", "");
                    else
                    {
                        // play transition
                        svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
                else if (svHelper.IsCrafting(blockValue.meta2))
                {
                    svHelper.ForceState(gameObject, blockPos, cIdx, "working", "isWorking", workSound);
                }
                else if (!svHelper.IsSleeping(blockValue.meta2))
                {
                    if (!svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "WakeUp", "");
                    else
                    {
                        // play transition
                        svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
            }
            if (DateTime.Now > nextSleepCheck && !svHelper.IsCrafting(blockValue.meta2))
            {
                debugHelper.doDebug("FarmerWorkScript: CRAFTER CHECK meta2: " + blockValue.meta2, debug);
                nextSleepCheck = DateTime.Now.AddSeconds(_rnd.Next(7, 30)); // from 1 to 30 seconds, just so that not all change at the same time
                int sleepProb = 0;
                int wakeProb = 0;
                // this will only run on the server
                if (!world.IsRemote())
                {
                    #region sleep check;
                    if (!GameManager.Instance.World.IsDaytime())
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 80; // 80% chance of sleeping during the night
                        else wakeProb = 5; // 5% chance of waking up during the night                            
                    }
                    else if (GameManager.Instance.World.IsDaytime() && svHelper.IsSleeping(blockValue.meta2))
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 5; // 5% chance of sleeping during the day
                        else wakeProb = 80; // 80% chance of waking up during the day
                    }
                    int rndCheck = _rnd.Next(1, 101);
                    if (rndCheck <= sleepProb && sleepProb > 0)
                    {
                        pos = "4";
                        // fall asleep if not sleeping
                        pos = svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }
                    else if (rndCheck <= wakeProb && wakeProb > 0)
                    {
                        pos = "5";
                        // wakeup if sleeping
                        pos = svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }
                    #endregion;
                }
                if (!GameManager.IsDedicatedServer)
                {
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        string sound = "sleep_male_sound";
                        if (requirement != "Male") sound = "sleep_female_sound";
                        debugHelper.doDebug(string.Format("FarmerWorkScript: PLAY SOUND {0}", sound), debug);
                        //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                        Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    }
                }
            }
            if (!world.IsRemote())
            {
                if (DateTime.Now > nextFoodCheck)
                {
                    #region food check;
                    nextFoodCheck = DateTime.Now.AddSeconds(checkInterval);
                    int currentDmg = blockValue.damage;
                    int maxDmg = Block.list[blockValue.type].MaxDamage;
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        // if the survivor is sleeping it will reduce hp each 5 checks, but only by 1
                        sleepFood++;
                        if (sleepFood > 10)
                        {
                            debugHelper.doDebug(string.Format("FarmerWorkScript: SLEEPING FOR TOO LONG"), debug);
                            svHelper.DmgBlock(maxDmg, blockValue, 1, world, blockPos, cIdx);
                        }
                        else debugHelper.doDebug(string.Format("FarmerWorkScript: SLEEPING SAINLY - NO HUNGER"), debug);
                    }
                    else
                    {
                        sleepFood = 0;
                        float hpPerc = 0;
                        if (currentDmg > 0)
                            hpPerc = (float)currentDmg / (float)maxDmg * 100;
                        int hpAdd = 0;
                        string[] foodItems = null;
                        // check what is the type of food to check
                        // For example: if a survivor is BADLY hurt he will ONLY accept high food tiers!
                        if (hpPerc <= 10)
                        {
                            debugHelper.doDebug(string.Format("FarmerWorkScript: LOW tier FOOD"), debug);
                            // more then 90%HP - low tier  
                            if (foodLow != "")
                            {
                                foodItems = foodLow.Split(',');
                            }
                        }
                        else if (hpPerc <= 70)
                        {
                            debugHelper.doDebug(string.Format("FarmerWorkScript: MEDIUM tier FOOD"), debug);
                            // more then 30%HP - medium tier
                            if (foodMed != "")
                            {
                                foodItems = foodMed.Split(',');
                            }
                        }
                        else
                        {
                            debugHelper.doDebug(string.Format("FarmerWorkScript: HIGH tier FOOD"), debug);
                            // less then 30%HP - high tier 
                            if (foodHigh != "")
                            {
                                foodItems = foodHigh.Split(',');
                            }
                        }
                        if (foodItems.Length > 1 && foodContainer != "")
                        {
                            if (int.TryParse(foodItems[0], out hpAdd))
                            {
                                bool couldEat = false;
                                // look for containers in area
                                foodContainers = svHelper.GetContainers(foodContainer, world, blockPos, cIdx, checkArea);
                                //GetFoodContainers();
                                if (foodContainers.Count > 0)
                                {
                                    // search of 1 valid food item
                                    if (svHelper.EatFood(foodItems, foodContainers, world, cIdx))
                                    {
                                        // if exists, consumes it and recover ammount of hp
                                        debugHelper.doDebug(string.Format("FarmerWorkScript: EAT FOOD AND RECOVERS {0}", hpAdd), debug);
                                        svHelper.DmgBlock(maxDmg, blockValue, -hpAdd, world, blockPos, cIdx);
                                        couldEat = true;
                                    }
                                }
                                // if doesn't exist looses 2hp -> this means that, as long as there is valid food, they will easly recover.                        
                                if (!couldEat)
                                {
                                    debugHelper.doDebug(string.Format("FarmerWorkScript: NO FOOD AVAILABLE"), debug);
                                    svHelper.DmgBlock(maxDmg, blockValue, 2, world, blockPos, cIdx);
                                }
                            }
                            else debugHelper.doDebug(string.Format("FarmerWorkScript: No hp increment configured"), true);
                        }
                        else debugHelper.doDebug(string.Format("FarmerWorkScript: No food tier configured"), true);
                    }
                    #endregion;
                }
                if (!svHelper.IsSleeping(blockValue.meta2))
                {
                    #region Farming Task;

                    if (inv != null)
                    {
                        if (DateTime.Now >= nextJobCheck)
                        {
                            nextJobCheck = DateTime.Now.AddSeconds(craftCheckInterval);
                            if (svHelper.IsCrafting(blockValue.meta2))
                            {
                                debugHelper.doDebug(string.Format("FarmerWorkScript: IS WORKING UNTIL {0}",
                                    craftEnd.ToString("yyyy-MM-dd HH:mm:ss")), debug);
                                if (DateTime.Now >= craftEnd)
                                {
                                    debugHelper.doDebug(string.Format("FarmerWorkScript: FINISHED WORKING"), debug);
                                    // check for item containers
                                    itemContainers = svHelper.GetContainers(itemContainer, world, blockPos, cIdx,
                                        CraftCheckArea);
                                    debugHelper.doDebug(string.Format("FarmerWorkScript: FINISHED WORKING (1)"), debug);
                                    if (itemContainers.Count > 0)
                                    {                                        
                                        debugHelper.doDebug(string.Format("FarmerWorkScript: Looking for plants to harvest"), debug);
                                        #region Harvesting Plants;
                                        ItemStack[] items = inv.GetItems();
                                        if (items.Length > 0)
                                        {
                                            debugHelper.doDebug("FarmerWorkScript: There are item in crafter inv", debug);
                                            // if the first position in the inv has an item, that's the assigned job
                                            if (items[0] != null)
                                            {
                                                debugHelper.doDebug("FarmerWorkScript: There is item0", debug);
                                                if (items[0].count > 0)
                                                {
                                                    string[] plants = null;
                                                    if (items[0].itemValue.Meta == 1) plants = lowTier.Split(',');
                                                    else if (items[0].itemValue.Meta == 2) plants = medTier.Split(',');
                                                    else if (items[0].itemValue.Meta == 3) plants = highTier.Split(',');
                                                    else if (items[0].itemValue.Meta == 4) plants = expertTier.Split(',');
                                                    //TODO - check for seeds to plant
                                                    if (plants != null)
                                                    {
                                                        bool skillUp = false;
                                                        foreach (string str in plants)
                                                        {
                                                            // look for a block of that specific type
                                                            BlockValue plantBlockValue = Block.GetBlockValue(str);
                                                            Vector3i positionToHarvest =
                                                                svHelper.GetBlockToHarvest(plantBlockValue, world,
                                                                    blockPos, cIdx, WorkArea);
                                                            if (positionToHarvest != Vector3i.zero)
                                                            {
                                                                BlockValue blockToHarvest = world.GetBlock(cIdx,
                                                                    positionToHarvest);
                                                                if (Block.list[plantBlockValue.type] is BlockCropsGrown)
                                                                {
                                                                    debugHelper.doDebug(
                                                                        string.Format(
                                                                            "FarmerWorkScript: {0} is CropsGrown",
                                                                            str),
                                                                        debug);
                                                                    ItemStack itemStack = null;
                                                                    itemStack =
                                                                        svHelper.GetHarvestdItems(blockToHarvest,
                                                                            world, cIdx);
                                                                    if (itemStack != null)
                                                                    {
                                                                        if (svHelper.harvestPlant(itemContainers,
                                                                            itemStack, world, cIdx, debug))
                                                                        {
                                                                            //remove the block
                                                                            world.SetBlockRPC(cIdx,
                                                                                positionToHarvest, BlockValue.Air);
                                                                            skillUp = true;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    debugHelper.doDebug(
                                                                        string.Format(
                                                                            "FarmerWorkScript: Gonna HIT {0}",
                                                                            str),
                                                                        debug);
                                                                    // hits the block and stores whatever it gets
                                                                    List<ItemStack> itemList = new List<ItemStack>();
                                                                    itemList =
                                                                        svHelper.GetHarvestdItemsHit(blockToHarvest,
                                                                            world, cIdx);
                                                                    if (itemList.Count > 0)
                                                                    {
                                                                        foreach (ItemStack itemStack in itemList)
                                                                        {
                                                                            if (svHelper.harvestPlant(itemContainers,
                                                                                itemStack, world, cIdx, debug))
                                                                            {
                                                                                skillUp = true;
                                                                            }
                                                                        }
                                                                    }
                                                                    //damage the block by 1
                                                                    int maxDamage =
                                                                        Block.list[blockToHarvest.type]
                                                                            .MaxDamage;
                                                                    int dmgAmount = 0;
                                                                    dmgAmount =
                                                                        Convert.ToInt32(
                                                                            Math.Round(
                                                                                ((decimal) maxDamage/10), 0));
                                                                    svHelper.DmgBlock(maxDamage,
                                                                        blockToHarvest,
                                                                        dmgAmount,
                                                                        world, positionToHarvest, cIdx);
                                                                }
                                                            }

                                                            else debugHelper.doDebug(
                                                                        string.Format(
                                                                            "FarmerWorkScript: Could not find any {0} to harvest",
                                                                            str),
                                                                        debug);
                                                        }
                                                        if (skillUp)
                                                        {
                                                            System.Random _rndSkill =
                                                                new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                                                            if (_rndSkill.Next(0, 100) < 10)
                                                            {
                                                                // general skill increase
                                                                if (blockValue.meta < 15)
                                                                    blockValue.meta++;
                                                            }
                                                        }
                                                        craftEnd = DateTime.Now.AddSeconds(WorkInterval);
                                                    }
                                                }
                                                else debugHelper.doDebug("FarmerWorkScript: No job defined(1)", debug);
                                            }
                                            else debugHelper.doDebug("FarmerWorkScript: No job defined", debug);
                                        }
                                        else debugHelper.doDebug("FarmerWorkScript: No known receipts", debug);
                                        #endregion;
                                    }
                                    //crafting = false;
                                    debugHelper.doDebug(string.Format("FarmerWorkScript: CHANGES BIT 2 TO STOP WORKING"), debug);
                                    blockValue.meta2 = (byte)(blockValue.meta2 & ~(1 << 2));
                                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                                }
                            }
                            else
                            {
                                // check for material containers
                                matContainers = svHelper.GetContainers(matContainer, world, blockPos, cIdx, CraftCheckArea);
                                if (matContainers.Count > 0)
                                {
                                    debugHelper.doDebug("FarmerWorkScript: FOUND CONTAINER " + matContainer, debug);
                                    //check for item containers
                                    itemContainers = svHelper.GetContainers(itemContainer, world, blockPos, cIdx,
                                        CraftCheckArea);
                                }
                                if (matContainers.Count > 0 && itemContainer.Length > 0)
                                {
                                    #region Planting seeds;
                                    ItemStack[] items = inv.GetItems();
                                    if (items.Length > 0)
                                    {
                                        debugHelper.doDebug("FarmerWorkScript: There are item in crafter inv", debug);
                                        // if the first position in the inv has an item, that's the assigned job
                                        if (items[0] != null)
                                        {
                                            debugHelper.doDebug("FarmerWorkScript: There is item0", debug);
                                            if (items[0].count > 0)
                                            {
                                                string[] seeds = null;
                                                if (items[0].itemValue.Meta == 1) seeds = lowTierSeeds.Split(',');
                                                else if (items[0].itemValue.Meta == 2) seeds = medTierSeeds.Split(',');
                                                else if (items[0].itemValue.Meta == 3) seeds = highTierSeeds.Split(',');
                                                else if (items[0].itemValue.Meta == 4) seeds = expertTierSeeds.Split(',');
                                                if (seeds != null)
                                                {
                                                    bool skillUp = false;
                                                    foreach (string str in seeds)
                                                    {
                                                        debugHelper.doDebug("FarmerWorkScript: PLANTING " + str, debug);
                                                        // look for a suitable spot (namely a air block on top of fertile land)
                                                        // search for 1 seed, and removes it from the given container.
                                                        ItemClass seedClass = ItemClass.GetItemClass(str);
                                                        if (seedClass != null)
                                                        {
                                                            if (seedClass.Actions.Length >= 2)
                                                            {
                                                                if (seedClass.Actions[1] is ItemActionPlaceAsBlock)
                                                                {
                                                                    if ((seedClass.Actions[1] as ItemActionPlaceAsBlock)
                                                                            .Properties.Values.ContainsKey("Blockname"))
                                                                    {
                                                                        debugHelper.doDebug("FarmerWorkScript: HAS PLACE AS BLOCK ", debug);
                                                                        string blockname =
                                                                            (seedClass.Actions[1] as
                                                                                ItemActionPlaceAsBlock).Properties
                                                                                .Values["Blockname"];
                                                                        BlockValue plantedPlant =
                                                                            Block.GetBlockValue(blockname);
                                                                        debugHelper.doDebug("FarmerWorkScript: LOOK FOR PLACE TO PLANT " + blockname, debug);
                                                                        Vector3i placeSpot =
                                                                            svHelper.GetSpotToPlaceBlock(plantedPlant,
                                                                                world, blockPos, cIdx, WorkArea);
                                                                        if (placeSpot != Vector3i.zero)
                                                                        {
                                                                            debugHelper.doDebug("FarmerWorkScript: FOUND SPOT ", debug);
                                                                            //BlockValue _blockValue = data.item.OnConvertToBlockValue(data.itemValue, this.blockToPlace);
                                                                            // check what block it wants to place                                                                            
                                                                            ItemValue seedAux = ItemClass.GetItem(seedClass.GetItemName());
                                                                            //ItemValue seedAux = ItemClass.GetItem(str);
                                                                            ItemStack seedStack = new ItemStack(
                                                                                seedAux, 1);
                                                                            if (svHelper.checkIngredient(matContainers,
                                                                                seedStack,
                                                                                world,
                                                                                cIdx,
                                                                                true))
                                                                            {
                                                                                WorldRayHitInfo worldRayHitInfo = new WorldRayHitInfo();
                                                                                worldRayHitInfo.hit.blockPos = placeSpot;
                                                                                worldRayHitInfo.bHitValid = true;
                                                                                BlockPlacement.Result _bpResult =
                                                                                    Block.list[plantedPlant.type].BlockPlacementHelper.OnPlaceBlock(world, plantedPlant,
                                                                                        worldRayHitInfo.hit, blockPos.ToVector3());
                                                                                Block.list[plantedPlant.type].OnBlockPlaceBefore(world, ref _bpResult, null,
                                                                                    new System.Random());
                                                                                if (Block.list[plantedPlant.type].CanPlaceBlockAt(world, cIdx, _bpResult.blockPos,
                                                                                    _bpResult.blockValue))
                                                                                {
                                                                                    // spawn the plant block on this new position    
                                                                                    Block.list[plantedPlant.type].PlaceBlock(world, _bpResult, null);
                                                                                    skillUp = true;
                                                                                }
                                                                                else
                                                                                    debugHelper.doDebug("FarmerWorkScript: CANNOT PLACE BLOCK AT DEFINED POSITION " +
                                                                                                        _bpResult.blockPos.ToString(), debug);
                                                                            }
                                                                            else debugHelper.doDebug(string.Format("FarmerWorkScript: impossible to find enough {0}", str), debug);
                                                                        }
                                                                        else debugHelper.doDebug(string.Format("FarmerWorkScript: impossible to find a place to plant {0}", str), debug);
                                                                    }
                                                                    else debugHelper.doDebug(string.Format("FarmerWorkScript: {0} action 1 has no blockname defined", str), true);
                                                                }
                                                                else debugHelper.doDebug(string.Format("FarmerWorkScript: {0} action 1 is NOT placeasblock", str), true);
                                                            }
                                                            else debugHelper.doDebug(string.Format("FarmerWorkScript: {0} has no action 1 ", str), true);
                                                        }
                                                        else debugHelper.doDebug("FarmerWorkScript: Impossible to indentify " + str, true);
                                                    }
                                                    if (skillUp)
                                                    {
                                                        System.Random _rndSkill =
                                                            new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                                                        if (_rndSkill.Next(0, 100) < 10)
                                                        {
                                                            // general skill increase
                                                            if (blockValue.meta < 15)
                                                                blockValue.meta++;
                                                        }
                                                    }
                                                    blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 2));
                                                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                                                    craftEnd = DateTime.Now.AddSeconds(WorkInterval);
                                                }
                                            }
                                            else debugHelper.doDebug("FarmerWorkScript: No job defined(1)", debug);
                                        }
                                        else debugHelper.doDebug("FarmerWorkScript: No job defined", debug);
                                    }
                                    else debugHelper.doDebug("FarmerWorkScript: No known receipts", debug);
                                    #endregion;
                                }
                                else debugHelper.doDebug("FarmerWorkScript: There are no Material containers or item containers", debug);
                            }
                        }
                    }
                    else debugHelper.doDebug("FarmerWorkScript: Farmer has no inventory", true);

                    #endregion;
                }
                // only emits heatmap if working.
                if (DateTime.Now > nextHeatCheck && svHelper.IsCrafting(blockValue.meta2))
                {
                    nextHeatCheck = DateTime.Now.AddSeconds(HeatInterval);
                    if (GameManager.Instance.World.aiDirector != null)
                    {
                        if (HeatMapStrength > 0 && HeatMapWorldTime > 0)
                        {
                            GameManager.Instance.World.aiDirector.NotifyActivity(
                                heatType, blockPos, HeatMapStrength,
                                HeatMapWorldTime);
                        }
                    }
                }
            }
            // random sound
            if (!GameManager.IsDedicatedServer)
            {
                if (!svHelper.IsSleeping(blockValue.meta2) && DateTime.Now > nextRandomSound &&
                    !svHelper.IsCrafting(blockValue.meta2))
                {
                    // next random sound will be in random time too
                    System.Random _rndSound = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    // there are 4 different sound: 1 - burp, 2 - cough, 3 - hickup, 4 - fart
                    int soundT = _rndSound.Next(0, 100);
                    string sound = "";
                    if (soundT < 10)
                    {
                        sound = "burp_sound";
                    }
                    else if (soundT < 30)
                    {
                        sound = "fart_sound";
                    }
                    else if (soundT < 60)
                    {
                        sound = "hickup_sound";
                    }
                    else if (soundT < 100)
                    {
                        sound = "cough_male_sound";
                        if (requirement != "Male") sound = "cough_female_sound";
                    }
                    debugHelper.doDebug(string.Format("FarmerWorkScript: PLAY SOUND {0}", sound), debug);
                    //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                    Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
                }
            }
            oldBlockValue = blockValue;
        }
    }    
}

public class GuardInvScript : MonoBehaviour
{
    private GuardInvScript script;
    private Rect guiAreaRect = new Rect(0, 0, 0, 0);
    private Rect infoAreaRect = new Rect(0, 0, 0, 0);
    private bool openGUI = false;
    private TileEntitySecureLootContainer inv;
    EntityAlive player = null;
    int numButtons = 0;
    private string pistol = "";
    private string rifle = "";
    private string mg = "";
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    private bool debug = false;
    private string targetName = "No Target";
    private string currentAmmo = "No Ammo";
    private ItemStack[] items = null;
    private BlockValue blockValue = BlockValue.Air;
    private SurvivorHelper srcHelper = new SurvivorHelper();
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color titleColor = Color.red;
    Color buttonColor = Color.green;

    void Start()
    {

    }

    public void initialize(TileEntitySecureLootContainer _inv, EntityAlive _player, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        inv = _inv;
        player = _player;
        GetNumButtons();
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        BlockValue blockValue = world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Pistol"))
            pistol = Block.list[blockValue.type].Properties.Values["Pistol"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Rifle"))
            rifle = Block.list[blockValue.type].Properties.Values["Rifle"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MG"))
            mg = Block.list[blockValue.type].Properties.Values["MG"];

        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        debugHelper.doDebug(string.Format("GuardInvScript: Initializing custom INV with numButtons = {0}, cursor = {1}", numButtons,
            Cursor.lockState), debug);
        openGUI = true;
    }

    private void GetNumButtons()
    {
        numButtons = 7; // equip button, exit button, job label and Title
        guiAreaRect = new Rect(10, 10, 250, 60 * numButtons);
        infoAreaRect = new Rect(270, 10, 250, 60 * numButtons);
    }

    void Update()
    {
        // here we get the different stuff, like target, ammo and all that stuff?               
        // get current blockvalue
        blockValue = world.GetBlock(cIdx, blockPos);
        inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it
        items = inv.GetItems();
        if (items[1] != null)
        {
            if (items[1].count > 0)
            {
                currentAmmo = string.Format("Current Ammo: {1}x{0}", ItemClass.GetForId(items[1].itemValue.type).localizedName, items[1].count);
            }
            else currentAmmo = "No Ammo";
        }
        else currentAmmo = "No Ammo";        
        if (items[items.Length - 1] != null)
        {
            if (items[items.Length - 1].count > 0)
            {
                int entityID = items[items.Length - 1].itemValue.Meta;
                Entity target = world.GetEntity(entityID);
                //debugHelper.doDebug(string.Format("GuardInvScript: ENTITY ID = {0}", entityID), debug);
                string name = "";                
                if (target != null)
                {
                    if (!(target is EntityPlayer)) name = EntityClass.list[target.entityClass].entityClassName;
                    else name = (target as EntityPlayer).EntityName;
                    //targetName = string.Format("Current Target: {0}", name);                    
                }
                else targetName = "No Target";
            }
            else targetName = "No Target";
        }
        else targetName = "No Target";
        //debugHelper.doDebug(string.Format("GuardInvScript: TARGET = {0}", targetName), debug);
    }

    public void OnGUI()
    {
        if (openGUI && inv != null && items!=null)
        {
            // GUISTYLES **************
            GUIStyle buttonSkin = new GUIStyle(GUI.skin.button);
            GUIStyle boxSkin = new GUIStyle(GUI.skin.box);
            GUIStyle titleSkin = new GUIStyle(GUI.skin.box);
            GUIStyle noJobSkin = new GUIStyle(GUI.skin.box);
            GUIStyle recTitleSkin = new GUIStyle(GUI.skin.box);
            buttonSkin.fontSize = 14;
            titleSkin.fontSize = 17;
            titleSkin.fontStyle = FontStyle.Bold;
            titleSkin.normal.textColor = Color.cyan;
            titleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.red);
            boxSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            boxSkin.normal.textColor = Color.white;
            boxSkin.fontSize = 14;
            boxSkin.fontStyle = FontStyle.Bold;
            noJobSkin.normal.background = srcHelper.MakeTex(2, 2, Color.grey);
            noJobSkin.normal.textColor = Color.yellow;
            noJobSkin.fontSize = 15;
            noJobSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.fontSize = 16;
            recTitleSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.normal.textColor = Color.cyan;
            recTitleSkin.normal.background = srcHelper.MakeTex(2, 2, Color.green);
            // GUISTYLES **************
            GetNumButtons();
            (player as EntityPlayerLocal).SetControllable(false);
            (player as EntityPlayerLocal).bIntroAnimActive = true;
            string usableAmmo = "";
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
            #region Job area;
            //begin guilayout area
            GUILayout.BeginArea(guiAreaRect);
            //begin vertical group. This means that everything under this will go from up to down
            GUILayout.BeginVertical();
            //loop throught the list of available known "receipts"
            GUI.color = Color.magenta;
            GUILayout.Box(string.Format("Weapons Managment (lvl: {0})", blockValue.meta), titleSkin, GUILayout.Width(250),
                GUILayout.Height(50));
            GUI.color = colorNatural;
            int i = 0;
            //check if we find something
            bool buttonShown = false;
            if (items[0] != null)
            {
                if (items[0].count > 0)
                {
                    buttonShown = true;
                    //Do a gui button for every item we found on our weapon list. And make them draw their weaponLogos.    
                    string msg = ItemClass.GetForId(items[0].itemValue.type).localizedName;
                    if (items[0].itemValue.HasQuality)
                        msg = string.Format("{0} (lvl {1})",
                            ItemClass.GetForId(items[0].itemValue.type).localizedName, items[0].itemValue.Quality);
                    GUI.contentColor = Color.white;
                    if (i == 0)
                    {
                        GUI.contentColor = Color.yellow;
                        if (items[0].itemValue.HasQuality)
                            msg = string.Format("Equiped Gun: {0} (lvl {1})",
                                ItemClass.GetForId(items[0].itemValue.type).localizedName, items[0].itemValue.Quality);
                        else
                            msg = string.Format("Equiped Gun: {0}",
                                ItemClass.GetForId(items[0].itemValue.type).localizedName);
                        ItemClass gun = ItemClass.GetForId(items[0].itemValue.type);
                        usableAmmo = "";
                        if (gun.Actions.Length > 0)
                        {
                            if (gun.Actions[0].Properties.Values.ContainsKey("Magazine_items"))
                            {
                                usableAmmo = gun.Actions[0].Properties.Values["Magazine_items"];
                            }
                        }
                    }
                    GUI.backgroundColor = Color.blue;
                    if (GUILayout.Button(msg, buttonSkin,
                        GUILayout.Width(250),
                        GUILayout.Height(50)))
                    {
                        //selectedItem = item;
                        if (i == 0)
                        {
                            // places that item in player bag                            
                            (player as EntityPlayerLocal).AddUIHarvestingItem(items[0].Clone(), false);
                            (player as EntityPlayerLocal).bag.AddItem(items[0].Clone());
                            if (items[1] != null)
                            {
                                if (items[1].count > 0)
                                {
                                    // places ammo in player bag
                                    (player as EntityPlayerLocal).AddUIHarvestingItem(items[1].Clone(), false);
                                    (player as EntityPlayerLocal).bag.AddItem(items[1].Clone());
                                    items[1].Clear();
                                }
                            }
                            // clear slot 0 and 1
                            items[0].Clear();
                            inv.SetModified();
                        }
                    }
                }
            }
            GUI.contentColor = Color.yellow;
            GUI.backgroundColor = originalColor;
            if (!buttonShown && i == 0) GUILayout.Box("No gun", noJobSkin, GUILayout.Width(250),
                            GUILayout.Height(50));
            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Equip Held Item", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                // tries to "learn" the equiped weapon
                ItemStack heldItem = (player as EntityPlayerLocal).inventory.holdingItemStack;
                string heldName = ItemClass.GetForId(heldItem.itemValue.type).Name;
                //if (lowTier.Contains(heldName) || (medTier.Contains(heldName) && blockValue.meta>=35) || (highTier.Contains(heldName) && blockValue.meta >= 70) || (expertTier.Contains(heldName) && blockValue.meta >= 100))
                if (pistol == heldName || (rifle == heldName && blockValue.meta >= 6) ||
                    (mg == heldName && blockValue.meta >= 12))
                {
                    int f = 0;
                    bool foundSlot = false;
                    //foreach (ItemStack item in items)
                    {
                        bool freeStack = false;
                        if (items[0] == null) freeStack = true;
                        else if (items[0].count == 0) freeStack = true;
                        if (f == 0 && freeStack)
                        {
                            foundSlot = true;
                            ItemValue test = heldItem.itemValue.Clone();
                            ItemStack learnItem = new ItemStack(test, 1);
                            inv.UpdateSlot(f, learnItem);
                            inv.SetModified();
                            GameManager.Instance.ShowTooltip(
                                string.Format("Thank you Sir! I'm sure I'll blow some zed heads off with my new {0}",
                                    ItemClass.GetForId(heldItem.itemValue.type).localizedName));
                            (player as EntityPlayerLocal).inventory.DecHoldingItem(1);
                        }
                        f++;
                    }
                    if (!foundSlot) GameManager.Instance.ShowTooltip("Sir, I already have a gun, SIR!");
                }
                else if (pistol == heldName || rifle == heldName || mg == heldName)
                    GameManager.Instance.ShowTooltip("Sir, I'm not qualified to operate that weapon yet!");
                else GameManager.Instance.ShowTooltip("I don't understand Sir, that's not something I can use.");
            }
            GUI.contentColor = Color.red;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button("Close Guard", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                //if we clicked the button it will but that weapon to our selected(equipped) weapon
                openGUI = false;
                // Kills the scripts after it updates the inventory
                //inv.SetModified();
                KillScript();
            }
            //We need to close vertical gpr and gui area group.
            GUILayout.EndVertical();
            GUILayout.EndArea();
            #endregion;
            if (true)
            {
                #region Information Area;

                GUILayout.BeginArea(infoAreaRect);
                //begin vertical group. This means that everything under this will go from up to down
                GUILayout.BeginVertical();
                GUI.backgroundColor = Color.grey;
                GUI.contentColor = Color.cyan;
                GUILayout.Box(string.Format("Authorized Guns List"), recTitleSkin, GUILayout.Width(250),
                    GUILayout.Height(25));
                GUI.backgroundColor = originalColor;

                string gunStr = "";
                ItemClass gun = null;
                if (pistol != "")
                {
                    gun = ItemClass.GetItemClass(pistol);
                    string ammoName = "";
                    if (gun.Actions.Length > 0)
                    {
                        if (gun.Actions[0].Properties.Values.ContainsKey("Magazine_items"))
                        {
                            ammoName = gun.Actions[0].Properties.Values["Magazine_items"];
                            ammoName = ItemClass.GetItemClass(ammoName).localizedName;
                        }
                    }
                    gunStr = string.Format("{0} ({1})", gun.localizedName, ammoName);
                    GUI.backgroundColor = originalColor;
                    GUI.contentColor = Color.white;
                    GUILayout.Box(gunStr, boxSkin, GUILayout.Width(250),
                        GUILayout.Height(25));
                }
                if (rifle != "")
                {
                    if (blockValue.meta >= 6) GUI.contentColor = Color.white;
                    else GUI.contentColor = Color.grey;
                    gun = ItemClass.GetItemClass(rifle);
                    string ammoName = "";
                    if (gun.Actions.Length > 0)
                    {

                        if (gun.Actions[0].Properties.Values.ContainsKey("Magazine_items"))
                        {
                            ammoName = gun.Actions[0].Properties.Values["Magazine_items"];
                            ammoName = ItemClass.GetItemClass(ammoName).localizedName;
                        }
                    }

                    gunStr = string.Format("{0} ({1})", gun.localizedName, ammoName);
                    GUI.backgroundColor = originalColor;
                    GUI.contentColor = Color.white;
                    GUILayout.Box(gunStr, boxSkin, GUILayout.Width(250),
                        GUILayout.Height(25));
                }
                if (mg != "")
                {
                    if (blockValue.meta >= 12) GUI.contentColor = Color.white;
                    else GUI.contentColor = Color.grey;
                    gun = ItemClass.GetItemClass(mg);
                    string ammoName = "";
                    if (gun.Actions.Length > 0)
                    {

                        if (gun.Actions[0].Properties.Values.ContainsKey("Magazine_items"))
                        {
                            ammoName = gun.Actions[0].Properties.Values["Magazine_items"];
                            ammoName = ItemClass.GetItemClass(ammoName).localizedName;
                        }
                    }

                    gunStr = string.Format("{0} ({1})", gun.localizedName, ammoName);
                    GUI.backgroundColor = originalColor;
                    GUI.contentColor = Color.white;
                    GUILayout.Box(gunStr, boxSkin, GUILayout.Width(250),
                        GUILayout.Height(25));
                }
                GUI.backgroundColor = originalColor;
                GUI.contentColor = Color.yellow;
                string msg = currentAmmo;
                GUILayout.Box(msg, noJobSkin, GUILayout.Width(250), GUILayout.Height(25));
                // button to load ammo       
                GUI.contentColor = Color.green;
                GUI.backgroundColor = buttonColor;
                if (GUILayout.Button("Give Ammo", buttonSkin, GUILayout.Width(250),
                                GUILayout.Height(50)))
                {
                    ItemStack heldItem = (player as EntityPlayerLocal).inventory.holdingItemStack;
                    string heldName = ItemClass.GetForId(heldItem.itemValue.type).Name;
                    debugHelper.doDebug(string.Format("HELD: {0}, USABLE: {1}", heldName, usableAmmo), debug);
                    if (usableAmmo == heldName)
                    {
                        int f = 1;
                        bool foundSlot = false;
                        //foreach (ItemStack item in items)
                        {
                            bool freeStack = false;
                            if (items[f] == null) freeStack = true;
                            else if (items[f].count == 0) freeStack = true;
                            else if (items[f].CanStackWith(heldItem)) freeStack = true;
                            else if (items[f].CanStackPartlyWith(heldItem)) freeStack = true;
                            if (f == 1 && freeStack)
                            {
                                foundSlot = true;
                                int number = heldItem.count;
                                if (items[f] == null)
                                {
                                    inv.UpdateSlot(f, heldItem.Clone());
                                    inv.SetModified();
                                }
                                else if (items[f].count == 0)
                                {
                                    inv.UpdateSlot(f, heldItem.Clone());
                                    inv.SetModified();
                                }
                                else
                                {
                                    if (items[f].CanStackPartly(ref number))
                                    {
                                        items[f].count += number;
                                        inv.SetModified();
                                    }
                                }
                                GameManager.Instance.ShowTooltip(
                                    string.Format("Thank you Sir! I needed that!"));
                                (player as EntityPlayerLocal).inventory.DecHoldingItem(number);
                            }
                            f++;
                        }
                        if (!foundSlot) GameManager.Instance.ShowTooltip("Sir, I can't hold any more ammo, SIR!");
                    }
                    else GameManager.Instance.ShowTooltip("Sir, I do not need that!");
                }
                GUI.contentColor = Color.green;
                buttonShown = false;
                msg = "No Bullet Casings to recover";
                if (items[3] != null)
                {
                    if (items[3].count > 0)
                    {
                        buttonShown = true;
                        msg = string.Format("Recover {0} Bullet Casings", items[3].count);
                    }
                    else msg = "No Bullet Casings to recover";
                }
                else msg = "No Bullet Casings to recover";
                if (buttonShown)
                {
                    GUI.contentColor = Color.green;
                    GUI.backgroundColor = buttonColor;
                    if (GUILayout.Button(msg, buttonSkin, GUILayout.Width(250),
                        GUILayout.Height(50)))
                    {
                        // recover bulletCasing
                        (player as EntityPlayerLocal).AddUIHarvestingItem(items[3].Clone(), false);
                        (player as EntityPlayerLocal).bag.AddItem(items[3].Clone());
                        items[3].Clear();
                        inv.SetModified();
                    }
                }
                else
                {
                    GUI.backgroundColor = originalColor;
                    GUI.contentColor = Color.yellow;
                    GUILayout.Box(msg, noJobSkin, GUILayout.Width(250), GUILayout.Height(25));
                }
                // Aquired target
                GUI.backgroundColor = buttonColor;
                GUI.contentColor = Color.yellow;
                msg = targetName;
                GUILayout.Box(msg, boxSkin, GUILayout.Width(250), GUILayout.Height(25));
                GUILayout.EndVertical();
                GUILayout.EndArea();
                #endregion;
            }
        }
        else debugHelper.doDebug(string.Format("GuardInvScript: openGUI={0} or inventory is null", openGUI.ToString()), debug);
    }

    public void KillScript()
    {
        (player as EntityPlayerLocal).bIntroAnimActive = false;
        (player as EntityPlayerLocal).SetControllable(true);
        GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
        script = gameObject.GetComponent<GuardInvScript>();
        if (script != null)
        {
            Destroy(this);
        }
        else
        {
            //Debug.Log("FindChildTele script not found");
        }
    }
}

public class GuardWorkScript : MonoBehaviour
{
    private DateTime nextShootCheck = DateTime.MinValue;
    private int ShootDelay = 1000; // miliseconds
    private int craftCheckInterval = 10;
    private int CraftCheckArea = 2;
    private int WorkArea = 5;
    private int WorkInterval = 30;
    private TileEntitySecureLootContainer inv;
    private string matContainer = ""; // containers to look for raw materials
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    List<Vector3i> matContainers = new List<Vector3i>();
    private SurvivorHelper svHelper = new SurvivorHelper();
    // farming stuff
    private string pistol = "";
    private string rifle = "";
    private string mg = "";
    private string pistol_sound = "";
    private string rifle_sound = "";
    private string mg_sound = "";
    // sleep and food parameters
    private int checkInterval = 30;
    private int checkArea = 10;
    private string foodLow = "";
    private string foodMed = "";
    private string foodHigh = "";    
    private string foodContainer = "";
    private string craftArea = "";
    private string foodAwake = "";
    private int awakeTime = 0;
    DateTime nextSleepCheck = DateTime.MinValue;
    DateTime nextFoodCheck = DateTime.MinValue;
    List<Vector3i> foodContainers = new List<Vector3i>();
    private int sleepFood = 0;
    // heatmap
    private DateTime nextHeatCheck = DateTime.Now;
    private float HeatMapStrength = 0.0F;
    private ulong HeatMapWorldTime = 0;
    private int HeatInterval = 60;
    private string requirement = "Any";
    EnumAIDirectorChunkEvent heatType = EnumAIDirectorChunkEvent.Sound;
    // random sounds
    private DateTime nextRandomSound = DateTime.MinValue;
    private string workSound = "";
    private BlockValue oldBlockValue = BlockValue.Air;
    private bool debug = false;
    bool hasGun = false;
    int ammoAmount = 0;
    bool oldHasGun = false;
    string gunName = "";
    string gunType = "";
    // shooting
    ItemActionRanged attack = null;
    Transform Muzzle;
    LineRenderer Tracer;
    int TracerCount = 0;
    DateTime TracerEnd = DateTime.MinValue;
    public Transform TargetTransform;
    public Transform Rotation;
    //string OwnerName;
    //EntityPlayer OwnerEntity;
    public Entity Target;
    public float speed = 4.5f;
    DateTime findTargetDelay = DateTime.MinValue;
    private bool fireShot = false;
    private bool oldFireShot = false;
    private int burstCount = 0;
    private bool burstFire = false;
    private bool firstrun = false;
    private DateTime outOfSightCount = DateTime.MinValue;

    void Start()
    {
    }

    public void initialize(TileEntitySecureLootContainer _inv, WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        oldHasGun = false;
        hasGun = false;
        firstrun = true;
        Rotation = gameObject.transform;        
        inv = _inv;        
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        blockValue = _world.GetBlock(cIdx, blockPos);
        // guard stuff
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Pistol"))
            pistol = Block.list[blockValue.type].Properties.Values["Pistol"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Rifle"))
            rifle = Block.list[blockValue.type].Properties.Values["Rifle"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MG"))
            mg = Block.list[blockValue.type].Properties.Values["MG"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Pistol_Sound"))
            pistol_sound = Block.list[blockValue.type].Properties.Values["Pistol_Sound"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Rifle_Sound"))
            rifle_sound = Block.list[blockValue.type].Properties.Values["Rifle_Sound"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MG_Sound"))
            mg_sound = Block.list[blockValue.type].Properties.Values["MG_Sound"];
        WorkArea = 0;
        WorkInterval = 0;
        // containers
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("AmmoContainer"))
            matContainer = Block.list[blockValue.type].Properties.Values["AmmoContainer"];
        CraftCheckArea = 2;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftCheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CraftCheckArea"], out CraftCheckArea) == false) CraftCheckArea = 2;
        }
        // food
        checkInterval = 30;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckInterval"], out checkInterval) == false) checkInterval = 30;
        }
        checkArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        foodLow = "";
        foodMed = "";
        foodHigh = "";
        foodContainer = "";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodLow"))
            foodLow = Block.list[blockValue.type].Properties.Values["FoodLow"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodMedium"))
            foodMed = Block.list[blockValue.type].Properties.Values["FoodMedium"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodHigh"))
            foodHigh = Block.list[blockValue.type].Properties.Values["FoodHigh"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodContainer"))
            foodContainer = Block.list[blockValue.type].Properties.Values["FoodContainer"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CraftArea"))
            craftArea = Block.list[blockValue.type].Properties.Values["CraftArea"];
        // Awake Food
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FoodAwake"))
            foodAwake = Block.list[blockValue.type].Properties.Values["FoodAwake"];
        awakeTime = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("AwakeTime"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["AwakeTime"], out awakeTime) == false) awakeTime = 0;
        }
        // HeatMap
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatStrength"))
            HeatMapStrength = Utils.ParseFloat(Block.list[blockValue.type].Properties.Values["HeatStrength"]);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatTime"))
            HeatMapWorldTime = ulong.Parse(Block.list[blockValue.type].Properties.Values["HeatTime"]) * 10UL;
        HeatInterval = 120;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatInterval"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["HeatInterval"], out HeatInterval) == false) HeatInterval = 120;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("HeatType"))
        {
            if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Sound") heatType = EnumAIDirectorChunkEvent.Sound;
            else if (Block.list[blockValue.type].Properties.Values["HeatType"] == "Smell") heatType = EnumAIDirectorChunkEvent.Smell;
        }
        requirement = "Any";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Requires"))
            requirement = Block.list[blockValue.type].Properties.Values["Requires"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("WorkSound"))
            workSound = Block.list[blockValue.type].Properties.Values["WorkSound"];
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }        
        System.Random _rndSound = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
        world = _world;
    }

    void Update()
    {
        string pos = "0";
        if (world != null)
        {
            blockValue = world.GetBlock(cIdx, blockPos);
            inv = world.GetTileEntity(cIdx, blockPos) as TileEntitySecureLootContainer; // to make sure it's always updated no matter whom is doing it            
            ItemStack[] items = null;
            if (inv!=null)
            {
                items = inv.GetItems();
                if (world.IsRemote())
                {
                    if (!oldFireShot && svHelper.FireShot(blockValue.meta2)) fireShot = true;
                    else fireShot = false;
                    oldFireShot = svHelper.FireShot(blockValue.meta2);
                    // clients will keep the target updated as much as possible
                    if (items[items.Length - 1] != null)
                    {
                        if (items[items.Length - 1].count > 0)
                        {
                            int entityID = items[items.Length - 1].itemValue.Meta;
                            //debugHelper.doDebug("GuardWorkScript: GUARD TARGET ID: " + entityID, debug);
                            Target = world.GetEntity(entityID);
                            if (Target != null) TargetTransform = Target.transform;
                        }
                        else if (Target != null)
                        {
                            Target = null;
                            TargetTransform = null;
                        }
                    }
                    else if (Target != null)
                    {
                        Target = null;
                        TargetTransform = null;
                    }
                }
            }
            //if (!GameManager.IsDedicatedServer)
            //{
            //    // force animation
            //    if (svHelper.IsSleeping(blockValue.meta2))
            //    {
            //        if (svHelper.IsSleeping(oldBlockValue.meta2))
            //            svHelper.ForceState(gameObject, blockPos, cIdx, "sleeping", "Sleep", "");
            //        else
            //        {
            //            // play transition
            //            svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
            //        }
            //    }
            //    else if (!svHelper.IsSleeping(blockValue.meta2))
            //    {
            //        if (!svHelper.IsSleeping(oldBlockValue.meta2))
            //            svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "WakeUp", "");
            //        else
            //        {
            //            // play transition
            //            svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
            //        }
            //    }
            //}
            if (!GameManager.IsDedicatedServer)
            {
                // force animation
                if (svHelper.IsSleeping(blockValue.meta2))
                {
                    if (svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "sleeping", "Sleep", "");
                    else
                    {
                        // play transition
                        svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
                else if (!svHelper.IsSleeping(blockValue.meta2) && svHelper.IsSleeping(oldBlockValue.meta2) && !svHelper.IsCrafting(blockValue.meta2))
                {
                    if (!svHelper.IsSleeping(oldBlockValue.meta2))
                        svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "WakeUp", "");
                    else
                    {
                        // play transition
                        svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                    }
                }
                if (svHelper.IsCrafting(blockValue.meta2) && !svHelper.IsCrafting(oldBlockValue.meta2))
                {
                    debugHelper.doDebug("GuardWorkScript: GOTO SHOOTING", debug);
                    svHelper.ForceState(gameObject, blockPos, cIdx, "working", "isShooting", workSound);
                }
                else if (!svHelper.IsCrafting(blockValue.meta2) && svHelper.IsCrafting(oldBlockValue.meta2))
                {
                    debugHelper.doDebug("GuardWorkScript: GOTO IDLE", debug);
                    svHelper.ForceState(gameObject, blockPos, cIdx, "idle", "stopShooting", "");
                }                
            }
            oldBlockValue = blockValue;
            System.Random _rnd = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));            
            if (DateTime.Now > nextSleepCheck && !svHelper.IsCrafting(blockValue.meta2))
            {
                debugHelper.doDebug("GuardWorkScript: GUARD CHECK meta2: " + blockValue.meta2, debug);
                nextSleepCheck = DateTime.Now.AddSeconds(_rnd.Next(7, 30)); // from 1 to 30 seconds, just so that not all change at the same time
                int sleepProb = 0;
                int wakeProb = 0;
                // this will only run on the server
                if (!world.IsRemote())
                {
                    #region sleep check;
                    if (!GameManager.Instance.World.IsDaytime())
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 80; // 80% chance of sleeping during the night
                        else wakeProb = 5; // 5% chance of waking up during the night                            
                    }
                    else if (GameManager.Instance.World.IsDaytime() && svHelper.IsSleeping(blockValue.meta2))
                    {
                        if (!svHelper.IsSleeping(blockValue.meta2))
                            sleepProb = 5; // 5% chance of sleeping during the day
                        else wakeProb = 80; // 80% chance of waking up during the day
                    }
                    bool foundAwakeFood = false;
                    if (wakeProb > 0)
                    {
                        // trying to wakeup
                        foundAwakeFood = FindAwakeFood();
                        if (foundAwakeFood) wakeProb = 101;
                    }
                    else if (sleepProb > 0)
                    {
                        // trying to sleep
                        foundAwakeFood = FindAwakeFood();
                        if (foundAwakeFood) sleepProb = 0;
                    }
                    int rndCheck = _rnd.Next(1, 101);
                    if (rndCheck <= sleepProb && sleepProb > 0)
                    {
                        // if he wants to sleep, he will try to find a coffee to stay awake.  
                        pos = "4";
                        // fall asleep if not sleeping
                        pos = svHelper.SleepGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        if (!foundAwakeFood)
                            nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }
                    else if (rndCheck <= wakeProb && wakeProb > 0)
                    {
                        pos = "5";
                        // wakeup if sleeping
                        pos = svHelper.WakeupGeneric(gameObject, pos, blockValue, blockPos, cIdx, world);
                        if (!foundAwakeFood) nextSleepCheck = DateTime.Now.AddSeconds(30);
                    }

                    #endregion;
                }
                if (!GameManager.IsDedicatedServer)
                {
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        string sound = "sleep_male_sound";
                        if (requirement != "Male") sound = "sleep_female_sound";
                        //debugHelper.doDebug(string.Format("GuardWorkScript: PLAY SOUND {0}", sound), debug);
                        //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                        Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    }
                }
            }
            if (!world.IsRemote())
            {
                if (DateTime.Now > nextFoodCheck)
                {
                    #region food check;
                    nextFoodCheck = DateTime.Now.AddSeconds(checkInterval);
                    int currentDmg = blockValue.damage;
                    int maxDmg = Block.list[blockValue.type].MaxDamage;
                    if (svHelper.IsSleeping(blockValue.meta2))
                    {
                        // if the survivor is sleeping it will reduce hp each 5 checks, but only by 1
                        sleepFood++;
                        if (sleepFood > 10)
                        {
                            debugHelper.doDebug(string.Format("GuardWorkScript: SLEEPING FOR TOO LONG"), debug);
                            svHelper.DmgBlock(maxDmg, blockValue, 1, world, blockPos, cIdx);
                        }
                        else debugHelper.doDebug(string.Format("GuardWorkScript: SLEEPING SAINLY - NO HUNGER"), debug);
                    }
                    else
                    {
                        sleepFood = 0;
                        float hpPerc = 0;
                        if (currentDmg > 0)
                            hpPerc = (float)currentDmg / (float)maxDmg * 100;
                        int hpAdd = 0;
                        string[] foodItems = null;
                        // check what is the type of food to check
                        // For example: if a survivor is BADLY hurt he will ONLY accept high food tiers!
                        if (hpPerc <= 10)
                        {
                            debugHelper.doDebug(string.Format("GuardWorkScript: LOW tier FOOD"), debug);
                            // more then 90%HP - low tier  
                            if (foodLow != "")
                            {
                                foodItems = foodLow.Split(',');
                            }
                        }
                        else if (hpPerc <= 70)
                        {
                            debugHelper.doDebug(string.Format("GuardWorkScript: MEDIUM tier FOOD"), debug);
                            // more then 30%HP - medium tier
                            if (foodMed != "")
                            {
                                foodItems = foodMed.Split(',');
                            }
                        }
                        else
                        {
                            debugHelper.doDebug(string.Format("GuardWorkScript: HIGH tier FOOD"), debug);
                            // less then 30%HP - high tier 
                            if (foodHigh != "")
                            {
                                foodItems = foodHigh.Split(',');
                            }
                        }
                        if (foodItems.Length > 1 && foodContainer != "")
                        {
                            if (int.TryParse(foodItems[0], out hpAdd))
                            {
                                bool couldEat = false;
                                // look for containers in area
                                foodContainers = svHelper.GetContainers(foodContainer, world, blockPos, cIdx, checkArea);
                                //GetFoodContainers();
                                if (foodContainers.Count > 0)
                                {
                                    // search of 1 valid food item
                                    if (svHelper.EatFood(foodItems, foodContainers, world, cIdx))
                                    {
                                        // if exists, consumes it and recover ammount of hp
                                        debugHelper.doDebug(string.Format("GuardWorkScript: EAT FOOD AND RECOVERS {0}", hpAdd), debug);
                                        svHelper.DmgBlock(maxDmg, blockValue, -hpAdd, world, blockPos, cIdx);
                                        couldEat = true;
                                    }
                                }
                                // if doesn't exist looses 2hp -> this means that, as long as there is valid food, they will easly recover.                        
                                if (!couldEat)
                                {
                                    debugHelper.doDebug(string.Format("GuardWorkScript: NO FOOD AVAILABLE"), debug);
                                    svHelper.DmgBlock(maxDmg, blockValue, 2, world, blockPos, cIdx);
                                }
                            }
                            else debugHelper.doDebug(string.Format("GuardWorkScript: No hp increment configured"), true);
                        }
                        else debugHelper.doDebug(string.Format("GuardWorkScript: No food tier configured"), true);
                    }
                    #endregion;
                }                
                // only emits heatmap if working.
                if (DateTime.Now > nextHeatCheck && svHelper.IsCrafting(blockValue.meta2))
                {
                    nextHeatCheck = DateTime.Now.AddSeconds(HeatInterval);
                    if (GameManager.Instance.World.aiDirector != null)
                    {
                        if (HeatMapStrength > 0 && HeatMapWorldTime > 0)
                        {
                            GameManager.Instance.World.aiDirector.NotifyActivity(
                                heatType, blockPos, HeatMapStrength,
                                HeatMapWorldTime);
                        }
                    }
                }
            }
            if (!svHelper.IsSleeping(blockValue.meta2))
            {
                //debugHelper.doDebug("GuardWorkScript: DOING SHOOT CHECK", debug);
                #region Shoot Task;
                if (inv != null)
                {                                        
                    if (items.Length > 0)
                    {
                        // if the first position in the inv has an item, that's the assigned job
                        if (items[0] != null)
                        {
                            if (items[0].count > 0)
                            {
                                hasGun = true;
                                gunName = ItemClass.GetForId(items[0].itemValue.type).Name;
                                if (gunName == mg)
                                {
                                    ShootDelay = 1000;
                                    gunType = "MG";
                                    burstFire = true;
                                }
                                else if (gunName == rifle)
                                {
                                    gunType = "Rifle";
                                    ShootDelay = 1000;
                                    burstFire = false;
                                }
                                else
                                {
                                    gunType = "Pistol";
                                    ShootDelay = 1000;
                                    burstFire = false;
                                }
                            }
                            else hasGun = false;
                        }
                        else hasGun = false;
                        if (items[1] != null)
                        {
                            if (items[1].count > 0)
                            {
                                ammoAmount = items[1].count;
                            }
                            else ammoAmount = 0;
                        }
                        else ammoAmount = 0;

                        // attack action
                        if (items[0] != null && items[1] != null && attack == null && hasGun && ammoAmount >= 0)
                        {
                            attack = (ItemClass.GetForId(items[0].itemValue.type).Actions[0] as ItemActionRanged);
                            (attack as ItemActionRanged).ReadFrom(attack.Properties);
                            attack.Range = AttributeBase.GetVal<AttributeFalloffRange>(items[0].itemValue, 10);
                            float damageAux = 1;
                            //damageAux = GetDamageBlock(items[1], items[0], blockValue.meta);
                            damageAux = 0.0F; // no need to calculate here
                            attack.DamageBlock = new DataItem<float>(damageAux);
                            //damageAux = GetDamageEntity(items[1], items[0], blockValue.meta);
                            attack.DamageEntity = new DataItem<int>(Convert.ToInt32(Math.Floor(damageAux)));
                            attack.SoundStart =
                                new DataItem<string>(
                                    ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                        "Sound_start"]);
                            attack.SoundRepeat =
                                new DataItem<string>(
                                    ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                        "Sound_repeat"]);
                            if (gunType == "MG" && mg_sound!="") attack.SoundStart = new DataItem<string>(mg_sound);
                            else if (gunType == "Pistol" && pistol_sound != "") attack.SoundStart = new DataItem<string>(pistol_sound);
                            else if (gunType == "Rifle" && rifle_sound != "") attack.SoundStart = new DataItem<string>(rifle_sound);
                            attack.SoundEnd =
                                new DataItem<string>(
                                    ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                        "Sound_end"]);
                            attack.SoundEmpty = new DataItem<string>(
                                ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                    "Sound_empty"]);
                            attack.MagazineSize =
                                new DataItem<int>(
                                    Convert.ToInt32(
                                        ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                            "Magazine_size"]));
                            attack.MagazineItem =
                                new DataItem<string>(
                                    ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                        "Magazine_items"]);
                            attack.ReloadTime =
                                new DataItem<float>(
                                    Utils.ParseFloat(
                                        ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                            "Reload_time"]));
                            attack.Delay =
                                Utils.ParseFloat(
                                    ItemClass.GetForId(items[0].itemValue.type).Actions[0].Properties.Values[
                                        "Delay"]);
                            attack.AutoFire = true;
                            attack.RaysPerShot = 10;
                            attack.RaysSpread = 0.75f;
                            //debugHelper.doDebug(
                            //    string.Format("RANGE: {0}, sound={1}, DmgBlock={2}, DmgEntity={3}", attack.Range,
                            //        attack.SoundStart, attack.DamageBlock, attack.DamageEntity), debug);
                        }
                    }
                    if ((oldHasGun && !hasGun) || (firstrun && !hasGun))
                    {                        
                        debugHelper.doDebug("GuardWorkScript: HIDE WEAPON FIRST RUN = " + firstrun, debug);
                        // always goes to pistol, and hides it                   
                        svHelper.ChangeTransformState(gameObject, false, "MG", "");
                        svHelper.ChangeTransformState(gameObject, false, "Pistol", "");
                        svHelper.ChangeTransformState(gameObject, false, "Rifle", "");
                        svHelper.ChangeTransformState(gameObject, true, "Idle", "");                        
                        attack = null;
                        burstFire = false;
                        burstCount = 0;
                        SetTargetToNull();
                    }
                    else if (!oldHasGun && hasGun)
                    {
                        debugHelper.doDebug("GuardWorkScript: SHOW WEAPON " + gunType, debug);
                        svHelper.ChangeTransformState(gameObject, false, "Idle", "");
                        if (gunType == "MG")
                        {
                            svHelper.ChangeTransformState(gameObject, true, "MG", "");
                            svHelper.ChangeTransformState(gameObject, false, "Pistol", "");
                            svHelper.ChangeTransformState(gameObject, false, "Rifle", "");                            
                            svHelper.ChangeTransformState(gameObject, true, "mp5Braun", gunName);
                        }
                        else if (gunType == "Rifle")
                        {
                            svHelper.ChangeTransformState(gameObject, false, "MG", "");
                            svHelper.ChangeTransformState(gameObject, false, "Pistol", "");
                            svHelper.ChangeTransformState(gameObject, true, "Rifle", "");
                            svHelper.ChangeTransformState(gameObject, true, "gun", gunName);
                        }
                        else if (gunType == "Pistol")
                        {
                            svHelper.ChangeTransformState(gameObject, false, "MG", "");
                            svHelper.ChangeTransformState(gameObject, true, "Pistol", "");
                            svHelper.ChangeTransformState(gameObject, false, "Rifle", "");
                            svHelper.ChangeTransformState(gameObject, true, "MonsterEagleAttachment", gunName);                            
                        }
                        else hasGun = false;
                        Muzzle = svHelper.FindBody(gameObject.transform, "Muzzle");
                    }

                    oldHasGun = hasGun;
                    firstrun = false;                    
                    if (Muzzle != null)
                    {
                        if (true)
                        {
                            if (world.IsRemote() && svHelper.IsCrafting(blockValue.meta2) && fireShot)
                            {
                                // CLIENT ONLY PLAYS SOUND
                                if (items[1] != null && items[0] != null && ammoAmount > 0)
                                {
                                    Quaternion rot = Rotation.rotation;
                                    svHelper.SetAnimationSpeed(gameObject, "working", 1);
                                    Rotation.rotation = rot;
                                    debugHelper.doDebug("GuardWorkScript: (REMOTE) SHOOT", debug);
                                    doShoot(items[1], items[0], blockValue.meta);
                                }
                            }
                            else if (svHelper.IsCrafting(blockValue.meta2) && DateTime.Now > nextShootCheck)
                            {
                                //if (Target != null)
                                //    debugHelper.doDebug(
                                //        string.Format(
                                //            "GuardWorkScript: IS WORKING WITH ammoAmmount={0}, hasGun={1}, targetID={2}, fireShot={3}",
                                //            ammoAmount, hasGun, Target.entityId, fireShot), debug);
                                //else debugHelper.doDebug(
                                //        string.Format(
                                //            "GuardWorkScript: IS WORKING WITH ammoAmmount={0}, hasGun={1}, NO TARGET??, fireShot={2}",
                                //            ammoAmount, hasGun, fireShot), debug);
                                // most of this only runs on the server
                                if (Target != null)
                                {
                                    if (!world.IsRemote())
                                    {
                                        if (hasGun && ammoAmount > 0 && fireShot)
                                        {
                                            bool skillUp = false;
                                            // check if there's any target                                    
                                            if (Target != null)
                                            {
                                                if (Target.IsAlive())
                                                {
                                                    if (
                                                        Vector3.Distance(Target.transform.position,
                                                            gameObject.transform.position) > attack.Range)
                                                    {
                                                        if (!world.IsRemote())
                                                        {
                                                            debugHelper.doDebug(
                                                                "GuardWorkScript: TARRGETTONULL -> TARGET OUT OF RANGE",
                                                                debug);
                                                            SetTargetToNull();
                                                            burstFire = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // shoots
                                                        debugHelper.doDebug("GuardWorkScript: SHOOT", debug);
                                                        if (items[1] != null && items[0] != null)
                                                        {
                                                            doShoot(items[1], items[0], blockValue.meta);
                                                            if (!world.IsRemote() && Target != null)
                                                            {
                                                                // decreases ammo
                                                                items[1].count--;
                                                                // chance to recover the casing, based on skill
                                                                // level 15 -> 100%
                                                                int chance =
                                                                    Convert.ToInt32(
                                                                        Math.Floor(100*(double) blockValue.meta/15));
                                                                if (_rnd.Next(0, 100) < chance)
                                                                {
                                                                    ItemStack casingStack =
                                                                        new ItemStack(
                                                                            new ItemValue(
                                                                                ItemClass.GetItem("bulletCasing").type,
                                                                                false),
                                                                            1);
                                                                    int number = 1;
                                                                    if (items[3] == null)
                                                                    {
                                                                        inv.UpdateSlot(3, casingStack.Clone());
                                                                    }
                                                                    else if (items[3].count == 0)
                                                                    {
                                                                        inv.UpdateSlot(3, casingStack.Clone());
                                                                    }
                                                                    else
                                                                    {
                                                                        if (items[3].CanStackPartly(ref number))
                                                                        {
                                                                            items[3].count += number;
                                                                        }
                                                                    }
                                                                }
                                                                inv.SetModified();
                                                                //TODO - Decay weapon.
                                                                skillUp = true;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (!world.IsRemote())
                                                    {
                                                        debugHelper.doDebug(
                                                            "GuardWorkScript: TARRGETTONULL -> Target is dead", debug);
                                                        SetTargetToNull();
                                                        burstFire = false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // stop shooting
                                                if (!world.IsRemote())
                                                {
                                                    debugHelper.doDebug(
                                                        "GuardWorkScript: Guard has no target and will stop shooting",
                                                        debug);
                                                    blockValue.meta2 = (byte) (blockValue.meta2 & ~(1 << 2));
                                                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                                                    burstFire = false;
                                                }
                                            }
                                            if (skillUp && !world.IsRemote())
                                            {
                                                System.Random _rndSkill =
                                                    new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                                                // 15% chance of skilling up...
                                                if (_rndSkill.Next(0, 100) < 15)
                                                {
                                                    debugHelper.doDebug("GuardWorkScript: DING DING", debug);
                                                    // general skill increase
                                                    if (blockValue.meta < 15)
                                                    {
                                                        blockValue.meta++;
                                                        world.SetBlockRPC(cIdx, blockPos, blockValue);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!world.IsRemote())
                                            {
                                                debugHelper.doDebug(
                                                    string.Format(
                                                        "GuardWorkScript: Guard has no gun ({1}) or no ammo ({0}) and will stop shooting",
                                                        ammoAmount, hasGun), debug);
                                                SetTargetToNull();
                                                blockValue.meta2 = (byte) (blockValue.meta2 & ~(1 << 2));
                                                world.SetBlockRPC(cIdx, blockPos, blockValue);
                                            }
                                        }
                                        if (burstFire && fireShot && Target != null && !world.IsRemote())
                                        {
                                            burstCount++;
                                            if (burstCount < 2)
                                                nextShootCheck = DateTime.Now.AddMilliseconds(attack.Delay);
                                            else
                                            {
                                                burstCount = 0;
                                                setFireShot(false);
                                                nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay);
                                            }
                                        }
                                        else
                                        {
                                            setFireShot(false);
                                            nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay);
                                        }
                                    }                                    
                                }
                                else
                                {
                                    if (!world.IsRemote())
                                    {
                                        blockValue.meta2 = (byte)(blockValue.meta2 & ~(1 << 2));
                                        world.SetBlockRPC(cIdx, blockPos, blockValue);
                                        setFireShot(false);
                                    }
                                }
                            }
                            //else
                            //keeps doing this
                            {
                                if (!hasGun)
                                {
                                    //debugHelper.doDebug(
                                    //    "GuardWorkScript: Guard has no gun and will NOT search for a target", debug);
                                    //debugHelper.doDebug("GuardWorkScript: TARRGETTONULL -> NO GUN", debug);
                                    SetTargetToNull();
                                    nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay);
                                }
                                else
                                {
                                    if (Target == null && hasGun && !world.IsRemote())
                                    {
                                        FindTarget(blockValue);                                        
                                    }
                                    if (Target != null && TargetTransform != null)
                                    {
                                        //if (world.IsRemote())
                                        //    debugHelper.doDebug(
                                        //        "GuardWorkScript: Target IS NOT NULL -> " + Target.entityId, debug);
                                        if (!Target.IsAlive())
                                        {
                                            debugHelper.doDebug("GuardWorkScript: TARRGETTONULL -> Target is dead (1)", debug);
                                            SetTargetToNull();
                                            nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay);
                                        }
                                        else
                                        {                                            
                                            if (!world.IsRemote())
                                            {
                                                if (!svHelper.IsCrafting(blockValue.meta2))
                                                {
                                                    // changes to fire position, so that it can rotate correctly
                                                    // but does not fire yet.
                                                    blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 2));
                                                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                                                    debugHelper.doDebug(
                                                    "GuardWorkScript: Guard going to ready position", debug);
                                                }
                                            }
                                            if (svHelper.IsCrafting(blockValue.meta2))
                                            {                                                
                                                // rotate to the target
                                                Vector3 dir = TargetTransform.position - Muzzle.position;
                                                dir.y = 0;
                                                //dir.z = 0;
                                                Quaternion rot = Quaternion.LookRotation(dir);
                                                Rotation.rotation = Quaternion.Slerp(Rotation.rotation, rot,
                                                        speed * Time.deltaTime);
                                                rot = Rotation.rotation;
                                                if (Mathf.Abs(Quaternion.Angle(Rotation.rotation, rot)) < 10)
                                                {
                                                    if (world.IsRemote())
                                                    {
                                                        if (fireShot)
                                                        {
                                                            //debugHelper.doDebug(
                                                            //    "GuardWorkScript: (REMOTE) Guard WILL START SHOOTING",
                                                            //    debug);
                                                            svHelper.SetAnimationSpeed(gameObject, "working", 1);
                                                            Rotation.rotation = rot;
                                                        }
                                                        else
                                                        {
                                                            //debugHelper.doDebug(
                                                            //            "GuardWorkScript: (REMOTE) Guard WILLP AUSE SHOOTING",
                                                            //            debug);
                                                            svHelper.SetAnimationSpeed(gameObject, "working", 0);
                                                            Rotation.rotation = rot;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!fireShot)
                                                        {
                                                            // check if it's in line of sight before starting to shoot...
                                                            if (checkLineOfSight())
                                                            {
                                                                outOfSightCount = DateTime.Now.AddSeconds(20);
                                                                // check if line of sight
                                                                if (Target.IsAlive())
                                                                {
                                                                    //debugHelper.doDebug(
                                                                    //    "GuardWorkScript: (SERVER) Guard WILL START SHOOTING",
                                                                    //    debug);
                                                                    setFireShot(true);
                                                                    // unpause the animation
                                                                    svHelper.SetAnimationSpeed(gameObject, "working", 1);
                                                                    Rotation.rotation = rot;
                                                                }                                                                
                                                            }
                                                            else
                                                            {                                                                                                                            
                                                                //debugHelper.doDebug(
                                                                //       "GuardWorkScript: (SERVER) OUT OF SIGHT, Guard WILL PAUSE SHOOTING",
                                                                //       debug);
                                                                svHelper.SetAnimationSpeed(gameObject, "working", 0);
                                                                Rotation.rotation = rot;
                                                                nextShootCheck = DateTime.Now.AddMilliseconds(2000);
                                                                if (outOfSightCount > DateTime.Now)
                                                                {
                                                                    debugHelper.doDebug(
                                                                       "GuardWorkScript: (SERVER) Target has been out of sight for more then 20s, abort target",
                                                                       debug);
                                                                    SetTargetToNull();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (!fireShot)
                                                {
                                                    //debugHelper.doDebug(
                                                    //"GuardWorkScript: Guard target not in line of fire", debug);
                                                    //Rotation.rotation = Quaternion.Slerp(Rotation.rotation, rot,
                                                    //    speed * Time.deltaTime);
                                                    // pause the animation   
                                                    svHelper.SetAnimationSpeed(gameObject, "working", 0);
                                                    Rotation.rotation = rot;
                                                }                                                
                                            }
                                        }
                                    }
                                    else
                                    {
                                        debugHelper.doDebug(
                                                            "GuardWorkScript: TARRGETTONULL -> Target is NULL", debug);
                                        SetTargetToNull();                                        
                                        nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay); // only has a small delay before looking another time
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        debugHelper.doDebug("GuardWorkScript: IMPOSSIBLE TO FIND MUZZLE", debug);
                        Muzzle = svHelper.FindBody(gameObject.transform, "Muzzle");
                        nextShootCheck = DateTime.Now.AddMilliseconds(ShootDelay);
                    }                    
                }
                else debugHelper.doDebug("GuardWorkScript: Guard has no inventory", true);
                #endregion;                
                //debugHelper.doDebug("GuardWorkScript: NEXTSHOOTCHECK = " + nextShootCheck.ToString("yyyy-MM-dd HH:mm:ss"), debug);
            }
            // random sound
            if (!GameManager.IsDedicatedServer)
            {
                if (!svHelper.IsSleeping(blockValue.meta2) && DateTime.Now > nextRandomSound &&
                    !svHelper.IsCrafting(blockValue.meta2))
                {
                    // next random sound will be in random time too
                    System.Random _rndSound = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                    // there are 4 different sound: 1 - burp, 2 - cough, 3 - hickup, 4 - fart
                    int soundT = _rndSound.Next(0, 100);
                    string sound = "";
                    if (soundT < 10)
                    {
                        sound = "burp_sound";
                    }
                    else if (soundT < 30)
                    {
                        sound = "fart_sound";
                    }
                    else if (soundT < 60)
                    {
                        sound = "hickup_sound";
                    }
                    else if (soundT < 100)
                    {
                        sound = "cough_male_sound";
                        if (requirement != "Male") sound = "cough_female_sound";
                    }
                    //debugHelper.doDebug(string.Format("FarmerWorkScript: PLAY SOUND {0}", sound), debug);
                    //AudioManager.AudioManager.Play(blockPos.ToVector3(), sound, 0, false, -1, -1, 0F);
                    Audio.Manager.BroadcastPlay(blockPos.ToVector3(), sound);
                    nextRandomSound = DateTime.Now.AddSeconds(_rndSound.Next(30, 120));
                }
            }                       
        }
    }

    private bool FindAwakeFood()
    {
        bool foundAwakeFood = false;
        if (awakeTime > 0 && foodAwake != "")
        {
            int hpSub = 0;
            string[] foodItems = null;
            foodItems = foodAwake.Split(',');
            int currentDmg = blockValue.damage;
            int maxDmg = Block.list[blockValue.type].MaxDamage;
            if (foodItems.Length > 1 && foodContainer != "")
            {
                if (int.TryParse(foodItems[0], out hpSub))
                {
                    // look for containers in area
                    foodContainers = svHelper.GetContainers(foodContainer, world, blockPos, cIdx, checkArea);
                    if (foodContainers.Count > 0)
                    {
                        if (svHelper.EatFood(foodItems, foodContainers, world, cIdx))
                        {
                            // if exists, consumes it and recover ammount of hp
                            debugHelper.doDebug(
                                string.Format("GuardWorkScript: EAT AWAKE FOOD AND LOOSES {0}", hpSub),
                                debug);
                            svHelper.DmgBlock(maxDmg, blockValue, hpSub, world, blockPos, cIdx);
                            foundAwakeFood = true;
                            nextSleepCheck = DateTime.Now.AddSeconds(awakeTime);
                        }
                    }
                    if (!foundAwakeFood)
                    {
                        debugHelper.doDebug(string.Format("GuardWorkScript: NO AWAKE FOOD AVAILABLE"), debug);
                    }
                }
                else
                    debugHelper.doDebug(string.Format("GuardWorkScript: No awake hp decrease configured"),
                        true);
            }
        }
        return foundAwakeFood;
    }

    private float GetDamageBlock(ItemStack ammoItem, ItemStack gunItem, int skillLevel)
    {
        ItemClass ammoClass = ItemClass.GetForId(ammoItem.itemValue.type);
        ItemClass gunClass = ItemClass.GetForId(gunItem.itemValue.type);
        if (!ammoClass.HasAttributes)
            return AttributeBase.GetVal<AttributeBlockDamage>(gunItem.itemValue, 1);
        ItemValue _itemValue = ammoItem.itemValue;
        if (!gunClass.HasQuality && !gunClass.HasParts)
            return AttributeBase.GetVal<AttributeBlockDamage>(gunItem.itemValue, 1);
        _itemValue.Quality = gunItem.itemValue.Quality;
        float num = AttributeBase.GetVal<AttributeBlockDamage>(gunItem.itemValue, 1) + AttributeBase.GetVal<AttributeEntityDamage>(_itemValue, 0);
        if ((double)num < 0.0)
            num = 0.0f;
        // skill modifier by the guard
        if (skillLevel > 0)
        {
            num = num*((skillLevel/100) + 1);
        }
        return num;
    }

    private float GetDamageEntity(ItemStack ammoItem, ItemStack gunItem, int skillLevel)
    {
        ItemClass ammoClass = ItemClass.GetForId(ammoItem.itemValue.type);
        ItemClass gunClass = ItemClass.GetForId(gunItem.itemValue.type);
        ItemValue _itemValue = ammoItem.itemValue;
        //debugHelper.doDebug("GuardWorkScript: ENTITYDMG 1", debug);
        //if (!ammoClass.HasAttributes)
        //    return AttributeBase.GetVal<AttributeEntityDamage>(gunItem.itemValue, 1);
        //debugHelper.doDebug("GuardWorkScript: ENTITYDMG 2", debug);
        //if (!gunClass.HasQuality && !gunClass.HasParts)
        //    return AttributeBase.GetVal<AttributeEntityDamage>(gunItem.itemValue, 1);
        //debugHelper.doDebug("GuardWorkScript: ENTITYDMG 3", debug);
        //_itemValue.Quality = gunItem.itemValue.Quality;
        float num = AttributeBase.GetVal<AttributeEntityDamage>(gunItem.itemValue, 1) + AttributeBase.GetVal<AttributeEntityDamage>(_itemValue, 0);
        debugHelper.doDebug("GuardWorkScript: ENTITYDMG BASE DAMAGE = " + num.ToString("#0.##") + " and SKILL=" + skillLevel, debug);
        if ((double)num < 0.0)
            num = 0.0f;
        // skill modifier by the guard
        if (skillLevel > 0)
        {
            float modifier = ((float) skillLevel/100.0F) + 1.0F;
            num = num * modifier;
            debugHelper.doDebug("GuardWorkScript: ENTITYDMG MODIFIED DAMAGE = " + num.ToString("#0.##") + " modifier=" + modifier.ToString("#0.##"), debug);
        }
        debugHelper.doDebug("GuardWorkScript: ENTITYDMG MODIFIED DAMAGE = " + num.ToString("#0.##"), debug);
        return num;
    }

    protected virtual Vector3 doShoot(ItemStack ammoItem, ItemStack gunItem, int skillLevel)
    {
        if (!GameManager.IsDedicatedServer && burstCount == 0)
            //AudioManager.AudioManager.Play(gameObject.transform.position, attack.SoundStart.Value, 0, false, -1, -1, 0);
            Audio.Manager.BroadcastPlay(gameObject.transform.position, attack.SoundStart.Value);
        if (!world.IsRemote())
        {

            //Vector3 direction = this.transform.forward;
            //Vector3 direction = this.Muzzle.forward;
            Vector3 direction = TargetTransform.position - Muzzle.position; // i'll add here random inacuracy based on skill            

            Vector3 vector3 = Muzzle.position;

            Vector3 a = (1f - attack.RaysSpread)*direction.normalized;
            Vector3 b = vector3 + a;
            Vector3 c = b - vector3;
            Vector3 spread = c;

            direction = spread;

            Ray ray = new Ray(vector3, direction);

            WorldRayHitInfo hitInfo = null;
            int _hitMask = 8;
            if (Voxel.Raycast(GameManager.Instance.World, ray, attack.Range, -1486853, _hitMask, 0.0f))
            {
                //debugHelper.doDebug("GuardWorkScript: RANGE = " + attack.Range, debug);
                hitInfo = Voxel.voxelRayHitInfo.Clone();
                if ((double) hitInfo.hit.distanceSq < (double) attack.Range*(double) attack.Range)
                    Hit(GameManager.Instance.World, hitInfo, -1, EnumDamageSourceType.Bullet,
                        GetDamageBlock(ammoItem, gunItem, skillLevel), GetDamageEntity(ammoItem, gunItem, skillLevel),
                        1f, 100, 0, "metal"); //, null,null, null);
            }           
            if (Tracer != null)
            {
                if (--TracerCount <= 0 && !Tracer.enabled)
                {
                    Tracer.SetPosition(0, ray.origin);

                    float dist = 5f;
                    if (hitInfo == null)
                    {
                    }
                    else if (hitInfo.transform == null)
                    {
                    }
                    else
                    {
                        dist = Vector3.Distance(Rotation.position, hitInfo.transform.position);

                    }

                    System.Random Rand = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                    Tracer.SetPosition(1, ray.origin + (ray.direction.normalized*dist));
                    Tracer.enabled = true;
                    TracerEnd = DateTime.Now.AddMilliseconds(Rand.Next(25, 50));
                    TracerCount = 2;
                }
            }
            return direction;
        }
        else return Vector3.zero;
    }

    private bool checkLineOfSight()
    {
        bool isInSight = false;
        if (Muzzle != null && TargetTransform != null)
        {
            Vector3 direction = TargetTransform.position - Muzzle.position;
                // i'll add here random inacuracy based on skill            

            Vector3 vector3 = Muzzle.position;

            Vector3 a = (1f - attack.RaysSpread)*direction.normalized;
            Vector3 b = vector3 + a;
            Vector3 c = b - vector3;
            Vector3 spread = c;

            direction = spread;

            Ray ray = new Ray(vector3, direction);
            if (Muzzle != null && TargetTransform != null)
            {
                //RaycastHit hit;
                //if (Physics.Raycast(ray, out hit, attack.Range))
                WorldRayHitInfo hit = null;
                int _hitMask = 8;
                if (Voxel.Raycast(GameManager.Instance.World, ray, attack.Range, false, false))
                {
                    hit = Voxel.voxelRayHitInfo.Clone();
                    if (hit.transform != TargetTransform)
                    {
                        if (hit.tag.StartsWith("T_Mesh"))
                        {

                            ChunkCluster chunkCluster = world.ChunkClusters[hit.hit.clrIdx];
                            if (chunkCluster == null)
                                isInSight = true;
                            else
                            {
                                Vector3i vector3i = hit.hit.blockPos;
                                BlockValue block1 = chunkCluster.GetBlock(vector3i);
                                Block block2 = Block.list[block1.type];
                                if (block1.Equals((object)BlockValue.Air))
                                    isInSight = true;
                                else
                                {
                                    //Block block3 = Block.list[hit.fmcHit.blockValue.type];
                                    if (!block2.blockMaterial.IsLiquid && !block2.blockMaterial.IsGroundCover && !block2.blockMaterial.IsPlant)
                                    {                                        
                                        debugHelper.doDebug(string.Format("Line of Sight blocked by {0}", block2.GetLocalizedBlockName()), debug);
                                        isInSight = false;

                                    }
                                    else isInSight = true;
                                }                                
                            }                            
                        }
                        else isInSight = true;
                        //if it is another entity it WILL shoot! Even if it's the owner...
                        //friendly fire is on, you need to get out of the way
                        //if there is any block different from air there
                        //if its a block, it will NOT shoot

                    }
                    else isInSight = true;
                }
            }
        }        
        return isInSight;
    }

    public void Hit(World _world, WorldRayHitInfo hitInfo, int _attackerEntityId, EnumDamageSourceType _dst, float _blockDamage, float _entityDamage, float _staminaDamageMultiplier, float _weaponCondition, float _criticalHitChance, string _attackingDeviceMadeOf)//, DamageMultiplier _damageMultiplier, List<MultiBuffClassAction> _buffActions, ItemActionAttack.BlockAttackInfo _blockAttackInfo)
    {
        string str1 = null;
        string str2 = null;
        float _lightValue = 1f;
        Color _color = Color.white;
        _staminaDamageMultiplier = ItemActionAttack.StaminaModifier(_staminaDamageMultiplier);
        if (hitInfo.tag.StartsWith("T_Mesh"))
        {

            ChunkCluster chunkCluster = _world.ChunkClusters[hitInfo.hit.clrIdx];
            if (chunkCluster == null)
                return;
            Vector3i vector3i = hitInfo.hit.blockPos;
            BlockValue block1 = chunkCluster.GetBlock(vector3i);
            Block block2 = Block.list[block1.type];
            if (block1.Equals((object)BlockValue.Air))
                return;

            Block block3 = Block.list[hitInfo.fmcHit.blockValue.type];
            _lightValue = _world.GetLightBrightness(hitInfo.fmcHit.blockPos);
            _color = block3.GetColorForSide(hitInfo.fmcHit.blockValue, hitInfo.fmcHit.blockFace);
            str1 = block3.GetParticleForSide(hitInfo.fmcHit.blockValue, hitInfo.fmcHit.blockFace);
            str2 = block3.GetMaterialForSide(hitInfo.fmcHit.blockValue, hitInfo.fmcHit.blockFace).SurfaceCategory;
            float num3 = _blockDamage;
            if (!block2.blockMaterial.IsLiquid)
            {
                int num1 = (int)block1.damage;
                int num2 = block2.DamageBlock((WorldBase)_world, chunkCluster.ClusterIdx, vector3i, block1, (int)_blockDamage, _attackerEntityId, false);

            }
        }
        else
        {
            Transform transform = hitInfo.transform;
            string _group = "";
            if (hitInfo.tag.StartsWith("tree"))
            {
                transform = GameUtils.GetHitRootTransform(Voxel.voxelRayHitInfo.tag, hitInfo.transform);
            }
            else
            {
                Entity component = hitInfo.transform.GetComponent<Entity>();

                if (component == null)
                {

                    component = GetEntityFromHit(hitInfo);

                    if (component == null)
                    {
                        return;
                    }

                }

                if (!component.IsAlive())
                    return;


                DamageSourceEntity damageSourceEntity = new DamageSourceEntity(_dst, _attackerEntityId, hitInfo.ray.direction, hitInfo.transform.name, hitInfo.hit.pos, Voxel.phyxRaycastHit.textureCoord);
                int _strength = (int)_entityDamage;
                bool flag = (double)_criticalHitChance > 0.0 && (double)UnityEngine.Random.value <= (double)_criticalHitChance;
                component.DamageEntity((DamageSource)damageSourceEntity, _strength, flag, 1f);
                str2 = EntityClass.list[component.entityClass].Properties.Values["SurfaceCategory"];
                str1 = str2;
                _lightValue = component.GetLightBrightness();
            }
        }


        if (str1 == null)
            return;
        _world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect("impact_" + _attackingDeviceMadeOf + "_on_" + str1, hitInfo.fmcHit.pos, Utils.BlockFaceToRotation(hitInfo.fmcHit.blockFace), _lightValue, _color, str2 == null ? (string)null : _attackingDeviceMadeOf + "hit" + str2, hitInfo.transform), _attackerEntityId);



    }

    public Entity GetEntityFromHit(WorldRayHitInfo hitInfo)
    {
        Transform hitRootTransform = GameUtils.GetHitRootTransform(hitInfo.tag, hitInfo.transform);
        if (hitRootTransform != null)
            return hitRootTransform.GetComponent<Entity>();
        return (Entity)null;

    }

    #region CODE TO REMOVE AFTER I'M SURE IT'S NOT NEEDED
    //private EntityPlayer FindOwner()
    //{
    //    // will not check for owner online, but instead will just get owner ID
    //    // it wont matter if he is online or not.

    //    Vector3i v = blockPos;
        

    //    List<EntityPlayer> players = GameManager.Instance.World.Players.list;


    //    int id = -1;
    //    string steamID = "";
    //    if (inv != null)
    //    {

    //        //            string steamID = GamePrefs.GetString(EnumGamePrefs.PlayerId);
    //        steamID = inv.GetOwner();


    //        if (ConnectionManager.Instance == null)
    //        {

    //        }
    //        else
    //        {

    //            if (GameManager.IsDedicatedServer)
    //            {

    //                if (ConnectionManager.Instance != null)
    //                {

    //                    List<ClientInfo> clients = ConnectionManager.Instance.GetClients();

    //                    if (clients != null)
    //                    {
    //                        foreach (ClientInfo c in clients)
    //                        {

    //                            if (c == null)
    //                            {
    //                                continue;
    //                            }

    //                            if (c.playerId == steamID)
    //                            {
    //                                id = c.entityId;
    //                                break;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    EntityPlayer player = null;
    //    foreach (EntityPlayer p in players)
    //    {
    //        if (p == null)
    //        {
    //            continue;
    //        }

    //        if (p.entityId == id)
    //        {
    //            player = p;
    //            break;
    //        }
    //    }


    //    if (player == null)
    //    {


    //        string localID = GamePrefs.GetString(EnumGamePrefs.PlayerId);

    //        if (steamID == localID)
    //        {
    //            id = GameManager.Instance.World.GetLocalPlayerId();

    //            foreach (EntityPlayer p in players)
    //            {
    //                if (p == null)
    //                {
    //                    continue;
    //                }

    //                if (p.entityId == id)
    //                {
    //                    player = p;
    //                    break;
    //                }
    //            }
    //        }

    //    }

    //    if (player != null)
    //    {
    //        OwnerEntity = player;
    //        OwnerName = player.EntityName;
    //    }
    //    return player;



    //}
    #endregion;
    private void FindTarget(BlockValue blockvalue)
    {
        if (Muzzle == null) return;
        if (DateTime.Now <= findTargetDelay) return; // dont search in less then 1s interval
        //debugHelper.doDebug("GuardWorkScript: LOOKING FOR TARGET WITH RANGE=" + attack.Range, debug);
        findTargetDelay = DateTime.Now.AddSeconds(1);
        System.Random Rand = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));

        //if (OwnerEntity == null)
        //{
        //    FindOwner();
        //}

        string OwernerPlayerID = "";

        if (inv != null)
        {
            OwernerPlayerID= inv.GetOwner();
        }

        List<Entity> players = new List<Entity>();
        List<Entity> primary = new List<Entity>();
        List<Entity> secondary = new List<Entity>();


        foreach (EntityPlayer p in GameManager.Instance.World.Players.list)
        {
            PersistentPlayerData dataFromEntityId1 = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(p.entityId);

            if (OwernerPlayerID!="" && dataFromEntityId1.PlayerId == OwernerPlayerID) continue; // is owner

            if (OwernerPlayerID != "" && dataFromEntityId1.ACL.Contains(OwernerPlayerID)) continue; // is friendly

            if (Vector3.Distance(p.transform.position, gameObject.transform.position) < attack.Range)
            {             
                //if (OwnerEntity == null || !(p == OwnerEntity || p.IsFriendsWith(OwnerEntity)))
                {
                    debugHelper.doDebug("GuardWorkScript: Found Player Target", debug);
                    players.Add(p);
                }
            }
        }

        foreach (Entity p in GameManager.Instance.World.Entities.list)
        {

            //if (OwnerEntity != null)
            //{
            //    if (p.entityId == OwnerEntity.entityId)
            //        continue;
            //}

            if ((p is EntityEnemy) && !(p is EntityDogMorte))
            {
                if (p.IsAlive() && Vector3.Distance(p.transform.position, gameObject.transform.position) < attack.Range)
                {
                    primary.Add(p);
                    debugHelper.doDebug("GuardWorkScript: Found Enemy Target", debug);
                }
            }
            // DO NOT TARGET animals - leave that for future hunters?
            //else if (p is EntityAnimal)
            //{
            //    if (p.IsAlive() && Vector3.Distance(p.transform.position, gameObject.transform.position) < attack.Range)
            //    {
            //        secondary.Add(p);
            //        debugHelper.doDebug("GuardWorkScript: Found Animal Target", debug);
            //    }
            //}

        }

        if (players.Count == 0 && primary.Count == 0 && secondary.Count == 0)
            return;

        if (players.Count > 0)
        {
            Target = players[Rand.Next(0, players.Count)];
        }
        else if (primary.Count > 0)
        {
            Target = primary[Rand.Next(0, primary.Count)];
        }
        else if (secondary.Count > 0)
        {
            Target = secondary[Rand.Next(0, secondary.Count)];
        }


        Transform head = svHelper.FindBody(Target.transform, "Head");
        //aiming for head chance = skill level * 2
        //if (Rand.Next(1, 100) > (blockvalue.meta*2))
        //    head = null;

        if (head == null)
            TargetTransform = Target.transform;
        else
        {
            debugHelper.doDebug("GuardWorkScript: Aim for HEAD", debug);
            TargetTransform = head;
        }
        if (!world.IsRemote())
        {
            if (Target != null && inv != null)
            {
                // add target to the inventory so that client can know it
                //clubMaster
                // adds a club master to the last position of the inventory
                ItemStack targetItem = new ItemStack(new ItemValue(ItemClass.GetItem("clubMaster").type, false), 1);
                targetItem.itemValue.Meta = Target.entityId;
                debugHelper.doDebug(string.Format("GuardWorkScript: SET ENTITY ID = {0}", Target.entityId), debug);
                inv.UpdateSlot(inv.items.Length - 1, targetItem);
                inv.SetModified();
            }
            else
            {
                SetTargetToNull();
            }
        }
    }

    private void setFireShot(bool state)
    {
        if (!world.IsRemote())
        {
            if (fireShot != state)
            {
                fireShot = state;
                if (!svHelper.FireShot(blockValue.meta2) && fireShot)
                {
                    blockValue.meta2 = (byte) (blockValue.meta2 | (1 << 3));
                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                }
                if (svHelper.FireShot(blockValue.meta2) && !fireShot)
                {
                    blockValue.meta2 = (byte) (blockValue.meta2 & ~(1 << 3));
                    world.SetBlockRPC(cIdx, blockPos, blockValue);
                }
            }
        }
    }

    private void SetTargetToNull()
    {
        if (!world.IsRemote())
        {
            if (inv != null)
            {
                if (inv.items[inv.items.Length - 1] != null)
                {
                    if (!inv.items[inv.items.Length - 1].IsEmpty())
                    {
                        inv.items[inv.items.Length - 1].Clear();
                        inv.SetModified();
                        debugHelper.doDebug("GuardWorkScript: TARGET WAS SET TO NULL", debug);
                    }
                }
            }
            if (svHelper.IsCrafting(blockValue.meta2))
            {
                // changes to fire position, so that it can rotate correctly
                // but does not fire yet.
                blockValue.meta2 = (byte) (blockValue.meta2 & ~(1 << 2));
                world.SetBlockRPC(cIdx, blockPos, blockValue);
            }
            Target = null;
            TargetTransform = null;
        }
    }
}