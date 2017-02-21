using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using TrashNetBeacon.DriverInterface;

namespace TrashNetBeacon
{
    public partial class TrashNetBeacon : Form
    {
        private bool stop { get; set; } = true;

        private string targetProcessName { get; set; }

        private BackgroundWorker backgroundWorker;

        public TrashNetBeacon()
        {
            this.InitializeComponent();
            this.SetIconVisible(false);
            this.WindowState = FormWindowState.Normal;
            this.backgroundWorker = new BackgroundWorker
                                        {
                                            WorkerReportsProgress = true,
                                            WorkerSupportsCancellation = true
                                        };
            this.backgroundWorker.DoWork += this.BackgroundWorkerOnDoWork;
            this.backgroundWorker.ProgressChanged += this.BackgroundWorkerOnProgressChanged;
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            do
            {
                this.SetIconVisible(true);

                using (var driverInstance = this.ResolveSelectedDriver())
                {
                    this.stop = false;
                    var status = driverInstance.GetStatus($"http://{this.textBoxRouterUrl.Text.Trim()}");
                    status = string.IsNullOrWhiteSpace(status) ? "null" : status;

                    this.notifyIcon.ShowBalloonTip(
                        2,
                        $"Status of device {driverInstance.DeviceName} via {driverInstance.DriverName}".Substring(0, 60)
                        + "...",
                        $"{status}",
                        ToolTipIcon.Info);

                    this.DriverProcessCleanup();
                }

                Thread.Sleep(TimeSpan.FromMinutes(1));
                worker.ReportProgress(0, string.Empty);
            }
            while (!worker.CancellationPending && !this.stop);

            if (this.stop)
            {
                this.DriverProcessCleanup();
                this.SetIconVisible(false);
            }
        }

        private void TrashNetBeacon_Load(object sender, EventArgs e)
        {
            this.LoadDriverAssemblies();
            this.textBoxRouterUrl.Text = RegistryHelper.GetRegistrySetting(Constants.RouterUrlRegistryKeyName)
                                         ?? "192.168.1.1";
        }

        private void TrashNetBeacon_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.backgroundWorker.CancelAsync();
            this.DriverProcessCleanup();
            RegistryHelper.SetRegistrySetting(
                new KeyValuePair<string, string>(Constants.RouterUrlRegistryKeyName, this.textBoxRouterUrl.Text.Trim()));
        }

        private void LoadDriverAssemblies()
        {
            try
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var driversAssemblyPaths =
                    Directory.GetFiles(path, "*.dll")
                        .Where(
                            _ =>
                                !_.ToLowerInvariant()
                                    .Contains("TrashNetBeacon.DriverInterface.dll".ToLowerInvariant())
                                && !_.ToLowerInvariant().Contains("WebDriver.dll".ToLowerInvariant())
                                && !_.ToLowerInvariant().Contains("WebDriver.Support".ToLowerInvariant()));

                var driverClasses = new List<Type>();

                foreach (var driversAssemblyPath in driversAssemblyPaths)
                {
                    var driversAssembly = Assembly.LoadFile(driversAssemblyPath);

                    driverClasses.AddRange(
                        driversAssembly.ExportedTypes.Where(_ => _.GetInterfaces().Contains(typeof(IBeaconWebDriver)))
                            .ToList());
                }

                this.comboBoxDrivers.DataSource = driverClasses;
                this.comboBoxDrivers.DisplayMember = "Name";
                this.comboBoxDrivers.DropDownStyle = ComboBoxStyle.DropDownList;

                if (driverClasses.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "WebDrivers not found",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                    return;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "An error has ocurred while resolving WebDrivers",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                return;
            }

            this.btnStart.Enabled = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.backgroundWorker.RunWorkerAsync();
        }

        public void SetIconVisible(bool isVisible)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(
                    (MethodInvoker)delegate
                        {
                            this.WindowState = isVisible ? FormWindowState.Minimized : FormWindowState.Normal;
                            this.ShowInTaskbar = !isVisible;
                            this.notifyIcon.Visible = isVisible;
                        });
            }
            else
            {
                this.WindowState = isVisible ? FormWindowState.Minimized : FormWindowState.Normal;
                this.ShowInTaskbar = !isVisible;
                this.notifyIcon.Visible = isVisible;
            }
        }

        private void DriverProcessCleanup()
        {
            if (!this.backgroundWorker.IsBusy) return;

            // Driver spawned process cleanup has to be done manually
            foreach (var process in
                Process.GetProcesses()
                    .Where(p => p.ProcessName.ToLowerInvariant().Equals(this.targetProcessName.ToLowerInvariant())))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // ignored
                }
            }
        }

        private IBeaconWebDriver ResolveSelectedDriver()
        {
            Type selectedDriver = null;

            this.Invoke((MethodInvoker)delegate { selectedDriver = this.comboBoxDrivers.SelectedItem as Type; });

            var driverInstance = Activator.CreateInstance(selectedDriver) as IBeaconWebDriver;
            this.targetProcessName = driverInstance.DriverProcessName;
            return driverInstance;
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            this.notifyIcon.ContextMenuStrip.Show(Cursor.Position);
        }

        private void notifyContextMenuStrip_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.stop = true;
            this.backgroundWorker.CancelAsync();
        }
    }
}