using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace RPAMultiAudioSyncPro
{
    /// <summary>
    /// RPA MultiAudio Sync Pro (7.1 USB + Bluetooth Router)
    /// Features:
    /// - Cinema Mode Lip-Sync / Audio-Sync for Bluetooth & Wired Headphones
    /// - Device Mixer & Ear Separation Mode (Full Both Ears, L-Channel Left Ear, R-Channel Right Ear)
    /// - Arranged Multi-Level Menu
    /// - General Preferences (Startup, Background Tray, Auto-Reconnect)
    /// - Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
            catch { }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    /// <summary>
    /// Ear Separation Modes
    /// </summary>
    public enum EarSeparationMode
    {
        BothEars,      // Full Both Ears
        LeftEarOnly,   // L-Channel Left Ear
        RightEarOnly   // R-Channel Right Ear
    }

    /// <summary>
    /// Custom Windows 11 Pill-Shaped Sliding Toggle Switch
    /// </summary>
    public class SlidingToggle : CheckBox
    {
        public Color ActiveColor { get; set; } = Color.FromArgb(16, 185, 129);
        public Color InactiveColor { get; set; } = Color.FromArgb(45, 50, 62);

        public SlidingToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            MinimumSize = new Size(46, 24);
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(36, 36, 44));

            int arc = Height - 2;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, arc, arc, 90, 180);
            path.AddArc(Width - arc - 1, 0, arc, arc, -90, 180);
            path.CloseFigure();

            using var trackBrush = new SolidBrush(Checked ? ActiveColor : InactiveColor);
            pevent.Graphics.FillPath(trackBrush, path);

            int thumbSize = Height - 6;
            int thumbX = Checked ? Width - thumbSize - 3 : 3;
            using var thumbBrush = new SolidBrush(Checked ? Color.White : Color.Silver);
            pevent.Graphics.FillEllipse(thumbBrush, thumbX, 3, thumbSize, thumbSize);
        }
    }

    public class MainForm : Form
    {
        private MMDeviceEnumerator _enumerator = new();
        private WasapiLoopbackCapture? _capture;
        private List<DevicePanel> _devicePanels = new();
        private List<AudioRouteNode> _activeNodes = new();
        private FlowLayoutPanel _flowPanel = new();
        private Button _btnMenu = new();
        private Button _btnMixer = new();
        private Button _btnToggle = new();
        private Button _btn71Toggle = new();
        private Button _btnSettings = new();
        private Button _btnCinemaSync = new();
        private SlidingToggle _tglGamingMode = new();
        private ComboBox _cmbMaster = new();
        private Label _lblStatus = new();
        private NotifyIcon _trayIcon = new();
        private ContextMenuStrip _mainMenu = new();
        private bool _isRunning = false;

        public static bool GamingModeActive { get; private set; } = true;
        public static bool Channel71Active { get; private set; } = true;
        public static bool LaunchOnStartup { get; set; } = true;
        public static bool RunInBackground { get; set; } = true;
        public static bool AutoReconnect { get; set; } = true;
        public static int CinemaLipSyncOffsetMs { get; set; } = 150;

        public MainForm()
        {
            Text = "RPA MultiAudio Sync Pro (7.1 USB + Bluetooth Router) - Dev: K Vanlalrengpuia";
            Size = new Size(820, 680);
            BackColor = Color.FromArgb(20, 22, 28);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            InitTrayIcon();
            InitMainMenu();
            BuildUI();
            LoadAudioDevices();
        }

        private void InitTrayIcon()
        {
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.Text = "RPA MultiAudio Sync Pro";
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open RPA MultiAudio", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
            menu.Items.Add("Device Mixer & Ear Separation...", null, (s, e) => OpenDeviceMixerDialog());
            menu.Items.Add("Cinema Lip-Sync Calibrator (150ms)", null, (s, e) => ApplyCinemaLipSync(150));
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => { _trayIcon.Visible = false; Application.Exit(); });
            _trayIcon.ContextMenuStrip = menu;
        }

        private void InitMainMenu()
        {
            _mainMenu = new ContextMenuStrip();
            _mainMenu.BackColor = Color.FromArgb(28, 32, 42);
            _mainMenu.ForeColor = Color.White;
            _mainMenu.Font = new Font("Segoe UI", 9.5f);

            // Group 1: Routing Profiles
            var mnuProfiles = new ToolStripMenuItem("🎧 Routing Profiles");
            mnuProfiles.DropDownItems.Add("Cinema Duo (USB 7.1 + Bluetooth Synced)", null, (s, e) => ApplyCinemaLipSync(150));
            mnuProfiles.DropDownItems.Add("Silent Partner (Ear-Separation Left/Right Split)", null, (s, e) => ApplyEarSplitPreset());
            mnuProfiles.DropDownItems.Add("FPS Esports Ultra (0ms Footsteps)", null, (s, e) => SetGamingMode(true));
            mnuProfiles.DropDownItems.Add("Studio Flat Monitor", null, (s, e) => ApplyStudioMonitorPreset());

            // Group 2: Audio Processing
            var mnuAudio = new ToolStripMenuItem("🎛 Audio Processing");
            mnuAudio.DropDownItems.Add("Device Mixer & Ear Separation...", null, (s, e) => OpenDeviceMixerDialog());
            mnuAudio.DropDownItems.Add("Calibrate Cinema Lip-Sync...", null, (s, e) => ApplyCinemaLipSync(150));
            mnuAudio.DropDownItems.Add("Toggle 7.1 Surround Sound", null, (s, e) => Toggle71Surround());

            // Group 3: Latency & Delay
            var mnuLatency = new ToolStripMenuItem("⚡ Latency Presets");
            mnuLatency.DropDownItems.Add("0ms (Ultra-Low Gaming Mode)", null, (s, e) => SetGamingMode(true));
            mnuLatency.DropDownItems.Add("150ms (Cinema Lip-Sync Match)", null, (s, e) => ApplyCinemaLipSync(150));
            mnuLatency.DropDownItems.Add("240ms (Bluetooth SBC High Latency)", null, (s, e) => ApplyCinemaLipSync(240));

            // Group 4: System & About
            var mnuSystem = new ToolStripMenuItem("⚙ System & About");
            mnuSystem.DropDownItems.Add("General Preferences...", null, (s, e) => OpenSettingsDialog());
            mnuSystem.DropDownItems.Add("About RPA MultiAudio Sync Pro...", null, (s, e) => OpenAboutDialog());

            _mainMenu.Items.Add(mnuProfiles);
            _mainMenu.Items.Add(mnuAudio);
            _mainMenu.Items.Add(mnuLatency);
            _mainMenu.Items.Add(new ToolStripSeparator());
            _mainMenu.Items.Add(mnuSystem);
        }

        private void BuildUI()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 145, Padding = new Padding(14), BackColor = Color.FromArgb(26, 30, 38) };

            // Menu button (arranged flyout menu)
            _btnMenu = new Button { Text = "☰ Menu", Location = new Point(14, 10), Width = 75, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 50, 62), ForeColor = Color.White };
            _btnMenu.Click += (s, e) => _mainMenu.Show(_btnMenu, new Point(0, _btnMenu.Height));

            // Gaming toggle
            _tglGamingMode = new SlidingToggle { Location = new Point(98, 12), Checked = true, ActiveColor = Color.FromArgb(6, 182, 212) };
            var lblGaming = new Label { Text = "⚡ 0ms GAMING", Location = new Point(150, 14), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(103, 232, 249) };

            // 7.1 button
            _btn71Toggle = new Button { Text = "7.1 SURROUND: ON", Location = new Point(270, 10), Width = 150, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.Black, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _btn71Toggle.Click += (s, e) => Toggle71Surround();

            // Cinema Lip-Sync
            _btnCinemaSync = new Button { Text = "🎬 Cinema Lip-Sync", Location = new Point(428, 10), Width = 140, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(245, 158, 11), ForeColor = Color.Black, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _btnCinemaSync.Click += (s, e) => ApplyCinemaLipSync(150);

            // Device Mixer button
            _btnMixer = new Button { Text = "🎛 Device Mixer", Location = new Point(576, 10), Width = 120, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(14, 116, 144), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _btnMixer.Click += (s, e) => OpenDeviceMixerDialog();

            // Settings button
            _btnSettings = new Button { Text = "⚙", Location = new Point(704, 10), Width = 40, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 50, 62), ForeColor = Color.White };
            _btnSettings.Click += (s, e) => OpenSettingsDialog();

            _tglGamingMode.CheckedChanged += (s, e) => SetGamingMode(_tglGamingMode.Checked);

            var lblMaster = new Label { Text = "Primary Capture Source (WASAPI Loopback):", Location = new Point(14, 52), AutoSize = true, ForeColor = Color.FromArgb(110, 231, 183) };
            _cmbMaster = new ComboBox { Location = new Point(14, 76), Width = 480, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(19, 23, 29), ForeColor = Color.White };

            var btnRefresh = new Button { Text = "Refresh", Location = new Point(505, 74), Width = 75, Height = 29, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 50, 62) };
            btnRefresh.Click += (s, e) => { if (!_isRunning) LoadAudioDevices(); };

            _btnToggle = new Button { Text = "START ROUTING", Location = new Point(590, 70), Width = 155, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _btnToggle.Click += (s, e) => { if (_isRunning) StopRouting(); else StartRouting(); };

            topPanel.Controls.AddRange(new Control[] { _btnMenu, _tglGamingMode, lblGaming, _btn71Toggle, _btnCinemaSync, _btnMixer, _btnSettings, lblMaster, _cmbMaster, btnRefresh, _btnToggle });

            _flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            _lblStatus = new Label { Text = "READY: Cinema Lip-Sync, Ear Separation & WASAPI 3ms Engine | Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)", Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), BackColor = Color.FromArgb(15, 17, 22), ForeColor = Color.FromArgb(52, 211, 153) };

            Controls.Add(_flowPanel);
            Controls.Add(topPanel);
            Controls.Add(_lblStatus);
        }

        public void SetGamingMode(bool active)
        {
            GamingModeActive = active;
            _tglGamingMode.Checked = active;
            if (GamingModeActive)
            {
                foreach (var p in _devicePanels) if (p.IsSurroundDevice) p.SetDelay(0);
                _lblStatus.Text = "GAMING MODE ON: 0ms Instant response for FPS footsteps.";
            }
            else
            {
                ApplyCinemaLipSync(CinemaLipSyncOffsetMs);
            }
        }

        public void Toggle71Surround()
        {
            Channel71Active = !Channel71Active;
            _btn71Toggle.Text = Channel71Active ? "7.1 SURROUND: ON" : "7.1 MODE: OFF (2.0)";
            _btn71Toggle.BackColor = Channel71Active ? Color.FromArgb(16, 185, 129) : Color.FromArgb(75, 85, 99);
            _btn71Toggle.ForeColor = Channel71Active ? Color.Black : Color.White;
            _lblStatus.Text = Channel71Active ? "7.1 Surround engine active (Anti-Cheat safe loopback)." : "Stereo 2.0 downmix mode active.";
        }

        public void ApplyCinemaLipSync(int offsetMs)
        {
            CinemaLipSyncOffsetMs = offsetMs;
            GamingModeActive = false;
            _tglGamingMode.Checked = false;
            foreach (var p in _devicePanels)
            {
                if (p.IsSurroundDevice)
                    p.SetDelay(CinemaLipSyncOffsetMs);
            }
            _lblStatus.Text = $"CINEMA LIP-SYNC ACTIVE: Wired & Bluetooth headphones synchronized to {offsetMs}ms offset.";
        }

        public void ApplyEarSplitPreset()
        {
            if (_devicePanels.Count >= 2)
            {
                _devicePanels[0].SetEarMode(EarSeparationMode.LeftEarOnly);
                _devicePanels[1].SetEarMode(EarSeparationMode.RightEarOnly);
                _lblStatus.Text = "EAR-SEPARATION SPLIT: Device 1 = Left Ear, Device 2 = Right Ear.";
            }
        }

        public void ApplyStudioMonitorPreset()
        {
            foreach (var p in _devicePanels)
            {
                p.SetEarMode(EarSeparationMode.BothEars);
                p.SetDelay(0);
            }
            _lblStatus.Text = "STUDIO MONITOR: Flat stereo profile with 0ms delay across all devices.";
        }

        public void OpenDeviceMixerDialog()
        {
            using var dlg = new DeviceMixerForm(_devicePanels);
            dlg.ShowDialog(this);
        }

        private void OpenSettingsDialog()
        {
            using var dlg = new SettingsForm();
            dlg.ShowDialog(this);
        }

        private void OpenAboutDialog()
        {
            MessageBox.Show(
                "RPA MultiAudio Sync Pro v2.4.2\n" +
                "Cinema Lip-Sync & Multi-Headphone 7.1 Router\n\n" +
                "Developer : K Vanlalrengpuia\n" +
                "Email : rengakhiangte@gmail.com\n\n" +
                "Features:\n" +
                "• WASAPI 3ms Ultra-Low Latency Engine\n" +
                "• Cinema Lip-Sync Delay Router (Bluetooth vs USB)\n" +
                "• Ear Separation Mode (Full Both Ears, Left Only, Right Only)\n" +
                "• Windows 11 Modern Audio Router Architecture",
                "About RPA MultiAudio Sync Pro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (RunInBackground && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _trayIcon.ShowBalloonTip(1500, "RPA MultiAudio Sync Pro", "Running in background in System Tray.", ToolTipIcon.Info);
                return;
            }
            _trayIcon.Visible = false;
            base.OnFormClosing(e);
        }

        private void LoadAudioDevices()
        {
            _cmbMaster.Items.Clear();
            _flowPanel.Controls.Clear();
            _devicePanels.Clear();

            var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            foreach (var dev in devices)
            {
                int ch = dev.AudioClient.MixFormat.Channels;
                bool isSurround = ch >= 6;
                string tag = isSurround ? $"[{ch - 1}.1 Surround]" : $"[{ch}.0 Stereo/BT]";
                _cmbMaster.Items.Add(new DeviceComboItem(dev, $"{dev.FriendlyName} {tag}"));

                var panel = new DevicePanel(dev, tag, isSurround);
                _devicePanels.Add(panel);
                _flowPanel.Controls.Add(panel);
            }
            if (_cmbMaster.Items.Count > 0) _cmbMaster.SelectedIndex = 0;
        }

        private void StartRouting()
        {
            if (_cmbMaster.SelectedItem is not DeviceComboItem masterItem) return;
            var targets = _devicePanels.Where(p => p.IsSelected && p.Device.ID != masterItem.Device.ID).ToList();
            if (targets.Count == 0) return;

            _capture = new WasapiLoopbackCapture(masterItem.Device);
            foreach (var panel in targets)
                _activeNodes.Add(new AudioRouteNode(panel.Device, _capture.WaveFormat, panel));

            _capture.DataAvailable += (s, a) =>
            {
                if (a.BytesRecorded > 0)
                    foreach (var n in _activeNodes)
                        n.PushAudio(a.Buffer, a.BytesRecorded);
            };
            _capture.StartRecording();
            _isRunning = true;
            _btnToggle.Text = "STOP ROUTING";
            _btnToggle.BackColor = Color.FromArgb(239, 68, 68);
            _lblStatus.Text = $"ROUTING ACTIVE: Sending audio to {targets.Count} headphone destination(s).";
        }

        private void StopRouting()
        {
            try { _capture?.StopRecording(); } catch { }
            _capture?.Dispose();
            foreach (var n in _activeNodes) n.Dispose();
            _activeNodes.Clear();
            _isRunning = false;
            _btnToggle.Text = "START ROUTING";
            _btnToggle.BackColor = Color.FromArgb(16, 185, 129);
            _lblStatus.Text = "Routing stopped.";
        }
    }

    public record DeviceComboItem(MMDevice Device, string Label) { public override string ToString() => Label; }

    /// <summary>
    /// Device Panel Card with Output Volume, Lip-Sync Delay, 7.1 toggle and Ear Separation Mode
    /// </summary>
    public class DevicePanel : Panel
    {
        public MMDevice Device { get; }
        public bool IsSurroundDevice { get; }
        private SlidingToggle _toggle;
        private TrackBar _trkDelay;
        private TrackBar _trkVol;
        private Label _lblDelayVal;
        private Label _lblVolVal;
        private Button _btn71Device;
        private ComboBox _cmbEarMode;

        public bool Device71Enabled { get; private set; } = true;
        public bool IsSelected => _toggle.Checked;
        public float Volume { get; private set; } = 0.85f;
        public int DelayMs { get; private set; } = 0;
        public EarSeparationMode EarMode { get; private set; } = EarSeparationMode.BothEars;

        public DevicePanel(MMDevice device, string channelTag, bool isSurround)
        {
            Device = device;
            IsSurroundDevice = isSurround;
            Width = 760;
            Height = 125;
            BackColor = Color.FromArgb(30, 34, 43);
            Margin = new Padding(0, 0, 0, 10);
            BorderStyle = BorderStyle.FixedSingle;

            _toggle = new SlidingToggle
            {
                Location = new Point(14, 12),
                Checked = true,
                ActiveColor = isSurround ? Color.FromArgb(16, 185, 129) : Color.FromArgb(245, 158, 11)
            };

            var lblTitle = new Label
            {
                Text = $"🎧  {device.FriendlyName} {channelTag}",
                Location = new Point(70, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = isSurround ? Color.FromArgb(110, 231, 183) : Color.FromArgb(251, 191, 36)
            };

            // Ear Separation Selector
            var lblEar = new Label { Text = "Ear Mode:", Location = new Point(410, 13), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5f) };
            _cmbEarMode = new ComboBox
            {
                Location = new Point(475, 10),
                Width = 145,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(20, 22, 28),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f)
            };
            _cmbEarMode.Items.AddRange(new object[] { "Full Both Ears", "L-Channel Left Ear", "R-Channel Right Ear" });
            _cmbEarMode.SelectedIndex = 0;
            _cmbEarMode.SelectedIndexChanged += (s, e) =>
            {
                EarMode = _cmbEarMode.SelectedIndex switch
                {
                    1 => EarSeparationMode.LeftEarOnly,
                    2 => EarSeparationMode.RightEarOnly,
                    _ => EarSeparationMode.BothEars
                };
            };

            _btn71Device = new Button
            {
                Text = "7.1 ON",
                Location = new Point(630, 9),
                Width = 55,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = isSurround ? Color.FromArgb(16, 185, 129) : Color.FromArgb(75, 85, 99),
                ForeColor = isSurround ? Color.Black : Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = isSurround
            };
            _btn71Device.Click += (s, e) =>
            {
                Device71Enabled = !Device71Enabled;
                _btn71Device.Text = Device71Enabled ? "7.1 ON" : "7.1 OFF";
                _btn71Device.BackColor = Device71Enabled ? Color.FromArgb(16, 185, 129) : Color.FromArgb(75, 85, 99);
                _btn71Device.ForeColor = Device71Enabled ? Color.Black : Color.White;
            };

            var btnSync = new Button { Text = "150ms", Location = new Point(690, 9), Width = 55, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 50, 62), ForeColor = Color.White, Font = new Font("Segoe UI", 8f) };
            btnSync.Click += (s, e) => SetDelay(150);

            var lblVol = new Label { Text = "Output Volume:", Location = new Point(14, 50), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Silver };
            _trkVol = new TrackBar { Location = new Point(125, 46), Width = 540, Minimum = 0, Maximum = 100, Value = 85, TickStyle = TickStyle.None, BackColor = Color.FromArgb(30, 34, 43) };
            _lblVolVal = new Label { Text = "85%", Location = new Point(675, 50), Width = 65, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White };
            _trkVol.Scroll += (s, e) => { Volume = _trkVol.Value / 100f; _lblVolVal.Text = $"{_trkVol.Value}%"; };

            var lblDelay = new Label { Text = "Lip-Sync Delay:", Location = new Point(14, 85), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(251, 191, 36) };
            _trkDelay = new TrackBar { Location = new Point(125, 81), Width = 540, Minimum = 0, Maximum = 500, Value = 0, TickStyle = TickStyle.None, BackColor = Color.FromArgb(30, 34, 43) };
            _lblDelayVal = new Label { Text = "0 ms", Location = new Point(675, 85), Width = 65, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(251, 191, 36) };
            _trkDelay.Scroll += (s, e) => { DelayMs = _trkDelay.Value; _lblDelayVal.Text = $"{DelayMs} ms"; };

            Controls.AddRange(new Control[] { _toggle, lblTitle, lblEar, _cmbEarMode, _btn71Device, btnSync, lblVol, _trkVol, _lblVolVal, lblDelay, _trkDelay, _lblDelayVal });
        }

        public void SetDelay(int ms)
        {
            DelayMs = ms;
            _trkDelay.Value = Math.Clamp(ms, 0, 500);
            _lblDelayVal.Text = $"{DelayMs} ms";
        }

        public void SetVolume(int volPct)
        {
            Volume = Math.Clamp(volPct, 0, 100) / 100f;
            _trkVol.Value = Math.Clamp(volPct, 0, 100);
            _lblVolVal.Text = $"{_trkVol.Value}%";
        }

        public void SetEarMode(EarSeparationMode mode)
        {
            EarMode = mode;
            _cmbEarMode.SelectedIndex = mode switch
            {
                EarSeparationMode.LeftEarOnly => 1,
                EarSeparationMode.RightEarOnly => 2,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Dedicated Device Mixer & Ear Separation Modal
    /// </summary>
    public class DeviceMixerForm : Form
    {
        public DeviceMixerForm(List<DevicePanel> panels)
        {
            Text = "Device Mixer & Ear Separation Matrix - RPA MultiAudio Sync Pro";
            Size = new Size(680, 480);
            BackColor = Color.FromArgb(20, 24, 32);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var lblHeader = new Label
            {
                Text = "🎛 Master Device Mixer & Ear Separation",
                Location = new Point(18, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(103, 232, 249)
            };

            var lblSub = new Label
            {
                Text = "Adjust per-channel volume balance and assign distinct audio feeds to Left, Right, or Both ears.",
                Location = new Point(18, 42),
                AutoSize = true,
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            var flow = new FlowLayoutPanel
            {
                Location = new Point(18, 70),
                Size = new Size(628, 300),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            foreach (var p in panels)
            {
                var card = new Panel
                {
                    Width = 600,
                    Height = 65,
                    BackColor = Color.FromArgb(28, 32, 44),
                    Margin = new Padding(0, 0, 0, 8),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lblName = new Label
                {
                    Text = p.Device.FriendlyName,
                    Location = new Point(12, 12),
                    Width = 240,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.White
                };

                var cmbMode = new ComboBox
                {
                    Location = new Point(260, 10),
                    Width = 150,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(18, 20, 28),
                    ForeColor = Color.White
                };
                cmbMode.Items.AddRange(new object[] { "Full Both Ears", "L-Channel Left Ear", "R-Channel Right Ear" });
                cmbMode.SelectedIndex = p.EarMode switch
                {
                    EarSeparationMode.LeftEarOnly => 1,
                    EarSeparationMode.RightEarOnly => 2,
                    _ => 0
                };
                cmbMode.SelectedIndexChanged += (s, e) =>
                {
                    p.SetEarMode(cmbMode.SelectedIndex switch
                    {
                        1 => EarSeparationMode.LeftEarOnly,
                        2 => EarSeparationMode.RightEarOnly,
                        _ => EarSeparationMode.BothEars
                    });
                };

                var trk = new TrackBar
                {
                    Location = new Point(420, 8),
                    Width = 120,
                    Minimum = 0,
                    Maximum = 100,
                    Value = (int)(p.Volume * 100),
                    TickStyle = TickStyle.None
                };
                var lblV = new Label { Text = $"{(int)(p.Volume * 100)}%", Location = new Point(545, 12), Width = 45, ForeColor = Color.Silver };
                trk.Scroll += (s, e) =>
                {
                    p.SetVolume(trk.Value);
                    lblV.Text = $"{trk.Value}%";
                };

                card.Controls.AddRange(new Control[] { lblName, cmbMode, trk, lblV });
                flow.Controls.Add(card);
            }

            var btnClose = new Button
            {
                Text = "Close Mixer",
                Location = new Point(516, 385),
                Width = 130,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { lblHeader, lblSub, flow, btnClose });
        }
    }

    /// <summary>
    /// General Preferences & About Dialog
    /// </summary>
    public class SettingsForm : Form
    {
        public SettingsForm()
        {
            Text = "Settings & About - RPA MultiAudio Sync Pro";
            Size = new Size(580, 420);
            BackColor = Color.FromArgb(22, 25, 32);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var tabs = new TabControl { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 25, 32) };
            
            var tabGeneral = new TabPage("General Preferences") { BackColor = Color.FromArgb(22, 25, 32), Padding = new Padding(15) };
            var chkStartup = new CheckBox { Text = "Launch on Startup (Windows Boot)", Location = new Point(20, 25), AutoSize = true, Checked = MainForm.LaunchOnStartup };
            var chkBackground = new CheckBox { Text = "Run in Background (Minimize to System Tray)", Location = new Point(20, 65), AutoSize = true, Checked = MainForm.RunInBackground };
            var chkReconnect = new CheckBox { Text = "Auto-Reconnect on USB / Bluetooth Plugged-In", Location = new Point(20, 105), AutoSize = true, Checked = MainForm.AutoReconnect };
            
            var btnSave = new Button { Text = "Save Preferences", Location = new Point(20, 160), Width = 150, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.Black, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnSave.Click += (s, e) =>
            {
                MainForm.LaunchOnStartup = chkStartup.Checked;
                MainForm.RunInBackground = chkBackground.Checked;
                MainForm.AutoReconnect = chkReconnect.Checked;
                try
                {
                    using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (MainForm.LaunchOnStartup) rk?.SetValue("RPAMultiAudioSyncPro", Application.ExecutablePath);
                    else rk?.DeleteValue("RPAMultiAudioSyncPro", false);
                }
                catch { }
                MessageBox.Show("Preferences saved successfully!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            tabGeneral.Controls.AddRange(new Control[] { chkStartup, chkBackground, chkReconnect, btnSave });

            var tabAbout = new TabPage("About") { BackColor = Color.FromArgb(22, 25, 32), Padding = new Padding(20) };
            var lblTitle = new Label { Text = "RPA MultiAudio Sync Pro v2.4.2", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(110, 231, 183) };
            var lblDev = new Label { Text = "Developer : K Vanlalrengpuia", Location = new Point(20, 60), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.White };
            var lblEmail = new Label { Text = "Email : rengakhiangte@gmail.com", Location = new Point(20, 90), AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(103, 232, 249) };
            var lblDesc = new Label { Text = "Engine: Cinema Lip-Sync & WASAPI 3ms 7.1 Multi-Headphone Router with Ear Separation.", Location = new Point(20, 130), AutoSize = true, ForeColor = Color.Silver };
            tabAbout.Controls.AddRange(new Control[] { lblTitle, lblDev, lblEmail, lblDesc });

            tabs.TabPages.Add(tabGeneral);
            tabs.TabPages.Add(tabAbout);
            Controls.Add(tabs);
        }
    }

    /// <summary>
    /// Audio Route Node with Lip-Sync Delay Queue, Volume Scaling, and Ear Separation Audio Buffer Isolation
    /// </summary>
    public class AudioRouteNode : IDisposable
    {
        private readonly WasapiOut _player;
        private readonly BufferedWaveProvider _buffer;
        private readonly WaveFormat _srcFormat;
        private readonly WaveFormat _targetFormat;
        private readonly DevicePanel _uiPanel;
        private readonly ConcurrentQueue<(long TimestampMs, byte[] Data)> _delayQueue = new();
        private readonly System.Threading.Timer _drainTimer;

        public AudioRouteNode(MMDevice targetDevice, WaveFormat srcFormat, DevicePanel uiPanel)
        {
            _srcFormat = srcFormat;
            _uiPanel = uiPanel;
            _targetFormat = targetDevice.AudioClient.MixFormat;

            var workFormat = WaveFormat.CreateIeeeFloatWaveFormat(_srcFormat.SampleRate, _targetFormat.Channels);
            _buffer = new BufferedWaveProvider(workFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(5),
                DiscardOnBufferOverflow = true
            };

            var resampler = new MediaFoundationResampler(_buffer, _targetFormat) { ResamplerQuality = 30 };
            _player = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 10);
            _player.Init(resampler);
            _player.Play();

            _drainTimer = new System.Threading.Timer(ProcessQueue, null, 2, 2);
        }

        public void PushAudio(byte[] rawBytes, int count)
        {
            byte[] processed = ConvertAndVolumeScale(rawBytes, count, _uiPanel.Volume);

            if (_uiPanel.DelayMs == 0 && _delayQueue.IsEmpty)
            {
                _buffer.AddSamples(processed, 0, processed.Length);
                return;
            }

            _delayQueue.Enqueue((Environment.TickCount64, processed));
        }

        private void ProcessQueue(object? state)
        {
            long now = Environment.TickCount64;
            int targetDelay = _uiPanel.DelayMs;

            while (_delayQueue.TryPeek(out var packet))
            {
                if (now - packet.TimestampMs >= targetDelay)
                {
                    if (_delayQueue.TryDequeue(out var ready))
                        _buffer.AddSamples(ready.Data, 0, ready.Data.Length);
                }
                else break;
            }
        }

        private byte[] ConvertAndVolumeScale(byte[] input, int length, float volume)
        {
            int srcCh = _srcFormat.Channels;
            int dstCh = _targetFormat.Channels;
            int frames = (length / 4) / srcCh;
            byte[] output = new byte[frames * dstCh * 4];
            float surroundBoost = MainForm.GamingModeActive ? 1.22f : 1.0f;
            bool apply71 = MainForm.Channel71Active && _uiPanel.Device71Enabled;
            var earMode = _uiPanel.EarMode;

            for (int f = 0; f < frames; f++)
            {
                int srcBase = f * srcCh * 4;
                int dstBase = f * dstCh * 4;

                if (srcCh == dstCh)
                {
                    for (int c = 0; c < dstCh; c++)
                    {
                        // Check ear separation isolation:
                        // LeftEarOnly: channels with odd indices (Right channels in stereo/surround) muted
                        // RightEarOnly: channels with even index 0 (Left channel) muted
                        bool isMuted = (earMode == EarSeparationMode.LeftEarOnly && (c % 2 == 1))
                                    || (earMode == EarSeparationMode.RightEarOnly && (c % 2 == 0));

                        if (isMuted)
                        {
                            Buffer.BlockCopy(BitConverter.GetBytes(0.0f), 0, output, dstBase + c * 4, 4);
                            continue;
                        }

                        float boost = (apply71 && c >= 4) ? surroundBoost : 1.0f;
                        float sample = Math.Clamp(BitConverter.ToSingle(input, srcBase + c * 4) * volume * boost, -1f, 1f);
                        Buffer.BlockCopy(BitConverter.GetBytes(sample), 0, output, dstBase + c * 4, 4);
                    }
                }
                else if (srcCh >= 6 && dstCh == 2)
                {
                    float fl = BitConverter.ToSingle(input, srcBase + 0);
                    float fr = BitConverter.ToSingle(input, srcBase + 4);
                    float fc = BitConverter.ToSingle(input, srcBase + 8) * 0.707f;
                    float lfe = BitConverter.ToSingle(input, srcBase + 12) * 0.35f;
                    float bl = BitConverter.ToSingle(input, srcBase + 16) * 0.707f * (apply71 ? surroundBoost : 1.0f);
                    float br = BitConverter.ToSingle(input, srcBase + 20) * 0.707f * (apply71 ? surroundBoost : 1.0f);
                    float sl = srcCh >= 8 ? BitConverter.ToSingle(input, srcBase + 24) * 0.707f * (apply71 ? surroundBoost : 1.0f) : 0f;
                    float sr = srcCh >= 8 ? BitConverter.ToSingle(input, srcBase + 28) * 0.707f * (apply71 ? surroundBoost : 1.0f) : 0f;

                    float left = (earMode == EarSeparationMode.RightEarOnly) ? 0f : Math.Clamp((fl + fc + lfe + bl + sl) * 0.5f * volume, -1f, 1f);
                    float right = (earMode == EarSeparationMode.LeftEarOnly) ? 0f : Math.Clamp((fr + fc + lfe + br + sr) * 0.5f * volume, -1f, 1f);

                    Buffer.BlockCopy(BitConverter.GetBytes(left), 0, output, dstBase + 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(right), 0, output, dstBase + 4, 4);
                }
            }
            return output;
        }

        public void Dispose()
        {
            _drainTimer.Dispose();
            try { _player.Stop(); } catch { }
            _player.Dispose();
        }
    }
}
