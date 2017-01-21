using System;
using UnityEngine;
using SDX.Payload;

/// <summary>
/// Custom class for ACW gun with laser (inherited from ItemClass)
/// </summary>
public class ItemClassAttachments : ItemClass
{
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    // Weapon Attachments v1.0
    // --------------
    // by Mortelentus    
    // May 2016
    // 7 Days to Die Alpha 14.6
    // Includes Matite Laser code
    // This ACW gun code is inherited from the devs ItemClass code. It allows it to act like
    // a normal item apart from the code this class adds:
    // * toggle the laser on or off by pressing the numberpad 5 key
    // * change the flashlight color to green by pressing the numberpad 6 key
    // * change the flashlight color to white by pressing the numberpad 4 key
    // * ADDED BY MORTELENTUS
    // * allows adding/removing different attachments to the gun
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    
    private bool doDebug = false;
    private bool unequip = false;

    private Color laserColor = Color.red;
    private int laserDistance = 5000;
    private float inicialWith = 0.02f;
    private float finalWith = 0.02f;
    private Transform laserGameObject;
    private GameObject collisionLight;
    //private Light collisionLight;
    private LineRenderer lineRenderer;
    private Vector3 lightPos;
    private Transform muzzle;
    private bool openMenu = false;

    private WeaponGUI script;
    ~ItemClassAttachments()
    {
        DoDebug("Destroying");
        ResetLaserObjects();
    }

    /// <summary>
    /// Stores a reference to the local player (so we can check if they are aiming the weapon)
    /// </summary>
    private EntityPlayer epLocalPlayer;

    /// <summary>
    /// Stores whether the player is aiming or not (so we can toggle the laser off when aiming and back on when not aiming ONLY if the laser has not been disabled with the numberpad 5 key)
    /// </summary>
    private bool boolAimingMode = false;

    /// <summary>
    /// Stores whether the laser is enabled or not (so we know whether to disable it when aiming)
    /// </summary>
    private bool boolLaserEnabled = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    /// <summary>
    /// Stores the date and time a numberpad key was last pressed
    /// </summary>
    private DateTime dteNextToggleTime;

    private DateTime dteNextUpdateTime = DateTime.MinValue;

    /// <summary>
    /// Stores a reference to the LineRenderer (laser)
    /// </summary>
    private LineRenderer lrLaser;    

    /// <summary>
    /// Stores a reference to the Light (flashlight)
    /// </summary>
    private Light lightFlashlight;
    
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
            //GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, str, "", false, "", false);
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

    private void DoDebug(string str)
    {
        if (doDebug) Debug.Log("ATTACHMENTS - > " + str);
    }


    // -----------------------------------------------------------------------------------------------

    // i need to use metadata, and replace the gun, like I do with the whistle.
    // i will use bit 5 upwards, since all before that seem to be taken by something
    // META IS USED FOR BULLETS!
    public bool AddPartToItem(ItemInventoryData _data, ItemStack attachStack)
    {
        // set attachment to true, and replace the weapon by a cloned one
        // to make it work on dedi too (hopefully)
        if (_data.itemStack != null && !_data.itemStack.IsEmpty())
        {
            ItemClass forId = ItemClass.GetForId(attachStack.itemValue.type);
            DoDebug("Looking for " + forId.Name);
            if (_data.item.Attachments != null)
            {
                //if (_data.item.Attachments.Length <= 1) return false;
                if (_data.item.Attachments.Length < 1) return false;
                // go through available attachments to check if the respective attachment exists
                for (int i = 0; i <= (_data.item.Attachments.Length - 1); i++)
                //for (int i = 1; i <= (_data.item.Attachments.Length - 1); i++)
                {
                    if (_data.item.Attachments[i] == forId.Name)
                    {
                        bool hasAttacH = false;
                        // check if it already exists in the attachment list (to make it easier, i'll use the same IDs
                        if (_data.itemValue.Attachments.Length >= (i+1))
                        {
                            if (_data.itemValue.Attachments[i] != null)
                            {
                                //if (_data.itemValue.Attachments[i].type != attachStack.itemValue.type)
                                {                                    
                                    try
                                    {
                                        ItemClass forIdAux = ItemClass.GetForId(_data.itemValue.Attachments[i].type);
                                        DoDebug("Current item name: " + forIdAux.Name + " Comparing with position attachment name: " + forId.Name);
                                        if (forIdAux.Name == forId.Name)
                                        {
                                            // put attachment back in bag
                                            ItemStack newAttach = new ItemStack(_data.itemValue.Attachments[i], 1);
                                            if (newAttach != null)
                                            {
                                                _data.holdingEntity.AddUIHarvestingItem(newAttach, false);
                                                _data.holdingEntity.bag.AddItem(newAttach);
                                                // change the attachment to none
                                                _data.itemValue.Attachments[i] = ItemValue.None;
                                                //ItemStack newStack = new ItemStack(_data.itemValue.Attachments[i], 1);
                                                //_data.holdingEntity.inventory.DecItem(_data.itemValue, 1);
                                                //_data.holdingEntity.inventory.SetItem(_data.slotIdx, newStack);
                                                ToogleAttachment(_data, forId.GetItemName(), false, true);
                                                DisplayToolTipText(string.Format("You removed {0} from your gun",
                                                    forId.GetLocalizedItemName()));
                                                hasAttacH = true;
                                                // if the item has magazine size modifier, unload the gun
                                                if (true)
                                                {
                                                    if (!_data.holdingEntity.isEntityRemote)
                                                    {
                                                        #region Action0 modifiers;

                                                        if (forId.Properties.Classes.ContainsKey("AttachAction0"))
                                                        {
                                                            DoDebug("Adjusting properties Action0");
                                                            DynamicProperties dynamicProperties =
                                                                forId.Properties.Classes["AttachAction0"];
                                                            if (dynamicProperties.Contains("Magazine_size"))
                                                            {
                                                                // unload the gun
                                                                if (_data.itemValue.Meta > 0)
                                                                {
                                                                    // it has bullets, put them back in the inventory
                                                                    if (
                                                                        _data.item.Actions[0].Properties.Contains(
                                                                            "Magazine_items"))
                                                                    {
                                                                        // create a stack
                                                                        ItemValue bullet = global::ItemClass.GetItem((_data.item.Actions[0] as ItemActionRanged).MagazineItemNames[(int)_data.itemValue.SelectedAmmoTypeIndex]);
                                                                        ItemStack bullets = new ItemStack(bullet,
                                                                            _data.itemValue.Meta);
                                                                        _data.itemValue.Meta = 0;
                                                                        _data.holdingEntity.AddUIHarvestingItem(
                                                                            bullets, false);
                                                                        _data.holdingEntity.bag.AddItem(bullets);
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        #endregion;
                                                    }
                                                }
                                                return true;
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                            }
                        }
                        if (!hasAttacH)
                        {
                            int nummAttachs = _data.holdingEntity.bag.GetItemCount(attachStack.itemValue);
                            if (nummAttachs >= 1)
                            {
                                // needs to check if there's any of the incompatible attachments on the gun
                                if (CheckIncompatible(_data, forId)) return false;
                                ItemStack[] items = _data.holdingEntity.bag.GetSlots();
                                foreach (ItemStack stack in items)
                                {
                                    if (stack.itemValue.type == attachStack.itemValue.type)
                                    {
                                        attachStack = stack.Clone();
                                        stack.Clear();
                                        break;
                                    }
                                }                                
                                //_data.holdingEntity.bag.DecItem(attachStack.itemValue, 1);
                                // needs to add the attachment to the list
                                if (_data.itemValue.Attachments.Length < (i + 1))
                                {
                                    ItemValue[] newArray = new ItemValue[i + 1];
                                    if (_data.itemValue.Attachments.Length > 0)
                                    {
                                        _data.itemValue.Attachments.CopyTo(newArray, 0);
                                    }
                                    for (int j = 0; j <= (newArray.Length - 1); j++)
                                    {
                                        if (newArray[j] == null)
                                            newArray[j] = ItemValue.None;
                                    }
                                    newArray[i] = attachStack.itemValue;
                                    _data.itemValue.Attachments = newArray;
                                    //newArray.CopyTo(_data.itemValue.Attachments, 0);
                                }
                                else _data.itemValue.Attachments[i] = attachStack.itemValue;                               
                                ToogleAttachment(_data, forId.GetItemName(), true, true);
                                DisplayToolTipText(string.Format("You attached a {0} to your gun",
                                    forId.GetLocalizedItemName()));
                                // if the item has magazine size modifier, unload the gun
                                if (true)
                                {
                                    if (!_data.holdingEntity.isEntityRemote)
                                    {
                                        #region Action0 modifiers;

                                        if (forId.Properties.Classes.ContainsKey("AttachAction0"))
                                        {
                                            DoDebug("Adjusting properties Action0");
                                            DynamicProperties dynamicProperties =
                                                forId.Properties.Classes["AttachAction0"];
                                            if (dynamicProperties.Contains("Magazine_size"))
                                            {
                                                // unload the gun
                                                if (_data.itemValue.Meta > 0)
                                                {
                                                    // it has bullets, put them back in the inventory
                                                    if (_data.item.Actions[0].Properties.Contains("Magazine_items"))
                                                    {
                                                        // create a stack
                                                        ItemValue bullet = global::ItemClass.GetItem((_data.item.Actions[0] as ItemActionRanged).MagazineItemNames[(int)_data.itemValue.SelectedAmmoTypeIndex]);
                                                        ItemStack bullets = new ItemStack(bullet, _data.itemValue.Meta);
                                                        _data.itemValue.Meta = 0;
                                                        _data.holdingEntity.AddUIHarvestingItem(bullets, false);
                                                        _data.holdingEntity.bag.AddItem(bullets);
                                                    }
                                                }
                                            }
                                        }

                                        #endregion;
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                DisplayToolTipText(string.Format("You don't have any {0} on you",
                                    forId.GetLocalizedItemName()));
                                return false;
                            }
                        }                        
                    }
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Check for incompatible attachments on the gun
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="attachment"></param>
    /// <returns></returns>
    private bool CheckIncompatible(ItemInventoryData _data, ItemClass attachment)
    {
        bool result = false;
        Transform[] componentsInChildren = _data.model.GetComponentsInChildren<Transform>(false);
        if (componentsInChildren != null)
        {
            string[] incompatibleA = null;
            if (attachment.Properties.Values.ContainsKey("Incompatible"))
            {
                string str = attachment.Properties.Values["Incompatible"];
                if (str.Contains(","))
                {
                    incompatibleA = str.Split(',');
                }
                else
                {
                    incompatibleA = new string[1];
                    incompatibleA[0] = str.Trim();
                }
                foreach (string incomp in incompatibleA)
                {
                    foreach (Transform tra in componentsInChildren)
                    {
                        if (tra.name == incomp)
                        {
                            ItemValue item = GetItem(tra.name);
                            DisplayToolTipText(string.Format("You need to remove {0} first",
                                ItemClass.GetForId(item.type).GetLocalizedItemName()));
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool DecayAttachments(ItemInventoryData _data)
    {
        bool isBroken = false;
        // foreach currently attached item, causes decay to it
        if (_data.itemValue.Attachments.Length > 0)
        {
            //DisplayChatAreaText("There are available attachments");
            foreach (ItemValue item in _data.itemValue.Attachments)
            {
                try
                {
                    if (item != ItemValue.None && item != null)
                    {
                        ItemClass icA = ItemClass.GetForId(item.type);
                        if (icA != null)
                        {
                            bool hasDecay = false;
                            if (icA.Properties.Classes.ContainsKey("Attributes"))
                            {
                                DynamicProperties dynamicProperties = icA.Properties.Classes["Attributes"];
                                if (dynamicProperties.Contains("DegradationMax")) hasDecay = true;
                            }
                            if (hasDecay)
                            {
                                item.UseTimes += AttributeBase.GetVal<AttributeDegradationRate>(item, 1);
                                if (item.MaxUseTimes > 0 &&
                                    item.UseTimes >= item.MaxUseTimes ||
                                    item.UseTimes == 0 && item.MaxUseTimes == 0)
                                {
                                    // attachment is broken
                                    // removes attachment since it's broken
                                    isBroken = true;
                                    //DisplayToolTipText(string.Format("Your {0} is broken",
                                    //    icA.GetLocalizedItemName()));
                                    //ToogleAttachment(_data, ItemClass.list[item.type].GetItemName(), false, true);
                                    //_data.holdingEntity.SetHoldingItemTransform(_data.model);
                                    //_data.holdingEntity.inventory.ForceHoldingItemUpdate();
                                    //if (unequip)
                                    //    _data.holdingEntity.inventory.SetHoldingItemIdx(_data.slotIdx);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //DisplayChatAreaText("Error: " + ex.Message);
                }
            }
        }
        return isBroken;
    }

    public override void StartHolding(ItemInventoryData _data, Transform _modelTransform)
	{
        // Debug        
        DoDebug("Holding : " + _data.item.GetItemName() + " with META = " + _data.itemValue.Meta);
        //ItemClass.list[_data.itemValue.type].Properties.Contains()
        // get available attachments list and apply modifiers      
        DoDebug(" **** LOADING DEFAULTS ****");
        if ((_data.item.Actions[0] is ItemActionRangedAt))
            (_data.item.Actions[0] as ItemActionRangedAt).ReadFrom(_data.item.Actions[0].Properties);
        if (_data.item.Actions.Length > 1)
        {
            DoDebug(" **** RELOADING ACTION 1 ****");
            if (_data.item.Actions[1] is ItemActionZoom)
                (_data.item.Actions[1] as ItemActionZoom).ZoomOverlay = null;
            _data.item.Actions[1].ReadFrom(_data.item.Actions[1].Properties);
        }
        DoDebug(" **** CHECKING ATTACHMENTS ****");
        DisplayAttachments(_data);
        ResetLaserObjects();
        //ThreadManager.StartCoroutine(WeaponGUI());        
    }    
    /// <summary>
    /// Checks what attachments are present
    /// If an attachment is present it applies the modifier.
    /// If not, it does nothing
    /// </summary>
    /// <param name="_data"></param>
    private void DisplayAttachments(ItemInventoryData _data)
    {
        //DisplayChatAreaText("**************************************");
        //if (_data.itemValue.Attachments.Length > 0)
        //{
        //    //DisplayChatAreaText("There are available attachments");
        //    foreach (ItemValue item in _data.itemValue.Attachments)
        //    {
        //        try
        //        {
        //            if (item != ItemValue.None && item != null)
        //            {
        //                //DisplayChatAreaText(ItemClass.list[item.type].GetItemName());
        //                // does NOT apply modifier, as it should already be there
        //                ToogleAttachment(_data, ItemClass.list[item.type].GetItemName(), true,
        //                    (_data.item.Actions[0] as ItemActionRangedAt).needReload);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            //DisplayChatAreaText("Error: " + ex.Message);
        //        }
        //    }
        //}

        ItemClass item = GetForId(_data.itemValue.type);
        if (item.Attachments != null)
            if (item.Attachments.Length > 1)
            {
                //for (int i = 1; i <= (item.Attachments.Length - 1); i++)
                for (int i = 0; i <= (item.Attachments.Length - 1); i++)
                {
                    bool state = false;
                    if (_data.itemValue.Attachments.Length >= (i + 1))
                    {
                        if (!_data.itemValue.Attachments[i].IsEmpty())
                        {
                            if (_data.itemValue.Attachments[i] != ItemValue.None)
                            {
                                state = true;
                            }
                        }
                    }
                    DoDebug("Try setting " + item.Attachments[i] + " to " + state.ToString());
                    if ((_data.item.Actions[0] is ItemActionRangedAt))
                    {
                        ToogleAttachment(_data, item.Attachments[i], state,
                            (_data.item.Actions[0] as ItemActionRangedAt).needReload);
                    }
                    else ToogleAttachment(_data, item.Attachments[i], state, false);
                }
            }

        if ((_data.item.Actions[0] is ItemActionRangedAt))
            (_data.item.Actions[0] as ItemActionRangedAt).needReload = false;
        //else DisplayChatAreaText("NO ATTACHMENTS");        
        //if (_data.item.Attachments.Length > 1)
        //{
        //    DisplayChatAreaText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -> Property attachments");
        //int bit = 5;  
        ////i bypass position 0, since its used for flashlight          
        //for (int i = 1; i <= (_data.item.Attachments.Length - 1); i++)
        //{
        //    bool isPresent = (((int) _data.itemValue.Meta & 1 << bit) != 0);
        //    DisplayChatAreaText(string.Format("Attachment {0} present = {1}", _data.item.Attachments[i],
        //        isPresent.ToString()));
        //    bit++;
        //}            
        //}
    }

    public override ItemWorldData CreateWorldData(IGameManager _gm, EntityItem _entityItem, ItemValue _itemValue, int _belongsEntityId)
    {
        DoDebug("***CREATE WORLD DATA ****");
        return base.CreateWorldData(_gm, _entityItem, _itemValue, _belongsEntityId);        
    }

    /// <summary>
    /// Called when the player stops holding the ACW item
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    /// <param name="_modelTransform">A reference to the models transform.</param>
	public override void StopHolding(ItemInventoryData _data, Transform _modelTransform)
	{
        // Debug
        //DisplayChatAreaText("StopHolding");
        //ThreadManager.StopCoroutine(WeaponGUI());
        if (script != null)
        {
            script.KillScript();
        }
        // destroy the light object
        // Call base code
        if (epLocalPlayer != null)
            DoDebug("STOP HOLDING FOR ENTITY: " + epLocalPlayer.entityId);
        else DoDebug("STOP HOLDING");
        ResetLaserObjects();
        base.StopHolding(_data, _modelTransform);
        // Check if the model transform is null
        if (_modelTransform == null)
		{
			return;
		}
        // TODO - Reloads ALL original settings WITHOUT any modifier. Those only apply on start holding again.

        OnActivateItemGameObjectReference component = _modelTransform.GetComponent<OnActivateItemGameObjectReference>();
		if (component != null && component.IsActivated())
		{
			component.ActivateItem(false);
		}
        
        // Reset flags
        boolAimingMode = false;
        boolLaserEnabled = true;        
    }
    /// <summary>
    /// Try to show/hide the attachment
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="attachment"></param>
    /// <param name="active"></param>
    /// <returns></returns>
    private bool ToogleAttachment(ItemInventoryData _data, string attachment, bool active, bool applyModifier)
    {
        try
        {
            if (attachment.ToLower().Contains("flashlight"))
            {
                Transform transform2 = _data.model.Find("Attachments/flashlight");
                if (transform2 != null)
                {
                    transform2.gameObject.SetActive(active);
                }
                return true;
            }
            // Check if the reference to the LineRenderer component is null
            Transform[] componentsInChildren = _data.model.GetComponentsInChildren<Transform>(true);
            if (componentsInChildren != null)
            {
                foreach (Transform tra in componentsInChildren)
                {
                    if (tra.name == attachment)
                    {
                        DoDebug("FOUND " + attachment + " setting it to " + active.ToString());
                        tra.gameObject.SetActive(active);
                        // hide or show the standard version
                        if (attachment.Contains("_"))
                        {
                            string[] aux = attachment.Split('_');
                            DoDebug("STD ITEM = " + aux[0]);
                            //if there's any attachment present that will HIDE it, we DO NOT make it visible anywhere
                            bool doAction = true;
                            if (!active)
                            {
                                //for (int i = 1; i <= (_data.itemValue.Attachments.Length - 1); i++)
                                for (int i = 0; i <= (_data.itemValue.Attachments.Length - 1); i++)
                                {
                                    if (!_data.itemValue.Attachments[i].IsEmpty())
                                    {
                                        if (
                                            ItemClass.GetForId(_data.itemValue.Attachments[i].type)
                                                .GetItemName()
                                                .StartsWith(aux[0]) &&
                                            ItemClass.GetForId(_data.itemValue.Attachments[i].type).GetItemName() !=
                                            attachment)
                                        {
                                            // there at least 1 item that will hide the standard version present, that it's not itself. so do NOT show it here
                                            doAction = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (doAction)
                            {
                                foreach (Transform std in componentsInChildren)
                                {
                                    if (std.name == aux[0])
                                    {
                                        DoDebug("FOUND STD OBJECT AND SETTING IT TO" + !active);
                                        std.gameObject.SetActive(!active);
                                        break;
                                    }
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            DoDebug("ERROR ToogleAttachment - " + ex.ToString());
            return false;
        }
        finally
        {
            // resets laser object for force recreating them IF needed
            ResetLaserObjects();
        }
    }

    /// <summary>
    /// Checks if we have stored a reference to the LineRenderer component (laser)... if not it gets one and returns true if all is ok
    /// </summary>
    /// <param name="_data">Item inventory data reference</param>
    /// <returns>Returns true if the reference to the LineRenderer component (laser) is valid</returns>
    private bool CheckLineRendererReference_Dot(ItemInventoryData _data)
    {       
        // Check if the reference to the LineRenderer component is null
        if (!lrLaser)
        {
            // Debug
            //DisplayChatAreaText("Warning: ACWLaser LineRenderer reference is null... getting reference now.");
            DoDebug("GET LINE REFERENCE");
            // Get and store a reference to the LineRenderer component
            lrLaser = _data.model.GetComponentInChildren<LineRenderer>(false); // if it's not attached, it wont work.

            // Check if we were not able to get a reference to the LineRenderer component
            if (!lrLaser)
            {
                DoDebug("LINE NOT FOUND");
                // Debug
                //DisplayChatAreaText("Error: ACWLaser LineRenderer component not found!");

                // Return false as a reference to the LineRenderer component was not found
                // (when the gun is selected in the toolbelt it takes a second or two before it is ready as the character pulls it out)
               ResetLaserObjects();
                return false;
            }
            else
            {
                DoDebug("LINE FOUND");
                if (!muzzle)
                    muzzle = Extensions.FindInChilds(_data.model, "Muzzle");
                if (!laserGameObject)
                {
                    DoDebug("LASERATTACHMENT FOUND");
                    laserGameObject = Extensions.FindInChilds(_data.model, "LaserAttachment");
                }
                // Return true as we now have a reference to the LineRenderer component               
                lrLaser.SetPosition(0, lrLaser.transform.position);
                lrLaser.SetPosition(1, lrLaser.transform.position);
                if (!muzzle) muzzle = laserGameObject.transform;
                lineRenderer = laserGameObject.gameObject.GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    DoDebug("CUSTOM LINE RENDERER CREATED");
                    lineRenderer = laserGameObject.gameObject.AddComponent<LineRenderer>();
                    lineRenderer.material = lrLaser.material;
                    lineRenderer.SetColors(laserColor, laserColor);
                    lineRenderer.SetWidth(inicialWith, finalWith);
                    lineRenderer.SetVertexCount(2);
                }
                if (collisionLight == null)
                {
                    DoDebug("CUSTOM LIGHT CREATED");
                    collisionLight = new GameObject();
                    collisionLight.AddComponent<Light>();
                    collisionLight.GetComponent<Light>().intensity = 8;
                    collisionLight.GetComponent<Light>().bounceIntensity = 8;
                    collisionLight.GetComponent<Light>().range = finalWith * 8;
                    collisionLight.GetComponent<Light>().color = laserColor;
                    collisionLight.SetActive(false);
                    lightPos = new Vector3(0, 0, finalWith * 2);
                }
                return true;
            }
        }
        else
        {
            // Return true as we already have a reference to the LineRenderer component
            //DoDebug("REFERENCES ALREADY CREATED");
            return true;           
        }
    }
    private bool CheckLineRendererReference(ItemInventoryData _data)
    {
        // Check if the reference to the LineRenderer component is null
        if (!lrLaser)
        {
            // Debug
            //DisplayChatAreaText("Warning: ACWLaser LineRenderer reference is null... getting reference now.");

            // Get and store a reference to the LineRenderer component
            lrLaser = _data.model.GetComponentInChildren<LineRenderer>(false); // if it's not attached, it wont work.

            // Check if we were not able to get a reference to the LineRenderer component
            if (!lrLaser)
            {
                // Debug
                //DisplayChatAreaText("Error: ACWLaser LineRenderer component not found!");

                // Return false as a reference to the LineRenderer component was not found
                // (when the gun is selected in the toolbelt it takes a second or two before it is ready as the character pulls it out)
                return false;
            }
            else
            {
                // Return true as we now have a reference to the LineRenderer component
                return true;
            }
        }
        else
        {
            // Return true as we already have a reference to the LineRenderer component
            return true;
        }
    }

    /// <summary>
    /// Checks if we have stored a reference to the Light component (flashlight)... if not it gets one and returns true if all is ok
    /// </summary>
    /// <param name="_data">Item inventory data reference</param>
    /// <returns>Returns true if the reference to the Light component (flashlight) is valid</returns>
    private bool CheckLightReference(ItemInventoryData _data)
    {
        // Check if the reference to the Light component is null
        if (!lightFlashlight)
        {
            // Debug
            //DisplayChatAreaText("Warning: ACWLaser Light reference is null... getting reference now.");

            // Get and store a reference to the Light component
            lightFlashlight = _data.model.GetComponentInChildren<Light>();

            // Check if we were not able to get a reference to the Light component
            if (!lightFlashlight)
            {
                // Display an error message in the chat text area
                DisplayChatAreaText("Error: ACWLaser Light component not found!");
                return false;
            }
            else
            {
                // Return true as we now have a reference to the Light component
                return true;
            }
        }
        else
        {
            // Return true as we already have a reference to the Light component
            return true;
        }
    }

    public override void ExecuteAction(int _actionIdx, ItemInventoryData _data, bool _bReleased)
    {
        if (_actionIdx == 0)
        {
            if (DecayAttachments(_data))
            {
                // warn that it has broken attachments
                if (this.Properties.Values.ContainsKey(ItemClass.PropSoundJammed))
                    GameManager.Instance.ShowTooltipWithAlert("ttItemNeedsRepair",
                        this.Properties.Values[ItemClass.PropSoundJammed]);
                else
                    GameManager.Instance.ShowTooltip("ttItemNeedsRepair");
                return;
            }
        }
        base.ExecuteAction(_actionIdx, _data, _bReleased);        
    }

    System.Collections.IEnumerator UpdateLaser()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            //DrawLaser();            
            //yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// Called when holding the ACW item
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    public override void OnHoldingUpdate(ItemInventoryData _data)
    {
        // Debug

        // Base code
        base.OnHoldingUpdate(_data);
        // I SHOULD RUN LASER THINGS HERE TO SYNC ON ALL CLIENTS       

        //if (epLocalPlayer != null)
        //    DoDebug("HOLDINGUPDATE FOR ENTITY: " + epLocalPlayer.entityId);
        //else DoDebug("HOLDINGUPDATE");

        if (!GameManager.IsDedicatedServer)
        {
            if (!epLocalPlayer)
            {
                // Get and store a reference to the player holding the gun
                epLocalPlayer = (_data.holdingEntity as EntityPlayer);
            }
            if (boolLaserEnabled == true)
            {
                if (CheckLineRendererReference(_data))
                {
                    //Enable the laser
                    lrLaser.enabled = true;
                    //DrawLaser(_data);
                }
            }
            else if (collisionLight)
            {
                //collisionLight.SetActive(false);
                _data.model.gameObject.GetComponent<Light>().enabled = false;
            }
        }

        // Don't run this code if remote entity
        if (_data.holdingEntity.isEntityRemote)
        {
            return;
        }

        // Check if the laser is enabled
        if (boolLaserEnabled == true)
        {
            // Check reference to local player                                             

            // Check if the player is aiming the gun (we turn off the laser when aiming so it is not in the way)            
            if (epLocalPlayer.AimingGun)
            {
                // Check LineRenderer component reference (gets a reference if none currently exists)
                if (CheckLineRendererReference(_data))
                {
                    // Check aiming mode
                    if (boolAimingMode == false)
                    {
                        // Disable the laser
                        lrLaser.enabled = false;

                        // Debug
                        //DisplayChatAreaText("Aiming (laser was switched off)");

                        // Set flag
                        boolAimingMode = true;
                    }
                }

                // Check LineRenderer component reference (gets a reference if none currently exists)
                //if (CheckLineRendererReference(_data))
                //{
                //    // Check aiming mode
                //    if (boolAimingMode == false)
                //    {
                //        // Disable the laser
                //        lrLaser.enabled = false;
                //        //if (collisionLight) collisionLight.SetActive(false);
                //        // Set flag
                //        boolAimingMode = true;
                //    }
                //}                
            }
            else
            {
                // Check LineRenderer component reference (gets a reference if none currently exists)
                if (CheckLineRendererReference(_data))
                {
                    // Check aiming mode
                    if (boolAimingMode == true)
                    {
                        // Disable the laser
                        lrLaser.enabled = true;
                        // Set flag
                        boolAimingMode = false;
                    }
                }
            }
        }
        //else if (collisionLight) collisionLight.SetActive(false);
        //DrawLaser();
        // Check for numberpad 8 key
        if (openMenu)
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                openMenu = false;
                showMenu(_data);
            }            
        }
        else if (Input.GetKey(KeyCode.Keypad8))
        {
            #region Laser IF attached;

            // Check if we can process the key (i.e. half second has passed since last processing)
            // We can only call the code below once every half second because without it
            // it can trigger more than once by just pressing the key
            if (DateTime.Now > dteNextToggleTime)
            {
                // Debug
                //DisplayChatAreaText("Numberpad key 5 pressed while holding the item!");

                // Check LineRenderer component reference (gets a reference if none currently exists)
                if (!CheckLineRendererReference(_data))
                {
                    // Exit here (message already displayed)
                    return;
                }

                // Check if the player is aiming the gun (don't allow the laser toggle when aiming)            
                if (epLocalPlayer.AimingGun)
                {
                    // Display tool tip text message
                    DisplayToolTipText("Exit From Aim Mode Before Toggling Laser.");
                }
                else
                {
                    // Check if the laser is currently enabled
                    if (lrLaser.enabled == true)
                    {
                        // Display tool tip text message
                        DisplayToolTipText("Laser disabled.");

                        // Store status
                        boolLaserEnabled = false;

                        // Disable the laser
                        lrLaser.enabled = false;
                    }
                    else
                    {
                        // Display tool tip text message
                        DisplayToolTipText("Laser enabled.");

                        // Store status
                        boolLaserEnabled = true;

                        // Enable the laser
                        lrLaser.enabled = true;
                    }
                }

                // Set time we can next toggle the laser (once every half second)
                dteNextToggleTime = DateTime.Now.AddSeconds(0.5);
            }

            #endregion;
        }
        // Check for numberpad 7 key
        else if (Input.GetKey(KeyCode.Keypad7))
        {
            #region Flashlight white;

            // Check if we can process the key (i.e. half second has passed since last processing)
            // We can only call the code below once every half second because without it
            // it can trigger more than once by just pressing the key
            if (DateTime.Now > dteNextToggleTime)
            {
                // Debug
                //DisplayChatAreaText("Numberpad key 4 pressed while holding the item!");

                // Check if the flashlight is not activated (switched off)
                if (!this.IsActivated(_data.itemValue))
                {
                    // Debug
                    //DisplayChatAreaText("Switch on your flashlight then try again");

                    // Display tool tip text message
                    DisplayToolTipText("Switch On Flashlight Then Try Again.");
                }
                else
                {
                    // Debug
                    //DisplayChatAreaText("Flashlight is activated");

                    // Check Light component reference (gets a reference if none currently exists)
                    if (!CheckLightReference(_data))
                    {
                        // Exit here (message already displayed)
                        return;
                    }

                    // Set flashlight color
                    lightFlashlight.color = Color.white;

                    // Display tool tip text message
                    DisplayToolTipText("Flashlight Color White.");
                }

                // Set time we can next toggle the laser (once every half second)
                dteNextToggleTime = DateTime.Now.AddSeconds(0.5);
            }

            #endregion;
        }
        // Check for numberpad 9 key
        else if (Input.GetKey(KeyCode.Keypad9))
        {
            #region Flashlight green;

            // Check if we can process the key (i.e. half second has passed since last processing)
            // We can only call the code below once every half second because without it
            // it can trigger more than once by just pressing the key
            if (DateTime.Now > dteNextToggleTime)
            {
                // Debug
                //DisplayChatAreaText("Numberpad key 6 pressed while holding the item!");

                // Check if the flashlight is not activated (switched off)
                if (!this.IsActivated(_data.itemValue))
                {
                    // Debug
                    //DisplayChatAreaText("Switch on your flashlight then try again");

                    // Display tool tip text message
                    DisplayToolTipText("Switch On Flashlight Then Try Again.");
                }
                else
                {
                    // Debug
                    //DisplayChatAreaText("Flashlight is activated");

                    // Check Light component reference (gets a reference if none currently exists)
                    if (!CheckLightReference(_data))
                    {
                        // Exit here (message already displayed)
                        return;
                    }

                    // Set flashlight color
                    lightFlashlight.color = Color.green;

                    // Display tool tip text message
                    DisplayToolTipText("Flashlight Color Green.");
                }

                // Set time we can next toggle the laser (once every half second)
                dteNextToggleTime = DateTime.Now.AddSeconds(0.5);
            }

            #endregion;
        }
        else if (Input.GetKey(KeyCode.Keypad1))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                //DisplayChatAreaText("Press 1");
                ToogleAttachment(_data, 1, false);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad2))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                //DisplayChatAreaText("Press 2");
                ToogleAttachment(_data, 2, false);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad3))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                ToogleAttachment(_data, 3, false);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad4))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                ToogleAttachment(_data, 4, false);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad5))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                ToogleAttachment(_data, 5, false);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad6))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                ToogleAttachment(_data, 6, false);
            }
        }
        else if (Input.GetKey(KeyCode.Insert))
        {
            if (DateTime.Now > dteNextToggleTime)
            {
                showMenu(_data);
            }
        }
        //else if (Input.GetKey(KeyCode.K))
        //{
        //    unequip = !unequip;
        //    DoDebug("Set unequip to " + unequip.ToString());
        //}
    }

    private void showMenu(ItemInventoryData _data)
    {
        script = _data.model.gameObject.GetComponent<WeaponGUI>();
        if (script != null)
        {
            script.KillScript();
        }
        if (script == null)
        {
            script = _data.model.gameObject.AddComponent<WeaponGUI>();
        }
        if (script != null)
        {
            script.Initialize(epLocalPlayer, this);
            script.ShowUI();
            dteNextToggleTime = DateTime.Now.AddSeconds(0.5);
        }
    }

    private void DrawLaser(ItemInventoryData _data)
    {
        try
        {

            if (lineRenderer)
            {
                lineRenderer.SetPosition(0, lrLaser.transform.position);
                lineRenderer.SetPosition(1, lrLaser.transform.position);
            }
            if (lrLaser)
            {
                if (boolLaserEnabled)
                {
                    if (!muzzle) boolLaserEnabled = false;
                    else
                    {

                        // redraw the sight going forward
                        WorldRayHitInfo hit = null;
                        Vector3 direction = epLocalPlayer.GetLookVector(muzzle.forward);
                        Vector3 vector3 = muzzle.position;
                        if (!epLocalPlayer.isEntityRemote)
                        {
                            vector3 = epLocalPlayer.AimingGun
                                ? (epLocalPlayer as EntityPlayerLocal).GetCrosshairPosition3D(0.0f, 0.0F, vector3)
                                : (epLocalPlayer as EntityPlayerLocal).GetShotStartPositionRandom(0.01f, 0.0F, vector3);
                        }
                        Vector3 finalLaserPoint = vector3 + direction*laserDistance;
                        Ray ray = new Ray(vector3, direction);
                        if (Voxel.Raycast(GameManager.Instance.World, ray, laserDistance, -1486853, 8, 0.0f))
                        {
                            hit = Voxel.voxelRayHitInfo.Clone();
                            if (hit != null)
                            {
                                float distance = Vector3.Distance(vector3, hit.fmcHit.pos) - 0.05f;
                                //laserGameObject.GetComponent<LineRenderer>().SetPosition(0, lrLaser.transform.position);
                                //laserGameObject.GetComponent<LineRenderer>().SetPosition(1, hit.fmcHit.pos);
                                if (collisionLight)
                                {
                                    collisionLight.transform.position = vector3 + direction*distance;
                                    collisionLight.SetActive(true);
                                    //Debug.Log("HIT POSITION");
                                }
                            }
                        }
                        else
                        {
                            //laserGameObject.GetComponent<LineRenderer>().SetPosition(0, lrLaser.transform.position);
                            //laserGameObject.GetComponent<LineRenderer>().SetPosition(1, finalLaserPoint);
                            if (collisionLight)
                            {
                                collisionLight.SetActive(false);
                                collisionLight.transform.position = finalLaserPoint;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DoDebug("DawLaser Erro - " + ex.Message);
            ResetLaserObjects();
        }
    }

    private void ResetLaserObjects()
    {
        if (lineRenderer != null)
        {
            //UnityEngine.destroy
            lineRenderer.enabled = false;
            MonoBehaviour.Destroy(lineRenderer);
            lineRenderer = null;
        }
        if (collisionLight != null)
        {
            collisionLight.SetActive(false);
            MonoBehaviour.Destroy(collisionLight);
            collisionLight = null;
        }
        if (laserGameObject != null) laserGameObject = null;
        if (muzzle != null) muzzle = null;
        lrLaser = null;
    }

    public void ToogleAttachment(ItemInventoryData _data, int idA, bool reopenMenu)
    {
        DoDebug("TOOGLE ATTACHMENT");
        if (_data.item.Attachments.Length >= (idA + 1))
        {
            // add silencer testing
            ItemValue item = GetItem(_data.item.Attachments[idA]);
            ItemStack attach = new ItemStack(item, idA);            
            if (AddPartToItem(_data, attach))
            {
                // unequip the item
                _data.holdingEntity.SetHoldingItemTransform(_data.model);
                _data.holdingEntity.inventory.ForceHoldingItemUpdate();
                if (unequip)
                    _data.holdingEntity.inventory.SetHoldingItemIdx(_data.slotIdx);
                //StartHolding(_data, _data.model);
                _data.holdingEntity.inventory.CallOnToolbeltChangedInternal();
            }            
            // Set time we can next toggle the laser (once every half second)
            dteNextToggleTime = DateTime.Now.AddSeconds(0.5);
        }
        if (reopenMenu)
        {
            openMenu = true;
        }
    }

    public override Transform CloneModel(World _world, ItemValue _itemValue, Vector3 _position, Transform _parent,
        bool _bUseDropModel, bool _bUseHandModel)
    {
        Transform transform = base.CloneModel(_world, _itemValue, _position, _parent, _bUseDropModel, _bUseHandModel);
        // apply aditional attachment transforms (visible/invisible stuff)
        if (transform!=(Transform) null)
        {
            DoDebug("Do custom cloneModel");
            try
            {
                Transform transform1 = transform;
                ItemClass item = GetForId(_itemValue.type);
                if (item.Attachments == null) return transform;
                if (item.Attachments.Length > 1)
                {
                    //for (int i = 1; i <= (item.Attachments.Length - 1); i++)
                    for (int i = 0; i <= (item.Attachments.Length - 1); i++)
                    {
                        bool state = false;
                        if (_itemValue.Attachments.Length >= (i + 1))
                        {
                            if (!_itemValue.Attachments[i].IsEmpty())
                            {
                                if (_itemValue.Attachments[i] != ItemValue.None)
                                {
                                    state = true;
                                }
                            }
                        }
                        DoDebug("Try setting " + item.Attachments[i] + " to " + state.ToString());
                        // find the transform
                        Transform transform2 = transform1.Find(item.Attachments[i]);
                        if ((UnityEngine.Object) transform2 != (UnityEngine.Object) null)
                            transform2.gameObject.SetActive(state);
                    }
                }               
                return transform1;
            }
            catch (Exception ex)
            {
                DoDebug("Instantiate of '" + this.MeshFile + "' led to error: " + ex.Message);
            }
        }

        return transform;
    }
}

public class WeaponGUI : MonoBehaviour
{
    private EntityPlayer epLocalPlayer;
    private ItemClassAttachments gun;
    private bool showUI = false;
    bool lastShowUI = false;
    private Rect guiAreaRect = new Rect(0, 0, 0, 0);
    private Rect infoAreaRect = new Rect(0, 0, 0, 0);
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color titleColor = Color.red;
    Color buttonColor = Color.green;
    public Vector2 scrollPosition = Vector2.zero;
    private int numAllowedAttachments = 0;
    private ItemClass item = null;
    private bool closeUI = false;
    private bool openUI = false;
    private string keytosend = "";

    void Start()
    {
        showUI = false;        
    }

    public void Initialize(EntityPlayer _epLocalPlayer, ItemClassAttachments _gun)
    {
        gun = _gun;
        epLocalPlayer = _epLocalPlayer;        
        // get the list of allowed attachments
        item = ItemClass.GetForId(epLocalPlayer.inventory.holdingItemData.itemValue.type);
        if (item.Attachments != null)
            //if (item.Attachments.Length > 1)
            if (item.Attachments.Length > 0)
            {
                numAllowedAttachments = item.Attachments.Length;
                guiAreaRect = new Rect(10, 10, 250, 50 * numAllowedAttachments + 1);
                infoAreaRect = new Rect(300, 10, 250, 50 * numAllowedAttachments + 1);
            }
    }
    /// <summary>
    /// UI on/off
    /// </summary>
    public void ShowUI()
    {
        if (showUI)
        {
            showUI = false;
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
            (epLocalPlayer as EntityPlayerLocal).SetControllable(true);
            GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
            this.KillScript();
        }
        else
        {
            closeUI = false;
            openUI = true;
        }
    }

    void Update()
    {
        
    }

    public void OnGUI()
    {
        if (openUI)
        {
            showUI = true;
            openUI = false;
            closeUI = false;
        }
        else if (closeUI)
        {
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
            (epLocalPlayer as EntityPlayerLocal).SetControllable(true);            
            GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
            (epLocalPlayer as EntityPlayerLocal).inventory[(epLocalPlayer as EntityPlayerLocal).inventory.holdingItemIdx].Activated = true;
            this.KillScript();
        }
        if (showUI && numAllowedAttachments > 0)
        {
            // block gun in vanilla UI
            (epLocalPlayer as EntityPlayerLocal).inventory[(epLocalPlayer as EntityPlayerLocal).inventory.holdingItemIdx].Activated = false;
            // GUISTYLES **************
            GUIStyle buttonSkin = new GUIStyle(GUI.skin.button);
            GUIStyle boxSkin = new GUIStyle(GUI.skin.box);
            GUIStyle titleSkin = new GUIStyle(GUI.skin.box);
            GUIStyle noJobSkin = new GUIStyle(GUI.skin.box);
            GUIStyle recTitleSkin = new GUIStyle(GUI.skin.box);
            buttonSkin.fontSize = 14;
            titleSkin.fontSize = 17;
            titleSkin.fontStyle = FontStyle.Bold;
            titleSkin.normal.textColor = Color.cyan;
            titleSkin.normal.background = MakeTex(2, 2, Color.red);
            boxSkin.normal.background = MakeTex(2, 2, Color.grey);
            boxSkin.normal.textColor = Color.white;
            boxSkin.fontSize = 14;
            boxSkin.fontStyle = FontStyle.Bold;
            noJobSkin.normal.background = MakeTex(2, 2, Color.grey);
            noJobSkin.normal.textColor = Color.yellow;
            noJobSkin.fontSize = 15;
            noJobSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.fontSize = 16;
            recTitleSkin.fontStyle = FontStyle.Bold;
            recTitleSkin.normal.textColor = Color.cyan;
            recTitleSkin.normal.background = MakeTex(2, 2, Color.green);
            // GUISTYLES **************
            (epLocalPlayer as EntityPlayerLocal).SetControllable(false);
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = true;
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
            scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 280, 500), scrollPosition,
                new Rect(0, 0, 250, 50*(numAllowedAttachments + 1)));
            GUI.color = Color.magenta;
            GUILayout.Box(string.Format("Custom Attachments"), titleSkin, GUILayout.Width(250),
                GUILayout.Height(50));
            GUI.color = colorNatural;
            #region Close Button;
            GUI.backgroundColor = buttonColor;
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Close", buttonSkin, GUILayout.Width(250),
                            GUILayout.Height(50)))
            {
                (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
                (epLocalPlayer as EntityPlayerLocal).SetControllable(true);
                GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
                showUI = false;
            }
            #endregion;                        
            GUI.contentColor = Color.white;
            GUI.backgroundColor = originalColor;
            string msg = "";
            if (item.Attachments != null)
            {
                if (item.Attachments.Length >= 1)
                {
                    numAllowedAttachments = item.Attachments.Length;
                    //for (int i = 1; i <= (item.Attachments.Length - 1); i++)
                    for (int i = 0; i <= (item.Attachments.Length - 1); i++)
                    {
                        ItemValue itemA = ItemClass.GetItem(item.Attachments[i]);
                        if (itemA != null)
                        {
                            msg = ItemClass.GetForId(itemA.type).GetLocalizedItemName();
                            // check if the item is attached or not
                            if (checkAttached(itemA))
                            {
                                msg = "DETTACH: " + msg;
                                GUI.backgroundColor = Color.blue;
                            }
                            else
                            {
                                int nummAttachs = (epLocalPlayer as EntityPlayerLocal).inventory.holdingItemData.holdingEntity.bag.GetItemCount(itemA);
                                if (nummAttachs > 0)
                                    msg = "ATTACH: " + msg;
                                else msg = "NA: " + msg;
                                GUI.backgroundColor = Color.red;
                            }
                            if (msg.Contains("NA:"))
                            {
                                GUI.backgroundColor = originalColor;
                                GUI.contentColor = Color.white;
                                GUILayout.Box(msg, boxSkin, GUILayout.Width(250),
                                    GUILayout.Height(50));
                            }                           
                            else if (GUILayout.Button(msg, buttonSkin,
                                GUILayout.Width(250),
                                GUILayout.Height(50)))
                            {
                                // attach or detach
                                (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
                                (epLocalPlayer as EntityPlayerLocal).SetControllable(true);
                                GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
                                //(epLocalPlayer as EntityPlayerLocal).inventory[(epLocalPlayer as EntityPlayerLocal).inventory.holdingItemIdx].Activated = true;
                                showUI = false;
                                gun.ToogleAttachment((epLocalPlayer as EntityPlayerLocal).inventory.holdingItemData, i, true);                                
                            }
                        }
                    }
                }
            }
            GUI.contentColor = Color.white;
            GUI.backgroundColor = originalColor;
            GUI.EndScrollView();
        }
        lastShowUI = showUI;
    }

    private bool checkAttached(ItemValue attach)
    {
        bool result = false;
        if (epLocalPlayer.inventory.holdingItemData.itemValue.Attachments.Length > 0)
        {
            ItemClass forId = ItemClass.GetForId(attach.type);
            //DisplayChatAreaText("There are available attachments");
            foreach (ItemValue itemA in epLocalPlayer.inventory.holdingItemData.itemValue.Attachments)
            {
                if (itemA != null)
                {
                    ItemClass forIdAux = ItemClass.GetForId(itemA.type);
                    if (forIdAux != null)
                    {
                        if (forId.Name == forIdAux.Name)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
        }
        return result;
    }

    private Texture2D MakeTex(int width, int height, Color col)
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

    public void KillScript()
    {
        if (epLocalPlayer != null)
        {
            //(epLocalPlayer as EntityPlayerLocal).inventory[(epLocalPlayer as EntityPlayerLocal).inventory.holdingItemIdx].Activated = true;
        }
        WeaponGUI script = gameObject.GetComponent<WeaponGUI>();
        if (script != null)
        {
            Destroy(this);
        }
        else
        {
            //Debug.Log("FindChildTele script not found");
        }
    }
}
