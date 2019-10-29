﻿using ExStorageDepot.Buildable;
using ExStorageDepot.Configuration;
using ExStorageDepot.Enumerators;
using ExStorageDepot.Mono.Managers;
using FCSCommon.Utilities;
using System;
using UnityEngine;

namespace ExStorageDepot.Mono
{
    public class ExStorageDepotController : MonoBehaviour, IProtoEventListener, IConstructable
    {
        internal ExStorageDepotDisplayManager Display { get; private set; }
        private ExStorageDepotSaveDataEntry _saveData;
        private bool _initialized;
        internal ExStorageDepotNameManager NameController { get; private set; }
        internal ExStorageDepotAnimationManager AnimationManager { get; private set; }
        internal ExStorageDepotStorageManager Storage { get; private set; }
        internal BulkMultipliers BulkMultiplier { get; set; }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            if (!_initialized) return;

            if (!Mod.IsSaving())
            {
                QuickLogger.Info("Saving Ex Storage Depot");
                Mod.SaveExStorageDepot();
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoDeserialize");
            var prefabIdentifier = GetComponent<PrefabIdentifier>();
            var id = prefabIdentifier?.Id ?? string.Empty;
            var data = Mod.GetExStorageDepotSaveData(id);
            NameController.SetCurrentName(data.UnitName);
            Storage.LoadFromSave(data.StorageItems);
            BulkMultiplier = data.Multiplier;
            Display.UpdateMultiplier();
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = string.Empty;

            if (Storage == null || Storage.IsEmpty) return true;
            reason = "Please empty the Ex-Storage";
            return false;
        }

        public bool AddToStorage(InventoryItem item, out string reason)
        {
            if (Storage.CanHoldItem(1))
            {
                reason = String.Empty;
                Storage.ForceAddItem(item);
                return true;
            }

            reason = ExStorageDepotBuildable.ContainerFullMessage();
            return false;
        }

        public InventoryItem GetInventoryItem(TechType item)
        {
            if (Storage != null)
            {
                if (Storage.DoesItemExist(item))
                {
                    return Storage.ForceRemoveItem(item);
                }
            }

            return null;
        }

        public void OnConstructedChanged(bool constructed)
        {
            if (constructed)
            {
                if (!_initialized)
                {
                    Initialize();
                }
            }
        }

        private void Initialize()
        {
            if (NameController == null)
            {
                NameController = new ExStorageDepotNameManager();
                NameController.Initialize(this);
            }

            AnimationManager = gameObject.AddComponent<ExStorageDepotAnimationManager>();
            Storage = gameObject.AddComponent<ExStorageDepotStorageManager>();
            Storage.Initialize(this);

            if (Display == null)
            {
                Display = gameObject.AddComponent<ExStorageDepotDisplayManager>();
                Display.Initialize(this);
            }
            _initialized = true;
        }

        internal void Save(ExStorageDepotSaveData saveDataList)
        {
            var prefabIdentifier = GetComponent<PrefabIdentifier>();
            var id = prefabIdentifier.Id;

            if (_saveData == null)
            {
                _saveData = new ExStorageDepotSaveDataEntry();
            }
            _saveData.Id = id;
            _saveData.UnitName = NameController.GetCurrentName();
            //_saveData.StorageItems = Storage.GetTrackedItems();
            _saveData.Multiplier = BulkMultiplier;
            saveDataList.Entries.Add(_saveData);
        }
    }
}
