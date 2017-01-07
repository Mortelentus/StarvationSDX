using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

// By default, a survivor will run away from zombies, unless its of a specific type
// Hunters, will track and attack animals -> "entityanimal"
// "Guards", will track and attack zombies -> "entityzombie" -> maybe custom to attack enemy players
// You can order a zombie to attack zombies "targeted" by the "owner"
// If it is ordered to follow a person, it will resume following if it has no attack tasks to perform.


public class EntitySurvivorMod : EntityAnimalStag
//public class EntitySurvivorMod : EntityZombie
{
    private bool debug = false;

    private float meshScale = 1;
    private bool isSleeping = false;
    private bool isMale = false;
    DateTime dtaNextSleep = DateTime.MinValue;
    DateTime dtaAwake = DateTime.MinValue;
    private DateTime nextRandomSound = DateTime.MinValue;
    DateTime nextFoodCheck = DateTime.MinValue;
    List<Vector3i> foodContainers = new List<Vector3i>();    
    private int checkInterval = 30;
    private int checkArea = 10;
    private string ownerID = "";
    private string foodLow = "";
    private string foodMed = "";
    private string foodHigh = "";
    private string foodContainer = "";
    private string name = "";
    private int sleepFood = 0;
    private string EntityGroup = "";
    private int number = 0;
    private int area = 0;
    private SurvivorHelper svHelper = new SurvivorHelper();
    Vector3 FoodLocation = Vector3.zero;
    private bool dialogOpen = false;
    private survivorDialog script;
    private EAITaskList OA;
    private EAITaskList AttackTasks;
    private EAITaskList RunAwaysTasks;
    private static EAIFactory LM = new EAIFactory();
    private EntityAlive followTarget;
    private EntityAlive attackTarget;
    private List<clsDialog> dialogs = new List<clsDialog>();
    private List<int> blockedDialogs = new List<int>();
    private int indexInBlockActivationCommands = 0;
    private Vector3i tePo = Vector3i.zero;
    private DateTime dtaNextTarget = DateTime.MinValue;
    bool randomSounds = false;
    bool doSleep = false;
    public int followInterval = 2; // try to follow owner each 2s.
    DateTime NextFollowCheck = DateTime.MinValue;

    private EntityActivationCommand[] DDMOD = new EntityActivationCommand[1]
    {
        new EntityActivationCommand("<E> to talk", "", true)
    };

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
        if (entityClass.Properties.Values.ContainsKey("CheckInterval"))
        {
            if (int.TryParse(entityClass.Properties.Values["CheckInterval"], out checkInterval) == false) checkInterval = 30;
        }
        if (entityClass.Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(entityClass.Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        if (entityClass.Properties.Values.ContainsKey("FoodLow"))
            foodLow = entityClass.Properties.Values["FoodLow"];
        if (entityClass.Properties.Values.ContainsKey("FoodMedium"))
            foodMed = entityClass.Properties.Values["FoodMedium"];
        if (entityClass.Properties.Values.ContainsKey("FoodHigh"))
            foodHigh = entityClass.Properties.Values["FoodHigh"];
        if (entityClass.Properties.Values.ContainsKey("FoodContainer"))
            foodContainer = entityClass.Properties.Values["FoodContainer"];
        if (entityClass.Properties.Values.ContainsKey("IsMale"))
        {
            if (bool.TryParse(entityClass.Properties.Values["IsMale"], out isMale) == false) isMale = false;
        }
        if (entityClass.Properties.Values.ContainsKey("RandomSounds"))
        {
            if (bool.TryParse(entityClass.Properties.Values["RandomSounds"], out randomSounds) == false) randomSounds = false;
        }
        if (entityClass.Properties.Values.ContainsKey("DoSleep"))
        {
            if (bool.TryParse(entityClass.Properties.Values["DoSleep"], out doSleep) == false) doSleep = false;
        }
        // quest spawer
        if (entityClass.Properties.Values.ContainsKey("Spawn"))
        {
            EntityGroup = entityClass.Properties.Values["Spawn"];
            number = Convert.ToInt32(entityClass.Properties.Params1["Spawn"]);
            area = Convert.ToInt32(entityClass.Properties.Params2["Spawn"]);
        }
        // dialog options
        dialogs = GetDialogs();        
        lifetime = float.MaxValue;
    }

    protected override void Awake()
    {
        dtaNextSleep = DateTime.Now.AddSeconds(35);
        BoxCollider component = this.gameObject.GetComponent<BoxCollider>();
        if ((bool)((Object)component))
        {
            component.center = new Vector3(0.0f, 0.85f, 0.0f);
            component.size = new Vector3(20.0f, 15.6f, 20.0f);
        }
        base.Awake();
        AddRunAway("EntityZombie");        
    }

    public override void CopyPropertiesFromEntityClass()
    {
        base.CopyPropertiesFromEntityClass();
        AddRunAway("EntityZombie");
    }

    public string GetOwnerId()
    {
        return ownerID;
    }

    public bool IsSleeping()
    {
        return isSleeping;
    }

    public override void Read(byte _version, BinaryReader _br)
    {
        base.Read(_version, _br);
        if (_br.BaseStream.Position == _br.BaseStream.Length)
            return; //probably a vanilla entity so just return.
        try
        {
            bool reserve = _br.ReadBoolean();
            isSleeping = _br.ReadBoolean();
            ownerID = _br.ReadString();
            name = _br.ReadString();
            if (name == "") name = NameGenerator.CreateNewName(isMale); // mudar isto para a instanciação??
            if (name != "" && this.DDMOD[0].text == "<E> to talk") this.DDMOD[0].text = " < E> to talk to " + name;
        }
        catch (Exception ex)
        {
            //Debug.Log("SURVIVOR -> ERRO READING: " + ex.Message);
        }
    }

    public override void Write(BinaryWriter _bw)
    {
        base.Write(_bw);
        try
        {
            _bw.Write(isMale);
            _bw.Write(isSleeping);
            _bw.Write(ownerID);
            if (name == "") name = NameGenerator.CreateNewName(isMale);
            if (name != "" && this.DDMOD[0].text == "<E> to talk") this.DDMOD[0].text = "<E> to talk to " + name;
            _bw.Write(name);
        }
        catch (Exception ex)
        {
            //Debug.Log((string.Format("SURVIVOR -> ERRO WRITING: {0}", ex.Message)));
        }
    }

    protected override void updateTasks()
    {     
        // if inventory opened, doesn't do anything
        try
        {
            if (this.IsAlive() && !IsDead() && !this.world.IsRemote())
            {
                if(this.lootContainer != null)
                    if (auxHelper.IsInvOpened(this.lootContainer)) return; // someone is accessing the lootcontainer
                if (isSleeping) return; // sleeping
                if (dtaAwake > DateTime.Now) return; // waking up
                if (dialogOpen) return;
                if (this.RunAwaysTasks != null) // run custom runaway tasks
                {
                    if (this.RunAwaysTasks.Tasks.Count > 0)
                    {
                        using (ProfilerUsingFactory.Profile("entities.live.ai.tasks"))
                        {
                            this.RunAwaysTasks.OnUpdateTasks();
                        }
                    }
                }
                if (this.AttackTasks != null) // run custom attack tasks
                {
                    if (this.AttackTasks.Tasks.Count > 0)
                    {
                        if (attackTarget != null)
                        {
                            if (attackTarget.IsDead()) this.SetAttackTarget((EntityAlive)null, 0);
                            else if (GetDistance(attackTarget) > GetSeeDistance()) this.SetAttackTarget((EntityAlive)null, 0);
                        }
                        if (attackTarget == null && DateTime.Now >= dtaNextTarget)
                        {
                            FindValidTarget();
                            dtaNextTarget = DateTime.Now.AddSeconds(5);
                        }
                        if (this.GetAttackTarget() == null && attackTarget != null)
                        {
                            if (this.GetAttackTarget() != attackTarget)
                                this.SetAttackTarget(attackTarget, 1000);
                        }
                        using (ProfilerUsingFactory.Profile("entities.live.ai.tasks"))
                        {
                            this.AttackTasks.OnUpdateTasks();
                        }
                    }
                }
                if (followTarget != (EntityAlive)null && NextFollowCheck < DateTime.Now)
                {
                    if (followTarget.IsAlive())
                    {
                        if (this.GetDistanceSq(followTarget) <= 5.0F) this.getNavigator().clearPathEntity(); // stops if already close, otherwise it will just look odd
                        else
                        {
                            Legacy.PathFinderThread.Instance.FindPath(this, followTarget.GetPosition(),
                                               this.GetApproachSpeed(),
                                               (EAIBase)null);
                        }                        
                        NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                    }
                    else
                    {
                        ClearFollow();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("EXCEPTION1: " + ex.Message);
        }           
        base.updateTasks();
    }

    void FindValidTarget()
    {
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
                EntityAlive _other = enumerator.Current as EntityAlive;
                if (_other.IsAlive())
                {
                    attackTarget = _other;                                        
                    return;
                }
            }
        }
        //using (
        //                List<EntityGroup>.Enumerator enumerator =
        //                    this.world.GetEntitiesInBounds(typeof(EntityPlayer),
        //                        BoundsUtils.BoundsForMinMax(this.position.x - GetSeeDistance(), this.position.y - GetSeeDistance(),
        //                            this.position.z - GetSeeDistance(), this.position.x + GetSeeDistance(),
        //                            this.position.y + GetSeeDistance(),
        //                            this.position.z + GetSeeDistance())).GetEnumerator())
        //{
        //    while (enumerator.MoveNext())
        //    {
        //        EntityPlayer _other = enumerator.Current as EntityPlayer;
        //        PersistentPlayerData dataFromEntityId1 = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(_other.entityId);
        //        bool isFriendly = false;
        //        if (dataFromEntityId1 != null && ownerID != null)
        //        {
        //            if (ownerID != "" && dataFromEntityId1.PlayerId == ownerID) isFriendly = true; // is owner
        //            else if (ownerID != "" && dataFromEntityId1.ACL.Contains(ownerID)) isFriendly = true; // is friendly
        //        }
        //        if (_other.IsAlive() && !isFriendly)
        //        {
        //            attackTarget = (_other as EntityAlive);
        //            return;
        //        }
        //    }
        //}
    }

    public override void OnUpdateLive()
    {
        try
        {
            if (this.IsAlive() && !IsDead())
            {
                if (this.lootContainer != null)
                    if (auxHelper.IsInvOpened(this.lootContainer)) return; // someone is accessing the lootcontainer
                if (dialogOpen) return;
                if (dtaAwake > DateTime.Now) return; // waking up
                if (DateTime.Now > dtaNextSleep && doSleep)
                {
                    #region Sleep Check;
                    dtaNextSleep = DateTime.Now.AddSeconds(rand.Next(7, 30)); // 7 to 30s
                    if (true) // if I want to disable sleeping
                    {
                        if (!this.world.IsRemote())
                        {
                            int sleepProb = 0;
                            int wakeProb = 0;
                            if (!GameManager.Instance.World.IsDaytime())
                            {
                                //if (!isSleeping)
                                //    sleepProb = 80; // 80% chance of sleeping during the night
                                //else
                                //    wakeProb = 5; // 5% chance of waking up during the night  
                                sleepProb = 100;
                            }
                            else if (GameManager.Instance.World.IsDaytime() && isSleeping)
                            {
                                //if (!isSleeping)
                                //    sleepProb = 5; // 5% chance of sleeping during the day
                                //else wakeProb = 80; // 80% chance of waking up during the day
                                wakeProb = 100;
                            }
                            int rndCheck = rand.Next(1, 101);
                            if (rndCheck <= sleepProb && sleepProb > 0)
                            {
                                // fall asleep if not sleeping -> need to pass this to clients!!!
                                debugHelper.doDebug("SURVIVOR starts sleeping", debug);
                                isSleeping = true;
                                //this.Stats.Debuff("awake");
                                //MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction("sleeping");
                                //multiBuffClassAction.Execute(this.entityId, (EntityAlive)this,
                                //                        false,
                                //                        EnumBodyPartHit.None, (string)null);
                                //this.Stats.SendAll();
                                dtaNextSleep = DateTime.Now.AddSeconds(30);
                            }
                            else if (rndCheck <= wakeProb && wakeProb > 0)
                            {
                                // wakeup if sleeping  -> need to pass this to clients!!!
                                debugHelper.doDebug("SURVIVOR WAKES UP", debug);
                                isSleeping = false;
                                //this.Stats.Debuff("sleeping");
                                //MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction("awake");
                                //multiBuffClassAction.Execute(this.entityId, (EntityAlive) this,
                                //    false,
                                //    EnumBodyPartHit.None, (string) null);
                                //this.Stats.SendAll();
                                dtaAwake = DateTime.Now.AddSeconds(5);
                                dtaNextSleep = DateTime.Now.AddSeconds(30);
                            }
                            if (isSleeping)
                            {
                                // see of client gets this
                                string sound = "sleep_male_sound";
                                if (!isMale) sound = "sleep_female_sound";
                                debugHelper.doDebug("SURVIVOR PLAYING " + sound, debug);
                                this.PlayOneShot(sound);
                            }
                        }
                    }
                    #endregion;
                }
                if (doSleep)
                {
                    if (GameManager.Instance.World.IsDaytime() && isSleeping) isSleeping = false;
                    else if (!GameManager.Instance.World.IsDaytime() && !isSleeping) isSleeping = true;
                    //if (this.Stats.FindBuff("sleeping"))
                    //{
                    //    // if it moves, wakes up!.
                    //    if (!isSleeping)
                    //    {
                    //        if (this.Stats.FindBuff("awake")) this.Stats.Debuff("awake");
                    //        debugHelper.doDebug("SURVIVOR IS SLEEPING", debug);
                    //        isSleeping = true;
                    //    }                        
                    //}
                    //if (this.Stats.FindBuff("awake"))
                    //{
                    //    //debugHelper.doDebug("HAS BUFF AWAKE", debug);
                    //    if (isSleeping)
                    //    {
                    //        if (this.Stats.FindBuff("sleeping")) this.Stats.Debuff("sleeping");
                    //        debugHelper.doDebug("SURVIVOR WAKES UP", debug);
                    //        isSleeping = false;
                    //    }                        
                    //}
                }
                else isSleeping = false;
                if (!isSleeping && DateTime.Now > nextRandomSound && randomSounds && !this.world.IsRemote())
                {
                    #region Random Sound;

                    // there are 4 different sound: 1 - burp, 2 - cough, 3 - hickup, 4 - fart
                    int soundT = rand.Next(0, 100);
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
                        if (!isMale) sound = "cough_female_sound";
                    }
                    debugHelper.doDebug("SURVIVOR PLAYING " + sound, debug);
                    this.PlayOneShot(sound);
                    nextRandomSound = DateTime.Now.AddSeconds(rand.Next(30, 120));

                    #endregion;
                }
                if (DateTime.Now > nextFoodCheck && checkInterval > 0 && !this.world.IsRemote())
                {
                    #region Food Check;

                    nextFoodCheck = DateTime.Now.AddSeconds(checkInterval);
                    // only checks for food AFTER he is "owned" by a player...
                    if (ownerID != "")
                    {
                        if (isSleeping)
                        {
                            // if the survivor is sleeping it will reduce hp each 5 checks, but only by 1
                            sleepFood++;
                            if (sleepFood > 10)
                            {
                                debugHelper.doDebug(string.Format("SLEEPING FOR TOO LONG"), debug);
                                this.AddHealth(-1);
                            }
                            else debugHelper.doDebug(string.Format("SLEEPING SAINLY - NO HUNGER"), debug);
                        }
                        else
                        {
                            int currentDmg = this.Health;
                            int maxDmg = this.GetMaxHealth();
                            sleepFood = 0;
                            float hpPerc = 0;
                            hpPerc = (float) currentDmg/(float) maxDmg*100;
                            debugHelper.doDebug(
                                string.Format("HEALTH={0}, MAXHEALTH={1}, PERC={2}", currentDmg, maxDmg, hpPerc), debug);
                            int hpAdd = 0;
                            string[] foodItems = null;
                            // check what is the type of food to check
                            // For example: if a survivor is BADLY hurt he will ONLY accept high food tiers!
                            if (hpPerc >= 90)
                            {
                                debugHelper.doDebug(string.Format("LOW tier FOOD"), debug);
                                // more then 90%HP - low tier  
                                if (foodLow != "")
                                {
                                    foodItems = foodLow.Split(',');
                                }
                            }
                            else if (hpPerc >= 30)
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
                            // for this first version it will NOT move to food, just take the food and be done with it
                            if (foodItems.Length > 1 && foodContainer != "")
                            {
                                if (int.TryParse(foodItems[0], out hpAdd))
                                {
                                    bool couldEat = false;
                                    // search in its own inventory
                                    TileEntity tileEntity = world.GetTileEntity(this.entityId);
                                    if (svHelper.EatFoodInv(foodItems, tileEntity, this.world))
                                    {
                                        this.AddHealth(hpAdd);
                                        couldEat = true;
                                    }
                                    else
                                    {
                                        // look for containers in area
                                        debugHelper.doDebug(string.Format("FINDING CONTAINERS"), debug);
                                        FindFood();
                                        if (foodContainers.Count > 0)
                                        {
                                            debugHelper.doDebug(string.Format("EAT FOOD"), debug);
                                            // search of 1 valid food item
                                            if (svHelper.EatFoodEnt(foodItems, foodContainers, world))
                                            {
                                                // if exists, consumes it and recover ammount of hp
                                                // eventually make the entity move to the food container
                                                debugHelper.doDebug(
                                                    string.Format("EAT FOOD AND RECOVERS {0} - HP = {1}", hpAdd,
                                                        this.Health),
                                                    debug);
                                                this.AddHealth(hpAdd);
                                                couldEat = true;
                                            }
                                        }
                                    }
                                    // if doesn't exist looses 2hp -> this means that, as long as there is valid food, they will easly recover.                        
                                    if (!couldEat)
                                    {
                                        this.AddHealth(-2);
                                        debugHelper.doDebug(string.Format("NO FOOD AVAILABLE HP = " + this.Health),
                                            debug);
                                    }
                                }
                                else debugHelper.doDebug(string.Format("No hp increment configured"), debug);
                            }
                            else debugHelper.doDebug(string.Format("No food tier configured"), debug);
                        }
                    }

                    #endregion;
                }
                if (dtaAwake > DateTime.Now || isSleeping) return; // does nothing else.
            }            
        }
        catch (Exception ex)
        {
            Debug.Log("EXCEPTION: " + ex.ToString());
        }              
        base.OnUpdateLive();
    }
   
    private bool ProccessCommands(DamageResponse _dmResponse)
    {
        if (_dmResponse.Source.GetName() == EnumDamageSourceType.Disease)
        {
            if (_dmResponse.Strength >= 6000)
            {
                this.MarkToUnload();
                return true;
            }
            if (_dmResponse.Strength >= 2000 && _dmResponse.Strength < 9999)
            {
                // just reassigns buffs
                if (!this.world.IsRemote())
                {
                    if (isSleeping)
                    {
                        if (this.Stats.FindBuff("awake")) this.Stats.Debuff("awake");
                        debugHelper.doDebug("SURVIVOR add to reaply sleeping", debug);
                        MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction("sleeping");
                        multiBuffClassAction.Execute(this.entityId, (EntityAlive) this,
                            false,
                            EnumBodyPartHit.None, (string) null);
                    }
                    else
                    {
                        if (this.Stats.FindBuff("sleeping")) this.Stats.Debuff("sleeping");
                        debugHelper.doDebug("SURVIVOR add to reaply awake", debug);
                        MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction("awake");
                        multiBuffClassAction.Execute(this.entityId, (EntityAlive) this,
                            false,
                            EnumBodyPartHit.None, (string) null);
                    }
                    this.Stats.SendAll();
                }
                return true;
            }
            if (_dmResponse.Strength >= 1000 && _dmResponse.Strength < 2000)
            {
                // it's a command, i take out 1000 from it to obtain the command "bits"
                int commands = _dmResponse.Strength - 1000;
                bool follow = false;
                bool attack = false;
                bool retaliate = false;
                bool flee = false;
                bool spawn = false;
                //bit0 = dialogopen
                try
                { dialogOpen = ((int) commands & 1 << 0) != 0; }
                catch
                {
                    dialogOpen = false;
                }
                //bit1 = follow
                try { follow = ((int) commands & 1 << 1) != 0;}
                catch
                {
                    follow = false;
                }
                //bit2 = attack
                try { attack = ((int) commands & 1 << 2) != 0;}
                catch
                {
                    attack = false;
                }
                //bit3 = flee
                try { flee = ((int)commands & 1 << 3) != 0;}
                catch
                {
                    flee = false;
                }
                //bit4 = retaliate
                try { retaliate = ((int)commands & 1 << 4) != 0;}
                catch
                {
                    retaliate = false;
                }
                //bit5 = spawn
                try { spawn = ((int)commands & 1 << 5) != 0; }
                catch
                {
                    spawn = false;
                }
                if (!dialogOpen)
                {
                    Entity player = world.GetEntity(_dmResponse.Source.getEntityId());
                    if (player is EntityPlayer)
                    {
                        IssueOrders((player as EntityPlayer), follow, attack, flee, retaliate, spawn);
                    }
                }
                return true;
            }
        }
        return false;
    }

    public override void ProcessDamageResponse(DamageResponse _dmResponse)
    {
        if (ProccessCommands(_dmResponse)) return;
        base.ProcessDamageResponse(_dmResponse);
    }

    public override void ProcessDamageResponseLocal(DamageResponse _dmResponse)
    {
        if (ProccessCommands(_dmResponse)) return;
        base.ProcessDamageResponseLocal(_dmResponse);
    }

    private bool FindFood()
    {
        foodContainers.Clear();
        FoodLocation = this.GetPosition();
        Vector3i pos = new Vector3i(this.GetPosition());
        if (!FindFood(pos))
            if (!FindFood(pos + new Vector3i(-16, 0, -16)))
                if (!FindFood(pos + new Vector3i(0, 0, -16)))
                    if (!FindFood(pos + new Vector3i(16, 0, -16)))
                        if (!FindFood(pos + new Vector3i(-16, 0, 0)))
                            if (!FindFood(pos + new Vector3i(0, 0, 16)))
                                if (!FindFood(pos + new Vector3i(-16, 0, 16)))
                                    if (!FindFood(pos + new Vector3i(0, 0, 16)))
                                        if (!FindFood(pos + new Vector3i(16, 0, 16)))
                                        {
                                            FoodLocation = Vector3.zero;
                                            return false;
                                        }
        foodContainers.Add(new Vector3i(FoodLocation));
        return true;
    }

    private void AddFollow(EntityPlayer epLocalPlayer)
    {
        // custom AI - we will add a task to runaway, follow or approach playerentity
        if (this.world.IsRemote()) return;
        try
        {
            //trying my simple follow, since it seems more efficient
            followTarget = epLocalPlayer;
            //if (this.OA == null) this.OA = new EAITaskList();
            //if (this.OA.Tasks.Count > 0) return;
            //string _className = "EAIFollow"; // make the animal run away            
            //EAIBase _eai = (EAIBase)EntitySurvivorMod.LM.Instantiate(_className);
            //if (_eai == null)
            //    Debug.Log("Class '" + _className + "' not found!");
            //else
            //{
            //    _eai.SetEntity(this);
            //    _eai.SetPar1("EntityPlayer");
            //    _eai.SetPar2("50"); // setting a good range
            //    this.OA.AddTask(1, _eai);
            //    this.SetAttackTarget(epLocalPlayer as EntityAlive, 1000);
            //}
        }
        catch (Exception ex)
        {
            Debug.Log("ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }
    private void ClearFollow()
    {
        followTarget = (EntityAlive) null;
        this.SetAttackTarget((EntityAlive)null, 0);
        if (this.OA != null)
            if (this.OA.Tasks.Count > 0)
            {
                // if it has a follow task clears it
                // we dont want the animal to run from humans.
                EAIFollow[] tasks = this.GetLocalTasks<EAIFollow>();
                if (tasks != null)
                    if (tasks.Length > 0) this.OA.Tasks.Clear();                
            }
    }
    private void AddRunAway(string entityType)
    {
        // custom AI - we will add a task to runaway, follow or approach playerentity
        if (this.world.IsRemote()) return;
        try
        {

            if (this.RunAwaysTasks == null) this.RunAwaysTasks = new EAITaskList();
            //if (this.RunAwaysTasks.Tasks.Count > 0) return;
            debugHelper.doDebug("ADD RUNAWAY", debug);
            string _className = "EAIRunawayWhenHurt"; // make the animal run away            
            EAIBase _eai = (EAIBase)EntitySurvivorMod.LM.Instantiate(_className);
            if (_eai == null)
                Debug.Log("Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                if (entityType != "")
                    _eai.SetPar1(entityType);
                this.RunAwaysTasks.AddTask(1, _eai);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }
    private void ClearRunAway()
    {
        if (this.RunAwaysTasks != null)
            if (this.RunAwaysTasks.Tasks.Count > 0)
            {
                this.RunAwaysTasks.Tasks.Clear();
                this.SetAttackTarget((EntityAlive)null, 0);
            }
    }
    private void AddAttack(string entityType)
    {
        if (this.world.IsRemote()) return;
        try
        {

            if (this.AttackTasks == null) this.AttackTasks = new EAITaskList();
            if (this.AttackTasks.Tasks.Count > 0) return;
            //DoDebug("ADD RUNAWAY");
            string _className = "EAIApproachAndAttackTarget"; // make the animal run away            
            EAIBase _eai = (EAIBase)EntitySurvivorMod.LM.Instantiate(_className);
            if (_eai == null)
                Debug.Log("Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                if (entityType != "")
                {
                    _eai.SetPar1(entityType);
                    _eai.SetPar2("30");
                }
                this.AttackTasks.AddTask(1, _eai);
            }
            //_className = "EAISetNearestEntityAsTarget";          
            //_eai = (EAIBase)EntitySurvivorMod.LM.Instantiate(_className);
            //if (_eai == null)
            //    Debug.Log("Class '" + _className + "' not found!");
            //else
            //{
            //    _eai.SetEntity(this);
            //    if (entityType != "")
            //        _eai.SetPar1(entityType);
            //    this.AttackTasks.AddTask(2, _eai);
            //}
        }
        catch (Exception ex)
        {
            Debug.Log("ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }
    private void ClearAttack()
    {
        if (this.AttackTasks != null)
            if (this.AttackTasks.Tasks.Count > 0)
            {
                this.AttackTasks.Tasks.Clear();
                this.SetAttackTarget((EntityAlive)null, 0);
            }
    }
    private A[] DEH<A>([System.Runtime.InteropServices.In] EAITaskList obj0) where A : class
    {
        List<A> list = new List<A>();
        using (List<EAITaskEntry>.Enumerator enumerator = obj0.Tasks.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                EAITaskEntry current = enumerator.Current;
                if (current.action is A)
                    list.Add((object)current.action as A);
            }
        }
        if (list.Count > 0)
            return list.ToArray();
        return (A[])null;
    }

    private T[] GetLocalTasks<T>() where T : class
    {
        return this.DEH<T>(this.OA);
    }
    public void closeUI(EntityPlayer epLocalPlayer, bool follow, bool attack, bool inv, string questName, List<int> _blockedDialogs, bool flee, bool retaliate, bool spawn)
    {        
        (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
        (epLocalPlayer as EntityPlayerLocal).SetControllable(true);
        GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
        script = this.gameObject.GetComponent<survivorDialog>();
        if (script != null)
        {
            script.KillScript();
        }
        // save blocked dialogs
        SaveDialogFile();
        // send commands
        int comands = 0;
        comands = (comands & ~(1 << 0)); // dialog is closed
        if (follow)
        {
            try
            {
                string playerID = "";
                PersistentPlayerData dataFromEntityId1 =
                           GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(epLocalPlayer.entityId);
                if (dataFromEntityId1 != null) playerID = dataFromEntityId1.PlayerId;
                debugHelper.doDebug("SURVIVOR: owner is " + ownerID + " current player is " + playerID, debug);
                //// check if the current player is first owner, or friend of current owner...
                //if (ownerID != "" && playerID != ownerID && playerID != "")
                //{
                //    if (!dataFromEntityId1.ACL.Contains(ownerID))
                //    {
                //        GameManager.Instance.ClearTooltips();
                //        GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Refuse),"");
                //        follow = false;
                //    }
                //}
            }
            catch (Exception)
            {
                
                
            }            
            if (follow)
                comands = (comands | (1 << 1));
        }
        if (attack) comands = (comands | (1 << 2));
        if (flee) comands = (comands | (1 << 3));
        if (retaliate) comands = (comands | (1 << 4));
        if (spawn) comands = (comands | (1 << 5));
        SendComands(epLocalPlayer, comands);        
        if (_blockedDialogs != null)
        {
            blockedDialogs = _blockedDialogs;
            SaveDialogFile();
        }
        if (follow || attack)
        {
            if (doSleep)
            {
                GameManager.Instance.ClearTooltips();
                GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Accept));
            }
            return;
        }
        if (questName != "" && !attack && !follow && !GameManager.IsDedicatedServer)
        {            
            debugHelper.doDebug("LOOKS FOR QUEST " + questName, debug);            
            EntityPlayerLocal localPlayer = GameManager.Instance.World.GetLocalPlayer();
            QuestClass questClass = null;
            if (QuestClass.s_Quests.ContainsKey(questName.ToLower()))
            {
                questClass = QuestClass.s_Quests[questName.ToLower()];
            }
            if (questClass != null)
            {
                Quest quest = QuestClass.CreateQuest(questName.ToLower());
                Quest questExists = XUiM_Player.GetPlayer(-1).QuestJournal.FindQuest(questName.ToLower());
                if (questExists != null && (!questClass.Repeatable || quest.Active))
                {
                    GameManager.Instance.ClearTooltips();
                    GameManager.Instance.ShowTooltip("Not this again? I thought we were done with it...");
                    return;
                }
                else
                {
                    //localPlayer.QuestJournal.AddQuest(quest);
                    XUiM_Player.GetPlayer(-1).QuestJournal.AddQuest(quest);
                    if (doSleep)
                    {
                        GameManager.Instance.ClearTooltips();
                        GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Dismiss));
                    }
                    return;
                }
            }
            else debugHelper.doDebug("DID NOT FIND QUEST " + questName, debug);            
        }
        if (inv)
        {
            // opens entity inventory
            base.OnEntityActivated(indexInBlockActivationCommands, tePo, epLocalPlayer);
            return;
        }
        if (!retaliate && !flee && doSleep)
        {
            GameManager.Instance.ClearTooltips();
            GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Dismiss));
        }
    }

    private void SendComands(EntityPlayer epLocalPlayer, int comands)
    {
        DamageResponse dmg = new DamageResponse();
        dmg.Strength = 1000 + comands;
        dmg.Source = new DamageSourceEntity(EnumDamageSourceType.Disease, epLocalPlayer.entityId);
        dmg.Critical = false;
        dmg.ImpulseScale = 1;
        dmg.CrippleLegs = false;
        dmg.Dismember = false;
        dmg.Fatal = false;
        dmg.TurnIntoCrawler = false;
        if (this.world.IsRemote())
        {
            NetPackage _package1 = (NetPackage) new NetPackageDamageEntity(this.entityId, dmg);
            GameManager.Instance.SendToServer(_package1);
        }
        else
            this.DamageEntity(new DamageSourceEntity(EnumDamageSourceType.Disease, epLocalPlayer.entityId), 1000 + comands,
                false, 1);
    }

    private void GetSleepState()
    {
        DamageResponse dmg = new DamageResponse();
        dmg.Strength = 2000;
        dmg.Source = new DamageSourceEntity(EnumDamageSourceType.Disease, this.entityId);
        dmg.Critical = false;
        dmg.ImpulseScale = 1;
        dmg.CrippleLegs = false;
        dmg.Dismember = false;
        dmg.Fatal = false;
        dmg.TurnIntoCrawler = false;
        if (this.world.IsRemote())
        {
            NetPackage _package1 = (NetPackage)new NetPackageDamageEntity(this.entityId, dmg);
            GameManager.Instance.SendToServer(_package1);
        }
        else
            this.DamageEntity(new DamageSourceEntity(EnumDamageSourceType.Disease, this.entityId), 2000,
                false, 1);
    }

    private void IssueOrders(EntityPlayer epLocalPlayer, bool follow, bool attack, bool flee, bool retaliate, bool spawn)
    {        
        if (flee)
        {
            debugHelper.doDebug("Flee from player", debug);
            ClearAttack();
            ClearFollow();
            AddRunAway("EntityPlayer");
            return;
        }
        if (retaliate)
        {
            debugHelper.doDebug("Attack player", debug);
            ClearRunAway();
            ClearAttack();
            ClearFollow();
            SetAttackTarget((epLocalPlayer as EntityAlive), 30);
            AddAttack("EntityPlayer");
            return;
        }
        if (follow)
        {
            debugHelper.doDebug("Follow player", debug);
            ClearAttack();
            ClearRunAway();
            AddOwner(epLocalPlayer);
            AddFollow(epLocalPlayer);
        }
        else
        {
            ClearFollow();
        }
        if (attack)
        {
            debugHelper.doDebug("Patrol", debug);
            ClearFollow();
            ClearRunAway();
            AddOwner(epLocalPlayer);            
            AddAttack("EntityAlive");
        }
        else if (!follow)
        {
            debugHelper.doDebug("RunAway", debug);
            ClearAttack();
            ClearFollow();
            AddRunAway("EntityZombie");
        }
        if (spawn)
        {
            SpawnZedGroup();
        }
    }

    private void AddOwner(EntityPlayer epLocalPlayer)
    {
        if (ownerID == "")
        {
            PersistentPlayerData dataFromEntityId1 =
                GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(epLocalPlayer.entityId);
            if (dataFromEntityId1 != null) ownerID = dataFromEntityId1.PlayerId;
            debugHelper.doDebug("SURVIVOR NEW OWNER IS: " + ownerID, debug);
        }
    }

    private bool FindFood(Vector3i pos)
    {
        IChunk chunk = GameManager.Instance.World.GetChunkFromWorldPos(pos);

        for (int y = pos.y - 3; y < pos.y + 4; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    BlockValue b = chunk.GetBlock(x, y, z);

                    if (Block.list[b.type].GetBlockName() == foodContainer)
                    {
                        Vector3i wpos = chunk.GetWorldPos();

                        Vector3 dummyPos = new Vector3(wpos.x + x, y, wpos.z + z);
                        if (Vector3.Distance(this.GetPosition(), dummyPos) < checkArea)
                        {
                            FoodLocation = dummyPos;
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.45f, 0.45f, 1f);
    }


    public override EntityActivationCommand[] GetActivationCommands(Vector3i _tePos, EntityAlive _entityFocusing)
    {
        if (this.lootContainer == null)
            this.DDMOD[0].enabled = false;
        return this.DDMOD;
        ////EntityActivationCommand[] cmd = new EntityActivationCommand[0];
        ////cmd[0].text = "Press <E> to talk";
        ////cmd[0].enabled = true;
        ////return cmd;
        ////return "Click to talk";
        //EntityActivationCommand[] commands = base.GetActivationCommands(_tePos, _entityFocusing);
        //for (int i = 0; i < commands.Length; i++)
        //{
        //    if (name != "")
        //        commands[i].text = "<E>Talk to " + name;
        //    else commands[i].text = "<E> Talk";
        //    Debug.Log(commands[i].text);
        //}
        //return commands;
    }

    public override bool OnEntityActivated(int _indexInBlockActivationCommands, Vector3i _tePos,
        EntityAlive _entityFocusing)
    {
        if (this.lootContainer.IsUserAccessing() || dialogOpen)
        {
            GameManager.Instance.ClearTooltips();
            GameManager.Instance.ShowTooltip(string.Format("Can't you see i'm busy???"));
            return false; // someone is accessing the lootcontainer or dialog is opened   
        }
        if (IsDead())
        {
            return base.OnEntityActivated(_indexInBlockActivationCommands, _tePos, _entityFocusing);
        }
        indexInBlockActivationCommands = _indexInBlockActivationCommands;
        tePo = _tePos;
        //Debug.Log("BANG");
        if (!isSleeping && this.IsAlive() && !IsDead())
        {
            // attach script if its a player
            if ((_entityFocusing is EntityPlayer))
            {
                string playerID = "";
                //PersistentPlayerData dataFromEntityId1 =
                //    GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(_entityFocusing.entityId);
                //if (dataFromEntityId1 != null) playerID = dataFromEntityId1.PlayerId;
                //if (ownerID != "" && playerID != ownerID && playerID != "")
                //{
                //    if (!dataFromEntityId1.ACL.Contains(ownerID))
                //    {
                //        GameManager.Instance.ClearTooltips();
                //        GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Refuse));
                //        return false;
                //    }
                //    else debugHelper.doDebug("Friendly with owner", debug);
                //}
                // NPC info - name, gender, HP.
                if (name == "")
                {
                    name = NameGenerator.CreateNewName(isMale); // mudar isto para a instanciação
                    debugHelper.doDebug("Had to assign name", debug);
                }
                string msg = string.Format("{0}\nHP={1} ({2})", name, this.Health, isMale ? "Male" : "Female");
                script = this.gameObject.GetComponent<survivorDialog>();
                if (script != null)
                {
                    script.KillScript();
                }
                script = this.gameObject.AddComponent<survivorDialog>();
                if (script != null)
                {
                    if (ownerID != "" && playerID == ownerID && doSleep)
                    {
                        GameManager.Instance.ClearTooltips();
                        GameManager.Instance.ShowTooltip(QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Greeting));
                    }
                    LoadDialogFile();
                    script.ShowUI(_entityFocusing as EntityPlayer, this, dialogs, blockedDialogs, msg, doSleep);
                    //dialogOpen = true;
                    int comands = 0;
                    comands = (comands | (1 << 0)); // dialog is OPENED
                    SendComands((_entityFocusing as EntityPlayer), comands);
                    return true;
                }
            }
        }
        return false;
    }

    private List<clsDialog> GetDialogs()
    {
        List<clsDialog> dialogs = new List<clsDialog>();
        EntityClass entityClass = EntityClass.list[this.entityClass];
        for (int i = 1; i <= 20; i++)
        {
            if (entityClass.Properties.Values.ContainsKey("Dialog" + i))
            {
                clsDialog dlg = new clsDialog();
                dlg.dialogText = entityClass.Properties.Values["Dialog" + i].Replace("\\n", "\n");
                string[] aux = null;
                dlg.commandType = entityClass.Properties.Params2["Dialog" + i];
                if (dlg.commandType.Contains(","))
                {
                    aux = dlg.commandType.Split(',');
                    dlg.commandType = aux[0];
                    if (dlg.commandType == "Quest" || dlg.commandType == "Spawn")
                    {
                        if (aux.Length > 1) dlg.questName = aux[1];
                        if (aux.Length > 2) dlg.previousquestName = aux[2];
                        if (dlg.commandType == "Spawn")
                        {
                            dlg.commandType = "Quest";
                            dlg.spawn = true;
                            if (entityClass.Properties.Values.ContainsKey("SpawnText"))
                                dlg.spawnText = entityClass.Properties.Values["SpawnText"].Replace("\\n", "\n"); ;
                        }
                    }
                    else if (aux.Length > 1) dlg.previousquestName = aux[1];
                }
                dlg.skillName = entityClass.Properties.Params1["Dialog" + i];
                if (dlg.skillName.Contains(","))
                {
                    aux = dlg.skillName.Split(',');
                    dlg.skillName = aux[0];
                    if (aux.Length > 1) dlg.skillReq = int.Parse(aux[1]);
                }
                if (entityClass.Properties.Values.ContainsKey("Dialog" + i + "Ex"))
                {
                    // this has exclusion options
                    dlg.blockDialogs = entityClass.Properties.Values["Dialog" + i + "Ex"];
                    dlg.refusalAction = entityClass.Properties.Params1["Dialog" + i + "Ex"];
                }
                dialogs.Add(dlg);
            }
        }
        return dialogs;
    }

    private void SpawnZedGroup()
    {
        // spawns a EntityGroup of zeds
        try
        {
            int x;
            int y;
            int z;
            int i = 0;
            while (i < number)
            {
                GameManager.Instance.World.FindRandomSpawnPointNearPosition(
                    this.position, 15, out x, out y, out z,
                    new Vector3(area, area, area), true, false);
                int entityID = EntityGroups.GetRandomFromGroup(EntityGroup);
                Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                    new Vector3((float)x, (float)y, (float)z));
                spawnEntity.SetSpawnerSource(EnumSpawnerSource.Unknown);
                GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                i++;
            }
        }
        catch (Exception ex)
        {
            Debug.Log("SURVIVOR - error spawning");
        }        
    }

    // ************** FILE HANDLING ****************
    public string GetSavePath()
    {
        string path = GameUtils.GetSaveGameDir() + "/Survivors/";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        return path;
    }
    private void SaveDialogFile()
    {
        if (!IsAlive()) return;
        if (blockedDialogs.Count == 0)
        {
            DeleteDialogFile(); // no need to keep the file
            return;
        }
        if (GameManager.IsDedicatedServer) return; // only does this for the local player
        try
        {
            string path = GetSavePath();
            //DoDebug("SAVING FILE PATH: " + path);
            if (!System.IO.Directory.Exists(path))
            {
                return;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder("");
            for (int i = 0; i < blockedDialogs.Count; i++)
            {
                sb.Append(blockedDialogs[i].ToString() + Environment.NewLine);
            }
            string filename = path + this.entityId.ToString() + ".dg";
            //DoDebug("FILE: " + filename);
            debugHelper.doDebug("SURVIVOR SAVING FILE: " + filename, debug);
            System.IO.File.WriteAllText(filename, sb.ToString());
        }
        catch (Exception ex)
        {
            debugHelper.doDebug("SURVIVORS -> ERROR SAVING FILE " + ex.Message, debug);
        }
    }

    private bool LoadDialogFile()
    {
        try
        {
            blockedDialogs.Clear();
            if (GameManager.IsDedicatedServer) return false; // only does this for the local player
            string path = GetSavePath();
            string filename = path + this.entityId + ".dg";
            debugHelper.doDebug("SURVIVOR LOAD FILE: " + filename, debug);
            if (!System.IO.File.Exists(filename))
            {
                debugHelper.doDebug("FILE DOES NOT EXIST", debug);
                return false;
            }
            if (this.entityId > 0)
            {
                string[] lines = System.IO.File.ReadAllLines(filename);                
                foreach (string line in lines)
                {
                    debugHelper.doDebug("SURVIVOR READ: " + line, debug);
                    blockedDialogs.Add(Convert.ToInt32(line));
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            debugHelper.doDebug("SURVIVORS -> ERROR LOADING: " + ex.Message, debug);
            blockedDialogs.Clear();
        }
        return false;
    }

    private void DeleteDialogFile()
    {
        try
        {
            if (GameManager.IsDedicatedServer) return;
            if (this.entityId > 0)
            {
                string path = GetSavePath();
                string filename = path + this.entityId + ".dg";
                if (!System.IO.File.Exists(filename))
                {
                    return;
                }
                System.IO.File.Delete(filename);
            }
        }
        catch (Exception ex)
        {
        }
    }
}

public class survivorDialog : MonoBehaviour
{
    private EntityPlayer epLocalPlayer;
    private EntitySurvivorMod survivorObj;
    private bool showUI = false;
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color buttonColor = Color.green;
    public Vector2 scrollPosition = Vector2.zero;
    private bool doSleep = false;
    private bool closeUI = false;
    private bool openUI = false;
    private int numDialogs = 4;
    private bool follow = false;
    private bool attack = false;
    private bool flee = false;
    private bool retaliate = false;
    private bool inv = false;
    private bool spawn = false;
    private string questName = "";
    private List<clsDialog> Dialogs = new List<clsDialog>();
    private List<int> BlockedDialogs = new List<int>();
    private string _title = string.Empty;
    private bool waitConfirm = false;
    private string confirmText = "An event is about to start.\nDo you want to proceed?";
    private int with = 250;

    void Start()
    {
        showUI = false;
    }

    public void ShowUI(EntityPlayer player, EntitySurvivorMod survivor, List<clsDialog> dialogs, List<int> blockedDialogs, string title, bool DoSleep)
    {
        if (showUI)
        {
            CloseUI();
        }
        else
        {
            _title = title;
            epLocalPlayer = player;
            survivorObj = survivor;
            Dialogs = dialogs;
            BlockedDialogs = blockedDialogs;
            closeUI = false;
            openUI = true;
            doSleep = DoSleep;
        }
    }

    void Update()
    {

    }

    public void OnGUI()
    {
        if (openUI)
        {
            showUI = true;
            openUI = false;
            closeUI = false;
        }
        else if (closeUI)
        {
            CloseUI();
        }
        if (showUI && epLocalPlayer != null)
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
            titleSkin.normal.background = MakeTex(2, 2, Color.red);
            boxSkin.normal.background = MakeTex(2, 2, Color.grey);
            boxSkin.normal.textColor = Color.white;
            boxSkin.fontSize = 14;
            boxSkin.fontStyle = FontStyle.Bold;
            noJobSkin.normal.background = MakeTex(2, 2, Color.grey);
            noJobSkin.normal.textColor = Color.yellow;
            noJobSkin.fontSize = 15;
            noJobSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.fontSize = 16;
            recTitleSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.normal.textColor = Color.cyan;
            recTitleSkin.normal.background = MakeTex(2, 2, Color.green);
            if (waitConfirm) with = 500;
            else with = 250;
            // GUISTYLES **************          
            (epLocalPlayer as EntityPlayerLocal).SetControllable(false);
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = true;
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
            scrollPosition = GUI.BeginScrollView(new Rect(10, 10, with+30, 500), scrollPosition,
                new Rect(0, 0, with, 50*(numDialogs + 1)));
            GUI.color = Color.magenta;
            GUILayout.Box(_title, titleSkin, GUILayout.Width(with),
                GUILayout.Height(50));
            GUI.color = colorNatural;

            #region Close Button;

            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Nothing... Bye!", buttonSkin, GUILayout.Width(with),
                GUILayout.Height(50)))
            {
                follow = false;
                attack = false;
                flee = false;
                retaliate = false;
                inv = false;
                spawn = false;
                questName = "";
                CloseUI();
            }

            #endregion;                        

            GUI.contentColor = Color.white;
            GUI.backgroundColor = originalColor;
            string msg = "";
            if (waitConfirm)
            {
                // show the confirmation text, close button and cancel button.
                msg = confirmText;
                GUILayout.Box(msg, boxSkin, GUILayout.Width(with),
                    GUILayout.Height(250));
            }
            if (Dialogs.Count > 0 && !waitConfirm)
            {
                for(int i=0;i<Dialogs.Count;i++)
                {
                    msg = Dialogs[i].dialogText;
                    bool showDialog = true;
                    if (BlockedDialogs.Contains(i + 1)) showDialog = false;
                    else if (Dialogs[i].commandType == "Quest")
                    {
                        if (Dialogs[i].previousquestName != "")
                        {
                            Quest questExists =
                                XUiM_Player.GetPlayer(-1)
                                    .QuestJournal.FindQuest(Dialogs[i].previousquestName.ToLower());
                            if (questExists == null || questExists.Active) showDialog = false; // still needs to finish the previous quest
                        }
                        //else
                        {
                            Quest questExists =
                                XUiM_Player.GetPlayer(-1)
                                    .QuestJournal.FindQuest(Dialogs[i].questName.ToLower());
                            if (questExists != null) showDialog = false; // quest  already done or in progress
                        }
                    }
                    else if (Dialogs[i].previousquestName != "")
                    {
                        Quest questExists =
                            XUiM_Player.GetPlayer(-1)
                                .QuestJournal.FindQuest(Dialogs[i].previousquestName.ToLower());
                        if (questExists == null || questExists.Active) showDialog = false; // still needs to finish the previous quest
                    }
                    if (showDialog)
                    {
                        if (GUILayout.Button(msg, buttonSkin,
                            GUILayout.Width(with),
                            GUILayout.Height(50)))
                        {
                            questName = "";
                            // chance to fail - dependent on skill level?
                            int failChance = 0;
                            bool failed = false;
                            flee = false;
                            retaliate = false;
                            if (Dialogs[i].skillName != "" && Dialogs[i].skillReq > 0)
                            {
                                Skill skill = epLocalPlayer.Skills.GetSkillByName(Dialogs[i].skillName);
                                if (skill != null)
                                {
                                    failChance = Dialogs[i].skillReq - skill.Level;
                                    if (failChance <= 0)
                                    {
                                        failChance = 0;
                                        // residual chance to fail
                                        if (Dialogs[i].skillReq >= 50)
                                            failChance = 2;
                                        else if (Dialogs[i].skillReq >= 80)
                                            failChance = 4;
                                    }
                                }
                            }
                            if (failChance > 0)
                            {
                                System.Random rnd = new System.Random();
                                if (rnd.Next(0, 101) < failChance)
                                {
                                    failed = true;
                                    if (Dialogs[i].blockDialogs != "")
                                    {
                                        string[] aux = Dialogs[i].blockDialogs.Split(',');
                                        foreach (string blockD in aux)
                                        {
                                            if (!BlockedDialogs.Contains(Convert.ToInt32(blockD)))
                                                BlockedDialogs.Add(Convert.ToInt32(blockD));
                                        }
                                    }
                                    string quote = "";
                                    if (!doSleep)
                                        quote = string.Format("You should improve your {0} skill level...",
                                            Dialogs[i].skillName);
                                    if (Dialogs[i].refusalAction == "Flee")
                                    {
                                        if (doSleep)
                                            quote = QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Flee);
                                        flee = true;
                                    }
                                    else if (Dialogs[i].refusalAction == "Attack")
                                    {
                                        if (doSleep)
                                            quote = QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Attack);
                                        retaliate = true;
                                    }
                                    else
                                    {
                                        if (doSleep)
                                            quote = QuoteRandomizer.GetQuote(QuoteRandomizer.QuoteType.Refuse);                                        
                                    }
                                    GameManager.Instance.ClearTooltips();
                                    GameManager.Instance.ShowTooltip(quote);
                                }
                            }
                            follow = false;
                            attack = false;
                            if (!failed)
                            {
                                if (Dialogs[i].commandType == "Follow")
                                    follow = true;
                                if (Dialogs[i].commandType == "Patrol")
                                    attack = true;
                                if (Dialogs[i].commandType == "Inv")
                                    inv = true;
                                if (Dialogs[i].commandType == "Quest")
                                    questName = Dialogs[i].questName;
                                spawn = Dialogs[i].spawn;
                                if (spawn || questName != "")
                                {
                                    waitConfirm = true;
                                    QuestClass questClass = null;
                                    if (QuestClass.s_Quests.ContainsKey(questName.ToLower()))
                                    {
                                        questClass = QuestClass.s_Quests[questName.ToLower()];
                                    }
                                    if (questClass != null) confirmText = questClass.Description;                                    
                                    else if (Dialogs[i].spawnText == "")
                                        confirmText = "An event is about to start.\nDo you want to proceed?";
                                    else confirmText = Dialogs[i].spawnText;
                                }
                                else CloseUI();

                            }
                            else if (flee || retaliate)
                                CloseUI(); // if not relatiating or fleeing, it just blocks dialogs but does not close the dialog
                        }
                    }
                }
            }
            if (!waitConfirm)
            {
                // always valid
                msg = "Stay here";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(with),
                    GUILayout.Height(50)))
                {
                    follow = false;
                    attack = false;
                    flee = false;
                    retaliate = false;
                    inv = false;
                    spawn = false;
                    questName = "";
                    CloseUI();
                }
            }
            else
            {
                msg = "Go on\nShow me what you got!";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(with),
                    GUILayout.Height(50)))
                {                   
                    waitConfirm = false;
                    CloseUI();
                }
                msg = "Cancel";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(with),
                    GUILayout.Height(50)))
                {
                    follow = false;
                    attack = false;
                    flee = false;
                    retaliate = false;
                    inv = false;
                    spawn = false;
                    questName = "";
                    waitConfirm = false;
                }
            }
            GUI.contentColor = Color.white;
            GUI.backgroundColor = originalColor;
            GUI.EndScrollView();
        }
    }

    private void CloseUI()
    {
        // 
        showUI = false;
        if (survivorObj != null) survivorObj.closeUI(epLocalPlayer, follow, attack, inv, questName, BlockedDialogs, flee, retaliate, spawn);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    public void KillScript()
    {
        survivorDialog script = gameObject.GetComponent<survivorDialog>();
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