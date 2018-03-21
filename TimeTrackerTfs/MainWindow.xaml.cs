using arq.Common.Utilities;
using System.Windows;
using TimeTrackerTfs.BO;
using System.Linq;
using System;
using System.Threading;
using System.Windows.Threading;
using TimeTrackerTfs.Model;
using System.Configuration;
using System.Windows.Media;
using System.Windows.Controls;
using System.Globalization;
using System.Collections.Generic;

namespace TimeTrackerTfs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WorkItemBO _wkBO = new WorkItemBO();
        private WorkItemDTO _wkDTOInProgress = null;
        private bool closeT = false;
        private List<LangDTO> lstLang = new List<LangDTO> { new LangDTO { Code = "en-US", Description = "English" },
                                                         new LangDTO { Code = "pt-BR", Description = "Português-BR" } };
        private ConfigDTO configdto = null;

        public MainWindow()
        {
            loadConfig();
            loadLanguage();
            InitializeComponent();
            cbxLang.ItemsSource = lstLang;
            InitializeControls();
            loadConfig(true);
            new Thread(() => { Start(); }).Start();
            checkVersion();
        }

        #region [Controls]

        private System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private DispatcherTimer timerUpdate = new DispatcherTimer();

        private void InitializeControls()
        {
            InitializeTimer();
            InitializeTray();
        }
        private void InitializeTimer()
        {
            if (configdto == null)
                return;
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, configdto.UpdateCicle);
            dispatcherTimer.Start();

            timerUpdate.Tick += timerUpdate_Tick;
            timerUpdate.Interval = new TimeSpan(0, 0, 30);
            timerUpdate.Start();
        }
        private void InitializeTray()
        {
            System.Windows.Forms.ContextMenuStrip ctx = new System.Windows.Forms.ContextMenuStrip();
            ctx.Items.Add(FindResource("Exit").ToString());
            ctx.ItemClicked += ctx_click;
            ni.ContextMenuStrip = ctx;
            var icon = Application.GetResourceStream(new Uri("Images/timertfs.ico", UriKind.Relative));
            ni.Icon = new System.Drawing.Icon(icon.Stream);
            ni.Click += tray_Click;

            ni.Visible = true;
        }

        private void loadLanguage()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            string lang = configdto == null ? "en-US" : configdto.Language;
            ResourceDictionary dict = new ResourceDictionary();
            switch (lang)
            {
                case "en-US":
                    dict.Source = new Uri("..\\resource\\en-US.xaml",
                                  UriKind.Relative);
                    break;
                case "pt-BR":
                    dict.Source = new Uri("..\\resource\\pt-BR.xaml",
                                       UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("..\\resource\\en-US.xaml",
                                      UriKind.Relative);
                    break;
            }
            this.Resources.MergedDictionaries.Add(dict);
            this.Refresh();
        }
        #endregion
        #region [Control Events]
        private void tray_Click(object sender, EventArgs e)
        {
            var mouse = e as System.Windows.Forms.MouseEventArgs;
            if (mouse.Button == System.Windows.Forms.MouseButtons.Right)
                return;
            this.Show();
        }
        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            processInProgress();
        }
        private void ctx_click(object sender, EventArgs e)
        {
            closeT = true;
            this.Close();
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_wkDTOInProgress == null)
                    return;
                if (lstLog.Items.Count > 20)
                    lstLog.Items.Clear();
                int idleTime = (int)(TimeControl.GetIdleTime() / 1000);
                if (idleTime >= configdto.IdleCicle)
                {
                    lstLog.Items.Add(FindResource("IdleTime").ToString() + ": " + idleTime);
                    btnStatus.Fill = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    btnStatus.Fill = new SolidColorBrush(Colors.LightGreen);
                    _wkDTOInProgress.TimeWorked += configdto.UpdateCicle;
                }

                if (_wkDTOInProgress.TimeWorked >= configdto.SyncCicle)
                {
                    var boWkInProgress = _wkBO.GetValidInProgress();
                    if (boWkInProgress.isValid())
                    {
                        if (boWkInProgress.ObjectList.Count() > 1)
                        {
                            ni.ShowBalloonTip(50000, FindResource("Job").ToString(), FindResource("MultiProgressMsg").ToString(), System.Windows.Forms.ToolTipIcon.Warning);
                            return;
                        }
                        if (_wkDTOInProgress.RemainingWork == 0)
                        {
                            ni.ShowBalloonTip(50000, FindResource("Job").ToString(), FindResource("JobTimeMsg").ToString(), System.Windows.Forms.ToolTipIcon.Warning);
                            return;
                        }
                        double percentTime = _wkDTOInProgress.TimeWorkedPercent;
                        _wkDTOInProgress = boWkInProgress.ObjectList.First();
                        _wkDTOInProgress.CompletedWork += percentTime;
                        var boUpdate = _wkBO.UpdateWorked(_wkDTOInProgress);
                        if (boUpdate.isValid())
                        {
                            _wkDTOInProgress = boUpdate.ObjectList.First();
                            _wkDTOInProgress.TimeWorked = 0;

                        }
                    }
                    new Thread(() => { Start(); }).Start();
                }
                if (_wkDTOInProgress.TimeWorked >= configdto.UpdateCicle * 20)
                    checkVersion();
            }
            catch (Exception ex) { }
        }
        private void Cell_Dgrid_DoubleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                stkWait.Visibility = Visibility.Visible;
                stkWait.Refresh();
                var wk = (sender as DataGridCell).BindingGroup.Items[0] as WorkItemDTO;
                var bo = _wkBO.Play(wk, _wkDTOInProgress);
                if (bo.isValid())
                {
                    _wkDTOInProgress = bo.ObjectList.First();
                    new Thread(() => { Start(); }).Start();
                }

            }
            catch { }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closeT)
                return;
            e.Cancel = true;
            this.Hide();
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (configdto == null)
                    configdto = new ConfigDTO();
                configdto.UserName = Environment.UserName;
                configdto.TfsUrl = txbTfsUrl.Text;
                configdto.Language = (cbxLang.SelectedItem as LangDTO).Code;
                configdto.VersionPath = txbVersionLocation.Text;
                int upTime = 0;
                Int32.TryParse(txbUptime.Text, out upTime);
                if (upTime <= 0)
                    upTime = 60;
                configdto.UpdateCicle = upTime;
                ConfigBO.Save(configdto);
                tabControl.SelectedIndex = 2;
                lstLog.Items.Add(FindResource("SaveSuccessMsg").ToString());
            }
            catch(Exception ex)
            {
                lstLog.Items.Add(ex.Message);
            }
        }
        #endregion

        private void loadConfig(bool controlInit = false)
        {
            var boConfig = ConfigBO.Load();
            if (boConfig == null && controlInit)
                tabControl.SelectedIndex = 3;
            else
            {
                if(boConfig.isValid())
                    configdto = boConfig.ObjectList.First();
                if (controlInit)
                {
                    txbTfsUrl.Text = configdto.TfsUrl;
                    txbUptime.Text = configdto.UpdateCicle.ToString();
                    txbVersionLocation.Text = configdto.VersionPath;
                    cbxLang.SelectedItem = lstLang.Where(t => t.Code == configdto.Language).First();
                    WorkItemBO.TfsUrl = configdto.TfsUrl;
                    VersionBO.VersionFile = configdto.VersionPath;
                }
            }
        }
        private void checkVersion()
        {
            this.Title += " "+ VersionBO.CurrVersion;
            if (VersionBO.HasNewVersion)
                this.Title += " "+ FindResource("NewVersion").ToString();
            this.Refresh();
        }

        private void processInProgress()
        {
            if (_wkDTOInProgress == null)
                return;
            lstInProgress.Items.Clear();
            lstInProgress.Items.Add("Id: " + _wkDTOInProgress.Id);
            lstInProgress.Items.Add(FindResource("Project").ToString()+": " + _wkDTOInProgress.TeamProject);
            lstInProgress.Items.Add(FindResource("Remaining").ToString() + ": " + _wkDTOInProgress.FormattedRemaining);
            lstInProgress.Items.Add(FindResource("Title").ToString() + ": " + _wkDTOInProgress.Title);
            txtTimeWorked.Text = _wkDTOInProgress.FormattedWorkTime;
            txtSyncWorked.Text = _wkDTOInProgress.FormattedWork;
            txtTimeWorked.Refresh();
            lstInProgress.Refresh();
            txtSyncWorked.Refresh();
        }
        
        private void Start()
        {
            var bo = _wkBO.GetToDoInProgress();
            if (bo.isValid())
            {
                var lstWorkItems = bo.ObjectList;
                var inProgress = lstWorkItems.Where(t => t.State == "In Progress" && t.Blocked != "Yes").FirstOrDefault();
                int wkId = 0;
                if (inProgress != null)
                {
                    wkId = inProgress.Id;
                    this.Dispatcher.Invoke(() =>
                    {
                        _wkDTOInProgress = inProgress;
                        if (_wkDTOInProgress.RemainingWork == 0)
                            ni.ShowBalloonTip(50000, FindResource("Job").ToString(), FindResource("JobTimeMsg").ToString(), System.Windows.Forms.ToolTipIcon.Warning);
                        processInProgress();
                    });
                }
                
                var lstTodo = lstWorkItems.Where(t => t.Id != wkId).ToList();
                this.Dispatcher.Invoke(() =>
                {
                    dtgToDo.ItemsSource = lstTodo;
                    stkWait.Visibility = Visibility.Hidden;
                });

            }
        }

        
    }
}
