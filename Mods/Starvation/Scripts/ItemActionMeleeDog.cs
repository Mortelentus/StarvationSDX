using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ItemActionMeleeDog : ItemActionAttack
{
    protected Dictionary<string, float> ToolBonuses = new Dictionary<string, float>();
    private float U;
    private float dmgMultiplier = 1;

    public override ItemActionData CreateModifierData(ItemInventoryData _invData, int _indexInEntityOfAction)
    {
        return (ItemActionData)new ItemActionMeleeDog.InventoryDataMelee(_invData, _indexInEntityOfAction);
    }

    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);
        using (Dictionary<string, object>.KeyCollection.Enumerator enumerator = _props.Values.Keys.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                string current = enumerator.Current;
                if (current.StartsWith("ToolCategory."))
                    this.ToolBonuses[current.Substring("ToolCategory.".Length)] = Utils.ParseFloat(_props.Values[current]);
            }
        }
    }

    public override ItemClass.EnumCrosshairType GetCrosshairType(ItemActionData _actionData)
    {
        return this.isShowOverlay((ItemActionAttackData)_actionData) ? ItemClass.EnumCrosshairType.Damage : ItemClass.EnumCrosshairType.Plus;
    }

    public override WorldRayHitInfo GetExecuteActionTarget(ItemActionData _actionData)
    {
        ItemActionMeleeDog.InventoryDataMelee inventoryDataMelee = (ItemActionMeleeDog.InventoryDataMelee)_actionData;
        EntityAlive entityAlive1 = inventoryDataMelee.invData.holdingEntity;
        inventoryDataMelee.ray = entityAlive1.GetLookRay();
        if (entityAlive1.IsBreakingBlocks && (double)inventoryDataMelee.ray.direction.y < 0.0)
        {
            inventoryDataMelee.ray.direction = new Vector3(inventoryDataMelee.ray.direction.x, 0.0f, inventoryDataMelee.ray.direction.z);
            inventoryDataMelee.ray.origin += new Vector3(0.0f, -0.7f, 0.0f);
        }
        inventoryDataMelee.ray.origin -= 0.15f * inventoryDataMelee.ray.direction;
        int modelLayer = entityAlive1.GetModelLayer();
        entityAlive1.SetModelLayer(2);
        float distance = Utils.FastMax(this.Range, this.BlockRange) + 0.15f;
        if (entityAlive1 is EntityEnemy && entityAlive1.IsBreakingBlocks)
        {
            Voxel.Raycast(inventoryDataMelee.invData.world, inventoryDataMelee.ray, distance, 65536, 128, this.SphereRadius);
        }
        else
        {
            EntityAlive entityAlive2 = (EntityAlive)null;
            if (Voxel.Raycast(inventoryDataMelee.invData.world, inventoryDataMelee.ray, distance, -1486853, 128, this.SphereRadius))
                entityAlive2 = ItemActionAttack.GetEntityFromHit(Voxel.voxelRayHitInfo) as EntityAlive;
            if ((Object)entityAlive2 == (Object)null || !entityAlive2.IsAlive())
                Voxel.Raycast(inventoryDataMelee.invData.world, inventoryDataMelee.ray, distance, -1224709, 128, this.SphereRadius);
        }
        entityAlive1.SetModelLayer(modelLayer);
        return Voxel.voxelRayHitInfo.Clone();
    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        ItemActionMeleeDog.InventoryDataMelee inventoryDataMelee = (ItemActionMeleeDog.InventoryDataMelee)_actionData;
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
                _actionData.invData.holdingEntity.RightArmAnimationAttack = true;
                inventoryDataMelee.bHarvesting = this.IV(_actionData);
                if (inventoryDataMelee.bHarvesting)
                    _actionData.invData.holdingEntity.HarvestingAnimation = true;
                string clipName = this.soundStart;
                if (inventoryDataMelee.bHarvesting && this.soundHarvesting != null)
                    clipName = this.soundHarvesting;
                if (clipName != null)
                    _actionData.invData.holdingEntity.PlayOneShot(clipName);
                inventoryDataMelee.bAttackStarted = true;
                if ((double)inventoryDataMelee.invData.holdingEntity.speedForward > 0.009)
                    this.U = AnimationDelayData.AnimationDelay[inventoryDataMelee.invData.item.HoldType.Value].RayCastMoving;
                else
                    this.U = AnimationDelayData.AnimationDelay[inventoryDataMelee.invData.item.HoldType.Value].RayCast;
            }
        }
    }

    protected override bool isShowOverlay(ItemActionAttackData actionData)
    {
        if (!base.isShowOverlay(actionData) || ((ItemActionMeleeDog.InventoryDataMelee)actionData).bFirstHitInARow && (double)Time.time - (double)actionData.lastUseTime <= (double)this.U)
            return false;
        WorldRayHitInfo executeActionTarget = this.GetExecuteActionTarget((ItemActionData)actionData);
        return executeActionTarget.bHitValid && (executeActionTarget.tag == null || !executeActionTarget.tag.StartsWith("T_Mesh") || (double)executeActionTarget.hit.distanceSq <= (double)this.BlockRange * (double)this.BlockRange) && (executeActionTarget.tag == null || !executeActionTarget.tag.StartsWith("E_") || (double)executeActionTarget.hit.distanceSq <= (double)this.Range * (double)this.Range);
    }

    private bool IV([In] ItemActionData obj0)
    {
        WorldRayHitInfo executeActionTarget = this.GetExecuteActionTarget(obj0);
        ItemValue itemValue = obj0.invData.itemValue;
        ItemActionAttack.AttackHitInfo _attackDetails = new ItemActionAttack.AttackHitInfo();
        float damageToDoEntity = this.GetDamageEntity(itemValue, obj0.invData.holdingEntity as EntityPlayer) *
                           dmgMultiplier;
        float damageToDoBlock = this.GetDamageBlock(itemValue, obj0.invData.holdingEntity as EntityPlayer) *
                           dmgMultiplier;
        //Debug.Log("DamageEntity (2) = " + damageToDo.ToString("#0.##"));
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

    public override bool IsActionRunning(ItemActionData _actionData)
    {
        ItemActionMeleeDog.InventoryDataMelee inventoryDataMelee = (ItemActionMeleeDog.InventoryDataMelee)_actionData;
        return inventoryDataMelee.bAttackStarted && (double)Time.time - (double)inventoryDataMelee.lastUseTime < 2.0 * (double)this.U;
    }

    public override void OnHoldingUpdate(ItemActionData _actionData)
    {
        ItemActionMeleeDog.InventoryDataMelee _actionData1 = (ItemActionMeleeDog.InventoryDataMelee)_actionData;
        if (!_actionData1.bAttackStarted || (double)Time.time - (double)_actionData1.lastUseTime < (double)this.U)
            return;
        _actionData1.bAttackStarted = false;
        if (!_actionData.invData.holdingEntity.IsAttackValid())
            return;
        EntityPlayer entityPlayer = _actionData.invData.holdingEntity as EntityPlayer;
        if ((Object)entityPlayer != (Object)null && this.staminaUsage > 0)
        {
            float num = (float)this.staminaUsage;
            entityPlayer.Skills.ModifyValue(Skill.Effects.StaminaDegradation, ref num, _actionData.invData.itemValue.type, false);
            _actionData.invData.holdingEntity.AddStamina(-num);
            _actionData.invData.holdingEntity.AddCoreTemp(num * WeatherSurvivalParameters.DegreesPerPointOfStaminaUsed);
        }
        else
        {
            _actionData.invData.holdingEntity.AddStamina(-this.staminaUsage);
            _actionData.invData.holdingEntity.AddCoreTemp((float)this.staminaUsage * WeatherSurvivalParameters.DegreesPerPointOfStaminaUsed);
        }
        WorldRayHitInfo executeActionTarget = this.GetExecuteActionTarget(_actionData);
        if (!executeActionTarget.bHitValid || executeActionTarget.tag != null && executeActionTarget.tag.StartsWith("T_Mesh") && (double)executeActionTarget.hit.distanceSq > (double)this.BlockRange * (double)this.BlockRange || executeActionTarget.tag != null && executeActionTarget.tag.StartsWith("E_") && (double)executeActionTarget.hit.distanceSq > (double)this.Range * (double)this.Range)
            return;
        if (_actionData1.invData.itemValue.MaxUseTimes > 0)
        {
            ItemValue itemValue = _actionData.invData.itemValue;
            itemValue.UseTimes += AttributeBase.GetVal<AttributeDegradationRate>(_actionData.invData.itemValue, 1);
            _actionData1.invData.itemValue = itemValue;
        }
        this.hitTheTarget(_actionData1, executeActionTarget);
        if (!_actionData1.bFirstHitInARow)
            return;
        _actionData1.bFirstHitInARow = false;
    }

    public void SetDmgMultiplier(float multiplier)
    {
        dmgMultiplier = multiplier;
    }

    protected virtual void hitTheTarget(ItemActionMeleeDog.InventoryDataMelee _actionData, WorldRayHitInfo hitInfo)
    {
        float staminaMultiplier = _actionData.invData.holdingEntity.GetStaminaMultiplier();
        ItemValue itemValue = _actionData.invData.itemValue;
        float _weaponCondition = 1f;
        if (_actionData.invData.itemValue.MaxUseTimes > 0)
            _weaponCondition = (float)(_actionData.invData.itemValue.MaxUseTimes - itemValue.UseTimes) / (float)_actionData.invData.itemValue.MaxUseTimes;
        float _criticalHitChance = _actionData.invData.item.CritChance.Value;
        if ((double)_criticalHitChance > 0.0)
        {
            float num = (float)(_actionData.invData.holdingEntity.Stamina - 50) / 500f;
            _criticalHitChance = Mathf.Clamp01(_criticalHitChance + num);
        }
        float damageToDoEntity = this.GetDamageEntity(itemValue, _actionData.invData.holdingEntity as EntityPlayer)*
                           dmgMultiplier;
        float damageToDoBlock = this.GetDamageBlock(itemValue, _actionData.invData.holdingEntity as EntityPlayer) *
                           dmgMultiplier;
        Debug.Log("DamageEntity (1) = " + damageToDoEntity.ToString("#0.##"));
        //ItemActionAttack.Hit(_actionData.invData.world, hitInfo, _actionData.invData.holdingEntity.entityId, this.DamageType != EnumDamageSourceType.Undef ? this.DamageType : EnumDamageSourceType.Melee, damageToDoBlock, damageToDoEntity, staminaMultiplier, _weaponCondition, _criticalHitChance, this.getDismembermentBaseChance((ItemActionData)_actionData), this.getDismembermentBonus((ItemActionData)_actionData), this.item.MadeOfMaterial.id, this.damageMultiplier, this.getBuffActions((ItemActionData)_actionData), _actionData.attackDetails, this.ActionExp, this.ActionExpBonusMultiplier, this.ToolBonuses, !_actionData.bHarvesting ? ItemActionAttack.EnumAttackMode.RealNoHarvesting : ItemActionAttack.EnumAttackMode.RealAndHarvesting);
        ItemActionAttack.Hit(_actionData.invData.world, hitInfo, _actionData.invData.holdingEntity.entityId, this.DamageType != EnumDamageSourceType.Undef ? this.DamageType : EnumDamageSourceType.Melee, damageToDoBlock, damageToDoEntity, staminaMultiplier, _weaponCondition, _criticalHitChance, this.getDismembermentBaseChance((ItemActionData)_actionData), this.getDismembermentBonus((ItemActionData)_actionData), this.item.MadeOfMaterial.id, this.damageMultiplier, this.getBuffActions((ItemActionData)_actionData), _actionData.attackDetails, this.ActionExp, this.ActionExpBonusMultiplier, (ItemActionAttack)this, this.ToolBonuses, !_actionData.bHarvesting ? ItemActionAttack.EnumAttackMode.RealNoHarvesting : ItemActionAttack.EnumAttackMode.RealAndHarvesting);
        GameUtils.HarvestOnAttack((ItemActionAttackData)_actionData, this.ToolBonuses);
    }

    protected class InventoryDataMelee : ItemActionAttackData
    {
        public bool bAttackStarted;
        public Ray ray;
        public bool bHarvesting;
        public bool bFirstHitInARow;

        public InventoryDataMelee(ItemInventoryData _invData, int _indexInEntityOfAction)
          : base(_invData, _indexInEntityOfAction)
        {
        }
    }
}