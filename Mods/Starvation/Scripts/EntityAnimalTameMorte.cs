using Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions.Must;
using XMLData.Item;
using Object = UnityEngine.Object;

public class clsAnimal
{
    public string AnimalName;
    public bool IsPregnant = false;
    public ulong BirthTime;
    public ulong offspring;
    public ulong NextMating; // if it's a male he will look for a female and try to mate with it
    public uint FoodLevel; // 0-20 -> if it reaches 0, starts to do damage. The percentage of insemination success depends on this food level. Always starts with 20
    public uint Loyalty; // 0-20 -> how much the animal is used to human presence
}

public class EntityAnimalTameMorte : EntityAnimalStag
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
    //public static int EatTime = 40;
    public int EatTime = 5;
    public float MaxDistanceToFood = 40f;
    public float MaxDistanceToWater = 15f;
    public int MaxAnimalsInArea = 5;
    public float MaxAnimalsAreaCheck = 40f;
    public float MaxMatingAreaCheck = 10f;


    private int pathTimeout = 250; // after this ticks they will simply giveup on the current path
    public int followInterval = 10; // a friendly animal will try to follow a human present each 10s.

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
    Vector3 FoodLocation = new Vector3();
    Vector3 WaterLocation = new Vector3(); // no need to persist, every time it wants water it will look around.
    #endregion;        
    #region General properties;
    private float meshScale = 1;
    private string entityMale = "";
    private string entityFemale = "";
    private string entityBaby = "";
    private string itemTamed = "";
    private string foodBlock = "";
    private string foodItem = "";
    private string tameItem = "";
    private string harvestItem = "";
    private string lootItem = "";
    private string tameSkill = "";
    private string harvestSkill = "";
    #endregion;
    #region Custom AI;
    // trying to customize AI, so that I can manipulate
    // AI tasks refering to player (approach or run away)
    // this means this entities will have 1 aditional taskList, refering to custom AUTOMATIC AI
    private static EAIFactory LM = new EAIFactory();
    private EAITaskList OA;
    private EntityAlive followEntityAlive; // follow a human instead of wandering
    #endregion;
    AnimalState State = AnimalState.Wander;
    #region Update Timers
    DateTime EatUntil;
    DateTime NextUpdateCheck = DateTime.MinValue;
    DateTime NextFollowCheck = DateTime.MinValue;
    #endregion
    private bool needsLoad = false; // to tell the server that it is a tamed animal, that needs to load the file    
    public clsAnimal animalClass = new clsAnimal(); // has the structure containing the personality variables, which are saved to file for persistency.

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
                DoDebug(string.Format("HUSBANDRY -> Init -> PLAYERID: {0}, TEAM: {1} - {2}, LIFETIME: {3}", this.belongsPlayerId, this.TeamNumber, this.teamNumber, this.lifetime));
                if (id0 == 0)
                {
                    GetUID(); // i DO NOT tame here, because if may be a vanilla animal.
                    idInit = id0;
                    SaveAnimalFile(idInit, true); // reserve the ID
                }
            }
            catch (Exception ex)
            {
                DoDebug("HUSBANDRY -> Init ERROR: " + ex.Message);
            }
            // HERE I SHOULD ALREADY HAVE THE ID, IF IT EXISTED PREVIOUSLY           
        }
        base.Init(_entityClass);
        try
        {
            if (lifetime < float.MaxValue) lifetime = float.MaxValue;
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
            animalClass.BirthTime = GameManager.Instance.World.GetWorldTime();
            if (IsBaby) MakeBaby();
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> Init(1) ERROR: " + ex.Message);
        }
    }

    public override void PostInit()
    {
        try
        {
            DoDebug(string.Format("HUSBANDRY -> PostInit -> PLAYERID: {0}, TEAM: {1} - {2}, LIFETIME: {3}", this.belongsPlayerId, this.TeamNumber, this.teamNumber, this.lifetime));
            base.PostInit();
            if (!world.IsRemote())
                CheckCreationID();
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> PostInit ERROR: " + ex.Message);
        }                        
    }

    private void CheckCreationID()
    {
        // IF LIFETIME HERE BRINGS AND ID (>100 && < float.maxvalue), I MUST use this ID instead... What i'll do is copy this "old" file to the new one
        // thus keeping things synced between clients.
        if (lifetime == 100)
        {            
            DoDebug("A NEWLY TAMED ANIMAL, NO FILE YET");
            this.Tame(id0, 0, 0);            
        }
        else if (lifetime == 200)
        {
            DoDebug("A NEWLY TAMED FEMALE BABY, NO FILE YET");
            this.Tame(id0, 0, 0);
            if (IsBaby)
                MakeBaby();
            IsMale = false;
        }
        else if (lifetime == 300)
        {
            DoDebug("A NEWLY TAMED MALE BABY, NO FILE YET");
            this.Tame(id0, 0, 0);
            if (IsBaby)
                MakeBaby();
            IsMale = true;
        }
        else
        {
            if (lifetime > 100 && lifetime <= 99999999)
            {
                int oldID = (int) lifetime;
                DoDebug("TAMED ANIMAL, OLD ID = " + oldID);
                LoadAnimalFile(oldID); // loads old file
                DeleteAnimalFile(oldID); // delete old file
                SaveAnimalFile(id0, false); // creates new file
            }
            else
            {
                DoDebug(string.Format("MAYBE RANDOM SPAWN - TAMED = {0}", IsTamed.ToString()));
                if (!IsTamed)
                {
                    // free the ID
                    DeleteAnimalFile(id0);
                    id0 = 0;
                }
            }
        }
        this.lifetime = float.MaxValue;
    }

    // CUSTOM AI CONTROL
    public override void CopyPropertiesFromEntityClass()
    {        
        base.CopyPropertiesFromEntityClass();
       AddRunAway();
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
            DoDebug("ADD RUNAWAY");
            string _className = "EAIRunawayFromEntity"; // make the animal run away            
            EAIBase _eai = (EAIBase) EntityAnimalTameMorte.LM.Instantiate(_className);
            if (_eai == null)
                DoDebug("Class '" + _className + "' not found!");
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
            DoDebug("ADD APPROACH SPOT");
            string _className = "EAIApproachSpot"; // make the animal run away            
            EAIBase _eai = (EAIBase)EntityAnimalTameMorte.LM.Instantiate(_className);
            if (_eai == null)
                DoDebug("Class '" + _className + "' not found!");
            else
            {
                _eai.SetEntity(this);
                this.OA.AddTask(1, _eai);
            }
        }
        catch (Exception ex)
        {
            DoDebug("ERROR CopyPropertiesFromEntityClass: " + ex.Message);
        }
    }

    protected override void updateTasks()
    {
        int pos = 0;
        try
        {
            //if (!Steam.Network.IsServer)
            if (this.world.IsRemote())
            {
                //this.setMoveForwardWithModifier(0.0f, false);
                // doesn't do anything locally
                //DoDebug("UpdateTasks overrided locally");
            }
            else if (IsTamed)
            {
                //DoDebug("HUSBANDRY -> UpdateTasks cycle");
                pos = 1;
                if (animalClass != null)
                {
                    if (animalClass.Loyalty != null)
                    {
                        pos = 2;
                        if (animalClass.Loyalty > 15)
                        {
                            pos = 3;
                            if (this.OA != null)
                                if (this.OA.Tasks.Count > 0)
                                {
                                    // if it has a runaway task clears it
                                    // we dont want the animal to run from humans.
                                    EAIRunAway[] tasks = this.GetLocalTasks<EAIRunAway>();
                                    if (tasks != null)
                                        if (tasks.Length > 0) this.OA.Tasks.Clear();
                                }
                            // the animal does not run away from humans any more
                            // but also does not move to them
                            pos = 4;
                            if (animalClass.Loyalty >= 19 && NextFollowCheck < DateTime.Now && State != AnimalState.GotoFood && State != AnimalState.GotoWater)
                            {
                                pos = 5;                                
                                // the animal will move towards humans if well fed
                                if (FindHumanToFollow())
                                {
                                    pos = 6;
                                    AddApproachSpot();
                                    //DoDebug("SET INVESTIGATE POSITION");
                                    this.SetInvestigatePosition(followEntityAlive.GetPosition(), 60);
                                    NextFollowCheck = DateTime.Now.AddSeconds(followInterval);
                                }
                            }
                        }
                        else
                        {
                            pos = 7;
                            if (this.OA != null)
                                if (this.OA.Tasks.Count > 0)
                                {
                                    pos = 8;
                                    // if it has a runaway task clears it
                                    // we dont want the animal to run from humans.
                                    EAIApproachSpot[] tasks = this.GetLocalTasks<EAIApproachSpot>();
                                    if (tasks != null)
                                        if (tasks.Length > 0) this.OA.Tasks.Clear();
                                }
                            pos = 9;
                            AddRunAway();
                            pos = 10;
                        }
                    }
                }                
                if (this.OA != null)
                {
                    pos = 11;
                    if (this.OA.Tasks.Count > 0)
                    {
                        pos = 12;
                        using (ProfilerUsingFactory.Profile("entities.live.ai.tasks"))
                        {
                            pos = 13;
                            this.OA.OnUpdateTasks();
                            pos = 14;
                        }
                    }
                }                
            }
        }
        catch (Exception ex)
        {
            DoDebug("ERROR " + pos + " updateTasks: " + ex.Message);
        }
        base.updateTasks();
    }

    // CUSTOM AI CONTROL

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
            DoDebug("HUSBANDRY -> ERROR CLAIMING FOOD " + ex.Message);
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
            DoDebug("HUSBANDRY -> ERROR DisownFood " + ex.Message);
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
            DoDebug("HUSBANDRY -> ERROR CLAIMING FOOD " + ex.Message);
        }
        return false;
    }

    public override void Write(BinaryWriter _bw)
    {
        //DoDebug(string.Format("HUSBANDRY -> WRITTING with id={0}, entityID={1}", id0, this.entityId));
        base.Write(_bw);
        try
        {
            //        _bw.Write((int)State);
            _bw.Write(IsTamed);
            _bw.Write(IsMale);
            _bw.Write(animalClass.IsPregnant);
            _bw.Write(animalClass.BirthTime);
            _bw.Write(animalClass.offspring);
            _bw.Write(animalClass.NextMating);
            _bw.Write(animalClass.FoodLevel);
            _bw.Write(animalClass.Loyalty);
            _bw.Write(FoodLocation.x);
            _bw.Write(FoodLocation.y);
            _bw.Write(FoodLocation.z);
            _bw.Write(WaterLocation.x);
            _bw.Write(WaterLocation.y);
            _bw.Write(WaterLocation.z);                                                
            _bw.Write(id0);
            _bw.Write(id1);
            _bw.Write(id2);
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
            DoDebug("HUSBANDRY -> ERRO WRITING: " + ex.Message);
        }
    }

    private void SaveAnimalFile(int idAux, bool saveOverride)
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
            DoDebug(string.Format("HUSBANDRY -> SAVING with id={0}", idAux));
            if (idAux > 0)
            {
                animalClass.AnimalName = idAux.ToString();
                System.Text.StringBuilder sb = new System.Text.StringBuilder("");
                sb.Append(animalClass.AnimalName + Environment.NewLine);
                sb.Append(animalClass.IsPregnant.ToString() + Environment.NewLine);
                sb.Append(animalClass.BirthTime + Environment.NewLine);
                sb.Append(animalClass.offspring + Environment.NewLine);
                sb.Append(animalClass.NextMating + Environment.NewLine);
                sb.Append(animalClass.FoodLevel + Environment.NewLine);
                sb.Append(animalClass.Loyalty + Environment.NewLine);
                string filename = path + animalClass.AnimalName + ".anm";
                //DoDebug("FILE: " + filename);
                System.IO.File.WriteAllText(filename, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> ERROR SAVING FILE " + ex.Message);
        }
    }

    private bool LoadAnimalFile(int idAux)
    {
        try
        {
            if (this.world.IsRemote()) return false;
            if (!IsTamed) return false;
            string path = GetSavePath();
            string filename = path + idAux + ".anm";
            if (!System.IO.File.Exists(filename))
            {
                return false;
            }
            if (idAux > 0)
            {
                DoDebug(string.Format("HUSBANDRY -> LOADING with id={0}", idAux));
                string[] lines = System.IO.File.ReadAllLines(filename);
                DoDebug("FILE: " + filename + "lines = " + lines.Length);
                if (lines.Length < 7) return false;
                animalClass.AnimalName = lines[0];
                animalClass.IsPregnant = Convert.ToBoolean(lines[1]);
                animalClass.BirthTime = Convert.ToUInt64(lines[2]);
                animalClass.offspring = Convert.ToUInt64(lines[3]);
                animalClass.NextMating = Convert.ToUInt64(lines[4]);
                animalClass.FoodLevel = Convert.ToUInt32(lines[5]);
                animalClass.Loyalty = Convert.ToUInt32(lines[6]);
                IsTamed = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> ERROR LOADING: " + ex.Message);
            animalClass.AnimalName = "new";
            animalClass.FoodLevel = 10;
            animalClass.Loyalty = 1;
            id0 = 0;
            id1 = 0;
            id2 = 0;
        }
        return false;
    }

    private void DeleteAnimalFile(int idAux)
    {
        try
        {
            if (this.world.IsRemote()) return;
            DoDebug(string.Format("HUSBANDRY -> DELETING FILE"));
            if (idAux > 0)
            {
                string path = GetSavePath();
                string filename = path + idAux + ".anm";
                DoDebug("HUSBANDRY -> DELETING FILENAME: " + filename);
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
            DateTime isNow = DateTime.UtcNow;
            string calcID = string.Format("{0}{1}", isNow.ToString("yy"), isNow.DayOfYear.ToString("000")); //isNow.ToString("yyMMdd")
            string path = GetSavePath();
            string filename = "";
            for (int i = 1; i <= 999; i++)
            {
                filename = string.Format("{0}{1}.anm", calcID, i.ToString("000"));
                if (!System.IO.File.Exists(path + filename))
                {
                    id0 = Convert.ToInt32(string.Format("{0}{1}", calcID, i.ToString("000")));
                    break;
                }
            }
            DoDebug("HUSBANDRY -> NEW ID: " + id0);            
            // creates unique ID                        
            //id0 = Convert.ToInt32(isNow.ToString("yyMM"));
            //id1 = Convert.ToInt32(isNow.ToString("HHmm"));            
            //id2 = Convert.ToInt32(isNow.ToString("ssfff"));
        }
    }

    public override void Read(byte _version, BinaryReader _br)
    {
        DoDebug(string.Format("HUSBANDRY -> READING with id={0}, playerid={1}", id0, belongsPlayerId));
        base.Read(_version, _br);
        if (_br.BaseStream.Position == _br.BaseStream.Length)
            return; //probably a vanilla chicken so just return.

        try
        {
            IsTamed = _br.ReadBoolean();
            IsMale = _br.ReadBoolean();
            animalClass.IsPregnant = _br.ReadBoolean();
            animalClass.BirthTime = (ulong)_br.ReadInt64();
            animalClass.offspring = (ulong)_br.ReadInt64();
            animalClass.NextMating = (ulong)_br.ReadInt64();
            animalClass.FoodLevel = (uint)_br.ReadInt32();
            animalClass.Loyalty = (uint)_br.ReadInt32();
            FoodLocation = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            WaterLocation = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            id0 = _br.ReadInt32();
            id1 = _br.ReadInt32();
            id2 = _br.ReadInt32();
            DoDebug(string.Format("HUSBANDRY -> READING with id={0}, playerid={1}", id0, belongsPlayerId));
            DoDebug(string.Format("HUSBANDRY -> READING with Version={0}, lenght={1}", _version, _br.BaseStream.Length));

            if (idInit != id0 && id0 != 0)
            {
                DeleteAnimalFile(idInit); // free the resercation
            }

            if (animalClass.IsPregnant)
            {
                // enlarge female
                this.gameObject.transform.localScale = new Vector3(meshScale, meshScale + 0.2F, meshScale);
            }

            if (!ClaimFood(new Vector3i(FoodLocation)))
                FoodLocation = new Vector3();

            if (IsBaby)
                if (animalClass.BirthTime + Adolesence < GameManager.Instance.World.worldTime)
                {
                    State = AnimalState.OffSpring;
                    return;
                }
            State = AnimalState.Wander;
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> ERRO READING: " + ex.Message);
            animalClass.FoodLevel = 10;
        }       
    }

    protected override void Awake()
    {
        // this will depend if its rabbit or stag type.
        // if possible to do all on the same place

        BoxCollider component = this.gameObject.GetComponent<BoxCollider>();
        if ((bool)((Object)component))
        {
            component.center = new Vector3(0.0f, 0.85f, 0.0f);
            component.size = new Vector3(20.0f, 15.6f, 20.0f);
        }
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
                //if (player.inventory.IsHoldingGun())
                string itemName = player.inventory.holdingItem.GetItemName();
                DoDebug(string.Format("HUSBANDRY -> Hit with {0}, tameitem is {1}, harvestitem is {2}", itemName, tameItem, harvestItem));
                if (itemName == tameItem)
                {
                    Skill skill = player.Skills.GetSkillByName(tameSkill);
                    if (skill == null)
                    {

                    }
                    else
                    {
                        int chance = skill.Level;
                        int roll = Rand.Next(0, 100);
                        if (roll <= chance)
                        {
                            if (!this.world.IsRemote() && IsTamed)
                            {
                                if (id0 == 0) GetUID(); // no file yet?
                                SaveAnimalFile(id0, false);
                            }                                          
                            ItemStack animal = new ItemStack(ItemClass.GetItem(itemTamed), 1);
                            // i have to divide the number in 2 so that I'm able to save it persistently with the item
                            string idAux = id0.ToString("00000000");
                            animal.itemValue.Meta = Convert.ToInt32(idAux.Substring(0, 4));
                            animal.itemValue.Quality = Convert.ToInt32(idAux.Substring(4, 4));
                            animal.itemValue.UseTimes = id0;
                            DoDebug(string.Format("HUSBANDRY -> ITEM CREATED WITH ID0: {0}, ID1: {1}, ID2: {2}", animal.itemValue.Meta, animal.itemValue.Quality, id2));
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
                            player.bag.AddItem(lootHarvestd);                                                        
                        }
                        // it NEVER hurst the animal. NEVER
                        return 0;
                    }
                }
            }
        }      
        return base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
    }

    static int FoodBlockID = 0;
    static int FoodItemID = 0;

    public override void OnEntityDeath()
    {
        DoDebug(string.Format("HUSBANDRY -> DEATH tamed: {0}", WasTamed));
        if (!WasTamed && !this.world.IsRemote() && id0 > 0)
        {
            // delete the file, if exists
            DeleteAnimalFile(id0);
        }
        base.OnEntityDeath();
    }

    private void GetFoodBlock()
    {        
        FoodItemID = ItemClass.GetItem(foodItem).type;
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

    private bool DrinkWater()
    {
        bool result = false;
        Vector3i water = new Vector3i(WaterLocation);
        IChunk chunk = GameManager.Instance.World.GetChunkFromWorldPos(water);
        BlockValue block = GameManager.Instance.World.GetBlock(water);

        if (Block.list[block.type].blockMaterial.IsLiquid)
        {
            try
            {
                // deplete water
                Block.list[block.type].DoExchangeAction(GameManager.Instance.World, water, block, "deplete1", 1);
                BlockLiquidv2.DepleteFromBlock(block, water);
                return true;
            }
            catch (Exception)
            {
                return false;
            }            
        }
        return result;
    }

    private bool FindWater(WaterType waterType)
    {
        // only looks for water from time to time
        // food is more often
        WaterLocation = new Vector3();

        Vector3i pos = new Vector3i(this.GetPosition());

        // tries the last known water location, to save time
        BlockValue waterStorage = GameManager.Instance.World.GetBlock(new Vector3i(WaterLocation));
        if (Block.list[waterStorage.type].blockMaterial.IsLiquid)
        {
            bool isValid = CheckWaterType(waterType, waterStorage);

            if (Vector3.Distance(this.GetPosition(), WaterLocation) < MaxDistanceToWater && isValid)
            {
                // it is on water
                return true;
            }
        }

        if (!FindWater(pos, waterType))
            if (!FindWater(pos + new Vector3i(-16, 0, -16), waterType))
                if (!FindWater(pos + new Vector3i(0, 0, -16), waterType))
                    if (!FindWater(pos + new Vector3i(16, 0, -16), waterType))
                        if (!FindWater(pos + new Vector3i(-16, 0, 0), waterType))
                            if (!FindWater(pos + new Vector3i(0, 0, 16), waterType))
                                if (!FindWater(pos + new Vector3i(-16, 0, 16), waterType))
                                    if (!FindWater(pos + new Vector3i(0, 0, 16), waterType))
                                        if (!FindWater(pos + new Vector3i(16, 0, 16), waterType))
                                        {
                                            // //BackupManager.sendMessage("Couldn't find nest");
                                            return false;
                                        }
        return true;

    }

    private static bool CheckWaterType(WaterType waterType, BlockValue waterStorage)
    {
        bool isValid = false;
        if (waterType == WaterType.Bucket)
        {
            if (Block.list[waterStorage.type].GetBlockName() == "waterMovingBucket" ||
                Block.list[waterStorage.type].GetBlockName() == "waterStaticBucket")
                isValid = true;
        }
        else if (waterType == WaterType.Natural)
        {
            if (Block.list[waterStorage.type].GetBlockName() == "waterMoving" ||
                Block.list[waterStorage.type].GetBlockName() == "water")
                isValid = true;
        }
        else isValid = true; // i imagine putting here custom stuff, to make pathing easier.
        return isValid;
    }

    private bool FindWater(Vector3i pos, WaterType waterType)
    {
        IChunk chunk = GameManager.Instance.World.GetChunkFromWorldPos(pos);

        for (int y = pos.y - 3; y < pos.y + 4; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    BlockValue b = chunk.GetBlock(x, y, z);

                    if (Block.list[b.type].blockMaterial.IsLiquid)
                    {
                        bool isValid = CheckWaterType(waterType, b);

                        if (!isValid) return false;
                        Vector3i wpos = chunk.GetWorldPos();

                        WaterLocation = new Vector3(wpos.x + x, y, wpos.z + z);

                        // goto waterpos
                        this.SetInvestigatePosition(WaterLocation, 30);
                        this.getNavigator().clearPathEntity();
                        Legacy.PathFinderThread.Instance.FindPath(this, WaterLocation, this.GetWanderSpeed(),
                            (EAIBase)null);
                        return true;
                    }

                }
            }
        }
        return false;
    }

    private bool FindFood()
    {

        if (FoodBlockID == 0)
            GetFoodBlock();

        Vector3i pos = new Vector3i(this.GetPosition());

        BlockValue foodStorage = GameManager.Instance.World.GetBlock(new Vector3i(FoodLocation));
        if (foodStorage.type == FoodBlockID)
            if (Vector3.Distance(this.GetPosition(), FoodLocation) < MaxDistanceToFood)
                return true;
            else
                DisownFood();
        
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
                                           // //BackupManager.sendMessage("Couldn't find nest");
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
                        if (ClaimFood(new Vector3i(foodPos)))
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

    private bool FindHumanToFollow()
    {
        //DoDebug(string.Format("LOOK FOR HUMAN"));
        // IF THE POSITION IS LONGER THEN 5f ? Otherwise it will just do what it has to do.
        try
        {
            if (animalClass != null)
            {
                if (animalClass.FoodLevel < 10)
                {
                    // if it needs food it will look for food instead
                    //DoDebug(string.Format("TOO HUNGRY TO FOLLOW"));
                    return false;
                }
            }
            float seeDistance = this.GetSeeDistance();
            using (
                List<Entity>.Enumerator enumerator =
                    this.world.GetEntitiesInBounds(typeof (EntityPlayer),
                        BoundsUtils.BoundsForMinMax(this.position.x - seeDistance, this.position.y - seeDistance,
                            this.position.z - seeDistance, this.position.x + seeDistance, this.position.y + seeDistance,
                            this.position.z + seeDistance)).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    EntityAlive _other = enumerator.Current as EntityAlive;
                    if (!this.CanSee(_other))
                    {
                        if ((double) this.GetDistanceSq((Entity) _other) <= 16.0)
                        {
                            followEntityAlive = _other;
                            break;
                        }
                    }
                    else
                    {
                        followEntityAlive = _other;
                        break;
                    }
                }
            }
            if ((UnityEngine.Object) followEntityAlive == (UnityEngine.Object) null)
            {
                //DoDebug(string.Format("NO PLAYER FOUND TO FOLLOW"));
                return false;
            }
            float dist = Vector3.Distance(GetPosition(), followEntityAlive.GetPosition());
            if (dist < 2.5f)
            {
                //DoDebug(string.Format("ALREADY CLOSE"));
                return false;
            }
            //DoDebug(string.Format("FOUND PLAYER TO FOLLOW"));
        }
        catch (Exception ex)
        {
            DoDebug("ERROR FINDING HUMAN " + ex.Message);
            return false;
        }
        return true;
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();

        if (this.world.IsRemote()) return;
        if (IsDead() || !IsTamed)
            return;
        try
        {
            //DoDebug(string.Format("HUSBANDRY {0}-> UPDATE LIVE CYCLE. NextUpdateCheck = {0}", NextUpdateCheck));
            if (NextUpdateCheck < DateTime.Now)
            {
                DoDebug(string.Format("HUSBANDRY {0}-> UPDATE LIVE CYCLE. WANDERTIME = {1}", id0, WanderTime));
                
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
                        DoDebug("HUSBANDRY -> FULLY GROWN");
                        SaveAnimalFile(id0, false);
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
                    //DoDebug(String.Format("HUSBANDRY -> WANDERING. isMale={0}, nextMating={1}, worldTime={2}, isBaby={3}", IsMale.ToString(), animalClass.NextMating, GameManager.Instance.World.worldTime, IsBaby.ToString()));
                    if (IsMale && !IsBaby)
                    {
                        if (animalClass.NextMating < GameManager.Instance.World.worldTime)
                        {
                            // the chance to mate depends on the foodlevel
                            int chance =
                                Convert.ToInt32(
                                    Math.Floor(Convert.ToDecimal(animalClass.FoodLevel/2*5 + animalClass.FoodLevel)));
                            roll = Rand.Next(0, 100);
                            if (roll < chance)
                            {
                                DoDebug("HUSBANDRY -> Male trying to mate");
                                List<Entity> animals =
                                    GameManager.Instance.World.GetEntitiesInBounds(typeof (EntityAnimalTameMorte),
                                        new Bounds(this.GetPosition(),
                                            new Vector3(MaxMatingAreaCheck, 255, MaxMatingAreaCheck)));
                                foreach (Entity entity in animals)
                                {
                                    if (!entity.IsDead())
                                    {
                                        // TODO - get entityname OR COMPARE BY ENTITYID
                                        //DoDebug("Found: " + (entity as EntityAnimalTameMorte).EntityName + " comparing with " + entityFemale + " isPregnate = " + (entity as EntityAnimalTameMorte).IsPregnant.ToString());
                                        //entity.entityId                                 
                                        if ((entity as EntityAnimalTameMorte).EntityName == entityFemale &&
                                            !(entity as EntityAnimalTameMorte).animalClass.IsPregnant)
                                        {
                                            //DoDebug("Impregnate female");
                                            (entity as EntityAnimalTameMorte).Impregnate();
                                            break;
                                        }
                                    }
                                }
                            }
                            animalClass.NextMating = GameManager.Instance.World.worldTime + MatingTime;
                        }
                    }
                    // 80% chance of needing food
                    roll = Rand.Next(0, 100);
                    if (roll < 80)
                    {
                        if (FindFood())
                        {
                            this.SetInvestigatePosition(FoodLocation, 30);
                            this.getNavigator().clearPathEntity();
                            Legacy.PathFinderThread.Instance.FindPath(this, FoodLocation, this.GetWanderSpeed(),
                                (EAIBase) null);
                            State = AnimalState.GotoFood;
                            DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " GOING TO FOOD");
                        }
                        else
                        {
                            DecreaseFood();
                            DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " NO FOOD FOODLEVEL: " +
                                      animalClass.FoodLevel);
                            NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                        }
                    }
                    else NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                    //else
                    //{
                    //    // looks for bucket water but only very close
                    //    MaxDistanceToWater = 15f;
                    //    if (FindWater(WaterType.Bucket))
                    //    {
                    //        State = AnimalState.GotoWater;
                    //        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " GOING TO WATER (BUCKET)");
                    //    }
                    //    else
                    //    {
                    //        // looks for natural water but only very close
                    //        MaxDistanceToWater = 5f;
                    //        if (FindWater(WaterType.Natural))
                    //        {
                    //            State = AnimalState.GotoWater;
                    //            DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " GOING TO WATER (NATURAL)");
                    //        }
                    //        DecreaseFood();
                    //        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " NO WATER FOODLEVEL: " +
                    //                  animalClass.FoodLevel);
                    //        NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                    //    }
                    //}
                }
                else if (State == AnimalState.GotoWater)
                {
                    NextUpdateCheck = DateTime.Now.AddMilliseconds(500);
                    Legacy.PathFinderThread.PathDescr path = PathFinderThread.Instance.GetPath(this.entityId);
                    string finished = "";

                    if (path.path == null)
                    {
                        finished = "path null";
                        // could NOT find a path, looks for another place
                        Legacy.PathFinderThread.Instance.FindPath(this, WaterLocation, this.GetWanderSpeed(),
                            (EAIBase)null);
                        //State = AnimalState.Wander;
                        //NextUpdateCheck = DateTime.Now.AddSeconds(WanderTime);
                        //DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " COULD NOT FIND A PATH TO WATER");
                    }
                    else
                        finished = path.path.isFinished().ToString();

                    float dist = Vector3.Distance(GetPosition(), WaterLocation);
                    if (finished == "true" || dist < 3.5f)
                        // allows for bigger distance, cause water may be in a hole, and the path.. well, get's stuck
                    {
                        State = AnimalState.Eating;
                        EatUntil = DateTime.Now.AddSeconds(EatTime);
                        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " DRINKING");
                        if (!DrinkWater())
                        {
                            DecreaseFood();
                        }
                        else
                        {
                            IncreaseFood();
                        }
                        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " DRINK WATER FOODLEVEL: " +
                                  animalClass.FoodLevel);
                    }
                    return;

                }
                else if (State == AnimalState.GotoFood)
                {
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
                        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " EATING");
                        if (!EatFood())
                        {
                            DecreaseFood();
                        }
                        else
                        {
                            IncreaseFood();
                        }
                        DisownFood(); // it can allow another animal to go eat at the same block.
                        DoDebug("HUSBANDRY -> isMale: " + IsMale.ToString() + " EAT FOODLEVEL: " +
                                  animalClass.FoodLevel);
                    }
                    return;

                }
                else if (State == AnimalState.Eating)
                {
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
                }
                if (animalClass.IsPregnant)
                {
                    // checks if it can give birth
                    if (animalClass.offspring < GameManager.Instance.World.worldTime)
                    {
                        if ((GameManager.Instance.World.worldTime - animalClass.offspring) < 12000)
                        {
                            DoDebug("HUSBANDRY -> Baby borning");
                            SpawnAnimal(this.GetPosition(), true, true);
                            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
                            animalClass.IsPregnant = false;
                        }
                        else
                        {
                            DoDebug("HUSBANDRY -> Lost baby");
                            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
                            animalClass.IsPregnant = false;
                        }
                    }
                }
                // write file.
                if (id0 == 0)
                    GetUID();
                SaveAnimalFile(id0, false);
            }            
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> Error on updateLive: " + ex.Message);
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

    private void IncreaseFood()
    {
        if (animalClass.FoodLevel < 20)
        {
            animalClass.FoodLevel++;
        }
        else
        {
            // well fed - improves loyalty
            if (animalClass.Loyalty < 20) animalClass.Loyalty++;
        }
    }

    private void DecreaseFood()
    {
        if (animalClass.FoodLevel > 0) animalClass.FoodLevel--;
        if (animalClass.FoodLevel == 0) this.DamageEntity(DamageSource.starve, 10, false, 1);
    }

    public void Tame(int _id0, int _id1, int _id2)
    {        
        animalClass.FoodLevel = 10; // starts half food to avoid exploit.
        // get from file, if it exists        
        id0 = _id0;
        id1 = _id1;
        id2 = _id2;
        //if (id0 > 0 && id1 > 0 && id2 > 0)
        if (id0 > 0)
        {
            // read file
            LoadAnimalFile(id0);
        }
        if (IsMale)
            animalClass.NextMating = GameManager.Instance.World.worldTime + MatingTime;
        DoDebug("HUSBANDRY -> TAMED isMale: " + IsMale.ToString() + " foodlevel:" + animalClass.FoodLevel);
        IsTamed = true;
    }

    public void Impregnate()
    {
        if (!IsMale)
        {            
            System.Random RandP = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
            int chance = Convert.ToInt32(Math.Floor(Convert.ToDecimal(animalClass.FoodLevel /2*5 + animalClass.FoodLevel)));
            int roll = RandP.Next(0, 100);
            if (roll < chance)
            {
                DoDebug("HUSBANDRY -> Female was Impregnated");
                animalClass.IsPregnant = true;
                // enlarge female
                this.gameObject.transform.localScale = new Vector3(meshScale, meshScale + 0.2F, meshScale);
                // increses "horizontal" scale to make it fat?
                animalClass.offspring = GameManager.Instance.World.worldTime + PregnancyTime;
            }
            else DoDebug("HUSBANDRY -> Impregnation Failed");

        }
    }

    public override void OnEntityUnload()
    {
        try
        {
            if (id0 > 0 && IsAlive()) SaveAnimalFile(id0, false);
            base.OnEntityUnload();
        }
        catch (Exception ex)
        {

        }
    }

    private bool EatFood()
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

            bool hasFood = false;
            foreach (ItemStack i in items)
            {
                if (i == null)
                    continue;

                if (i.itemValue.type == FoodItemID && i.count > 0)
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

                    if (i.itemValue.type == FoodItemID && i.count > 0)
                    {
                        i.count--;
                        inv.SetModified();
                        return true;
                    }
                }                
            }
        }
        return result;
    }

    private int GetAnimalsInArea() // i can use this to improve spawner class
    {
        return GameManager.Instance.World.GetEntitiesInBounds(typeof(EntityAnimalTameMorte), new Bounds(this.GetPosition(), new Vector3(MaxAnimalsAreaCheck, 255, MaxAnimalsAreaCheck))).Count;
    }

    private void SpawnAnimal(Vector3 posAux, bool Makebaby, bool CheckArea)
    {
        try
        {
            int idSpawn = -1;
            DoDebug("HUSBANDRY -> SpawnAnimal");
            if (CheckArea)
            {

                int count = GetAnimalsInArea();

                if (count >= MaxAnimalsInArea)
                {
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
            using (Dictionary<int, EntityClass>.KeyCollection.Enumerator enumerator = EntityClass.list.Keys.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int current = enumerator.Current;
                    if (EntityClass.list[current].entityClassName == entityToSpawn)
                    {
                        idSpawn = current;
                        break;
                    }
                }
            }
            if (entityToSpawn != "" && idSpawn > -1)
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
                    // find a near position instead
                    int x;
                    int y;
                    int z;
                    GameManager.Instance.World.FindRandomSpawnPointNearPosition(posAux, 15, out x, out y,
                        out z, new Vector3(2, 2, 2), true, true);
                    _es.pos = new Vector3((float) x, (float) y, (float) z);
                    uid = 200; // it's a tamed baby female
                    if (spawnMale) uid = 300; // it's a tamed baby male
                }                
                if (!Makebaby)
                {
                    // make the current animal disapear
                    DoDebug("HUSBANDRY -> SPAWNED MATURE, GONNA 'KILL' BABY");
                    this.Kill(new DamageResponse());
                    this.MarkToUnload();
                }
                else DoDebug("HUSBANDRY -> SPAWNED NEW BABY");
                _es.lifetime = (float)uid;
                GameManager.Instance.RequestToSpawnEntityServer(_es);
                #endregion;               
            }
        }
        catch (Exception ex)
        {
            DoDebug("HUSBANDRY -> ERROR SpawnAnimal" + ex.Message);
        }
                
    }

    public override float GetApproachSpeed()
    {
        if (State == AnimalState.OffSpring)
            return base.GetApproachSpeed() * this.gameObject.transform.localScale.x;

        return base.GetApproachSpeed();
    }

    public void MakeBaby()
    {
        DoDebug("HUSBANDRY -> MAKE BABY");
        animalClass.BirthTime = GameManager.Instance.World.GetWorldTime();
        State = AnimalState.OffSpring;
        animalClass.FoodLevel = 10; // starts half food to avoid exploit.;
    }

    public override Vector3 GetMapIconScale()
    {
        //return new Vector3(0.25f, 0.25f, 1f);
        return new Vector3(0.45f, 0.45f, 1f);
    }

    public enum AnimalState
    {
        OffSpring,
        Wander,
        GotoFood,
        Eating,
        Pregnant,
        GotoWater,
        Other
    }

    public enum WaterType
    {
        Natural,
        Bucket,
        Other
    }
}