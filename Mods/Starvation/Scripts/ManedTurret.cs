using System;
using A;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using XInputDotNetPure;
using Object = UnityEngine.Object;

public class TurretHelper
{
    public static Vector3i _blockPos;
    public static int _clrIdx;
    public static Vector3 _playerPos;
    public static Vector3 _playerRot;
    public static Vector3 _newPlayerPos;

    static TurretHelper()
    {
        _blockPos = Vector3i.zero;
        _clrIdx = -1;
        _playerPos = Vector3.zero;
        _playerRot = Vector3.zero;
        _newPlayerPos = Vector3.zero;
    }
}

public class ItemActionTurret : ItemClass
{
    private float currentModifier = 0;
    private string blockName = "BrowningBlock";
    private string buffName = "turret";
    private int alreadyAdjusted = 0;
    private string soundJammed = "";
    private int failChance = 0;
    private DateTime jamTime = DateTime.MinValue;

    public override void StartHolding(ItemInventoryData _data, Transform _modelTransform)
    {
        base.StartHolding(_data, _modelTransform);
        if (_data.holdingEntity.isEntityRemote) return;
        // player cannot move
        if (this.Properties.Values.ContainsKey("SoundJammed")) soundJammed = this.Properties.Values["soundJammed"];
        if (this.Properties.Values.ContainsKey("JamChance"))
            failChance = Convert.ToInt32(this.Properties.Values["JamChance"]);
        currentModifier = (_data.holdingEntity as EntityPlayerLocal).Stats.SpeedModifier.Value;
        //(_data.holdingEntity as EntityPlayerLocal).Stats.SpeedModifier.Value = 0;
        if (TurretHelper._clrIdx == -1)
        {
            // something is wrong I need to identify positions
            Debug.Log("Starting turret incorrectly");

        }
        ApplyBuff(_data);
    }

    private void ApplyBuff(ItemInventoryData _data)
    {
        MultiBuffClassAction multiBuffClassAction = MultiBuffClassAction.NewAction(buffName);
        multiBuffClassAction.Execute(_data.holdingEntity.entityId, (EntityAlive) _data.holdingEntity,
            false,
            EnumBodyPartHit.None, (string) null);
    }

    public override void ExecuteAction(int _actionIdx, ItemInventoryData _data, bool _bReleased)
    {
        if (DateTime.Now >= jamTime || _actionIdx > 0)
        {
            if (_actionIdx == 0 && failChance > 0 && soundJammed != "")
            {
                if ((_data.holdingEntity as EntityPlayerLocal).GetRandom().Next(1, 10001) > failChance)
                    base.ExecuteAction(_actionIdx, _data, _bReleased);
                else
                {
                    GameManager.Instance.ShowTooltip("Fucking piece of shit jammed!!!");
                    RemoveBullets(_data);
                    jamTime = DateTime.Now.AddSeconds(1);
                }
            }
            else base.ExecuteAction(_actionIdx, _data, _bReleased);
        }
    }

    public override void OnHoldingUpdate(ItemInventoryData _data)
    {
        base.OnHoldingUpdate(_data);
        if (_data.holdingEntity.isEntityRemote) return;
        // if player presses E it will stop holding the gun, and place the block again
        if (Input.GetKey(KeyCode.E))
        {
            UnmanTurret(_data);
            return;
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            UnmanTurret(_data);
            return;
        }
        //else (_data.holdingEntity as EntityPlayerLocal).Stats.SpeedModifier.Value = 0;
        if (global::GameManager.Instance.windowManager.IsWindowOpen("backpack"))
        {
            UnmanTurret(_data); // imediatly unman turret if a player opens the inventory
            return;
        }
        if (_data.holdingEntity.IsDead() || _data.holdingEntity.IsDespawned)
        {
            UnmanTurret(_data);
            return;
        }
        // if the buff is not present, applies it.
        if (!(_data.holdingEntity as EntityPlayerLocal).Stats.FindBuff(buffName)) ApplyBuff(_data);
        if (TurretHelper._clrIdx > -1)
        {
            float distanceToPos = _data.holdingEntity.GetDistanceSq(TurretHelper._newPlayerPos);
            if (distanceToPos > 0.09)
            {
                Debug.Log("DISTANCE: " + distanceToPos);
                alreadyAdjusted++;
                _data.holdingEntity.SetPosition(TurretHelper._newPlayerPos);
            }
        }
    }

    private void UnmanTurret(ItemInventoryData _data)
    {
        if (_data.holdingEntity.isEntityRemote) return;
        try
        {
            if (this.Properties.Values.ContainsKey("turretBlock")) blockName = this.Properties.Values["turretBlock"];
            // remove bullets
            RemoveBullets(_data);
            // remove "gun"
            _data.holdingEntity.inventory.DecHoldingItem(1);
            // place the block right at it's original position            
            BlockValue offBlock = Block.GetBlockValue(blockName); // i probably want to add durability here
            if (TurretHelper._clrIdx > -1)
            {
                // move the player to its initial spot
                _data.holdingEntity.position = TurretHelper._playerPos;
                _data.holdingEntity.SetRotation(TurretHelper._playerRot);
                // show the block again                            
                _data.holdingEntity.world.SetBlockRPC(TurretHelper._clrIdx, TurretHelper._blockPos, offBlock);
            }
            else
            {
                Vector3i position = new Vector3i(_data.holdingEntity.position);
                _data.holdingEntity.position.y = _data.holdingEntity.position.y - 1;
                //_data.holdingEntity.SetLookPosition(position);    
                _data.holdingEntity.world.SetBlockRPC(position, offBlock);
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            if ((_data.holdingEntity as EntityPlayerLocal).Stats.FindBuff(buffName)) (_data.holdingEntity as EntityPlayerLocal).Stats.Debuff(buffName);
            (_data.holdingEntity as EntityPlayerLocal).SetInvestigatePosition(Vector3.zero, 1);
            (_data.holdingEntity as EntityPlayerLocal).inventory.CallOnToolbeltChangedInternal();
            TurretHelper._blockPos = Vector3i.zero;
            TurretHelper._clrIdx = -1;
            TurretHelper._playerPos = Vector3.zero;
            TurretHelper._playerRot = Vector3.zero;
            //(_data.holdingEntity as EntityPlayerLocal).Stats.SpeedModifier.Value = currentModifier;
            //(_data.holdingEntity as EntityPlayerLocal).stepHeight = jumpModifier;            
        }
    }

    private static void RemoveBullets(ItemInventoryData _data)
    {
        if (_data.itemValue.Meta > 0)
        {
            // it has bullets, put them back in the inventory
            if (
                _data.item.Actions[0].Properties.Contains(
                    "Magazine_items"))
            {
                // create a stack
                ItemValue bullet =
                    global::ItemClass.GetItem(
                        (_data.item.Actions[0] as ItemActionRanged).MagazineItemNames[
                            (int) _data.itemValue.SelectedAmmoTypeIndex]);
                ItemStack bullets = new ItemStack(bullet,
                    _data.itemValue.Meta);
                _data.itemValue.Meta = 0;
                _data.holdingEntity.AddUIHarvestingItem(
                    bullets, false);
                _data.holdingEntity.bag.AddItem(bullets);
            }
        }
    }

    public override void StopHolding(ItemInventoryData _data, Transform _modelTransform)
    {
        if (_data.holdingEntity.inventory.holdingCount > 0)
            UnmanTurret(_data);
        base.StopHolding(_data, _modelTransform);        
    }
}

public class BlockMannedTurret : Block
{
    private BlockActivationCommand[] QJ = new BlockActivationCommand[1]
    {
        new BlockActivationCommand("take", "hand", false)
    };

    private string weaponName = "Browning";
    private int defaultQuality = 500;

    public override void Init()
    {
        base.Init();
        if (this.Properties.Values.ContainsKey("weaponItem"))
        {
            weaponName = this.Properties.Values["weaponItem"];
        }
        if (this.Properties.Values.ContainsKey("defaultQuality"))
        {
            defaultQuality = Convert.ToInt32(this.Properties.Values["defaultQuality"]);
        }
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
       return "Press <{0}> to man the turret";       
    }

    public override bool OnBlockActivated(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        // hide the block, and equip the respective item
        // create the item
        if ((_player as EntityPlayerLocal).inventory.holdingItemStack.count == 0)
        {
            ItemValue itemGun = new ItemValue(ItemClass.GetItem(weaponName).type, defaultQuality,
                                       defaultQuality, ItemClass.GetItem(weaponName).Parts.Length > 0);
            ItemStack loot1Stack = new ItemStack(itemGun, 1);
            loot1Stack.itemValue.Quality = 600; // i probably wanna make this dependent on block??
            // stores player position, player rotation and block position
            TurretHelper._blockPos = _blockPos;
            TurretHelper._clrIdx = _clrIdx;
            TurretHelper._playerPos = (_player as EntityPlayerLocal).GetPosition();
            TurretHelper._playerRot = (_player as EntityPlayerLocal).rotation;

            BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
            TurretHelper._newPlayerPos = _ebcd.transform.position;
            // hides the block
            _world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);
            // moves to block position
            (_player as EntityPlayerLocal).SetPosition(TurretHelper._newPlayerPos);
            // show weapon
            //(_player as EntityPlayerLocal).inventory.holdingItemData.itemStack = loot1Stack;
            (_player as EntityPlayerLocal).inventory.SetItem((_player as EntityPlayerLocal).inventory.holdingItemIdx,
                loot1Stack);
            (_player as EntityPlayerLocal).inventory.CallOnToolbeltChangedInternal();
        }
        else GameManager.Instance.ShowTooltip("You need to have your hands free to man this turret!");
        return true;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }   
}
