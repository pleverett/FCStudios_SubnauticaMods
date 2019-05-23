﻿using FCSAlterraShipping.Display;
using FCSAlterraShipping.Interfaces;
using FCSAlterraShipping.Models;
using FCSCommon.Extensions;
using FCSCommon.Utilities;
using Oculus.Newtonsoft.Json;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FCSAlterraShipping.Mono
{
    internal class AlterraShippingTarget : MonoBehaviour, IContainer, IConstructable, IProtoTreeEventListener
    {
        #region Private Members
        private Constructable _buildable = null;
        private IContainer _container;
        private AlterraShippingTransferHandler _transferHandler;
        #endregion

        #region Public Properties
        public string Name { get; set; } = "Shipping Box";
        public int ID { get; set; }
        public bool Recieved { get; set; }
        internal bool IsConstructed => _buildable != null && _buildable.constructed;
        public bool IsReceivingTransfer { get; set; }
        public Action OnReceivingTransfer;
        public Action OnItemSent;
        public Action<string> OnTimerChanged;
        private GameObject _cargoContainerModel;
        private bool _hasBreakerTripped;
        private readonly string SaveDirectory = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "AlterraShipping");
        private PrefabIdentifier _prefabID;
        private string SaveFile => Path.Combine(SaveDirectory, _prefabID.Id + ".json");
        public bool IsFull(TechType techType)
        {
            return _container.IsFull(techType);
        }

        public int NumberOfItems
        {
            get => _container.NumberOfItems;
        }

        public ShippingTargetManager Manager { get; private set; }
        public AlterraShippingAnimator AnimatorController { get; set; }

        #endregion

        private void Awake()
        {
            ID = gameObject.GetInstanceID();

            _prefabID = GetComponentInParent<PrefabIdentifier>();

            if (_buildable == null)
            {
                _buildable = GetComponentInParent<Constructable>();
            }

            if (_transferHandler == null)
            {
                _transferHandler = GetComponentInParent<AlterraShippingTransferHandler>();
            }

            Name = $"Shipping Box {ShippingTargetManager.GlobalShippingTargets.Count}";

            if (_transferHandler == null)
            {
                QuickLogger.Error($"Transfer Handler is null.");
            }

            _cargoContainerModel = gameObject.FindChild("model")
                .FindChild("mesh_body")
                .FindChild("cargo_container")?.gameObject;
            if (_cargoContainerModel == null)
            {
                QuickLogger.Error("Cargo Container Model is null");
            }
            _container = new AlterraShippingContainer(this);

            AnimatorController = this.transform.GetComponent<AlterraShippingAnimator>();

            if (AnimatorController == null)
            {
                QuickLogger.Error("Animator component not found on the GameObject.");
            }

            DoorStateHash = UnityEngine.Animator.StringToHash("DoorState");

            InvokeRepeating("CargoContainer", 1, 0.5f);
        }

        public int DoorStateHash { get; private set; }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            if (Manager != null)
            {
                Manager.ShippingTargets.Remove(this);
                Manager.UpdateGlobalTargets();
            }
            else
                ShippingTargetManager.RemoveShippingTarget(this);
        }

        internal AlterraShippingTransferHandler GetTransferHandler()
        {
            return _transferHandler;
        }
        public void OnAddItemEvent(InventoryItem item)
        {
            _buildable.deconstructionAllowed = false;
        }

        public void OnRemoveItemEvent(InventoryItem item)
        {
            _buildable.deconstructionAllowed = _container.NumberOfItems == 0;

        }

        public bool HasItems()
        {
            return _container.HasItems();
        }

        public void OpenStorage()
        {
            _container.OpenStorage();
        }

        public ItemsContainer GetContainer()
        {
            return _container.GetContainer();
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = string.Empty;

            if (IsReceivingTransfer) return false;
            return _buildable.deconstructionAllowed;
        }

        public void OnConstructedChanged(bool constructed)
        {
            if (constructed)
            {
                var display = gameObject.GetOrAddComponent<AlterraShippingDisplay>();
                SubRoot root = GetComponentInParent<SubRoot>();

                if (root != null)
                {
                    QuickLogger.Debug(root.name);
                    AddToManager(root);
                    QuickLogger.Debug($"Added to manager");
                    gameObject.GetOrAddComponent<AlterraShippingDisplay>();
                }
                else
                {
                    QuickLogger.Error($"Root returned null");
                }
            }
        }

        private void CargoContainer()
        {
            if (gameObject == null || _container == null)
            {
                QuickLogger.Error($"The gameObject/Container is null.");
                return;
            }

            if (_cargoContainerModel != null) _cargoContainerModel.SetActive(_container.NumberOfItems != 0);

            if (_container.NumberOfItems == 0)
            {
                AnimatorController.SetBoolHash(DoorStateHash, false);
            }
        }

        public void AddToManager(SubRoot subRoot, ShippingTargetManager managers = null)
        {
            Manager = managers ?? ShippingTargetManager.FindManager(subRoot);
            Manager.AddShippingTarget(this);

            QuickLogger.Debug("Target has been connected", true);
        }

        public void TransferItems(AlterraShippingTarget target)
        {
            _transferHandler.SendItems(_container.GetContainer(), target);
        }

        public void ClearContainer()
        {
            _container.ClearContainer();
        }

        public void AddItem(InventoryItem item)
        {
            _container.AddItem(item);
        }

        public void RemoveItem(Pickupable item)
        {
            _container.RemoveItem(item);
        }

        public void RemoveItem(TechType item)
        {
            _container.RemoveItem(item);
        }

        public bool CanFit()
        {
            return _container.CanFit();
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            var saveData = new SaveData
            {
                HasBreakerTripped = _hasBreakerTripped,

            };

            //int amount = 1;
            foreach (InventoryItem item in _container.GetContainer())
            {
                if (saveData.ContainerItems.ContainsKey(item.item.GetTechType())) continue;

                //Get all of the current type
                var items = _container.GetContainer().GetItems(item.item.GetTechType());

                saveData.ContainerItems.Add(item.item.GetTechType(), items.Count);
            }

            string output = JsonConvert.SerializeObject(saveData, Formatting.Indented);

            File.WriteAllText(SaveFile, output);
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            QuickLogger.Info($"In De serialized");

            if (File.Exists(SaveFile))
            {
                string savedDataJson = File.ReadAllText(SaveFile).Trim();

                //LoadData
                var savedData = JsonConvert.DeserializeObject<SaveData>(savedDataJson);

                _hasBreakerTripped = savedData.HasBreakerTripped;

                foreach (KeyValuePair<TechType, int> containerItem in savedData.ContainerItems)
                {
                    for (int i = 0; i < containerItem.Value; i++)
                    {
                        GameObject prefab = CraftData.GetPrefabForTechType(containerItem.Key);

                        var gameObject = GameObject.Instantiate<GameObject>(prefab);

                        Pickupable pickupable = gameObject.GetComponent<Pickupable>().Pickup(false);

                        var item = new InventoryItem(pickupable);

                        _container.AddItem(item);
                    }
                }
                //// Set the TechType value on the TechTag
                //obj.GetOrAddComponent<TechTag>().type = savedData.ItemID;


                //// Set the ClassId
                //obj.GetComponent<PrefabIdentifier>().ClassId = LoadItems.AllowedFilters[savedData.ItemID].ClassID;


                //Pickupable pickupable = obj.GetComponent<Pickupable>().Pickup(false);



                //_itemContainer.AddItem(pickupable);
            }
        }
    }
}