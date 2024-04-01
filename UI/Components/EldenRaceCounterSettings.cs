using LiveSplit.Options;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace LiveSplit.UI.Components
{
    public partial class EldenRaceCounterSettings : UserControl
    {
        public EldenRaceCounterSettings(bool allowGamepads)
        {
            InitializeComponent();

            Hook = new CompositeHook(allowGamepads);

            // Set default values.
            GlobalHotkeysEnabled = false;
            CounterFont = new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel);
            OverrideCounterFont = false;
            CounterTextColor = Color.FromArgb(255, 255, 255, 255);
            CounterValueColor = Color.FromArgb(255, 255, 255, 255);
            OverrideTextColor = false;
            BackgroundColor = Color.Transparent;
            BackgroundColor2 = Color.Transparent;
            BackgroundGradient = GradientType.Plain;
            CounterText = "EldenRace Counter:";

            // Hotkeys
            ResetKey = new KeyOrButton(Keys.NumPad0);

            // Set bindings.
            txtCounterText.DataBindings.Add("Text", this, "CounterText");
            chkGlobalHotKeys.DataBindings.Add("Checked", this, "GlobalHotkeysEnabled", false, DataSourceUpdateMode.OnPropertyChanged);
            chkFont.DataBindings.Add("Checked", this, "OverrideCounterFont", false, DataSourceUpdateMode.OnPropertyChanged);
            lblFont.DataBindings.Add("Text", this, "CounterFontString", false, DataSourceUpdateMode.OnPropertyChanged);
            chkColor.DataBindings.Add("Checked", this, "OverrideTextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor.DataBindings.Add("BackColor", this, "CounterTextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor3.DataBindings.Add("BackColor", this, "CounterValueColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor1.DataBindings.Add("BackColor", this, "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor2.DataBindings.Add("BackColor", this, "BackgroundColor2", false, DataSourceUpdateMode.OnPropertyChanged);
            cmbGradientType.DataBindings.Add("SelectedItem", this, "GradientString", false, DataSourceUpdateMode.OnPropertyChanged);

            // Assign event handlers.
            cmbGradientType.SelectedIndexChanged += cmbGradientType_SelectedIndexChanged;
            chkFont.CheckedChanged += chkFont_CheckedChanged;
            chkColor.CheckedChanged += chkColor_CheckedChanged;
            chkGlobalHotKeys.CheckedChanged += chkGlobalHotKeys_CheckedChanged;

            Load += CounterSettings_Load;

            RegisterHotKeys();
        }

        public CompositeHook Hook { get; set; }

        public bool GlobalHotkeysEnabled { get; set; }

        public Color CounterTextColor { get; set; }
        public Color CounterValueColor { get; set; }
        public bool OverrideTextColor { get; set; }

        public string CounterFontString { get { return String.Format("{0} {1}", CounterFont.FontFamily.Name, CounterFont.Style); } }
        public Font CounterFont { get; set; }
        public bool OverrideCounterFont { get; set; }

        public Color BackgroundColor { get; set; }
        public Color BackgroundColor2 { get; set; }
        public GradientType BackgroundGradient { get; set; }
        public String GradientString
        {
            get { return BackgroundGradient.ToString(); }
            set { BackgroundGradient = (GradientType)Enum.Parse(typeof(GradientType), value); }
        }

        public string CounterText { get; set; }
        public string CSVPath { get; set; }
        public string RandomConfPath { get; set; }
        public string CSVOutputPath { get; set; }

        public KeyOrButton ResetKey { get; set; }

        public event EventHandler CounterReinitialiseRequired;
        public event EventHandler IncrementUpdateRequired;
        public event EventHandler RandomizedMappingUpdateRequired;
        public event EventHandler OutputDefaultCSVPointConf;

        private enum inputFileType
        {
            PointConfiguration,
            PointConfigurationRandom
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            GlobalHotkeysEnabled = SettingsHelper.ParseBool(element["GlobalHotkeysEnabled"]);
            CounterTextColor = SettingsHelper.ParseColor(element["CounterTextColor"]);
            CounterValueColor = SettingsHelper.ParseColor(element["CounterValueColor"]);
            CounterFont = SettingsHelper.GetFontFromElement(element["CounterFont"]);
            OverrideCounterFont = SettingsHelper.ParseBool(element["OverrideCounterFont"]);
            OverrideTextColor = SettingsHelper.ParseBool(element["OverrideTextColor"]);
            BackgroundColor = SettingsHelper.ParseColor(element["BackgroundColor"]);
            BackgroundColor2 = SettingsHelper.ParseColor(element["BackgroundColor2"]);
            GradientString = SettingsHelper.ParseString(element["BackgroundGradient"]);
            CounterText = SettingsHelper.ParseString(element["CounterText"]);
            CSVPath = SettingsHelper.ParseString(element["InitialValue"]);

            // Increment = SettingsHelper.ParseInt(element["Increment"]);
            XmlElement resetElement = element["ResetKey"];
            ResetKey = string.IsNullOrEmpty(resetElement.InnerText) ? null : new KeyOrButton(resetElement.InnerText);

            RegisterHotKeys();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.0") ^
            SettingsHelper.CreateSetting(document, parent, "GlobalHotkeysEnabled", GlobalHotkeysEnabled) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideCounterFont", OverrideCounterFont) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideTextColor", OverrideTextColor) ^
            SettingsHelper.CreateSetting(document, parent, "CounterFont", CounterFont) ^
            SettingsHelper.CreateSetting(document, parent, "CounterTextColor", CounterTextColor) ^
            SettingsHelper.CreateSetting(document, parent, "CounterValueColor", CounterValueColor) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor", BackgroundColor) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor2", BackgroundColor2) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundGradient", BackgroundGradient) ^
            SettingsHelper.CreateSetting(document, parent, "CounterText", CounterText) ^
            SettingsHelper.CreateSetting(document, parent, "CSVPath", CSVPath) ^
            SettingsHelper.CreateSetting(document, parent, "ResetKey", ResetKey);
        }

        // Behaviour essentially Lifted from LiveSplit Settings.
        private void SetHotkeyHandlers(TextBox txtBox, Action<KeyOrButton> keySetCallback)
        {
            string oldText = txtBox.Text;
            txtBox.Text = "Set Hotkey...";
            txtBox.Select(0, 0);

            KeyEventHandler handlerDown = null;
            KeyEventHandler handlerUp = null;
            EventHandler leaveHandler = null;
            EventHandlerT<GamepadButton> gamepadButtonPressed = null;

            // Remove Input handlers.
            Action unregisterEvents = () =>
            {
                txtBox.KeyDown -= handlerDown;
                txtBox.KeyUp -= handlerUp;
                txtBox.Leave -= leaveHandler;
                Hook.AnyGamepadButtonPressed -= gamepadButtonPressed;
            };

            // Handler for KeyDown
            handlerDown = (s, x) =>
            {
                KeyOrButton keyOrButton = x.KeyCode == Keys.Escape ? null : new KeyOrButton(x.KeyCode | x.Modifiers);

                // No action for special keys.
                if (x.KeyCode == Keys.ControlKey || x.KeyCode == Keys.ShiftKey || x.KeyCode == Keys.Menu)
                    return;

                keySetCallback(keyOrButton);
                unregisterEvents();

                // Remove Focus.
                txtBox.Select(0, 0);
                chkGlobalHotKeys.Select();

                txtBox.Text = FormatKey(keyOrButton);

                // Re-Register inputs.
                RegisterHotKeys();
            };

            // Handler for KeyUp (allows setting of special keys, shift, ctrl etc.).
            handlerUp = (s, x) =>
            {
                KeyOrButton keyOrButton = x.KeyCode == Keys.Escape ? null : new KeyOrButton(x.KeyCode | x.Modifiers);

                // No action for normal keys.
                if (x.KeyCode != Keys.ControlKey && x.KeyCode != Keys.ShiftKey && x.KeyCode != Keys.Menu)
                    return;

                keySetCallback(keyOrButton);
                unregisterEvents();
                txtBox.Select(0, 0);
                chkGlobalHotKeys.Select();
                txtBox.Text = FormatKey(keyOrButton);
                RegisterHotKeys();
            };

            leaveHandler = (s, x) =>
            {
                unregisterEvents();
                txtBox.Text = oldText;
            };

            // Handler for gamepad/joystick inputs.
            gamepadButtonPressed = (s, x) =>
            {
                KeyOrButton key = new KeyOrButton(x);
                keySetCallback(key);
                unregisterEvents();

                Action keyOrButton = () =>
                {
                    txtBox.Select(0, 0);
                    chkGlobalHotKeys.Select();
                    txtBox.Text = FormatKey(key);
                    RegisterHotKeys();
                };

                // May not be in the UI thread (likely).
                if (InvokeRequired)
                    Invoke(keyOrButton);
                else
                    keyOrButton();
            };

            txtBox.KeyDown += handlerDown;
            txtBox.KeyUp += handlerUp;
            txtBox.Leave += leaveHandler;

            Hook.AnyGamepadButtonPressed += gamepadButtonPressed;
        }

        /// <summary>
        /// Registers the hot keys (unregisters existing Hotkeys).
        /// </summary>
        private void RegisterHotKeys()
        {
            txtReset.Text = FormatKey(ResetKey);

            try
            {
                UnregisterAllHotkeys(Hook);

                Hook.RegisterHotKey(ResetKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Unregisters all hotkeys.
        /// </summary>
        public void UnregisterAllHotkeys(CompositeHook hook)
        {
            hook.UnregisterAllHotkeys();
            HotkeyHook.Instance.UnregisterAllHotkeys();
        }

        private string FormatKey(KeyOrButton key)
        {
            if (key == null)
                return "None";
            string str = key.ToString();
            if (key.IsButton)
            {
                int length = str.LastIndexOf(' ');
                if (length != -1)
                    str = str.Substring(0, length);
            }
            return str;
        }

        private void CounterSettings_Load(object sender, EventArgs e)
        {
            chkColor_CheckedChanged(null, null);
            chkFont_CheckedChanged(null, null);
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick((Button)sender, this);
        }

        private void btnFont_Click(object sender, EventArgs e)
        {
            var dialog = SettingsHelper.GetFontDialog(CounterFont, 7, 20);
            dialog.FontChanged += (s, ev) => CounterFont = ((CustomFontDialog.FontChangedEventArgs)ev).NewFont;
            dialog.ShowDialog(this);
            lblFont.Text = CounterFontString;
        }

        private void chkColor_CheckedChanged(object sender, EventArgs e)
        {
            label3.Enabled = btnColor.Enabled = label5.Enabled = btnColor3.Enabled = chkColor.Checked;
        }

        void chkFont_CheckedChanged(object sender, EventArgs e)
        {
            label1.Enabled = lblFont.Enabled = btnFont.Enabled = chkFont.Checked;
        }
        void chkGlobalHotKeys_CheckedChanged(object sender, EventArgs e)
        {
            GlobalHotkeysEnabled = chkGlobalHotKeys.Checked;
        }

        void cmbGradientType_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnColor1.Visible = cmbGradientType.SelectedItem.ToString() != "Plain";
            btnColor2.DataBindings.Clear();
            btnColor2.DataBindings.Add("BackColor", this, btnColor1.Visible ? "BackgroundColor2" : "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            GradientString = cmbGradientType.SelectedItem.ToString();
        }

        private void txtReset_Enter(object sender, EventArgs e)
        {
            SetHotkeyHandlers((TextBox)sender, x => ResetKey = x);
        }

        private void txtReset_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void browsePointConf_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = "csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                CSVPath = dialog.FileName;
                IncrementUpdateRequired(this, EventArgs.Empty);
                string[] message = { CSVPath };
                setPointConfigurationTextBox(message, inputFileType.PointConfiguration);
            }
        }

        private void pointConfRandomBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = "txt"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RandomConfPath = dialog.FileName;
                RandomizedMappingUpdateRequired(this, EventArgs.Empty);
                string[] message = { RandomConfPath };

                setPointConfigurationTextBox(message, inputFileType.PointConfigurationRandom);
            }
        }

        private void setPointConfigurationTextBox(string[] messageParts, inputFileType inputType)
        {
            var txtBox = inputType == inputFileType.PointConfiguration ? pointConfigurationTextBox : pointConfigurationRandomTextBox;

            txtBox.Text = "";
            for (int i = 0; i < messageParts.Length; i++)
            {
                txtBox.AppendText(messageParts[i]);
                if (i < messageParts.Length - 1)
                {
                    txtBox.AppendText(Environment.NewLine);
                }
            }
        }

        private void randomizerCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            pointConfRandomBtn.Enabled = randomizerCheckbox.Checked;
        }

        private void writeDefaultConfigBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                DefaultExt = "csv",
                FileName = "default_elden_race_point_configuration.csv"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                CSVOutputPath = saveFileDialog.FileName;
                OutputDefaultCSVPointConf(this, EventArgs.Empty);
            }
        }
    }
}
