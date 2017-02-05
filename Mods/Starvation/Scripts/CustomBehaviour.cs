using System;
using UnityEngine;
using SDX.Payload;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;

public class ConsoleCmdCustomChat : global::ConsoleCmdAbstract
{
    // Token: 0x170003B0 RID: 944
    // (get) Token: 0x060019FA RID: 6650 RVA: 0x000B58D0 File Offset: 0x000B3AD0
    public override bool IsExecuteOnClient
    {
        get
        {
            return true;
        }
    }

    // Token: 0x060019FB RID: 6651 RVA: 0x000B58D4 File Offset: 0x000B3AD4
    public override string[] GetCommands()
    {
        return new string[]
        {
            "toggleChat"
        };
    }

    // Token: 0x060019FC RID: 6652 RVA: 0x000B58E4 File Offset: 0x000B3AE4
    public override void Execute(List<string> _params, global::CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            MorteHelpers.chatModEnabled = !MorteHelpers.chatModEnabled;
            string state = "CUSTOM CHAT ENABLED IS NOW " + MorteHelpers.chatModEnabled.ToString();
            global::SingletonMonoBehaviour<global::SdtdConsole>.Instance.Output(state);
        }
        else
        {
            global::SingletonMonoBehaviour<global::SdtdConsole>.Instance.Output("toggleChat has no arguments!");
        }
    }

    // Token: 0x060019FD RID: 6653 RVA: 0x000B59B0 File Offset: 0x000B3BB0
    public override string GetDescription()
    {
        return "Applies a buff to the local player";
    }
}

public static class MorteHelpers
{
    public static string modVersion = "STARVATION 15.1.9.9";
    private static string whisperColor = "[B536DA]";
    private static string radioColor = "[478896]";
    private static string megaColor = "[E11818]";
    private static string shoutColor = "[FF6700]";
    private static string ChatColor = "[FFFFFF]";
    private static string InfoColor = "[00FF00]";
    private static string AdminColor = "[B0FF00]";    
    private static string radioItem = "WalkyTalky";
    private static string megaPhoneItem = "MegaPhone";
    private static string headsetItem = "MilitaryHeadset";
    public static string GamePath = GamePrefs.GetString(EnumGamePrefs.SaveGameFolder);
    public static string ConfigPath = string.Format("{0}/Starvation", GamePath);
    public static bool chatModEnabled = true;
    public static bool configLoaded = false;

    public static void LoadConfig()
    {
        if (!Directory.Exists(ConfigPath))
        {
            Directory.CreateDirectory(ConfigPath);
        }
        Config.Load();
    }

    public static bool RadioIsOn(ItemValue radioV)
    {
        if (radioV == null) return false;
        return ((int)radioV.Meta & 1 << 0) != 0;
    }

    private static int RadioChannel(ItemValue radioV)
    {
        if (radioV == null) return 0;
        if (((int)radioV.Meta & 1 << 1) != 0) return 2;
        if (((int)radioV.Meta & 1 << 2) != 0) return 3;
        if (((int)radioV.Meta & 1 << 3) != 0) return 4;
        return 1;
    }

    public static void MessageHook(ClientInfo _cInfo, EnumGameMessages _type, string _msg, string _mainName, bool _localizeMain, string _secondaryName, bool _localizeSecondary)
    {
        //Debug.Log("CHAT HOOK");
        if (!configLoaded)
        {
            configLoaded = true;
            LoadConfig();
            Debug.Log("CUSTOM CHAT ENABLED = " + chatModEnabled.ToString());
        }
        if (_msg == "")
        {
            Debug.Log("MSG EMPTY OR _cInfo is null");
            return;
        }
        ConnectionManager cman = ConnectionManager.Instance;
        try
        {
            if (Steam.Network.IsServer && _cInfo == null)
            {                
                VanillaChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
                return;
            }
            // check if modded chat is enabled.
            Debug.Log("Chat: " + _msg);
            if (_msg.StartsWith("/toggleChatMod") && !GameManager.Instance.World.IsRemote())
            {
                EntityAlive player = null;
                var state = "";
                if (!GameManager.IsDedicatedServer)
                {
                    try
                    {
                        player = (GameManager.Instance.World.GetLocalPlayer() as EntityAlive);
                    }
                    catch (Exception)
                    {
                        player = null;
                    }
                }
                if (_cInfo != null)
                {
                    if (GameManager.Instance.adminTools.IsAdmin(_cInfo.playerId))
                    {
                        state = ToggleChat();
                    }
                    else state = "You are not allowed to do this";
                    if (!GameManager.IsDedicatedServer)
                    {
                        if (player != null)
                            GameManager.Instance.GameMessage(EnumGameMessages.Chat, state, player);
                    }
                    else
                        _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, state, "Server", false, "",
                            false));
                }
                else
                {
                    state = ToggleChat();
                    if (player != null)
                        GameManager.Instance.GameMessage(EnumGameMessages.Chat, state, player);
                }
                return;
            }
            if (_mainName.ToLower().Contains("server") || _msg == "")
                VanillaChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
            else if (chatModEnabled && Steam.Network.IsServer)
            {
                CustomChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
            }
            else
            {
                VanillaChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
            }
        }
        catch (Exception ex)
        {
            // TODO - solve all exceptions
            //Debug.Log("STARVATION: Check this later");            
        }             
    }

    private static string ToggleChat()
    {
        chatModEnabled = !chatModEnabled;
        string state = "CUSTOM CHAT ENABLED IS NOW " + chatModEnabled.ToString();
        Debug.Log(state);
        Config.UpdateConfig();
        return state;
    }

    private static void VanillaChat(ClientInfo _cInfo, EnumGameMessages _type, string _msg, string _mainName,
        bool _localizeMain, string _secondaryName, bool _localizeSecondary, ConnectionManager cman)
    {
        //Debug.Log("USING VANILLA CODE");        
        if (Steam.Network.IsServer)
        {
            string text = ModManager.ChatMessage(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary);
            string text2 = GameManager.Instance.DisplayGameMessage(_type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, true, text == null);
            if (text == null)
            {
                cman.SendPackage(new NetPackageGameMessage(_type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary), new IPackageDestinationFilter[]
                {
                new PackageDestinationAttachedToEntity()
                });
            }
            else
            {
                Debug.Log(string.Format("GameMessage handled by mod '{0}': {1}", text, text2));
            }
        }
        else
        {
            cman.SendToServer(new NetPackageGameMessage(_type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary), false);
        }
    }

    private static void CustomChat(ClientInfo _cInfo, EnumGameMessages _type, string _msg, string _mainName,
        bool _localizeMain, string _secondaryName, bool _localizeSecondary, ConnectionManager cman)
    {
        //Debug.Log("USING CUSTOM CHAT");
        Entity player = null;
        if (!GameManager.IsDedicatedServer)
        {
            player = GameManager.Instance.World.GetEntity(GameManager.Instance.GetPersistentLocalPlayer().EntityId);
        }
        else if (_cInfo != null)
            player = GameManager.Instance.World.GetEntity(_cInfo.entityId);
        if (_msg.StartsWith("/buffs"))
        {
            if (!GameManager.Instance.adminTools.IsAdmin(_cInfo.playerId)) return;
            // returns player buffs to self
            // get localplayer                        
            if (player is EntityPlayer)
            {
                // gets all his buffs
                string buffMs = "";
                foreach (Buff buffS in (player as EntityPlayer).Stats.Buffs)
                {
                    buffMs += buffS.Name + "; ";
                }
                if (buffMs == "") buffMs = "NO CURRENT BUFFS";
                if (!GameManager.IsDedicatedServer)
                {
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, buffMs, (player as EntityAlive));                   
                }
                else _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, buffMs, "Server", false, "", false));
            }
            return;
        }

        int msgRange = 0; // 0 = infinite
        EntityPlayer _player = null;
        try
        {
            if (player != null)
            {
                if (player is EntityPlayer) _player = (player as EntityPlayer);
            }
            //_player = GameManager.Instance.World.Players.dict[_cInfo.entityId];
        }
        catch (Exception)
        {
            _player = null;
        }        
        if (_player != null)
        {
            bool hasRadio = false;
            int radioChannel = 0;
            bool hasMegaphone = false;
            bool militaryHeadset = false;
            // see if player has a military headset
            radioChannel = HasHEadset(_player, radioChannel, ref hasRadio, ref militaryHeadset);
            // see what is the player holding
            ItemClass itemHeld = _player.inventory.holdingItem;
            if (itemHeld != null)
            {
                if (itemHeld.Name == radioItem && !militaryHeadset)
                {
                    hasRadio = true;
                    radioChannel = RadioChannel(_player.inventory.holdingItemItemValue);
                    string msg = string.Format("Player holding radio in channel={0} and On={1}",
                        RadioChannel(_player.inventory.holdingItemItemValue),
                        RadioIsOn(_player.inventory.holdingItemItemValue));
                    //Debug.Log(msg);
                }
                else if (itemHeld.Name == megaPhoneItem) hasMegaphone = true;
            }
            if (_msg.StartsWith("/admin"))
            {
                _msg = _msg.Replace("/admin", "");
                SendAdmins(_cInfo, _msg, false);
            }
            else if (_msg.StartsWith("/all"))
            {
                _msg = _msg.Replace("/all", "");
                VanillaChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
            }
            else if (_msg.StartsWith("/r"))
            {
                if (!hasRadio)
                {                    
                    _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}You are not holding a radio[-]", InfoColor), "Server", false, "", false));
                }
                else
                {
                    if (!RadioIsOn(_player.inventory.holdingItemItemValue))
                        _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}You need to turn on your radio[-]", InfoColor), "Server", false, "", false));
                    else
                    {
                        _msg = _msg.Substring(2).Trim();
                        // radio message
                        //Debug.Log("RADIO MESSAGE");
                        SendRadioMessage(_msg, _mainName, _localizeMain, _player, radioColor, radioItem, radioChannel);
                        // TODO - decay radio   
                    }
                }                
            }
            else if (_msg.StartsWith("/w"))
            {
                //Debug.Log("WHISPER");
                _msg = _msg.Substring(2).Trim();
                // whisper
                // look for players within 1 block only
                msgRange = 1;
                string color = whisperColor;
                SendRangedMessage(_msg, _mainName, _localizeMain, _player, msgRange, color, "");
            }
            else if (_msg.StartsWith("/s"))
            {
                //Debug.Log("SHOUT");
                _msg = _msg.Substring(2).Trim();
                // whisper
                // look for players within 20 block only
                msgRange = 20;
                string color = shoutColor;
                SendRangedMessage(_msg, _mainName, _localizeMain, _player, msgRange, color, "");
            }
            else
            {
                if (hasRadio && militaryHeadset)
                {
                    // military headset, talks to turned on radio by default, but also whispers
                    //Debug.Log("HEADSET MESSAGE");
                    SendRadioMessage(_msg, _mainName, _localizeMain, _player, radioColor, radioItem, radioChannel);
                    // TODO - decay radio
                }
                else
                {
                    // talk
                    msgRange = 10;
                    string color = ChatColor;
                    if (hasMegaphone)
                    {
                        msgRange = 40;
                        color = megaColor;
                    }
                    //Debug.Log("NORMAL MESSAGE");
                    SendRangedMessage(_msg, _mainName, _localizeMain, _player, msgRange, color, "");
                }                
            }
        }
        else VanillaChat(_cInfo, _type, _msg, _mainName, _localizeMain, _secondaryName, _localizeSecondary, cman);
    }

    private static int HasHEadset(EntityPlayer _player, int radioChannel, ref bool hasRadio, ref bool militaryHeadset)
    {
        ItemValue headItemValue = _player.equipment.GetSlotItem((int) XMLData.Item.EnumEquipmentSlot.Head);
        if (headItemValue != null)
        {
            ItemClass itemClassHead = null;
            if (itemClassHead == null)
            {
                //Debug.Log(string.Format("Player has no head item??"));
                int i = 0;
                foreach (ItemValue item in _player.equipment.GetItems())
                {
                    if (item != null)
                    {
                        itemClassHead = ItemClass.GetForId(item.type);
                        if (itemClassHead != null)
                        {
                            if (itemClassHead.Name == headsetItem)
                            {
                                //Debug.Log(string.Format("Player is wearing {0} on slot {1}", itemClassHead.Name, i));
                                // check if player has a turned on radio on the belt
                                foreach (ItemStack stack in _player.inventory.GetSlots())
                                {
                                    if (stack != null)
                                    {
                                        if (stack.itemValue != null)
                                        {
                                            itemClassHead = ItemClass.GetForId(stack.itemValue.type);
                                            if (itemClassHead != null)
                                            {
                                                if (itemClassHead.Name == radioItem)
                                                {
                                                    radioChannel = RadioChannel(stack.itemValue);
                                                    if (RadioIsOn(stack.itemValue))
                                                    {
                                                        //Debug.Log(string.Format("Player is wearing HEADSET on slot {0} and radio is ON", i));
                                                        hasRadio = true;
                                                        militaryHeadset = true;
                                                        return radioChannel;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    i++;
                }
                return 0;
            }
        }
        return radioChannel;
    }

    private static void SendRadioMessage(string _msg, string _mainName, bool _localizeMain, EntityPlayer player,
        string color, string requiredItem, int channel)
    {
        List<ClientInfo> _cInfoList = ConnectionManager.Instance.GetClients();
        _msg = string.Format("{0}(CH{2}){1}[-]", color, _msg, channel);
        foreach (ClientInfo _cInfo in _cInfoList)
        {
            EntityPlayer _player = null;
            try
            {
                _player = GameManager.Instance.World.Players.dict[_cInfo.entityId];
            }
            catch (Exception)
            {
                _player = null;
            }
            if (_player != null)
            {
                bool canSend = true;
                if (requiredItem != "")
                {
                    canSend = false;
                    bool hasRadio = false;
                    int radioChannel = 0;
                    bool militaryHeadset = false;
                    // see if player has a military headset
                    radioChannel = HasHEadset(_player, radioChannel, ref hasRadio, ref militaryHeadset);
                    if (militaryHeadset && hasRadio && radioChannel == channel) canSend = true;
                    else
                    {
                        ItemClass itemHeld = _player.inventory.holdingItem;
                        if (itemHeld != null)
                        {
                            if (itemHeld.Name == requiredItem)
                            {
                                string msg = string.Format("Player {2} holding radio in channel={0} and On={1}",
                                    RadioChannel(_player.inventory.holdingItemItemValue),
                                    RadioIsOn(_player.inventory.holdingItemItemValue), _cInfo.playerName);
                                //Debug.Log(msg);
                                if (RadioChannel(_player.inventory.holdingItemItemValue) == channel)
                                {
                                    if (RadioIsOn(_player.inventory.holdingItemItemValue))
                                        canSend = true;
                                }
                            }
                        }
                    }                    
                }
                if (canSend)
                {
                    // send message with a different color.                    
                    _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, _msg, _mainName,
                        _localizeMain, "", false));
                }
            }
        }
    }

    private static void SendRangedMessage(string _msg, string _mainName, bool _localizeMain, EntityPlayer player, int msgRange,
        string color, string requiredItem)
    {
        _msg = string.Format("{0}{1}[-]", color, _msg);
        using (
            List<Entity>.Enumerator enumerator =
                GameManager.Instance.World.GetEntitiesInBounds(typeof (EntityPlayer),
                    BoundsUtils.BoundsForMinMax(player.position.x - msgRange, player.position.y - msgRange,
                        player.position.z - msgRange, player.position.x + msgRange,
                        player.position.y + msgRange,
                        player.position.z + msgRange)).GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                EntityPlayer _other = enumerator.Current as EntityPlayer;
                bool canSend = true;
                if (requiredItem != "")
                {
                    canSend = false;
                    ItemClass itemHeld = _other.inventory.holdingItem;
                    if (itemHeld != null)
                    {
                        if (itemHeld.Name == requiredItem)
                        {
                            canSend = true;
                        }
                    }
                }
                if (canSend)
                {
                    PersistentPlayerData dataFromEntityId1 =
                        GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(_other.entityId);
                    // send message with a different color.                    
                    ClientInfo _destination = ConsoleHelper.ParseParamIdOrName(dataFromEntityId1.PlayerId, false,
                        false);
                    _destination.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, _msg, _mainName,
                        _localizeMain, "", false));
                }
            }
        }
    }

    public static void SendAdmins(ClientInfo _sender, string _message, bool toAll)
    {
        if (!GameManager.Instance.adminTools.IsAdmin(_sender.playerId))
        {
            string _phrase200 = "You do not have permissions to use this command.";
            _sender.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}{1}[-]", InfoColor, _phrase200), "Server", false, "", false));
        }
        else
        {
            List<ClientInfo> _cInfoList = ConnectionManager.Instance.GetClients();
            foreach (ClientInfo _cInfo in _cInfoList)
            {
                if (GameManager.Instance.adminTools.IsAdmin(_cInfo.playerId) || toAll)
                {
                    _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}{1}[-]", AdminColor, _message), _sender.playerName, false, "", false));
                }
            }
        }
    }

    public static string checkSound(string str, EntityAlive entity)
    {
        if (entity is EntityPlayerLocal)
        {
            // check if it has power armor WITH power
            EntityPlayerLocal player = (entity as EntityPlayerLocal);
            foreach (Buff buff in player.Stats.Buffs)
            {                           
                if (buff.Name == "Exoskeleton")
                {
                    int powerLevel = 0;
                    MultiBuffVariable multiBuffVariable1;
                    Dictionary<string, MultiBuffVariable> playerStats = getStats(player);                   
                    if (playerStats != null)
                    {
                        //if (player.Stats.PI.TryGetValue("powerLevel", out multiBuffVariable1))
                        if (playerStats.TryGetValue("powerLevel",
                            out multiBuffVariable1))
                        {
                            powerLevel = Convert.ToInt32(multiBuffVariable1.Value);
                        }
                        else powerLevel = 0;
                        if (powerLevel > 0) str = "Exo";
                    }
                    break;
                }
            }
        }
        return str;
    }

    public static Dictionary<string, MultiBuffVariable> getStats(EntityPlayerLocal player)
    {
        Dictionary<string, MultiBuffVariable> sts = new Dictionary<string, MultiBuffVariable>();
        if (!GameManager.IsDedicatedServer)
        {
            try
            {
                //Debug.Log("LOOKING FOR PLAYER STATS");
                var playerStats = player.Stats.GetType().GetFields().First(d => d.Name == "PI").GetValue(player.Stats);
                sts = (playerStats as Dictionary<string, MultiBuffVariable>);
                //Debug.Log("Found playerStats");
                return sts;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
        else
        {
            try
            {
                var playerStats = player.Stats.GetType().GetFields().First(d => d.Name == "MB").GetValue(player.Stats);
                sts = (playerStats as Dictionary<string, MultiBuffVariable>);
                return sts;
            }
            catch (Exception)
            {

            }
        }            
        return null;
    }
    public static bool checkPowerOn(EntityAlive entity)
    {
        if (entity is EntityPlayerLocal)
        {
            EntityPlayerLocal player = (entity as EntityPlayerLocal);
            foreach (Buff buff in player.Stats.Buffs)
            {
                if (buff.Name == "Exoskeleton")
                {
                    int powerLevel = 0;
                    MultiBuffVariable multiBuffVariable1;
                    Dictionary<string, MultiBuffVariable> playerStats = getStats(player);
                    if (playerStats != null)
                    {
                        //if (player.Stats.PI.TryGetValue("powerLevel", out multiBuffVariable1))
                        if (playerStats.TryGetValue("powerLevel",
                            out multiBuffVariable1))
                        {
                            powerLevel = Convert.ToInt32(multiBuffVariable1.Value);
                        }
                        else powerLevel = 0;
                        if (powerLevel > 0) return false;
                    }
                    break;
                }
            }
        }
        return true;
    }
}

public class Config
{
    private const string configFile = "StarvationConfig.xml";
    private static string configFilePath = string.Format("{0}/{1}", MorteHelpers.ConfigPath, configFile);

    public static void Load()
    {
        LoadConfig();
    }

    private static void LoadConfig()
    {
        if (!Utils.FileExists(configFilePath))
        {
            UpdateConfig();
            return;
        }
        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            xmlDoc.Load(configFilePath);
        }
        catch (XmlException e)
        {
            Debug.Log(string.Format("[STARVATION] Failed loading {0}: {1}", configFilePath, e.Message));
            return;
        }
        XmlNode _configXml = xmlDoc.DocumentElement;
        foreach (XmlNode childNode in _configXml.ChildNodes)
        {
            if (childNode.Name == "Options")
            {
                foreach (XmlNode subChild in childNode.ChildNodes)
                {
                    if (subChild.NodeType == XmlNodeType.Comment)
                    {
                        continue;
                    }
                    if (subChild.NodeType != XmlNodeType.Element)
                    {
                        Debug.Log(string.Format("[STARVATION] Unexpected XML node found in 'Options' section: {0}",
                            subChild.OuterXml));
                        continue;
                    }
                    XmlElement _line = (XmlElement) subChild;
                    if (!_line.HasAttribute("Name"))
                    {
                        Debug.Log(
                            string.Format("[STARVATION] Ignoring tool entry because of missing 'Name' attribute: {0}",
                                subChild.OuterXml));
                        continue;
                    }
                    switch (_line.GetAttribute("Name"))
                    {
                        case "CustomChat":
                            if (!_line.HasAttribute("Enable"))
                            {
                                Debug.Log(
                                    string.Format(
                                        "[STARVATION] Ignoring CustomChat entry because of missing 'Enable' attribute: {0}",
                                        subChild.OuterXml));
                                continue;
                            }
                            if (!bool.TryParse(_line.GetAttribute("Enable"), out MorteHelpers.chatModEnabled))
                            {
                                Debug.Log(
                                    string.Format(
                                        "[STARVATION] Ignoring CustomChat entry because of invalid (true/false) value for 'Enable' attribute: {0}",
                                        subChild.OuterXml));
                                continue;
                            }
                            break;
                    }
                }
            }
        }
    }

    public static void UpdateConfig()
    {
        using (StreamWriter sw = new StreamWriter(configFilePath))
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sw.WriteLine("<Starvation>");
            sw.WriteLine("    <Options>");
            sw.WriteLine(string.Format("        <Option Name=\"CustomChat\" Enable=\"{0}\" />",
                MorteHelpers.chatModEnabled.ToString().ToLower()));
            sw.WriteLine("    </Options>");
            sw.WriteLine("</Starvation>");
            sw.Flush();
            sw.Close();
        }
    }
}

public class CustomBehaviour : ModScript
{
    private static BehaviourScript script;
    static DateTime dtaNextTickG = DateTime.MinValue;
    static GameObject obj;
    public override void OnGameStarted()
   {
        LogToConsole("CustomBehaviour - OnGameStarted");
        if (!GameManager.IsDedicatedServer)
        {
            if (obj == null)
                obj = new GameObject();
            script = obj.GetComponent<BehaviourScript>();
            if (script == null)
            {
                obj.AddComponent<BehaviourScript>();
            }
            // locks all receipts, even if the player has read the books
            LockReceips.LockAll();
        }
        //else Config.Load();
   }
    public void LogToConsole(string str)
    {
        try
        {
            Debug.Log(str);
            if (GameManager.Instance != null)
            {
                EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
            }
        }
        catch (Exception)
        {
           
        }		
    }
}

public class BehaviourScript : MonoBehaviour
{
    private void LogToConsole(string str)
    {
        try
        {
            Debug.Log(str);
        }
        catch (Exception)
        {

        }
    }

    private bool debug = false;
    DateTime dtaNextTick = DateTime.MinValue;
    DateTime dtaNextClick = DateTime.MinValue;
    Dictionary<string, string[]> currentSets = new Dictionary<string, string[]>();
    Dictionary<string, string[]> oldSets = new Dictionary<string, string[]>();
    private DateTime dtaNextFoodCheck = DateTime.Now.AddSeconds(30);
    private DateTime dtaNextHealCheck = DateTime.MinValue;
    private DateTime dtaNextSickCheck = DateTime.MinValue;
    private DateTime dtaNextDiseaseCheck = DateTime.MinValue;
    private ItemStack[] oldBag = null;
    private float fluModifier = 1.0F;
    private float lastWetValue = 0;
    private EntityPlayer player = null;
    // information GUI
    public Vector2 scrollPosition = Vector2.zero;
    public static bool exoSuit = false;
    private static bool exoHud = false;
    public static int powerLevel = 0;
    private static int radlevel = 0;
    private static int fatiguelevel = 0;
    private static int addiction = 0;
    private static int sanity = 0;
    private static Dictionary<string, Texture2D> hudTextures = new Dictionary<string, Texture2D>();
    private static Dictionary<string, MultiBuffVariable> playerStats = new Dictionary<string, MultiBuffVariable>();
    //private static Texture2D bgImage;
    //private static Texture2D fgImage;
    //private static Texture2D hudWindow;
    //private static Texture2D hudL;
    //private static Texture2D hudR;
    //private static Texture2D hudToolBelt;
    //private static Texture2D hudRadio;
    //private static Texture2D hudOverview;

    static int healthBarLength = 292;
    static int maxPower = 12;
    static int maxRad = 1000;
    static int maxAddiction = 60;
    static int maxSanity = 100;
    static int maxFatigue = 48;

    void Start()
    {
        LogToConsole("BehaviourScript: Start Script");
        LogToConsole("BehaviourScript: Loading UI elements");
        string hudPath = global::Utils.GetApplicationPath() + '/' + "Data/Hud";      
        //bgImage = new Texture2D(healthBarLength, 70);
        //fgImage = new Texture2D(healthBarLength, 70);
        //hudWindow = new Texture2D(1920, 1080);
        //hudL = new Texture2D(312, 366);
        //hudR = new Texture2D(175, 159);
        //hudToolBelt = new Texture2D(650, 105);
        //hudRadio = new Texture2D(175, 205);
        //hudOverview = new Texture2D(650, 583);
        Texture2D auxTexture;
        if (Directory.Exists(hudPath))
        {
            string[] files = Directory.GetFiles(hudPath);
            for (int i = 0; i < files.Length; i++)
            {
                string text = files[i];
                LogToConsole("BehaviourScript: Found file " + text);
                try
                {
                    if (text.ToLower().Contains("bgimage") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(healthBarLength, 70);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading bgImage");                            
                        }
                        else
                        {
                            hudTextures.Add("bgImage", auxTexture);
                            LogToConsole("BehaviourScript: Loaded bgImage");                            
                        }
                    }
                    else if (text.ToLower().Contains("fgimage") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(healthBarLength, 70);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading fgImage");
                        }
                        else
                        {
                            hudTextures.Add("fgImage", auxTexture);
                            LogToConsole("BehaviourScript: Loaded fgImage");                            
                        }
                    }
                    else if (text.ToLower().Contains("exowindow") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(1920, 1080);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading ExoWindow");
                        }
                        else
                        {
                            hudTextures.Add("hudWindow", auxTexture);
                            LogToConsole("BehaviourScript: Loaded ExoWindow");                            
                        }
                    }
                    else if (text.ToLower().Contains("exohudl") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(312, 366);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading ExoHudL");
                        }
                        else
                        {
                            hudTextures.Add("hudL", auxTexture); 
                            LogToConsole("BehaviourScript: Loaded ExoHudL");                            
                        }
                    }
                    else if (text.ToLower().Contains("exohudr") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(175, 159);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {                            
                            LogToConsole("BehaviourScript: Error loading ExoHudR");
                        }
                        else
                        {
                            hudTextures.Add("hudR", auxTexture);
                            LogToConsole("BehaviourScript: Loaded ExoHudR");                            
                        }
                    }
                    else if (text.ToLower().Contains("exoradio") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(175, 205);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading ExoRadio");
                        }
                        else
                        {
                            hudTextures.Add("hudRadio", auxTexture);
                            LogToConsole("BehaviourScript: Loaded ExoRadio");                            
                        }
                    }
                    else if (text.ToLower().Contains("exotoolbelt") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(650, 105);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {                            
                            LogToConsole("BehaviourScript: Error loading ExoToolbelt");
                        }
                        else
                        {
                            hudTextures.Add("hudToolBelt", auxTexture);
                            LogToConsole("BehaviourScript: Loaded ExoToolbelt");                            
                        }
                    }
                    else if (text.ToLower().Contains("exooverview") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(650, 583);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading exoOverview");
                        }
                        else
                        {
                            hudTextures.Add("hudOverview", auxTexture);
                            LogToConsole("BehaviourScript: Loaded exoOverview");
                        }
                    }
                    else if (text.Contains("AddictionEmpty") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading AddictionEmpty");
                        }
                        else
                        {
                            hudTextures.Add("AddictionEmpty", auxTexture);
                            LogToConsole("BehaviourScript: Loaded AddictionEmpty");
                        }
                    }
                    else if (text.Contains("AddictionFull") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading AddictionFull");
                        }
                        else
                        {
                            hudTextures.Add("AddictionFull", auxTexture);
                            LogToConsole("BehaviourScript: Loaded AddictionFull");
                        }
                    }
                    else if (text.Contains("FatigueEmpty") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading FatigueEmpty");
                        }
                        else
                        {
                            hudTextures.Add("FatigueEmpty", auxTexture);
                            LogToConsole("BehaviourScript: Loaded FatigueEmpty");
                        }
                    }
                    else if (text.Contains("FatigueFull") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading FatigueFull");
                        }
                        else
                        {
                            hudTextures.Add("FatigueFull", auxTexture);
                            LogToConsole("BehaviourScript: Loaded FatigueFull");
                        }
                    }
                    else if (text.Contains("RadiationEmpty") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading RadiationEmpty");
                        }
                        else
                        {
                            hudTextures.Add("RadiationEmpty", auxTexture);
                            LogToConsole("BehaviourScript: Loaded RadiationEmpty");
                        }
                    }
                    else if (text.Contains("RadiationFull") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading RadiationFull");
                        }
                        else
                        {
                            hudTextures.Add("RadiationFull", auxTexture);
                            LogToConsole("BehaviourScript: Loaded RadiationFull");
                        }
                    }
                    else if (text.Contains("SanityBarEmpty") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading SanityBarEmpty");
                        }
                        else
                        {
                            hudTextures.Add("SanityBarEmpty", auxTexture);
                            LogToConsole("BehaviourScript: Loaded SanityBarEmpty");
                        }
                    }
                    else if (text.Contains("SanityBarFull") && text.ToLower().EndsWith("png"))
                    {
                        auxTexture = new Texture2D(215, 60);
                        if (!auxTexture.LoadImage(File.ReadAllBytes(text)))
                        {
                            LogToConsole("BehaviourScript: Error loading SanityBarFull");
                        }
                        else
                        {
                            hudTextures.Add("SanityBarFull", auxTexture);
                            LogToConsole("BehaviourScript: Loaded SanityBarFull");
                        }
                    }
                }
                catch (Exception e)
                {
                    LogToConsole("BehaviourScript: Error loading UI elements " + e.Message);
                }
            }
        }
        else LogToConsole("BehaviourScript: DID NOT FIND FOLDER " + hudPath);
    }

    //public void Initiate()
    //{
    //    Debug.Log("BehaviourScript: initiate script");
    //    StartCoroutine(runCycle());
    //}
    //public IEnumerator runCycle()
    //{
    //    while (true)
    //    {
    //        if (DateTime.Now >= dtaNextTick)
    //        {
    //            dtaNextTick = DateTime.Now.AddSeconds(1);
    //            Debug.Log("BehaviourScript: GENERAL TICK");
    //            if (GameManager.Instance != null)
    //                GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, "GENERAL TICK", "", false, "", false);
    //        }
    //    }
    //}


    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.World == null) exoSuit = false;
        if (DateTime.Now >= dtaNextTick && GameManager.Instance != null && GameManager.Instance.gameStateManager.IsGameStarted())
        {
            dtaNextTick = DateTime.Now.AddSeconds(1);
            string pos = "0";
            try
            {
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.World != null)
                    {
                        //Debug.Log("BehaviourScript: GENERAL TICK");
                        // get localplayer                                        
                        player =
                            (GameManager.Instance.World.GetEntity(
                                GameManager.Instance.GetPersistentLocalPlayer().EntityId)
                                as EntityPlayer);
                        if (player != null)
                        {
                            if (player.IsAlive())
                            {
                                playerStats = MorteHelpers.getStats(player as EntityPlayerLocal);
                                if (powerLevel > 0)
                                {
                                    if (Input.GetKey(KeyCode.PageUp))
                                    {
                                        if (DateTime.Now > dtaNextClick)
                                        {
                                            exoHud = !exoHud;
                                            player.PlayOneShot("openHud");
                                            dtaNextClick = DateTime.Now.AddSeconds(2);
                                        }
                                    }
                                }
                                if (oldBag == null) oldBag = player.bag.GetSlots();                                
                                #region armor sets;              

                                //Debug.Log("BehaviourScript: PLAYER WEIGHT = " + player.GetWeight().ToString("#0.##"));
                                string wearing = "";
                                currentSets = new Dictionary<string, string[]>();
                                if (player.equipment.HasAnyItems())
                                {
                                    //Debug.Log("BehaviourScript: HAS GEAR");
                                    ItemValue[] equip = player.equipment.GetItems();
                                    if (equip != null)
                                    {
                                        //Debug.Log("BehaviourScript: FOUND ITEMS");
                                        for (int i = 0; i < equip.Length; i++)
                                            //foreach (ItemValue item in equip)
                                        {
                                            ItemValue item = equip[i];
                                            ItemClass itemClass = ItemClass.GetForId(item.type);
                                            string armorSet = "";
                                            int setNumber = 0;
                                            string setBuffs = "";
                                            string turnTo = "";
                                            if (itemClass != null)
                                            {
                                                if (itemClass.Properties.Values.ContainsKey("ArmorSet"))
                                                {
                                                    // is armorset
                                                    armorSet = itemClass.Properties.Values["ArmorSet"];
                                                    if (itemClass.Properties.Values.ContainsKey("SetBuff"))
                                                        setBuffs = itemClass.Properties.Values["SetBuff"];
                                                    if (itemClass.Properties.Values.ContainsKey("TurnTo"))
                                                        turnTo = itemClass.Properties.Values["TurnTo"];
                                                    if (itemClass.Properties.Values.ContainsKey("SetNum"))
                                                    {
                                                        if (
                                                            int.TryParse(itemClass.Properties.Values["SetNum"],
                                                                out setNumber) ==
                                                            false) setNumber = 0;
                                                    }
                                                    //Debug.Log(string.Format("Found {0} with set={1}, buffs={2}, Number={3}",
                                                    //    itemClass.localizedName, armorSet, setBuffs, setNumber));
                                                    if (armorSet != "" && setBuffs != "" && setNumber > 0)
                                                    {
                                                        // see if this specific set number exists
                                                        int currentNumber = 0;
                                                        if (currentSets.ContainsKey(armorSet))
                                                        {
                                                            if (
                                                                int.TryParse(currentSets[armorSet][2], out currentNumber) ==
                                                                false)
                                                                currentNumber = 0;
                                                            currentNumber++;
                                                            currentSets[armorSet][2] = currentNumber.ToString();

                                                        }
                                                        else
                                                        {
                                                            string[] valAux = new string[3];
                                                            valAux[0] = setBuffs;
                                                            valAux[1] = setNumber.ToString();
                                                            valAux[2] = "1";
                                                            currentSets.Add(armorSet, valAux);
                                                        }
                                                    }
                                                    if (turnTo != "")
                                                    {
                                                        // transforms into that object
                                                        ItemValue newItem = ItemClass.GetItem(turnTo);
                                                        player.equipment.SetSlotItem(i, newItem, true);
                                                    }
                                                }
                                                wearing += itemClass.localizedName + ", ";
                                            }
                                        }
                                    }
                                }
                                //if (wearing != "")
                                //{
                                //    Debug.Log("YOU ARE WEARING " + wearing);
                                //}
                                //else Debug.Log("YOU ARE WEARING NOTHING!");                                
                                if (!compareDicts())
                                {
                                    oldSets = new Dictionary<string, string[]>(currentSets);
                                    if (currentSets.Count > 0)
                                    {
                                        foreach (KeyValuePair<string, string[]> entry in currentSets)
                                        {
                                            string setName = entry.Key;
                                            string buffs = entry.Value[0];
                                            int numSet = Convert.ToInt32(entry.Value[1]);
                                            int numParts = Convert.ToInt32(entry.Value[2]);
                                            List<MultiBuffClassAction> BuffActions = null;
                                            if (debug)
                                                LogToConsole(
                                                    string.Format(
                                                        "YOU ARE WEARING {0} OF {1} PARTS OF THE {2} ARMOR SET",
                                                        numParts, numSet, setName));
                                            if (numParts > 0)
                                            {
                                                // apply buffs
                                                if (buffs != "")
                                                {
                                                    try
                                                    {
                                                        string[] buffList = buffs.Split(',');
                                                        if (buffList.Length > 0)
                                                        {
                                                            foreach (string buffItem in buffList)
                                                            {
                                                                MultiBuffClassAction multiBuffClassAction = null;
                                                                multiBuffClassAction =
                                                                    MultiBuffClassAction.NewAction(buffItem);
                                                                if (BuffActions == null)
                                                                    BuffActions = new List<MultiBuffClassAction>();
                                                                BuffActions.Add(multiBuffClassAction);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MultiBuffClassAction multiBuffClassAction = null;
                                                            multiBuffClassAction = MultiBuffClassAction.NewAction(buffs);
                                                            if (BuffActions == null)
                                                                BuffActions = new List<MultiBuffClassAction>();
                                                            BuffActions.Add(multiBuffClassAction);
                                                        }
                                                        // apply buff to player
                                                        if (BuffActions.Count > 0)
                                                        {
                                                            using (
                                                                List<MultiBuffClassAction>.Enumerator enumerator =
                                                                    BuffActions.GetEnumerator())
                                                            {
                                                                while (enumerator.MoveNext())
                                                                {                                                                    
                                                                    //Debug.Log("BehaviourScript: CHECKING buff " + enumerator.Current.Class.Name);
                                                                    bool buffOn = false;
                                                                    foreach (Buff buff in player.Stats.Buffs)
                                                                    {
                                                                        //if (debug) Debug.Log("BehaviourScript: Has buff " + buff.Name);
                                                                        if (buff.Name == enumerator.Current.Class.Name)
                                                                        {
                                                                            buffOn = true;
                                                                            break;
                                                                        }
                                                                    }
                                                                    if (numParts >= numSet)
                                                                    {
                                                                        if (!buffOn)
                                                                        {
                                                                            if (debug)
                                                                                LogToConsole(
                                                                                    "BehaviourScript: Applying armor buff " +
                                                                                    enumerator.Current.Class.Name);
                                                                            enumerator.Current.Execute(player.entityId,
                                                                                (EntityAlive) player,
                                                                                false,
                                                                                EnumBodyPartHit.None, (string) null);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (buffOn)
                                                                        {
                                                                            // debuff                                                            
                                                                            if (debug)
                                                                                LogToConsole(
                                                                                    "BehaviourScript: Removing armor buff " +
                                                                                    enumerator.Current.Class.Name);
                                                                            player.Stats.Debuff(
                                                                                enumerator.Current.Class.Id);
                                                                        }                                                                        
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        BuffActions = null;
                                                        LogToConsole(
                                                            "BehaviourScript: Invalid buffs in armor configuration: " +
                                                            ex.Message);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion;

                                bool exoAux = false;
                                bool exoSpeed = false;
                                bool exoGun = false;
                                int fluStage = 0;
                                int yawnLevel = 0;
                                bool cigar = false;
                                bool blunt = false;
                                bool isresting = false;
                                bool iswarming = false;
                                bool wellRested = true;
                                int lastPowerLevel = powerLevel;                               
                                //string buffsstr = "";
                                foreach (Buff buff in player.Stats.Buffs)
                                {
                                    // power armors                              
                                    if (buff.Name == "Exoskeleton")
                                    {
                                        exoAux = true;                                        
                                    }
                                    else if (buff.Name == "exoSpeed")
                                    {
                                        exoSpeed = true;
                                    }
                                    else if (buff.Name == "exoGun")
                                    {
                                        exoGun = true;
                                    }
                                    // diseases
                                    //if (debug) Debug.Log("BehaviourScript: Has buff " + buff.Name);                                        
                                    if (buff.Name == "flu")
                                    {
                                        fluStage = 1;
                                    }
                                    else if (buff.Name == "flu2")
                                    {
                                        fluStage = 2;
                                    }
                                    if (buff.Name == "sleepy") yawnLevel = 1;
                                    else if (buff.Name == "fatigued") yawnLevel = 2;
                                    else if (buff.Name == "sleepDeprived") yawnLevel = 3;
                                    if (buff.Name == "Cigar") cigar = true;
                                    else if (buff.Name == "Blunt") blunt = true;
                                    if (buff.Name == "sleepingBagEffect" || buff.Name == "sleepingBedEffect" ||
                                        buff.Name == "sleepingBigBedEffect") isresting = true;
                                    if (buff.Name == "wellrested") wellRested = true;
                                    if (buff.Name == "warmByFire") iswarming = true;
                                }
                                //Debug.Log(buffsstr);)
                                exoSuit = exoAux;
                                if (DateTime.Now > dtaNextDiseaseCheck)
                                {
                                    dtaNextDiseaseCheck = DateTime.Now.AddSeconds(5);

                                    #region Special diseases;
                                    
                                    string sound = "";

                                    #region Yawning;

                                    if (yawnLevel > 0)
                                    {
                                        if (player.GetRandom().Next(0, 100) < (5*yawnLevel))
                                        {
                                            if (player.IsMale) sound = "male_yawn";
                                            else sound = "female_yawn";
                                        }
                                    }

                                    #endregion;

                                    #region Flu Sounds;

                                    if (fluStage > 0 && sound == "")
                                    {

                                        if (player.GetRandom().Next(0, 100) < (20*fluStage))
                                        {
                                            int soundType = player.GetRandom().Next(0, 100);
                                            // play flu sounds randomly                                    
                                            if (fluStage == 1)
                                            {
                                                // flu1 only sniffs, and ocationally coughs
                                                if (soundType < 25)
                                                {
                                                    if (player.IsMale) sound = "male_cough";
                                                    else sound = "female_cough";
                                                }
                                                else if (soundType < 5)
                                                {
                                                    if (player.IsMale) sound = "male_sneeze";
                                                    else sound = "female_sneeze";
                                                }
                                                else
                                                {
                                                    if (player.IsMale) sound = "male_sniffle";
                                                    else sound = "female_sniffle";
                                                }
                                            }
                                            else
                                            {
                                                // flu2 coughs, sniffs and ocationally sneezes                                        
                                                if (soundType < 20)
                                                {
                                                    if (player.IsMale) sound = "male_sneeze";
                                                    else sound = "female_sneeze";
                                                }
                                                else if (soundType < 60)
                                                {
                                                    if (player.IsMale) sound = "male_cough";
                                                    else sound = "female_cough";
                                                }
                                                else
                                                {
                                                    if (player.IsMale) sound = "male_sniffle";
                                                    else sound = "female_sniffle";
                                                }
                                            }
                                        }
                                    }

                                    #endregion;

                                    #region Cigar sounds;

                                    if (cigar && sound == "")
                                    {
                                        if (player.GetRandom().Next(0, 100) < 50) sound = "smoke_cigar";
                                    }
                                    if (blunt && sound == "")
                                    {
                                        if (player.GetRandom().Next(0, 100) < 50) sound = "smoke_blunt";
                                    }
                                    if (sound != "")
                                    {
                                        player.PlayOneShot(sound);
                                    }

                                    #endregion;

                                    // add 0 or more buffs
                                    List<MultiBuffClassAction> BuffActionsD = null;
                                    float wetness = (Mathf.Clamp01(player.Stats.WaterLevel + 0.01f)*100);
                                    float coreTempPerc = player.Stats.CoreTemp.Value/player.Stats.CoreTemp.Max*100;
                                    float wellNessPerc = player.Stats.Wellness.Value/player.Stats.Wellness.Max*100;
                                    if (wetness > 30 && lastWetValue > 30 && fluStage == 0)
                                    {
                                        // flu chance will increase approximately 0.1 for each 30s that you're wet
                                        if (lastWetValue > 0 && fluModifier < 1.5F)
                                            fluModifier += 0.017F;
                                    }
                                    else if (wetness < 30)
                                    {
                                        fluModifier = 1;
                                    }
                                    lastWetValue = wetness;
                                    if (DateTime.Now > dtaNextSickCheck)
                                    {
                                        dtaNextSickCheck = DateTime.Now.AddSeconds(15);
                                        if (fluStage == 0 && !isresting && !iswarming)
                                        {
                                            #region Chance of Getting Flu;                                

                                            //Debug.Log(string.Format("WaterLevel={0}, Water={1}, CoreTemp={2}", Mathf.Clamp01(player.Stats.WaterLevel + 0.01f), player.Stats.WaterLevel, player.Stats.CoreTemp.Value));
                                            // flue, small change depending on wetness                                    
                                            if ((wetness > 20 || coreTempPerc < 10) && fluStage == 0)
                                            {
                                                // chance to get flu buff, depending on different stuff
                                                // if coretemp less then 41, it actually INCREASES the chance                                                                                
                                                float chance = wetness + (100 - coreTempPerc) - wellNessPerc;
                                                chance = 25*chance/100; // adjust the chance to be maxed at 15%
                                                if (chance <= 0) chance = 2; // never below 2%
                                                else if (chance > 25)
                                                    chance = 25; // never above 25%  
                                                chance = chance*fluModifier;
                                                //Debug.Log("Flu Chance = " + Math.Round(chance).ToString());
                                                if (player.GetRandom().Next(0, 100) < Math.Round(chance))
                                                {
                                                    string buffName = "flu";
                                                    // apply buff
                                                    MultiBuffClassAction multiBuffClassAction = null;
                                                    multiBuffClassAction = MultiBuffClassAction.NewAction(buffName);
                                                    if (BuffActionsD == null)
                                                        BuffActionsD = new List<MultiBuffClassAction>();
                                                    BuffActionsD.Add(multiBuffClassAction);
                                                }
                                            }

                                            #endregion;
                                        }
                                    }
                                    if (DateTime.Now >= dtaNextHealCheck)
                                    {
                                        dtaNextHealCheck = DateTime.Now.AddSeconds(10);
                                        if (fluStage > 0)
                                        {
                                            //Debug.Log(string.Format("WELLNESS:{0}, WELLNESSBASEMAX:{1}", player.Stats.Wellness.Value,
                                            //    player.Stats.Wellness.Max));
                                            //Debug.Log(string.Format("Wetness={0}, CoreTempPerc={1}, WellnessPerc={2}", wetness,
                                            //    coreTempPerc, wellNessPerc));

                                            #region Chances of healing flu;

                                            float chanceToHeal = 0;
                                            if (isresting)
                                            {
                                                // simple math - if you get too hot or too wet, resting wont heal you
                                                // but since it will decrease your temp, in time it will eventually hit
                                                chanceToHeal = wellNessPerc - (coreTempPerc/2) - wetness;
                                                if (fluStage > 1 && chanceToHeal > 3)
                                                    chanceToHeal = 3; // stage 2 has a residual chance of healing
                                                if (chanceToHeal > 40) chanceToHeal = 40;
                                                if (wellRested) chanceToHeal += 5; // 5% bonus for wellrested
                                                if (coreTempPerc > 45 && !iswarming)
                                                {
                                                    // resting will reduce temperature - the higher you get, the more it will do it
                                                    // if it gets higher then 80% it will actually start decreasing the chance again
                                                    float heatDownChance = (80 - (80 - coreTempPerc))/1.5F;
                                                    //Debug.Log(string.Format("CORETEMP:{0}, CoreTempBaseMax:{1}",
                                                    //    player.Stats.CoreTemp.Value,
                                                    //    player.Stats.CoreTemp.Max));
                                                    //Debug.Log(string.Format("heatDownChance={0}", heatDownChance));
                                                    if (player.GetRandom().Next(0, 100) < heatDownChance)
                                                    {
                                                        //Debug.Log("DECREASING CORETEMP");
                                                        player.Stats.CoreTemp.Value--;
                                                        if (coreTempPerc > 51) player.Stats.CoreTemp.Value--;
                                                        //Debug.Log(string.Format("CORETEMP:{0}, CoreTempBaseMax:{1}",
                                                        //player.Stats.CoreTemp.Value,
                                                        //player.Stats.CoreTemp.Max));
                                                    }
                                                }
                                            }
                                            else if (iswarming)
                                            {
                                                chanceToHeal = wellNessPerc - coreTempPerc - wetness;
                                                if (fluStage > 1 && chanceToHeal > 1) chanceToHeal = 1;
                                                if (chanceToHeal > 5) chanceToHeal = 5;
                                            }
                                            if (chanceToHeal > 0)
                                            {
                                                //Debug.Log(string.Format("chanceToHeal={0}", chanceToHeal));
                                                if (player.GetRandom().Next(0, 100) < Math.Round(chanceToHeal))
                                                {
                                                    // heal flue
                                                    if (fluStage == 1)
                                                        player.Stats.Debuff("flu");
                                                    else player.Stats.Debuff("flu2");
                                                    GameManager.Instance.ShowTooltip(
                                                        "You feel much better... There's nothing like a good rest to heal a flu!");
                                                }
                                            }

                                            #endregion;
                                        }
                                    }
                                    // apply buffs
                                    if (BuffActionsD != null)
                                    {
                                        if (BuffActionsD.Count > 0)
                                        {
                                            using (
                                                List<MultiBuffClassAction>.Enumerator enumerator =
                                                    BuffActionsD.GetEnumerator())
                                            {
                                                while (enumerator.MoveNext())
                                                    enumerator.Current.Execute(player.entityId, (EntityAlive) player,
                                                        false,
                                                        EnumBodyPartHit.None, (string) null);
                                            }
                                        }
                                    }

                                    #endregion;
                                }
                                if (exoSuit)
                                {
                                    bool powerUp = false;
                                    //Debug.Log("has exosuit");
                                    #region Custom stats;
                                    MultiBuffVariable multiBuffVariable1;
                                    //if (player.Stats.PI.TryGetValue("powerLevel", out multiBuffVariable1))
                                    if (playerStats.TryGetValue("powerLevel", out multiBuffVariable1))
                                    {
                                        powerLevel = Convert.ToInt32(multiBuffVariable1.Value);
                                    }
                                    else powerLevel = 0;
                                    if (playerStats.TryGetValue("radlevel", out multiBuffVariable1))
                                    {
                                        radlevel = Convert.ToInt32(multiBuffVariable1.Value);
                                    }
                                    else radlevel = 0;
                                    if (playerStats.TryGetValue("fatiguelevel", out multiBuffVariable1))
                                    {
                                        fatiguelevel = Convert.ToInt32(multiBuffVariable1.Value);
                                    }
                                    else fatiguelevel = 0;
                                    if (playerStats.TryGetValue("addiction", out multiBuffVariable1))
                                    {
                                        addiction = Convert.ToInt32(multiBuffVariable1.Value);
                                    }
                                    else addiction = 0;
                                    #endregion;
                                    if (powerLevel > 0)
                                    {
                                        // buff with exoSpeed or exoGun
                                        List<MultiBuffClassAction> BuffActionsD = null;
                                        // checks if its holding a exo gun / tool
                                        bool removeExogun = true;
                                        if (player.inventory.holdingItem != null)
                                        {
                                            if (player.inventory.holdingItem.Actions != null)
                                            {
                                                if (player.inventory.holdingItem.Actions.Length > 0)
                                                {
                                                    if (player.inventory.holdingItem.Actions[0] != null)
                                                    {
                                                        if (player.inventory.holdingItem.Actions[0].Properties.Contains(
                                                            "Require"))
                                                        {
                                                            if (
                                                                player.inventory.holdingItem.Actions[0].Properties
                                                                    .Values[
                                                                        "Require"] ==
                                                                "exoGun")
                                                            {
                                                                removeExogun = false;
                                                                if (!exoGun)
                                                                {
                                                                    //Debug.Log("BUFFING EXOGUN");
                                                                    powerUp = true;
                                                                    string buffName = "exoGun";
                                                                    // apply buff
                                                                    MultiBuffClassAction multiBuffClassAction = null;
                                                                    multiBuffClassAction =
                                                                        MultiBuffClassAction.NewAction(buffName);
                                                                    if (BuffActionsD == null)
                                                                        BuffActionsD = new List<MultiBuffClassAction>();
                                                                    BuffActionsD.Add(multiBuffClassAction);
                                                                    if (exoSpeed) player.Stats.Debuff("exoSpeed");
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (exoGun && removeExogun)
                                        {
                                            player.Stats.Debuff("exoGun");
                                            exoGun = false;
                                        }
                                        if (!exoSpeed && !exoGun)
                                        {
                                            powerUp = true;                            
                                            string buffName = "exoSpeed";
                                            // apply buff
                                            MultiBuffClassAction multiBuffClassAction = null;
                                            multiBuffClassAction = MultiBuffClassAction.NewAction(buffName);
                                            if (BuffActionsD == null)
                                                BuffActionsD = new List<MultiBuffClassAction>();
                                            BuffActionsD.Add(multiBuffClassAction);
                                        }
                                        if (BuffActionsD != null)
                                        {
                                            if (BuffActionsD.Count > 0)
                                            {
                                                using (
                                                    List<MultiBuffClassAction>.Enumerator enumerator =
                                                        BuffActionsD.GetEnumerator())
                                                {
                                                    while (enumerator.MoveNext())
                                                        enumerator.Current.Execute(player.entityId, (EntityAlive)player,
                                                            false,
                                                            EnumBodyPartHit.None, (string)null);
                                                }
                                            }
                                        }
                                        if (powerUp && lastPowerLevel == 0) player.PlayOneShot("powerUp");
                                    }
                                }
                                else powerLevel = 0;
                                if (powerLevel == 0)
                                {
                                    bool powerDown = false;
                                    if (exoSpeed)
                                    {
                                        player.Stats.Debuff("exoSpeed");
                                        powerDown = true;
                                    }
                                    if (exoGun)
                                    {
                                        player.Stats.Debuff("exoGun");
                                        powerDown = true;
                                    }
                                    if (exoHud) exoHud = false;
                                    if (powerDown) player.PlayOneShot("powerDown");
                                }
                                else
                                {
                                    
                                }                            
                                if (false)
                                {
                                    #region Food - UNDER DEVELOPMENT;

                                    if (DateTime.Now > dtaNextFoodCheck)
                                    {
                                        dtaNextFoodCheck = DateTime.Now.AddSeconds(30);
                                        foreach (ItemStack stack in player.bag.GetSlots())
                                        {
                                            if (stack != null)
                                            {
                                                if (!stack.IsEmpty())
                                                {
                                                    // if it is rawmeat
                                                    if (ItemClass.GetForId(stack.itemValue.type).GetItemName() ==
                                                        "rawMeat")
                                                    {
                                                        // if it was NOT initialized yet, I must initialize it
                                                        // if it WAS initialized but the number changed, from last time, i need to check the new quality                                            
                                                        stack.itemValue.Quality++;
                                                        if (debug)
                                                            Debug.Log(string.Format("rawmeat value = {0}",
                                                                stack.itemValue.Quality));
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    #endregion;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("BehaviourScript.Update (ERROR): (" + pos + ") " + ex.ToString());
            }
        }
    }

    bool compareDicts()
    {
        bool result = true;
        if (currentSets.Count != oldSets.Count)
        {
            if (debug) LogToConsole("BehaviourScript: sets different count");
            return false;
        }
        foreach (KeyValuePair<string, string[]> entry in currentSets)
        {
            if (oldSets.ContainsKey(entry.Key))
            {
                string[] valAux;
                if (oldSets.TryGetValue(entry.Key, out valAux))
                {
                    if (valAux[0] != entry.Value[0] ||
                        valAux[1] != entry.Value[1] ||
                        valAux[2] != entry.Value[2])
                    {
                        if (debug) LogToConsole("BehaviourScript: sets different value");
                        return false;
                    }
                }
                else
                {
                    if (debug) LogToConsole("BehaviourScript: sets different value 1");
                    return false;
                }
            }
            else
            {
                if (debug) LogToConsole("BehaviourScript: sets different keys");
                return false;
            }
        }
        if (result)
        {
            foreach (KeyValuePair<string, string[]> entry in oldSets)
            {
                if (currentSets.ContainsKey(entry.Key))
                {
                    string[] valAux;
                    if (currentSets.TryGetValue(entry.Key, out valAux))
                    {
                        if (valAux[0] != entry.Value[0] ||
                            valAux[1] != entry.Value[1] ||
                            valAux[2] != entry.Value[2])
                        {
                            if (debug) LogToConsole("BehaviourScript: sets different value OLD");
                            return false;
                        }
                    }
                    else
                    {
                        if (debug) LogToConsole("BehaviourScript: sets different value OLD 1");
                        return false;
                    }
                }
                else
                {
                    if (debug) LogToConsole("BehaviourScript: sets different keys OLD");
                    return false;
                }
            }
        }
        return result;
    }

    public static void OnCustomGUI()
    {
        // modversion
        //var rightStyle = GUI.skin.GetStyle("Label");
        //rightStyle.alignment = TextAnchor.MiddleRight;       
        //GUIStyle versionSkin = new GUIStyle(GUI.skin.label);
        //versionSkin.font = Font.CreateDynamicFontFromOSFont("Impact", 12);
        //versionSkin.fontStyle = FontStyle.Bold;
        //GUI.Label(new Rect(Screen.width - 150, 30, 150, 20), MorteHelpers.modVersion, versionSkin);
        if (GameManager.Instance.gameStateManager.IsGameStarted() && GUIWindowManager.IsHUDEnabled())
        {
            GUIStyle versionSkin = new GUIStyle(GUI.skin.label);
            versionSkin.normal.textColor = Color.grey;
            GUI.Label(new Rect(Screen.width - 250, 5, 150, 20), MorteHelpers.modVersion, versionSkin);
            if (exoSuit)
            {
                // if wearing a exosuit, it will show the amount of power left       
                GUI.depth = 1;
                Texture2D auxTexture;
                if (hudTextures.TryGetValue("hudWindow", out auxTexture))
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), auxTexture,
                        ScaleMode.StretchToFill, true);
                if (hudTextures.TryGetValue("hudL", out auxTexture))
                    GUI.DrawTexture(new Rect(0, Screen.height - auxTexture.height + 4, auxTexture.width, auxTexture.height), auxTexture,
                        ScaleMode.StretchToFill, true);
                if (hudTextures.TryGetValue("hudR", out auxTexture))
                    GUI.DrawTexture(new Rect(Screen.width - auxTexture.width, Screen.height - auxTexture.height, auxTexture.width, auxTexture.height), auxTexture,
                        ScaleMode.StretchToFill, true);
                if (hudTextures.TryGetValue("hudToolBelt", out auxTexture))
                    GUI.DrawTexture(
                        new Rect((Screen.width/2) - (auxTexture.width/2) - 5, Screen.height - auxTexture.height,
                            auxTexture.width, auxTexture.height), auxTexture,
                        ScaleMode.StretchToFill, true);
                if (hudTextures.TryGetValue("hudRadio", out auxTexture))
                    GUI.DrawTexture(new Rect(10, 30, auxTexture.width, auxTexture.height), auxTexture,
                        ScaleMode.StretchToFill, true);
                //GUI.Label(new Rect(Screen.width - 210 - 50, 55, 50, 20), "POWER:");
                float powerPerc = (float) ((float) powerLevel/(float) maxPower);
                if (hudTextures.TryGetValue("bgImage", out auxTexture))
                {
                    Rect infoAreaBg = new Rect(Screen.width - healthBarLength - 10, 30, healthBarLength, auxTexture.height);
                    GUILayout.BeginArea(infoAreaBg);
                    GUI.DrawTexture(new Rect(0, 0, healthBarLength, auxTexture.height), auxTexture);
                    if (hudTextures.TryGetValue("fgImage", out auxTexture))
                        GUI.DrawTextureWithTexCoords(
                            new Rect(0, 0, (float) powerPerc*(float) (healthBarLength), auxTexture.height), auxTexture,
                            new Rect(0f, 0f, (float) powerPerc, 1f));
                    GUILayout.EndArea();
                }
                if (exoHud)
                {
                    // open a GUI window with the exohud
                    if (hudTextures.TryGetValue("hudOverview", out auxTexture))
                    {
                        Rect overViewRec = new Rect((Screen.width/2) - (auxTexture.width/2),
                            (Screen.height/2) - (auxTexture.height/2), auxTexture.width, auxTexture.height);
                        GUILayout.BeginArea(overViewRec);
                        GUI.DrawTexture(new Rect(0, 0, auxTexture.width, auxTexture.height), auxTexture);
                        GUIStyle textSkin = new GUIStyle(GUI.skin.label);
                        textSkin.normal.textColor = Color.red;
                        textSkin.alignment = TextAnchor.MiddleCenter;
                        textSkin.fontStyle = FontStyle.Bold;
                        GUI.Label(new Rect(518, 65, 84, 20), "No upgrade", textSkin);
                        GUI.Label(new Rect(86, 540, 80, 20), "0 left", textSkin);
                        // draw the remaining stat bars
                        if (hudTextures.TryGetValue("RadiationEmpty", out auxTexture))
                        {
                            float auxPerc = (float)((float)radlevel / (float)maxRad);
                            if (auxPerc > 1) auxPerc = 1;
                            Rect infoAreaBg = new Rect(0, 46, auxTexture.width, auxTexture.height);
                            GUILayout.BeginArea(infoAreaBg);
                            GUI.DrawTexture(new Rect(0, 0, auxTexture.width, auxTexture.height), auxTexture);
                            if (hudTextures.TryGetValue("RadiationFull", out auxTexture))
                                GUI.DrawTextureWithTexCoords(
                                    new Rect(0, 0, (float)auxPerc * (float)(auxTexture.width), auxTexture.height), auxTexture,
                                    new Rect(0f, 0f, (float)auxPerc, 1f));
                            GUILayout.EndArea();
                        }
                        if (hudTextures.TryGetValue("FatigueEmpty", out auxTexture))
                        {
                            float auxPerc = (float)((float)fatiguelevel / (float)maxFatigue);
                            if (auxPerc > 1) auxPerc = 1;
                            Rect infoAreaBg = new Rect(55, 255, auxTexture.width, auxTexture.height);
                            GUILayout.BeginArea(infoAreaBg);
                            GUI.DrawTexture(new Rect(0, 0, auxTexture.width, auxTexture.height), auxTexture);
                            if (hudTextures.TryGetValue("FatigueFull", out auxTexture))
                                GUI.DrawTextureWithTexCoords(
                                    new Rect(0, 0, (float)auxPerc * (float)(auxTexture.width), auxTexture.height), auxTexture,
                                    new Rect(0f, 0f, (float)auxPerc, 1f));
                            GUILayout.EndArea();
                        }
                        if (hudTextures.TryGetValue("AddictionEmpty", out auxTexture))
                        {
                            float auxPerc = (float)((float)addiction / (float)maxAddiction);
                            if (auxPerc > 1) auxPerc = 1;
                            Rect infoAreaBg = new Rect(432, 244, auxTexture.width, auxTexture.height);
                            GUILayout.BeginArea(infoAreaBg);
                            GUI.DrawTexture(new Rect(0, 0, auxTexture.width, auxTexture.height), auxTexture);
                            if (hudTextures.TryGetValue("AddictionFull", out auxTexture))
                                GUI.DrawTextureWithTexCoords(
                                    new Rect(0, 0, (float)auxPerc * (float)(auxTexture.width), auxTexture.height), auxTexture,
                                    new Rect(0f, 0f, (float)auxPerc, 1f));
                            GUILayout.EndArea();
                        }
                        if (hudTextures.TryGetValue("SanityBarEmpty", out auxTexture))
                        {
                            float auxPerc = (float)((float)sanity / (float)maxSanity);
                            if (auxPerc > 1) auxPerc = 1;
                            auxPerc = 1;
                            Rect infoAreaBg = new Rect(411, 476, auxTexture.width, auxTexture.height);
                            GUILayout.BeginArea(infoAreaBg);
                            GUI.DrawTexture(new Rect(0, 0, auxTexture.width, auxTexture.height), auxTexture);
                            if (hudTextures.TryGetValue("SanityBarFull", out auxTexture))
                                GUI.DrawTextureWithTexCoords(
                                    new Rect(0, 0, (float)auxPerc * (float)(auxTexture.width), auxTexture.height), auxTexture,
                                    new Rect(0f, 0f, (float)auxPerc, 1f));
                            GUILayout.EndArea();
                        }
                        GUILayout.EndArea();
                    }
                }
            }
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
}

