using Newtonsoft.Json;
using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using Random = System.Random;

//code taken and modified from: https://github.com/Mpstark/LessPilotInjuries
//code taken and modified from: https://github.com/RealityMachina/Battletech_LessHeadInjuries

public static class FewerHeadInjuries
{
    //public static float ArmorHeadHitIgnoreDamageBelow = 10;
    //public static float StructHeadHitIgnoreDamageBelow = 5;
    internal static Settings Settings;
    public static HashSet<Pilot> IgnoreNextHeadHit = new HashSet<Pilot>();

    public static void Init(string settings)
    {
        var harmony = HarmonyInstance.Create("com.btmodders.FewerHeadInjuries");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        try
        {
            Settings = JsonConvert.DeserializeObject<Settings>(settings);
        }
        catch (Exception)
        {
            Settings = new Settings();
        }
    }

    public static void Reset() => IgnoreNextHeadHit = new HashSet<Pilot>();
}

public class Settings
{
    public bool EnableDebug = false;
}

[HarmonyPatch(typeof(SimGameState), "_OnFirstPlayInit")]
public static class SimGameState__OnFirstPlayInit_Patch
{
    public static void Postfix(SimGameState __instance)
    {
        // change a custom difficulty setting into a tag 
        var sim = UnityGameInstance.BattleTechGame.Simulation;

        if (sim.Constants.Story.MaximumDebt == 42)
            sim.Constants.Story.MaximumDebt = 0;
        else
            sim.CompanyTags.Add("FewerHeadInjuriesDisabled");
    }
}

[HarmonyPatch(typeof(BattleTech.GameInstance), "LaunchContract", new Type[] {typeof(Contract), typeof(string)})]
public static class PatchLaunchContract
{
    static void Postfix()
    {
        // reset on new contracts
        FewerHeadInjuries.Reset();
    }
}

[HarmonyPatch(typeof(BattleTech.Mech), "DamageLocation")]
public static class PatchDamageLocation
{
    static void Prefix(Mech __instance, ArmorLocation aLoc, float totalDamage)
    {
        if (aLoc == ArmorLocation.Head)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            if (sim.CompanyTags.Contains("FewerHeadInjuriesDisabled"))
                return;

            var currentArmor = __instance.GetCurrentArmor(aLoc);
            var maxArmor = __instance.GetMaxArmor(aLoc);
            if (__instance.GetCurrentArmor(aLoc) == 0)
                return;

            if (currentArmor - totalDamage <= 0)
                return;

            float Modifier = UnityGameInstance.BattleTechGame.Simulation.GetCareerModeOverallDifficultyMod();
            var rng = new Random().Next(1, 101);
            if (rng >= currentArmor / maxArmor * 100 * Modifier)
                FewerHeadInjuries.IgnoreNextHeadHit.Add(__instance.pilot);
        }
    }
}

[HarmonyPatch(typeof(BattleTech.Pilot), "SetNeedsInjury")]
public static class PatchSetNeedsInjury
{
    /// <summary>
    /// true implies injury occurs
    /// </summary>
    static bool Prefix(Pilot __instance, InjuryReason reason)
    {
        var sim = UnityGameInstance.BattleTechGame.Simulation;
        if (sim.CompanyTags.Contains("FewerHeadInjuriesDisabled"))
            return true;

        if (reason == InjuryReason.HeadHit && FewerHeadInjuries.IgnoreNextHeadHit.Contains(__instance))
        {
            FewerHeadInjuries.IgnoreNextHeadHit.Remove(__instance);
            return false;
        }

        return true;
    }
}
