using System;
using UnityEngine;
//using Random = System.Random;

/// <summary>
/// Custom class for Dog Whistle (inherited from ItemClass)
/// </summary>
public class ItemClassDogWhistle : ItemClass
{
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    // Fishing Rod v1.0
    // --------------
    // by Mortelentus - based on Matite ACW Laser code   
    // April 2016
    // 7 Days to Die Alpha 14.6
    // This Dog Whistle code is inherited from the devs ItemClass code. It allows it to act like
    // a normal item apart from the code this class adds:
    // * Adds 5 different orders you can give the dog
    // * attack zeds
    // * attack humans
    // * follow
    // * come to me
    // * stay (wander)
    // * dogs have 20s to comply, after that order is dismissed
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Stores a reference to the local player
    /// </summary>
    private EntityPlayerLocal epLocalPlayer;

    /// <summary>
    /// Stores whether the player is aiming water or not (so we can check if the player is targeting water - if he is not he cannot start fishing, or fishing needs to be interupted)
    /// </summary>
    private bool boolAimingWater = false;

    private bool boolPlaced = false;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    /// <summary>
    /// Stores the date and time a numberpad key was last pressed
    /// </summary>
    private DateTime dteNextAction;

    private bool needDismiss = false;

    private Vector3i vPlace;

   // -----------------------------------------------------------------------------------------------


    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        // Check if the game instance is not null
        if (GameManager.Instance != null)
        {
            // Display the string in the chat text area
            EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
            GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
        }
    }


    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds to prevent spamming

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }


    // -----------------------------------------------------------------------------------------------



    public override void StartHolding(ItemInventoryData _data, Transform _modelTransform)
    {
        base.StartHolding(_data, _modelTransform);
        if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle1")
            _data.itemValue.Meta = 1;
        else if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle2")
            _data.itemValue.Meta = 2;
        else if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle3")
            _data.itemValue.Meta = 3;
        else if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle4")
            _data.itemValue.Meta = 4;
        else if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle5")
            _data.itemValue.Meta = 5;
        else if (_data.holdingEntity.inventory.holdingItem.GetItemName() == "DogWhistle6")
            _data.itemValue.Meta = 6;
        if (_data.itemValue.Meta > 0)
        {
            dteNextAction = DateTime.Now.AddSeconds(20);
            needDismiss = true;
        }
        epLocalPlayer = null;
    }


    /// <summary>
    /// Called when the player stops holding the ACW item
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    /// <param name="_modelTransform">A reference to the models transform.</param>
    public override void StopHolding(ItemInventoryData _data, Transform _modelTransform)
    {
       // Call base code
        base.StopHolding(_data, _modelTransform);

       OnActivateItemGameObjectReference component = _modelTransform.GetComponent<OnActivateItemGameObjectReference>();
        if (component != null && component.IsActivated())
        {
            component.ActivateItem(false);
        }
    }

    /// <summary>
    /// Called when holding the fishing rod
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    public override void OnHoldingUpdate(ItemInventoryData _data)
    {
        // Debug
        //DisplayChatAreaText("OnHoldingUpdate");

        // Base code - no need to run it for the rod.
        //base.OnHoldingUpdate(_data);

        // Don't run this code if remote entity
        if (_data.holdingEntity.isEntityRemote)
        {
            return;
        }
        // check if the player is aiming at water
        // Check reference to local player
        if (!epLocalPlayer)
        {
            // Get and store a reference to the local player
            epLocalPlayer = GameManager.Instance.World.GetLocalPlayer();

            // Debug
            //DisplayChatAreaText("Reference to local player stored.");
        }
        if (dteNextAction > DateTime.Now && needDismiss)
        {
            // revert to whistle
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        if (needDismiss) return;
        if (Input.GetKey(KeyCode.Keypad1))
        {
            // come to me (human whistle)
            epLocalPlayer.PlayOneShot("whistleCall");
            // removes the item, and adds a new one
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle1"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        else if (Input.GetKey(KeyCode.Keypad2))
        {
            // follow
            epLocalPlayer.PlayOneShot("dog_whistle_2small");
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle2"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        else if (Input.GetKey(KeyCode.Keypad3))
        {
            // attack zeds 
            epLocalPlayer.PlayOneShot("dog_whistle_3small");
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle3"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        else if (Input.GetKey(KeyCode.Keypad4))
        {
            // attack humans 
            epLocalPlayer.PlayOneShot("dog_whistle_7small");
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle4"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        else if (Input.GetKey(KeyCode.Keypad5))
        {
            // stay 
            epLocalPlayer.PlayOneShot("dog_whistle_long_2small");
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle5"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
        else if (Input.GetKey(KeyCode.Keypad6))
        {
            // stay 
            epLocalPlayer.PlayOneShot("whistleStay");
            ItemStack newStack = new ItemStack(ItemClass.GetItem("DogWhistle6"), 1);
            _data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
            _data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
            _data.holdingEntity.inventory.ForceHoldingItemUpdate();
        }
    }
}
