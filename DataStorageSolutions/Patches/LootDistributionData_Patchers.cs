﻿using DataStorageSolutions.Configuration;
using FCSCommon.Utilities;
using HarmonyLib;

namespace DataStorageSolutions.Patches
{
    internal class LootDistributionData_Patchers
    {
        [HarmonyPatch(typeof(LootDistributionData))]
        [HarmonyPatch("Initialize")]
        public static class LootDistributionDataPatch
        {
            [HarmonyPostfix]
            public static void Postfix(LootDistributionData __instance)
            {
                QuickLogger.Debug($"Initialize Loot Distribution Data: {__instance}");
                Mod.LootDistributionData = __instance;
            }
        }
    }
}
