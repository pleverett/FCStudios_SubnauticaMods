﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStorageSolutions.Buildables;
using DataStorageSolutions.Configuration;
using DataStorageSolutions.Mono;
using FCSCommon.Components;
using FCSCommon.Enums;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace DataStorageSolutions.Model
{
    internal class RackSlot
    {
        private GameObject _dummy;
        private Text _counter;
        private bool _isInstantiated;
        private StringBuilder sb = new StringBuilder();
        private DSSRackController _mono;
        private bool _isOccupied;
        private HashSet<ObjectData> _server;
        private List<Filter> _filter = new List<Filter>();
        private float timeLeft = 1.0f;
        private const float TimerLimit = 1f;
        private bool _update;

        internal readonly int Id;
        internal readonly Transform Slot;

        internal bool IsOccupied
        {
            get => _isOccupied;
            set
            {
                _isOccupied = value;
                _mono.UpdatePowerUsage();
                ChangeDummyState(value);
            }
        }

        public bool HasFilters => Filter != null && Filter.Count > 0;

        internal HashSet<ObjectData> Server
        {
            get => _server;
            set
            {
                _server = value;
                UpdateNetwork();
            }
        }

        public List<Filter> Filter
        {
            get => _filter;
            set => _filter = value ?? new List<Filter>();
        }

        private bool FilterCrossCheck(TechType techType)
        {
            foreach (Filter filter in _filter)
            {
                if (filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            foreach (var filter in _filter)
            {
                if (!filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (_update)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft < 0)
                {
                    UpdateNetwork();
                    _update = false;
                    timeLeft = TimerLimit;
                }
            }
        }

        private void ChangeDummyState(bool b = true)
        {
            if (_dummy == null)
            {
                InstantiateDummy();
            }

            if (_dummy.activeSelf != b)
            {
                _dummy.SetActive(b);
            }
        }

        private void UpdateScreen()
        {
            _counter.text = $"{Server?.Count}/{QPatch.Configuration.Config.ServerStorageLimit}";
        }

        private string FormatData()
        {
            sb.Clear();

            var lookup = Server?.Where(x => x != null).ToLookup(x => x.TechType).ToArray();

            if (lookup == null) return sb.ToString();

            sb.Append(string.Format(AuxPatchers.FiltersCheckFormat(), Filter != null && Filter.Count > 0));
            sb.Append(Environment.NewLine);

            for (int i = 0; i < lookup.Length; i++)
            {
                if (i < 5)
                {
                    if (lookup[i].All(objectData => objectData.TechType != lookup[i].Key)) continue;
                    sb.Append($"{Language.main.Get(lookup[i].Key)} x{lookup[i].Count()}");
                    sb.Append(Environment.NewLine);
                }

            }

            return sb.ToString();
        }

        private void ResetTimer()
        {
            _update = true;
            timeLeft = TimerLimit;
        }

        private void OnButtonClick(string arg1, object arg2)
        {
            if (_mono.IsRackOpen())
            {
                var result = _mono.GivePlayerItem(QPatch.Server.TechType, new ObjectDataTransferData { data = Server, Filters = Filter, IsServer = true });
                QuickLogger.Debug($"Give Player ITem Result: {result}", true);
                if (result)
                {
                    DisconnectFromRack();
                }
            }
        }

        internal RackSlot(DSSRackController controller, int id, Transform slot)
        {
            _mono = controller;
            _mono.OnUpdate += Update;
            Id = id;
            Slot = slot;
        }

        internal void Clear()
        {
            Server = null;
        }

        internal void Add(ObjectData data)
        {
            Server.Add(data);
            ResetTimer();
        }

        internal void Remove(ObjectData data)
        {
            Server.Remove(data);
            ResetTimer();
        }
        
        internal bool FindAllComponents()
        {
            try
            {
                #region Canvas  
                var canvasGameObject = _dummy.gameObject.GetComponentInChildren<Canvas>()?.gameObject;

                if (canvasGameObject == null)
                {
                    QuickLogger.Error("Canvas cannot be found");
                    return false;
                }
                #endregion

                #region Counter

                _counter = canvasGameObject.GetComponentInChildren<Text>();
                #endregion

                #region Hit

                var interactionFace = InterfaceHelpers.FindGameObject(canvasGameObject, "Hit");
                var catcher = interactionFace.AddComponent<InterfaceButton>();
                catcher.ButtonMode = InterfaceButtonMode.TextColor;
                catcher.TextLineOne = string.Format(AuxPatchers.TakeServer(), Mod.ServerFriendlyName);
                catcher.TextLineTwo = "Data: {0}";
                catcher.GetAdditionalDataFromString = true;
                catcher.GetAdditionalString = FormatData;
                catcher.BtnName = "ServerClick";
                catcher.OnButtonClick += OnButtonClick;

                #endregion

                return true;
            }
            catch (Exception e)
            {
                QuickLogger.Error($"{e.Message}: {e.StackTrace}");
                return false;
            }
        }
        
        internal void InstantiateDummy()
        {
            if (_isInstantiated) return;

            _dummy = Slot.Find("Server")?.gameObject;

            if (FindAllComponents())
            {
                UpdateScreen();
            }

            _isInstantiated = true;
        }

        internal bool IsFull()
        {
            return Server != null && Server.Count >= QPatch.Configuration.Config.ServerStorageLimit;
        }

        internal void DisconnectFromRack()
        {
            IsOccupied = false;
            Clear();
            _mono.DisplayManager.UpdateContainerAmount();
            Mod.OnBaseUpdate?.Invoke();
        }

        internal void UpdateNetwork()
        {
            UpdateScreen();
            Mod.OnContainerUpdate?.Invoke(_mono);
        }

        public bool IsAllowedToAdd(TechType techType)
        {
            if (!IsOccupied) return false;

            return _filter.Count == 0 || FilterCrossCheck(techType);
        }
        
        ~RackSlot()
        {
            _mono.OnUpdate -= Update;
        }

        internal void Remove(TechType techType)
        {
            for (int i = 0; i < Server.Count; i++)
            {
                if (Server.ElementAt(i).TechType != techType) continue;
                Remove(Server.ElementAt(i));
                _mono.RemoveFromTrackedItems(techType);
                break;
            }
        }

        internal int GetItemCount(TechType techType)
        {
            return Server?.Where((t, i) => Server.ElementAt(i).TechType == techType).Count() ?? 0;
        }
    }
}