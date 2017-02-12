using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using SDX.Payload;

// By default, a survivor will run away from zombies, unless its of a specific type
// Hunters, will track and attack animals -> "entityanimal"
// "Guards", will track and attack zombies -> "entityzombie" -> maybe custom to attack enemy players
// You can order a zombie to attack zombies "targeted" by the "owner"
// If it is ordered to follow a person, it will resume following if it has no attack tasks to perform.

public class clsDialog
{
    public string dialogText = "";
    public string commandType = "";
    public string questName = "";
    public string previousquestName = "";
    public string skillName = "";
    public int skillReq = 0;
    public string blockDialogs = "";
    public string refusalAction = "";
    public bool spawn = false;
    public string spawnText = "";
}

public class QuoteRandomizer
{
    public enum QuoteType
    {
        Attack,
        Flee,
        Greeting,
        Dismiss,
        Refuse,
        Accept
    }
    private static List<string> attackQuotes = new List<string>();
    private static List<string> fleeQuotes = new List<string>();
    private static List<string> greetingQuotes = new List<string>();
    private static List<string> dismissQuotes = new List<string>();
    private static List<string> refusalQuotes = new List<string>();
    private static List<string> acceptQuotes = new List<string>();

    static QuoteRandomizer()
    {
        attackQuotes.Add("I'LL CHOP OFF YOUR NANNER AND SHIT DOWN YOUR NECK");
        attackQuotes.Add("RUMBLE TIME!");
        attackQuotes.Add("Do you require medication ?");
        attackQuotes.Add("I wouldn't if I were you.");
        attackQuotes.Add("You're no zed, but I will put you in the ground just the same.");
        attackQuotes.Add("Normally I hunt zombies but for you I'll make an exception");
        attackQuotes.Add("You'll find it difficult to speak, without a head");
        attackQuotes.Add("I do not suffer fools easily");

        fleeQuotes.Add("To me! TO MEE!!");
        fleeQuotes.Add("Why you do this?");
        fleeQuotes.Add("Does this amuse yourself!?");
        fleeQuotes.Add("Are you insane?");

        greetingQuotes.Add("Heeey, how ya doing?");
        greetingQuotes.Add("Wazzup?");
        greetingQuotes.Add("You need something?");
        greetingQuotes.Add("Come, speak with me");
        greetingQuotes.Add("Welcome friend");
        greetingQuotes.Add("Interest ya'n a pint?");

        dismissQuotes.Add("Keep your feet on the ground");
        dismissQuotes.Add("Good health, long life");
        dismissQuotes.Add("If we do not meet again... die well");
        dismissQuotes.Add("There's work to be done");
        dismissQuotes.Add("Beware the red moon");
        dismissQuotes.Add("You come back sometime");

        refusalQuotes.Add("You lost me at hello...");
        refusalQuotes.Add("You have other matters to attend to yes?");
        refusalQuotes.Add("You're startin' to bother me");
        refusalQuotes.Add("Blah, blah blah blah blah are you through?");
        refusalQuotes.Add("Would you knock it off?");
        refusalQuotes.Add("Fuck off, do I know you from anywhere?");
        refusalQuotes.Add("I'm sorry, but boss hasn't told me anything about you...");
        refusalQuotes.Add("And you are?");

        acceptQuotes.Add("Pleasure doing business with ya");
        acceptQuotes.Add("Hey, I got your back");
        acceptQuotes.Add("Glad I can help");
        acceptQuotes.Add("Alrighty, then!");
        acceptQuotes.Add("Always glad to help");
    }

    public static string GetQuote(QuoteType quoteType)
    {
        System.Random rnd = new System.Random();
        string quote = "";
        List<string> quotes = new List<string>();
        string color = "";
        if (quoteType == QuoteType.Attack)
        {
            color = "[FF6700]";
            quotes = attackQuotes;
        }
        else if (quoteType == QuoteType.Flee)
        {
            color = "[FFDF00]";
            quotes = fleeQuotes;
        }
        else if (quoteType == QuoteType.Dismiss) quotes = dismissQuotes;
        else if (quoteType == QuoteType.Greeting) quotes = greetingQuotes;
        else if (quoteType == QuoteType.Refuse)
        {
            color = "[E11818]";
            quotes = refusalQuotes;
        }
        else if (quoteType == QuoteType.Accept)
        {
            color = "[B0FF00]";
            quotes = acceptQuotes;
        }
        quote = quotes[rnd.Next(0, quotes.Count)];
        return color + quote;
    }
}

public class NameGenerator
{
    private static List<string> firstNameSyllabesMale = new List<string>();
    private static List<string> firstNameSyllabesFemale = new List<string>();
    private static List<string> lastNameSyllabes = new List<string>();
    static NameGenerator()
    {
        firstNameSyllabesMale.Add("anth");
        firstNameSyllabesMale.Add("ony");
        firstNameSyllabesMale.Add("benj");
        firstNameSyllabesMale.Add("amin");
        firstNameSyllabesMale.Add("cam");
        firstNameSyllabesMale.Add("eron");
        firstNameSyllabesMale.Add("chris");
        firstNameSyllabesMale.Add("dom");
        firstNameSyllabesMale.Add("inic");
        firstNameSyllabesMale.Add("eli");
        firstNameSyllabesMale.Add("jha");
        firstNameSyllabesMale.Add("fern");
        firstNameSyllabesMale.Add("ando");
        firstNameSyllabesMale.Add("jona");
        firstNameSyllabesMale.Add("than");
        firstNameSyllabesMale.Add("oli");
        firstNameSyllabesMale.Add("ver");
        firstNameSyllabesMale.Add("zac");
        firstNameSyllabesMale.Add("chary");
        firstNameSyllabesMale.Add("se");
        firstNameSyllabesMale.Add("bas");
        firstNameSyllabesMale.Add("tion");

        firstNameSyllabesFemale.Add("abi");
        firstNameSyllabesFemale.Add("gail");
        firstNameSyllabesFemale.Add("al");
        firstNameSyllabesFemale.Add("exa");
        firstNameSyllabesFemale.Add("lison");
        firstNameSyllabesFemale.Add("aly");
        firstNameSyllabesFemale.Add("ssa");
        firstNameSyllabesFemale.Add("bri");
        firstNameSyllabesFemale.Add("ana");
        firstNameSyllabesFemale.Add("em");
        firstNameSyllabesFemale.Add("ily");
        firstNameSyllabesFemale.Add("kim");
        firstNameSyllabesFemale.Add("ber");
        firstNameSyllabesFemale.Add("jess");
        firstNameSyllabesFemale.Add("ica");
        firstNameSyllabesFemale.Add("lil");
        firstNameSyllabesFemale.Add("lian");

        lastNameSyllabes.Add("smi");
        lastNameSyllabes.Add("th");
        lastNameSyllabes.Add("john");
        lastNameSyllabes.Add("hn");
        lastNameSyllabes.Add("son");
        lastNameSyllabes.Add("nes");
        lastNameSyllabes.Add("da");
        lastNameSyllabes.Add("vis");
        lastNameSyllabes.Add("wil");
        lastNameSyllabes.Add("tay");
        lastNameSyllabes.Add("lor");
    }

    public static string CreateNewName(bool isMale)
    {
        System.Random rnd = new System.Random();
        string fullName = "";

        string firstName = "";
        int sylNumber = rnd.Next(2, 4);
        for (int i = 0; i < sylNumber; i++)
        {
            if (isMale)
                firstName += firstNameSyllabesMale[rnd.Next(0, firstNameSyllabesMale.Count)];
            else firstName += firstNameSyllabesFemale[rnd.Next(0, firstNameSyllabesFemale.Count)];
        }
        string lastName = "";
        sylNumber = rnd.Next(1, 3);
        for (int i = 0; i < sylNumber; i++)
        {
            lastName += lastNameSyllabes[rnd.Next(0, lastNameSyllabes.Count)];
        }
        firstName = firstName.Substring(0, 1).ToUpper() + firstName.Substring(1);
        lastName = lastName.Substring(0, 1).ToUpper() + lastName.Substring(1);
        fullName = firstName + " " + lastName;
        return fullName;
    }
}

public static class debugHelper
{
    private static bool debugON = false;
    public static void doDebug(string msg, bool debug)
    {
        if (debugON || debug)
            Debug.Log(msg);
    }
}

public class SurvivorHelper
{
    public string WakeupChar(Transform[] componentsInChildren, string pos, BlockValue blockValue, Vector3i blockPos,
        int cIdx, WorldBase world)
    {
        foreach (Transform tra in componentsInChildren)
        {
            pos = "5." + tra.name;
            if (tra.name.StartsWith("char") && tra.gameObject.activeInHierarchy)
            {
                pos = "5." + tra.name + ".1";
                Animator[] animatorsInChildren =
                    tra.gameObject.GetComponentsInChildren<Animator>(false);
                pos = "5." + tra.name + ".2";
                if (animatorsInChildren != null)
                {
                    foreach (Animator animator in animatorsInChildren)
                    {
                        PlayWakeUp(blockValue, blockPos, cIdx, animator, world);
                    }
                }
                else debugHelper.doDebug("Imporssible to find animator", true);
            }
        }
        return pos;
    }

    public string WakeupGeneric(GameObject gameObject, string pos, BlockValue blockValue, Vector3i blockPos,
        int cIdx, WorldBase world)
    {
        pos = "4.1";
        Animator[] animatorsInChildren =
            gameObject.GetComponentsInChildren<Animator>(false);
        pos = "4.2";
        if (animatorsInChildren != null)
        {
            foreach (Animator animator in animatorsInChildren)
            {
                PlayWakeUp(blockValue, blockPos, cIdx, animator, world);
            }
        }
        else debugHelper.doDebug("Imporssible to find animator", true);
        return pos;
    }

    private void PlayWakeUp(BlockValue blockValue, Vector3i blockPos, int cIdx, Animator animator, WorldBase world)
    {
        // play the WakeUp animation 
        if (!GameManager.IsDedicatedServer)
        {
            debugHelper.doDebug("WakeUp", false);
            animator.SetTrigger("WakeUp");
        }
        if (!world.IsRemote())
        {
            // reset sleeping bit
            blockValue.meta2 = (byte)(blockValue.meta2 & ~(1 << 0));
            debugHelper.doDebug("Setting meta2 to " + blockValue.meta2, false);
            world.SetBlockRPC(cIdx, blockPos, blockValue);
        }
    }

    public string SleepChar(Transform[] componentsInChildren, string pos, BlockValue blockValue, Vector3i blockPos,
        int cIdx, WorldBase world)
    {
        foreach (Transform tra in componentsInChildren)
        {
            pos = "4." + tra.name;
            if (tra.name.StartsWith("char") && tra.gameObject.activeInHierarchy)
            {
                pos = "4." + tra.name + ".1";
                Animator[] animatorsInChildren =
                    tra.gameObject.GetComponentsInChildren<Animator>(false);
                pos = "4." + tra.name + ".2";
                if (animatorsInChildren != null)
                {
                    foreach (Animator animator in animatorsInChildren)
                    {
                        PlaySleep(blockValue, blockPos, cIdx, animator, world);
                    }
                }
                else debugHelper.doDebug("Imporssible to find animator", true);
            }
        }
        return pos;
    }

    public string SleepGeneric(GameObject gameObject, string pos, BlockValue blockValue, Vector3i blockPos,
        int cIdx, WorldBase world)
    {
        pos = "4.1";
        Animator[] animatorsInChildren =
            gameObject.GetComponentsInChildren<Animator>(false);
        pos = "4.2";
        if (animatorsInChildren != null)
        {
            foreach (Animator animator in animatorsInChildren)
            {
                PlaySleep(blockValue, blockPos, cIdx, animator, world);
            }
        }
        else debugHelper.doDebug("Imporssible to find animator", true);
        return pos;
    }

    private void PlaySleep(BlockValue blockValue, Vector3i blockPos, int cIdx, Animator animator, WorldBase world)
    {
        // play the sleeping animation
        if (!GameManager.IsDedicatedServer)
        {
            debugHelper.doDebug("Sleeping", false);
            animator.SetTrigger("Sleep");
        }
        if (!world.IsRemote())
        {
            // set bit 1
            blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 0));
            debugHelper.doDebug("Setting meta2 to " + blockValue.meta2, false);
            world.SetBlockRPC(cIdx, blockPos, blockValue);
        }
    }

    public void ForceState(GameObject gameObject, Vector3i blockPos, int cIdx, string stateToGo, string trigger,
        string playSound)
    {
        if (!GameManager.IsDedicatedServer)
        {
            Animator[] animatorsInChildren =
                gameObject.GetComponentsInChildren<Animator>(false);
            if (animatorsInChildren != null)
            {
                foreach (Animator animator in animatorsInChildren)
                {
                    // if it's not running a transition, checks if it needs to be forced to the correct state.
                    if (!animator.IsInTransition(0))
                    {
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo) &&
                            !animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo + " 0") &&
                            !animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo + " 1") &&
                            !animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo + " 2") &&
                            !animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo + " 3") &&
                            !animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo + " 4"))
                        {
                            animator.CrossFade(stateToGo, 0.0f);
                            //animator.SetTrigger(trigger); // forces with transition, seems better.
                            //debugHelper.doDebug("Forced state to " + stateToGo, true);
                            if (playSound != "" && !GameManager.IsDedicatedServer)
                            {
                                //debugHelper.doDebug(string.Format("PLAY SOUND {0}", playSound), true);
                                //AudioManager.AudioManager.Play(blockPos.ToVector3(), playSound, 0, false, -1, -1, 0F);
                                Audio.Manager.BroadcastPlay(blockPos.ToVector3(), playSound);
                            }
                        }
                    }
                }
            }
            else debugHelper.doDebug("Imporssible to find animator", true);
        }
    }

    public void SetAnimationSpeed(GameObject gameObject, string stateToGo, int newSpeed)
    {
        if (!GameManager.IsDedicatedServer)
        {
            Animator[] animatorsInChildren =
                gameObject.GetComponentsInChildren<Animator>(false);
            if (animatorsInChildren != null)
            {
                foreach (Animator animator in animatorsInChildren)
                {
                    // if it's not running a transition, checks if it needs to be forced to the correct state.
                    if (!animator.IsInTransition(0))
                    {
                        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateToGo))
                        {
                            if (animator.speed != newSpeed)
                            {
                                animator.speed = newSpeed;
                                // removed go to frame 0 because
                                //if (newSpeed == 0) animator.playbackTime = 0;
                                //debugHelper.doDebug(
                                //    string.Format("GuardWorkScript: Forced {0} speed to {1}", stateToGo, animator.speed), false);
                            }
                        }
                    }
                }
            }
            else debugHelper.doDebug("Imporssible to find animator", true);
        }
    }

    public void ChangeTransformState(GameObject gameObject, bool enabled, string objectName, string objectName1)
    {
        if (!GameManager.IsDedicatedServer)
        {
            // if i want to make inactive, i don't need to search innactive stuff
            Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(true);
            if (componentsInChildren != null)
            {
                foreach (Transform transform in componentsInChildren)
                {
                    if (transform.name == objectName || transform.name == objectName1)
                    {
                        transform.gameObject.SetActive(enabled);
                        debugHelper.doDebug(string.Format("Set {0} to {1}", objectName, enabled), true);
                    }
                }
            }
            else debugHelper.doDebug("Imporssible to find Transform", true);
        }
    }

    public Transform FindTransform(GameObject gameObject, string objectName)
    {
        //if (!GameManager.IsDedicatedServer)
        {
            // if i want to make inactive, i don't need to search innactive stuff
            Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(true);
            if (componentsInChildren != null)
            {
                foreach (Transform transform in componentsInChildren)
                {
                    if (transform.name == objectName)
                    {
                        return transform;
                    }
                }
            }
            else debugHelper.doDebug("Imporssible to find Transform", true);
        }
        return null;
    }

    public Transform FindBody(Transform tran, string nameContains)
    {

        if (tran.gameObject.name.Contains(nameContains))
            return tran;

        foreach (Transform t in tran)
        {
            if (!t.gameObject.activeInHierarchy) continue;
            if (t.gameObject.name.Contains(nameContains) && t.transform.gameObject.activeInHierarchy)
            {
                return t;
            }

            Transform ret = FindBody(t, nameContains);
            if (ret != null)
                return ret;
        }

        return null;
    }

    // bit 0
    public bool IsSleeping(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }

    // bit 2
    public bool IsCrafting(byte _metadata)
    {
        return ((int)_metadata & 1 << 2) != 0;
    }

    // bit 3 (crafter)
    public bool IsFirstRun(byte _metadata)
    {
        return ((int)_metadata & 1 << 3) != 0;
    }

    // bit 3 (guard)
    public bool FireShot(byte _metadata)
    {
        return ((int)_metadata & 1 << 3) != 0;
    }

    // increases (or decreases) damage by a set ammount. If it goes over maxDmg destroys the survivor
    public void DmgBlock(int maxDmg, BlockValue blockValue, int dmgAmount, WorldBase world, Vector3i blockPos, int cIdx)
    {
        // since there is on entity damaging it, I'll just reduce the value and, if needed, destroy it myself making it disapear (turn to air)
        int newDamage = blockValue.damage;
        newDamage = newDamage + dmgAmount;
        if (newDamage < 0) newDamage = 0;
        if (newDamage >= maxDmg)
        {
            debugHelper.doDebug(string.Format("!!!SURVIVOR DIED - I HOPE SCRIPT STOPS!!!"), false);
            world.SetBlockRPC(cIdx, blockPos, BlockValue.Air);
        }
        else
        {
            if (newDamage != blockValue.damage)
            {
                blockValue.damage = newDamage;
                world.SetBlockRPC(cIdx, blockPos, blockValue);
            }
        }
    }

    // looks for a valid food item, and consumes it
    public bool EatFood(string[] foodItems, List<Vector3i> foodContainers, WorldBase world, int cIdx)
    {
        bool result = false;
        foreach (Vector3i foodC in foodContainers)
        {
            TileEntity tile = null;
            tile = world.GetTileEntity(cIdx, foodC);
            if (tile != null)
            {
                if (tile is TileEntitySecureLootContainer)
                {
                    //check for a existing fooditem
                    TileEntitySecureLootContainer inv = (TileEntitySecureLootContainer)tile;
                    ItemStack[] items = inv.GetItems();
                    foreach (ItemStack i in items)
                    {
                        // check if it belong to the foodlist
                        for (int j = 1; j < foodItems.Length; j++)
                        {
                            int foodID = ItemClass.GetItem(foodItems[j]).type;
                            if (foodID == i.itemValue.type && i.count > 0)
                            {
                                debugHelper.doDebug(string.Format("FOUND {0} to eat", foodItems[j]), false);
                                i.count--;
                                inv.SetModified();
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    // looks for a valid food item, and consumes it
    public bool EatFoodEnt(string[] foodItems, List<Vector3i> foodContainers, WorldBase world)
    {
        bool result = false;
        foreach (Vector3i foodC in foodContainers)
        {
            TileEntity tile = null;
            for (int _clrIdx = 0; _clrIdx < world.ChunkClusters.Count; ++_clrIdx)
            {
                tile = world.GetTileEntity(_clrIdx, foodC);
                if (tile != null)
                    break;
            }
            if (tile != null)
            {
                if (tile is TileEntitySecureLootContainer)
                {
                    //check for a existing fooditem
                    TileEntitySecureLootContainer inv = (TileEntitySecureLootContainer)tile;
                    ItemStack[] items = inv.GetItems();
                    foreach (ItemStack i in items)
                    {
                        // check if it belong to the foodlist
                        for (int j = 1; j < foodItems.Length; j++)
                        {
                            int foodID = ItemClass.GetItem(foodItems[j]).type;
                            if (foodID == i.itemValue.type && i.count > 0)
                            {
                                debugHelper.doDebug(string.Format("FOUND {0} to eat", foodItems[j]), false);
                                i.count--;
                                inv.SetModified();
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    // looks for food inside a loot container, for survivors
    // looks for a valid food item, and consumes it
    public bool EatFoodInv(string[] foodItems, TileEntity inventory, WorldBase world)
    {
        bool result = false;
        if (inventory != null)
        {
            if (!(inventory is TileEntityLootContainer)) return false;
            ItemStack[] items = (inventory as TileEntityLootContainer).GetItems();
            foreach (ItemStack i in items)
            {
                // check if it belong to the foodlist
                for (int j = 1; j < foodItems.Length; j++)
                {
                    int foodID = ItemClass.GetItem(foodItems[j]).type;
                    if (foodID == i.itemValue.type && i.count > 0)
                    {
                        i.count--;
                        inventory.SetModified();
                        return true;
                    }
                }
            }
        }
        return result;
    }

    public List<Vector3i> GetContainers(string containerName, WorldBase world, Vector3i blockPos, int cIdx,
        int checkArea)
    {
        List<Vector3i> containerList = new List<Vector3i>();
        BlockValue containerBlock = Block.GetBlockValue(containerName);
        containerList.Clear();
        for (int i = blockPos.x - checkArea; i <= (blockPos.x + checkArea); i++)
        {
            for (int j = blockPos.z - checkArea; j <= (blockPos.z + checkArea); j++)
            {
                for (int k = blockPos.y - checkArea; k <= (blockPos.y + checkArea); k++)
                {
                    BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
                    if (block.type == containerBlock.type)
                    {
                        containerList.Add(new Vector3i(i, k, j));
                    }
                }
            }
        }
        return containerList;
    }

    public List<Vector3i> GetContainersEnt(string containerName, WorldBase world, Vector3i blockPos, IChunk chunk,
        int checkArea)
    {
        List<Vector3i> containerList = new List<Vector3i>();
        Debug.Log("DO 1");
        BlockValue containerBlock = Block.GetBlockValue(containerName);
        Debug.Log("DO 2");
        containerList.Clear();
        Debug.Log("DO 3");
        for (int i = blockPos.x - checkArea; i <= (blockPos.x + checkArea); i++)
        {
            for (int j = blockPos.z - checkArea; j <= (blockPos.z + checkArea); j++)
            {
                for (int k = blockPos.y - checkArea; k <= (blockPos.y + checkArea); k++)
                {
                    Debug.Log("DO 4");
                    BlockValue block = chunk.GetBlock(blockPos.x + i, blockPos.y + k, blockPos.z + j);
                    Debug.Log("DO 5");
                    if (block.type == containerBlock.type)
                    {
                        containerList.Add(new Vector3i(blockPos.x + i, blockPos.y + k, blockPos.z + j));
                        Debug.Log("FOUND AT " + new Vector3i(blockPos.x + i, blockPos.y + k, blockPos.z + j).ToString());
                    }
                }
            }
        }
        return containerList;
    }

    public bool checkIngredient(List<Vector3i> containerList, ItemStack ingredient, WorldBase world,
        int cIdx, bool consume)
    {
        bool result = false;
        int requiredNum = ingredient.count;
        foreach (Vector3i container in containerList)
        {
            TileEntity tile = null;
            tile = world.GetTileEntity(cIdx, container);
            if (tile != null)
            {
                if (tile is TileEntitySecureLootContainer)
                {
                    //check for a existing fooditem
                    TileEntitySecureLootContainer inv = (TileEntitySecureLootContainer)tile;
                    ItemStack[] items = inv.GetItems();
                    foreach (ItemStack i in items)
                    {
                        // check if its the ingredient we're looking for
                        if (i.count > 0 && i.itemValue.type == ingredient.itemValue.type)
                        {
                            int currNum = i.count;
                            if (consume)
                            {
                                if (currNum <= requiredNum)
                                {
                                    i.Clear();
                                    inv.SetModified();
                                }
                                else
                                {
                                    i.count -= requiredNum;
                                    inv.SetModified();
                                }
                            }
                            requiredNum -= currNum;
                            if (requiredNum <= 0)
                            {
                                // there's enough items
                                result = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    public bool proccessReceipt(List<Vector3i> containerList, Recipe craftRecipe, WorldBase world,
        int cIdx)
    {
        bool result = false;
        bool canCraft = true;
        // rechecks availability just in case
        foreach (ItemStack ing in craftRecipe.ingredients)
        {
            if (!checkIngredient(containerList, ing, world, cIdx, false))
            {
                debugHelper.doDebug(string.Format("CrafterWorkScript: Missing {0} when finishing crafting",
                    ItemClass.GetForId(ing.itemValue.type).localizedName), false);
                canCraft = false;
            }
        }
        if (canCraft)
        {
            // consume items
            foreach (ItemStack ing in craftRecipe.ingredients)
            {
                if (!checkIngredient(containerList, ing, world, cIdx, true))
                {
                    debugHelper.doDebug(string.Format("CrafterWorkScript: Missing {0} when crafting",
                        ItemClass.GetForId(ing.itemValue.type).localizedName), false);
                    canCraft = false;
                }
            }
        }
        result = canCraft; // if all went well, produces the final product
        return result;
    }

    public bool craftItem(List<Vector3i> matContainerList, List<Vector3i> itemContainerList, ItemStack item,
        Recipe craftRecipe, WorldBase world,
        int cIdx)
    {
        // look for a position to place the item
        bool result = false;
        if (item == null || craftRecipe == null || world == null || matContainerList.Count == 0 ||
            itemContainerList.Count == 0) return false;
        foreach (Vector3i container in itemContainerList)
        {
            TileEntity tile = null;
            tile = world.GetTileEntity(cIdx, container);
            if (tile != null)
            {
                if (tile is TileEntitySecureLootContainer)
                {
                    //if it is possible to stack, will first look for a place to stack it.
                    // other wise searches for a empty slot
                    TileEntitySecureLootContainer invItem = (TileEntitySecureLootContainer)tile;
                    ItemStack[] items = invItem.GetItems();
                    int positionFound = 0;
                    foreach (ItemStack i in items)
                    {
                        // check if its the ingredient we're looking for
                        if (i.count > 0 && i.itemValue.type == item.itemValue.type && item.CanStackWith(i))
                        {
                            // position found
                            if (proccessReceipt(matContainerList, craftRecipe, world, cIdx))
                            {
                                // places the final product  
                                i.count += item.count;
                                invItem.SetModified();
                                debugHelper.doDebug(string.Format("CrafterWorkScript: STACKING {0} in container",
                                    ItemClass.GetForId(item.itemValue.type).localizedName), false);
                                result = true;
                            }
                            break;
                        }
                        else if (i.count == 0 || i.IsEmpty())
                        {
                            // position found
                            if (proccessReceipt(matContainerList, craftRecipe, world, cIdx))
                            {
                                // places the final product  
                                invItem.UpdateSlot(positionFound, item);
                                invItem.SetModified();
                                debugHelper.doDebug(string.Format("CrafterWorkScript: PLACING {0} in container",
                                    ItemClass.GetForId(item.itemValue.type).localizedName), false);
                                result = true;
                            }
                            break;
                        }
                        positionFound++;
                    }
                }
            }
        }
        return result;
    }

    public Vector3i GetSpotToPlaceBlock(BlockValue blockToPlace, WorldBase world, Vector3i blockPos, int cIdx,
        int checkArea)
    {
        Vector3i position = Vector3i.zero;
        for (int i = blockPos.x - checkArea; i <= (blockPos.x + checkArea); i++)
        {
            for (int j = blockPos.z - checkArea; j <= (blockPos.z + checkArea); j++)
            {
                for (int k = blockPos.y - checkArea; k <= (blockPos.y + checkArea); k++)
                {
                    BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
                    BlockValue blockTop = world.GetBlock(cIdx, new Vector3i(i, k + 1, j));
                    //if (
                    //    Block.list[blockToPlace.type].CanPlaceBlockAt(world, cIdx, (new Vector3i(i, k, j)), blockToPlace) &&
                    //    blockTop.type == BlockValue.Air.type &&
                    //    block.type == Block.GetBlockValue("fertileFarmland").type)
                    if (blockTop.type == BlockValue.Air.type &&
                        (block.type == Block.GetBlockValue("fertileFarmland").type || block.type == Block.GetBlockValue("fertilizedFarmland").type))
                    {
                        position = (new Vector3i(i, k + 1, j));
                        break;
                    }
                }
            }
        }
        return position;
    }

    public Vector3i GetBlockToHarvest(BlockValue blockToHarvest, WorldBase world, Vector3i blockPos, int cIdx,
        int checkArea)
    {
        Vector3i position = Vector3i.zero;
        for (int i = blockPos.x - checkArea; i <= (blockPos.x + checkArea); i++)
        {
            for (int j = blockPos.z - checkArea; j <= (blockPos.z + checkArea); j++)
            {
                for (int k = blockPos.y - checkArea; k <= (blockPos.y + checkArea); k++)
                {
                    BlockValue block = world.GetBlock(cIdx, new Vector3i(i, k, j));
                    if (block.type == blockToHarvest.type)
                    {
                        position = (new Vector3i(i, k, j));
                        break;
                    }
                }
            }
        }
        return position;
    }

    public bool harvestPlant(List<Vector3i> itemContainerList, ItemStack item, WorldBase world,
        int cIdx, bool debug)
    {
        // look for a position to place the item
        bool result = false;
        if (item == null || world == null ||
            itemContainerList.Count == 0) return false;
        foreach (Vector3i container in itemContainerList)
        {
            TileEntity tile = null;
            tile = world.GetTileEntity(cIdx, container);
            if (tile != null)
            {
                if (tile is TileEntitySecureLootContainer)
                {
                    //if it is possible to stack, will first look for a place to stack it.
                    // other wise searches for a empty slot
                    TileEntitySecureLootContainer invItem = (TileEntitySecureLootContainer)tile;
                    ItemStack[] items = invItem.GetItems();
                    int positionFound = 0;
                    foreach (ItemStack i in items)
                    {
                        // check if its the ingredient we're looking for
                        if (i.count > 0 && i.itemValue.type == item.itemValue.type && item.CanStackWith(i))
                        {
                            // position found
                            i.count += item.count;
                            invItem.SetModified();
                            debugHelper.doDebug(
                                string.Format("FarmerWorkScript: STACKING {0} in container",
                                    ItemClass.GetForId(item.itemValue.type).localizedName), debug);
                            result = true;
                            break;
                        }
                        else if (i.count == 0 || i.IsEmpty())
                        {
                            // position found
                            invItem.UpdateSlot(positionFound, item);
                            invItem.SetModified();
                            debugHelper.doDebug(
                                string.Format("FarmerWorkScript: PLACING {0} in container",
                                    ItemClass.GetForId(item.itemValue.type).localizedName), debug);
                            result = true;
                            break;
                        }
                        positionFound++;
                    }
                }
            }
        }
        return result;
    }

    public ItemStack GetHarvestdItems(BlockValue blockToHarvest, WorldBase world, int cIdx)
    {
        ItemStack itemStack = null;
        List<Block.SItemDropProb> list =
            (List<Block.SItemDropProb>)null;
        if (
            Block.list[blockToHarvest.type].itemsToDrop
                .TryGetValue(EnumDropEvent.Harvest, out list))
        {
            int v1 = (int)blockToHarvest.meta;
            int v2 = list[0].minCount;
            int _count;
            if ((_count = Utils.FastMax(v1, v2)) > 0)
            {
                ItemStack _itemStack =
                    new ItemStack(
                        ItemClass.GetItem(list[0].name),
                        _count);
                itemStack = _itemStack.Clone();
            }
        }
        return itemStack;
    }

    public List<ItemStack> GetHarvestdItemsHit(BlockValue blockToHarvest, WorldBase world, int cIdx)
    {
        List<ItemStack> itemstack = new List<ItemStack>();
        List<Block.SItemDropProb> list =
            (List<Block.SItemDropProb>)null;
        if (
            Block.list[blockToHarvest.type].itemsToDrop
                .TryGetValue(EnumDropEvent.Harvest, out list))
        {
            using (List<Block.SItemDropProb>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Block.SItemDropProb current = enumerator.Current;
                    int _count = UnityEngine.Random.Range(current.minCount, current.maxCount + 1);
                    if (_count > 0)
                    {
                        ItemValue _itemValue = !current.name.Equals("*")
                            ? new ItemValue(ItemClass.GetItem(current.name).type, false)
                            : blockToHarvest.ToItemValue();
                        if (!_itemValue.IsEmpty() && (double)current.prob > (double)UnityEngine.Random.value)
                            itemstack.Add(new ItemStack(_itemValue, _count));
                    }
                }
            }
        }
        return itemstack;
    }

    public Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

}

public class LockReceips
{
    public static List<string> receiptsToLock = new List<string>();

    static LockReceips() 
    {        
        receiptsToLock.Add("steampunkRevolver");
        receiptsToLock.Add("smallCrossbow");
        receiptsToLock.Add("Ump");
        receiptsToLock.Add("HK416");
        receiptsToLock.Add("mg4");
        receiptsToLock.Add("gunRevolver");
        receiptsToLock.Add("gun44Magnum");        
        receiptsToLock.Add("MTAR");
        receiptsToLock.Add("ACW");
        receiptsToLock.Add("gunSawedOffPumpShotgun");
        receiptsToLock.Add("gunPumpShotgun");
        receiptsToLock.Add("gunPistol");
        receiptsToLock.Add("gunMP5");
        receiptsToLock.Add("gunAK47");
        receiptsToLock.Add("gunHuntingRifle");
        receiptsToLock.Add("gunSniperRifle");        
    }
    public static void UnlockAll()
    {
        //Debug.Log("unlocking receipts");
        foreach (string recipt in receiptsToLock)
        {
            CraftingManager.UnlockRecipe(recipt);
        }
    }
    public static void LockAll()
    {
        //Debug.Log("Locking receipts");                
        foreach (string recipt in receiptsToLock)
        {
            CraftingManager.LockRecipe(recipt, CraftingManager.RecipeLockTypes.None);
            CraftingManager.UnlockedRecipeList.Remove(recipt);
        }        
    }
}

public class MorteSpawners
{
    public static void SpawnGroupToPos(WorldBase _world, int _clrIdx, Vector3i _blockPos, string entityGroup, int minDistance, int maxDistance, int zone, bool debug)
    {
        int x;
        int y;
        int z;
        try
        {
            // spreads the rats to about 15 blocks away
            Vector3 spawnPos = Vector3.zero;
            if (GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(_blockPos.ToVector3(), minDistance, maxDistance, false,
                out spawnPos, false))
            {
                //GameManager.Instance.World.FindRandomSpawnPointNearPosition(
                //    _blockPos.ToVector3(), 50, out x, out y, out z,
                //    new Vector3(area, area, area), true, false);
                int entityID = EntityGroups.GetRandomFromGroup(entityGroup);
                //spawnPos = new Vector3((float) x, (float) y, (float) z);
                Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                    spawnPos);
                spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner, _clrIdx, entityGroup);
                GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                debugHelper.doDebug("Spawned from " + entityGroup, debug);
                if (spawnEntity is EntityAlive)
                {
                    EntityAlive ent = (EntityAlive) spawnEntity;
                    ent.SetInvestigatePosition(_blockPos.ToVector3(), 9999);
                    ent.getNavigator().clearPathEntity();
                    Legacy.PathFinderThread.Instance.FindPath(ent, _blockPos.ToVector3(), ent.GetWanderSpeed(),
                        (EAIBase) null);
                    ent.setHomeArea(_blockPos, zone);
                }
            }
            else Debug.Log("ERROR SPAWNING: IMPOSSIBLE TO FIND A SPAWN POSITION");
        }
        catch (Exception ex)
        {
            Debug.Log("ERROR SPAWNING:" + ex.Message);
        }
    }
    public static bool SpreadPlague(WorldBase _world, int _clrIdx, Vector3i _blockPos, int spreadRange, System.Random rnd, out Vector3i plaguePos, bool debug)
    {
        //string blocks = "";
        debugHelper.doDebug("PLAGUE TRYING TO SPREAD", debug);
        for (int i = _blockPos.x - spreadRange; i <= (_blockPos.x + spreadRange); i++)
        {
            for (int j = _blockPos.z - spreadRange; j <= (_blockPos.z + spreadRange); j++)
            {
                for (int k = _blockPos.y - spreadRange; k <= (_blockPos.y + spreadRange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    //blocks = blocks + block.type + ", ";                    
                    if (Block.list[block.type] is BlockMortePlantGrowing ||
                        Block.list[block.type] is BlockMortePlantGrown)
                    {
                        if (block.meta != 1) // already infected
                        {
                            bool spread = true;
                            if (block.meta == 0 && Block.list[block.type] is BlockMortePlantGrown) // wild plant
                            {
                                //debugHelper.doDebug("WILD PLANT", !debug);
                                spread = false;
                            }
                            else if (block.meta == 2)
                            {
                                // plant has pesticide, has a good chance of not getting infected
                                if (rnd.Next(1, 101) < 50) spread = false;
                            }
                            if (spread)
                            {
                                debugHelper.doDebug("PLAGUE SPREADING", debug);
                                plaguePos = new Vector3i(i, k, j);
                                return true;
                            }
                        }
                    }
                }
            }
        }
        plaguePos = Vector3i.zero;
        return false;
    }
    public static int PlagueChance(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int baseChance, int checkRange, bool debug)
    {
        decimal chance = baseChance;        
        bool checkForLight = false;
        if (checkForLight)
        {
            for (int i = _blockPos.x - checkRange; i <= (_blockPos.x + checkRange); i++)
            {
                for (int j = _blockPos.z - checkRange; j <= (_blockPos.z + checkRange); j++)
                {
                    for (int k = _blockPos.y - checkRange; k <= (_blockPos.y + checkRange); k++)
                    {
                        BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                        //blocks = blocks + block.type + ", ";
                        if (block.type == Block.GetBlockValue("MortePlantLight").type)
                        {
                            // if block has power
                            if (block.meta == 1)
                            {
                                chance = chance/10;
                            }
                            else chance++;
                        }
                    }
                }
            }
        }
        if (_blockValue.meta == 2)
        {
            chance = chance/2;
            debugHelper.doDebug("PLANT IS PESTICIZED", debug);
        }
        chance = chance*10;
        return (int) chance;
    }
    public static bool FindTrap(WorldBase _world, int _clrIdx, Vector3i _blockPos, int checkRange, string blockName, bool debug)
    {
        for (int i = _blockPos.x - checkRange; i <= (_blockPos.x + checkRange); i++)
        {
            for (int j = _blockPos.z - checkRange; j <= (_blockPos.z + checkRange); j++)
            {
                for (int k = _blockPos.y - checkRange; k <= (_blockPos.y + checkRange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    string[] traps = blockName.Split(',');
                    bool isTrap = false;
                    foreach (string trap in traps)
                    {
                        if (block.type == Block.GetBlockValue(trap).type)
                        {
                            // check if its powered on
                            if (Block.list[block.type] is BlockElectricDevice)
                            {
                                debugHelper.doDebug("FOUND ELECTRIC DEVICE", debug);
                                if (BlockElectricDevice.IsOn(block.meta))
                                {
                                    debugHelper.doDebug("FOUND A POWERED ON DEVICE, REDUCING CHANCE!!!", debug);
                                    return true;
                                }
                            }
                        }
                    }                    
                }
            }
        }
        debugHelper.doDebug("NO POW", debug);
        return false;
    }
}

public class MorteParticleEffect
{
    public static void addParticle(BlockValue _blockValue, BlockEntityData _ebcd, Vector3 offSet, string particle, bool disableDebug)
    {
        debugHelper.doDebug("LOADING particle " + particle, !disableDebug);
        if (_ebcd == null)
        {
            debugHelper.doDebug("NO ENTITYDATA", !disableDebug);
            return;
        }
        if (_ebcd.transform == null)
        {
            debugHelper.doDebug("NO TRANSFORM", !disableDebug);
            return;
        }
        if (_ebcd.transform.FindInChilds(particle))
        {
            debugHelper.doDebug("Particle " + particle + "already loaded", !disableDebug);
        }
        GameObject original = ResourceWrapper.Load1P(particle) as GameObject;
        if ((UnityEngine.Object)original == (UnityEngine.Object)null)
        {
            Debug.Log("Error loading " + particle);
            return;
        }
        Vector3 localPosition;
        Vector3 localScale;
        Quaternion localRotation;
        GameObject _go;
        //localPosition = original.transform.localPosition + getParticleOffset(_blockValue);
        localPosition = original.transform.localPosition + offSet;
        localScale = original.transform.localScale;
        localRotation = original.transform.localRotation;
        _go = UnityEngine.Object.Instantiate<GameObject>(original);
        _go.transform.parent = _ebcd.transform;
        _go.transform.localPosition = localPosition;
        _go.transform.localRotation = localRotation;
        _go.transform.localScale = localScale;
    }
}