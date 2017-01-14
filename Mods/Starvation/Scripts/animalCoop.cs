using System;
using System.Collections.Generic;
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
public class BlockAnimalCoop : BlockLoot
{
    private bool debug = false;

    private bool disableDebug = true;
    AnimalCoopScript script;
    UnityEngine.GameObject gameObject;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    public BlockAnimalCoop()
    {
        //this.IsRandomlyTick = true;
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
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override BlockValue OnBlockPlaced(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Random _rnd)
    {
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
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);       
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    // process transformation on each tick
    public bool DoTransform(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool produce)
    {       
        //Debug.Log("TRANSFORMER TICK");
        bool resultado = false;
        try
        {
            System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
            int maxAnimals = 4;
            string foodName = "";
            string waterName = "";
            string animalName = "";
            char[] splitter = new char[1];
            string[] auxStrings;
            splitter[0] = ',';
            string productName; // up to 2 items
            ItemValue fooObject = (ItemValue) null;
            ItemValue waterObject = (ItemValue) null;
            ItemValue animalObject = (ItemValue) null;
            ItemValue[] ProductObject = null;
            try
            {
                #region finds the food object

                if (this.Properties.Values.ContainsKey("foodName"))
                {
                    foodName = this.Properties.Values["foodName"];
                }
                fooObject = ItemClass.GetItem(foodName);
                //DisplayChatAreaText(string.Format("FOOD: {0} - {1}", foodName, fooObject.ToString()));

                #endregion;

                #region finds the water object

                if (this.Properties.Values.ContainsKey("waterName"))
                {
                    waterName = this.Properties.Values["waterName"];
                }
                waterObject = ItemClass.GetItem(waterName);
                //DisplayChatAreaText(string.Format("WATER: {0} - {1}", waterName, waterObject.ToString()));

                #endregion;

                #region finds the animal object

                if (this.Properties.Values.ContainsKey("animalName"))
                {
                    animalName = this.Properties.Values["animalName"];
                }
                animalObject = ItemClass.GetItem(animalName);
                //DisplayChatAreaText(string.Format("ANIMAL: {0} - {1}", animalName, animalObject.ToString()));

                #endregion;

                #region finds the product objects

                if (this.Properties.Values.ContainsKey("productName"))
                {
                    productName = this.Properties.Values["productName"];
                    auxStrings = productName.Split(splitter);
                    if (auxStrings.Length > 1)
                    {
                        ProductObject = new ItemValue[2];
                        ProductObject[0] = ItemClass.GetItem(auxStrings[0]);
                        //DisplayChatAreaText(string.Format("PRODUCES(1): {0} - {1}", auxStrings[0], ProductObject[0].ToString()));
                        ProductObject[1] = ItemClass.GetItem(auxStrings[1]);
                        //DisplayChatAreaText(string.Format("PRODUCES(2): {0} - {1}", auxStrings[1], ProductObject[1].ToString()));
                    }
                    else
                    {
                        ProductObject = new ItemValue[1];
                        ProductObject[0] = ItemClass.GetItem(productName);
                        //DisplayChatAreaText(string.Format("PRODUCES: {0} - {1}", productName, ProductObject[0].ToString()));
                    }
                }                

                #endregion;
            }
            catch (Exception ex)
            {
               Debug.Log("ANIMALCOOP (1) - error finding objects");
            }
            if (animalObject != null)
            {
                int animalNumber = 0;
                int foodCount = 0;
                int waterCount = 0;
                string pos = "0";
                try
                {
                    // try to find the godamn loot container list
                    //TileEntitySecureLootContainer Container;
                    TileEntityLootContainer Container;
                    TileEntity tileEntity = (TileEntity) null;
                    tileEntity = _world.GetTileEntity(_cIdx, _blockPos);
                    if (tileEntity != null)
                    {
                        pos = "1";
                        if (tileEntity is TileEntityLootContainer)
                        {
                            Container = (TileEntityLootContainer) tileEntity;
                            pos = "2";
                            // finds first with fuel and stack>=fuelnumber
                            // finds first with raw and stack>=rawnumber if defined
                            // finds first emtpy
                            // finds first with emptyshell if defined.
                            // finds first with end product.
                            ItemStack animalStack = (ItemStack) null;
                            foreach (ItemStack itemStack1 in Container.items)
                            {
                                if (animalObject != null)
                                {
                                    #region Checks animal count

                                    if (itemStack1.itemValue.Equals(animalObject))
                                    {
                                        animalNumber += itemStack1.count;
                                        if (animalStack == null)
                                            animalStack = itemStack1;
                                    }

                                    #endregion;
                                }
                                if (fooObject != null)
                                {
                                    #region Checks food count

                                    if (itemStack1.itemValue.Equals(fooObject))
                                    {
                                        foodCount += itemStack1.count;
                                    }

                                    #endregion;
                                }
                                if (waterObject != null)
                                {
                                    #region Checks food count

                                    if (itemStack1.itemValue.Equals(waterObject))
                                    {
                                        waterCount += itemStack1.count;
                                    }

                                    #endregion;
                                }
                            }
                            pos = "3";
                            if (produce)
                            {
                                bool updateContainer = false;
                                // checks if enough food and updates food state if needed
                                // checks if enough water and update water state if needed
                                int foodToEat = animalNumber;
                                int waterToDrink = 1;
                                // only a small chance to actually need water, so water lasts much longer
                                // water count is always 1, because its a big container
                                if (Rand.Next(0, 100) >= 2) waterToDrink = 0;
                                if (foodToEat > foodCount || waterToDrink > waterCount)
                                {
                                    foodToEat = foodCount;
                                    waterToDrink = 0;
                                    // not enough food or drink, there's a chance that an animal will die
                                    if (Rand.Next(0, 100) < 10)
                                    {
                                        animalStack.count--;
                                        updateContainer = true;
                                    }
                                }
                                foodCount -= foodToEat;
                                waterCount -= waterToDrink;
                                pos = "4";
                                #region removes food and water from the food;

                                foreach (ItemStack itemStack1 in Container.items)
                                {
                                    if (fooObject != null)
                                    {
                                        if (foodToEat > 0 && itemStack1.itemValue.Equals(fooObject))
                                        {
                                            if (itemStack1.count >= foodToEat)
                                            {
                                                itemStack1.count -= foodToEat;
                                                foodToEat = 0;
                                                updateContainer = true;
                                            }
                                            else
                                            {
                                                foodToEat -= itemStack1.count;
                                                itemStack1.count = 0;
                                                updateContainer = true;
                                            }
                                        }
                                    }
                                    if (waterObject != null)
                                    {
                                        if (waterToDrink > 0 && itemStack1.itemValue.Equals(waterObject))
                                        {
                                            if (itemStack1.count >= waterToDrink)
                                            {
                                                itemStack1.count -= waterToDrink;
                                                waterToDrink = 0;
                                                updateContainer = true;
                                            }
                                            else
                                            {
                                                waterToDrink -= itemStack1.count;
                                                itemStack1.count = 0;
                                                updateContainer = true;
                                            }
                                        }
                                    }
                                }
                                #endregion;

                                if (updateContainer) Container.SetModified();
                                pos = "5";
                                if (animalNumber < maxAnimals)
                                {
                                    // tries to reproduce if lower then max animals  
                                    if (Rand.Next(0, 100) < 5)
                                    {
                                        animalStack.count++;
                                        updateContainer = true;
                                    }
                                }
                                pos = "6";
                                if (animalNumber > 0)
                                {
                                    // produces the items (eggs, feathers, turd....)
                                    if (Rand.Next(0, 100) < (15*animalNumber))
                                    {
                                        if (ProductObject != null)
                                        {
                                            foreach (ItemValue itemP in ProductObject)
                                            {
                                                //DisplayChatAreaText("ANIMALCOOP - PRODUCING: " + itemP.ToString());
                                                ItemStack prod = new ItemStack(itemP, 1);
                                                if (!Container.TryStackItem(0, prod)) Container.AddItem(prod);
                                            }
                                        }
                                    }
                                }
                            }
                            pos = "7";
                            bool needUpdate = false;

                            #region updates the different parameters to animate;

                            if ((animalNumber > 0 && !HasAnimals(_blockValue.meta2)) ||
                                (animalNumber > 0 && HasAnimals(_blockValue.meta2) &&
                                 _blockValue.meta3 != (animalNumber - 1)) ||
                                 (animalNumber == 0 && HasAnimals(_blockValue.meta2)))
                            {
                                if (animalNumber > 0)
                                {
                                    _blockValue.meta3 = (byte) (animalNumber - 1);
                                    _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 2));
                                }
                                else
                                {
                                    _blockValue.meta3 = 0;
                                    _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 2));
                                }
                                needUpdate = true;
                            }
                            pos = "8";
                            if (!HasFood(_blockValue.meta2) && foodCount > 0)
                            {
                                _blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 0));
                                needUpdate = true;
                            }
                            else if (HasFood(_blockValue.meta2) && foodCount <= 0)
                            {
                                _blockValue.meta2 = (byte) (_blockValue.meta2 & ~(1 << 0));
                                needUpdate = true;
                            }
                            if (!HasWater(_blockValue.meta2) && waterCount > 0)
                            {
                                _blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 1));
                                needUpdate = true;
                            }
                            else if (HasWater(_blockValue.meta2) && waterCount <= 0)
                            {
                                _blockValue.meta2 = (byte) (_blockValue.meta2 & ~(1 << 1));
                                needUpdate = true;
                            }
                            pos = "9";
                            if (needUpdate) _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                            pos = "10";
                            #endregion;
                        }
                    }
                    else
                        Debug.Log("No container for this object");
                }
                catch (Exception ex)
                {
                    Debug.Log("ANIMALCOOP(2) - ERROR AT POS " + pos + " - " + ex.Message);
                }
            }
            else Debug.Log("No animal configured");
        }
        catch (Exception ex1)
        {
            Debug.Log("ANIMALCOOP (7): ERROR - " + ex1.Message);
        }
        return resultado;
    }

    public static bool HasFood(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }
    public static bool HasWater(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }
    public static bool HasAnimals(byte _metadata)
    {
        return ((int)_metadata & 1 << 2) != 0;
    }
    public static int AnimalCount(byte _metadata, byte _metadata1)
    {
        int animalNum = 0;
        if (HasAnimals(_metadata))
            animalNum = (int) _metadata1 + 1;
        return animalNum;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        //if ((int)_oldBlockValue.meta2 == (int)_newBlockValue.meta2) return;
        // trigger animation
        playAnimation(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue, true);
    }

    private void playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _blockValue, bool valueChanged)
    {
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animate(_blockValue, _ebcd);
        if (!_world.IsRemote() && valueChanged) // only runs on server        
        {
            // get the transform
            if (_ebcd != null)
            {
                try
                {
                    gameObject = _ebcd.transform.gameObject;
                    // adds the script if still not existing.
                    script = gameObject.GetComponent<AnimalCoopScript>();
                    if (script == null)
                    {
                        if (!disableDebug) Debug.Log("ANIMALCOOP: ADDING SCRIPT");
                        script = gameObject.AddComponent<AnimalCoopScript>();
                        script.initialize(_world, _blockPos, _clrIdx);
                    }
                    else if (!disableDebug)
                        Debug.Log("ANIMALCOOP: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                }
                catch (Exception ex)
                {
                    Debug.Log("ANIMALCOOP (3): Error OnBlockValueChanged - " + ex.Message);
                }
            }
        }
    }

    private void Animate(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        Transform[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null) return;

        try
        {
            // number of animals to show
            int numAnimal = AnimalCount(_blockValue.meta2, _blockValue.meta3);
            if (!disableDebug) Debug.Log("ANIMALCOOP ANIMATE WITH ANIMALCOUNT=" + numAnimal);
            for (int i = 1; i < 5; i++)
            {
                string animal = string.Format("Animal{0}", i);
                foreach (Transform tra in componentsInChildren)
                {
                    if (tra.name == animal)
                    {
                        if (numAnimal >= i)
                            tra.gameObject.SetActive(true);
                        else tra.gameObject.SetActive(false);
                        break;
                    }
                }
            }
            // water && food
            foreach (Transform tra in componentsInChildren)
            {
                if (tra.name == "Water")
                {
                    if (HasWater(_blockValue.meta2))
                        tra.gameObject.SetActive(true);
                    else tra.gameObject.SetActive(false);
                }
                if (tra.name == "Food")
                {
                    if (HasFood(_blockValue.meta2))
                        tra.gameObject.SetActive(true);
                    else tra.gameObject.SetActive(false);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("ANIMALCOOP (4): Error ANIMATING - " + ex.Message);
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        Animate(_blockValue, _ebcd);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {        
        //DisplayChatAreaText("ON BLOCK LOADED");
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        //if (!_world.IsRemote())
        //    _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("ANIMALCOOP: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue); // saving a precious bit here
    }
}

public class AnimalCoopScript : MonoBehaviour
{
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    ulong tickRate = 10; //in seconds
    ulong transformRate = 10; //in seconds
    private string animalSound = "";
    private string wakeUpSound = "";
    private int numberToPause = 0;
    private bool lastCheckWasNight = false;
    DateTime dtaNextTick = DateTime.MinValue;
    DateTime dtaNextSound = DateTime.MinValue;
    DateTime dtaNextWakeup = DateTime.MinValue;
    DateTime dtaNextTransform = DateTime.MinValue;
    private bool debug = false;
    System.Random Rand = new System.Random();

    void Start()
    {

    }

    public void initialize(WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        blockValue = _world.GetBlock(cIdx, blockPos);
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("animalSound"))
        {
            animalSound = Block.list[blockValue.type].Properties.Values["animalSound"];
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("wakeUpSound"))
        {
            wakeUpSound = Block.list[blockValue.type].Properties.Values["wakeUpSound"];
        }
        tickRate = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["TickRate"], out tickRate) == false) tickRate = 10;
        }
        transformRate = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("LifeRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["LifeRate"], out transformRate) == false) transformRate = 10;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        world = _world;
    }

    void Update()
    {
        if (world != null)
        {                        
            if (DateTime.Now > dtaNextTransform)
            {
                dtaNextTransform = DateTime.Now.AddSeconds(transformRate);
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    (Block.list[blockValue.type] as BlockAnimalCoop).DoTransform(world, cIdx, blockPos, blockValue,
                        true);                    
                }
                catch (Exception ex)
                {
                    Debug.Log("ANIMALCOOP (5): ERROR - " + ex.Message);                    
                }                
            }
            else if (DateTime.Now > dtaNextTick)
            {                
                dtaNextTick = DateTime.Now.AddSeconds(tickRate);
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    if (BlockAnimalCoop.AnimalCount(blockValue.meta2, blockValue.meta3) > 0)
                    {
                        if (DateTime.Now > dtaNextSound || (GameManager.Instance.World.IsDaytime() && lastCheckWasNight))
                        {
                            int nextSound = 0;
                            string soundToPlay = animalSound;
                            bool wakeUp = false;
                            if (!GameManager.Instance.World.IsDaytime())
                            {
                                nextSound = Rand.Next(120, 240);
                                lastCheckWasNight = true;
                            }
                            else
                            {
                                nextSound = Rand.Next(30, 60);
                                if (lastCheckWasNight && wakeUpSound != "") soundToPlay = wakeUpSound;
                                lastCheckWasNight = false;
                            }
                            dtaNextSound = DateTime.Now.AddSeconds(nextSound);
                            // play sound more often in the day then in the night.
                            if (soundToPlay != null)
                                Audio.Manager.BroadcastPlay(blockPos.ToVector3(), soundToPlay);
                        }
                        else
                        {
                            try
                            {
                                if (animalSound != "") Audio.Manager.Stop(blockPos.ToVector3(), animalSound);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    (Block.list[blockValue.type] as BlockAnimalCoop).DoTransform(world, cIdx, blockPos, blockValue,
                        false);
                }
                catch (Exception ex)
                {
                    debugHelper.doDebug("ANIMALCOOP (6): ERROR - " + ex.Message, debug);
                }
            }            
        }
    }
}