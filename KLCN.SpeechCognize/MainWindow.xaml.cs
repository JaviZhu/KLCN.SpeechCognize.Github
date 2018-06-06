using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace KLCN.SpeechCognize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread thd;
        public MainWindow()
        {
            InitializeComponent();
            CheckMultiInstance();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += timer_Tick;
            this.Left = SystemParameters.WorkArea.Width - this.Width;
            this.EndTop = SystemParameters.WorkArea.Height - this.Height;
            this.Top = SystemParameters.WorkArea.Height;
            this.Visibility = Visibility.Hidden;


            thd = new Thread(CheckTime);
            thd.Start();
        }
        private DispatcherTimer timer;
        public double EndTop { get; set; }
        public delegate void DelShowWin();
        public DelShowWin dsw;
        private void ShowWin()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.Visibility = Visibility.Visible;
                timer.Start();
            }));
        }
        void timer_Tick(object sender, EventArgs e)
        {
            while (this.Top > EndTop)
            {
                this.Top -= 0.01;
            }
        }
        void CheckTime()
        {
            while (true)
            {
                //if (DateTime.Now.Second == 10)
                {
                    dsw = new DelShowWin(ShowWin);
                    dsw.Invoke();
                }
                GC.Collect();
                Thread.Sleep(1 * 1000);
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
        private void CheckMultiInstance()
        {
            var mainPro = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            var listPro = System.Diagnostics.Process.GetProcessesByName(mainPro);
            if (listPro.Length > 1)
            {
                listPro[1].Kill();
                MessageBox.Show("the program is already running,do not click again.");
            }
        }
    }
}
