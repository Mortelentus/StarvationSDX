using System;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// this class is basically a "transformer"
/// they do not produce energy per se, but they transform items
/// they need a catalist (or fuel), a raw material, and a emtpy vessel for the end product
/// catalist, raw material, vessel and end product items are configurable
/// if any are left blank they will be ignored
/// Mortelentus - 2016
/// </summary>
public class BlockUtilityContainer : BlockLoot
{
    private bool debug = false;

    private bool disableDebug = true;

    string obj1Name = "";
    string obj2Name = "";
    string obj3Name = "";
    string obj4Name = "";
    string st1Name = "";
    string st2Name = "";
    string st3Name = "";
    string st4Name = "";
    ItemValue object1 = (ItemValue)null;
    ItemValue object2 = (ItemValue)null;
    ItemValue object3 = (ItemValue)null;
    ItemValue object4 = (ItemValue)null;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    public BlockUtilityContainer()
    {
        //this.IsRandomlyTick = true;
        //this.IsRandomlyTick = true;        
    }

    private void Getinivars()
    {
        char[] splitter = new char[1];
        string[] auxStrings;
        splitter[0] = ',';
        try
        {
            #region finds the objects

            if (this.Properties.Values.ContainsKey("item1"))
            {
                obj1Name = this.Properties.Values["item1"];
                auxStrings = obj1Name.Split(splitter);
                if (auxStrings.Length > 1)
                {
                    obj1Name = auxStrings[0].Trim();
                    st1Name = auxStrings[1].Trim();
                }
                if (obj1Name != "") object1 = ItemClass.GetItem(obj1Name);
            }
            if (this.Properties.Values.ContainsKey("item2"))
            {
                obj2Name = this.Properties.Values["item2"];
                auxStrings = obj2Name.Split(splitter);
                if (auxStrings.Length > 1)
                {
                    obj2Name = auxStrings[0].Trim();
                    st2Name = auxStrings[1].Trim();
                }
                if (obj2Name != "") object2 = ItemClass.GetItem(obj2Name);
            }
            if (this.Properties.Values.ContainsKey("item3"))
            {
                obj3Name = this.Properties.Values["item3"];
                auxStrings = obj3Name.Split(splitter);
                if (auxStrings.Length > 1)
                {
                    obj3Name = auxStrings[0].Trim();
                    st3Name = auxStrings[1].Trim();
                }
                if (obj3Name != "") object3 = ItemClass.GetItem(obj3Name);
            }
            if (this.Properties.Values.ContainsKey("item4"))
            {
                obj4Name = this.Properties.Values["item4"];
                auxStrings = obj4Name.Split(splitter);
                if (auxStrings.Length > 1)
                {
                    obj4Name = auxStrings[0].Trim();
                    st4Name = auxStrings[1].Trim();
                }
                if (obj4Name != "") object4 = ItemClass.GetItem(obj4Name);
            }

            #endregion;
        }
        catch (Exception ex)
        {
            Debug.Log("UTILITYCONTAINER -> ERROR in drotransfor " + ex.Message);
        }
        //Debug.Log(string.Format("UTILITYCONTAINER -> 1={0},2={1},3={2},4={3}", obj1Name, obj2Name, obj3Name, obj4Name));
    }

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
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
        Getinivars();
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        //DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}", this.IsRandomlyTick.ToString(), this.GetTickRate()));
        return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override BlockValue OnBlockPlaced(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Random _rnd)
    {
        //_blockValue.meta = (byte)_rnd.Next(16); // tick ?
        return base.OnBlockPlaced(_world, _clrIdx, _blockPos, _blockValue, _rnd);
    }

    public override ulong GetTickRate()
    {
        ulong result = 10;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 10; 
        }
        return result;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}, BLOCKID: {2}", this.IsRandomlyTick.ToString(), this.GetTickRate(), this.blockID));
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        if (!world.IsRemote())
        {
            world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        }        
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        try
        {
            DoTransform(_world, _clrIdx, _blockPos, _blockValue);
        }
        catch (Exception)
        {
            
        }        
        finally 
        {
            // add next tick - if no transformation is possible, no use to stress the system
            // it will be done next time something is added to the container
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
        //return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
        return true;
    }

    // process up to 4 different item with the corresponding asset object
    public bool DoTransform(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        //DisplayChatAreaText("UtilityContainer TICK - if change needed, set bit to 1");
        bool resultado = false;
        try
        {
            Getinivars();
            DisplayChatAreaText(string.Format("object1={0} object2={1}", object1.type, object2.type));
            if (object1 != null)
            {
                // try to find the godamn loot container list
                //TileEntitySecureLootContainer Container;
                TileEntityLootContainer Container;
                TileEntity tileEntity = (TileEntity) null;

                tileEntity = _world.GetTileEntity(_cIdx, _blockPos);
                if (tileEntity != null)
                {
                    if (tileEntity is TileEntityLootContainer)
                    {
                        bool hasObj1 = false;
                        bool hasObj2 = false;
                        bool hasObj3 = false;
                        bool hasObj4 = false;                        
                        Container = (TileEntityLootContainer) tileEntity;
                        foreach (ItemStack itemStack1 in Container.items)
                        {
                            if (object1 != null)
                            {
                                if (itemStack1.itemValue.Equals(object1) && itemStack1.count > 0)
                                {
                                    hasObj1 = true;
                                }
                            }
                            if (object2 != null)
                            {
                                if (itemStack1.itemValue.Equals(object2) && itemStack1.count > 0)
                                {
                                    hasObj2 = true;
                                }
                            }
                            if (object3 != null)
                            {
                                if (itemStack1.itemValue.Equals(object3) && itemStack1.count > 0)
                                {
                                    hasObj3 = true;
                                }
                            }
                            if (object4 != null)
                            {
                                if (itemStack1.itemValue.Equals(object4) && itemStack1.count > 0)
                                {
                                    hasObj4 = true;
                                }
                            }
                            if (hasObj1 && hasObj2 && hasObj3 && hasObj4)
                            {
                                //DisplayChatAreaText("CAN STOP SEARCHING");                                
                                break;
                            }
                        }
                        DisplayChatAreaText(string.Format("UTILITYCONTAINER -> META={0}, OBJ1={1}, OBJ2={2}, OBJ3={3}, OBJ4={4}", _blockValue.meta.ToString(), hasObj1, hasObj2, hasObj3, hasObj4));
                        int numBit = 0;
                        if (hasObj1) _blockValue.meta = (byte) (_blockValue.meta | (1 << numBit));
                        else _blockValue.meta = (byte)(_blockValue.meta & ~(1 << numBit));
                        numBit++;
                        if (hasObj2) _blockValue.meta = (byte)(_blockValue.meta | (1 << numBit));
                        else _blockValue.meta = (byte)(_blockValue.meta & ~(1 << numBit));
                        numBit++;
                        if (hasObj3) _blockValue.meta = (byte)(_blockValue.meta | (1 << numBit));
                        else _blockValue.meta = (byte)(_blockValue.meta & ~(1 << numBit));
                        numBit++;
                        if (hasObj4) _blockValue.meta = (byte)(_blockValue.meta | (1 << numBit));
                        else _blockValue.meta = (byte)(_blockValue.meta & ~(1 << numBit));
                        DisplayChatAreaText(string.Format("META={0}", _blockValue.meta.ToString()));
                        // sets bits for the 4 objects
                        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                    }
                }
                else
                    Debug.Log("UTILITYCONTAINER -> No container for this object");
            }
            else Debug.Log("UTILITYCONTAINER -> At least one object should be configured");
        }
        catch (Exception ex1)
        {
            Debug.Log("UTILITYCONTAINER -> ERROR DOTRANSFORM " + ex1.Message);
        }
        return resultado;
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        Transform[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null)
        {
            if (_ebcd == null) DisplayChatAreaText("EBCD = NULL");
            else if (!_ebcd.bHasTransform) DisplayChatAreaText("EBCD HAS NO TRANSFORM");
            else DisplayChatAreaText("componentsInChildren = null");
            return;
        }
        DisplayChatAreaText("FORCE ANIMATION");
        bool hasObj1 = false; bool hasObj2 = false; bool hasObj3 = false; bool hasObj4 = false;
        if (((int) _blockValue.meta & 1 << 0) != 0) hasObj1 = true;  
        if (((int)_blockValue.meta & 1 << 1) != 0) hasObj2 = true;
        if (((int)_blockValue.meta & 1 << 2) != 0) hasObj3 = true;
        if (((int)_blockValue.meta & 1 << 3) != 0) hasObj4 = true;
        foreach (Transform tra in componentsInChildren)
        {
            if (tra.name == st1Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st1Name + " to " + hasObj1.ToString());
                tra.gameObject.SetActive(hasObj1);
            }
            if (tra.name == st2Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st2Name + " to " + hasObj2.ToString());
                tra.gameObject.SetActive(hasObj2);
            }
            if (tra.name == st3Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st3Name + " to " + hasObj3.ToString());
                tra.gameObject.SetActive(hasObj3);
            }
            if (tra.name == st4Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st4Name + " to " + hasObj4.ToString());
                tra.gameObject.SetActive(hasObj4);
            }
        }
    }

    private void playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _blockValue)
    {       
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Transform[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null)
        {
            if (_ebcd == null) DisplayChatAreaText("EBCD = NULL");
            else if (!_ebcd.bHasTransform) DisplayChatAreaText("EBCD HAS NO TRANSFORM");
            else DisplayChatAreaText("componentsInChildren = null");
            return;
        }
        bool hasObj1 = false; bool hasObj2 = false; bool hasObj3 = false; bool hasObj4 = false;
        if (((int)_blockValue.meta & 1 << 0) != 0) hasObj1 = true;
        if (((int)_blockValue.meta & 1 << 1) != 0) hasObj2 = true;
        if (((int)_blockValue.meta & 1 << 2) != 0) hasObj3 = true;
        if (((int)_blockValue.meta & 1 << 3) != 0) hasObj4 = true;
        foreach (Transform tra in componentsInChildren)
        {
            if (tra.name == st1Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st1Name + " to " + hasObj1.ToString());
                tra.gameObject.SetActive(hasObj1);
            }
            if (tra.name == st2Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st2Name + " to " + hasObj2.ToString());
                tra.gameObject.SetActive(hasObj2);
            }
            if (tra.name == st3Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st3Name + " to " + hasObj3.ToString());
                tra.gameObject.SetActive(hasObj3);
            }
            if (tra.name == st4Name)
            {
                DisplayChatAreaText("UTILITYCONTAINER -> SET " + st4Name + " to " + hasObj4.ToString());
                tra.gameObject.SetActive(hasObj4);
            }
        }
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (!(this.shape is BlockShapeModelEntity) || _oldBlockValue.type == _newBlockValue.type && (int)_oldBlockValue.meta == (int)_newBlockValue.meta || _newBlockValue.ischild)
            return;
        // trigger animation       
        DisplayChatAreaText("UTILITYCONTAINER -> Value changed");
        playAnimation(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText("ON BLOCK LOADED");
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        if (!_world.IsRemote())
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }
}