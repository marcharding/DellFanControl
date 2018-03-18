using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace DellFanControl
{

    public static class Global
    {

        public enum ACTION : int
        {
            NONE = 0,
            INIT = 1,
            ENABLE = 2,
            DISABLE = 3,
            SUSPEND = 5,
            RESUME = 7,
            EXIT = 9
        }

    }

    static class Program
    {

        private static DellFanControlApplicationContext appContext;

        [STAThread]
        public static void Main()
        {
            // confirm warning (just on time)
            if (!File.Exists(".confirmed"))
            {
                var confirmResult = MessageBox.Show(
                    String.Join(
                        Environment.NewLine,
                        "This programm takes over the DELL fan conrol",
                        "and disables the internal thermal fan management!",
                        "",
                        "Use at your own risk and control the temperatures",
                        "with an external tool.",
                        "",
                        "The driver will ONLY be unloaded on a hard reset!"
                    ),
                    "Caution!",
                    MessageBoxButtons.OKCancel
                );

                if (confirmResult == DialogResult.Cancel)
                {
                    Program.Quit();
                    return;
                }

                File.Create(".confirmed");
            }

            // all other exceptions
            Application.ThreadException += new ThreadExceptionEventHandler(Program.OnThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Program.OnUnhandledException);

            // see https://stackoverflow.com/a/10579614, must be called before Appplication.Run();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Program.OnApplicationExit);
            Application.ApplicationExit += new EventHandler(Program.OnApplicationExit);
            SystemEvents.SessionEnding += new SessionEndingEventHandler(Program.OnSessionEnding);

            // see https://stackoverflow.com/a/406473
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            appContext = new DellFanControlApplicationContext();
            Application.Run(appContext);
        }

        public static void Quit()
        {
            Application.Exit();
        }

        public static void OnApplicationExit(object sender, EventArgs e)
        {
            // Remove custom fan control on Logout/Shutdown
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

        public static void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Remove custom fan control on Logout/Shutdown
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            Thread.Sleep(1500);
        }

        static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

    }

    public class DellFanControlApplicationContext : ApplicationContext
    {

        public NotifyIcon trayIcon;
        public Thread notifyThread;
        public int nextAction;
        public Boolean driverRunning = true;

        public Dictionary<string, int> config = new Dictionary<string, int>()
        {
            {"pollingInterval", 1000}, // milliseconds
            {"minCooldownTime", 120}, // seconds
            {"FanOneActive", 1},
            {"FanOneCPUTemperatureThresholdZero", 45},
            {"FanOneCPUTemperatureThresholdOne", 50},
            {"FanOneCPUTemperatureThresholdTwo", 65},
            {"FanOneGPUemperatureThresholdZero", 45},
            {"FanOneGPUemperatureThresholdOne", 50},
            {"FanOneGPUTemperatureThresholdTwo", 65},
            {"FanTwoActive", 1},
            {"FanTwoCPUTemperatureThresholdZero", 45},
            {"FanTwoCPUTemperatureThresholdOne", 50},
            {"FanTwoCPUTemperatureThresholdTwo", 65},
            {"FanTwoGPUTemperatureThresholdZero", 45},
            {"FanTwoGPUTemperatureThresholdOne", 50},
            {"FanTwoGPUTemperatureThresholdTwo", 65},
        };

        public DellFanControlApplicationContext()
        {
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnPowerModeChanged);

            // Initialize tray icon
            this.trayIcon = new NotifyIcon()
            {
                Icon = global::DellFanControl.Properties.Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem ("Enable Custom Fan Control", this.ContextMenuActionEnableFanControl),
                new MenuItem ("Disable Custom Fan Control", this.ContextMenuActionDisableFanControl),
                new MenuItem ("Exit", this.Exit),
                }),
                Visible = true
            };

            // Read config.xml
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.xml");
                this.config["pollingInterval"] = Int32.Parse(doc.DocumentElement.Attributes["pollingInterval"].Value);
                this.config["minCooldownTime"] = Int32.Parse(doc.DocumentElement.Attributes["minCooldownTime"].Value);
                this.config["fanOneActive"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne").Attributes["active"].Value);
                this.config["fanTwoActive"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo").Attributes["active"].Value);
                this.config["FanOneCPUTemperatureThresholdZero"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdZero").Attributes["CPU"].Value);
                this.config["FanOneCPUTemperatureThresholdOne"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdOne").Attributes["CPU"].Value);
                this.config["FanOneCPUTemperatureThresholdTwo"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdTwo").Attributes["CPU"].Value);
                this.config["FanOneGPUTemperatureThresholdZero"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdZero").Attributes["GPU"].Value);
                this.config["FanOneGPUTemperatureThresholdOne"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdOne").Attributes["GPU"].Value);
                this.config["FanOneGPUTemperatureThresholdTwo"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdTwo").Attributes["GPU"].Value);
                this.config["FanTwoCPUTemperatureThresholdZero"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdZero").Attributes["CPU"].Value);
                this.config["FanTwoCPUTemperatureThresholdOne"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdOne").Attributes["CPU"].Value);
                this.config["FanTwoCPUTemperatureThresholdTwo"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdTwo").Attributes["CPU"].Value);
                this.config["FanTwoGPUTemperatureThresholdZero"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdZero").Attributes["GPU"].Value);
                this.config["FanTwoGPUTemperatureThresholdOne"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdOne").Attributes["GPU"].Value);
                this.config["FanTwoGPUTemperatureThresholdTwo"] = Int32.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdTwo").Attributes["GPU"].Value);
            }
            catch (Exception)
            {
                this.trayIcon.BalloonTipText = "Could not load config.xml, using default values.";
                this.trayIcon.ShowBalloonTip(5000);
            }
            
            // Start driver
            Thread notifyThread = new Thread(
                delegate ()
                {
                    DellFanCtrl app = new DellFanCtrl(this);
                });
            notifyThread.IsBackground = true;
            notifyThread.Start();
        }

        void ContextMenuActionEnableFanControl(object sender, EventArgs e)
        {
            this.nextAction = (int)Global.ACTION.ENABLE;
        }

        void ContextMenuActionDisableFanControl(object sender, EventArgs e)
        {
            this.nextAction = (int)Global.ACTION.DISABLE;
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Program.Quit();
        }

        private void OnPowerModeChanged(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    this.nextAction = (int)Global.ACTION.RESUME;
                    break;
                case PowerModes.Suspend:
                    this.nextAction = (int)Global.ACTION.SUSPEND;
                    break;
            }
        }

    }

}