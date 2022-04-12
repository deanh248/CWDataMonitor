using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DataMonitor
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timerFixedUpdate = new DispatcherTimer();
        private MyCareData data = null;
        private DateTime lastDateMyCareWasAccessedOn = DateTime.MinValue;
        private TimeSpan myCareUpdateIntervalDelay = new TimeSpan(0, 5, 0);
        private readonly System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

        private Mutex mutex;

        public MainWindow()
        {

            bool isNewInstance;
            mutex = new Mutex(true, "CWDataMonitor", out isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show("Data Monitor is already running, Check your Notification Area.");
                App.Current.Shutdown();
                return;
            }

            Storage.Load();

            // https://stackoverflow.com/questions/10230579/easiest-way-to-have-a-program-minimize-itself-to-the-system-tray-using-net-4

            ni.Icon = System.Drawing.Icon.FromHandle(DataMonitor.Properties.Resources.windowIcon.Handle);
            ni.Visible = true;
            ni.Click += (object sender, EventArgs args) => {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                ShowMainPanel();
            };
            ni.ContextMenu = new System.Windows.Forms.ContextMenu();
            ni.ContextMenu.MenuItems.Add("Restore", (object sender, EventArgs e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                ShowMainPanel();
            });

            ni.ContextMenu.MenuItems.Add("Exit", (object sender, EventArgs e) =>
            {
                Storage.Save();
                Application.Current.Shutdown();
            });

            InitializeComponent();

            // Prevent maximize from outside sources.
            this.StateChanged += new System.EventHandler((sender, eventArgs) =>
            {
                if (this.WindowState == System.Windows.WindowState.Maximized)
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                }
            });

            timerFixedUpdate.Tick += timer_FixedUpdateTick;
            timerFixedUpdate.Interval = new TimeSpan(0, 0, 1);
            timerFixedUpdate.Start();

            // Some UI
            chkRunAtStartup.IsChecked = Storage.RunAtStartUp;
            chkResetSessionDaily.IsChecked = Storage.ResetSessionDaily;
            txtUsername.Text = Storage.Username;
        }

        private async void timer_FixedUpdateTick(object sender, EventArgs e)
        {

            // Realtime data counters
            if (Storage.ResetSessionDaily && (Storage.CurrentSessionDateStarted.Date != DateTime.Now.Date))
            {
                Storage.CurrentSessionDateStarted = DateTime.Now;
                Storage.CurrentSessionDataCounter = 0;
                Storage.Save();
            }
            NetworkDataRateWrapper drw = NetworkAdapterHelper.GetDataRate();
            Storage.CurrentSessionDataCounter += drw.BytesSentRate + drw.BytesReceivedRate;
            txtSessionDataRateDown.Text = NetworkAdapterHelper.ParseBytesToReadableFormat(drw.BytesReceivedRate) + "/s";
            txtSessionDataRateUp.Text = NetworkAdapterHelper.ParseBytesToReadableFormat(drw.BytesSentRate) + "/s";
            txtSessionDataCounter.Text = NetworkAdapterHelper.ParseBytesToReadableFormat(Storage.CurrentSessionDataCounter);
            
            // Show settings if we have no account set.
            if (Storage.Password == "" || Storage.Username == "")
            {
                lblMyCareSettingsDetail.Foreground = Brushes.Red;
                ShowSettingsPanel();
                return;
            }

            // Request usage data from mycare?
            if ((DateTime.Now - lastDateMyCareWasAccessedOn).TotalSeconds > myCareUpdateIntervalDelay.TotalSeconds)
            {
                // Unrelated, but necessary.
                Storage.Save();
                NetworkAdapterHelper.RefreshAdapterList();

                lastDateMyCareWasAccessedOn = DateTime.Now;
                data = null;

                lblStatus.Text = "Fetching MyCare Data...";

                if (!MyCare.Token.IsValid())
                {
                    object isLogged = await new MyCareRequestLogin(Storage.Username,Storage.Password).Run();
                    if(isLogged == null)
                    {
                        lblStatus.Text = "ERROR - MyCare is offline?";
                        return;
                    }
                    else if (!((bool)isLogged))
                    {
                        lblStatus.Text = "ERROR - Wrong user/password?";
                        return;
                    }
                }

                object val = await new MyCareRequestDataUsage().Run();
                if (val != null)
                {
                    data = (MyCareData) val;

                    // lblPackageName.Text = String.Format("{0} ({1} GB)",data.packageName,data.packageSize);
                    if (data.packageName == "")
                        lblSmallHeader.Text = String.Format("{0}", "Unknown Package");
                    else
                        lblSmallHeader.Text = String.Format("{0}", data.packageName);
                    
                    if (data.packageRemainingData > -1)
                        lblLargeHeader.Text = String.Format("{0:n0} MB Remaining", data.packageRemainingData,data.packageSize);
                    else
                        lblLargeHeader.Text = String.Format("<Read Error>", data.packageRemainingData, data.packageSize);

                    double percent = data.packageRemainingData / (data.packageSize * 1000);
                    percent = Math.Min(Math.Max(0, percent), 100);
                    lblPercent.Text = String.Format("{0:p0}", percent);
                    SetProgressBar(percent);

                    // TODO: Data updates are delayed and tokens tend to expire before then so
                    // might aswell get rid of it and save time later.
                    MyCare.Token.Value = null;
                }
                else
                {
                    lblStatus.Text = "ERROR - MyCare is offline?";
                }
            }

            if (data == null)
                return;

            TimeSpan timeSinceLastCableUsage = (DateTime.Now - data.packageLastUsageTime);
            lblStatus.Text = String.Format("Mycare last update {0}m {1}s ago.", timeSinceLastCableUsage.Minutes, timeSinceLastCableUsage.Seconds);
            //lblSmallHeader.Text = "Broadband";
            //lblStatus.Text = ""
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
            catch { }
        }

        private void SetProgressBar(double val)
        {
            ProgressBarInner.Width = (ProgressBarContainer.ActualWidth / 1) * val;
        }

        private void ShowSettingsPanel()
        {
            if (panelSettings.Visibility == Visibility.Visible)
                return;

            HideMainPanel();
            panelSettings.Visibility = Visibility.Visible;
        }

        private void HideSettingsPanel()
        {
            panelSettings.Visibility = Visibility.Collapsed;
        }

        private void ShowMainPanel()
        {
            if (panelMain.Visibility == Visibility.Visible)
                return;

            HideSettingsPanel();
            panelMain.Visibility = Visibility.Visible;
        }

        private void HideMainPanel()
        {
            panelMain.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void btnSaveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text.Trim() == "" || txtPassword.Password.Trim() == "")
                return;
            
            // Test Account
            btnSaveAccount.IsEnabled = false;
            object response = await new MyCareRequestLogin(txtUsername.Text, txtPassword.Password).Run();
            btnSaveAccount.IsEnabled = true;

            if(response == null)
            {
                MessageBox.Show("MyCare is not communicating properly, This application might be out-of-date.", "", MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            else if (!((bool) response))
            {
                Console.WriteLine("Wrong username or password.");
                MessageBox.Show("Could not login to MyCare. Check your username and password.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            lblMyCareSettingsDetail.Foreground = Brushes.Black;
            Storage.Username = txtUsername.Text;
            Storage.Password = txtPassword.Password;
            txtPassword.Password = "";

            // This forces a data usage update request.
            lastDateMyCareWasAccessedOn = DateTime.MinValue;

            ShowMainPanel();
            e.Handled = true;
        }

        bool isFirstPopup = true;
        private void btnMinimize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;

            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();
            if (isFirstPopup)
            ni.ShowBalloonTip(1000, "Data Monitor is Running", "Right click on my notification icon to Exit or Restore.",System.Windows.Forms.ToolTipIcon.Info);
            
            isFirstPopup = false;
            e.Handled = true;
        }

        private void btnSetting_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (panelSettings.Visibility == Visibility.Collapsed)
                ShowSettingsPanel();
            else
                ShowMainPanel();

            e.Handled = true;
        }

        private void btnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult mr = MessageBox.Show("Would you like to exit?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            switch (mr)
            {
                case MessageBoxResult.Yes:
                    ni.Visible = false;
                    Application.Current.Shutdown();
                break;
            }
        }

        private void btnResetSession_Click(object sender, RoutedEventArgs e)
        {
            Storage.CurrentSessionDataCounter = 0;
            Storage.CurrentSessionDateStarted = DateTime.MinValue;
            Storage.Save();
            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {

        }

        private void chkRunAtStartup_Checked(object sender, RoutedEventArgs e)
        {
            Storage.RunAtStartUp = true;
            e.Handled = true;
        }

        private void chkResetSessionDaily_Checked(object sender, RoutedEventArgs e)
        {
            Storage.ResetSessionDaily = true;
            e.Handled = true;
        }

        private void chkResetSessionDaily_UnChecked(object sender, RoutedEventArgs e)
        {
            Storage.ResetSessionDaily = false;
            e.Handled = true;
        }

        private void chkRunAtStartup_UnChecked(object sender, RoutedEventArgs e)
        {
            Storage.RunAtStartUp = false;
            e.Handled = true;
        }

        private void btnForgetMe_Click(object sender, RoutedEventArgs e)
        {
            Storage.Username = "";
            Storage.Password = "";
            txtUsername.Text = "";
            txtPassword.Password = "";

            MessageBox.Show("Your locally stored data has been cleared.");
        }
    }
}
