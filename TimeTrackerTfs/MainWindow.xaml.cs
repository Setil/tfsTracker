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

        private string lang = ConfigurationManager.AppSettings["language"];

        private int tickPeriod
        {
            get
            {
                int time = 0;
                Int32.TryParse(ConfigurationManager.AppSettings["tickPeriodSeconds"], out time);
                if (time > 0)
                    return time;
                return 120;
            }
        }
        public MainWindow()
        {
            loadLanguage();
            InitializeComponent();
            new Thread(() => { Start(); }).Start();
            InitializeTimer();
            InitializeTray();
            checkVersion();
        }

        private void loadLanguage()
        {
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
        }

        private void checkVersion()
        {
            if (VersionBO.HasNewVersion)
                this.Title += " "+ FindResource("NewVersion").ToString();
            this.Refresh();
        }

        private void InitializeTray()
        {
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            System.Windows.Forms.ContextMenuStrip ctx = new System.Windows.Forms.ContextMenuStrip();
            ctx.Items.Add(FindResource("Exit").ToString());
            ctx.ItemClicked += ctx_click;
            ni.ContextMenuStrip = ctx;
            var icon = Application.GetResourceStream(new Uri("Images/timertfs.ico", UriKind.Relative));
            ni.Icon = new System.Drawing.Icon(icon.Stream);
            ni.Click += tray_Click;

            ni.Visible = true;
        }

        private void InitializeTimer()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0,0, tickPeriod);
            dispatcherTimer.Start();
        }

        private void ctx_click(object sender, EventArgs e)
        {
            closeT = true;
            this.Close();
        }

        private void tray_Click(object sender, EventArgs e)
        {
            var mouse = e as System.Windows.Forms.MouseEventArgs;
            if (mouse.Button == System.Windows.Forms.MouseButtons.Right)
                return;
            this.Show();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (lstLog.Items.Count > 20)
                    lstLog.Items.Clear();
                int idleTime = (int) (TimeControl.GetIdleTime() / 1000);
                if (idleTime >= tickPeriod * 6)
                {
                    lstLog.Items.Add(FindResource("IdleTime").ToString()+ ": " + idleTime);
                    btnStatus.Fill = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    btnStatus.Fill = new SolidColorBrush(Colors.LightGreen);
                    _wkDTOInProgress.TimeWorked += tickPeriod;
                }

                if(_wkDTOInProgress.TimeWorked >= tickPeriod*5)
                {
                    var boWkInProgress = _wkBO.GetValidInProgress();
                    if (boWkInProgress.isValid())
                    {
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
                }
                if (_wkDTOInProgress.TimeWorked >= tickPeriod * 20)
                    checkVersion();
                processInProgress();
            }
            catch(Exception ex) { }
        }

        private void processInProgress()
        {
            lstInProgress.Items.Clear();
            lstInProgress.Items.Add("Id: " + _wkDTOInProgress.Id);
            lstInProgress.Items.Add(FindResource("Project").ToString()+": " + _wkDTOInProgress.TeamProject);
            lstInProgress.Items.Add(FindResource("Remaining").ToString() + ": " + _wkDTOInProgress.FormattedRemaining);
            lstInProgress.Items.Add(FindResource("Title").ToString() + ": " + _wkDTOInProgress.Title);
            txtTimeWorked.Text = _wkDTOInProgress.FormattedWorkTime;
            txtTimeWorked.Refresh();
            lstInProgress.Refresh();
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
    }
}
