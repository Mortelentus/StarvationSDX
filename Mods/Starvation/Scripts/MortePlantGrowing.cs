using System;
using Random = System.Random;
using UnityEngine;
using SDX.Payload;

//public class BlockMortePlantGrowing : BlockPlantGrowing
/// <summary>
/// Custom class for floating blocks
/// Mortelentus 2016 - v1.0
/// </summary>
public class BlockMortePlantGrowing : BlockPlantGrowing
{
    private bool disableDebug = true;
    private int checkrange = 7;
    private float meshScale = 1;
    float minScale = 1;
    float maxScale = 1;
    private int plagueChance = 5;
    private int ratChance = 5;
    private int spreadChance = 20;
    private int deathChance = 10;
    private string particle = "#fly?FlyParticle";
    string entityGroup = "RatPlague";
    string antiPlagueDevice = "InsectLamp";
    string antiPestDevice = "UltraSound";
    private Vector3 UY;
    private BlockValue lastBlockValue = BlockValue.Air;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    public override void Init()
    {
        base.Init();
        // mesh size
        if (this.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = this.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');
            
            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }            
        }
        if (this.Properties.Values.ContainsKey("ParticleName"))
            this.particle = this.Properties.Values["ParticleName"];
        if (!this.Properties.Values.ContainsKey("ParticleOffset"))
            this.UY = Utils.ParseVector3("0.0,0.5,0.0");
        else this.UY = Utils.ParseVector3(this.Properties.Values["ParticleOffset"]);
        if (this.Properties.Values.ContainsKey("plagueChance"))
            plagueChance = int.Parse(this.Properties.Values["plagueChance"]);
        if (this.Properties.Values.ContainsKey("ratChance"))
            ratChance = int.Parse(this.Properties.Values["ratChance"]);
        if (this.Properties.Values.ContainsKey("EntityGroup"))
        {
            entityGroup = this.Properties.Values["EntityGroup"];
        }
    }

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "PLANT: " + str;
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
            {
                // Check if the game instance is not null
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
                }
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        int _damagePoints,
        int _entityIdThatDamaged, bool _bUseHarvestTool)
    {
        Entity player = _world.GetEntity(_entityIdThatDamaged);
        if (player is EntityPlayer)
        {
            //if (!_world.IsRemote())
            {
                ItemClass itemHeld = (player as EntityPlayer).inventory.holdingItem;
                if (itemHeld != null)
                {
                    if (itemHeld.Name == "SprayItem")
                    {
                        if (_blockValue.meta != 1)
                        {
                            debugHelper.doDebug(string.Format("PLANT GROWN: PLANT SPRAYED"), !disableDebug);
                            _blockValue.meta = 2;
                            _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                        }
                        return 0;
                    }
                }
            }
            //else return 0;
        }
        return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged,
            _bUseHarvestTool);
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        if (_world.IsRemote())
        {
            if (lastBlockValue.type != BlockValue.Air.type && _blockValue.type != BlockValue.Air.type)
            {
                if (lastBlockValue.meta != _blockValue.meta && _blockValue.meta == 1)
                {
                    debugHelper.doDebug(string.Format("PLANT GROWN WAS INFECTED"), !disableDebug);
                    checkParticles(_world, _clrIdx, _blockPos, _blockValue);
                }
            }
            lastBlockValue = _blockValue;
        }
        if (!_world.IsRemote())
        {
            bool flyPlague = false;
            bool ratPlague = false;
            bool plagued = false;
            System.Random rdnRandom = new Random();
            int roll = 0;
            if (_blockValue.meta == 1)
                plagued = true;

            if (!plagued)
            {
                int baseChance = plagueChance;
                baseChance = MorteSpawners.PlagueChance(_world, _clrIdx, _blockPos, _blockValue, baseChance, 5, !disableDebug);
                // chance to crit - 2%
                if (rdnRandom.Next(1, 1001) < 20)
                    roll = rdnRandom.Next(1, 501);
                else roll = rdnRandom.Next(1, 1001);
                debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with FLY CHANCE = {0} ROLL = {1}", baseChance, roll), !disableDebug);
                flyPlague = (roll < baseChance);
                if (!flyPlague)
                {
                    roll = rdnRandom.Next(1, 101);
                    debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with RAT CHANCE = {0} ROLL = {1}", ratChance, roll), !disableDebug);
                    ratPlague = (roll < ratChance);
                }
            }
            // check for plague
            if (flyPlague)
            {
                // check if there's any powered light
                if (antiPlagueDevice != "")
                {
                    if (MorteSpawners.FindTrap(_world, _clrIdx, _blockPos, 10, antiPlagueDevice, !disableDebug))
                    {
                        // reduces to a residual 1%
                        roll = rdnRandom.Next(1, 1001);
                        debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with FLY CHANCE = 10 ROLL = {0}", roll),
                            !disableDebug);
                        flyPlague = (roll < 10);
                    }
                }
                if (flyPlague)
                {
                    _blockValue.meta = 1;
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                    if (!this.isPlantGrowingRandom)
                        _world.GetWBT()
                            .AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, (ulong) _rnd.Next(500, 3000));
                    return true;
                }
            }
            // rats - it keeps growing though
            if (ratPlague)
            {
                if (antiPestDevice != "")
                {
                    if (MorteSpawners.FindTrap(_world, _clrIdx, _blockPos, 10, antiPestDevice, !disableDebug))
                    {
                        // reduces to a residual 1%
                        roll = rdnRandom.Next(1, 1001);
                        debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with RAT CHANCE = 10 ROLL = {0}", roll),
                            !disableDebug);
                        ratPlague = (roll < 10);
                    }
                }
                if (ratPlague)
                {
                    // spawn rats and make them check this spot.                
                    MorteSpawners.SpawnGroupToPos(_world, _clrIdx, _blockPos, entityGroup, 10, 15, 10, !disableDebug);
                }
            }
            // if already plagued, it will eventually die
            if (plagued)
            {
                roll = rdnRandom.Next(1, 101);
                debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with DEATH CHANCE = {0} ROLL = {1}", deathChance, roll), !disableDebug);
                bool die = (roll < deathChance);
                //if (!die)
                {
                    // spread plague on nearby plants (up to 3 blocks away)
                    spreadChance = rdnRandom.Next(15, 25);
                    roll = rdnRandom.Next(1, 101);
                    debugHelper.doDebug(string.Format("PLANT GROWNING: Tick with SPREAD CHANCE = {0} ROLL = {1}", spreadChance, roll), !disableDebug);
                    if (roll < spreadChance)
                    {
                        Vector3i _plaguePos = Vector3i.zero;
                        if (MorteSpawners.SpreadPlague(_world, _clrIdx, _blockPos, 2, rdnRandom, out _plaguePos, !disableDebug))
                        {
                            BlockValue block = _world.GetBlock(_clrIdx, _plaguePos);
                            block.meta = 1;
                            _world.SetBlockRPC(_clrIdx, _plaguePos, block);
                        }
                    }
                    if (!this.isPlantGrowingRandom)
                        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, (ulong)_rnd.Next(500, 3000));                    
                }
                if (die)
                {
                    //removeParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
                    debugHelper.doDebug("Plant dying", disableDebug);
                    _world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);                    
                }
                return true;
            }
            // check if it's not raining.                
            if (WeatherManager.theInstance.GetCurrentRainfallValue() == 0.0F)
            {
                //Debug.Log(string.Format("Check for water"));
                //if it's not raining, then check if there's any liquid near it
                if (!this.CheckWaterNear(_world, _clrIdx, _blockPos))
                {
                    //Debug.Log(string.Format("NO WATER"));
                    DisplayChatAreaText("No water near");
                    if (!this.isPlantGrowingRandom)
                        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
                    return true;
                }
            }
        }
        return DoFullTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
        //return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    private bool DoFullTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick, ulong _ticksIfLoaded, Random _rnd)
    {
        if (this.nextPlant.type == 0)
            return false;
        if (!this.CheckPlantAlive(_world, _clrIdx, _blockPos, _blockValue))
            return true;
        if (!this.isPlantGrowingRandom && _bRandomTick)
        {
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
            return true;
        }
        ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
        if (chunkCluster == null)
            return true;
        Vector3i _blockPos1 = _blockPos + Vector3i.up;
        if ((int)chunkCluster.GetLight(_blockPos1, Chunk.LIGHT_TYPE.SUN) < this.lightLevelGrow)
        {
            if (!this.isPlantGrowingRandom)
                _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
            return true;
        }
        BlockValue block = _world.GetBlock(_clrIdx, _blockPos + Vector3i.up);
        if (!this.isPlantGrowingIfAnythingOnTop && block.type != 0 || Block.list[this.nextPlant.type] is BlockPlant && !((BlockPlant)Block.list[this.nextPlant.type]).CanGrowOn(_world, _clrIdx, _blockPos - Vector3i.up, this.nextPlant))
            return true;
        if (this.isPlantGrowingRandom)
        {
            if ((double)_blockValue.meta3and2 < (double)this.GetGrowthRate() - 1.0)
            {
                if (_rnd.Next(2) == 0)
                {
                    _blockValue.meta3and2 = (byte)((uint)_blockValue.meta3and2 + 1U);
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                }
                return true;
            }
            _blockValue.meta3and2 = (byte)0;
        }
        _blockValue.type = this.nextPlant.type;
        BiomeDefinition biome = ((World)_world).GetBiome(_blockPos.x, _blockPos.z);
        if (biome != null && biome.Replacements.ContainsKey(_blockValue.type))
            this.nextPlant.type = biome.Replacements[_blockValue.type];
        if (this.bGrowOnTopEnabled)
            _blockValue.meta = (byte) ((int) _blockValue.meta + 1 & 15);
        else
        {
            if (_blockValue.meta == 0)
                _blockValue.meta = 3; // informing that it comes from growing crops, thus will tick
        }
        if (this.isPlantGrowingRandom || _ticksIfLoaded <= this.GetTickRate() || !Block.list[_blockValue.type].UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded - this.GetTickRate(), _rnd))
            _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
        if ((this.growOnTop.type != 0 || this.growOnTopAlternative.type != 0) && (_blockPos.y + 1 < (int)byte.MaxValue && block.type == 0))
        {
            if ((int)_blockValue.meta < this.growOnTopRestrictMeta)
                _blockValue.type = this.growOnTop.type;
            else if (this.growOnTopAlternative.type != 0)
                _blockValue.type = this.growOnTopAlternative.type;
            if (_blockValue.damage >= Block.list[_blockValue.type].blockMaterial.MaxDamage)
                _blockValue.damage = Block.list[_blockValue.type].blockMaterial.MaxDamage - 1;
            if (this.isPlantGrowingRandom || _ticksIfLoaded <= this.GetTickRate() || !Block.list[_blockValue.type].UpdateTick(_world, _clrIdx, _blockPos + Vector3i.up, _blockValue, _bRandomTick, _ticksIfLoaded - this.GetTickRate(), _rnd))
                _world.SetBlockRPC(_clrIdx, _blockPos + Vector3i.up, _blockValue);
        }
        return true;
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if (_blockValue.meta != 1)
            return; // plant is NOT plagued
        if(_ebcd == null || !_ebcd.bHasTransform) return;
        meshScale = UnityEngine.Random.Range(minScale, maxScale);
        _ebcd.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
        MorteParticleEffect.addParticle(_blockValue, _ebcd, this.UY, particle, disableDebug);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    private bool CheckWaterNear(WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        //string blocks = "";
        for (int i = _blockPos.x - checkrange; i <= (_blockPos.x + checkrange); i++)
        {
            for (int j = _blockPos.z - checkrange; j <= (_blockPos.z + checkrange); j++)
            {
                for (int k = _blockPos.y - checkrange; k <= (_blockPos.y + checkrange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    //blocks = blocks + block.type + ", ";
                    if (Block.list[block.type].blockMaterial.IsLiquid)
                    {
                        // deplete water randomly - 20% chance
                        System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
                        if (Rand.Next(0, 100) < 20)
                        {
                            DisplayChatAreaText("Consume water");
                            Block.list[block.type].DoExchangeAction(_world, new Vector3i(i, k, j), block, "deplete1", 1);
                            BlockLiquidv2.DepleteFromBlock(block, new Vector3i(i, j, k));
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    protected virtual void checkParticles(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (this.particle == null)
            return;
        if (_world.IsRemote()) return;
        this.addParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
    }

    protected virtual void addParticles(WorldBase _world, int _clrIdx, int _x, int _y, int _z, BlockValue _blockValue)
    {
        if (_world.IsRemote()) return;
        if (string.IsNullOrEmpty(particle))
            return;        
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(new Vector3i(_x, _y, _z));
        MorteParticleEffect.addParticle(_blockValue, _ebcd, this.UY, particle, disableDebug);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        debugHelper.doDebug(string.Format("PLANT GROWING: VALUE CHANGED = {0}", _newBlockValue), !disableDebug);
        if (this.particle == null || _newBlockValue.meta != 1)
            return;
        if (this.particle == "") return;
        debugHelper.doDebug(string.Format("PLANT GROWING: INFECTED"), !disableDebug);
        this.checkParticles(_world, _clrIdx, _blockPos, _newBlockValue);
    }
}

public class BlockMortePlantGrown : BlockCropsGrown
{
    private bool disableDebug = true;
    private int checkrange = 7;
    private float meshScale = 1;
    float minScale = 1;
    float maxScale = 1;
    private int plagueChance = 5;
    private int ratChance = 5;
    private int spreadChance = 15;
    private int deathChance = 10;
    private string particle = "#fly?FlyParticle";
    string entityGroup = "RatPlague";
    string antiPlagueDevice = "InsectLamp";
    string antiPestDevice = "UltraSound";
    private Vector3 UY;
    BlockValue lastBlockValue = BlockValue.Air;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "PLANT: " + str;
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
            {
                // Check if the game instance is not null
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
                }
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override void Init()
    {
        base.Init();
        // mesh size
        if (this.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = this.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }
        }
        if (this.Properties.Values.ContainsKey("ParticleName"))
            this.particle = this.Properties.Values["ParticleName"];
        if (!this.Properties.Values.ContainsKey("ParticleOffset"))
            this.UY = Utils.ParseVector3("0.0,0.5,0.0");
        else this.UY = Utils.ParseVector3(this.Properties.Values["ParticleOffset"]);
        if (this.Properties.Values.ContainsKey("plagueChance"))
            plagueChance = int.Parse(this.Properties.Values["plagueChance"]);
        if (this.Properties.Values.ContainsKey("ratChance"))
            ratChance = int.Parse(this.Properties.Values["ratChance"]);
        if (this.Properties.Values.ContainsKey("EntityGroup"))
        {
            entityGroup = this.Properties.Values["EntityGroup"];
        }
        this.IsRandomlyTick = true; // i want crops to tick, at least once. After that they can stop, unless they get infected, i dunno
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue,
        EntityAlive _player)
    {
        if (_blockValue.meta == 1)
        {
            DisplayToolTipText("Yuck! This plant is ruinied! No way i'm gonna eat that!");
            return false;
        }
        return base.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        int _damagePoints,
        int _entityIdThatDamaged, bool _bUseHarvestTool)
    {
        Entity player = _world.GetEntity(_entityIdThatDamaged);
        if (player is EntityPlayer)
        {
            //if (!_world.IsRemote())
            {
                ItemClass itemHeld = (player as EntityPlayer).inventory.holdingItem;
                if (itemHeld != null)
                {
                    if (itemHeld.Name == "SprayItem")
                    {
                        if (_blockValue.meta != 1)
                        {
                            debugHelper.doDebug(string.Format("PLANT GROWN: PLANT SPRAYED"), !disableDebug);
                            _blockValue.meta = 2;
                            _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                        }
                        return 0;
                    }
                }
            }
        }
        if (_blockValue.meta == 1)
        {
            // just turns to air
            _world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);
            return 0;
        }
        else
        {
            return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged,
               _bUseHarvestTool);
        }        
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if (_blockValue.meta != 1)
            return; // plant is NOT plagued
        if (_ebcd == null || !_ebcd.bHasTransform) return;
        meshScale = UnityEngine.Random.Range(minScale, maxScale);
        _ebcd.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
        MorteParticleEffect.addParticle(_blockValue, _ebcd, this.UY, particle, disableDebug);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        debugHelper.doDebug(string.Format("PLANT GROWN: VALUE CHANGED = {0}", _newBlockValue), !disableDebug);
        if (this.particle == null || _newBlockValue.meta != 1)
            return;
        if (this.particle == "") return;
        debugHelper.doDebug(string.Format("PLANT GROWN: INFECTED"), !disableDebug);
        this.checkParticles(_world, _clrIdx, _blockPos, _newBlockValue);
    }

    protected virtual void checkParticles(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (this.particle == null)
            return;
        if (!_world.IsRemote()) return;
        this.addParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
    }

    protected virtual void addParticles(WorldBase _world, int _clrIdx, int _x, int _y, int _z, BlockValue _blockValue)
    {
        if (!_world.IsRemote()) return;
        if (string.IsNullOrEmpty(particle))
            return;        
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(new Vector3i(_x, _y, _z));
        MorteParticleEffect.addParticle(_blockValue, _ebcd, this.UY, particle, disableDebug);
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
       // debugHelper.doDebug(string.Format("PLANT GROWN: Tick with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()), !disableDebug);
        if (_blockValue.meta == 0) // this is not bullet proof, if there is any grow on top stuff...
        {
            debugHelper.doDebug(string.Format("PLANT GROWN: DOES NOT COME FROM CROP WILL NOT TICK AGAIN"), !disableDebug);
            this.IsRandomlyTick = false;
            return false;
        }
        if (_world.IsRemote())
        {
            if (lastBlockValue.type != BlockValue.Air.type && _blockValue.type != BlockValue.Air.type)
            {
                if (lastBlockValue.meta != _blockValue.meta && _blockValue.meta == 1)
                {
                    debugHelper.doDebug(string.Format("PLANT GROWN WAS INFECTED"), !disableDebug);
                    checkParticles(_world, _clrIdx, _blockPos, _blockValue);
                }
            }
            lastBlockValue = _blockValue;
        }
        if (!_world.IsRemote())
        {
            bool flyPlague = false;
            bool ratPlague = false;
            bool plagued = false;
            System.Random rdnRandom = new Random();
            int roll = 0;
            if (_blockValue.meta == 1)
                plagued = true;

            if (!plagued)
            {
                int baseChance = plagueChance;
                baseChance = MorteSpawners.PlagueChance(_world, _clrIdx, _blockPos, _blockValue, baseChance, 5,
                    !disableDebug);
                // chance to crit - 2%
                if (rdnRandom.Next(1, 1001) < 20)
                    roll = rdnRandom.Next(1, 501);
                else roll = rdnRandom.Next(1, 1001);
                debugHelper.doDebug(
                    string.Format("PLANT GROWN: Tick with FLY CHANCE = {0} ROLL = {1}", baseChance, roll),
                    !disableDebug);
                flyPlague = (roll < baseChance);
                if (!flyPlague)
                {
                    roll = rdnRandom.Next(1, 101);
                    debugHelper.doDebug(
                        string.Format("PLANT GROWN: Tick with RAT CHANCE = {0} ROLL = {1}", ratChance, roll),
                        !disableDebug);
                    ratPlague = (roll < ratChance);
                }
            }
            // check for plague
            if (flyPlague)
            {
                // check if there's any powered light
                if (antiPlagueDevice != "")
                {
                    if (MorteSpawners.FindTrap(_world, _clrIdx, _blockPos, 10, antiPlagueDevice, !disableDebug))
                    {
                        // reduces to a residual 1%
                        roll = rdnRandom.Next(1, 1001);
                        debugHelper.doDebug(string.Format("PLANT GROWN: Tick with FLY CHANCE = 10 ROLL = {0}", roll),
                            !disableDebug);
                        flyPlague = (roll < 10);
                    }
                }
                if (flyPlague)
                {
                    _blockValue.meta = 1;
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                    if (!this.IsRandomlyTick)
                        _world.GetWBT()
                            .AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, (ulong)_rnd.Next(500, 3000));
                    return true;
                }
            }
            // rats - it keeps growing though
            if (ratPlague)
            {
                // check if there's any powered ultra sound
                if (antiPestDevice != "")
                {
                    if (MorteSpawners.FindTrap(_world, _clrIdx, _blockPos, 10, antiPestDevice, !disableDebug))
                    {
                        // reduces to a residual 1%
                        roll = rdnRandom.Next(1, 1001);
                        debugHelper.doDebug(string.Format("PLANT GROWN: Tick with RAT CHANCE = 10 ROLL = {0}", roll),
                            !disableDebug);
                        ratPlague = (roll < 10);
                    }
                }
                if (ratPlague)
                {
                    // spawn rats and make them check this spot.                
                    MorteSpawners.SpawnGroupToPos(_world, _clrIdx, _blockPos, entityGroup, 10, 15, 10, !disableDebug);
                }
            }
            // if already plagued, it will eventually die
            if (plagued)
            {
                roll = rdnRandom.Next(1, 101);
                debugHelper.doDebug(
                    string.Format("PLANT GROWN: Tick with DEATH CHANCE = {0} ROLL = {1}", deathChance, roll),
                    !disableDebug);
                bool die = (roll < deathChance);
                //if (!die)
                {
                    // spread plague on nearby plants (up to 3 blocks away)
                    spreadChance = rdnRandom.Next(15, 25);
                    roll = rdnRandom.Next(1, 101);
                    debugHelper.doDebug(
                        string.Format("PLANT GROWN: Tick with SPREAD CHANCE = {0} ROLL = {1}", spreadChance, roll),
                        !disableDebug);
                    if (roll < spreadChance)
                    {
                        Vector3i _plaguePos = Vector3i.zero;
                        if (MorteSpawners.SpreadPlague(_world, _clrIdx, _blockPos, 2, rdnRandom, out _plaguePos, !disableDebug))
                        {
                            BlockValue block = _world.GetBlock(_clrIdx, _plaguePos);
                            block.meta = 1;
                            _world.SetBlockRPC(_clrIdx, _plaguePos, block);
                        }
                    }
                    if (!this.IsRandomlyTick)
                        _world.GetWBT()
                            .AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, (ulong) _rnd.Next(2500, 5500));
                }
                if (die)
                {
                    //removeParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
                    debugHelper.doDebug("Plant dying", disableDebug);
                    _world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);
                }
                return true;
            }
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }
}

public class BlockMorteTreeGrown : BlockModelTreeEx
{
    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "PLANT: " + str;
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
            {
                // Check if the game instance is not null
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
                }
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //debugHelper.doDebug(string.Format("TREE GROWN: Tick with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()), !disableDebug);
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }
}

public class BlockMorteTreeGrowing : BlockModelTreeEx
{
    private bool disableDebug = true;
    private int checkrange = 7;
    private float meshScale = 1;
    float minScale = 1;
    float maxScale = 1;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "PLANT: " + str;
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
            {
                // Check if the game instance is not null
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
                }
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override void Init()
    {
        base.Init();
        // mesh size
        if (this.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = this.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if (_ebcd == null || !_ebcd.bHasTransform) return;
        meshScale = UnityEngine.Random.Range(minScale, maxScale);
        _ebcd.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //debugHelper.doDebug(string.Format("TREE GROWNING: Tick with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()), disableDebug);
        if (WeatherManager.theInstance.GetCurrentRainfallValue() == 0.0F)
        {
            //Debug.Log(string.Format("Check for water"));
            //if it's not raining, then check if there's any liquid near it
            if (!this.CheckWaterNear(_world, _clrIdx, _blockPos))
            {
                //Debug.Log(string.Format("NO WATER"));
                DisplayChatAreaText("No water near");
                if (!this.isPlantGrowingRandom)
                    _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
                return true;
            }
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        //debugHelper.doDebug(string.Format("TREE GROWNING: COLLIDE with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()), !disableDebug);
        bool doDamage = false;
        if (this.Properties.Values.ContainsKey("doDamage"))
        {
            if (bool.TryParse(this.Properties.Values["doDamage"], out doDamage) == false) doDamage = false;
        }
        if (doDamage)
        {
            if (!_entity.IsAlive()) return false;
            if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
                this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        }
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    private bool CheckWaterNear(WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        //string blocks = "";
        for (int i = _blockPos.x - checkrange; i <= (_blockPos.x + checkrange); i++)
        {
            for (int j = _blockPos.z - checkrange; j <= (_blockPos.z + checkrange); j++)
            {
                for (int k = _blockPos.y - checkrange; k <= (_blockPos.y + checkrange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    //blocks = blocks + block.type + ", ";
                    if (Block.list[block.type].blockMaterial.IsLiquid)
                    {
                        // deplete water
                        System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
                        if (Rand.Next(0, 100) < 20)
                        {
                            DisplayChatAreaText("Consume water");
                            Block.list[block.type].DoExchangeAction(_world, new Vector3i(i, k, j), block, "deplete1", 1);
                            BlockLiquidv2.DepleteFromBlock(block, new Vector3i(i, j, k));
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }
}