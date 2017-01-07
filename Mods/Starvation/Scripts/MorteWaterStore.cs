using System;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Custom class for water rain storage
/// Mortelentus 2016 - v1.0
/// </summary>
public class BlockMorteWaterStore : Block
{
    private bool disableDebug = true;

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
            str = "WATER STORE: " + str;
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

    public BlockMorteWaterStore() 
    {
        this.IsRandomlyTick = true;
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        DisplayChatAreaText("TICK with rain = " + WeatherManager.theInstance.GetCurrentRainfallValue());
        float rainLevel = WeatherManager.theInstance.GetCurrentRainfallValue();
        // check if it's raining.
        // if it is, spawns water (bucket) on top of himself if not water already
        //if ((double) WeatherManager.theInstance.GetCurrentRainfallValue() >= 0.25)
        if (rainLevel != 0.0F)
        {            
            for (int i = 1; i <= 2; i++)
            {
                // spawns water up to 2 blocks on top, not all at the same time.
                if (_world.IsOpenSkyAbove(_clrIdx, _blockPos.x, _blockPos.y + i, _blockPos.z))
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(_blockPos.x, _blockPos.y + i, _blockPos.z));
                    if (block.type == 0)
                    {
                        DisplayChatAreaText("SPAWN WATER ON TOP " + i);
                        // spawn water
                        BlockValue offBlock = Block.GetBlockValue("water");
                        GameManager.Instance.World.SetBlockRPC(_clrIdx,
                            new Vector3i(_blockPos.x, _blockPos.y + i, _blockPos.z),
                            offBlock);
                        // get out, cause i want it to take some time to fill
                        break;
                    }
                }
                else break; // if the is not open sky already, well I can break out.
            }
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }
}