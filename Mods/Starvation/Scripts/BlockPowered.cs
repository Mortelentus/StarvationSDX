using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BlockElectricDevice : Block
{

    ElectricDeviceScript script;
    UnityEngine.GameObject gameObject;
    private bool disableDebug = true;
    private int maxLevel = 10;
    private bool isPowered = true;
    private int valveNumber = 10;

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
            str = "ELECTRICDEVICE: " + str;
            //bool debug = false;
            //if (this.Properties.Values.ContainsKey("debug"))
            //{
            //    if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            //}
            //if (debug)
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
        if (this.Properties.Values.ContainsKey("isPowered"))
        {
            if (bool.TryParse(this.Properties.Values["isPowered"], out isPowered) == false) isPowered = false;
        }
    }

    public static bool IsOn(byte _metadata)
    {
        // bit 2
        return ((int)_metadata & 1 << 2) != 0;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        debugHelper.doDebug("BlockElectricDevice: OnBlockValueChanged", !disableDebug);
        // here I update the animation, if it's not dedicated
        if (!GameManager.IsDedicatedServer)
            playAnimation(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (_world.IsRemote()) return; // only the server needs to run the script        
        // get the transform
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        if (_ebcd != null)
        {
            try
            {
                if (_ebcd.transform == null) return;
                debugHelper.doDebug("BlockElectricDevice: OnBlockValueChanged - CHECKING SCRIPT?", !disableDebug);
                gameObject = _ebcd.transform.gameObject;
                if (gameObject == null) return;
                // adds the script if still not existing.
                script = gameObject.GetComponent<ElectricDeviceScript>();
                if (script == null)
                {
                    debugHelper.doDebug("BlockElectricDevice: OnBlockValueChanged - ADDING SCRIPT?",
                        !disableDebug);
                    AddScriptToObject(_world, _blockPos, _clrIdx, _newBlockValue, _ebcd);
                }
                else
                    debugHelper.doDebug(
                        "BlockElectricDevice: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?",
                        !disableDebug);
            }
            catch (Exception ex)
            {
                debugHelper.doDebug("BlockElectricDevice: Error OnBlockValueChanged - " + ex.Message, false);
            }
        }
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        debugHelper.doDebug("BLOCKELECTRICDEVICE: OnBlockEntityTransformAfterActivated with META2 = " + _blockValue.meta2, !disableDebug);
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
        gameObject = _ebcd.transform.gameObject;
        script = gameObject.AddComponent<ElectricDeviceScript>();
        script.init(_world, _blockPos, _cIdx, isPowered);
    }
    public void CheckForPower(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        try
        {            
            bool needsAction = false;
            if (Findorigin(_world, _clrIdx, _blockValue, _blockPos, _blockPos, 1, "Electric"))
            {
                if (!IsOn(_blockValue.meta))
                {
                    debugHelper.doDebug("BLOCKELECTRICDEVICE: TRIGGER TURNING ON", !disableDebug);
                    _blockValue.meta = (byte)(_blockValue.meta | (1 << 2));
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                }
            }
            else
            {
                if (IsOn(_blockValue.meta))
                {
                    debugHelper.doDebug("BLOCKELECTRICDEVICE: TRIGGER TURNING OFF", !disableDebug);
                    _blockValue.meta = (byte)(_blockValue.meta & ~(1 << 2));
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                }
            }
        }
        catch (Exception ex)
        {
            DisplayChatAreaText("Error - " + ex.Message);
        }
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
        DisplayChatAreaText(string.Format("CHECKING PARENT OF BLOCK {0}", blockname));
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
        bool result = false;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
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
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED ON"));
                return true;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED OFF"));
                return false; // boiler is not on
            }
        }
        else if (blockAux is BlockValve)
        {
            // needs to verify the valve powerType, to make sure.
            if ((blockAux as BlockValve).GetPowerType() != "Electric") return false;

            // asks valve for power, instead of going all the way to the generator
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, 1, valveNumber))
            {
                DisplayChatAreaText(string.Format("FOUND A VALVE WITH POWER"));
                return true; // available power
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND A VALVE WITHOUT POWER"));
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

    private void playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _blockValue)
    {
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        DisplayChatAreaText("FOUND ANIMATOR with BLOCKVALUE = " + _blockValue.ToString());
        foreach (Animator animator in componentsInChildren)
        {
            if (IsOn(_blockValue.meta))
            {
                // play open animation
                DisplayChatAreaText("Turn On");
                animator.CrossFade("LampOn", 0.0f);
                //animator.ResetTrigger("IsOff");
                //animator.SetTrigger("IsOn");
            }
            else
            {
                // play close animation
                DisplayChatAreaText("Turn Off");
                animator.CrossFade("LampOff", 0.0f);
                //animator.ResetTrigger("IsOn");
                //animator.SetTrigger("IsOff");
            }
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        bool _isOn = IsOn(_blockValue.meta);
        foreach (Animator animator in componentsInChildren)
        {
            if (!_isOn)
                animator.CrossFade("LampOff", 0.0f);
            else animator.CrossFade("LampOn", 0.0f);
        }
    }

}

public class ElectricDeviceScript : MonoBehaviour
{
    private float SecondsPassed = 0.0F;
    private bool isPowered = true;
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    DateTime nextCheck = DateTime.MinValue;
    private DateTime nextSpawn = DateTime.MinValue;
    bool hasPower = false;
    private bool debug = false;

    void Start()
    {
    }

    public void init(WorldBase _world, Vector3i _blockPos, int _cIdx, bool _isPowered)
    {
        world = _world;
        blockPos = _blockPos;
        cIdx = _cIdx;
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
                        (Block.list[_blockValue.type] as BlockElectricDevice).CheckForPower(world, cIdx, blockPos, _blockValue);
                    }
                }
                catch (Exception ex)
                {
                    debugHelper.doDebug(string.Format("ElectricDeviceScript: ERROR: {0}", ex.Message), true);
                }                              
            }
        }
    }
}