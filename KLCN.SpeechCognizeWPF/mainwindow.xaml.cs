using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace KLCN.SpeechCognizeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread thd;
        SQLHelper sQLHelper;
        List<Model> list;
        long chkID = 0;
        long beginID = 0;
        Model currentModel = new Model();
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
            sQLHelper = new SQLHelper();
            list = new List<Model>();

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
                Initial();
                if (this.Visibility == Visibility.Hidden && chkID!=beginID)
                {
                    beginID = chkID;

                    dsw = new DelShowWin(ShowWin);
                    dsw.Invoke();
                }

                GC.Collect();
                Thread.Sleep(10 * 1000);
            }
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
        private void Initial()
        {
            DataTable dt = sQLHelper.Query(@"select * from [ApplicationTest].[dbo].[KLCN_Tab_SpeechRecognition] where IsRead=1");
            chkID = Convert.ToInt64(sQLHelper.Query(@"select top 1 * from [ApplicationTest].[dbo].[KLCN_Tab_SpeechCalculate]").Rows[0]["Calculate"].ToString());

            list.Clear();
            if (dt!=null && dt.Rows.Count>0)
            {
                foreach (DataRow m in dt.Rows)
                    list.Add(new Model()
                    {
                        Account = m["Account"].ToString(),
                        AttachmentContentType = m["AttachmentContentType"].ToString(),
                        AttachmentURI = m["AttachmentURI"].ToString(),
                        Content = m["Content"].ToString(),
                        CurrentDirectory = m["CurrentDirectory"].ToString(),
                        Datetime = Convert.ToDateTime(m["Datetime"]).ToString("yy/MM/dd HH:mm:ss"),
                        Email = m["Email"].ToString(),
                        ID = m["ID"].ToString(),
                        MsgId = m["MsgId"].ToString(),
                        PhysicsURI = m["PhysicsURI"].ToString(),
                        Subject = m["Subject"].ToString(),
                        UserobjectId = m["UserobjectId"].ToString(),
                        IsRead = Convert.ToBoolean(m["IsRead"].ToString())
                    });
            }
            this.Dispatcher.Invoke((Action)(() =>
            {
                listView1.ItemsSource = list;
                listView1.Items.Refresh();
            }));
        }
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Opacity = 0.5;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string ID = (sender as Button).CommandParameter.ToString();

            foreach(Model m in list)
            {
                if (m.ID == ID)
                {
                    currentModel = m;
                    break;
                }
            }
            string filepath = @"\\10.23.6.11\c$" + (currentModel.PhysicsURI.Split('.')[0] + ".wav").Split(':')[1];
            //filepath = @"C:\Z_Disk\document\source\KLCN.SpeechCognize\KLCN.SpeechCognizeConsole\bin\Debug\Temp\20170914\cnMaSchmid_111223987.wav";

            SoundPlayer player = new SoundPlayer(filepath);
            player.Load();
            player.Play();
        }
        private string Token
        {
            get
            {
                string username = "Ycnadmin";
                string password = "C1sc0123";
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                return svcCredentials;
            }
        }
        private string SaveAttachment(string AttachmentURI, string UserobjectId,string Account)
        {
            string CurrentDirectory=System.IO.Directory.GetCurrentDirectory();
            string requestUri = string.Format(@"https://10.23.0.107{0}/?userobjectid={1}", AttachmentURI, UserobjectId);
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.ContentType = "application/xml;charset=UTF-8";
            request.Headers["Authorization"] = "Basic " + Token;
            WebProxy wp = new WebProxy();
            request.Proxy = wp;

            string responseString = string.Empty;
            string path = string.Empty;
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream sr = response.GetResponseStream())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] b = null;
                            int count = 0;
                            do
                            {
                                byte[] buf = new byte[1024];
                                count = sr.Read(buf, 0, 1024);
                                ms.Write(buf, 0, count);
                            }
                            while (sr.CanRead && count > 0);
                            b = ms.ToArray();

                            if (!Directory.Exists(CurrentDirectory + "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd")))
                                Directory.CreateDirectory(CurrentDirectory + "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd"));
                            path = CurrentDirectory + "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + Account + "_" + DateTime.Now.ToString("HHmmssfff") + ".wav";
                            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                ms.WriteTo(fs);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            return path;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            #region ignore
            //MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            //if (messageBoxResult == MessageBoxResult.Yes)
            //{
            //    string ID = (sender as Button).CommandParameter.ToString();

            //    Model currentModel = new Model();
            //    foreach (Model m in list)
            //    {
            //        if (m.ID == ID)
            //        {
            //            currentModel = m;
            //            break;
            //        }
            //    }
            //    string filepath = @"\\10.23.6.11\c$" + (currentModel.PhysicsURI.Split('.')[0] + ".wav").Split(':')[1];
            //    try
            //    {
            //        File.Delete(filepath);

            //        if (sQLHelper.ExecuteSql(@"delete from [ApplicationTest].[dbo].[KLCN_Tab_SpeechRecognition] where ID=" + currentModel.ID) > 0)
            //        {
            //            if (list.Remove(currentModel))
            //            {
            //                this.Dispatcher.Invoke((Action)(() =>
            //                {
            //                    listView1.ItemsSource = list;
            //                    listView1.Items.Refresh();
            //                }));
            //            }
            //        }
            //    }
            //    catch
            //    {

            //    }
            //}
            #endregion
            string ID = (sender as Button).CommandParameter.ToString();

            foreach (Model m in list)
            {
                if (m.ID == ID)
                {
                    currentModel = m;
                    break;
                }
            }
            UIElementCollection uIElementCollection = this.mailGrid.Children;
            foreach(UIElement element in uIElementCollection)
            {
                if(element is RadioButton)
                {
                    RadioButton radioButton = element as RadioButton;
                    if (radioButton.Content.ToString().ToLower() == currentModel.Account.ToLower())
                        radioButton.IsChecked = true;
                }
            }
            listView1.Visibility = Visibility.Hidden;
            mailPanel.Visibility = Visibility.Visible;
        }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.SizeAll;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Send Mail Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                UIElementCollection uIElementCollection = this.mailGrid.Children;
                bool mailRes = false;
                foreach (UIElement element in uIElementCollection)
                {
                    if (element is RadioButton)
                    {
                        RadioButton radioButton = element as RadioButton;
                        if ((bool)radioButton.IsChecked)
                        {
                            mailRes=SendMail(radioButton);break;
                        }
                    }
                }
                if (mailRes)
                {
                    if (sQLHelper.ExecuteSql(@"update [ApplicationTest].[dbo].[KLCN_Tab_SpeechRecognition] set IsRead=0 where ID=" + currentModel.ID)>0)
                    {
                        if (list.Remove(currentModel))
                        {
                            mailPanel.Visibility = Visibility.Hidden;
                            listView1.Visibility = Visibility.Visible;
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                listView1.ItemsSource = list;
                                listView1.Items.Refresh();
                            }));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Sent Failed, please contact developer.");
                }
            }
        }
        public bool SendMail(RadioButton radioButton)
        {
            MailMessage message = new MailMessage();
            message.To.Add("KLCN.Test@cn.klueber.com");
            message.From = new MailAddress(currentModel.Email,currentModel.Account, Encoding.UTF8);
            message.Subject = "Voicemail - " + radioButton.Content.ToString() + " - " + currentModel.Datetime;
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = currentModel.Subject+"   "+currentModel.Content;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = false;
            message.Priority = MailPriority.Normal;
            Attachment att = new Attachment(@"\\10.23.6.11\c$" + (currentModel.PhysicsURI.Split('.')[0] + ".wav").Split(':')[1]);//添加附件，确保路径正确
            message.Attachments.Add(att);

            SmtpClient smtp = new SmtpClient();
            smtp.Credentials = new System.Net.NetworkCredential("ycnspadmin", "fcs@sp937");
            smtp.Host = "10.23.0.18";
            object userState = message;

            try
            {
                smtp.SendAsync(message, userState);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tips");
                return false;
            }
        }
        private void back_Click(object sender, RoutedEventArgs e)
        {
            mailPanel.Visibility = Visibility.Hidden;
            listView1.Visibility = Visibility.Visible;
            this.Dispatcher.Invoke((Action)(() =>
            {
                listView1.ItemsSource = list;
                listView1.Items.Refresh();
            }));
        }
    }
    public partial struct Model
    {
        public string ID { get; set; }
        public string Subject { get; set; }
        public string AttachmentURI { get; set; }
        public string AttachmentContentType { get; set; }
        public string MsgId { get; set; }
        public string UserobjectId { get; set; }
        public string PhysicsURI { get; set; }
        public string Email { get; set; }
        public string Account { get; set; }
        public string CurrentDirectory { get; set; }
        public string Datetime { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
    }

}
