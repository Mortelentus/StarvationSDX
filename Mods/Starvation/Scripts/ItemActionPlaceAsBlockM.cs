using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class ItemActionPlaceAsBlockM : ItemActionPlaceAsBlock
{
    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        if (!_bReleased || (double)Time.time - (double)_actionData.lastUseTime < (double)this.Delay || (double)Time.time - (double)_actionData.lastUseTime < (double)Constants.cBuildIntervall)
            return;
        ItemInventoryData data = _actionData.invData;
        Vector3i vector3i = data.hitInfo.lastBlockPos;
        if (!data.hitInfo.bHitValid || data.hitInfo.tag.StartsWith("E_") || GameUtils.IsColliderWithinBlock(vector3i) && !data.world.bEditorMode || data.world.GetBlock(vector3i).type != 0)
            return;
        BlockValue _blockValue = data.item.OnConvertToBlockValue(data.itemValue, this.blockToPlace);
        _blockValue.damage = data.itemValue.UseTimes;
        _blockValue.meta = (byte) data.itemValue.Meta;
        WorldRayHitInfo worldRayHitInfo = data.hitInfo.Clone();
        worldRayHitInfo.hit.blockPos = vector3i;
        if (!Block.list[_blockValue.type].CanPlaceBlockAt((WorldBase)data.world, worldRayHitInfo.hit.clrIdx, vector3i, _blockValue))
        {
            GameManager.Instance.ShowTooltip("blockCantPlaced");
        }
        else
        {
            BlockPlacement.Result _bpResult = Block.list[_blockValue.type].BlockPlacementHelper.OnPlaceBlock((WorldBase)data.world, _blockValue, worldRayHitInfo.hit, data.holdingEntity.position);
            Block.list[_blockValue.type].OnBlockPlaceBefore((WorldBase)data.world, ref _bpResult, data.holdingEntity, new System.Random());
            _blockValue = _bpResult.blockValue;
            if (Block.list[_blockValue.type].IndexName == "lpblock")
            {
                if (data.world.CanPlaceLandProtectionBlockAt(_bpResult.blockPos, data.world.gameManager.GetPersistentLocalPlayer()))
                {
                    data.holdingEntity.PlayOneShot("keystone_placed");
                }
                else
                {
                    data.holdingEntity.PlayOneShot("keystone_build_warning");
                    return;
                }
            }
            else if (!data.world.CanPlaceBlockAt(_bpResult.blockPos, data.world.gameManager.GetPersistentLocalPlayer()))
            {
                data.holdingEntity.PlayOneShot("keystone_build_warning");
                return;
            }
            _actionData.lastUseTime = Time.time;
            Block.list[_blockValue.type].PlaceBlock((WorldBase)data.world, _bpResult, data.holdingEntity);
            QuestEventManager.Current.BlockPlaced(Block.list[_blockValue.type].GetBlockName());
            data.holdingEntity.RightArmAnimationUse = true;
            if (this.changeItemTo != null)
            {
                ItemValue _itemValue = ItemClass.GetItem(this.changeItemTo);
                if (!_itemValue.IsEmpty())
                    data.holdingEntity.inventory.SetItem(data.holdingEntity.inventory.holdingItemIdx, new ItemStack(_itemValue, 1));
            }
            else
                GameManager.Instance.StartCoroutine(this.decInventoryLater(data, data.holdingEntity.inventory.holdingItemIdx));
            data.holdingEntity.PlayOneShot(this.soundStart == null ? "placeblock" : this.soundStart);
        }
    }
}