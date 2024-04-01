using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiveSplit.Model.Input;
using System.Linq;
using SoulMemory.EldenRing;
using System.IO;
using LiveSplit.Options;

namespace LiveSplit.UI.Components
{

    public class EldenRaceCounterComponent : IComponent
    {
        public EldenRaceCounterComponent(LiveSplitState state)
        {
            VerticalHeight = 10;
            Settings = new EldenRaceCounterSettings(state.Settings.HotkeyProfiles.First().Value.AllowGamepadsAsHotkeys);
            Cache = new GraphicsCache();
            CounterNameLabel = new SimpleLabel();
            Counter = new EldenRaceCounter();
            this.state = state;
            Settings.CounterReinitialiseRequired += Settings_CounterReinitialiseRequired;
            Settings.IncrementUpdateRequired += Settings_CSVPathUpdated;
            Settings.RandomizedMappingUpdateRequired += Settings_RandomizedMappingUpdated;
            Settings.OutputDefaultCSVPointConf += Settings_OutputDefaultCSVPointConf;

            // Subscribe to input hooks.
            Settings.Hook.KeyOrButtonPressed += hook_KeyOrButtonPressed;
            state.OnReset += reset;
        }

        public EldenRaceCounter Counter { get; set; }
        public EldenRaceCounterSettings Settings { get; set; }

        public GraphicsCache Cache { get; set; }

        public float VerticalHeight { get; set; }

        public float MinimumHeight { get; set; }

        public float MinimumWidth
        {
            get
            {
                return CounterNameLabel.X + CounterValueLabel.ActualWidth;
            }
        }

        public float HorizontalWidth { get; set; }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public float PaddingTop { get; set; }
        public float PaddingLeft { get { return 7f; } }
        public float PaddingBottom { get; set; }
        public float PaddingRight { get { return 7f; } }

        protected SimpleLabel CounterNameLabel = new SimpleLabel();
        protected SimpleLabel CounterValueLabel = new SimpleLabel();

        protected Font CounterFont { get; set; }

        private LiveSplitState state;

        private EldenRing ERGame = new EldenRing();

        private void DrawGeneral(Graphics g, Model.LiveSplitState state, float width, float height, LayoutMode mode)
        {
            // Set Background colour.
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);

                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }

            // Set Font.
            CounterFont = Settings.OverrideCounterFont ? Settings.CounterFont : state.LayoutSettings.TextFont;

            // Calculate Height from Font.
            var textHeight = g.MeasureString("A", CounterFont).Height;
            VerticalHeight = 1.2f * textHeight * 2;
            MinimumHeight = MinimumHeight;

            PaddingTop = Math.Max(0, ((VerticalHeight / 2 - 0.75f * textHeight) / 2f));
            PaddingBottom = PaddingTop;

            // Assume most users won't count past four digits (will cause a layout resize in Horizontal Mode).
            float fourCharWidth = g.MeasureString("1000", CounterFont).Width;
            HorizontalWidth = CounterNameLabel.X + CounterNameLabel.ActualWidth + (fourCharWidth > CounterValueLabel.ActualWidth ? fourCharWidth : CounterValueLabel.ActualWidth) + 5;

            // Set Counter Name Label
            CounterNameLabel.HorizontalAlignment = mode == LayoutMode.Horizontal ? StringAlignment.Near : StringAlignment.Near;
            CounterNameLabel.VerticalAlignment = StringAlignment.Center;
            CounterNameLabel.X = 5;
            CounterNameLabel.Y = 0;
            CounterNameLabel.Width = (width - fourCharWidth - 5);
            CounterNameLabel.Height = height;
            CounterNameLabel.Font = CounterFont;
            CounterNameLabel.Brush = new SolidBrush(Settings.OverrideTextColor ? Settings.CounterTextColor : state.LayoutSettings.TextColor);
            CounterNameLabel.HasShadow = state.LayoutSettings.DropShadows;
            CounterNameLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            CounterNameLabel.OutlineColor = state.LayoutSettings.TextOutlineColor;
            CounterNameLabel.Draw(g);

            // Set Counter Value Label.
            CounterValueLabel.HorizontalAlignment = mode == LayoutMode.Horizontal ? StringAlignment.Far : StringAlignment.Far;
            CounterValueLabel.VerticalAlignment = StringAlignment.Center;
            CounterValueLabel.X = 5;
            CounterValueLabel.Y = 0;
            CounterValueLabel.Width = (width - 10);
            CounterValueLabel.Height = height;
            CounterValueLabel.Font = CounterFont;
            CounterValueLabel.Brush = new SolidBrush(Settings.OverrideTextColor ? Settings.CounterValueColor : state.LayoutSettings.TextColor);
            CounterValueLabel.HasShadow = state.LayoutSettings.DropShadows;
            CounterValueLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            CounterValueLabel.OutlineColor = state.LayoutSettings.TextOutlineColor;
            CounterValueLabel.Draw(g);
        }

        public void DrawHorizontal(Graphics g, Model.LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height, LayoutMode.Horizontal);
        }

        public void DrawVertical(System.Drawing.Graphics g, Model.LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight, LayoutMode.Vertical);
        }

        public string ComponentName
        {
            get { return "EldenRace Counter"; }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);

            // Initialise Counter from settings.
            Counter = new EldenRaceCounter();
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            try
            {
                if (Settings.Hook != null)
                    Settings.Hook.Poll();
            }
            catch { }

            this.state = state;

            if (state.CurrentPhase == TimerPhase.Running)
            {
                var refreshGameResult = ERGame.TryRefresh();
                if (!refreshGameResult.IsErr)
                {
                    Counter.Increment(ERGame);
                }
            }

            CounterNameLabel.Text = string.Format("{0}{1}{2}", Settings.CounterText, Environment.NewLine, Counter.lastPointsEarnedMsg);
            CounterValueLabel.Text = Counter.Count.ToString();

            Cache.Restart();
            Cache["CounterNameLabel"] = CounterNameLabel.Text;
            Cache["CounterValueLabel"] = CounterValueLabel.Text;

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
            Settings.Hook.KeyOrButtonPressed -= hook_KeyOrButtonPressed;
        }

        public int GetSettingsHashCode()
        {
            return Settings.GetSettingsHashCode();
        }

        /// <summary>
        /// Handles the CounterReinitialiseRequired event of the Settings control.
        /// </summary>
        private void Settings_CounterReinitialiseRequired(object sender, EventArgs e)
        {
            try { Counter = new EldenRaceCounter(); }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(ex.Message, "Reset counter error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void Settings_CSVPathUpdated(object sender, EventArgs e)
        {
            try { Counter.SetIncrement(Settings.CSVPath); }
            catch (FileFormatException ex)
            {
                Log.Error(ex);
                MessageBox.Show(ex.Message, "Configuration CSV error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Settings_RandomizedMappingUpdated(object sender, EventArgs e)
        {
            try { Counter.SetRandomizerMapping(Settings.RandomConfPath); }
            catch (FileFormatException ex)
            {
                Log.Error(ex);
                MessageBox.Show(ex.Message, "Randomized mapping error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void Settings_OutputDefaultCSVPointConf(object sender, EventArgs e)
        {
            try { Counter.OutputIncrement(Settings.CSVOutputPath); }
            catch (Exception ex)
            {
                Log.Error(ex);
                MessageBox.Show(ex.Message, "Output Default Config error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        // Basic support for keyboard/button input.
        private void hook_KeyOrButtonPressed(object sender, KeyOrButton e)
        {
            if ((Form.ActiveForm == state.Form && !Settings.GlobalHotkeysEnabled)
                || Settings.GlobalHotkeysEnabled && e == Settings.ResetKey)
            {
                Counter.Reset();
            }
        }

        private void reset(object sender, TimerPhase e)
        {
            Counter.Reset();
        }
    }
}
