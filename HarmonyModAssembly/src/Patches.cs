using System.Collections.Generic;
using System.Reflection.Emit;
using System.IO;
using System.Linq;
using Assets.Scripts.Mods;
using Assets.Scripts.Services;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarmonyModAssembly
{
    public static class Patcher
    {
        public static string ModInfoFile = "modInfo.json";
        public static Texture HarmonyTexture;

        public static bool ToggleModInfo()
        {
            if (ModInfoFile == "modInfo.json")
            {
                ModInfoFile = "modInfo_Harmony.json";
                return false;
            }
            ModInfoFile = "modInfo.json";
            return true;
        }

        public static void Patch(Texture _HarmonyTexture)
        {
            HarmonyTexture = _HarmonyTexture;
            new Harmony("qkrisi.harmonymod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(ModManager), "GetModInfoFromPath")]
    [HarmonyPriority(Priority.First)]
    public static class ModInfoPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && (string) instruction.operand == "modInfo.json")
                    yield return new CodeInstruction(OpCodes.Ldsfld,
                        typeof(Patcher).GetField("ModInfoFile", AccessTools.all));
                else yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(ManageModsScreen), "OnEnable")]
    [HarmonyPriority(Priority.First)]
    public static class WorkshopPatch
    {
        public static bool Prefix(ManageModsScreen __instance, ref List<ModInfo> ___installedMods, ref List<ModInfo> ___fullListOfMods, ref List<ModInfo> ___allSubscribedMods)
        {
            ModManager.Instance.ReloadModMetaData();
            ___installedMods = ModManager.Instance.InstalledModInfos.Values
                .Where(info => File.Exists(Path.Combine(info.FilePath, Patcher.ModInfoFile))).ToList();
            ___allSubscribedMods = AbstractServices.Instance.GetSubscribedMods();
            ___fullListOfMods = Patcher.ModInfoFile == "modInfo.json"
                ? ___installedMods.Union(___allSubscribedMods).ToList()
                : ___installedMods;
            ___fullListOfMods.Sort((ModInfo a, ModInfo b) => a.Title.CompareTo(b.Title));
            Traverse.Create(__instance).Method("ShowMods").GetValue();
            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerManualInstructionScreen), "HandleContinue")]
    [HarmonyPriority(Priority.First)]
    public static class ContinueButtonPatch
    {
        public static bool Prefix(ModManagerManualInstructionScreen __instance, out bool __result)
        {
            bool cont = Patcher.ToggleModInfo();
            __result = false;
            if (cont)
                return true;
            __instance.ReleaseSelection();
            SceneManager.Instance.EnterModManagerState();
            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerState), "ShouldShowManualInstructions")]
    [HarmonyPriority(Priority.First)]
    public static class InstructionPatch
    {
        public static bool Prefix(out bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(MenuScreen), "EnterScreenComplete")]
    [HarmonyPriority(Priority.First)]
    public static class ChangeButtonText
    {
        public static void Postfix(MenuScreen __instance)
        {
            if (Patcher.ModInfoFile == "modInfo.json")
            {
                if(__instance is ModManagerManualInstructionScreen screen)
                    screen.ContinueButton.GetComponentInChildren<TextMeshProUGUI>(true)
                        .text = "Manage Harmony mods";
            }
            else if (__instance is ModManagerMainMenuScreen MenuScreen)
            {
                MenuScreen.SteamWorkshopBrowserButton.gameObject.SetActive(false);
                MenuScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Harmony Mod Manager";
                var image = MenuScreen.GetComponentInChildren<RawImage>();
                image.texture = Patcher.HarmonyTexture;
                image.transform.localScale = new Vector3(image.transform.localScale.x + 1,
                    image.transform.localScale.y, image.transform.localScale.z);
                MenuScreen.ManageModsButton.GetComponentInChildren<TextMeshProUGUI>(true).text =
                    "Manage Harmony mods";
            }
            else if(__instance is ManageModsScreen ManagerScreen)
                ManagerScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Manage installed Harmony mods";
        }
    }
}