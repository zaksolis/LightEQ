using System;
using System.Drawing;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Generic;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System.Threading.Tasks;
using Q42.HueApi.NET;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using CSCore.DSP;
using CSCore.Streams;
using CSCore;
using CSCore.SoundIn;
using CSCore.Codecs.WAV;

namespace LightEQ
{
    public class MyApplicationContext : ApplicationContext
    {
        //Component declarations
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;
        private ToolStripMenuItem LightsOnMenuItem;
        private ToolStripMenuItem LightsOffMenuItem;
        private IHueClient _client;

        //vars
        public string HueKey = ConfigurationManager.AppSettings["key"].ToString();

        public MyApplicationContext()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            InitializeComponent();
            TrayIcon.Visible = true;
            InitializeHue();

        }

        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon();

            //TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            //TrayIcon.BalloonTipText =
            //  "I noticed that you double-clicked me! What can I do for you?";
            //TrayIcon.BalloonTipTitle = "You called Master?";
            TrayIcon.Text = "LightEQ";


            //The icon is added to the project resources.
            //Here I assume that the name of the file is 'TrayIcon.ico'
            TrayIcon.Icon = new Icon("LightEQ_ico.ico");

            //Optional - handle doubleclicks on the icon:
            TrayIcon.DoubleClick += TrayIcon_DoubleClick;


            //Optional - Add a context menu to the TrayIcon:
            TrayIconContextMenu = new ContextMenuStrip();
            CloseMenuItem = new ToolStripMenuItem();
            LightsOnMenuItem = new ToolStripMenuItem();
            LightsOffMenuItem = new ToolStripMenuItem();
            TrayIconContextMenu.SuspendLayout();

            // 
            // TrayIconContextMenu
            // 
            this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
            this.CloseMenuItem, this.LightsOnMenuItem, this.LightsOffMenuItem});
            this.TrayIconContextMenu.Name = "TrayIconContextMenu";
            this.TrayIconContextMenu.Size = new Size(153, 70);

            // 
            // CloseMenuItem
            // 
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Size = new Size(152, 22);
            this.CloseMenuItem.Text = "Exit";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            // 
            // LightsOnMenuItem
            // 
            this.LightsOnMenuItem.Name = "LightsOnMenuItem";
            this.LightsOnMenuItem.Size = new Size(152, 22);
            this.LightsOnMenuItem.Text = "Lights on";
            this.LightsOnMenuItem.Click += new EventHandler(this.LightsOnMenuItem_Click);

            // 
            // LightsOffMenutem
            // 
            this.LightsOffMenuItem.Name = "LightsOffMenuItem";
            this.LightsOffMenuItem.Size = new Size(152, 22);
            this.LightsOffMenuItem.Text = "Lights off";
            this.LightsOffMenuItem.Click += new EventHandler(this.LightsOffMenuItem_Click);

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
        }

        //hue connection
        public async void InitializeHue()
        {
            string ip = ConfigurationManager.AppSettings["ip"].ToString();
            string key = ConfigurationManager.AppSettings["key"].ToString();
            HueKey = key;

            if (ip.Length == 0 || key.Length == 0)
            {
                await GetConfig();
            }
            while (key.Length == 0)
            {
                //do nothing
            }
            _client = new LocalHueClient(ip, key);
        }

        public async Task GetConfig()
        {
            var result = await FindBridge();

            SetConfig("ip", result);

            //user not set up
            if (ConfigurationManager.AppSettings["key"].Length == 0)
            {
                try
                {
                    ILocalHueClient client = new LocalHueClient(result);
                    var appKey = await client.RegisterAsync("LightEQ", System.Net.Dns.GetHostName());
                    SetConfig("key", appKey);
                    HueKey = appKey;
                }
                catch
                {
                    MessageBox.Show("Press thue Hue Bridge push-link button.");
                    ILocalHueClient client = new LocalHueClient(result);
                    var appKey = await client.RegisterAsync("LightEQ", System.Net.Dns.GetHostName());
                    SetConfig("key", appKey);
                    HueKey = appKey;
                }


                //Save the app key for later use
            }
        }

        async Task<string> FindBridge()
        {
            IBridgeLocator locator = new HttpBridgeLocator();

            //For Windows 8 and .NET45 projects you can use the SSDPBridgeLocator which actually scans your network. 
            //See the included BridgeDiscoveryTests and the specific .NET and .WinRT projects
            IEnumerable<string> bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            //foreach (var bridgeIp in bridgeIPs)
            //{
            //    MessageBox.Show(bridgeIp);
            //}
            if (bridgeIPs != null)
            {
                foreach (string ip in bridgeIPs)
                {
                    return ip;
                }
            }
            return "none";
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            TrayIcon.Visible = false;
        }

        public void SetConfig(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);
        }

        public void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            //Here you can do stuff if the tray icon is doubleclicked
            //TrayIcon.ShowBalloonTip(10000);
            MainForm = new Form1();
            //is form open?
            if (Application.OpenForms.Count == 0)
            {
                MainForm.ShowDialog();
            }
            //focus if open
            else
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f.Modal)
                    {
                        f.WindowState = FormWindowState.Normal;
                        f.Focus();
                        f.BringToFront();
                    }
                }
            }
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Close LightEQ?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void LightsOnMenuItem_Click(object sender, EventArgs e)
        {
            var command = new LightCommand();
            command.On = true;
            //command.Effect = Effect.ColorLoop;
            string debugHex = Prompt.ShowDialog("Enter Hex Value", "Debug");
            bool pass = false;
            while (pass == false)
            {
                try
                {
                    command.TurnOn().SetColor(new RGBColor(debugHex));
                    pass = true;
                }
                catch
                {
                    debugHex = Prompt.ShowDialog("Enter Valid Hex Value", "Debug");
                }

            }
            _client.SendCommandAsync(command);
        }

        private void LightsOffMenuItem_Click(object sender, EventArgs e)
        {
            var command = new LightCommand();
            command.On = false;
            _client.SendCommandAsync(command);
        }

        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                Form prompt = new Form()
                {
                    Width = 500,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen
                };
                Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
                TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
                Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }

        public class SoundCapture
        {

            public int numBars = 30;

            public int minFreq = 5;
            public int maxFreq = 4500;
            public int barSpacing = 0;
            public bool logScale = true;
            public bool isAverage = false;

            public float highScaleAverage = 2.0f;
            public float highScaleNotAverage = 3.0f;

            WasapiCapture capture;
            WaveWriter writer;
            FftSize fftSize;
            float[] fftBuffer;

            SingleBlockNotificationStream notificationSource;

            IWaveSource finalSource;

            public SoundCapture()
            {

                // This uses the wasapi api to get any sound data played by the computer
                capture = new WasapiLoopbackCapture();

                capture.Initialize();

                // Get our capture as a source
                IWaveSource source = new SoundInSource(capture);


                // From https://github.com/filoe/cscore/blob/master/Samples/WinformsVisualization/Form1.cs

                // This is the typical size, you can change this for higher detail as needed
                fftSize = FftSize.Fft4096;

                // Actual fft data
                fftBuffer = new float[(int)fftSize];


                // Tells us when data is available to send to our spectrum
                var notificationSource = new SingleBlockNotificationStream(source.ToSampleSource());

                notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;

                // We use this to request data so it actualy flows through (figuring this out took forever...)
                finalSource = notificationSource.ToWaveSource();

                capture.DataAvailable += Capture_DataAvailable;
                capture.Start();
            }
            private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
            {
                finalSource.Read(e.Data, e.Offset, e.ByteCount);
            }

            private void NotificationSource_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
            {
                //do something
                //spectrumProvider.Add(e.Left, e.Right);
            }
            ~SoundCapture()
            {
                capture.Stop();
                capture.Dispose();
            }
            public float[] barData = new float[20];

            public float[] GetFFtData()
            {
                lock (barData)
                {
                    //lineSpectrum.BarCount = numBars;
                    if (numBars != barData.Length)
                    {
                        barData = new float[numBars];
                    }
                }

                //if (spectrumProvider.IsNewDataAvailable)
                //{
                //    lineSpectrum.MinimumFrequency = minFreq;
                //    lineSpectrum.MaximumFrequency = maxFreq;
                //    lineSpectrum.IsXLogScale = logScale;
                //    lineSpectrum.BarSpacing = barSpacing;
                //    lineSpectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                //    return lineSpectrum.GetSpectrumPoints(100.0f, fftBuffer);
                //}
                //else
                //{
                //    return null;
                //}
                return null;
            }

            public void ComputeData()
            {


                float[] resData = GetFFtData();

                int numBars = barData.Length;

                if (resData == null)
                {
                    return;
                }

                lock (barData)
                {
                    for (int i = 0; i < numBars && i < resData.Length; i++)
                    {
                        // Make the data between 0.0 and 1.0
                        barData[i] = resData[i] / 100.0f;
                    }

                    for (int i = 0; i < numBars && i < resData.Length; i++)
                    {
                        //if (lineSpectrum.UseAverage)
                        //{
                        //    // Scale the data because for some reason bass is always loud and treble is soft
                        //    barData[i] = barData[i] + highScaleAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                        //}
                        //else
                        //{
                        barData[i] = barData[i] + highScaleNotAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                        //}
                    }
                }
            }
        }
    }

    }