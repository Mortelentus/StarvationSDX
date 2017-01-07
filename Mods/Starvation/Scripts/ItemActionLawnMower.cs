#if !IsBroken
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
/// <summary>
/// Custom class for Lawn Mower Operation
/// Mortelentus 2016 - v1.0
/// </summary>
public class ItemActionLawnMower : ItemActionMelee
{
    private float Q;

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        ItemActionMelee.InventoryDataMelee inventoryDataMelee = (ItemActionMelee.InventoryDataMelee)_actionData;
        if (_bReleased)
        {
            inventoryDataMelee.bFirstHitInARow = true;
        }
        else
        {
            if ((double)Time.time - (double)inventoryDataMelee.lastUseTime < (double)this.Delay)
                return;
            inventoryDataMelee.lastUseTime = Time.time;
            if (inventoryDataMelee.invData.itemValue.MaxUseTimes > 0 && inventoryDataMelee.invData.itemValue.UseTimes >= inventoryDataMelee.invData.itemValue.MaxUseTimes)
            {
                if (this.item.Properties.Values.ContainsKey(ItemClass.PropSoundJammed))
                    GameManager.Instance.ShowTooltipWithAlert("ttItemNeedsRepair", this.item.Properties.Values[ItemClass.PropSoundJammed]);
                else
                    GameManager.Instance.ShowTooltip("ttItemNeedsRepair");
            }
            else
            {
                //_actionData.invData.holdingEntity.RightArmAnimationAttack = true;
                inventoryDataMelee.bHarvesting = this.GV(_actionData);
                if (inventoryDataMelee.bHarvesting)
                    _actionData.invData.holdingEntity.HarvestingAnimation = true;
                string clipName = this.soundStart;
                if (inventoryDataMelee.bHarvesting && this.soundHarvesting != null)
                    clipName = this.soundHarvesting;
                if (clipName != null)
                    _actionData.invData.holdingEntity.PlayOneShot(clipName);
                inventoryDataMelee.bAttackStarted = true;
                if ((double)inventoryDataMelee.invData.holdingEntity.speedForward > 0.009)
                    this.Q = AnimationDelayData.AnimationDelay[inventoryDataMelee.invData.item.HoldType.Value].RayCastMoving;
                else
                    this.Q = AnimationDelayData.AnimationDelay[inventoryDataMelee.invData.item.HoldType.Value].RayCast;
            }
        }
    }

    public override bool IsActionRunning(ItemActionData _actionData)
    {
        ItemActionMelee.InventoryDataMelee inventoryDataMelee = (ItemActionMelee.InventoryDataMelee)_actionData;
        return inventoryDataMelee.bAttackStarted && (double)Time.time - (double)inventoryDataMelee.lastUseTime < 2.0 * (double)this.Q;
    }

    private bool GV([In] ItemActionData obj0)
    {
        WorldRayHitInfo executeActionTarget = this.GetExecuteActionTarget(obj0);
        ItemValue itemValue = obj0.invData.itemValue;
        ItemActionAttack.AttackHitInfo _attackDetails = new ItemActionAttack.AttackHitInfo();
        ItemActionAttack.Hit(obj0.invData.world, executeActionTarget, obj0.invData.holdingEntity.entityId, this.DamageType != EnumDamageSourceType.Undef ? this.DamageType : EnumDamageSourceType.Melee, this.GetDamageBlock(itemValue, obj0.invData.holdingEntity as EntityPlayer), this.GetDamageEntity(itemValue, obj0.invData.holdingEntity as EntityPlayer), 1f, 1f, 0.0f, this.getDismembermentBaseChance(obj0), this.getDismembermentBonus(obj0), this.item.MadeOfMaterial.id, this.damageMultiplier, this.getBuffActions(obj0), _attackDetails, this.ActionExp, this.ActionExpBonusMultiplier, (ItemActionAttack)this, this.ToolBonuses, ItemActionAttack.EnumAttackMode.Simulate);
        if (_attackDetails.bKilled || _attackDetails.itemsToDrop == null || !_attackDetails.itemsToDrop.ContainsKey(EnumDropEvent.Harvest))
            return false;
        List<Block.SItemDropProb> list = _attackDetails.itemsToDrop[EnumDropEvent.Harvest];
        for (int index = 0; index < list.Count; ++index)
        {
            if (list[index].toolCategory != null && this.ToolBonuses != null && this.ToolBonuses.ContainsKey(list[index].toolCategory))
                return true;
        }
        return false;
    }
}
#endif