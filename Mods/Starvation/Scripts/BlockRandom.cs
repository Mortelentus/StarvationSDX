using System;
using UnityEngine;
using Random = System.Random;

public class BlockLootRandom : BlockLoot
{
    private bool disableDebug = true;
    private string objectName = "";
    private string rootName = "";

    public override void Init()
    {
        base.Init();
        if (this.Properties.Values.ContainsKey("objectName"))
        {
            objectName = this.Properties.Values["objectName"];            
        }
        if (this.Properties.Values.ContainsKey("rootName"))
        {
            rootName = this.Properties.Values["rootName"];
        }
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);        
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        debugHelper.doDebug("FORCEANIMATION STATE", !disableDebug);
        Transform[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null ||
            objectName == "")
        {
            return;
        }
        debugHelper.doDebug("FORCEANIMATION STATE 1", !disableDebug);
        foreach (Transform tra in componentsInChildren)
        {
            if (tra.name.StartsWith(objectName) && tra.name != rootName)
            {
                if (tra.name == string.Format("{0}{1}", objectName, _blockValue.meta3.ToString()))
                {
                    tra.gameObject.SetActive(true);
                }
                else tra.gameObject.SetActive(false);
            }
        }
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, System.Random _rnd)
    {
        int charValue = _bpResult.blockValue.meta;
        if (_bpResult.blockValue.meta3 == 0)
        {            
            System.Random _random = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            charValue = _random.Next(1, 90);
            if (charValue <= 30) charValue = 1;
            else if (charValue <= 60) charValue = 2;
            else charValue = 3;
            _bpResult.blockValue.meta3 = (byte) charValue;            
        }
        debugHelper.doDebug("BlockRandom: Type is " + charValue, !disableDebug);
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }
}
