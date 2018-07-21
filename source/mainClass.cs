using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using Random = System.Random;

//code taken and modified from: https://github.com/Mpstark/LessPilotInjuries
//code taken and modified from: https://github.com/RealityMachina/Battletech_LessHeadInjuries
namespace FewerHeadInjuries
{
    public static class FewerHeadInjuries
    {
        //public static float ArmorHeadHitIgnoreDamageBelow = 10;
        //public static float StructHeadHitIgnoreDamageBelow = 5;
        public static HashSet<Pilot> IgnoreNextHeadHit = new HashSet<Pilot>();

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("com.gnivler.FewerHeadInjuries");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Reset()
        {
            IgnoreNextHeadHit = new HashSet<Pilot>();
        }
    }

    [HarmonyPatch(typeof(BattleTech.GameInstance), "LaunchContract", new Type[] { typeof(Contract), typeof(string) })]
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
                var currentArmor = __instance.GetCurrentArmor(aLoc);
                var maxArmor = __instance.GetMaxArmor(aLoc);
                if (__instance.GetCurrentArmor(aLoc) == 0)
                {
                    return;
                }

                if (currentArmor - totalDamage <= 0)
                {
                    return;
                }

                var rng = new Random().Next(1, 101);
                if (rng >= (currentArmor / maxArmor) * 100)
                {
                    FewerHeadInjuries.IgnoreNextHeadHit.Add(__instance.pilot);
                }
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
            if (reason == InjuryReason.HeadHit && FewerHeadInjuries.IgnoreNextHeadHit.Contains(__instance))
            {
                FewerHeadInjuries.IgnoreNextHeadHit.Remove(__instance);
                return false;
            }
            return true;
        }
    }
}
