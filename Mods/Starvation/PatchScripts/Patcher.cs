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

	public bool Patch(ModuleDefinition module)
	{
		Console.WriteLine(" == Mortelentus Patch Tasks Running == ");

	    ChangeFieldPermission(module);
	    CopyPrefabs();
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
    }

    private void ChangeFieldPermission(ModuleDefinition module)
    {

        var gm = module.Types.First(d => d.Name == "GameManager");
        var field = gm.Fields.First(d => d.FieldType.Name == "ConnectionManager");        
        SetFieldToPublic(field);
        field = gm.Fields.First(d => d.Name == "BA");
        SetFieldToPublic(field);
        if (SDX.Core.ModManager.Target.Name.Contains("Server"))
        {
            field = gm.Fields.First(d => d.Name == "BT");
            SetFieldToPublic(field);
        }
    }

    private void SetFieldToPublic(FieldDefinition field)
    {

        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;
    }
}