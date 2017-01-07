using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Random = System.Random;

public class BlockNuke : Block
{
    private bool disableDebug = true;

    private ExplosionData CS;

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
            str = "NUKE: " + str;
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
        this.CS = new ExplosionData(this.Properties);
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityId, bool _bUseHarvestTool)
    {
        if (_damagePoints > 0)
        {
            //DisplayChatAreaText("CABOOOM");            
            this.CC(_world, _clrIdx, _blockPos, _entityId, 0.0f);
        }
        else DisplayChatAreaText("DAMAGE");
        return 0;
    }

    public override void OnBlockRemoved(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText("OnBlockRemoved");
        base.OnBlockRemoved(_world, _chunk, _blockPos, _blockValue);
        // spawn raid air here
        // turn into air
        //DisplayChatAreaText("SPAWN RADIATED AIR");
        BlockValue offBlock = Block.GetBlockValue("RadAir");
        Block block = Block.list[offBlock.type];
        // spawns it on top of (if anything is there, it will be destroyed anyway
        Vector3i position = new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z);
        _world.SetBlockRPC(_chunk.ClrIdx, position, offBlock, MarchingCubes.DensityAir);
    }

    private void CC([In] WorldBase obj0, [In] int obj1, [In] Vector3i obj2, [In] int obj3, [In] float obj4)
    {
        ChunkCluster chunkCluster = obj0.ChunkClusters[obj1];
        Vector3 _worldPos = obj2.ToVector3();
        if (chunkCluster != null)
            _worldPos = chunkCluster.ToWorldPosition(obj2.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
        obj0.GetGameManager().ExplosionServer(obj1, _worldPos, obj2, Quaternion.identity, this.CS, obj3, obj4, true, (ItemValue)null);        
    }    
}

public class BlockRadAir : BlockUpgradeRated
{
    static bool bTipAction = true;

    private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);
    
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
            str = "RADAIR: " + str;
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

    private string QS;
    private Vector3 JS;

    public BlockRadAir()
    {
        this.IsNotifyOnLoadUnload = true;
    }

    public override void Init()
    {
        base.Init();        
        this.StabilitySupport = false;
        
        this.MaxDamage = 999999;
        this.FallDamage = false;
        if (this.Properties.Values.ContainsKey("ParticleName"))
            this.QS = this.Properties.Values["ParticleName"];
        if (!this.Properties.Values.ContainsKey("ParticleOffset"))
            return;
        this.JS = Utils.ParseVector3(this.Properties.Values["ParticleOffset"]);
    }

    public override void LateInit()
    {
        base.LateInit();
        this.DowngradeBlock= Block.GetBlockValue("RadAir"); // downgrades to self. no way to destroy
        this.MaxDamage = 999999;
        this.FallDamage = false;
        if (this.Properties.Values.ContainsKey("ParticleName"))
            this.QS = this.Properties.Values["ParticleName"];
        if (!this.Properties.Values.ContainsKey("ParticleOffset"))
            return;
        this.JS = Utils.ParseVector3(this.Properties.Values["ParticleOffset"]);
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints,
        int _entityIdThatDamaged, bool _bUseHarvestTool)
    {
        _blockValue.damage = 0;
        _damagePoints = 0;
        return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _bUseHarvestTool);
    }

    public override bool IsExplosionAffected()
    {
        return false;
    }

    public override void OnBlockStartsToFall(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
    {
        //DisplayChatAreaText("STARTS TO FALL OVERRIDE");
    }

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText("RADIATED AIR ADDED");
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
        this.checkParticles(_world, _chunk.ClrIdx, _blockPos, _blockValue);
    }

    public override void OnBlockRemoved(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText("OnBlockRemoved");
        int halfTicks = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(this.upgradeRate) / 2));        
        base.OnBlockRemoved(_world, _chunk, _blockPos, _blockValue);
        this.removeParticles(_world, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
        MemoryPools.Cleanup();
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        if (_blockValue.meta2 == 0)
        {
            // play the animation
            Animator[] componentsInChildren;
            if (_ebcd == null || !_ebcd.bHasTransform ||
                (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
                return;
            DisplayChatAreaText("FOUND ANIMATOR");
            foreach (Animator animator in componentsInChildren)
            {
                DisplayChatAreaText("CABOOM ANIMATION");
                animator.SetTrigger("triggerExplode");
                // save the animation state so that it does not play again.
                _blockValue.meta2 = 1;
                GameManager.Instance.World.SetBlockRPC(_ebcd.pos, _blockValue);
            }
        }
        base.ForceAnimationState(_blockValue, _ebcd);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        Transform blockParticleEffect;
        if ((int)_oldBlockValue.rotation == (int)_newBlockValue.rotation || this.QS == null || !((UnityEngine.Object)(blockParticleEffect = _world.GetGameManager().GetBlockParticleEffect(_blockPos)) != (UnityEngine.Object)null))
            return;
        Vector3 particleOffset1 = this.getParticleOffset(_oldBlockValue);
        Vector3 particleOffset2 = this.getParticleOffset(_newBlockValue);
        blockParticleEffect.localPosition -= particleOffset1;
        blockParticleEffect.localPosition += particleOffset2;
        blockParticleEffect.localRotation = this.shape.GetRotation(_newBlockValue);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        this.checkParticles(_world, _clrIdx, _blockPos, _blockValue);
    }

    public override void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
        this.removeParticles(_world, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        DisplayChatAreaText("tick");
        DamageEntities(_world, _clrIdx, _blockPos);
        // change ground blocks to radiated, progressivly - only up to 5 blocks per tick?
        int halfTicks = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(this.upgradeRate)/2));        
        if (halfTicks > 0)
        {
            DisplayChatAreaText("HALF TICKS = " + halfTicks + "TICK = " + (int) _blockValue.meta);
            if ((int) _blockValue.meta < halfTicks)
                DamageBlocks(_world, _clrIdx, _blockPos, false, halfTicks, (int)_blockValue.meta + 1); // radiaties surrounding progressively
            else DamageBlocks(_world, _clrIdx, _blockPos, true, halfTicks, (int)_blockValue.meta + 1 - halfTicks); // starts reversing back.
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    // searches for entities withing radiation range, and do radiation damage to them
    private void DamageEntities(WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        int radDamage = 0;
        if (this.Properties.Values.ContainsKey("RadiationDamage"))
        {
            if (int.TryParse(this.Properties.Values["RadiationDamage"], out radDamage) == false) radDamage = 0;
        }
        int radRadius = 0;
        if (this.Properties.Values.ContainsKey("RadiationEntityRadius"))
        {
            if (int.TryParse(this.Properties.Values["RadiationEntityRadius"], out radRadius) == false) radRadius = 0;
        }
        if (radRadius > 0 && radDamage > 0)
        {
            // i can try to use this instead of checking ALL entities
            // using the tag E_BP_Body because I only want to hit them ONCE
            //foreach (Component component1 in Physics.OverlapSphere(_blockPos.ToVector3(), (float) radRadius, -1216517))
            //{
            //    Transform transform1 = component1.transform;
            //    //if (transform1.tag.StartsWith("E_") || transform1.CompareTag("Item"))
            //    DisplayChatAreaText(transform1.tag);
            //    if (transform1.tag.StartsWith("E_"))
            //    {
            //        {                        
            //            Transform transform2 = (Transform) null;
            //            if (transform1.tag.StartsWith("E_BP_"))
            //                transform2 = GameUtils.GetHitRootTransform(transform1.tag, transform1);
            //            EntityAlive entityAlive = !((UnityEngine.Object) transform2 != (UnityEngine.Object) null)
            //                ? (EntityAlive) null
            //                : transform2.GetComponent<EntityAlive>();                        
            //            if (!((UnityEngine.Object) entityAlive == (UnityEngine.Object) null) && !entityAlive.IsDead())
            //            {
            //                DisplayChatAreaText("FOUND ALIVE ENTITY " + entityAlive.EntityName);
            //                //entityAlive.DamageEntity(DamageSource.radiation, radDamage, false, 1f);
            //            }
            //        }
            //    }
            //}

            // search by entities in the surrounding and do radiation damage..
            foreach (Entity entity in GameManager.Instance.World.Entities.list)
            {
                if (entity.IsAlive())
                {
                    try
                    {
                        float distance = Math.Abs(Vector3.Distance(entity.position, _blockPos.ToVector3()));
                        if (distance <= radRadius && (entity is EntityAlive))
                        {
                            EntityAlive ent = (entity as EntityAlive);
                            //DisplayChatAreaText("FOUND ALIVE ENTITY " + ent.name);                            
                            // do radiation damage..
                            ent.DamageEntity(DamageSource.radiation, radDamage, false, 1f);
                            //Apply radiation buff ?
                        }
                    }
                    catch (Exception)
                    {
                        DisplayChatAreaText("Couldnt do damage");
                    }
                }
            }
        }
    }

    public void DamageBlocks(WorldBase _world, int _clrIdx, Vector3i _blockPos, bool reverse, int halfTicks, int currentTick)
    {
        int radDamage = 0;
        if (this.Properties.Values.ContainsKey("RadiationDamage"))
        {
            if (int.TryParse(this.Properties.Values["RadiationDamage"], out radDamage) == false) radDamage = 0;
        }

        //int num1 = (int)this.E.BlockDamage;
        //int num3 = this.E.BlockRadius;
        int radRadius = 0;
        if (this.Properties.Values.ContainsKey("RadiationBlockRadius"))
        {
            if (int.TryParse(this.Properties.Values["RadiationBlockRadius"], out radRadius) == false) radRadius = 0;
        }

        if (radDamage == 0 || radRadius == 0) return;

        int maxRange = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(radRadius / halfTicks))) * currentTick;

        DisplayChatAreaText("MAXRANGE = " + maxRange);

        radRadius = maxRange;

        if (radRadius<=0) return;

        ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];

        //float num4 = (float)(Utils.FastMax(radDamage, 1) / radRadius);
        string offBlockName = "radiated";
        if (reverse) offBlockName = "burntForestGround";

        // blocks to replace
        List<string> list = new List<string>();
        if (!reverse)
        {
            list.Add("snow");
            list.Add("clay");
            list.Add("grass");
            list.Add("plainsGround");
            list.Add("desertGround");
            list.Add("forestGround");
            list.Add("dirt");
            list.Add("destroyedStone");
            list.Add("burntForestGround");
            list.Add("asphalt");            
        }
        else list.Add("radiated");

        BlockValue offBlock = Block.GetBlockValue(offBlockName);


        int num1 = radDamage;
        int num3 = radRadius;
        float num4 = (float)(Utils.FastMax(num1, 1) / radRadius);

        for (int index1 = 0; index1 < num3; ++index1)
        {
            for (int index2 = 0; index2 < num3; ++index2)
            {
                for (int index3 = 0; index3 < num3; ++index3)
                {
                    if (index1 == 0 || index1 == num3 - 1 || (index2 == 0 || index2 == num3 - 1) || (index3 == 0 || index3 == num3 - 1))
                    {
                        float num2 = (float)num1;
                        Vector3 normalized = new Vector3((float)((double)index1 / ((double)num3 - 1.0) * 2.0 - 1.0), (float)((double)index2 / ((double)num3 - 1.0) * 2.0 - 1.0), (float)((double)index3 / ((double)num3 - 1.0) * 2.0 - 1.0)).normalized;
                        Vector3 _worldPos = _blockPos.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f);
                        while ((double)num2 > 0.899999976158142)
                        {
                            Vector3i vector3i = World.worldToBlockPos(_worldPos);
                            BlockValue _blockValue = chunkCluster.GetBlock(vector3i);
                            int hashCode = vector3i.GetHashCode();
                            //DisplayChatAreaText("seeing " + Block.list[_blockValue.type].GetBlockName());


                            if (list.Contains(Block.list[_blockValue.type].GetBlockName()))
                            {
                                //DisplayChatAreaText("transform " + Block.list[_blockValue.type].GetBlockName());
                                // if block is terrain, change it to radiated or back to burned forest ground.   
                                Block block = Block.list[offBlock.type];
                                _world.SetBlockRPC(chunkCluster.ClusterIdx, vector3i, offBlock);
                            }

                            _worldPos += normalized * 0.5f;
                            num2 -= num4;
                        }
                    }
                }
            }
        }
        list.Clear();
        list = null;
    }

    protected virtual Vector3 getParticleOffset(BlockValue _blockValue)
    {
        return this.shape.GetRotation(_blockValue) * (this.JS - new Vector3(0.5f, 0.5f, 0.5f)) + new Vector3(0.5f, 0.5f, 0.5f);
    }

    protected virtual void checkParticles(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (this.QS == null || _world.GetGameManager().HasBlockParticleEffect(_blockPos))
            return;
        this.addParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
    }

    protected virtual void addParticles(WorldBase _world, int _clrIdx, int _x, int _y, int _z, BlockValue _blockValue)
    {
        float num = 0.0f;
        if (_y > 0 && Block.list[_blockValue.type].IsTerrainDecoration && Block.list[_world.GetBlock(_x, _y - 1, _z).type].shape.IsTerrain())
            num = MarchingCubes.GetDecorationOffsetY(_world.GetDensity(_clrIdx, _x, _y, _z), _world.GetDensity(_clrIdx, _x, _y - 1, _z));
        _world.GetGameManager().SpawnBlockParticleEffect(new Vector3i(_x, _y, _z), new ParticleEffect(this.QS, new Vector3((float)_x, (float)_y + num, (float)_z) + this.getParticleOffset(_blockValue), this.shape.GetRotation(_blockValue), 1f, Color.white));
    }

    protected virtual void removeParticles(WorldBase _world, int _x, int _y, int _z, BlockValue _blockValue)
    {
        _world.GetGameManager().RemoveBlockParticleEffect(new Vector3i(_x, _y, _z));
        alzheimer();
    }

    private static void alzheimer()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        //SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
    }    

}

public class BlockCanon : Block
{
    ItemActionCatapult itemActionCatapult;

    //public class BlockTrapOn : BlockUpgradeRated
    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "CANON: " + str;
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
        itemActionCatapult.ReadFrom(this.Properties);
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        // fire or reload cannon
        DisplayChatAreaText("!!!FIRE CABOOM!!!");
        // for now, use rocket launcher
        ItemClass fireItem = ItemClass.GetItemClass("rocketLauncher");        
        itemActionCatapult.ExecuteAction((ItemActionData) null, false);
        //fireItem.Actions[0].ExecuteAction(new ItemActionData(new ItemInventoryData(fireItem, new ItemStack(fireItem.), ), ), false);
        return true;
        //return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }
}