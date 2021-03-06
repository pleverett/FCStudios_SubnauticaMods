﻿using FCSAlterraIndustrialSolutions.Logging;
using FCSAlterraIndustrialSolutions.Models.Controllers;
using FCSCommon.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace FCSAlterraIndustrialSolutions.Models.Buttons
{
    public class TurbineItem : MonoBehaviour
    {
        #region Private Members
        private bool _foundComponents = true;
        private Text _speedText;
        private Text _depthText;
        private GameObject _healthLbl;
        private GameObject _damagedLbl;
        private GameObject _pingBTN;
        private Text _healthText;
        private Color _healthColor;
        private GameObject _health;
        private GameObject _damaged;
        private Text _powerBTNText;
        private Text _pingBTNText;
        #endregion

        #region Public Members
        public JetStreamT242Controller Turbine { get; set; }
        public MarineMoniterDisplay Display { get; set; }
        public GameObject PowerBTN { get; set; }
        #endregion

        #region Unity Methods

        private void Awake()
        {

        }

        private void Update()
        {
            if (_foundComponents)
            {
                GetTurbineData();
            }
        }

        #endregion

        #region Private Methods
        private bool FindAllComponents()
        {


            #region Speed Text

            _speedText = gameObject.FindChild("Speed").FindChild("Text")?.GetComponent<Text>();

            if (_speedText == null)
            {
                Log.Error("Speed Text not found.");
                return false;
            }

            #endregion

            #region Depth Text

            _depthText = gameObject.FindChild("Depth").FindChild("Text")?.GetComponent<Text>();

            if (_depthText == null)
            {
                Log.Error("Depth Text not found.");
                return false;
            }

            #endregion

            #region Health

            _health = gameObject.FindChild("Health")?.gameObject;

            if (_health == null)
            {
                Log.Error("Health not found.");
                return false;
            }

            #endregion

            #region Health Label

            _healthLbl = _health.FindChild("Health_LBL")?.gameObject;

            if (_healthLbl == null)
            {
                Log.Error("Health Label not found.");
                return false;
            }

            #endregion

            #region Health Text

            _healthText = _healthLbl?.GetComponent<Text>();

            if (_healthText == null)
            {
                Log.Error("Health Text not found.");
                return false;
            }

            #endregion

            #region Health Color

            _healthColor = _healthText.color;

            if (_healthColor == null)
            {
                Log.Error("Health Color not found.");
                return false;
            }

            #endregion

            #region Damaged

            _damaged = gameObject.FindChild("Damaged")?.gameObject;

            if (_damaged == null)
            {
                Log.Error("Damaged not found.");
                return false;
            }

            #endregion

            #region Damaged Label

            _damagedLbl = _damaged.FindChild("DamagedLBL")?.gameObject;

            if (_damagedLbl == null)
            {
                Log.Error("Damaged Label not found.");
                return false;
            }

            #endregion

            Log.Info("Damage Label Reached");

            #region Ping BTN

            _pingBTN = gameObject.FindChild("PingBTN")?.gameObject;

            if (_pingBTN == null)
            {
                Log.Error("Ping BTN not found.");
                return false;
            }

            _pingBTNText = _pingBTN?.GetComponentInChildren<Text>();

            if (_pingBTNText == null)
            {
                Log.Error("Ping BTN Text not found.");
                return false;
            }

            var pingBTN = _pingBTN.AddComponent<InterfaceButton>();
            pingBTN.Display = Display;
            pingBTN.STARTING_COLOR = new Color(0.06640625f, 0.62109375f, 0.8828125f);
            pingBTN.BtnName = "PingBTN";
            pingBTN.ButtonMode = InterfaceButtonMode.BackgroundScale;
            pingBTN.TextComponent = _pingBTNText;
            pingBTN.IncreaseButtonBy = 0.013282f;
            pingBTN.Tag = this;
            #endregion
            //Log.Info("PingBTN Reached");

            #region Power BTN

            PowerBTN = gameObject.FindChild("PowerBTN")?.gameObject;

            if (PowerBTN == null)
            {
                Log.Error("Power BTN not found.");
                return false;
            }


            _powerBTNText = PowerBTN.GetComponentInChildren<Text>();

            if (_powerBTNText == null)
            {
                Log.Error("Power BTN Text not found.");
                return false;
            }

            var powerBTN = PowerBTN.AddComponent<InterfaceButton>();
            powerBTN.Display = Display;
            powerBTN.BtnName = "PowerBTN";
            powerBTN.ButtonMode = InterfaceButtonMode.BackgroundScale;
            powerBTN.STARTING_COLOR = new Color(0.05859375f, 0.73828125f, 0.59375f);
            powerBTN.TextComponent = _powerBTNText;
            powerBTN.IncreaseButtonBy = 0.013282f;
            powerBTN.Tag = this;

            #endregion
            Log.Info("PowerBTN Reached");

            return true;
        }

        private void GetTurbineData()
        {
            _depthText.text = $"{Mathf.Round(Turbine.GetDepth())}M";

            _speedText.text = $"{Turbine.GetSpeed()}rpm";

            UpdateData();
        }

        private void UpdateData()
        {
            _damaged.SetActive(Turbine.GetHealth() <= 0);
            _health.SetActive(Turbine.GetHealth() > 0);

            _healthText.text = $"{LoadItems.MarineMonitorModStrings.Health} - {Turbine.GetHealth()}%";


            if (Turbine.GetHealth() <= 100 && Turbine.GetHealth() > 50)
            {
                _healthText.color = new Color(0f, 0.99609375f, 0.25390625f);
            }
            else if (Turbine.GetHealth() <= 50 && Turbine.GetHealth() > 25)
            {
                _healthText.color = new Color(0.99609375f, 0.765625f, 0f);
            }
            else
            {
                _healthText.color = new Color(0.99609375f, 0, 0);
            }

            _powerBTNText.text = Turbine.HasBreakerTripped ? LoadItems.MarineMonitorModStrings.OFF : LoadItems.MarineMonitorModStrings.ON;
            _pingBTNText.text = Turbine.IsBeingPinged ? LoadItems.MarineMonitorModStrings.PINGING : LoadItems.MarineMonitorModStrings.PING;
        }

        public void Setup(MarineMoniterDisplay display)
        {
            if (display == null)
            {
                Log.Error($"TurbineItem: Display is null");
            }
            else
            {
                Display = display;
            }


            if (FindAllComponents() == false)
            {
                _foundComponents = false;
                Log.Error("// ============== Error getting all Components ============== //");
            }
        }

        #endregion

    }
}
