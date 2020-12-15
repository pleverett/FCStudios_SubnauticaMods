﻿using System;
using System.Collections.Generic;
using System.Linq;
using FCS_AlterraHomeSolutions.Mono.PaintTool;
using FCS_AlterraHub.Configuration;
using FCS_AlterraHub.Extensions;
using FCS_AlterraHub.Interfaces;
using FCS_AlterraHub.Registration;
using FCS_AlterraHub.Systems;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using UnityEngine;

namespace FCS_AlterraHub.Mono.OreConsumer
{
    internal class OreConsumerController : FcsDevice, IFCSStorage, IFCSSave<SaveData>, IHandTarget
    {
        private bool _isFromSave;
        private bool _runStartUpOnEnable;
        private OreConsumerDataEntry _savedData;
        public int GetContainerFreeSpace { get; }
        public bool IsFull { get; }
        public Action<int, int> OnContainerUpdate { get; set; }
        public Action<FcsDevice, TechType> OnContainerAddItem { get; set; }
        public Action<FcsDevice, TechType> OnContainerRemoveItem { get; set; }
        public DumpContainer DumpContainer { get; private set; }
        public override bool IsOperational => CheckIfOperational();
        public bool IsOnPlatform => Manager?.Habitat != null;

        private bool CheckIfOperational()
        {
            if(IsInitialized && IsConstructed && Manager != null && _oreQueue != null && Manager.HasEnoughPower(GetPowerUsage())) return true;
            return false;
        }

        public OreConsumerDisplay DisplayManager { get; private set; }
        public TransferHandler TransferHandler { get; private set; }
        public MotorHandler MotorHandler { get; private set; }
        public EffectsManager EffectsManager { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public Action<bool> onUpdateSound { get; private set; }
        public ColorManager ColorManager { get; private set; }
        

        private Queue<TechType> _oreQueue;
        private const float OreProcessingTime = 90f;
        private float _timeLeft;
        public override float GetPowerUsage()
        {
            return _oreQueue?.Count > 0 ? 0.85f : 0;
        }

        #region Unity Methods

        private void Awake()
        {
            _timeLeft = OreProcessingTime;
        }

        private void Start()
        {
            InvokeRepeating(nameof(UpdateAnimation),1f,1f);
            FCSAlterraHubService.PublicAPI.RegisterDevice(this, Mod.OreConsumerTabID, Mod.ModName);
        }

        private void UpdateAnimation()
        {
            if (_oreQueue != null && _oreQueue.Count > 0)
            {
                MotorHandler.Start();
                EffectsManager.ShowEffect();
                AudioManager.PlayMachineAudio();
            }
            else
            {
                MotorHandler.Stop();
                EffectsManager.HideEffect();
                AudioManager.StopMachineAudio();
            }
        }

        private void Update()
        {
            if (_oreQueue != null && IsOperational && _oreQueue.Count > 0)
            {
                _timeLeft -= Time.deltaTime;
                if (_timeLeft < 0)
                {
                    AppendMoney(StoreInventorySystem.GetOrePrice(_oreQueue.Dequeue()));
                    _timeLeft = OreProcessingTime;
                }
            }
        }

        private void OnEnable()
        {
            if (_runStartUpOnEnable)
            {
                if (!IsInitialized)
                {
                    Initialize();
                }

                if (_isFromSave)
                {
                    if (_savedData == null)
                    {
                        ReadySaveData();
                    }

                    if(_savedData.OreQueue != null)
                    {
                        _oreQueue = _savedData.OreQueue;
                        _timeLeft = _savedData.TimeLeft;
                    }
                    MotorHandler.SpeedByPass(_savedData.RPM);
                    ColorManager.ChangeColor(_savedData.Color.Vector4ToColor(),ColorTargetMode.Both);
                }

                _runStartUpOnEnable = false;
            }
        }
        
        #endregion

        public override void Initialize()
        {
            if (IsInitialized) return;

            if (_oreQueue == null)
            {
               _oreQueue = new Queue<TechType>();
            }

            if (DumpContainer == null)
            {
                DumpContainer = gameObject.AddComponent<DumpContainer>();
                DumpContainer.Initialize(transform,Buildables.AlterraHub.OreConsumerReceptacle(),this);
            }

            if (TransferHandler == null)
            {
                TransferHandler = gameObject.AddComponent<TransferHandler>();
                TransferHandler.Initialize();
            }

            if(DisplayManager == null)
            {
                DisplayManager = gameObject.AddComponent<OreConsumerDisplay>();
                DisplayManager.Setup(this);
                DisplayManager.onDumpButtonClicked.AddListener(() =>
                {
                    DumpContainer.OpenStorage();
                });
                DisplayManager.ForceRefresh(CardSystem.main.GetAccountBalance());
            }

            if (MotorHandler == null)
            {
                MotorHandler = GameObjectHelpers.FindGameObject(gameObject, "core_anim").AddComponent<MotorHandler>();
                MotorHandler.Initialize(30);
            }

            if (EffectsManager == null)
            {
                EffectsManager = gameObject.AddComponent<EffectsManager>();
                EffectsManager.Initialize(IsUnderWater());
            }

            if(AudioManager == null)
            {
                AudioManager = new AudioManager(gameObject.EnsureComponent<FMOD_CustomLoopingEmitter>());
                AudioManager.PlayMachineAudio();
            }

            QPatch.Configuration.OnPlaySoundToggleEvent += value => { onUpdateSound?.Invoke(value); };

            onUpdateSound += value =>
            {
                if(value)
                {
                    AudioManager.PlayMachineAudio();
                }
                else
                {
                    AudioManager.StopMachineAudio();
                }
            };

            if (ColorManager == null)
            {
                ColorManager = gameObject.AddComponent<ColorManager>();
                ColorManager.Initialize(gameObject, Buildables.AlterraHub.BodyMaterial);
            }



            CardSystem.main.onBalanceUpdated += () =>
            {
                DisplayManager?.onTotalChanged?.Invoke(CardSystem.main.GetAccountBalance());
            };

            DisplayManager?.onTotalChanged?.Invoke(CardSystem.main.GetAccountBalance());

#if DEBUG
            QuickLogger.Debug($"Initialized Ore Consumer {GetPrefabID()}");
#endif
            IsInitialized = true;
        }

        private bool IsUnderWater()
        {
            return GetDepth() >= 7.0f;
        }

        internal float GetDepth()
        {
#if SUBNAUTICA
            return gameObject == null ? 0f : Ocean.main.GetDepthOf(gameObject);
#elif BELOWZERO
            return gameObject == null ? 0f : Ocean.GetDepthOf(gameObject);
#endif
        }

        public override void OnProtoSerialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoSerialize");

            if (!Mod.IsSaving())
            {
                QuickLogger.Info($"Saving {GetPrefabID()}");
                Mod.Save();
                QuickLogger.Info($"Saved {GetPrefabID()}");
            }
        }

        public override void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoDeserialize");

            if (_savedData == null)
            {
                ReadySaveData();
            }

            _isFromSave = true;
        }

        public override bool CanDeconstruct(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override void OnConstructedChanged(bool constructed)
        {
            IsConstructed = constructed;
            if (constructed)
            {
                if (isActiveAndEnabled)
                {
                    if (!IsInitialized)
                    {
                        Initialize();
                    }

                    IsInitialized = true;
                }
                else
                {
                    _runStartUpOnEnable = true;
                }
            }
        }

        public bool CanBeStored(int amount, TechType techType)
        {
            return StoreInventorySystem.ValidResource(techType);
        }

        public bool AddItemToContainer(InventoryItem item)
        {
            try
            {
                _oreQueue.Enqueue(item.item.GetTechType());
                Destroy(item.item.gameObject);
            }
            catch (Exception e)
            {
                QuickLogger.DebugError($"Message: {e.Message} || StackTrace: {e.StackTrace}");
                return false;
            }

            return true;
        }

        private void AppendMoney(decimal price)
        {
            CardSystem.main.AddFinances(price);
        }
        
        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return CanBeStored(0, pickupable.GetTechType());
        }

        public bool IsAllowedToRemoveItems()
        {
            return false;
        }

        public Pickupable RemoveItemFromContainer(TechType techType, int amount)
        {
            return null;
        }

        public Dictionary<TechType, int> GetItemsWithin()
        {
            return null;
        }

        public bool ContainsItem(TechType techType)
        {
            return false;
        }

        public void Save(SaveData newSaveData, ProtobufSerializer serializer)
        {
            if (!IsInitialized
                || !IsConstructed) return;

            if (_savedData == null)
            {
                _savedData = new OreConsumerDataEntry();
            }

            _savedData.Id = GetPrefabID();
            _savedData.OreQueue = _oreQueue;
            _savedData.TimeLeft = _timeLeft;
            _savedData.RPM = MotorHandler.GetRPM();
            _savedData.Color = ColorManager.GetColor().ColorToVector4();
            _savedData.BaseId = BaseId;
            QuickLogger.Debug($"Saving ID {_savedData.Id}", true);
            newSaveData.OreConsumerEntries.Add(_savedData);
        }

        private void ReadySaveData()
        {
            QuickLogger.Debug("In OnProtoDeserialize");
            _savedData = Mod.GetOreConsumerDataEntrySaveData(GetPrefabID());
        }

        public override bool ChangeBodyColor(Color color, ColorTargetMode mode)
        {
            return ColorManager.ChangeColor(color,mode);   
        }

        public void OnHandHover(GUIHand hand)
        {
            var main = HandReticle.main;
            main.SetIcon(HandReticle.IconType.Info);

            if (_oreQueue != null && DisplayManager?.CheckInteraction.IsHovered == false)
            {
                if (_oreQueue.Any())
                {
                    var pendingAmount = _oreQueue.Count > 1 ? _oreQueue.Count - 1 : 0;
                    main.SetInteractText(Buildables.AlterraHub.OreConsumerTimeLeftFormat(Language.main.Get(_oreQueue.Peek()), _timeLeft.ToString("N0"),$"{pendingAmount}"));
                }
                else
                {
                    main.SetInteractText(Buildables.AlterraHub.NoOresToProcess());
                }
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            QuickLogger.Debug("Clicked",true);
        }
    }
}