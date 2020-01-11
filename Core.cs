using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using Harmony;
using Newtonsoft.Json;
using static FewerHeadInjuries.FewerHeadInjuries;

// ReSharper disable InconsistentNaming

namespace FewerHeadInjuries
{

    //code taken and modified from: https://github.com/Mpstark/LessPilotInjuries
    //code taken and modified from: https://github.com/RealityMachina/Battletech_LessHeadInjuries
    public static class FewerHeadInjuries
    {
        internal static Settings Settings;
        public static HashSet<Pilot> IgnoreNextHeadHit = new HashSet<Pilot>();

        public static void Init(string settings)
        {
            try
            {
                Settings = JsonConvert.DeserializeObject<Settings>(settings);
            }
            catch (Exception)
            {
                Settings = new Settings();
            }

            var harmony = HarmonyInstance.Create("com.btmodders.FewerHeadInjuries");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Log(object input)
        {
            //FileLog.Log($"[FewerHeadInjuries] {input}");
        }

        public static void Reset() => IgnoreNextHeadHit = new HashSet<Pilot>();
    }


    public class Settings
    {
        public bool EnableDebug = false;
    }

//[HarmonyPatch(typeof(SimGameState), "_OnFirstPlayInit")]
//public static class SimGameState__OnFirstPlayInit_Patch
//{
//    public static void Prefix(SimGameState __instance, ref float __state)
//    {
//        //var fieldInfo =  __instance.Constants.Story.CompanyEventStartingChance;
//        __state = __instance.Constants.Story.CompanyEventStartingChance;
//    }
//
//    public static void Postfix(SimGameState __instance, float __state)
//    {
//        Log("Init: " + __instance.Constants.Story.CompanyEventStartingChance);
//        // change a custom difficulty setting into a tag
//        if (__instance.Constants.Story.CompanyEventStartingChance == 42)
//        {
//            __instance.CompanyTags.Add("FewerHeadInjuriesDisabled");
//            __instance.Constants.Story.CompanyEventStartingChance = __state;
//        }
//    }
//}

    [HarmonyPatch(typeof(GameInstance), "LaunchContract", typeof(Contract), typeof(string))]
    public static class GameInstance_LaunchContract_Patch
    {
        // reset on new contracts
        public static void Postfix() => Reset();
    }

    [HarmonyPatch(typeof(Mech), "DamageLocation")]
    public static class Mech_DamageLocation_Patch
    {
        static void Prefix(Mech __instance, ArmorLocation aLoc, float totalArmorDamage, float directStructureDamage)
        {
            if (aLoc == ArmorLocation.Head)
            {
                //var sim = UnityGameInstance.BattleTechGame.Simulation;
                //if (sim.CompanyTags.Contains("FewerHeadInjuriesDisabled"))
                //    return;

                var currentArmor = __instance.GetCurrentArmor(aLoc);
                var maxArmor = __instance.GetMaxArmor(aLoc);
                if (Math.Abs(__instance.GetCurrentArmor(aLoc)) < float.Epsilon)
                {
                    return;
                }

                if (currentArmor - totalArmorDamage + directStructureDamage <= 0)
                {
                    return;
                }

                var Modifier =
                    UnityGameInstance.BattleTechGame.Simulation.GetCareerModeOverallDifficultyMod();
                var rng = new Random().Next(1, 101);
                if (rng <= currentArmor / maxArmor * 100 * Modifier)
                {
                    IgnoreNextHeadHit.Add(__instance.pilot);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pilot), "SetNeedsInjury")]
    public static class Pilot_SetNeedsInjury_Patch
    {
        // true implies injury occurs
        static bool Prefix(Pilot __instance, InjuryReason reason)
        {
            try
            {
                if (reason != InjuryReason.SideTorsoDestroyed &&
                    reason == InjuryReason.HeadHit && IgnoreNextHeadHit.Contains(__instance))
                {
                    //var sim = UnityGameInstance.BattleTechGame.Simulation;
                    //if (sim.CompanyTags.Contains("FewerHeadInjuriesDisabled"))
                    //    return true;

                    UnityGameInstance.BattleTechGame.Combat.MessageCenter.PublishMessage(
                        new AddSequenceToStackMessage(
                            new ShowActorInfoSequence(
                                __instance.ParentActor,
                                "INJURY AVOIDED",
                                FloatieMessage.MessageNature.Inspiration,
                                false)
                        )
                    );
                    IgnoreNextHeadHit.Remove(__instance);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            return true;
        }
    }
}
