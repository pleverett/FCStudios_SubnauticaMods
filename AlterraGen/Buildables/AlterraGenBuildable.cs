﻿using System;
using System.Collections.Generic;
using System.IO;
using AlterraGen.Configuration;
using AlterraGen.Mono;
using FCSCommon.Extensions;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using FCSTechFabricator.Components;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;

namespace AlterraGen.Buildables
{
    internal partial class AlterraGenBuildable : Buildable
    {
        public AlterraGenBuildable() : base(Mod.ModClassName, Mod.ModFriendlyName, Mod.ModDescription)
        {
            OnFinishedPatching += AdditionalPatching;
        }

        public override GameObject GetGameObject()
        {
            try
            {
                if (GetPrefabs())
                {
                    var prefab = GameObject.Instantiate(Prefab);

                    //Scale the object
                    prefab.transform.localScale += new Vector3(0.24f, 0.24f, 0.24f);

                    var size = new Vector3(2.493512f, 1.875936f, 1.439421f);
                    var center = new Vector3(0.07963049f, 1.088284f,0f);

                    GameObjectHelpers.AddConstructableBounds(prefab, size, center);

                    var model = prefab.FindChild("model");

                    //========== Allows the building animation and material colors ==========// 
                    Shader shader = Shader.Find("MarmosetUBER");
                    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
                    SkyApplier skyApplier = prefab.EnsureComponent<SkyApplier>();
                    skyApplier.renderers = renderers;
                    skyApplier.anchorSky = Skies.Auto;
                    //========== Allows the building animation and material colors ==========// 

                    // Add large world entity ALLOWS YOU TO SAVE ON TERRAIN
                    var lwe = prefab.AddComponent<LargeWorldEntity>();
                    lwe.cellLevel = LargeWorldEntity.CellLevel.Far;

                    // Add constructible
                    var constructable = prefab.AddComponent<Constructable>();

                    constructable.allowedOutside = true;
                    constructable.allowedInBase = true;
                    constructable.allowedOnGround = true;
                    constructable.allowedOnWall = false;
                    constructable.rotationEnabled = true;
                    constructable.allowedOnCeiling = false;
                    constructable.allowedInSub = false;
                    constructable.allowedOnConstructables = false;
                    constructable.model = model;
                    constructable.techType = TechType;



                    PrefabIdentifier prefabID = prefab.AddComponent<PrefabIdentifier>();
                    prefabID.ClassId = ClassID;

                    //AddBubbles(prefab);

                    PowerRelay solarPowerRelay = CraftData.GetPrefabForTechType(TechType.SolarPanel).GetComponent<PowerRelay>();

                    var ps = prefab.AddComponent<PowerSource>();
                    ps.maxPower = 500f;

                    var pFX = prefab.AddComponent<PowerFX>();
                    pFX.vfxPrefab = solarPowerRelay.powerFX.vfxPrefab;
                    pFX.attachPoint = prefab.transform;

                    var pr = prefab.AddComponent<PowerRelay>();
                    pr.powerFX = pFX;
                    pr.maxOutboundDistance = 15;
                    pr.internalPowerSource = ps;
                    
                    prefab.AddComponent<TechTag>().type = TechType;
                    prefab.AddComponent<AlterraGenController>();
                    
                    
                    Resources.UnloadAsset(solarPowerRelay);

                    //Apply the glass shader here because of autosort lockers for some reason doesnt like it.
                    MaterialHelpers.ApplyGlassShaderTemplate(prefab, "_glass", Mod.ModName);
                    return prefab;
                }

            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
            }

            return null;
        }

        private void AddBubbles(GameObject prefab)
        {
            //foreach (Vector3 bubbleLocation in _bubbleLocations)
            //{
            //    MaterialHelpers.AddNewBubbles(prefab, bubbleLocation, new Vector3(270f, 266f, 0f));
            //}
        }

#if SUBNAUTICA
        protected override TechData GetBlueprintRecipe()
        {
            QuickLogger.Debug($"Creating recipe...");
            // Create and associate recipe to the new TechType
            var customFabRecipe = new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Mod.AlterraGenKitClassID.ToTechType(),1)
                }
            };
            return customFabRecipe;
        }

        protected override Atlas.Sprite GetItemSprite()
        {
            return new Atlas.Sprite(ImageUtils.LoadTextureFromFile(Path.Combine(_assetFolder, $"{ClassID}.png")));
        }
#elif BELOWZERO
        protected override RecipeData GetBlueprintRecipe()
        {
            QuickLogger.Debug($"Creating recipe...");
            // Create and associate recipe to the new TechType
            var customFabRecipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Mod.AlterraGenKitClassID.ToTechType(),1)
                }
            };
            return customFabRecipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(_assetFolder, $"{ClassID}.png"));
        }
#endif
        public override TechGroup GroupForPDA => TechGroup.Miscellaneous;
        public override TechCategory CategoryForPDA => TechCategory.Misc;
        private string _assetFolder => Mod.GetAssetFolder();
        public override string AssetsFolder => _assetFolder;
    }
}