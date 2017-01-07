using Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions.Must;
using XMLData.Item;
using Debug = UnityEngine.Debug;

public class clsDog
{
    public string AnimalName;
    public bool IsPregnant = false;
    public ulong BirthTime;
    public ulong offspring;
    public ulong NextMating; // if it's a male he will look for a female and try to mate with it
    public ulong NextFoodDecay;
    // stores the father dog UID, so that the pup can have its stats calculated
    // TODO - if the father dies... Hmmm...
    public int BabyUID;
    public uint FoodLevel; // 0-20 -> if it reaches 0, starts to do damage. The percentage of insemination success depends on this food level. Always starts with 20    
    public uint Happiness;
    // DOG STATISTICS
    public uint Loyalty; // 0-20 -> influences obidience to call and slightly influences hability to follow an order
    public uint Inteligence; // 0-20 -> influences how easy it is for a dog to accept an order, and slightly affects damage
    public uint Strenght; // 0-20 -> influences how hard the dog will hit
    public uint Stamina; // 0-20 -> influences how fast the dog recovers health, and slightly affects damag
    // TRAINING ATTRIBUTES - DOG HABILITIES
    // Through training you cannot have more then 30 assigned points.
    // So you cannot have the perfect dog through training only
    // Through breeding you can achieve highly versatile combinations, since they will inherit combined parent statistics
    public uint attackDog; // 0-20 -> training on zombie attack - influences zombie damage
    public uint militaryDog; // 0-20 -> training on human attack - influences human damage
    public uint hunterDog; // 0-20 -> training on animal attack - influences animal damage
    public uint breederDog; // 0-20 -> training on breeding - influences hability to breed
    public string Owner;
}
//public class EntityDogMorte : EntityAnimalStag
public class EntityDogMorte : EntityZombieDog
{
    private bool debug = false;

    private System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
    public static Dictionary<Vector3i, int> FoodClaims = new Dictionary<Vector3i, int>();

    public static int SkillID = -1;

        //1 day = 24000 ticks (for 60min day)
    public ulong Adolesence = (48000);
    public int BabyCheck = 60;
    public int WanderTime = 1000;
    public ulong PregnancyTime = 48000;
    public ulong MatingTime = 24000;
    //public ulong FoodDecayTime = 6000;
    public ulong FoodDecayTime = 100;
    //public static int EatTime = 40;
    public int EatTime = 5;
    public float MaxDistanceToFood = 40f;
    public int MaxAnimalsInArea = 5;
    public float MaxAnimalsAreaCheck = 40f;
    public float MaxMatingAreaCheck = 20f;
    public float DelayToResumeTasks = 30; // in seconds
    private int AttackTimeout = 0;
    private string animalBlock = "dummyAnimalBlock";
    private string humanBlock = "dummyHumanBlock";
    private string zombieBlock = "dummyZombieBlock";
   
    private int pathTimeout = 250; // after this ticks they will simply giveup on the current path
    public int followInterval = 2; // a friendly animal will try to follow a human present each 2s.

    #region Class stat limits;

    private uint[] FoodLevelL = {0, 20};
    private uint[] HappinessL = {0, 20};
    // DOG STATISTICS
    private uint[] LoyaltyL = {0, 20};
    private uint[] InteligenceL = {1, 20};
    private uint[] StrenghtL = {1, 20};
    private uint[] StaminaL = {1, 20};
    // TRAINING ATTRIBUTES - DOG HABILITIES
    private uint[] attackDogL = {0, 20};
    private uint[] militaryDogL = {0, 20};
    private uint[] hunterDogL = {0, 20};
    private uint[] breederDogL = {0, 20};
    #endregion;
    #region Personal Variables, refering to the animal instance;
    //save these variables
    private bool WasTamed = false;
    bool IsTamed = false;
    bool IsMale = false; // males are rare      
    bool IsBaby = false;
    private int id0Prev = 0;
    private int idInit = 0;
    int id0; // used to identify the file
    int id1; // used to identify the file
    int id2; // used to identify the file  
    string MsgColor = "[B62F13]";
    Vector3 FoodLocation = new Vector3();
    Vector3 DummyLocation = Vector3.zero; // location of the last training dummy
    #endregion;        
    #region General properties;
    private float meshScale = 1;
    private string entityMale = "";
    private string entityFemale = "";
    private string entityBaby = "";
    private string itemTamed = "";
    private string foodBlock = "";
    private string foodItem = "";
    private string waterItem = "";
    private string emptyFoodItem = "";
    private string emptyWaterItem = "";
    private string tameItem = "";
    private string harvestItem = "";
    private string lootItem = "";
    private string tameSkill = "";
    private string harvestSkill = "";
    private string SoundPlay = "";
    private string SoundDropBall = "";
    private string SoundStarve = "";
    private string SoundBark = "";
    private string SoundHowl = "";
    private string SoundDrink = "";
    private string SoundEat = "";
    #endregion;
    #region Custom AI;
    // trying to customize AI, so that I can manipulate
    // AI tasks refering to player (approach or run away)
    // this means this entities will have 1 aditional taskList, refering to custom AUTOMATIC AI
    private static EAIFactory LM = new EAIFactory();
    private EAITaskList OA; // attack tasks
    private EAITaskList IO; // target tasks
    private EntityAlive followEntityAlive; // follow owner instead of wandering
    private EntityItem toyToPlay; // go to toy
    #endregion;
    AnimalState State = AnimalState.Wander;
    #region Update Timers
    DateTime EatUntil;
    DateTime NextUpdateCheck = DateTime.MinValue;
    DateTime NextFollowCheck = DateTime.MinValue;
    DateTime NextAskCheck = DateTime.MinValue;
    DateTime NextHowl = DateTime.MinValue;
    DateTime NextTaskCycle = DateTime.MinValue;
    Vector3 LastDropPosition = Vector3.zero;
    #endregion
    private bool needsLoad = false; // to tell the server that it is a tamed animal, that needs to load the file    
    public clsDog animalClass = new clsDog(); // has the structure containing the personality variables, which are saved to file for persistency.

    private void DoDebug(string msg)
    {
        if (debug)
            Debug.Log(msg);
    }

    public override void Init(int _entityClass)
    {        
        if (!world.IsRemote())
        {
            try
            {
                DoDebug(string.Format("DOGSDX -> Init -> PLAYERID: {0}, TEAM: {1} - {2}, LIFETIME: {3}", this.belongsPlayerId, this.TeamNumber, this.teamNumber, this.lifetime));
                // no need to initialize here. If a players spawns a newly tamed animal, the random stats will be defined at that moment,
                if (id0 == 0)
                {
                    // i DO NOT tame here, because if may be a vanilla animal.
                    // this id is created for the sole purpose of syncronizing with clients.
                    GetUID(); 
                    idInit = id0;
                //    SaveAnimalFile(idInit, true, animalClass); // reserve the ID
                }
            }
            catch (Exception ex)
            {
                DoDebug("DOGSDX -> Init ERROR: " + ex.Message);
            }
            // HERE I SHOULD ALREADY HAVE THE ID, IF IT EXISTED PREVIOUSLY           
        }
        base.Init(_entityClass);        
        try
        {
            this.timeToDie = this.world.worldTime + 8760000;
            EntityClass entityClass = EntityClass.list[_entityClass];
            if (entityClass.Properties.Values.ContainsKey("entityMale"))
                entityMale = entityClass.Properties.Values["entityMale"];
            if (entityClass.Properties.Values.ContainsKey("entityFemale"))
                entityFemale = entityClass.Properties.Values["entityFemale"];
            if (entityClass.Properties.Values.ContainsKey("entityBaby"))
                entityBaby = entityClass.Properties.Values["entityBaby"];
            if (entityClass.Properties.Values.ContainsKey("itemTamed"))
                itemTamed = entityClass.Properties.Values["itemTamed"];
            if (entityClass.Properties.Values.ContainsKey("isMale"))
                if (!bool.TryParse(entityClass.Properties.Values["isMale"].ToString(), out IsMale)) IsMale = false;
            if (entityClass.Properties.Values.ContainsKey("isBaby"))
                if (!bool.TryParse(entityClass.Properties.Values["isBaby"].ToString(), out IsBaby)) IsBaby = false;
            if (entityClass.Properties.Values.ContainsKey("foodBlock"))
                foodBlock = entityClass.Properties.Values["foodBlock"];
            if (entityClass.Properties.Values.ContainsKey("foodItem"))
                foodItem = entityClass.Properties.Values["foodItem"];
            if (entityClass.Properties.Values.ContainsKey("waterItem"))
                waterItem = entityClass.Properties.Values["waterItem"];
            if (entityClass.Properties.Values.ContainsKey("emptyFoodItem"))
                emptyFoodItem = entityClass.Properties.Values["emptyFoodItem"];
            if (entityClass.Properties.Values.ContainsKey("emptyWaterItem"))
                emptyWaterItem = entityClass.Properties.Values["emptyWaterItem"];
            if (entityClass.Properties.Values.ContainsKey("tameItem"))
                tameItem = entityClass.Properties.Values["tameItem"];
            if (entityClass.Properties.Values.ContainsKey("harvestItem"))
                harvestItem = entityClass.Properties.Values["harvestItem"];
            if (entityClass.Properties.Values.ContainsKey("lootItem"))
                lootItem = entityClass.Properties.Values["lootItem"];
            if (entityClass.Properties.Values.ContainsKey("tameSkill"))
                tameSkill = entityClass.Properties.Values["tameSkill"];
            if (entityClass.Properties.Values.ContainsKey("harvestSkill"))
                harvestSkill = entityClass.Properties.Values["harvestSkill"];
            if (entityClass.Properties.Values.ContainsKey("SoundPlay"))
                SoundPlay = entityClass.Properties.Values["SoundPlay"];
            if (entityClass.Properties.Values.ContainsKey("SoundDropBall"))
                SoundDropBall = entityClass.Properties.Values["SoundDropBall"];
            if (entityClass.Properties.Values.ContainsKey("SoundStarve"))
                SoundStarve = entityClass.Properties.Values["SoundStarve"];
            if (entityClass.Properties.Values.ContainsKey("SoundBark"))
                SoundBark = entityClass.Properties.Values["SoundBark"];
            if (entityClass.Properties.Values.ContainsKey("SoundHowl"))
                SoundHowl = entityClass.Properties.Values["SoundHowl"];
            if (entityClass.Properties.Values.ContainsKey("SoundDrink"))
                SoundDrink = entityClass.Properties.Values["SoundDrink"];
            if (entityClass.Properties.Values.ContainsKey("SoundEat"))
                SoundEat = entityClass.Properties.Values["SoundEat"];
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
            // i passed this properties here, because I want them to be associated to each different animal
            if (entityClass.Properties.Values.ContainsKey("Adolesence"))
                Adolesence = ulong.Parse(entityClass.Properties.Values["Adolesence"].ToString());
            if (entityClass.Properties.Values.ContainsKey("PregnancyTime"))
                PregnancyTime = ulong.Parse(entityClass.Properties.Values["PregnancyTime"].ToString());
            if (entityClass.Properties.Values.ContainsKey("WanderTime"))
                WanderTime = int.Parse(entityClass.Properties.Values["WanderTime"].ToString());
            if (entityClass.Properties.Values.ContainsKey("MatingTime"))
                MatingTime = ulong.Parse(entityClass.Properties.Values["MatingTime"].ToString());
            if (entityClass.Properties.Values.ContainsKey("MaxAnimalsInArea"))
                MaxAnimalsInArea = int.Parse(entityClass.Properties.Values["MaxAnimalsInArea"].ToString());
            if (entityClass.Properties.Values.ContainsKey("MaxAnimalsAreaCheck"))
                MaxAnimalsAreaCheck = float.Parse(entityClass.Properties.Values["MaxAnimalsAreaCheck"].ToString());
            if (entityClass.Properties.Values.ContainsKey("MaxMatingAreaCheck"))
                MaxMatingAreaCheck = float.Parse(entityClass.Properties.Values["MaxMatingAreaCheck"].ToString());

            GetStatLimits(entityClass);

            animalClass.BirthTime = GameManager.Instance.World.GetWorldTime();
            if (IsBaby) MakeBaby();
            else
            {
                NextAskCheck = DateTime.Now.AddSeconds(this.GetRandom().Next(30, 50));
                NextHowl = DateTime.Now.AddSeconds(this.GetRandom().Next(20, 40));
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> Init(1) ERROR: " + ex.Message);
        }
    }

    private void GetStatLimits(EntityClass entityClass)
    {
        #region stat limits if configured;

        uint minVal = 0;
        uint maxVal = 20;
        string scaleStr = "";
        string[] auxS = null;
        if (entityClass.Properties.Values.ContainsKey("Loyalty"))
        {
            scaleStr = entityClass.Properties.Values["Loyalty"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            LoyaltyL[0] = minVal;
            LoyaltyL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("Inteligence"))
        {
            scaleStr = entityClass.Properties.Values["Inteligence"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            InteligenceL[0] = minVal;
            InteligenceL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("Stamina"))
        {
            scaleStr = entityClass.Properties.Values["Stamina"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            StaminaL[0] = minVal;
            StaminaL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("Strenght"))
        {
            scaleStr = entityClass.Properties.Values["Strenght"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            StrenghtL[0] = minVal;
            StrenghtL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("Happiness"))
        {
            scaleStr = entityClass.Properties.Values["Happiness"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            HappinessL[0] = minVal;
            HappinessL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("FoodLevel"))
        {
            scaleStr = entityClass.Properties.Values["FoodLevel"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            FoodLevelL[0] = minVal;
            FoodLevelL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("attackDog"))
        {
            scaleStr = entityClass.Properties.Values["attackDog"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            attackDogL[0] = minVal;
            attackDogL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("militaryDog"))
        {
            scaleStr = entityClass.Properties.Values["militaryDog"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            militaryDogL[0] = minVal;
            militaryDogL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("hunterDog"))
        {
            scaleStr = entityClass.Properties.Values["hunterDog"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            hunterDogL[0] = minVal;
            hunterDogL[1] = maxVal;
        }
        if (entityClass.Properties.Values.ContainsKey("breederDog"))
        {
            scaleStr = entityClass.Properties.Values["breederDog"];
            auxS = scaleStr.Split(',');

            if (auxS.Length == 1)
            {
                minVal = 0;
                maxVal = uint.Parse(auxS[0]);
            }
            else if (auxS.Length == 2)
            {
                minVal = uint.Parse(auxS[0]);
                maxVal = uint.Parse(auxS[1]);
            }
            breederDogL[0] = minVal;
            breederDogL[1] = maxVal;
        }

        #endregion;
    }

    public override void PostInit()
    {
        try
        {
            DoDebug(string.Format("DOGSDX -> PostInit -> PLAYERID: {0}, TEAM: {1} - {2}, LIFETIME: {3}", this.belongsPlayerId, this.TeamNumber, this.teamNumber, this.lifetime));
            base.PostInit();
            if (!world.IsRemote())
                CheckCreationID();
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> PostInit ERROR: " + ex.Message);
        }                        
    }

    private void CheckCreationID()
    {
        // IF LIFETIME HERE BRINGS AND ID (>100 && < float.maxvalue), I MUST use this ID instead... What i'll do is copy this "old" file to the new one
        // thus keeping things synced between clients.
        if (lifetime == 100)
        {            
            DoDebug("DOGSDX - A NEWLY TAMED ANIMAL, NO FILE YET");
            this.Tame(id0, 0, 0);
            SaveAnimalFile(id0, true, animalClass);
            this.timeToDie = this.world.worldTime + 8760000;
        }
        else if (lifetime == 200)
        {
            DoDebug("DOGSDX - A NEWLY TAMED FEMALE BABY, NO FILE YET");
            this.Tame(id0, 0, 0);
            if (IsBaby)
                MakeBaby();
            IsMale = false;            
            this.timeToDie = this.world.worldTime + 8760000;
            SaveAnimalFile(id0, true, animalClass);
        }
        else if (lifetime == 300)
        {
            DoDebug("DOGSDX - A NEWLY TAMED MALE BABY, NO FILE YET");
            this.Tame(id0, 0, 0);
            if (IsBaby)
                MakeBaby();
            IsMale = true;
            this.timeToDie = this.world.worldTime + 8760000;
            SaveAnimalFile(id0, true, animalClass);
        }
        else
        {
            if (lifetime > 100 && lifetime <= 99999999)
            {
                int oldID = (int) lifetime;
                DoDebug("DOGSDX - TAMED ANIMAL, OLD ID = " + oldID);
                if (LoadAnimalFile(oldID))
                {
                    // loads old file
                    DeleteAnimalFile(oldID); // delete old file
                }
                else
                {
                    DoDebug("DOGSDX - ERROR LOADING ORIGINAL FILE");
                    NewDog(ref animalClass);
                    Tame(id0, 0, 0);
                }                
                SaveAnimalFile(id0, true, animalClass); // creates new file
                // if its a baby animal, remember to make baby
                if (animalClass.AnimalName == "babyMale" && IsBaby)
                {
                    MakeBaby();
                    IsMale = true;
                }
                else if (animalClass.AnimalName == "babyFemale" && IsBaby)
                {
                    MakeBaby();
                    IsMale = false;
                }
            }
            else
            {
                DoDebug(string.Format("DOGSDX - MAYBE RANDOM SPAWN - TAMED = {0}", IsTamed.ToString()));
                if (!IsTamed)
                {
                    // free the ID
                    //DeleteAnimalFile(id0);
                    //id0 = 0;
                    NewDog(ref animalClass); // assign random stats
                    this.timeToDie = this.world.worldTime + 8760000;
                    if (id0 == 0) NewUID();
                    //SaveAnimalFile(id0, true, animalClass); 
                    // the file always exists even for random spawns, because we WANT to have the random stats in
                    // the dog can age and lives up to 1 year (50min days)
                }
            }
        }
        lifetime = float.MaxValue;
    }

    #region CUSTOM AI CONTROL;

    public override void CopyPropertiesFromEntityClass()
    {        
        base.CopyPropertiesFromEntityClass();
       //AddRunAway();
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

    private void AddRunAway()
    {
        // custom AI - we will add a task to runaway, follow or approach playerentity
        if (this.world.IsRemote()) return;
        try
        {
            if (this.OA == null) this.OA = new EAITaskList();
            if (this.OA.Tasks.Count > 0) return;
            DoDebug("DOGSDX - ADD RUNAWAY");
            string _className = "EAIRunawayFromEntity"; // make the animal run away            
            EAIBase _eai = (EAIBase)EntityDogMorte.LM.Instantiate(_className);
            if (_eai == null)
                DoDebug("DOGSDX - Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                _eai.SetPar1("EntityPlayer");
                this.OA.AddTask(1, _eai);
            }
        }
        catch (Exception ex)
        {
            DoDebug("ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }
    private void AddApproachSpot()
    {
        // custom AI - we will add a task to runaway, follow or approach playerentity
        if (this.world.IsRemote()) return;
        try
        {
            if (this.OA == null) this.OA = new EAITaskList();
            if (this.OA.Tasks.Count > 0) return;
            DoDebug("DOGSDX - ADD APPROACH SPOT");
            string _className = "EAIApproachSpot"; // make the animal run away            
            EAIBase _eai = (EAIBase)EntityDogMorte.LM.Instantiate(_className);
            if (_eai == null)
                DoDebug("DOGSDX - Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                this.OA.AddTask(1, _eai);
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX - ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }
    private void AddApproachAndAttackTarget(string parameter1, int prio)
    {
        // custom AI - we will add a task to runaway, follow or approach playerentity
        if (this.world.IsRemote()) return;
        try
        {
            if (this.OA == null) this.OA = new EAITaskList();
            if (this.IO == null) this.IO = new EAITaskList();
            if (this.OA.Tasks.Count >= prio) return;
            DoDebug("DOGSDX - ADD APPROACHANDATTACK " + parameter1);
            string _className = "EAIApproachAndAttackTarget"; // make the approach and attack the set target            
            EAIBase _eai = (EAIBase)EntityDogMorte.LM.Instantiate(_className);
            if (_eai == null)
                DoDebug("DOGSDX - Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                _eai.SetPar1(parameter1);
                _eai.SetPar2("30");
                this.OA.AddTask(prio, _eai);
            }            
            _className = "EAISetNearestEntityAsTarget";
            EAIBase _eat = (EAIBase)EntityDogMorte.LM.Instantiate(_className);
            if (_eat == null)
                DoDebug("DOGSDX - Class '" + _className + "' not found!");
            else
            {
                _eat.SetEntity(this);
                _eat.SetPar1(parameter1);
                this.IO.AddTask(prio, _eat);
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX - ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }

    private bool FindTarget()
    {
        try
        {
            if (animalClass != null)
            {
                uint min = 5;
                if (FoodLevelL[1] < min) min = FoodLevelL[1];
                if (FoodLevelL[0] > min) min = FoodLevelL[0];
                if (animalClass.FoodLevel < min)
                {
                    // too hungry - refuses to act
                    //this.getNavigator().clearPathEntity(); // cancel current action
                    if (State == AnimalState.FollowOwner || State == AnimalState.AttackZed ||
                        State == AnimalState.AttackHuman || State == AnimalState.AttackAnimals)
                    {
                        DoDebug("DOGSDX - Action canceled. Too hungry");
                        ClearCustomTasks(2);
                        this.getNavigator().clearPathEntity();
                        State = AnimalState.Wander;
                        return false;
                    }                    
                }
            }
            if (State == AnimalState.AttackZed || State == AnimalState.AttackHuman || State == AnimalState.AttackAnimals)
            {
                return SearchAndAttackTarget();
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX - ERROR FINDING TARGET " + ex.Message);
            return false;
        }
        return false;
    }

    private bool FindOwnerToFollow()
    {
        //DoDebug(string.Format("LOOK FOR HUMAN"));
        // IF THE POSITION IS LONGER THEN 5f ? Otherwise it will just do what it has to do.
        try
        {
            if (animalClass != null)
            {
                // if the animal max foodlevel is greater then 5, then it will consider that value
                uint min = 5;
                if (FoodLevelL[1] < min) min = FoodLevelL[1];
                if (FoodLevelL[0] > min) min = FoodLevelL[0];
                if (animalClass.FoodLevel < min && State != AnimalState.ReturnToy)
                {
                    // if it needs food it will look for food instead
                    //DoDebug(string.Format("TOO HUNGRY TO PLAY"));
                    if (State == AnimalState.AskPlay || State == AnimalState.ReturnToy || State == AnimalState.GotoToy ||
                        State == AnimalState.FollowOwner || State == AnimalState.AttackZed ||
                        State == AnimalState.AttackHuman || State == AnimalState.AttackAnimals)
                    {
                        DoDebug("DOGSDX - Action canceled. Too hungry");                        
                        ClearCustomTasks(2);
                        this.getNavigator().clearPathEntity(); // cancel current action
                        State = AnimalState.Wander;
                    }
                    NextAskCheck = DateTime.Now.AddSeconds(120);
                    //return false;
                }                
            }
            float seeDistance = this.GetSeeDistance();
            EntityAlive _other = null;
            if (animalClass != null)
            {
                using (
                    List<Entity>.Enumerator enumerator =
                        this.world.GetEntitiesInBounds(typeof (EntityPlayer),
                            BoundsUtils.BoundsForMinMax(this.position.x - seeDistance, this.position.y - seeDistance,
                                this.position.z - seeDistance, this.position.x + seeDistance,
                                this.position.y + seeDistance,
                                this.position.z + seeDistance)).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        _other = enumerator.Current as EntityAlive;
                        // see if it is the owner.
                        PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
                        if (persistentPlayerList.GetPlayerDataFromEntityID(_other.entityId).PlayerId ==
                            animalClass.Owner)
                        {
                            break;
                        }
                        else _other = null;
                    }
                }
            }
            else _other = null;
            if (_other == null) followEntityAlive = null;
            else if (_other != followEntityAlive) followEntityAlive = _other;
            _other = null;
            if ((UnityEngine.Object)followEntityAlive == (UnityEngine.Object)null)
            {
                //DoDebug(string.Format("NO PLAYER FOUND TO FOLLOW"));
                if (State == AnimalState.AskPlay || State == AnimalState.ReturnToy || State == AnimalState.AskFood)
                {
                    State = AnimalState.Wander; // cancels states
                    NextAskCheck = DateTime.Now.AddSeconds(120);
                }
                return false;
            }
            // if owner is holding a whistle, check if it has any pending order.
            if ((followEntityAlive as EntityPlayer).inventory.holdingItem.GetItemName().StartsWith("DogWhistle"))
            {
                // check sound played
                int orderID = (followEntityAlive as EntityPlayer).inventory.holdingItemItemValue.Meta;
                (followEntityAlive as EntityPlayer).inventory.holdingItemItemValue.Meta = 0;
                // if the dog is not wandering it ignores orders (gotoXXX, eating, drinking)
                if (orderID > 0 && !IsBaby && (State == AnimalState.Wander || State == AnimalState.FollowOwner || State == AnimalState.AttackZed ||
                        State == AnimalState.AttackHuman || State == AnimalState.AttackAnimals))
                {
                    // there's a chance the dog WILL NOT accept the order. It depends on dog loyalty
                    if (animalClass != null)
                    {
                        System.Random Randorder = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
                        int chance =
                            Convert.ToInt32(Math.Floor(Convert.ToDecimal(animalClass.Loyalty/1.25*5 + animalClass.Loyalty)));
                        int roll = Randorder.Next(0, 100);
                        DoDebug(string.Format("DOGSDX -> ORDER WITH LOYALTY = {0}, CHANCE = {1}, ROLL = {2}",
                            animalClass.Loyalty, chance, roll));
                        if (roll < chance)
                        //if (true)
                        {
                            switch (orderID)
                            {
                                case 1:
                                    DoDebug("DOGSDX -> Come to owner");                                    
                                    if (State != AnimalState.GotoOwner)
                                    {
                                        //ClearCustomTasks(2);
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        State = AnimalState.GotoOwner;
                                    }
                                    break;
                                case 2:
                                    DoDebug("DOGSDX -> Follow Owner");
                                    if (State != AnimalState.FollowOwner)
                                    {
                                        //ClearCustomTasks(2);
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        State = AnimalState.FollowOwner;
                                    }
                                    break;
                                case 3:
                                    DoDebug("DOGSDX -> Attack Zeds");
                                    if (State != AnimalState.AttackZed)
                                    {
                                        //ClearCustomTasks(2);
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        State = AnimalState.AttackZed;
                                    }
                                    break;
                                case 4:
                                    DoDebug("DOGSDX -> Attack Humans");
                                    if (State != AnimalState.AttackHuman)
                                    {
                                        //ClearCustomTasks(2);
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        State = AnimalState.AttackHuman;
                                    }
                                    break;
                                case 5:
                                    DoDebug("DOGSDX -> Attack Animals");
                                    if (State != AnimalState.AttackAnimals)
                                    {
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        State = AnimalState.AttackAnimals;
                                    }
                                    break;
                                case 6:
                                    DoDebug("DOGSDX -> Stay");
                                    if (State != AnimalState.Wander)
                                    {
                                        //ClearCustomTasks(2);
                                        this.SetAttackTarget((EntityAlive)null, 0);
                                        DummyLocation = Vector3.zero;
                                        // disown pet (to sell for example)
                                        animalClass.Owner = "";
                                        SaveAnimalFile(id0, false, animalClass);
                                        State = AnimalState.Wander;
                                    }
                                    break;
                                default:
                                    DoDebug("DOGSDX -> Unknown Order");
                                    break;
                            }
                        }
                    }
                }
            }
            float dist = this.GetDistanceSq((Entity) followEntityAlive);
            if (dist < 5.0F)
            {
                //DoDebug(string.Format("ALREADY CLOSE"));
                if (State == AnimalState.GotoOwner)
                {
                    this.getNavigator().clearPathEntity();
                    State = AnimalState.Wander;
                    DoDebug(string.Format("GOT TO OWNER"));
                    PlayOneShot(this.SoundBark);
                    PlayOneShot(this.SoundBark);
                }
                else if (State == AnimalState.ReturnToy)
                {
                    NextAskCheck = DateTime.Now.AddSeconds(120); // wont ask again for the next 120s                    
                    this.getNavigator().clearPathEntity();
                    State = AnimalState.Wander;
                    DoDebug(string.Format("RETURN TOY"));
                    // add toy to owner inventory
                    PlayOneShot(this.SoundBark);
                    ItemStack toy = new ItemStack(ItemClass.GetItem("DogToy"), 1);
                    LastDropPosition = this.GetPosition();
                    GameManager.Instance.ItemDropServer(toy, this.GetPosition(), Vector3.zero, Vector3.zero, -1, 999F);
                    if (animalClass != null)
                    {
                        if (animalClass.Happiness < 20) animalClass.Happiness++;
                        if (animalClass.Loyalty < 20) animalClass.Loyalty++; // playing with dog also improves dog loyalty
                    }

                    //followEntityAlive.AddUIHarvestingItem(toy, false);
                    //followEntityAlive.bag.AddItem(toy);                    
                }
                else if (State == AnimalState.AskPlay)
                {
                    DoDebug("DOGSDX -> ASKS PLAYER TO PLAY - " + this.SoundPlay);
                    this.getNavigator().clearPathEntity();
                    NextAskCheck = DateTime.Now.AddSeconds(120); // wont ask again for the next 120s
                    State = AnimalState.Wander;                    
                    PlayOneShot(this.SoundPlay);
                }
                else if (State == AnimalState.AskFood)
                {
                    DoDebug("DOGSDX -> ASKS PLAYER FOR FOOD - " + this.SoundStarve);
                    this.getNavigator().clearPathEntity();
                    NextAskCheck = DateTime.Now.AddSeconds(120); // wont ask again for the next 120s
                    State = AnimalState.Wander;
                    PlayOneShot(this.SoundStarve);
                }
                return false;
            }
            //DoDebug(string.Format("FOUND PLAYER TO FOLLOW"));
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX - ERROR FINDING HUMAN " + ex.Message);
            return false;
        }
        return true;
    }

    private bool FindToyToPlay()
    {
        //DoDebug(string.Format("LOOK FOR TOY"));
        // IF THE POSITION IS LONGER THEN 5f ? Otherwise it will just do what it has to do.
        try
        {
            if (animalClass != null)
            {
                uint min = 5;
                if (FoodLevelL[1] < min) min = FoodLevelL[1];
                if (FoodLevelL[0] > min) min = FoodLevelL[0];
                if (animalClass.FoodLevel < min && State != AnimalState.ReturnToy)
                {
                    // if it needs food it will look for food instead
                    //DoDebug(string.Format("TOO HUNGRY TO PLAY"));
                    if (State == AnimalState.AskPlay || State == AnimalState.ReturnToy || State == AnimalState.GotoToy ||
                        State == AnimalState.FollowOwner || State == AnimalState.AttackZed ||
                        State == AnimalState.AttackHuman || State == AnimalState.AttackAnimals)
                    {
                        DoDebug("DOGSDX - Action canceled. Too hungry");
                        this.SetAttackTarget((EntityAlive)null, 0);
                        DummyLocation = Vector3.zero;
                        ClearCustomTasks(2);
                        this.getNavigator().clearPathEntity(); // cancel current action
                        State = AnimalState.Wander;
                    }
                    return false;
                }
            }
            float seeDistance = this.GetSeeDistance();
            int toyItemID = ItemClass.GetItem("DogToy").type;
            using (
                List<Entity>.Enumerator enumerator =
                    this.world.GetEntitiesInBounds(typeof(EntityItem),
                        BoundsUtils.BoundsForMinMax(this.position.x - seeDistance, this.position.y - seeDistance,
                            this.position.z - seeDistance, this.position.x + seeDistance, this.position.y + seeDistance,
                            this.position.z + seeDistance)).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    EntityItem _other = enumerator.Current as EntityItem;
                    if (_other.itemStack.itemValue.type == toyItemID)
                    {
                        toyToPlay = _other;
                        break;
                    }
                }
            }
            if ((UnityEngine.Object)toyToPlay == (UnityEngine.Object)null)
            {
               // DoDebug(string.Format("NO TOY FOUND TO FOLLOW"));
                return false;
            }
            float dist = this.GetDistanceSq(LastDropPosition);
            if (dist < 5.0F)
            {
                return false; // too close from where he dropped it last time
            }
            //float dist = Vector3.Distance(GetPosition(), toyToPlay.GetPosition());
            dist = this.GetDistance((Entity)toyToPlay);
            if (dist < 5.0f)
            {               
                DoDebug(string.Format("FOUND TOY, RETURN TO PLAYER"));
                this.getNavigator().clearPathEntity(); // cancel current action
                // pickup toy and return it to owner
                toyToPlay.MarkToUnload();                
                State = AnimalState.ReturnToy;
                return false;
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX - ERROR FINDING TOY " + ex.Message);
            return false;
        }
        return true;
    }

    protected override void updateTasks()
    {
        int pos = 0;
        // if its going for food/water of eating, it wont do anything else
        if (IsTamed)
        {
            if (State != AnimalState.GotoFood && State != AnimalState.GotoWater && State != AnimalState.Eating)
            {
                try
                {
                    //if (!Steam.Network.IsServer)
                    if (this.world.IsRemote())
                    {
                        //this.setMoveForwardWithModifier(0.0f, false);
                        // doesn't do anything locally
                        //DoDebug("UpdateTasks overrided locally");
                    }
                    else
                    {
                        pos = 1;
                        uint min = 5;
                        uint max = 7;
                        int _daysUntil7 = 7 - GameUtils.WorldTimeToDays(GameManager.Instance.World.GetWorldTime())%7;
                        //DoDebug("DAYS UNTIL DAY 7 = " + _daysUntil7);
                        if (_daysUntil7 == 7 && !this.world.IsDaytime() && NextHowl < DateTime.Now && !IsBaby &&
                            SoundHowl != "")
                        {
                            if (State == AnimalState.Wander || State == AnimalState.FollowOwner)
                            {
                                // howl
                                //DoDebug("HOWLING");
                                PlayOneShot(SoundHowl);
                                NextHowl = DateTime.Now.AddSeconds(120);
                            }
                        }
                        if (animalClass != null)
                        {
                            if (animalClass.Loyalty == null) animalClass.Loyalty = 0;
                            if (animalClass.Happiness == null) animalClass.Happiness = 0;
                            if (animalClass.Loyalty != null && animalClass.Happiness != null)
                            {
                                pos = 2;
                                // FOR DOGS, loyalty only influences the hability to follow orders
                                // it does NOT make it blindly follow owner
                                min = 5;
                                if (FoodLevelL[1] < min) min = FoodLevelL[1];
                                if (FoodLevelL[0] > min) min = FoodLevelL[0];
                                if (animalClass.FoodLevel < min && foodItem != "")
                                {
                                    // dog starts asking for food
                                    if (NextAskCheck < DateTime.Now)
                                    {
                                        State = AnimalState.AskFood;
                                    }
                                }
                                bool doActions = true;
                                if (State != AnimalState.AskFood)
                                    doActions = !FindTarget(); // only if it isn't attacking anything.
                                if (doActions || State == AnimalState.AskFood)
                                {
                                    pos = 3;
                                    // the animal does not run away from humans any more
                                    // but also does not move to them
                                    // if the dog is on attack mode, it will first check for targets.
                                    // it goes for the first target it can find 
                                    pos = 4;
                                    // dogs are almost always ready to play
                                    if (animalClass.Happiness < 17 && NextFollowCheck < DateTime.Now &&
                                        (State == AnimalState.Wander || State == AnimalState.GotoToy))
                                    {
                                        if (FindToyToPlay())
                                        {
                                            min = 5;
                                            if (FoodLevelL[1] < min) min = FoodLevelL[1];
                                            if (FoodLevelL[0] > min) min = FoodLevelL[0];
                                            if (animalClass.FoodLevel >= min)
                                            {
                                                State = AnimalState.GotoToy;
                                                Legacy.PathFinderThread.Instance.FindPath(this, toyToPlay.GetPosition(),
                                                    this.GetApproachSpeed(),
                                                    (EAIBase) null);
                                                NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                                            }
                                        }
                                        else
                                        {
                                            //this.SetInvestigatePosition(Vector3.zero, 0);
                                            // asks to play if well fed and happiness getting too low
                                            min = 5;
                                            max = 7;
                                            if (FoodLevelL[1] < max) max = FoodLevelL[1];
                                            if (FoodLevelL[0] > max) max = FoodLevelL[0];
                                            if (HappinessL[1] < min) min = HappinessL[1];
                                            if (HappinessL[0] > min) min = HappinessL[0];
                                            if (animalClass.FoodLevel >= max && NextAskCheck < DateTime.Now &&
                                                animalClass.Happiness < min)
                                            {
                                                DoDebug("DOGSDX - Look for owner to play");
                                                State = AnimalState.AskPlay;
                                            }
                                        }
                                    }
                                    // it will also follow owner if on attack mode? or on attack mode it just tracks and attacks
                                    if ((State == AnimalState.ReturnToy ||
                                         State == AnimalState.AskPlay || State == AnimalState.GotoOwner ||
                                         State == AnimalState.FollowOwner || State == AnimalState.AskFood) &&
                                        NextFollowCheck < DateTime.Now)
                                    {
                                        pos = 5;
                                        if (FindOwnerToFollow())
                                        {
                                            pos = 6;
                                            Legacy.PathFinderThread.Instance.FindPath(this,
                                                followEntityAlive.GetPosition(),
                                                this.GetApproachSpeed(),
                                                (EAIBase) null);
                                            NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                                        }
                                    }
                                    else FindOwnerToFollow(); // just check for orders.
                                }
                                else FindOwnerToFollow(); // just check for orders.
                            }
                        }
                        if (this.OA != null && this.IO != null)
                        {
                            pos = 11;
                            if (this.OA.Tasks.Count > 0)
                            {
                                //DoDebug("DOGSDX - HAS CUSTOM TASKS");
                                pos = 12;
                                using (ProfilerUsingFactory.Profile("entities.live.ai.tasks"))
                                {
                                    pos = 13;
                                    this.OA.OnUpdateTasks();
                                    if (this.IO.Tasks.Count > 0) this.IO.OnUpdateTasks();
                                    pos = 14;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DoDebug("DOGSDX - ERROR " + pos + " updateTasks: " + ex.Message);
                }
                if (NextTaskCycle >= DateTime.Now)
                    return; //its on delay to resume vanilla tasks like wander                    
            }
        }
        base.updateTasks();
    }

    private void ClearCustomTasks(int taskType)
    {        
        if (this.OA != null)
            if (this.OA.Tasks.Count > 0)
            {
                if (taskType == 0)
                {
                    // if it has a runaway task clears it
                    // we dont want the animal to run from humans.
                    EAIRunAway[] tasks = this.GetLocalTasks<EAIRunAway>();
                    if (tasks != null)
                        if (tasks.Length > 0) this.OA.Tasks.Clear();
                }
                else if (taskType == 1)
                {
                    // we dont want the animal to follow anything
                    EAIApproachSpot[] tasks = this.GetLocalTasks<EAIApproachSpot>();
                    if (tasks != null)
                        if (tasks.Length > 0) this.OA.Tasks.Clear();
                }
                else if (taskType == 2)
                {
                    // we dont want the animal to follow anything
                    EAIApproachAndAttackTarget[] tasks = this.GetLocalTasks<EAIApproachAndAttackTarget>();
                    if (tasks != null)
                        if (tasks.Length > 0) this.OA.Tasks.Clear();
                    if (IO.Tasks.Count > 0) this.IO.Tasks.Clear();
                    this.SetAttackTarget((EntityAlive)null, 0); // cancel current target
                    DummyLocation = Vector3.zero;
                }
            }
    }

    private bool SearchAndAttackTarget()
    {
        if (State != AnimalState.AttackZed && State != AnimalState.AttackAnimals && State != AnimalState.AttackHuman)
            return false;
        try
        {
            float distanceMult = 1;
            if (animalClass != null)
            {
                if (State == AnimalState.AttackZed) distanceMult = 1 + (animalClass.attackDog/30);
                else if (State == AnimalState.AttackHuman) distanceMult = 1 + (animalClass.militaryDog/30);
                else if (State == AnimalState.AttackAnimals) distanceMult = 1 + (animalClass.hunterDog/30);
            }
            float maxDistance = this.GetSeeDistance();
            maxDistance = maxDistance*distanceMult; // increases maxDistance according to skill
            EntityAlive targetEntity = this.GetAttackTarget();
            if ((UnityEngine.Object) targetEntity == (UnityEngine.Object) null)
            {
                // if the entity is null, tries to find a new target
                if (State == AnimalState.AttackZed)
                {
                    using (
                        List<Entity>.Enumerator enumerator =
                            this.world.GetEntitiesInBounds(typeof (EntityZombie),
                                BoundsUtils.BoundsForMinMax(this.position.x - maxDistance, this.position.y - maxDistance,
                                    this.position.z - maxDistance, this.position.x + maxDistance,
                                    this.position.y + maxDistance,
                                    this.position.z + maxDistance)).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            EntityAlive _other = enumerator.Current as EntityAlive;
                            if (_other.IsAlive())
                            {
                                targetEntity = _other;
                                DoDebug("DOGSDX - found new zombie");
                                break;
                            }
                        }
                    }                    
                }
                else if (State == AnimalState.AttackAnimals)
                {
                    using (
                        List<Entity>.Enumerator enumerator =
                            this.world.GetEntitiesInBounds(typeof (EntityAnimal),
                                BoundsUtils.BoundsForMinMax(this.position.x - maxDistance, this.position.y - maxDistance,
                                    this.position.z - maxDistance, this.position.x + maxDistance,
                                    this.position.y + maxDistance,
                                    this.position.z + maxDistance)).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {                            
                            EntityAlive _other = enumerator.Current as EntityAlive;
                            if (_other.IsAlive())
                            {
                                targetEntity = _other;
                                DoDebug("DOGSDX - found new animal");
                                break;
                            }
                        }
                    }                    
                }
                else if (State == AnimalState.AttackHuman)
                {
                    using (
                        List<Entity>.Enumerator enumerator =
                            this.world.GetEntitiesInBounds(typeof (EntityPlayer),
                                BoundsUtils.BoundsForMinMax(this.position.x - maxDistance, this.position.y - maxDistance,
                                    this.position.z - maxDistance, this.position.x + maxDistance,
                                    this.position.y + maxDistance,
                                    this.position.z + maxDistance)).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            EntityAlive _other = enumerator.Current as EntityAlive;
                            // check if it is owner or friend of owner
                            PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
                            bool isOwner = false;
                            bool isOwnerFriend = false;
                            if (animalClass != null)
                            {
                                if (animalClass.Owner != "")
                                {
                                    if (persistentPlayerList.GetPlayerDataFromEntityID(_other.entityId).PlayerId ==
                                        animalClass.Owner) isOwner = true;
                                    EntityPlayer entityOwner = (EntityPlayer) GameManager.Instance.World.GetEntity(
                                        persistentPlayerList.GetPlayerData(animalClass.Owner).EntityId);
                                    if (entityOwner.IsFriendsWith((EntityPlayer) _other)) isOwnerFriend = true;
                                }
                            }
                            // only targets a human IF it is not the owner and not a friend of the owner
                            if (!isOwner && !isOwnerFriend)                            
                            {
                                if (_other.IsAlive())
                                {
                                    bool canSelect = true;
                                    if (targetEntity.IsCrouching)
                                    {
                                        // there's a chance the dog will NOT detect the target depending on dog skill
                                        // well trained dogs, will almost always detect targets
                                        uint chance = 0;
                                        if (State == AnimalState.AttackAnimals) chance = animalClass.hunterDog;
                                        else if (State == AnimalState.AttackHuman) chance = animalClass.militaryDog;
                                        else if (State == AnimalState.AttackZed) chance = animalClass.attackDog;
                                        chance =
                                                Convert.ToUInt32(Math.Floor(Convert.ToDecimal(chance / 1.25 * 5 + chance)));
                                        if (this.GetRandom().Next(0, 100) >= chance)
                                        {
                                            //failed detecting crouching entity moves to next one.  
                                            DoDebug("DOGSDX - missed a crouching player");
                                            canSelect = false;
                                        }
                                    }
                                    if (canSelect)
                                    {
                                        targetEntity = _other;
                                        DoDebug("DOGSDX - found new player");
                                        break;
                                    }
                                }
                            }
                        }
                    }                    
                }
            }
            bool hasDummy = false;            
            if ((UnityEngine.Object) targetEntity == (UnityEngine.Object) null)
            {
                // look for dummy blocks
                if (State == AnimalState.AttackZed)
                {
                    if (FindDummy(zombieBlock)) hasDummy = true;
                }
                else if (State == AnimalState.AttackHuman)
                {
                    if (FindDummy(humanBlock)) hasDummy = true;
                }
                else if (State == AnimalState.AttackAnimals)
                {
                    if (FindDummy(animalBlock)) hasDummy = true;
                }
            }
            if ((UnityEngine.Object) targetEntity != (UnityEngine.Object) null || hasDummy)
            {
                // check if it's alive and check distance
                Vector3 targetPos = Vector3.zero;
                if ((UnityEngine.Object) targetEntity != (UnityEngine.Object) null)
                {
                    targetPos = targetEntity.GetPosition();
                    if (targetEntity.IsDead())
                    {
                        targetEntity = null;
                        this.SetAttackTarget((EntityAlive)null, 0);
                        //it will proceed to follow the owner.
                        DoDebug("DOGSDX - no more target (died)");
                        return false;
                    }
                    if (!targetEntity.boundingBox.Intersects(BoundsUtils.BoundsForMinMax(this.position.x - maxDistance, this.position.y - maxDistance,
                                    this.position.z - maxDistance, this.position.x + maxDistance,
                                    this.position.y + maxDistance,
                                    this.position.z + maxDistance)))
                    {
                        targetEntity = null;
                        this.SetAttackTarget((EntityAlive)null, 0);
                        DummyLocation = Vector3.zero;
                        //it will proceed to follow the owner.
                        DoDebug("DOGSDX - no more target (too far)");
                        return false;
                    }
                }
                else targetPos = DummyLocation;
                float distanceToTarget = this.GetDistanceSq(targetPos);
                if (true)
                {
                    if ((UnityEngine.Object) targetEntity != (UnityEngine.Object) null)
                    {
                        if (this.GetAttackTarget() != targetEntity) this.SetAttackTarget(targetEntity, 60);
                    }
                    if (distanceToTarget > this.GetSeeDistance())
                    {
                        // moves to it at wander speed, and makes tracking noise - OVERRIDES ANY OTHER MOVEMENT.
                        //DoDebug("DOGSDX - SOMETHING IS AROUND");
                        if (NextFollowCheck < DateTime.Now)
                        {
                            if (this.GetRandom().Next(0, 100) > 50)
                                PlayOneShot(GetSoundSense());
                            Legacy.PathFinderThread.Instance.FindPath(this, targetPos,
                                this.GetWanderSpeed(),
                                (EAIBase) null);
                            NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                        }
                        return true;
                    }
                    else
                    {
                        // check if its in attack range
                        ItemAction itemAction = this.inventory.holdingItem.Actions[0];                       
                        float num1 = true
                            ? (itemAction == null ? float.MaxValue : itemAction.Range*itemAction.Range)
                            : 1.2f;
                        float num2 = num1*0.5f;
                        if ((double) distanceToTarget > (double) num1)
                        {
                            // move to target at approach speed
                            // growls!
                            //DoDebug("DOGSDX - ITs CLOSE");
                            if (NextFollowCheck < DateTime.Now)
                            {
                                if (this.GetRandom().Next(0, 100) > 50)
                                    PlayOneShot(GetSoundAlert());
                                Legacy.PathFinderThread.Instance.FindPath(this, targetPos,
                                    this.GetApproachSpeed(),
                                    (EAIBase) null);
                                NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                            }
                            return true;
                        }
                        else
                        {
                            // attack target...                            
                            Vector3 _pos = Vector3.zero;
                            bool canSee = this.CanSee(targetPos);
                            Vector3 lookPosAux = Vector3.zero;
                            if ((UnityEngine.Object) targetEntity != (UnityEngine.Object) null)
                            {
                                this.IsBreakingBlocks = false;
                                lookPosAux = targetEntity.getHeadPosition();
                                _pos = new Vector3(targetPos.x, targetEntity.boundingBox.min.y, targetPos.z);
                            }
                            else
                            {
                                lookPosAux = DummyLocation;
                                _pos = new Vector3(targetPos.x, DummyLocation.y, targetPos.z);
                                this.IsBreakingBlocks = true;
                            }
                            //this.SetLookPosition(!canSee ? Vector3.zero : targetEntity.getHeadPosition());
                            this.SetLookPosition(!canSee ? Vector3.zero : lookPosAux);
                            if (!this.bodyDamage.HasNoArmsAndLegs)
                                this.RotateTo(_pos.x, _pos.y, _pos.z, 30f, 30f);


                            AttackTimeout = Utils.FastMax(AttackTimeout - 1, 0);
                            if (AttackTimeout > 0 || !this.Attack(false))
                                return true; // its attacking but on attacktimout
                            AttackTimeout = this.GetAttackTimeoutTicks();
                            // adjust the damage by skill
                            //DoDebug("DOGSDX - ATTACK ATTACK!");
                            (this.inventory.holdingItem.Actions[0] as ItemActionMeleeDog).SetDmgMultiplier(CalcDmgMult());
                            this.Attack(true);
                            // maximum of 50% of skilling up
                            int chance = Convert.ToInt32(Math.Floor(Convert.ToDecimal(animalClass.Inteligence / 2 * 3 + animalClass.Inteligence)));
                            if (chance < 5) chance = 5; // minumum of 5%
                            if (this.GetRandom().Next(0, 100) < chance)
                            {
                                // stats only improve if the total is under 30
                                if ((animalClass.hunterDog + animalClass.militaryDog + animalClass.breederDog +
                                     animalClass.attackDog) < 30)
                                {
                                    //chance to improve stat.
                                    //the happier and more loyal a dog is, the greater the chance
                                    // there's a bare minimum for dog training of 25%
                                    chance = (int)animalClass.Loyalty + (int)animalClass.Happiness +
                                                  this.GetRandom().Next(25, 40);
                                    if (this.GetRandom().Next(0, 100) <= chance)
                                    {
                                        if (animalClass.Owner != "")
                                        {
                                            SendMessage("Your dog skilled up", animalClass.Owner, false);
                                        }
                                        if (State == AnimalState.AttackAnimals && animalClass.hunterDog < hunterDogL[1])
                                            animalClass.hunterDog++;
                                        else if (State == AnimalState.AttackHuman && animalClass.militaryDog < militaryDogL[1])
                                            animalClass.militaryDog++;
                                        else if (State == AnimalState.AttackZed && animalClass.attackDog < attackDogL[1])
                                            animalClass.attackDog++;
                                    }
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            //Debug.Log("DOGSDX -> NO TARGET ENTITY FOUND");
            // it will proceed to follow the owner
            return false;
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR ON FINDANDATTACK - " + ex.Message);
            return false;
        }
    }

    private float CalcDmgMult()
    {
        float mult = 0.1F;
        try
        {            
            if (State == AnimalState.AttackAnimals) mult = (float)animalClass.hunterDog;
            else if (State == AnimalState.AttackHuman) mult = (float)animalClass.militaryDog;
            else if (State == AnimalState.AttackZed) mult = (float)animalClass.attackDog;
            mult = mult / 10;
            float str = (float) animalClass.Strenght;
            if (str == 0) str = 1;
            mult = mult*(str/10);
            if (mult == 0) mult = 0.1F;
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR ON CalcDmgMult - " + ex.Message);
            mult = 0.1F;
        }         
        return mult;
    }
    // dogs will only attack dummies, if they are near. They will not sense far dummies
    private bool FindDummy(string dummy)
    {
        // always resets FoodLocation - no need to store it.
        if (DummyLocation != Vector3.zero)
        {
            BlockValue dummyAux = GameManager.Instance.World.GetBlock(new Vector3i(DummyLocation));
            if (Block.list[dummyAux.type].GetBlockName() == dummy)
            {
                if (Vector3.Distance(this.GetPosition(), DummyLocation) < this.GetSeeDistance())
                    return true;
            }
        }
        DummyLocation = this.GetPosition();
        Vector3i pos = new Vector3i(this.GetPosition());
        if (!FindDummy(pos, dummy))
            if (!FindDummy(pos + new Vector3i(-16, 0, -16), dummy))
                if (!FindDummy(pos + new Vector3i(0, 0, -16), dummy))
                    if (!FindDummy(pos + new Vector3i(16, 0, -16), dummy))
                        if (!FindDummy(pos + new Vector3i(-16, 0, 0), dummy))
                            if (!FindDummy(pos + new Vector3i(0, 0, 16), dummy))
                                if (!FindDummy(pos + new Vector3i(-16, 0, 16), dummy))
                                    if (!FindDummy(pos + new Vector3i(0, 0, 16), dummy))
                                        if (!FindDummy(pos + new Vector3i(16, 0, 16), dummy))
                                        {
                                            DummyLocation = Vector3.zero;
                                            return false;
                                        }
        return true;
    }

    private bool FindDummy(Vector3i pos, string dummy)
    {
        IChunk chunk = GameManager.Instance.World.GetChunkFromWorldPos(pos);

        for (int y = pos.y - 3; y < pos.y + 4; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    BlockValue b = chunk.GetBlock(x, y, z);

                    if (Block.list[b.type].GetBlockName() == dummy)
                    {
                        Vector3i wpos = chunk.GetWorldPos();

                        Vector3 dummyPos = new Vector3(wpos.x + x, y, wpos.z + z);
                        if (Vector3.Distance(this.GetPosition(), dummyPos) < this.GetSeeDistance())
                        {
                            DummyLocation = dummyPos;
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    #endregion;


    public bool IsBedClaimed(Vector3i pos)
    {
        try
        {
            foreach (Vector3i v in FoodClaims.Keys)
            {
                if (v.x == pos.x && v.y == pos.y && v.z == pos.z)
                {
                    int entID = FoodClaims[pos];

                    Entity ent = GameManager.Instance.World.GetEntity(entID);

                    if (ent == null)
                    {
                        FoodClaims.Remove(v);
                        return false;
                    }
                    if (ent.IsAlive())
                        return true;

                    FoodClaims.Remove(v);
                    return false;

                }
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR CLAIMING FOOD " + ex.Message);
        }
        return false;
    }

    public string GetSavePath()
    {
        string path = GameUtils.GetSaveGameDir() + "/MorteAnimals/";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        return path;
    }

    private bool DisownFood()
    {
        try
        {
            Vector3i pos = new Vector3i(FoodLocation);

            foreach (Vector3i v in FoodClaims.Keys)
            {
                if (v.x == pos.x && v.y == pos.y && v.z == pos.z)
                {
                    int entID = FoodClaims[pos];

                    if (entID == this.entityId)
                    {
                        FoodClaims.Remove(v);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR DisownFood " + ex.Message);
        }
        return false;
    }

    /// <summary>
    /// I still have to claim food, cause i don't want 2 or more animals accessing the same container at the same time.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private bool ClaimFood(Vector3i pos)
    {
        try
        {
            if (!IsBedClaimed(pos))
            {
                FoodClaims.Add(pos, this.entityId);
                return true;
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR CLAIMING FOOD " + ex.Message);
        }
        return false;
    }

    #region Binary IO;
    public override void Write(BinaryWriter _bw)
    {
        DoDebug(string.Format("HUSBANDRY -> WRITTING with id={0}, entityID={1}", id0, this.entityId));
        int pos = 0;
        base.Write(_bw);
        try
        {
            //        _bw.Write((int)State);            
            _bw.Write(IsTamed); pos = 1;
            _bw.Write(IsMale); pos = 2;
            _bw.Write(animalClass.IsPregnant); pos = 3;
            _bw.Write(animalClass.BirthTime); pos = 4;
            _bw.Write(animalClass.offspring); pos = 5;
            _bw.Write(animalClass.NextMating); pos = 6;
            _bw.Write(animalClass.NextFoodDecay); pos = 7;
            _bw.Write(animalClass.FoodLevel); pos = 8;
            _bw.Write(animalClass.Happiness); pos = 9;
            _bw.Write(animalClass.BabyUID); pos = 10;
            // DOG ATTRIBUTES
            _bw.Write(animalClass.Loyalty); pos = 11;
            _bw.Write(animalClass.Inteligence); pos = 12;
            _bw.Write(animalClass.Strenght); pos = 13;
            _bw.Write(animalClass.Stamina); pos = 14;
            // DOG HABILITIES
            _bw.Write(animalClass.attackDog); pos = 15;
            _bw.Write(animalClass.militaryDog); pos = 16;
            _bw.Write(animalClass.hunterDog); pos = 17;
            _bw.Write(animalClass.breederDog); pos = 18;
            // last food and water location - ignore if too far.
            _bw.Write(FoodLocation.x); pos = 19;
            _bw.Write(FoodLocation.y); pos = 20;
            _bw.Write(FoodLocation.z); pos = 21;
            _bw.Write(0.0F); pos = 22;
            _bw.Write(0.0F); pos = 23;
            _bw.Write(0.0F); pos = 24;
            _bw.Write(id0); pos = 25;
            _bw.Write(id1); pos = 26;
            _bw.Write(id2); pos = 27;
            if (animalClass.Owner == null) animalClass.Owner = "";
            _bw.Write(animalClass.Owner); pos = 28;
            if (id0Prev != id0)
            {
                //DoDebug("HUSBANDRY -> FORCE UPDATE CLIENTS");
                //GameManager.Instance.World.entityDistributer.SendFullUpdateNextTick((Entity)this);
                //GameManager.Instance.World.entityDistributer.Add((Entity)this);
                id0Prev = id0;
            }
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("DOGSDX -> ERRO WRITING (pos: {1}): {0}", ex.Message, pos));
        }
    }

    public override void Read(byte _version, BinaryReader _br)
    {
        DoDebug(string.Format("DOGSDX -> READING with id={0}, playerid={1}", id0, belongsPlayerId));
        base.Read(_version, _br);
        if (_br.BaseStream.Position == _br.BaseStream.Length)
            return; //probably a vanilla chicken so just return.
        try
        {
            IsTamed = _br.ReadBoolean();
            IsMale = _br.ReadBoolean();
            animalClass.IsPregnant = _br.ReadBoolean();
            animalClass.BirthTime = (ulong) _br.ReadInt64();
            animalClass.offspring = (ulong) _br.ReadInt64();
            animalClass.NextMating = (ulong) _br.ReadInt64();
            animalClass.NextFoodDecay = (ulong) _br.ReadInt64();
            animalClass.FoodLevel = (uint) _br.ReadInt32();
            animalClass.Happiness = (uint) _br.ReadInt32();
            animalClass.BabyUID = _br.ReadInt32();
            // DOG ATTRIBUTES
            animalClass.Loyalty = (uint) _br.ReadInt32();
            animalClass.Inteligence = (uint) _br.ReadInt32();
            animalClass.Strenght = (uint) _br.ReadInt32();
            animalClass.Stamina = (uint) _br.ReadInt32();
            // DOG HABILITIES
            animalClass.attackDog = (uint) _br.ReadInt32();
            animalClass.militaryDog = (uint) _br.ReadInt32();
            animalClass.hunterDog = (uint) _br.ReadInt32();
            animalClass.breederDog = (uint) _br.ReadInt32();
            // positions and ids
            FoodLocation = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            Vector3 reserved = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            id0 = _br.ReadInt32();
            id1 = _br.ReadInt32();
            id2 = _br.ReadInt32();
            animalClass.Owner = _br.ReadString();
            DoDebug(string.Format("DOGSDX -> READING with id={0}, playerid={1}", id0, belongsPlayerId));
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERRO READING: " + ex.Message);
            animalClass.FoodLevel = 10;
        }
        if (world.IsRemote()) return; // do nothing more if the world is remote
        if (idInit != id0 && id0 != 0)
        {
            if (IsTamed)
            {
                LoadFile(id0);
                DeleteAnimalFile(id0);
                SaveAnimalFile(idInit, true, animalClass);
            }
            id0 = idInit;
        }
        else if (id0 == 0 && idInit > 0) id0 = idInit;

        if (IsBaby)
            if (animalClass.BirthTime + Adolesence < GameManager.Instance.World.worldTime)
            {
                State = AnimalState.OffSpring;
                return;
            }
    }

    #endregion;

    #region Animal File Handling;
    private void SaveAnimalFile(int idAux, bool saveOverride, clsDog dogClass)
    {
        //DoDebug("SAVING FILE");
        if ((!IsAlive() || !IsTamed) && !saveOverride) return;
        if (this.world.IsRemote()) return;
        try
        {            
            string path = GetSavePath();
            //DoDebug("SAVING FILE PATH: " + path);
            if (!System.IO.Directory.Exists(path))
            {
                return;
            }
            DoDebug(string.Format("DOGSDX -> SAVING with id={0}", idAux));
            if (idAux > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("");
                sb.Append(dogClass.AnimalName + Environment.NewLine);
                sb.Append(dogClass.IsPregnant.ToString() + Environment.NewLine);
                sb.Append(dogClass.BirthTime + Environment.NewLine);
                sb.Append(dogClass.offspring + Environment.NewLine);
                sb.Append(dogClass.NextMating + Environment.NewLine);
                sb.Append(dogClass.NextFoodDecay + Environment.NewLine);
                sb.Append(dogClass.FoodLevel + Environment.NewLine);
                sb.Append(dogClass.Happiness + Environment.NewLine);
                sb.Append(dogClass.BabyUID + Environment.NewLine);
                // dog statistics
                sb.Append(dogClass.Loyalty + Environment.NewLine);
                sb.Append(dogClass.Inteligence + Environment.NewLine);
                sb.Append(dogClass.Stamina + Environment.NewLine);
                sb.Append(dogClass.Strenght + Environment.NewLine);
                // dog habilities
                sb.Append(dogClass.attackDog + Environment.NewLine);
                sb.Append(dogClass.breederDog + Environment.NewLine);
                sb.Append(dogClass.hunterDog + Environment.NewLine);
                sb.Append(dogClass.militaryDog + Environment.NewLine);
                // Current Owner - if in follow mode
                sb.Append(dogClass.Owner + Environment.NewLine);
                sb.Append(this.timeToDie + Environment.NewLine);
                string filename = path + idAux.ToString() + ".dog";
                //DoDebug("FILE: " + filename);
                System.IO.File.WriteAllText(filename, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR SAVING FILE " + ex.Message);
        }
    }

    private bool LoadAnimalFile(int idAux)
    {
        try
        {
            if (this.world.IsRemote()) return false;
            if (!IsTamed) return false;
            string path = GetSavePath();
            string filename = path + idAux + ".dog";
            if (!System.IO.File.Exists(filename))
            {
                return false;
            }
            if (idAux > 0)
            {
                DoDebug(string.Format("DOGSDX -> LOADING with id={0}", idAux));
                string[] lines = System.IO.File.ReadAllLines(filename);
                DoDebug("FILE: " + filename + "lines = " + lines.Length);
                if (lines.Length < 18) return false;
                animalClass.AnimalName = lines[0];
                animalClass.IsPregnant = Convert.ToBoolean(lines[1]);               
                animalClass.BirthTime = Convert.ToUInt64(lines[2]);
                animalClass.offspring = Convert.ToUInt64(lines[3]);
                animalClass.NextMating = Convert.ToUInt64(lines[4]);
                animalClass.NextFoodDecay = Convert.ToUInt64(lines[5]);
                animalClass.FoodLevel = Convert.ToUInt32(lines[6]);
                animalClass.Happiness = Convert.ToUInt32(lines[7]);
                if (lines[0] != "-1")
                {
                    try
                    {
                        animalClass.BabyUID = Convert.ToInt32(lines[8]);
                    }
                    catch (Exception)
                    {
                        animalClass.BabyUID = 0;
                    }
                }
                else animalClass.BabyUID = 0;
                // dog statistics
                animalClass.Loyalty = Convert.ToUInt32(lines[9]);
                animalClass.Inteligence = Convert.ToUInt32(lines[10]);
                animalClass.Stamina = Convert.ToUInt32(lines[11]);
                animalClass.Strenght = Convert.ToUInt32(lines[12]);
                // dog habilities
                animalClass.attackDog = Convert.ToUInt32(lines[13]);
                animalClass.breederDog = Convert.ToUInt32(lines[14]);
                animalClass.hunterDog = Convert.ToUInt32(lines[15]);
                animalClass.militaryDog = Convert.ToUInt32(lines[16]);
                animalClass.Owner = lines[17];
                try
                {
                    timeToDie = Convert.ToUInt64(lines[18]);
                }
                catch (Exception)
                {
                }
                IsTamed = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR LOADING: " + ex.Message);
            //id0 = 0;
            //id1 = 0;
            //id2 = 0;
            //NewDog(ref animalClass);
            //id0 = NewUID();
        }
        return false;
    }    

    private void DeleteAnimalFile(int idAux)
    {
        try
        {
            if (this.world.IsRemote()) return;
            DoDebug(string.Format("DOGSDX -> DELETING FILE"));
            if (idAux > 0)
            {
                string path = GetSavePath();
                string filename = path + idAux + ".dog";
                DoDebug("DOGSDX -> DELETING FILENAME: " + filename);
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

    private void GetUID()
    {
        //if (id0 == 0 || id1 == 0 || id2 == 0)
        if (id0 == 0)
        {
            id0 = NewUID();
            DoDebug("DOGSDX -> NEW ID: " + id0);            
            // creates unique ID                        
            //id0 = Convert.ToInt32(isNow.ToString("yyMM"));
            //id1 = Convert.ToInt32(isNow.ToString("HHmm"));            
            //id2 = Convert.ToInt32(isNow.ToString("ssfff"));
        }
    }

    private int NewUID()
    {
        int newID = 0;
        DateTime isNow = DateTime.UtcNow;
        string calcID = string.Format("{0}{1}", isNow.ToString("yy"), isNow.DayOfYear.ToString("000"));
            //isNow.ToString("yyMMdd")
        string path = GetSavePath();
        string filename = "";
        for (int i = 1; i <= 999; i++)
        {
            filename = string.Format("{0}{1}.dog", calcID, i.ToString("000"));
            if (!System.IO.File.Exists(path + filename))
            {
                newID  = Convert.ToInt32(string.Format("{0}{1}", calcID, i.ToString("000")));
                break;
            }
        }
        return newID;
    }

    private clsDog LoadFile(int idAux)
    {
        clsDog result = new clsDog();
        try
        {
            if (this.world.IsRemote()) return null;
            string path = GetSavePath();
            string filename = path + idAux + ".dog";
            if (!System.IO.File.Exists(filename))
            {
                return null;
            }
            if (idAux > 0)
            {
                DoDebug(string.Format("DOGSDX -> LOADING with id={0}", idAux));
                string[] lines = System.IO.File.ReadAllLines(filename);
                DoDebug("FILE: " + filename + "lines = " + lines.Length);
                if (lines.Length < 18) return null;
                result.AnimalName = lines[0];
                result.IsPregnant = Convert.ToBoolean(lines[1]);
                result.BirthTime = Convert.ToUInt64(lines[2]);
                result.offspring = Convert.ToUInt64(lines[3]);
                result.NextMating = Convert.ToUInt64(lines[4]);
                result.NextFoodDecay = Convert.ToUInt64(lines[5]);
                result.FoodLevel = Convert.ToUInt32(lines[6]);
                result.Happiness = Convert.ToUInt32(lines[7]);
                if (lines[0] != "-1")
                {
                    try
                    {
                        result.BabyUID = Convert.ToInt32(lines[8]);
                    }
                    catch (Exception)
                    {
                        result.BabyUID = 0;
                    }
                }
                else result.BabyUID = 0;
                // dog statistics
                result.Loyalty = Convert.ToUInt32(lines[9]);
                result.Inteligence = Convert.ToUInt32(lines[10]);
                result.Stamina = Convert.ToUInt32(lines[11]);
                result.Strenght = Convert.ToUInt32(lines[12]);
                // dog habilities
                result.attackDog = Convert.ToUInt32(lines[13]);
                result.breederDog = Convert.ToUInt32(lines[14]);
                result.hunterDog = Convert.ToUInt32(lines[15]);
                result.militaryDog = Convert.ToUInt32(lines[16]);
                result.Owner = "";
            }
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR LOADING PARENT FILE: " + ex.Message);
            return null;
        }
        return result;
    }
    #endregion;

    protected override void Awake()
    {
        // this will depend if its rabbit or stag type.
        // if possible to do all on the same place

        //BoxCollider component = this.gameObject.GetComponent<BoxCollider>();
        //if ((component))
        //{
        //    component.center = new Vector3(0.0f, 0.15f, 0.0f);
        //    component.size = new Vector3(0.4f, 0.4f, 0.4f);
        //}                
        base.Awake();
        if (lifetime < float.MaxValue) lifetime = float.MaxValue;
        this.OA = new EAITaskList();
        if (id0 > 0)
        {            
            // read file
            LoadAnimalFile(id0);
        }
        //Transform transform = this.transform.Find("Graphics/BlobShadowProjector");
        //if (!(transform))
        //    return;
        //transform.gameObject.SetActive(false);        
    }

    public override void ProcessDamageResponseLocal(DamageResponse _dmResponse)
    {
        // this should only happen on local game, otherwise should be intercepted
        DoDebug(string.Format("ProcessDamageResponse with type = {0}, Strenght = {1}, EntityID = {2}",
            _dmResponse.Source.GetName(), _dmResponse.Strength, _dmResponse.Source.getEntityId()));
        if (_dmResponse.Source.GetName() == EnumDamageSourceType.Disease)
        {
            DoDebug("Is disease with " + _dmResponse.Strength);
            if (_dmResponse.Strength > 1000 && _dmResponse.Strength < 9999)
            {
                // owner ID
                int ownerID = _dmResponse.Strength - 1000;
                // find the owner entity, if it really exists.
                PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
                PersistentPlayerData plrData = persistentPlayerList.GetPlayerDataFromEntityID(ownerID);
                if (plrData != null)
                {
                    DoDebug("OWNER IS " + plrData.PlayerId);
                    if (animalClass != null)
                    {
                        animalClass.Owner = plrData.PlayerId;
                        SaveAnimalFile(id0, false, animalClass);
                        string msg =
                            string.Format(
                                "Dog Stats:{0}Intel:{1}{0}Str:{2}{0}Zed Hunt:{3}{0}Human Hunt:{4}{0}Animal Hunt:{5}{0}Breeder:{6}{0}Loyalty:{7}{0}Happiness:{8}",
                                Environment.NewLine,
                                animalClass.Inteligence, animalClass.Strenght,
                                animalClass.attackDog, animalClass.militaryDog, animalClass.hunterDog,
                                animalClass.breederDog, animalClass.Loyalty, animalClass.Happiness);
                        if (!IsTamed) msg = "This is a wild dog. You need to tame it before you can claim ownership";
                        // show local message
                        SendMessage(msg, "", true);
                       //GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, msg, "", false, "", false);                       
                    }
                }                
            }
            return;
        }
        base.ProcessDamageResponseLocal(_dmResponse);
    }

    public override void ProcessDamageResponse(DamageResponse _dmResponse)
    {
        //DoDebug(string.Format("ProcessDamageResponse with type = {0}, Strenght = {1}, EntityID = {2}",
        //    _dmResponse.Source.GetName(), _dmResponse.Strength, _dmResponse.Source.getEntityId()));
        if (_dmResponse.Source.GetName() == EnumDamageSourceType.Disease)
        {
            //DoDebug("Is disease with " + _dmResponse.Strength);
            if (_dmResponse.Strength > 1000 && _dmResponse.Strength < 9999)
            {
                // owner ID
                int ownerID = _dmResponse.Strength - 1000;                
                // find the owner entity, if it really exists.
                PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
                PersistentPlayerData plrData = persistentPlayerList.GetPlayerDataFromEntityID(ownerID);
                if (plrData != null)
                {
                    // if the dog is already owned, he wont respond to this...                    
                    //DoDebug("OWNER IS " + plrData.PlayerId);
                    if (animalClass != null)
                    {
                        string msg =
                            string.Format(
                                "Dog Stats:{0}Intel:{1}{0}Str:{2}{0}Zed Hunt:{3}{0}Human Hunt:{4}{0}Animal Hunt:{5}{0}Breeder:{6}{0}Loyalty:{7}{0}Happiness:{8}",
                                Environment.NewLine,
                                animalClass.Inteligence, animalClass.Strenght,
                                animalClass.attackDog, animalClass.militaryDog, animalClass.hunterDog,
                                animalClass.breederDog, animalClass.Loyalty, animalClass.Happiness);
                        if (!IsTamed) msg = "This is a wild dog. You need to tame it before you can claim ownership";
                        else if (plrData.PlayerId != animalClass.Owner && animalClass.Owner != "")
                            msg = "This dog is owned by someone else";
                        else
                        {
                            animalClass.Owner = plrData.PlayerId;
                            SaveAnimalFile(id0, false, animalClass);
                        }
                        // show local message
                        SendMessage(msg, plrData.PlayerId, false);
                    }
                }
            }
            return;
        }
        base.ProcessDamageResponse(_dmResponse);
    }

    private void SendMessage(string msg, string playerSID, bool local)
    {
        try
        {
            msg = string.Format("{0}{1}", MsgColor, msg);            
            if (!local) // if it IS a server, needs to send a net package
            {
                DoDebug("Server Send Message");
                ClientInfo _playerInfo = ConsoleHelper.ParseParamIdOrName(playerSID, true, true);
                NetPackage _package1 =
                    (NetPackage)
                        new NetPackageGameMessage(EnumGameMessages.Chat, msg, "DOGSDX", false, "", false);
                _playerInfo.SendPackage(_package1);
            }
            else if (!this.world.IsRemote()) // playing a local game
            {
                DoDebug("Local Send Message");
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    //GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, msg, "", false, "", false);
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, msg, entity);
                }
            }
        }
        catch (Exception ex)
        {
            DoDebug("Error sending message: " + ex.Message);
        }        
    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {
        // taming strategy - either I keep Hal's one, or loot baby animals while harvesting.

        Entity ent = GameManager.Instance.World.GetEntity(_damageSource.getEntityId());

        if (_damageSource.GetName() == EnumDamageSourceType.Melee && !this.IsDead())
        {
            //_damageSource.GetEntityDamageEquipmentSlot(ent)
            if (ent is EntityPlayer)
            {
                EntityPlayer player = (EntityPlayer)ent;
                // if its tamed but the owner is another player, reduces the success chance by half
                // and the dog goes in attack mode.
                // serves the purpose of making stealing a owned dog harder.
                bool canGetStats = true;
                float chanceMult = 1;
                if (!this.world.IsRemote())
                {
                    if (animalClass != null)
                    {
                        if (animalClass.Owner != "")
                        {
                            // dog is already owned by someone
                            PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
                            if (persistentPlayerList.GetPlayerDataFromEntityID(player.entityId).PlayerId !=
                                animalClass.Owner)
                            {
                                EntityPlayer entityOwner = (EntityPlayer) GameManager.Instance.World.GetEntity(
                                    persistentPlayerList.GetPlayerData(animalClass.Owner).EntityId);
                                if (!entityOwner.IsFriendsWith(player))
                                {
                                    // it's NOT the owner and NOT friendly, put the dog in attackhuman mode
                                    this.State = AnimalState.AttackHuman;
                                    // on this case, it wont be possible to check dog stats.
                                    canGetStats = false;
                                }
                                // drastically reduces success chance - even if its a friend, it will be hard to actually get the dog.
                                chanceMult = 0.3f;
                            }
                        }
                    }
                }
                string itemName = player.inventory.holdingItem.GetItemName();
                DoDebug(string.Format("DOGSDX -> Hit with {0}, tameitem is {1}, harvestitem is {2}", itemName, tameItem, harvestItem));
                if (itemName == "DogCollar" && canGetStats)
                {
                    // send the player ID + 1000 as damage. so if damage>1000 and < 9999 it should be a player.
                    DamageResponse dmg = new DamageResponse();
                    dmg.Strength = player.entityId + 1000;
                    dmg.Source = DamageSource.disease;
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
                    else this.DamageEntity(DamageSource.disease, player.entityId + 1000, false, 1);
                    return 0;
                }
                else if (itemName == tameItem)
                {
                    Skill skill = player.Skills.GetSkillByName(tameSkill);
                    if (skill == null)
                    {

                    }
                    else
                    {
                        int chance = skill.Level;
                        chance = Convert.ToInt32(Math.Floor(chance*chanceMult));
                        int roll = Rand.Next(0, 100);
                        if (roll <= chance)
                        {
                            if (!this.world.IsRemote() && IsTamed)
                            {
                                if (id0 == 0) GetUID(); // no file yet?
                                SaveAnimalFile(id0, false, animalClass);
                            }                                          
                            ItemStack animal = new ItemStack(ItemClass.GetItem(itemTamed), 1);
                            // i have to divide the number in 2 so that I'm able to save it persistently with the item
                            string idAux = id0.ToString("00000000");
                            animal.itemValue.Meta = Convert.ToInt32(idAux.Substring(0, 4));
                            animal.itemValue.Quality = Convert.ToInt32(idAux.Substring(4, 4));
                            animal.itemValue.UseTimes = id0;
                            DoDebug(string.Format("DOGSDX -> ITEM CREATED WITH ID0: {0}, ID1: {1}, ID2: {2}", animal.itemValue.Meta, animal.itemValue.Quality, id2));
                            player.AddUIHarvestingItem(animal, false);
                            player.bag.AddItem(animal);                           
                            WasTamed = true;
                            if (this.world.IsRemote())
                            {
                                //DamageResponse _dmResponse = this.damageEntityLocal(_damageSource, 999, false, player.entityId);
                                //NetPackage _package = (NetPackage)new NetPackageDamageEntity(this.entityId, _dmResponse);
                                //GameManager.Instance.SendToServer(_package);

                                // i just despawn it, but i don't kill it - I don't want the file to be deleted
                                NetPackage _package1 = (NetPackage)new NetPackageEntityRemove(this.entityId, EnumRemoveEntityReason.Despawned);
                                GameManager.Instance.SendToServer(_package1);
                            }
                            else
                            {
                                //this.Kill(new DamageResponse());
                                this.MarkToUnload();
                            }
                            return 999;
                        }
                    }
                }
                else if (itemName == harvestItem && lootItem != "")
                {
                    // harvest depending on skill
                    Skill skill = player.Skills.GetSkillByName(harvestSkill);
                    if (skill == null)
                    {

                    }
                    else
                    {
                        int chance = skill.Level;
                        int roll = Rand.Next(0, 100);
                        if (roll <= chance)
                        {
                            ItemStack lootHarvestd = new ItemStack(ItemClass.GetItem(lootItem), 1);
                            player.AddUIHarvestingItem(lootHarvestd, false);
                            player.inventory.AddItem(lootHarvestd);                            
                            return 0;
                        }
                    }
                }
            }
        }      
        return base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
    }

    static int FoodBlockID = -1;
    static int FoodItemID = -1;
    static int WaterItemID = -1;
    static int EmptyFoodItemID = -1;
    static int EmptyWaterItemID = -1;

    public override void OnEntityDeath()
    {
        DoDebug(string.Format("DOGSDX -> DEATH tamed: {0}", WasTamed));
        if (!WasTamed && !this.world.IsRemote() && id0 > 0)
        {
            // delete the file, if exists
            DeleteAnimalFile(id0);
        }
        base.OnEntityDeath();
    }

    #region Feeding;
    private void GetFoodBlock()
    {        
        FoodItemID = ItemClass.GetItem(foodItem).type;
        if (emptyFoodItem != "")
            EmptyFoodItemID = ItemClass.GetItem(emptyFoodItem).type;
        WaterItemID = ItemClass.GetItem(foodItem).type;
        if (emptyFoodItem != "")
            EmptyWaterItemID = ItemClass.GetItem(emptyFoodItem).type;
        foreach (Block b in Block.list)
        {
            if (b == null)
                continue;

            if (b.GetBlockName() == foodBlock)
            {
                FoodBlockID = b.blockID;
                return;
            }
        }
    }

    private bool FindFood()
    {

        if (FoodBlockID == -1)
            GetFoodBlock();        
        DoDebug(string.Format("DOGSDX -> FoodBlock={0}, FoodItem={1}, WaterItem={2}, FoodBlockID={3}", foodBlock, foodItem, waterItem, FoodBlockID));

        // always resets FoodLocation - no need to store it.
        FoodLocation = this.GetPosition();

        Vector3i pos = new Vector3i(this.GetPosition());

        //BlockValue foodStorage = GameManager.Instance.World.GetBlock(new Vector3i(FoodLocation));
        //if (foodStorage.type == FoodBlockID)
        //    if (Vector3.Distance(this.GetPosition(), FoodLocation) < MaxDistanceToFood)
        //        return true;
            //else
            //    DisownFood();
        
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
                                           DoDebug("DOGSDX-> WTF");
                                            return false;
                                        }


        return true;

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

                    if (b.type == FoodBlockID)
                    {
                        Vector3i wpos = chunk.GetWorldPos();

                        Vector3 foodPos = new Vector3(wpos.x + x, y, wpos.z + z);
                        //if (ClaimFood(new Vector3i(foodPos))) - No claiming of food anymore
                        if (Vector3.Distance(this.GetPosition(), foodPos) < MaxDistanceToFood)
                        {
                            FoodLocation = foodPos;
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    private void IncreaseFood()
    {
        if (animalClass.FoodLevel < FoodLevelL[1])
        {
            animalClass.FoodLevel++;
        }
        else
        {
            // well fed - improves loyalty
            if (animalClass.Loyalty < LoyaltyL[1]) animalClass.Loyalty++;
        }
    }

    private void DecreaseFood()
    {
        if (animalClass.FoodLevel > FoodLevelL[0]) animalClass.FoodLevel--;
        if (animalClass.Happiness > HappinessL[0]) animalClass.Happiness--; // if no food exists, it also decreases happiness
        uint min = 7;
        if (FoodLevelL[1] < min) min = FoodLevelL[1];
        if (FoodLevelL[0] > min) min = FoodLevelL[0];
        if (animalClass.FoodLevel < min && animalClass.Loyalty > 5 && LoyaltyL[0] < animalClass.Loyalty)
            animalClass.Loyalty--; // if you let food go too low, loyalty will start decreasing 
        if (animalClass.FoodLevel == 0) this.DamageEntity(DamageSource.starve, 10, false, 1);
    }

    private bool EatFood(bool water)
    {
        bool result = false;
        Vector3i food = new Vector3i(FoodLocation);
        IChunk chunk = GameManager.Instance.World.GetChunkFromWorldPos(food);
        BlockValue block = GameManager.Instance.World.GetBlock(food);

        if (block.type == FoodBlockID)
        {
            TileEntity te = null;

            for (int _clrIdx = 0; _clrIdx < GameManager.Instance.World.ChunkClusters.Count; ++_clrIdx)
            {
                te = GameManager.Instance.World.GetTileEntity(_clrIdx, food);
                if (te != null)
                    break;
            }

            if (te == null)
            {
                return false;
            }


            if (te.GetTileEntityType() != TileEntityType.Loot)
            {
                return false;
            }

            TileEntityLootContainer inv = (TileEntityLootContainer)te;

            ItemStack[] items = inv.GetItems();

            int itemID = FoodItemID;
            int emptyItemID = EmptyFoodItemID;
            string emptyItem = emptyFoodItem;

            if (water)
            {
                itemID = WaterItemID;
                emptyItemID = EmptyWaterItemID;
                emptyItem = emptyWaterItem;
            }

            bool hasFood = false;
            foreach (ItemStack i in items)
            {
                if (i == null)
                    continue;

                if (i.itemValue.type == itemID && i.count > 0)
                {
                    hasFood = true;
                    break;
                }
            }

            if (!hasFood)
            {
                return false;
            }

            if (hasFood)
            {
                foreach (ItemStack i in items)
                {
                    if (i == null)
                        continue;

                    if (i.itemValue.type == itemID && i.count > 0)
                    {
                        i.count--;                        
                        // add stack of empty object, if it exists
                        if (emptyItemID > -1)
                        {
                            ItemStack emptyStack = new ItemStack(ItemClass.GetItem(emptyItem), 1);
                            inv.AddItem(emptyStack);
                        }
                        inv.SetModified();
                        return true;
                    }
                }
            }
        }
        return result;
    }
    #endregion;
    
    public override void OnUpdateLive()
    {
        base.OnUpdateLive();

        if (this.world.IsRemote()) return;
        if (IsDead() || !IsTamed)
            return;
        try
        {
            if (NextUpdateCheck < DateTime.Now)
            {
                //DoDebug(string.Format("DOGSDX {0}-> UPDATE LIVE CYCLE. WANDERTIME = {1}", id0, WanderTime));
                
                // TODO - verificar se tenho de recalcular sempre isto ou é melhor não.
                Rand = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                //DoDebug("isMale: " + IsMale.ToString() + " state: " + State.ToString());

                if (State == AnimalState.OffSpring && IsBaby)
                {

                    double small = (double) (GameManager.Instance.World.worldTime - animalClass.BirthTime);
                    double large = (double) Adolesence;
                    float percentage = (float) (small/large);
                    percentage = Mathf.Min(1, percentage);

                    percentage = 0.5f + (0.5f*percentage);

                    this.gameObject.transform.localScale = new Vector3(percentage, percentage, percentage); // this will only work on local

                    NextUpdateCheck = DateTime.Now.AddSeconds(BabyCheck);

                    if (percentage > 0.99)
                    {
                        //this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
                        //State = AnimalState.Wander;
                        // SPAWN THE MATURE ANIMAL AND DESTROY THE CURRENT ONE!
                        DoDebug("DOGSDX -> FULLY GROWN");
                        SaveAnimalFile(id0, false, animalClass);
                        SpawnAnimal(this.GetPosition(), false, false);
                    }
                    return;
                }

                EAIRunAway[] tasks = this.GetTasks<EAIRunAway>();

                if (tasks != null)
                {
                    foreach (EAIRunAway t in tasks)
                    {
                        if (CheckRunAway(t))
                        {                                                 
                            return;
                        }
                        else continue;
                    }
                }
                // check local AI tasks too
                tasks = this.GetLocalTasks<EAIRunAway>();
                if (tasks != null)
                {
                    foreach (EAIRunAway t in tasks)
                    {
                        if (CheckRunAway(t))
                        {
                            return;
                        }
                        else continue;
                    }
                }

                int roll = 0;
                if (State == AnimalState.Wander)
                {
                    if (this.IsMale)
                    {
                        //debug = true;
                        DoDebug(
                            String.Format(
                                "HUSBANDRY -> WANDERING. isMale={0}, nextMating={1}, worldTime={2}, isBaby={3}",
                                IsMale.ToString(), animalClass.NextMating, GameManager.Instance.World.worldTime,
                                IsBaby.ToString()));
                        //debug = false;
                    }
                    if (IsMale && !IsBaby)
                    {
                        if (animalClass.NextMating < GameManager.Instance.World.worldTime && entityFemale != "") // some pet are not breedable
                        {
                            #region Mating Logic;

                            // the chance to mate depends on the foodlevel
                            int chance =
                                Convert.ToInt32(
                                    Math.Floor(Convert.ToDecimal(animalClass.breederDog/2*5 + animalClass.breederDog)));
                            if (chance < 10) chance = 15;
                            roll = Rand.Next(0, 100);
                            //debug = true;
                            DoDebug(
                                string.Format(
                                    "DOGSDX -> TRYING TO MATE WITH breederDog = {0}, CHANCE = {1}, ROLL = {2}",
                                    animalClass.breederDog, chance, roll));
                            if (roll < chance)
                                //if (true)
                            {
                                DoDebug("DOGSDX -> Male trying to mate");
                                List<Entity> animals =
                                    GameManager.Instance.World.GetEntitiesInBounds(typeof (EntityDogMorte),
                                        new Bounds(this.GetPosition(),
                                            new Vector3(MaxMatingAreaCheck, 255, MaxMatingAreaCheck)));
                                foreach (Entity entity in animals)
                                {
                                    if (!entity.IsDead())
                                    {
                                        DoDebug("Found: " + (entity as EntityDogMorte).EntityName + " comparing with " + entityFemale + " isPregnate = " + (entity as EntityDogMorte).animalClass.IsPregnant);
                                        //entity.entityId                                 
                                        if ((entity as EntityDogMorte).EntityName == entityFemale &&
                                            !(entity as EntityDogMorte).animalClass.IsPregnant)
                                        {
                                            // gains breeding skill
                                            if ((animalClass.hunterDog + animalClass.militaryDog +
                                                 animalClass.breederDog +
                                                 animalClass.attackDog) < 30)
                                                if (animalClass.breederDog < breederDogL[1]) animalClass.breederDog++;
                                            (entity as EntityDogMorte).Impregnate(id0);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // there's a chance of gaining skill here
                                if (this.GetRandom().Next(0, 100) > 75)
                                {
                                    DoDebug("DOGSDX -> Got skill from effort");
                                    if ((animalClass.hunterDog + animalClass.militaryDog +
                                         animalClass.breederDog +
                                         animalClass.attackDog) < 30)
                                        if (animalClass.breederDog < breederDogL[1]) animalClass.breederDog++;
                                }
                            }
                            //debug = false;
                            animalClass.NextMating = GameManager.Instance.World.worldTime + MatingTime;

                            #endregion;
                        }
                    }
                    if (animalClass.NextFoodDecay < GameManager.Instance.World.worldTime && foodItem != "") // some pets don't need food
                    {
                        #region Feeding Logic;
                        uint min = 15;
                        if (FoodLevelL[1] < min) min = FoodLevelL[1];
                        if (FoodLevelL[0] > min) min = FoodLevelL[0];
                        if (animalClass.FoodLevel <= 15)
                        {
                            // only looks for food if level is getting low                           
                            // 30% chance of needing water instead of food
                            roll = Rand.Next(0, 100);
                            if (FindFood())
                            {
                                //this.SetInvestigatePosition(FoodLocation, 30);
                                this.getNavigator().clearPathEntity();
                                Legacy.PathFinderThread.Instance.FindPath(this, FoodLocation, this.GetWanderSpeed(),
                                    (EAIBase) null);
                                if (roll < 70)
                                    State = AnimalState.GotoFood;
                                else State = AnimalState.GotoWater;
                                NextUpdateCheck = DateTime.Now.AddMilliseconds(500);
                                DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " GOING TO FOOD");
                            }
                            else
                            {
                                DecreaseFood();
                                DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " NO FOOD FOODLEVEL: " +
                                        animalClass.FoodLevel);
                                NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                            }
                        }
                        else
                        {
                            // food decrease
                            DecreaseFood();
                            NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                        }

                        #endregion;

                        animalClass.NextFoodDecay = GameManager.Instance.World.worldTime + FoodDecayTime;
                    }
                    else NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                }
                else if (State == AnimalState.GotoWater)
                {
                    #region Path to water and drinking;
                    NextUpdateCheck = DateTime.Now.AddMilliseconds(500);
                    Legacy.PathFinderThread.PathDescr path = PathFinderThread.Instance.GetPath(this.entityId);
                    string finished = "";
                    if (path.path == null)
                    {
                        finished = "path null";
                        // could NOT find a path, looks for another place
                        Legacy.PathFinderThread.Instance.FindPath(this, FoodLocation, this.GetWanderSpeed(),
                            (EAIBase)null);
                        //State = AnimalState.Wander;
                        //NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                        //DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " COULD NOT FIND A PATH TO WATER");
                    }
                    else
                        finished = path.path.isFinished().ToString();

                    float dist = Vector3.Distance(GetPosition(), FoodLocation);
                    if (finished == "true" || dist < 3.5f)
                        // allows for bigger distance, cause water may be in a hole, and the path.. well, get's stuck
                    {
                        State = AnimalState.Eating;
                        EatUntil = DateTime.Now.AddSeconds(EatTime);
                        DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " DRINKING");
                        if (!EatFood(true))
                        {
                            DecreaseFood();
                        }
                        else
                        {
                            this.PlayOneShot(SoundDrink);
                            IncreaseFood();
                        }
                        DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " DRINK WATER FOODLEVEL: " +
                                  animalClass.FoodLevel);
                    }
                    return;
                    #endregion;
                }
                else if (State == AnimalState.GotoFood)
                {
                    #region Path to food and eating;
                    NextUpdateCheck = DateTime.Now.AddMilliseconds(500);
                    Legacy.PathFinderThread.PathDescr path = PathFinderThread.Instance.GetPath(this.entityId);
                    string finished = "";

                    if (path.path == null)
                    {
                        finished = "path null";
                        Legacy.PathFinderThread.Instance.FindPath(this, FoodLocation, this.GetWanderSpeed(),
                            (EAIBase)null);
                        //State = AnimalState.Wander;
                        //NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                        //DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " COULD NOT FIND A PATH TO WATER");
                    }
                    else
                        finished = path.path.isFinished().ToString();

                    float dist = Vector3.Distance(GetPosition(), FoodLocation);
                    if (finished == "true" || dist < 1.5f)
                    {
                        State = AnimalState.Eating;
                        EatUntil = DateTime.Now.AddSeconds(EatTime);
                        DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " EATING");
                        if (!EatFood(false))
                        {
                            DecreaseFood();
                        }
                        else
                        {
                            this.PlayOneShot(SoundEat);
                            IncreaseFood();
                        }
                        DisownFood(); // it can allow another animal to go eat at the same block.
                        DoDebug("DOGSDX -> isMale: " + IsMale.ToString() + " EAT FOODLEVEL: " +
                                  animalClass.FoodLevel);
                    }
                    return;
                    #endregion;
                }
                else if (State == AnimalState.Eating)
                {
                    #region Eating/Drinking Delay;
                    if (EatUntil < DateTime.Now)
                    {
                        State = AnimalState.Wander;
                        NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                    }
                    else
                    {
                        this.getNavigator().SetPath((PathEntity) null, 0.0f);
                        NextUpdateCheck = DateTime.Now.AddMilliseconds(500);
                        return;
                    }
                    #endregion;
                }
                if (animalClass.IsPregnant)
                {
                    #region Birth Logic;
                    // checks if it can give birth
                    if (animalClass.offspring < GameManager.Instance.World.worldTime)
                    {
                        if ((GameManager.Instance.World.worldTime - animalClass.offspring) < 12000)
                        {
                            DoDebug("DOGSDX -> Baby borning");
                            SpawnAnimal(this.GetPosition(), true, true);
                            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
                            animalClass.IsPregnant = false;
                        }
                        else
                        {
                            DoDebug("DOGSDX -> Lost baby");
                            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
                            animalClass.IsPregnant = false;
                        }
                    }
                    #endregion;
                }
                //else NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                // write file.
                if (id0 == 0)
                    GetUID();    
                // TODO - only save from time-to-time or on key points (like unload)          
                SaveAnimalFile(id0, false, animalClass);
            }            
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> Error on updateLive: " + ex.Message);
        }
    }

    private bool CheckRunAway(EAIRunAway t)
    {
        if (t == null)
            return true;

        if (t.CanExecute())
        {
            this.getNavigator().clearPathEntity();
            t.Start();
            State = AnimalState.Wander;
            NextUpdateCheck = DateTime.Now.AddSeconds(10);
            return true;
        }
        return false;
    }

    public override void OnEntityUnload()
    {
        try
        {
            if (id0 > 0 && IsAlive()) SaveAnimalFile(id0, false, animalClass);
            base.OnEntityUnload();
        }
        catch (Exception ex)
        {

        }
    }

    public void Tame(int _id0, int _id1, int _id2)
    {        
        animalClass.FoodLevel = 10; // starts half food to avoid exploit.
        // get from file, if it exists        
        id0 = _id0;
        id1 = _id1;
        id2 = _id2;
        // a new tamed dog is ALWAYS a new dog.
        NewDog(ref animalClass);
        if (IsMale)
            animalClass.NextMating = GameManager.Instance.World.worldTime + MatingTime;
        DoDebug("DOGSDX -> TAMED isMale: " + IsMale.ToString() + " foodlevel:" + animalClass.FoodLevel);
        IsTamed = true;
    }

    public void Impregnate(int maleUID)
    {
        if (!IsMale)
        {            
            System.Random RandP = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
            int chance = Convert.ToInt32(Math.Floor(Convert.ToDecimal(animalClass.breederDog /2*5 + animalClass.breederDog)));
            if (chance < 10) chance = 15; // minumum of 10%
            int roll = RandP.Next(0, 100);
            if (roll < chance)
            //if (true)
            {
                DoDebug("DOGSDX -> Female was Impregnated with maleUID = " + maleUID);
                bool babyMale = false;
                roll = Rand.Next(0, 1000);
                if (roll <= 10) babyMale = true;
                int sonUID = combineStats(babyMale, maleUID);
                // needs to calculate stats of the baby right away, because otherwise the father can change his UID.
                DoDebug("DOGSDX -> created baby file with sonUID = " + sonUID);
                chance =
                    Convert.ToInt32(Math.Floor(Convert.ToDecimal(animalClass.Inteligence/2*5 + animalClass.Inteligence)));
                if (chance < 5) chance = 5; // minumum of 5%
                roll = RandP.Next(0, 100);
                if (roll < chance)
                {
                    if ((animalClass.hunterDog + animalClass.militaryDog + animalClass.breederDog +
                         animalClass.attackDog) < 30)
                        if (animalClass.breederDog < 20) animalClass.breederDog++;
                }
                animalClass.IsPregnant = true;
                // enlarge female
                this.gameObject.transform.localScale = new Vector3(meshScale, meshScale + 0.2F, meshScale);
                // increses "horizontal" scale to make it fat?
                animalClass.offspring = GameManager.Instance.World.worldTime + PregnancyTime;
                animalClass.BabyUID = sonUID;
                SaveAnimalFile(id0, false, animalClass);
            }
            else
            {               
                // there's a chance of gaining skill here
                if (this.GetRandom().Next(0, 100) > 75)
                {
                    DoDebug("DOGSDX -> Impregnation Failed but got skill from effort");
                    if ((animalClass.hunterDog + animalClass.militaryDog +
                         animalClass.breederDog +
                         animalClass.attackDog) < 30)
                        if (animalClass.breederDog < 20) animalClass.breederDog++;
                }
                else DoDebug("DOGSDX -> Impregnation Failed");
            }

        }
    }    

    private int GetAnimalsInArea() // i can use this to improve spawner class
    {
        return GameManager.Instance.World.GetEntitiesInBounds(typeof(EntityDogMorte), new Bounds(this.GetPosition(), new Vector3(MaxAnimalsAreaCheck, 255, MaxAnimalsAreaCheck))).Count;
    }

    private void SpawnAnimal(Vector3 posAux, bool Makebaby, bool CheckArea)
    {
        try
        {
            int idSpawn = -1;
            DoDebug(string.Format("DOGSDX -> SpawnAnimal with MALE={0}, FEMALE={1}, BABY={2}", entityMale, entityFemale, entityBaby));
            if (CheckArea)
            {

                int count = GetAnimalsInArea();

                if (count >= MaxAnimalsInArea)
                {
                    DoDebug("DOGSDX -> Too many dogs in area");
                    return;
                }
            }
            string entityToSpawn = "";
            bool spawnMale = false;
            // a VERY small change to breed a male IF a baby
            int roll = Rand.Next(0, 1000);
            if (roll <= 10) spawnMale = true;
            if (Makebaby) entityToSpawn = entityBaby;
            else
            {
                entityToSpawn = entityFemale;
                if (IsMale) entityToSpawn = entityMale;
            }
            DoDebug(string.Format("DOGSDX -> SpawnAnimal LOOK FOR {0}", entityToSpawn));
            if (entityToSpawn != "")
            {
                #region Create and give order to spawn the entity
                EntityCreationData _es = new EntityCreationData();
                _es.id = -1;
                _es.pos = posAux;
                _es.entityName = entityToSpawn;
                _es.entityClass = EntityClass.FromString(entityToSpawn);
                _es.onGround = false;
                _es.rot = Vector3.zero;
                int uid = id0; // mature animal that will inherit the baby personality file
                if (Makebaby)
                {
                    // a baby will
                    // find a near position instead
                    int x;
                    int y;
                    int z;
                    GameManager.Instance.World.FindRandomSpawnPointNearPosition(posAux, 15, out x, out y,
                        out z, new Vector3(2, 2, 2), true, true);
                    _es.pos = new Vector3((float) x, (float) y, (float) z);
                    // male / female will be decided on the baby creation.
                    // here I pass a previously created file with the combined statistics.
                    uid = animalClass.BabyUID;
                    if (uid == 0) // if combination fails.... well spawns randomly
                    {
                        uid = 200; // it's a tamed baby female
                        if (spawnMale) uid = 300; // it's a tamed baby male
                    }
                }                
                if (!Makebaby)
                {
                    // make the current animal disapear
                    DoDebug("DOGSDX -> SPAWNED MATURE, GONNA 'KILL' BABY");
                    // just unloads, no need to kill, because I want to keep the file
                    this.MarkToUnload();
                }
                else DoDebug("DOGSDX -> SPAWNED NEW BABY");
                _es.lifetime = (float)uid;
                GameManager.Instance.RequestToSpawnEntityServer(_es);
                #endregion;               
            }
            else DoDebug("DOGSDX -> SPAWN ENTITY NOT FOUND " + entityToSpawn);
        }
        catch (Exception ex)
        {
            DoDebug("DOGSDX -> ERROR SpawnAnimal" + ex.Message);
        }
                
    }

    // combine the female stats with the father stats
    // creates a new file with the combined stats
    // and passes the file to the baby
    private int combineStats(bool spawnMale, int maleUID)
    {
        int idPuppy = 0;
        try
        {
            clsDog father = LoadFile(maleUID);
            clsDog puppy = new clsDog();
            if (father != null && animalClass != null)
            {
                // combine the stats
                puppy.BabyUID = 0;
                puppy.Inteligence =
                    Convert.ToUInt32(Math.Round(Convert.ToDouble((father.Inteligence + animalClass.Inteligence)/2)));
                puppy.Stamina =
                    Convert.ToUInt32(Math.Round(Convert.ToDouble((father.Stamina + animalClass.Stamina) / 2)));
                puppy.Strenght =
                    Convert.ToUInt32(Math.Round(Convert.ToDouble((father.Strenght + animalClass.Strenght) / 2)));
                puppy.attackDog =
                        Convert.ToUInt32(Math.Round(Convert.ToDouble((father.attackDog + animalClass.attackDog) / 2)));
                puppy.breederDog =
                        Convert.ToUInt32(Math.Round(Convert.ToDouble((father.breederDog + animalClass.breederDog) / 2)));
                puppy.hunterDog =
                        Convert.ToUInt32(Math.Round(Convert.ToDouble((father.hunterDog + animalClass.hunterDog) / 2)));
                puppy.militaryDog =
                        Convert.ToUInt32(Math.Round(Convert.ToDouble((father.militaryDog + animalClass.militaryDog) / 2)));
                puppy.Loyalty =
                        Convert.ToUInt32(Math.Round(Convert.ToDouble((father.Loyalty + animalClass.Loyalty) / 2)));
                puppy.IsPregnant = false;
                puppy.FoodLevel = 10;
            }
            else NewDog(ref puppy);
            if (spawnMale)
                puppy.AnimalName = "babyMale";
            else puppy.AnimalName = "babyFemale";
            // generates a new ID
            idPuppy = NewUID();
            // saves the baby file
            SaveAnimalFile(idPuppy, true, puppy);
        }
        catch (Exception ex)
        {
            idPuppy = 0;
            DoDebug("DOGSDX -> ERROR combining parents" + ex.Message);
        }        
        return idPuppy;
    }

    public override float GetApproachSpeed()
    {
        if (State == AnimalState.OffSpring)
            return base.GetApproachSpeed() * this.gameObject.transform.localScale.x;

        return base.GetApproachSpeed();
    }

    public void MakeBaby()
    {
        DoDebug("DOGSDX -> MAKE BABY");
        animalClass.BirthTime = GameManager.Instance.World.GetWorldTime();
        State = AnimalState.OffSpring;
        animalClass.FoodLevel = 15; // starts half food to avoid exploit.;
        animalClass.AnimalName = "offspring";
        this.IsTamed = true; // a baby always borns already tamed, but with no owner (it can be sold)
    }

    private void NewDog(ref clsDog dogClass)
    {
        //EntityClass entityClass = EntityClass.list[this.entityClass];
        //GetStatLimits(entityClass);
        DoDebug("DOGSDX -> ASSIGNS RANDOM STATS");
        id1 = 0;
        id2 = 0;
        dogClass.AnimalName = "new";
        dogClass.Owner = "";
        // FoodLevel
        int min = 15;
        if ((int)FoodLevelL[0] != 5 && (int)FoodLevelL[0] > 0) min = (int)FoodLevelL[0];
        dogClass.FoodLevel = (uint) min;        
        // random properties if file does not exist
        System.Random RandStats = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        dogClass.BabyUID = -1;
        // loyalty
        min = 5;
        if ((int) LoyaltyL[0] > 5) min = (int) LoyaltyL[0];
        dogClass.Loyalty = Convert.ToUInt32(RandStats.Next(min, (int)LoyaltyL[1])); // starts with a minimum of loyalty always.
        // intelligence
        if ((int)InteligenceL[0] > 1) min = (int)InteligenceL[0];
        else min = 1;
        dogClass.Inteligence = Convert.ToUInt32(RandStats.Next(min, (int)InteligenceL[1]));
        // stamina
        if ((int)StaminaL[0] > 1) min = (int)StaminaL[0];
        else min = 1;
        dogClass.Stamina = Convert.ToUInt32(RandStats.Next(min, (int)StaminaL[1]));
        // Strenght
        if ((int)StrenghtL[0] > 1) min = (int)StrenghtL[0];
        else min = 1;
        dogClass.Strenght = Convert.ToUInt32(RandStats.Next(min, (int)StrenghtL[1]));
        // wild animals are not trained.
        // unless its a special class, with custom intervals
        dogClass.attackDog = Convert.ToUInt32(RandStats.Next((int)attackDogL[0], (int)attackDogL[1]));
        dogClass.breederDog = Convert.ToUInt32(RandStats.Next((int)breederDogL[0], (int)breederDogL[1]));
        dogClass.hunterDog = Convert.ToUInt32(RandStats.Next((int)hunterDogL[0], (int)hunterDogL[1]));
        dogClass.militaryDog = Convert.ToUInt32(RandStats.Next((int)militaryDogL[0], (int)militaryDogL[1]));
        dogClass.BirthTime = this.world.worldTime;
        dogClass.BabyUID = -1;
        // happiness
        if ((int)HappinessL[0] > 5) min = (int)HappinessL[0];
        else min = 5;
        dogClass.Happiness = (uint) min;
        dogClass.IsPregnant = false;
        dogClass.NextFoodDecay = this.world.worldTime;
        dogClass.NextMating = this.world.worldTime + MatingTime;
        dogClass.offspring = 0;
    }

    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.25f, 0.25f, 1f);
    }

    public enum AnimalState
    {
        OffSpring,
        Wander,
        GotoFood,
        Eating,
        Pregnant,
        GotoWater,
        AskPlay,
        GotoToy,
        ReturnToy,
        GotoOwner,
        FollowOwner,
        AttackZed, // attacks zombies and follow owner
        AttackHuman, // attacks humans and follow owner
        AttackAnimals, // attacks animals and follow owner
        AskFood,
        Other
    }

}