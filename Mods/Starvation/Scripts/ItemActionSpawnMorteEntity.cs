using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ItemActionSpawnMorteEntity : ItemAction
{
    protected int entityId = -1;
    protected string entityToSpawn;

    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);

        if (_props.Values.ContainsKey("entityToSpawn"))
            entityToSpawn = _props.Values["entityToSpawn"].ToString();
        this.Delay = 20;
    }

    public override void StartHolding(ItemActionData _actionData)
    {
    }

    public override void CancelAction(ItemActionData _actionData)
    {
    }

    public override void StopHolding(ItemActionData _actionData)
    {

    }

    public override void OnHoldingUpdate(ItemActionData _actionData)
    {

    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        if (!_bReleased || (double)Time.time - (double)_actionData.lastUseTime < (double)this.Delay || (double)Time.time - (double)_actionData.lastUseTime < (double)Constants.cBuildIntervall)
            return;

        ItemInventoryData itemInventoryData = _actionData.invData;
        if (this.entityId < 0)
        {
            using (Dictionary<int, EntityClass>.KeyCollection.Enumerator enumerator = EntityClass.list.Keys.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int current = enumerator.Current;
                    if (EntityClass.list[current].entityClassName == this.entityToSpawn)
                    {
                        this.entityId = current;
                        break;
                    }
                }
            }
            if (this.entityId == 0)
                return;
        }
        Vector3 vector3 = this.HC(_actionData);
        if (vector3.Equals((object)Vector3.zero))
            return;
        try
        {
            EntityCreationData _es = new EntityCreationData();
            _es.id = -1;
            _es.pos = vector3;
            //_es.id = entityId;
            _es.entityName = entityToSpawn;
            _es.entityClass = EntityClass.FromString(entityToSpawn);
            _es.onGround = true;
            _es.rot = Vector3.zero;
            Debug.Log("Going to send the package with " + entityToSpawn);
            GameManager.Instance.RequestToSpawnEntityServer(_es);
            _es = null;
            itemInventoryData.holdingEntity.inventory.DecItem(itemInventoryData.itemValue, 1);
        }
        catch (Exception)
        {
            return;
        }
        itemInventoryData.holdingEntity.RightArmAnimationUse = true;
    }

    private Vector3 HC([In] ItemActionData obj0)
    {
        Ray lookRay = obj0.invData.holdingEntity.GetLookRay();
        if (Voxel.Raycast(GameManager.Instance.World, lookRay, 5f, true, false))
            return Voxel.voxelRayHitInfo.hit.pos;
        return Vector3.zero;
    }

}