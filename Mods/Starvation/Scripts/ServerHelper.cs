﻿using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
public static class auxHelper
{
    public static bool IsInvOpened(TileEntityLootContainer lootC)
    {
        if (GameManager.IsDedicatedServer)
        {
            if (GameManager.Instance.BS.ContainsKey(lootC as TileEntity))
            {
                if (GameManager.Instance.World.GetEntity(GameManager.Instance.BS[lootC as TileEntity]) != null)
                    return true;
            }
        }        
        return false;
    }
}