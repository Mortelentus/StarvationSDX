using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using SDX.Core;
using SDX.Compiler;
using SDX.Payload;
using Mono.Cecil;
using Mono.Cecil.Cil;
using XMLData;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using ModManager = SDX.Core.ModManager;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

public class StarvationPatcher : IPatcherMod
{
    public static void CopyPrefabs()
    {
        Console.WriteLine(" == Mortelentus Copying Custom Prefabs == ");
        string path1 = GlobalVariables.Parse("${GameDir}\\data\\Prefabs");
        foreach (string filePath in SDX.Core.ModManager.FindFilesInMods("Prefabs", "*.*", false))
            IOUtils.CopyFileToDir(filePath, path1);
        Console.WriteLine(" == Mortelentus Finished Copying Prefabs == ");        
        Console.WriteLine(" == Cheating the System == ");
        string blockFile = Path.Combine(SDX.Core.ModManager.Target.BackupDirectory, "Data/Config/blocks.xml");
        // add a placeover block at position 2047
        var txtLines = File.ReadAllLines(blockFile).ToList();
        if (txtLines.IndexOf("<!--STARVATION AUX-->") <= 0)
        {            
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "<!--STARVATION AUX-->");
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "<block id=\"2047\" name=\"auxSDX\">");
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "<property name=\"Material\" value =\"air\" />");
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "<property name=\"Shape\" value =\"Invisible\" />");
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "<property name=\"Texture\" value =\"250\" />");
            txtLines.Insert(txtLines.IndexOf("</blocks>"), "</block>");
            string[] lines = txtLines.ToArray();
            File.WriteAllLines(blockFile, lines);
        }
        foreach (string filePath in SDX.Core.ModManager.FindFilesInMods("Scripts", "*.*", false))
        {
            if (filePath.Contains("ClientHelper"))
            {
                if (SDX.Core.ModManager.Target.Name.Contains("Server"))
                    File.Move(filePath, Path.ChangeExtension(filePath, ".cs_"));// rename to cs_
                else File.Move(filePath, Path.ChangeExtension(filePath, ".cs"));// rename to cs
            }
            if (filePath.Contains("ServerHelper"))
            {
                if (SDX.Core.ModManager.Target.Name.Contains("Server"))
                    File.Move(filePath, Path.ChangeExtension(filePath, ".cs"));// rename to cs
                else File.Move(filePath, Path.ChangeExtension(filePath, ".cs_"));// rename to cs_
            }
        }
        Console.WriteLine(" == Finished Cheating the System == ");
    }

    public static void CopyUI()
    {
        Console.WriteLine(" == Mortelentus Copying Custom UI elements == ");
        string path1 = GlobalVariables.Parse("${GameDir}\\data\\Hud");
        if (!Directory.Exists(path1))
            Directory.CreateDirectory(path1);
        foreach (string filePath in SDX.Core.ModManager.FindFilesInMods("Hud", "*.png", false))
            IOUtils.CopyFileToDir(filePath, path1);
        Console.WriteLine(" == Mortelentus Finished Copying Custom UI elements == ");      
    }

    public bool Patch(ModuleDefinition module)
	{
		Console.WriteLine(" == Mortelentus Patch Tasks Running == ");
	    ChangeFieldPermission(module);
	    CopyPrefabs();
        CopyUI();
        return true;
	}

    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        Console.WriteLine(" == MORTELENTUS CHAT HOOK == ");
        HookMethods(gameModule, modModule);
        return true;
    }

    private void HookMethods(ModuleDefinition module, ModuleDefinition mod)
    {

        var manager = mod.Types.First(d => d.Name == "MorteHelpers");
        var linkMethod = module.Import(manager.Methods.First(d => d.Name == "MessageHook"));
        var worldLoad = module.Types.First(d => d.Name == "GameManager").Methods.First(d => d.Name == "GameMessageServer");
        var pro = worldLoad.Body.GetILProcessor();
        // remove all other instructions
        pro.Body.Instructions.Clear();
        pro.Emit(OpCodes.Ldarg_1);
        pro.Emit(OpCodes.Ldarg_2);
        pro.Emit(OpCodes.Ldarg_3);
        pro.Emit(OpCodes.Ldarg_S, worldLoad.Parameters[3]);
        pro.Emit(OpCodes.Ldarg_S, worldLoad.Parameters[4]);
        pro.Emit(OpCodes.Ldarg_S, worldLoad.Parameters[5]);
        pro.Emit(OpCodes.Ldarg_S, worldLoad.Parameters[6]);
        pro.Emit(OpCodes.Call, linkMethod);
        pro.Emit(OpCodes.Ret);
        // locks assembly receipts when player closes a workstation Or assembly window
        worldLoad = module.Types.First(d => d.Name == "XUiC_AssembleWindow").Methods.First(d => d.Name == "OnClose");
        manager = mod.Types.First(d => d.Name == "LockReceips");
        linkMethod = module.Import(manager.Methods.First(d => d.Name == "LockAll"));
        pro = worldLoad.Body.GetILProcessor();
        pro.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, linkMethod));
        worldLoad = module.Types.First(d => d.Name == "XUiC_WorkstationWindowGroup").Methods.First(d => d.Name == "OnClose");
        manager = mod.Types.First(d => d.Name == "LockReceips");
        linkMethod = module.Import(manager.Methods.First(d => d.Name == "LockAll"));
        pro = worldLoad.Body.GetILProcessor();
        pro.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, linkMethod));
        // inject function to use exosuit sound
        manager = mod.Types.First(d => d.Name == "MorteHelpers");
        linkMethod = module.Import(manager.Methods.First(d => d.Name == "checkSound"));
        worldLoad = module.Types.First(d => d.Name == "EntityAlive").Methods.First(d => d.Name == "internalPlayStepSound");
        pro = worldLoad.Body.GetILProcessor();
        pro.Body.Instructions.Insert(198, Instruction.Create(OpCodes.Nop));
        pro.Body.Instructions.Insert(199, Instruction.Create(OpCodes.Ldloc_2));
        pro.Body.Instructions.Insert(200, Instruction.Create(OpCodes.Ldarg_0));
        pro.Body.Instructions.Insert(201, Instruction.Create(OpCodes.Call, linkMethod));
        pro.Body.Instructions.Insert(202, Instruction.Create(OpCodes.Stloc_2));
        // inject function to grant radiation imunity with a powered on exo suit
        linkMethod = module.Import(manager.Methods.First(d => d.Name == "checkPowerOn"));
        worldLoad = module.Types.First(d => d.Name == "EntityAlive").Methods.First(d => d.Name == "isRadiationSensitive");
        pro = worldLoad.Body.GetILProcessor();
        pro.Body.Instructions.Clear();
        pro.Emit(OpCodes.Ldarg_0);
        pro.Emit(OpCodes.Call, linkMethod);
        pro.Emit(OpCodes.Ret);
        // inject our custom UI functions, since we want to access custom properties
        manager = mod.Types.First(d => d.Name == "BehaviourScript");
        linkMethod = module.Import(manager.Methods.First(d => d.Name == "OnCustomGUI"));
        //worldLoad = module.Types.First(d => d.Name == "GUIWindowManager").Methods.First(d => d.Name == "OnGUI");
        worldLoad = module.Types.First(d => d.Name == "EntityPlayerLocal").Methods.First(d => d.Name == "OnHUD");
        pro = worldLoad.Body.GetILProcessor();
        pro.Body.Instructions.Insert(92, Instruction.Create(OpCodes.Call, linkMethod));
        // injecting new filds to ItemStack        
        //TypeDefinition modinj = module.Types.First(d => d.Name == "ItemStack");
        //var fieldDef = new FieldDefinition(
        //    "spoilTime",
        //    FieldAttributes.Public,
        //    modinj.Module.Import(typeof (ulong)));
        //modinj.Fields.Add(fieldDef);
    }

    private void ChangeFieldPermission(ModuleDefinition module)
    {
        Console.WriteLine(" == Changing ConnectionManager permissions == ");
        var gm = module.Types.First(d => d.Name == "GameManager");
        var field = gm.Fields.First(d => d.FieldType.Name == "ConnectionManager");        
        SetFieldToPublic(field);
        Console.WriteLine(" == Changing TyleEntityList permissions == ");
        if (SDX.Core.ModManager.Target.Name.Contains("Server")) // public Dictionary<TileEntity, int> 
        {
            field = gm.Fields.First(d => d.Name == "BS");
            SetFieldToPublic(field);
        }
        else
        {
            field = gm.Fields.First(d => d.Name == "EJ");
            SetFieldToPublic(field);
        }
        Console.WriteLine(" == Changing Player Status permissions == ");
        // player custom stat properties
        gm = module.Types.First(d => d.Name == "EntityStats");
        //field = gm.Fields.First(d => d.Name == "PI");
        //field = gm.Fields.First(d => d.FieldType.Name.Contains("Dictionary"));
        if (!SDX.Core.ModManager.Target.Name.Contains("Server")) // private Dictionary<string, MultiBuffVariable>
            field = gm.Fields.First(d => d.Name == "YJ");
        else field = gm.Fields.First(d => d.Name == "RK");
        SetFieldToPublic(field);

        // ClientHelper.cs, ServerHelper.cs, mortehelpers.getstats in custombehaviours.
    }

    private void SetFieldToPublic(FieldDefinition field)
    {

        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;
    }
}