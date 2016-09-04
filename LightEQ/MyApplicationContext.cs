using System;
using System.Drawing;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Generic;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System.Threading.Tasks;
using Q42.HueApi.NET;

namespace LightEQ
{
    public class MyApplicationContext : ApplicationContext
    {
        //Component declarations
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;
        private IHueClient _client;

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
            TrayIconContextMenu.SuspendLayout();

            // 
            // TrayIconContextMenu
            // 
            this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
            this.CloseMenuItem});
            this.TrayIconContextMenu.Name = "TrayIconContextMenu";
            this.TrayIconContextMenu.Size = new Size(153, 70);
            // 
            // CloseMenuItem
            // 
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Size = new Size(152, 22);
            this.CloseMenuItem.Text = "Exit";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
        }

        //hue connection
        public async void InitializeHue()
        {
            string ip = ConfigurationManager.AppSettings["ip"].ToString();
            string key = ConfigurationManager.AppSettings["key"].ToString();

            if (ip.Length == 0 || key.Length == 0)
            {
                await GetConfig();
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
                }
                catch
                {
                    MessageBox.Show("Press thue Hue Bridge push-link button.");
                    ILocalHueClient client = new LocalHueClient(result);
                    var appKey = await client.RegisterAsync("LightEQ", System.Net.Dns.GetHostName());
                    SetConfig("key", appKey);
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
            if(Application.OpenForms.Count == 0)
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
    }
}