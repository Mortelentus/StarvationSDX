using System;
using InControl;
using UnityEngine;
//using Random = System.Random;

/// <summary>
/// Custom class for WalkyTalky (inherited from ItemClass)
/// </summary>
public class ItemClassRadio : ItemClass
{
    private RadioGui script = null;
    /// <summary>
    /// Stores a reference to the local player
    /// </summary>
    private EntityPlayerLocal epLocalPlayer;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;
    private DateTime dteNextAction = DateTime.MinValue;

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
            //GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, str, "", false, "", false);
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


    public override void StartHolding(ItemInventoryData _data, Transform _modelTransform)
    {
        base.StartHolding(_data, _modelTransform);
        epLocalPlayer = null;        
    }

    private void AddScript(ItemInventoryData _data)
    {
        script = _data.model.gameObject.GetComponent<RadioGui>();
        if (script != null)
        {
            script.KillScript();
        }
        if (script == null)
        {
            script = _data.model.gameObject.AddComponent<RadioGui>();
        }
        if (script != null)
        {
            script.Initialize(epLocalPlayer);
            //script.ShowUI();
        }
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
        // Reset flags
        // Call base code
        base.StopHolding(_data, _modelTransform);
        script = _data.model.gameObject.GetComponent<RadioGui>();
        if (script != null)
        {
            script.KillScript();
        }
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
        }
        if (script == null && epLocalPlayer != null) AddScript(_data);
        if (Input.GetKey(KeyCode.Insert))
        {
            if (script != null) script.ShowUI();
        }
        if(Input.GetKey(KeyCode.Keypad1) && DateTime.Now> dteNextAction)
        {
            dteNextAction.AddSeconds(1);
            if (script != null)
            {
                if (!script.showUI)
                    script.SetChannel(1);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad2) && DateTime.Now > dteNextAction)
        {
            dteNextAction.AddSeconds(1);
            if (script != null)
            {
                if (!script.showUI)
                    script.SetChannel(2);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad3) && DateTime.Now > dteNextAction)
        {
            dteNextAction.AddSeconds(1);
            if (script != null)
            {
                if (!script.showUI)
                    script.SetChannel(3);
            }
        }
        else if (Input.GetKey(KeyCode.Keypad4) && DateTime.Now > dteNextAction)
        {
            dteNextAction.AddSeconds(1);
            if (script != null)
            {
                if (!script.showUI)
                    script.SetChannel(4);
            }
        }
        else if (Input.GetKey(KeyCode.F) && DateTime.Now > dteNextAction)
        {
            dteNextAction.AddSeconds(1);
            if (script != null)
            {
                if (!script.showUI)
                    script.Switch();
            }
        }
        if (IsOn(_data))
        {
            // TODO - do decay
        }
    }

    public bool IsOn(ItemInventoryData _data)
    {        
        return ((int)_data.itemValue.Meta & 1 << 0) != 0;
    }   
}

public class RadioGui : MonoBehaviour
{
    private EntityPlayer epLocalPlayer;
    ItemValue radioV = null;
    public bool showUI = false;
    bool lastShowUI = false;
    private Rect guiAreaRect = new Rect(0, 0, 0, 0);
    private Rect infoAreaRect = new Rect(0, 0, 0, 0);
    Color colorNatural = GUI.color;
    Color originalColor = Color.grey;
    Color titleColor = Color.red;
    Color buttonColor = Color.green;
    public Vector2 scrollPosition = Vector2.zero;
    private bool openUI = false;

    void Start()
    {
        showUI = false;
    }

    public void Initialize(EntityPlayer _epLocalPlayer)
    {
        epLocalPlayer = _epLocalPlayer;
    }
    /// <summary>
    /// UI on/off
    /// </summary>
    public void ShowUI()
    {
        if (showUI)
        {
            showUI = false;
            this.KillScript();
        }
        else
        {
            showUI = true;
        }
    }

    void Update()
    {

    }

    public void RotateChannel()
    {
        if (radioV == null) return;
        if (RadioChannel() < 4) SetChannel(RadioChannel() + 1);
        else SetChannel(1);
    }

    public void SetChannel(int channel)
    {
        if (radioV == null) return;
        if (channel == 1)
        {
            // all 3 bits to 0
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 1));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 2));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 3));
        }
        else if (channel == 2)
        {
            // bit 1
            radioV.Meta = (byte)(radioV.Meta | (1 << 1));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 2));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 3));
        }
        else if (channel == 3)
        {
            // bit 2
            radioV.Meta = (byte)(radioV.Meta | (1 << 2));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 1));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 3));
        }
        else if (channel == 4)
        {
            // bit 3
            radioV.Meta = (byte)(radioV.Meta | (1 << 3));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 1));
            radioV.Meta = (byte)(radioV.Meta & ~(1 << 2));
        }
        epLocalPlayer.inventory.holdingItemStack.itemValue = radioV;
        epLocalPlayer.inventory.ForceHoldingItemUpdate();
        epLocalPlayer.inventory.CallOnToolbeltChangedInternal();
    }

    private int RadioChannel()
    {
        if (radioV == null) return 0;
        if (((int)radioV.Meta & 1 << 1) != 0) return 2;
        if (((int)radioV.Meta & 1 << 2) != 0) return 3;
        if (((int)radioV.Meta & 1 << 3) != 0) return 4;
        return 1;
    }

    public bool IsOn()
    {
        if (radioV == null) return false;
        return ((int)radioV.Meta & 1 << 0) != 0;
    }

    public void Switch()
    {
        if (radioV == null) return;
        if (IsOn()) radioV.Meta = (byte)(radioV.Meta & ~(1 << 0));
        else radioV.Meta = (byte)(radioV.Meta | (1 << 0));
        epLocalPlayer.inventory.holdingItemStack.itemValue = radioV;
        epLocalPlayer.inventory.ForceHoldingItemUpdate();
        epLocalPlayer.inventory.CallOnToolbeltChangedInternal();
    }

    public void OnGUI()
    {
        if (showUI)
        {
            (epLocalPlayer as EntityPlayerLocal).SetControllable(false);
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = true;
            GameManager.Instance.windowManager.SetMouseEnabledOverride(true);
        }
        else
        {
            (epLocalPlayer as EntityPlayerLocal).bIntroAnimActive = false;
            (epLocalPlayer as EntityPlayerLocal).SetControllable(true);
            GameManager.Instance.windowManager.SetMouseEnabledOverride(false);            
        }
        if (true)
        {
            // block gun in vanilla UI
            radioV = (epLocalPlayer as EntityPlayerLocal).inventory.holdingItemItemValue;
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
            scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 280, 500), scrollPosition,
                new Rect(0, 0, 250, 50*(4 + 1)));
            GUI.color = Color.magenta;
            if (IsOn())
            {
                GUI.color = Color.grey;
                titleSkin.normal.textColor = Color.white;
                titleSkin.normal.background = MakeTex(2, 2, Color.green);
            }
            GUILayout.Box(string.Format("Radio Channel {0}\nTurned {1}", RadioChannel(), IsOn() ? "On" : "Off"), titleSkin, GUILayout.Width(250),
                GUILayout.Height(50));
            GUI.color = colorNatural;
            if (showUI)
            {
                #region Close Button;

                GUI.backgroundColor = buttonColor;
                GUI.contentColor = Color.red;
                if (GUILayout.Button("Close", buttonSkin, GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    showUI = false;
                }

                #endregion;                        

                GUI.contentColor = Color.white;
                GUI.backgroundColor = originalColor;
                string msg = "";
                if (IsOn()) msg = "Turn Off";
                else msg = "Turn On";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    Switch();
                }
                msg = "Channel 1";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    SetChannel(1);
                }
                msg = "Channel 2";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    SetChannel(2);
                }
                msg = "Channel 3";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    SetChannel(3);
                }
                msg = "Channel 4";
                if (GUILayout.Button(msg, buttonSkin,
                    GUILayout.Width(250),
                    GUILayout.Height(50)))
                {
                    SetChannel(4);
                }
            }
            GUI.contentColor = Color.white;
            GUI.backgroundColor = originalColor;
            GUI.EndScrollView();
        }
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
        RadioGui script = gameObject.GetComponent<RadioGui>();
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
