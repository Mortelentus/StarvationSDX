using System;
using UnityEngine;

/// <summary>
/// Custom class for ACW gun with laser (inherited from ItemClass)
/// </summary>
public class ItemClassACWLaser : ItemClass
{
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    // ACW Laser v1.0
    // --------------
    // by Matite    
    // November 2015
    // 7 Days to Die Alpha 12.5
    // This ACW gun code is inherited from the devs ItemClass code. It allows it to act like
    // a normal item apart from the code this class adds:
    // * toggle the laser on or off by pressing the numberpad 5 key
    // * change the flashlight color to green by pressing the numberpad 6 key
    // * change the flashlight color to white by pressing the numberpad 4 key
    //
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------

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


    /*
    public override void StartHolding(ItemInventoryData _data, Transform _modelTransform)
	{
        // Debug
        DisplayChatAreaText("StartHolding");

        base.StartHolding(_data, _modelTransform);
		if (_modelTransform == null)
		{
			return;
		}
		OnActivateItemGameObjectReference component = _modelTransform.GetComponent<OnActivateItemGameObjectReference>();
		if (component != null && !component.IsActivated())
		{
			component.ActivateItem(true);
		}
      
    }
    */


    /// <summary>
    /// Called when the player stops holding the ACW item
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    /// <param name="_modelTransform">A reference to the models transform.</param>
	public override void StopHolding(ItemInventoryData _data, Transform _modelTransform)
	{
        // Debug
        //DisplayChatAreaText("StopHolding");

        // Call base code
        base.StopHolding(_data, _modelTransform);

        // Check if the model transform is null
		if (_modelTransform == null)
		{
			return;
		}
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
    /// Checks if we have stored a reference to the LineRenderer component (laser)... if not it gets one and returns true if all is ok
    /// </summary>
    /// <param name="_data">Item inventory data reference</param>
    /// <returns>Returns true if the reference to the LineRenderer component (laser) is valid</returns>
    private bool CheckLineRendererReference(ItemInventoryData _data)
    {
        // Check if the reference to the LineRenderer component is null
        if (!lrLaser)
        {
            // Debug
            //DisplayChatAreaText("Warning: ACWLaser LineRenderer reference is null... getting reference now.");

            // Get and store a reference to the LineRenderer component
            lrLaser = _data.model.GetComponentInChildren<LineRenderer>();

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


    /// <summary>
    /// Called when holding the ACW item
    /// </summary>
    /// <param name="_data">A reference to the items inventory data.</param>
    public override void OnHoldingUpdate(ItemInventoryData _data)
    {
        // Debug
        //DisplayChatAreaText("OnHoldingUpdate");

        // Base code
        base.OnHoldingUpdate(_data);

        // Don't run this code if remote entity
        if (_data.holdingEntity.isEntityRemote)
        {
            return;
        }

        // Check if the laser is enabled
        if (boolLaserEnabled == true)
        {
            // Check reference to local player
            if (!epLocalPlayer)
            {
                // Get and store a reference to the local player
                epLocalPlayer = GameManager.Instance.World.GetLocalPlayer();

                // Debug
                //DisplayChatAreaText("Reference to local player stored.");
            }                                  

            // Check if the player is aiming the gun (we turn off the laser when aiming so it is not in the way)            
            if (epLocalPlayer.AimingGun)
            {
                // Check LineRenderer component reference (gets a reference if none currently exists)
                if (!CheckLineRendererReference(_data))
                {
                    // Exit here (message already displayed)
                    return;
                }

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
            else
            {
                // Check LineRenderer component reference (gets a reference if none currently exists)
                if (!CheckLineRendererReference(_data))
                {
                    // Exit here (message already displayed)
                    return;
                }

                // Check aiming mode
                if (boolAimingMode == true)
                {
                    // Disable the laser
                    lrLaser.enabled = true;

                    // Debug
                    //DisplayChatAreaText("Not Aiming (laser was switched back on)");

                    // Set flag
                    boolAimingMode = false;
                }
            }
        }
             
        // Check for numberpad 5 key
        if (Input.GetKey(KeyCode.Keypad5))
        {
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
        }
        // Check for numberpad 4 key
        else if (Input.GetKey(KeyCode.Keypad4))
        {
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
                    DisplayChatAreaText("Switch on your flashlight then try again");

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
        }
        // Check for numberpad 6 key
        else if (Input.GetKey(KeyCode.Keypad6))
        {
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
                    DisplayChatAreaText("Switch on your flashlight then try again");

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
        }
    }
}
