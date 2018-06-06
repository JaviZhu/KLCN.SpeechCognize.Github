using KLCN.CognizeLibrary;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KLCN.SpeechCognizeConsole
{
    class Program
    {
        static bool completed;
        static FileStream fs;
        private delegate void AsyncInvoke();
        static void Main(string[] args)
        {
            Thread thread = new Thread(Action);
            thread.Start();
        }
        static void Action()
        {
            while (true)
            {
                AsyncInvoke asyncInvoke = new AsyncInvoke(Delegate);
                asyncInvoke.Invoke();

                GC.Collect();
                Thread.Sleep(10 * 1000);
            }
        }
        static void Delegate()
        {
            lock (thisLock)
            {
                Analyze();
            }
        }
        private static Object thisLock = new object();
        static async Task Analyze()
        {
            Parameters.CurrentDirectory = System.IO.Directory.GetCurrentDirectory();

            Cisco cisco = new Cisco();
            List<Parameters> list = cisco.GetVoiceURI().Result;

            Speech speech;

            //ActiveDirectoryOperator ad = new ActiveDirectoryOperator();
            //SmsOperator sms = new SmsOperator();
            //WeChatOperator wechat = new WeChatOperator();

            if (list != null && list.Count > 0)
            {
                speech = new Speech();
                using (ApplicationTestEntities access = new ApplicationTestEntities())
                {
                    
                    foreach (var item in list)
                    {
                        //Console.WriteLine(item.Account + "_" + item.Subject + ":" + speech.Baidu_RecognizeByAPI(item.PhysicsURI));
                        string content = speech.Micro_RecognizeByAPI(item.PhysicsURI);
                        if (string.Equals(content, string.Empty))
                            content = speech.Baidu_RecognizeByAPI(item.PhysicsURI).Replace("[", "").Replace("]", "").Trim();
                        //string mobile = ad.GetDirectoryMobile(item.Account);
                        //sms.Sender(item.Subject, content, mobile);
                        //wechat.Sender(item);
                        SqlParameter[] sqlParameter =
                        {
                            new SqlParameter("@MsgId",item.MsgId)
                        };
                        List<KLCN_Tab_SpeechRecognition> model = await access.KLCN_Tab_SpeechRecognition.SqlQuery(@"select * from KLCN_Tab_SpeechRecognition where MsgId=@MsgId", sqlParameter).ToListAsync();
                        
                        KLCN_Tab_SpeechCalculate cal = await access.KLCN_Tab_SpeechCalculate.SqlQuery(@"select top 1 * from KLCN_Tab_SpeechCalculate", new SqlParameter[] { }).SingleOrDefaultAsync();

                        if (model==null || model.Count == 0)
                        {
                            KLCN_Tab_SpeechRecognition kLCN_Tab_SpeechRecognition = new KLCN_Tab_SpeechRecognition();
                            kLCN_Tab_SpeechRecognition.Account = item.Account;
                            kLCN_Tab_SpeechRecognition.AttachmentContentType = item.AttachmentContentType;
                            kLCN_Tab_SpeechRecognition.AttachmentURI = item.AttachmentURI;
                            kLCN_Tab_SpeechRecognition.Email = item.Email;
                            kLCN_Tab_SpeechRecognition.MsgId = item.MsgId;
                            kLCN_Tab_SpeechRecognition.PhysicsURI = item.PhysicsURI;
                            kLCN_Tab_SpeechRecognition.Subject = item.Subject;
                            kLCN_Tab_SpeechRecognition.UserobjectId = item.UserobjectId;
                            kLCN_Tab_SpeechRecognition.Datetime = DateTime.Now;
                            kLCN_Tab_SpeechRecognition.Content = content;
                            kLCN_Tab_SpeechRecognition.IsRead = true;
                            access.KLCN_Tab_SpeechRecognition.Add(kLCN_Tab_SpeechRecognition);

                            cal.Calculate = cal.Calculate + 1;

                            access.SaveChanges();
                        }
                    }
                }
            }
        }
    }
}
