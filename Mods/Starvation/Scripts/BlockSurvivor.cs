using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BlockSurvivor : Block
{
    private bool disableDebug = true;

    SurvivorScript script;
    UnityEngine.GameObject gameObject;

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if (!_world.IsRemote())
        // THE SCRIPT WILL ALWAYS RUN, but only does certain things remotely/locally
        {
            debugHelper.doDebug("BLOCKSURVIVOR: OnBlockValueChanged", !disableDebug);
            // check bit 1
            if (AddScript(_newBlockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                // get the transform
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null)
                {
                    try
                    {
                        debugHelper.doDebug("BLOCKSURVIVOR: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<SurvivorScript>();
                        if (script == null)
                        {
                            debugHelper.doDebug("BLOCKSURVIVOR: OnBlockValueChanged - ADDING SCRIPT?", !disableDebug);
                            AddScriptToObject(_world, _blockPos, _clrIdx, _ebcd);
                        }
                        else debugHelper.doDebug("BLOCKSURVIVOR: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?", !disableDebug);
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug("Error OnBlockValueChanged - " + ex.Message, false);
                    }
                }
                // resets the bit
                _newBlockValue.meta2 = (byte)(_newBlockValue.meta2 & ~(1 << 1));
                _world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
            }
        }
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        //AddScriptToObject(_world, _blockPos, _cIdx, _ebcd);
        // set bit to 1
        if (!AddScript(_blockValue.meta2))
        {
            _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 1));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }
    }

    private void AddScriptToObject(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockEntityData _ebcd)
    {
        gameObject = _ebcd.transform.gameObject;
        script = gameObject.AddComponent<SurvivorScript>();
        // initialize script vars
        int checkInterval = 30;
        if (this.Properties.Values.ContainsKey("CheckInterval"))
        {
            if (int.TryParse(this.Properties.Values["CheckInterval"], out checkInterval) == false) checkInterval = 30;
        }
        int checkArea = 10;
        if (this.Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(this.Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        string foodLow = "";
        string foodMed = "";
        string foodHigh = "";
        string foodContainer = "";
        if (this.Properties.Values.ContainsKey("FoodLow"))
            foodLow = this.Properties.Values["FoodLow"];
        if (this.Properties.Values.ContainsKey("FoodMedium"))
            foodMed = this.Properties.Values["FoodMedium"];
        if (this.Properties.Values.ContainsKey("FoodHigh"))
            foodHigh = this.Properties.Values["FoodHigh"];
        if (this.Properties.Values.ContainsKey("FoodContainer"))
            foodContainer = this.Properties.Values["FoodContainer"];
        script.init(_world, _blockPos, _cIdx, checkInterval, checkArea, foodContainer, foodLow, foodMed, foodHigh);
    }

    public override ItemStack OnBlockPickedUp(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId)
    {
        // returns a stack with the object current itemvalue
        // puts the right item in
        string PickedUpItemValueAux = null;
        if (_blockValue.meta == 1)
            PickedUpItemValueAux = "survivorMale";
        else if (_blockValue.meta == 2)
            PickedUpItemValueAux = "survivorFemale";
        else PickedUpItemValueAux = "survivorTeen";
        //ItemStack survivorObj = new ItemStack(ItemClass.GetItem("survivorItem"), 1);
        //survivorObj.itemValue = _blockValue.ToItemValue();
        //return survivorObj;
        ItemStack stack = this.PickupTarget != null ? new ItemStack(new ItemValue(ItemClass.GetItem(this.PickupTarget).type, false), 1) : new ItemStack(PickedUpItemValueAux != null ? ItemClass.GetItem(PickedUpItemValueAux) : _blockValue.ToItemValue(), 1);
        if (stack != null)
        {
            stack.itemValue.Meta = _blockValue.meta;
            stack.itemValue.UseTimes = _blockValue.damage;
        }
        return stack;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, System.Random _rnd)
    {
        int charValue = _bpResult.blockValue.meta;
        if (_bpResult.blockValue.meta == 0)
        {
            debugHelper.doDebug("BLOCKSURVIVOR: NEW Type is " + charValue, !disableDebug);
            System.Random _random = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            charValue = _random.Next(1, 90);
            if (charValue <= 30) charValue = 1;
            else if (charValue <= 60) charValue = 2;
            else charValue = 3;
            _bpResult.blockValue.meta = (byte) charValue;            
        }
        debugHelper.doDebug("BLOCKSURVIVOR: Type is " + charValue, !disableDebug);
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }
}

// Radio Tower is used to "attract" survivors.
// It will randomly spawn survivors up to a maxnumber as long as it is powered
// This block CAN ONLY BE PLACED ON CONTROLLED TERRITORY!
public class BlockRadioTower : Block
{

    TowerScript script;
    UnityEngine.GameObject gameObject;
    private bool disableDebug = true;
    private int maxLevel = 10;

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
            str = "RADIO TOWER: " + str;
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

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if (!_world.IsRemote())
        // THE SCRIPT WILL ALWAYS RUN, but only does certain things remotely/locally
        {
            debugHelper.doDebug("BLOCKRADIOTOWER: OnBlockValueChanged", !disableDebug);
            // check bit 1
            //if (AddScript(_newBlockValue.meta) && !AddScript(_oldBlockValue.meta))
            {
                // get the transform
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null)
                {
                    try
                    {
                        debugHelper.doDebug("BLOCKRADIOTOWER: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<TowerScript>();
                        if (script == null)
                        {
                            debugHelper.doDebug("BLOCKRADIOTOWER: OnBlockValueChanged - ADDING SCRIPT?", !disableDebug);
                            AddScriptToObject(_world, _blockPos, _clrIdx, _newBlockValue, _ebcd);
                        }
                        else debugHelper.doDebug("BLOCKRADIOTOWER: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?", !disableDebug);
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug("BLOCKRADIOTOWER: Error OnBlockValueChanged - " + ex.Message, false);
                    }
                }
                // resets the bit
                //_newBlockValue.meta = (byte)(_newBlockValue.meta & ~(1 << 1));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
            }
        }
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        debugHelper.doDebug("BLOCKRADIOTOWER: OnBlockEntityTransformAfterActivated with META2 = " + _blockValue.meta2, !disableDebug);
        // set bit to 1
        //if (!AddScript(_blockValue.meta))
        {
            //_blockValue.meta = (byte)(_blockValue.meta2 | (1 << 1));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }

        //AddScriptToObject(_world, _blockPos, _cIdx, _blockValue, _ebcd);
    }

    private void AddScriptToObject(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue,
        BlockEntityData _ebcd)
    {
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        int spawnChance = 50;
        if (this.Properties.Values.ContainsKey("SpawnChance"))
        {
            if (int.TryParse(this.Properties.Values["SpawnChance"], out spawnChance) == false) spawnChance = 50;
        }
        int spawnInterval = 30;
        if (this.Properties.Values.ContainsKey("SpawnInterval"))
        {
            if (int.TryParse(this.Properties.Values["SpawnInterval"], out spawnInterval) == false) spawnInterval = 30;
        }
        int spawnArea = 10;
        if (this.Properties.Values.ContainsKey("SpawnArea"))
        {
            if (int.TryParse(this.Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 10;
        }
        int maxSpawn = 3;
        if (this.Properties.Values.ContainsKey("MaxSpawn"))
        {
            if (int.TryParse(this.Properties.Values["MaxSpawn"], out maxSpawn) == false) maxSpawn = 3;
        }
        bool isPowered = false;
        if (this.Properties.Values.ContainsKey("isPowered"))
        {
            if (bool.TryParse(this.Properties.Values["isPowered"], out isPowered) == false) isPowered = false;
        }
        string entityGroup = "";
        if (this.Properties.Values.ContainsKey("EntityGroup"))
        {
            entityGroup = this.Properties.Values["EntityGroup"];
        }
        gameObject = _ebcd.transform.gameObject;
        script = gameObject.AddComponent<TowerScript>();
        script.init(_world, _blockPos, _cIdx, spawnInterval, spawnChance, spawnArea, maxSpawn, isPowered, entityGroup);
    }

    #region Check Parent;
    // make sure it doesn't come back to the same one as before
    private int GetParent(WorldBase _world, int _cIdx, Vector3i _blockPos)
    {
        int result = 0;
        // it only alows to go up, down, left, right, forward and back to facilitate navigation
        if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z), 1)) result = 1; // UP
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z), 2)) result = 2; // DOWN
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x - 1, _blockPos.y, _blockPos.z), 3)) result = 3; // LEFT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x + 1, _blockPos.y, _blockPos.z), 4)) result = 4; // RIGHT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z + 1), 5)) result = 5; // FORWARD
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z - 1), 6)) result = 6; // BACK

        return result;
    }

    private bool CheckParentBlock(WorldBase _world, int _cIdx, Vector3i _blockCheck, int direction)
    {
        bool result = false;

        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        // if block is compatible
        string powerType = "Electric";
        if ((blockAux is BlockGenerator && powerType == "Electric") || blockAux is BlockPowerLine)
        {
            if (powerType != "")
            {
                // if for some reason the type does not match
                if (blockAux is BlockPowerLine)
                {
                    if ((blockAux as BlockPowerLine).GetPowerType() != "Electric") return false;
                }
                else if (blockAux is BlockValve)
                {
                    if ((blockAux as BlockValve).GetPowerType() != "Electric") return false;
                }
            }
            // check if that block is not child
            BlockValue blkValue = _world.GetBlock(_cIdx, _blockCheck);
            int directionParent = blkValue.meta2;
            // check if I don't create a loop in line
            // for example if this wants to go up, the one up must not want to down
            if ((direction == 1 && directionParent != 2) || (direction == 2 && directionParent != 1) ||
                (direction == 3 && directionParent != 4) || (direction == 4 && directionParent != 3) ||
                (direction == 5 && directionParent != 6) || (direction == 6 && directionParent != 5))
            {
                // no loop, can place
                // can use this direction as parent
                result = true;
            }
        }
        return result;
    }
    #endregion;

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any powerline, generator or accumulator as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
        bool isPowered = false;
        if (this.Properties.Values.ContainsKey("isPowered"))
        {
            if (bool.TryParse(this.Properties.Values["isPowered"], out isPowered) == false) isPowered = false;
        }
        if (isPowered)
        {
            int parentPosition = GetParent(_world, _clrIdx, _blockPos);
            if (parentPosition > 0)
            {
                //parentPos = parentPosition;
                //_blockValue.meta2 = (byte)parentPosition;            
                // it just saves the direction of the parent
                // 1 - UP; 2 - DOWN; 3 - LEFT; 4 - RIGHT; 5 - FORWARD; 6 - BACK
                // diagonals are not alowed                        
                result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
            }
            else
            {
                DisplayToolTipText("This block can only be placed next to an electric line or generator");
            }
        }
        else result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        return result;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        bool isPowered = false;
        if (this.Properties.Values.ContainsKey("isPowered"))
        {
            if (bool.TryParse(this.Properties.Values["isPowered"], out isPowered) == false) isPowered = false;
        }
        if (isPowered)
        {
            if (_bpResult.blockValue.meta2 <= 0) // no parent, but MUST have one!
            {
                int parentPosition = GetParent(_world, _bpResult.clrIdx, _bpResult.blockPos);
                _bpResult.blockValue.meta2 = (byte) parentPosition;
                DisplayChatAreaText(string.Format("parent defined to {0}", _bpResult.blockValue.meta2));
            }
        }
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }
}


// This block is to be used for advanced crafters
// You can configure the group, ActionSkillGroup, or CraftingSkillGroup  of stuff it can learn,f.e, "Rifles, Pistols", "Food/Cooking" or "Gun Smithing"
// The crafter will then try to learn stuff that belongs to those groups AND has a receipt.
public class BlockCrafter : BlockSecureLoot
{
    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;
    UnityEngine.GameObject gameObject;
    CrafterWorkScript script;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "CRAFTER: " + str;
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

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if (!_world.IsRemote())
        // THE SCRIPT WILL ALWAYS RUN, but only does certain things remotely/locally
        {
            debugHelper.doDebug("BLOCKCRAFTER: OnBlockValueChanged", !disableDebug);
            // check bit 1
            //if (AddScript(_newBlockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                // resets the bit
                //_newBlockValue.meta2 = (byte)(_newBlockValue.meta2 & ~(1 << 1));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
                // get the transform
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null)
                {
                    try
                    {
                        debugHelper.doDebug("BLOCKCRAFTER: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<CrafterWorkScript>();
                        if (script == null)
                        {
                            debugHelper.doDebug("BLOCKCRAFTER: OnBlockValueChanged - ADDING SCRIPT?", !disableDebug);
                            script = gameObject.AddComponent<CrafterWorkScript>();
                            TileEntitySecureLootContainer secureLootContainer =
                                _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
                            script.initialize(secureLootContainer, _world, _blockPos, _clrIdx);
                        }
                        else debugHelper.doDebug("BLOCKCRAFTER: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?", !disableDebug);
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug("BLOCKCRAFTER: Error OnBlockValueChanged - " + ex.Message, false);
                    }
                }                
            }
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // every time this happens?
        // set bit to 1
        //if (!AddScript(_blockValue.meta2))
        {
            //_blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 1));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }      
    }
    // if a user tries to "open" a crafter he will get a textbox showing what he know, and what he is assinged to
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
        string keybindString = GUI_2.UIUtils.GetKeybindString(((EntityPlayerLocal)_entityFocusing).playerInput.Activate);
        if (secureLootContainer == null)
            return string.Empty;
        string str = Localization.Get(Block.list[_blockValue.type].GetBlockName(), string.Empty);
        if (!secureLootContainer.IsLocked())
            return string.Format(Localization.Get("tooltipUnlocked", string.Empty), keybindString, (object)str);
        return string.Format(Localization.Get("tooltipLocked", string.Empty), keybindString, (object)str);
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        //return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        debugHelper.doDebug("OnBlockActivated START", !disableDebug);
        try
        {
            BlockValue block = _world.GetBlock(_blockPos.x, _blockPos.y - 1, _blockPos.z);
            BlockEntityData _ebcd = _world.ChunkClusters[_cIdx].GetBlockEntity(_blockPos);
            if (_ebcd != null)
            {
                gameObject = _ebcd.transform.gameObject;
                if (Block.list[block.type].HasTag(BlockTags.Door))
                {
                    _blockPos = new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z);
                    return this.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                }
                TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntitySecureLootContainer;
                if (secureLootContainer == null)
                    return false;
                _player.AimingGun = false;
                //Vector3i _blockPos1 = secureLootContainer.ToWorldPos();
                //secureLootContainer.bWasTouched = secureLootContainer.bTouched;
                //_world.GetGameManager().TELockServer(_cIdx, _blockPos1, secureLootContainer.entityId, _player.entityId);
                // custom behaviour - opens a custom UI            
                CrafterInvScript scriptInv;

                scriptInv = gameObject.GetComponent<CrafterInvScript>();
                if (scriptInv != null)
                {
                    scriptInv.KillScript();
                    scriptInv = this.gameObject.AddComponent<CrafterInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
                else
                {
                    scriptInv = this.gameObject.AddComponent<CrafterInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            debugHelper.doDebug("Error OnBlockActivated - " + ex.Message, false);
            return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        }
    }

    // Check if there is any survivor (male) that can assume this position.
    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        if (result)
        {
            string requirement = "Any";
            if (this.Properties.Values.ContainsKey("Requires"))
                requirement = this.Properties.Values["Requires"];
            result = CheckSurvivor(_world, _clrIdx, _blockPos, requirement, false);
            if (requirement == "Any") requirement = " ";
            else requirement = " " + requirement + " ";
            if (!result) DisplayToolTipText(string.Format("You need at least one {0} survivor nearby to place this workbench.", requirement));
        }
        return result;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        // it's here that I remove the survivor
        string requirement = "Any";
        if (this.Properties.Values.ContainsKey("Requires"))
            requirement = this.Properties.Values["Requires"];
        bool result = CheckSurvivor(_world, _bpResult.clrIdx, _bpResult.blockPos, requirement, true);
        if (result)
            base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    private bool CheckSurvivor(WorldBase world, int cIdx, Vector3i _blockPos, string requirement, bool placing)
    {
        int spawnArea = 10;
        if (this.Properties.Values.ContainsKey("SpawnArea"))
        {
            if (int.TryParse(this.Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 5;
        }
        int survivorType = 0;        
        if (requirement == "Male") survivorType = 1;
        else if (requirement == "Female") survivorType = 2;
        else if (requirement == "Teen") survivorType = 3;
        int numBlocks = 0;
        // look for a specific survivor type nearby
        using (
                        List<Entity>.Enumerator enumerator =
                            GameManager.Instance.World.GetEntitiesInBounds(typeof(EntitySurvivorMod),
                                BoundsUtils.BoundsForMinMax(_blockPos.x - spawnArea, _blockPos.y - spawnArea,
                                    _blockPos.z - spawnArea, _blockPos.x + spawnArea,
                                    _blockPos.y + spawnArea,
                                    _blockPos.z + spawnArea)).GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                EntitySurvivorMod _other = enumerator.Current as EntitySurvivorMod;
                if (_other.IsAlive() && ((_other.IsMale && survivorType==1) || (!_other.IsMale && survivorType == 2) || (!_other.IsMale && survivorType == 3 && _other.EntityName=="Girl1")))
                {
                    if (placing)
                    {
                        // send dmg to the survivor to destroy it
                        DamageResponse dmg = new DamageResponse();
                        dmg.Strength = 6001;
                        dmg.Source = new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId);
                        dmg.Critical = false;
                        dmg.ImpulseScale = 1;
                        dmg.CrippleLegs = false;
                        dmg.Dismember = false;
                        dmg.Fatal = false;
                        dmg.TurnIntoCrawler = false;
                        if (world.IsRemote())
                        {
                            NetPackage _package1 = (NetPackage)new NetPackageDamageEntity(_other.entityId, dmg);
                            GameManager.Instance.SendToServer(_package1);
                        }
                        else
                            _other.DamageEntity(new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId), 6001,
                                false, 1);
                    }
                    return true;
                }
            }
        }

        //BlockValue survivorBlock = Block.GetBlockValue("survivor");
        //for (int i = _blockPos.x - spawnArea; i <= (_blockPos.x + spawnArea); i++)
        //{
        //    for (int j = _blockPos.z - spawnArea; j <= (_blockPos.z + spawnArea); j++)
        //    {
        //        for (int k = _blockPos.y - spawnArea; k <= (_blockPos.y + spawnArea); k++)
        //        {
        //            BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
        //            if (block.type == survivorBlock.type && (block.meta == survivorType || survivorType == 0))
        //            {
        //                // found a male survivor, will "destroy" it
        //                if (placing)
        //                {
        //                    debugHelper.doDebug("BLOCKCRAFTER: Destroying survivor to place a crafter", !disableDebug);
        //                    world.SetBlockRPC(cIdx, new Vector3i(i, k, j), BlockValue.Air);
        //                }
        //                return true;
        //            }
        //        }
        //    }
        //}
        if (placing) DisplayToolTipText(string.Format("You need at least one {0} survivor to operate this workbench.", requirement));
        return false;
    }
}

public class BlockFarmer : BlockSecureLoot
{
    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;
    UnityEngine.GameObject gameObject;
    FarmerWorkScript script;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "FARMER: " + str;
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

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if (!_world.IsRemote())
        // THE SCRIPT WILL ALWAYS RUN, but only does certain things remotely/locally
        {
            debugHelper.doDebug("BLOCKFARMER: OnBlockValueChanged", !disableDebug);
            // check bit 1
            //if (AddScript(_newBlockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null)
                {
                    try
                    {
                        debugHelper.doDebug("BLOCKFARMER: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<FarmerWorkScript>();
                        if (script == null)
                        {
                            debugHelper.doDebug("BLOCKFARMER: OnBlockValueChanged - ADDING SCRIPT?", !disableDebug);
                            script = gameObject.AddComponent<FarmerWorkScript>();
                            TileEntitySecureLootContainer secureLootContainer =
                                _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
                            script.initialize(secureLootContainer, _world, _blockPos, _clrIdx);
                        }
                        else debugHelper.doDebug("BLOCKFARMER: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?", !disableDebug);
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug("BLOCKFARMER: Error OnBlockValueChanged - " + ex.Message, false);
                    }
                }
                // resets the bit
                //_newBlockValue.meta2 = (byte)(_newBlockValue.meta2 & ~(1 << 1));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
            }
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // every time this happens?
        // set bit to 1
        //if (!AddScript(_blockValue.meta2))
        {
            //_blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 1));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }       
    }
    // if a user tries to "open" a crafter he will get a textbox showing what he know, and what he is assinged to
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
        string keybindString = GUI_2.UIUtils.GetKeybindString(((EntityPlayerLocal)_entityFocusing).playerInput.Activate);
        if (secureLootContainer == null)
            return string.Empty;
        string str = Localization.Get(Block.list[_blockValue.type].GetBlockName(), string.Empty);
        if (!secureLootContainer.IsLocked())
            return string.Format(Localization.Get("tooltipUnlocked", string.Empty), keybindString, (object)str);
        return string.Format(Localization.Get("tooltipLocked", string.Empty), keybindString, (object)str);
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        // it's here that I remove the survivor
        string requirement = "Any";
        if (this.Properties.Values.ContainsKey("Requires"))
            requirement = this.Properties.Values["Requires"];
        bool result = CheckSurvivor(_world, _bpResult.clrIdx, _bpResult.blockPos, requirement, true);
        if (result)
            base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        //return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        debugHelper.doDebug("OnBlockActivated START", !disableDebug);
        try
        {
            BlockValue block = _world.GetBlock(_blockPos.x, _blockPos.y - 1, _blockPos.z);
            BlockEntityData _ebcd = _world.ChunkClusters[_cIdx].GetBlockEntity(_blockPos);
            if (_ebcd != null)
            {
                gameObject = _ebcd.transform.gameObject;
                if (Block.list[block.type].HasTag(BlockTags.Door))
                {
                    _blockPos = new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z);
                    return this.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                }
                TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntitySecureLootContainer;
                if (secureLootContainer == null)
                    return false;
                _player.AimingGun = false;
                //Vector3i _blockPos1 = secureLootContainer.ToWorldPos();
                //secureLootContainer.bWasTouched = secureLootContainer.bTouched;
                //_world.GetGameManager().TELockServer(_cIdx, _blockPos1, secureLootContainer.entityId, _player.entityId);
                // custom behaviour - opens a custom UI            
                FarmerInvScript scriptInv;

                scriptInv = gameObject.GetComponent<FarmerInvScript>();
                if (scriptInv != null)
                {
                    scriptInv.KillScript();
                    scriptInv = this.gameObject.AddComponent<FarmerInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
                else
                {
                    scriptInv = this.gameObject.AddComponent<FarmerInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            debugHelper.doDebug("Error OnBlockActivated - " + ex.Message, false);
            return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        }
    }

    // Check if there is any survivor (male) that can assume this position.
    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        if (result)
        {
            string requirement = "Any";
            if (this.Properties.Values.ContainsKey("Requires"))
                requirement = this.Properties.Values["Requires"];
            result = CheckSurvivor(_world, _clrIdx, _blockPos, requirement, false);
            if (requirement == "Any") requirement = " ";
            else requirement = " " + requirement + " ";
            if (!result) DisplayToolTipText(string.Format("You need at least one {0} survivor nearby to place this workbench.", requirement));
        }
        return result;
    }

    private bool CheckSurvivor(WorldBase world, int cIdx, Vector3i _blockPos, string requirement, bool placing)
    {
        int spawnArea = 10;
        if (this.Properties.Values.ContainsKey("SpawnArea"))
        {
            if (int.TryParse(this.Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 5;
        }
        int survivorType = 0;
        if (requirement == "Male") survivorType = 1;
        else if (requirement == "Female") survivorType = 2;
        else if (requirement == "Teen") survivorType = 3;
        int numBlocks = 0;
        // look for a specific survivor type nearby
        using (
                        List<Entity>.Enumerator enumerator =
                            GameManager.Instance.World.GetEntitiesInBounds(typeof(EntitySurvivorMod),
                                BoundsUtils.BoundsForMinMax(_blockPos.x - spawnArea, _blockPos.y - spawnArea,
                                    _blockPos.z - spawnArea, _blockPos.x + spawnArea,
                                    _blockPos.y + spawnArea,
                                    _blockPos.z + spawnArea)).GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                EntitySurvivorMod _other = enumerator.Current as EntitySurvivorMod;
                if (_other.IsAlive() && ((_other.IsMale && survivorType == 1) || (!_other.IsMale && survivorType == 2) || (!_other.IsMale && survivorType == 3 && _other.EntityName == "Girl1")))
                {
                    if (placing)
                    {
                        // send dmg to the survivor to destroy it
                        DamageResponse dmg = new DamageResponse();
                        dmg.Strength = 6001;
                        dmg.Source = new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId);
                        dmg.Critical = false;
                        dmg.ImpulseScale = 1;
                        dmg.CrippleLegs = false;
                        dmg.Dismember = false;
                        dmg.Fatal = false;
                        dmg.TurnIntoCrawler = false;
                        if (world.IsRemote())
                        {
                            NetPackage _package1 = (NetPackage)new NetPackageDamageEntity(_other.entityId, dmg);
                            GameManager.Instance.SendToServer(_package1);
                        }
                        else
                            _other.DamageEntity(new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId), 6001,
                                false, 1);
                    }
                    return true;
                }
            }
        }
        if (placing) DisplayToolTipText(string.Format("You need at least one {0} survivor to operate this workbench.", requirement));
        return false;
    }
}

public class BlockGuard : BlockSecureLoot
{
    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;
    UnityEngine.GameObject gameObject;
    GuardWorkScript script;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "FARMER: " + str;
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

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if (!_world.IsRemote())
        // THE SCRIPT WILL ALWAYS RUN, but only does certain things remotely/locally
        {
            debugHelper.doDebug("BLOCKGUARD: OnBlockValueChanged", !disableDebug);
            // check bit 1
            //if (AddScript(_newBlockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null)
                {
                    try
                    {
                        debugHelper.doDebug("BLOCKGUARD: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<GuardWorkScript>();
                        if (script == null)
                        {
                            debugHelper.doDebug("BLOCKGUARD: OnBlockValueChanged - ADDING SCRIPT?", !disableDebug);
                            script = gameObject.AddComponent<GuardWorkScript>();
                            TileEntitySecureLootContainer secureLootContainer =
                                _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
                            script.initialize(secureLootContainer, _world, _blockPos, _clrIdx);
                        }
                        else debugHelper.doDebug("BLOCKGUARD: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?", !disableDebug);
                    }
                    catch (Exception ex)
                    {
                        debugHelper.doDebug("BLOCKGUARD: Error OnBlockValueChanged - " + ex.Message, true);
                    }
                }
                // resets the bit
                //_newBlockValue.meta2 = (byte)(_newBlockValue.meta2 & ~(1 << 1));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
            }
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // every time this happens?
        // set bit to 1
        // change this to add the script EVERY TIME
        //if (!AddScript(_blockValue.meta2))
        {
            //_blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 1));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }
    }
    // if a user tries to "open" a crafter he will get a textbox showing what he know, and what he is assinged to
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
        string keybindString = GUI_2.UIUtils.GetKeybindString(((EntityPlayerLocal)_entityFocusing).playerInput.Activate);
        if (secureLootContainer == null)
            return string.Empty;
        string str = Localization.Get(Block.list[_blockValue.type].GetBlockName(), string.Empty);
        if (!secureLootContainer.IsLocked())
            return string.Format(Localization.Get("tooltipUnlocked", string.Empty), keybindString, (object)str);
        return string.Format(Localization.Get("tooltipLocked", string.Empty), keybindString, (object)str);
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        //return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        debugHelper.doDebug("OnBlockActivated START", !disableDebug);
        try
        {
            BlockValue block = _world.GetBlock(_blockPos.x, _blockPos.y - 1, _blockPos.z);
            BlockEntityData _ebcd = _world.ChunkClusters[_cIdx].GetBlockEntity(_blockPos);
            if (_ebcd != null)
            {
                gameObject = _ebcd.transform.gameObject;
                if (Block.list[block.type].HasTag(BlockTags.Door))
                {
                    _blockPos = new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z);
                    return this.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                }
                TileEntitySecureLootContainer secureLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntitySecureLootContainer;
                if (secureLootContainer == null)
                    return false;
                _player.AimingGun = false;
                //Vector3i _blockPos1 = secureLootContainer.ToWorldPos();
                //secureLootContainer.bWasTouched = secureLootContainer.bTouched;
                //_world.GetGameManager().TELockServer(_cIdx, _blockPos1, secureLootContainer.entityId, _player.entityId);
                // custom behaviour - opens a custom UI            
                GuardInvScript scriptInv;

                scriptInv = gameObject.GetComponent<GuardInvScript>();
                if (scriptInv != null)
                {
                    scriptInv.KillScript();
                    scriptInv = this.gameObject.AddComponent<GuardInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
                else
                {
                    scriptInv = this.gameObject.AddComponent<GuardInvScript>();
                    scriptInv.initialize(secureLootContainer, _player, _world, _blockPos, _cIdx);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            debugHelper.doDebug("Error OnBlockActivated - " + ex.Message, false);
            return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        }
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        // it's here that I remove the survivor
        string requirement = "Any";
        if (this.Properties.Values.ContainsKey("Requires"))
            requirement = this.Properties.Values["Requires"];
        bool result = CheckSurvivor(_world, _bpResult.clrIdx, _bpResult.blockPos, requirement, true);
        if (result)
            base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    // Check if there is any survivor (male) that can assume this position.
    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        if (result)
        {
            string requirement = "Any";
            if (this.Properties.Values.ContainsKey("Requires"))
                requirement = this.Properties.Values["Requires"];
            result = CheckSurvivor(_world, _clrIdx, _blockPos, requirement, false);
            if (requirement == "Any") requirement = " ";
            else requirement = " " + requirement + " ";
            if (!result) DisplayToolTipText(string.Format("You need at least one {0} survivor nearby to place this workbench.", requirement));
        }
        return result;
    }

    private bool CheckSurvivor(WorldBase world, int cIdx, Vector3i _blockPos, string requirement, bool placing)
    {
        int spawnArea = 10;
        if (this.Properties.Values.ContainsKey("SpawnArea"))
        {
            if (int.TryParse(this.Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 5;
        }
        int survivorType = 0;
        if (requirement == "Male") survivorType = 1;
        else if (requirement == "Female") survivorType = 2;
        else if (requirement == "Teen") survivorType = 3;
        int numBlocks = 0;
        // look for a specific survivor type nearby
        using (
                        List<Entity>.Enumerator enumerator =
                            GameManager.Instance.World.GetEntitiesInBounds(typeof(EntitySurvivorMod),
                                BoundsUtils.BoundsForMinMax(_blockPos.x - spawnArea, _blockPos.y - spawnArea,
                                    _blockPos.z - spawnArea, _blockPos.x + spawnArea,
                                    _blockPos.y + spawnArea,
                                    _blockPos.z + spawnArea)).GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                EntitySurvivorMod _other = enumerator.Current as EntitySurvivorMod;
                if (_other.IsAlive() && ((_other.IsMale && survivorType == 1) || (!_other.IsMale && survivorType == 2) || (!_other.IsMale && survivorType == 3 && _other.EntityName == "Girl1")))
                {
                    if (placing)
                    {
                        // send dmg to the survivor to destroy it
                        DamageResponse dmg = new DamageResponse();
                        dmg.Strength = 6001;
                        dmg.Source = new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId);
                        dmg.Critical = false;
                        dmg.ImpulseScale = 1;
                        dmg.CrippleLegs = false;
                        dmg.Dismember = false;
                        dmg.Fatal = false;
                        dmg.TurnIntoCrawler = false;
                        if (world.IsRemote())
                        {
                            NetPackage _package1 = (NetPackage)new NetPackageDamageEntity(_other.entityId, dmg);
                            GameManager.Instance.SendToServer(_package1);
                        }
                        else
                            _other.DamageEntity(new DamageSourceEntity(EnumDamageSourceType.Disease, _other.entityId), 6001,
                                false, 1);
                    }
                    return true;
                }
            }
        }
        if (placing) DisplayToolTipText(string.Format("You need at least one {0} survivor to operate this workbench.", requirement));
        return false;
    }

    //private bool CheckSurvivor(WorldBase world, int cIdx, Vector3i _blockPos, string requirement, bool placing)
    //{
    //    int spawnArea = 10;
    //    if (this.Properties.Values.ContainsKey("SpawnArea"))
    //    {
    //        if (int.TryParse(this.Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 10;
    //    }
    //    int survivorType = 0;
    //    if (requirement == "Male") survivorType = 1;
    //    else if (requirement == "Female") survivorType = 2;
    //    else if (requirement == "Teen") survivorType = 3;
    //    int numBlocks = 0;
    //    BlockValue survivorBlock = Block.GetBlockValue("survivor");
    //    for (int i = _blockPos.x - spawnArea; i <= (_blockPos.x + spawnArea); i++)
    //    {
    //        for (int j = _blockPos.z - spawnArea; j <= (_blockPos.z + spawnArea); j++)
    //        {
    //            for (int k = _blockPos.y - spawnArea; k <= (_blockPos.y + spawnArea); k++)
    //            {
    //                BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
    //                if (block.type == survivorBlock.type && (block.meta == survivorType || survivorType == 0))
    //                {
    //                    // found a male survivor, will "destroy" it
    //                    if (placing)
    //                    {
    //                        debugHelper.doDebug("BLOCKGUARD: Destroying survivor to place a crafter", !disableDebug);
    //                        world.SetBlockRPC(cIdx, new Vector3i(i, k, j), BlockValue.Air);
    //                    }
    //                    return true;
    //                }
    //            }
    //        }
    //    }
    //    if (placing) DisplayToolTipText(string.Format("You need at least one{0}survivor to operate this workbench.", requirement));
    //    return false;
    //}
}

