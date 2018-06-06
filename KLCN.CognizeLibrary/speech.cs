using Baidu.Aip.Speech;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Excel=Microsoft.Office.Interop.Excel;

namespace KLCN.CognizeLibrary
{
    public class Speech
    {
        public string Micro_RecognizeByAPI(string filePhysicsPath)
        {
            new FFmpegSerializeOperator().ConvertToAmr(Parameters.CurrentDirectory, filePhysicsPath, filePhysicsPath);

            FileStream fs;

            Micro_Authentication Authentication = new Micro_Authentication("69650c8174a749e19da3dbd33c80d9ee");//458caaa1d6564a06a4ed5a7623db35fe;2c0225e9d7d346a9a4055a1aa472a3ab
            string token = Authentication.GetAccessToken();
            string requestUri = @"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1?language=zh-CN";//en-US
            string audioFile = filePhysicsPath;
            string responseString = string.Empty;

            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.SendChunked = true;
            request.Accept = @"application/json;text/xml";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version11;
            request.Host = @"speech.platform.bing.com";
            request.ContentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";
            request.Headers["Authorization"] = "Bearer " + token;

            try
            {
                using (fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = null;
                    int bytesRead = 0;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }
                        requestStream.Flush();
                    }
                }
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
            return JObject.Parse(responseString).GetValue("DisplayText").ToString();
        }
        public string Baidu_RecognizeByAPI(string filePhysicsPath)
        {
            new FFmpegSerializeOperator().ConvertToAmr(Parameters.CurrentDirectory, filePhysicsPath, filePhysicsPath);
            Asr _asrClient = new Asr("TmZSp2x7nZXlPIW6GapK45XG", "BuWaLotBu4ho1GnblaMbue3ppryGuOYG ");
            var data = File.ReadAllBytes(filePhysicsPath);
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("lan", "en");
            var result = _asrClient.Recognize(data, "WAV", 8000);
            string err_msg = result.GetValue("err_msg").ToString();
            string content = string.Empty;
            if (string.Equals(err_msg, "success."))
                content = result.GetValue("result").ToString();
            if (!string.Equals("success.", err_msg)||content.Split('"').Length >= 5)
                result=Baidu_RecognizeByAPI(filePhysicsPath, true);
            return result.GetValue("result").ToString();
        }
        private JObject Baidu_RecognizeByAPI(string filePhysicsPath,bool isEn)
        {
            Asr _asrClient = new Asr("TmZSp2x7nZXlPIW6GapK45XG", "BuWaLotBu4ho1GnblaMbue3ppryGuOYG ");
            var data = File.ReadAllBytes(filePhysicsPath);
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("lan", "en");
            var result = _asrClient.Recognize(data, "WAV", 8000, dic);
            return result;
        }
    }
    internal class Micro_Authentication
    {
        public static readonly string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";
        private string subscriptionKey;
        private string token;
        private Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes.
        private const int RefreshTokenDuration = 9;

        public Micro_Authentication(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
            this.token = FetchToken(FetchTokenUri, subscriptionKey).Result;

            // renew the token on set duration.
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));
        }

        public string GetAccessToken()
        {
            return this.token;
        }

        private void RenewAccessToken()
        {
            this.token = FetchToken(FetchTokenUri, this.subscriptionKey).Result;
            Console.WriteLine("Renewed token.");
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                Console.WriteLine("Token Uri: {0}", uriBuilder.Uri.AbsoluteUri);
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
    public class Cisco
    {
        private string ExcelPath
        {
            get
            {
                return Parameters.CurrentDirectory + "\\db\\db.xls";
            }
        }
        private string ExcelFolder
        {
            get
            {
                return Parameters.CurrentDirectory + "\\db";
            }
        }
        public async Task<List<Parameters>> GetVoiceURI()
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback
            (
               delegate { return true; }
            );
#region initial excel
            if (!Directory.Exists(ExcelFolder))
                Directory.CreateDirectory(ExcelFolder);
            if (!File.Exists(ExcelPath))
            {
                Excel.Application xlApp = new Excel.Application();
                if (xlApp == null)
                {
                    return null;
                }
                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                object misValue = System.Reflection.Missing.Value;
                xlWorkBook = xlApp.Workbooks.Add(misValue);
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

                xlWorkSheet.Cells[1, 1] = "UserObjectId";
                xlWorkSheet.Cells[1, 2] = "Email";
                xlWorkSheet.Cells[1, 3] = "Account";
                xlWorkSheet.Cells[1, 4] = "MgsId";

                xlWorkBook.SaveAs(ExcelPath, Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                xlWorkBook.Close(true, misValue, misValue);
                xlApp.Quit();

                Marshal.ReleaseComObject(xlWorkSheet);
                Marshal.ReleaseComObject(xlWorkBook);
                Marshal.ReleaseComObject(xlApp);
            }
#endregion
            List<Parameters> list = new List<Parameters>();
            foreach (var m in SpeecifyUsers)
            {
                List<Parameters> lt = await GetMailBoxInfor(m as string[]);
                if(lt!=null)
                list.AddRange(lt);
            }
            return list;
        }
        private async Task<bool> ExcelAddOrNot(string[] specifyuser,string msgId)
        {
            var fileName = ExcelPath;
            var connectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0; data source={0}; Extended Properties=Excel 8.0;", fileName);
            OleDbConnection myConn = new OleDbConnection(connectionString);
            myConn.Open();
            DataTable sheetNames = myConn.GetOleDbSchemaTable
                    (System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

            var adapter = new OleDbDataAdapter("SELECT * FROM ["+sheetNames.Rows[0]["TABLE_NAME"].ToString()+"]", connectionString);
            var ds = new DataSet();
            adapter.Fill(ds, "anyNameHere");
            DataTable data = ds.Tables["anyNameHere"];
            DataRow[] dr = null;
            if (data != null && data.Rows.Count > 0)
                dr = data.Select("MgsId='" + msgId + "'");
            bool success = false;
            if(dr==null || dr.Length==0)
            {
                string sql = "Insert into [" + sheetNames.Rows[0]["TABLE_NAME"].ToString() + "] (UserObjectId,Email,Account,MgsId) values('" + specifyuser[0] + "','" + specifyuser[1] + "','" + specifyuser[2] + "','" + msgId + "')";
                OleDbCommand cmd = new OleDbCommand(sql, myConn);
                success = cmd.ExecuteNonQuery() > 0;
            }
            myConn.Close();
            return success;
        }
        /// <summary>
        /// https://10.23.0.107/vmrest/mailbox/folders/inbox/messages?userobjectid=3226c1ac-eca9-42aa-8baa-2daa6cb1b56a&read=false&type=voice
        /// https://10.23.0.107/vmrest/messages/0:af31c108-1c27-448f-b308-eb3431e8b0b6/?userobjectid=3226c1ac-eca9-42aa-8baa-2daa6cb1b56a
        /// https://10.23.0.107/vmrest/messages/0:af31c108-1c27-448f-b308-eb3431e8b0b6/attachments/0/?userobjectid=3226c1ac-eca9-42aa-8baa-2daa6cb1b56a
        /// </summary>
        /// <returns></returns>
        private async Task<List<Parameters>> GetMailBoxInfor (string[] specifyuser)
        {
            string userobjectid = specifyuser[0];
            string requestUri = string.Format("https://10.23.0.107/vmrest/mailbox/folders/inbox/messages?userobjectid={0}&read=false&type=voice", userobjectid);
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Host = "10.23.0.107";
            request.ContentType = "application/xml;charset=UTF-8";
            request.Headers["Authorization"] = "Basic " + Token;
            WebProxy wp = new WebProxy();
            request.Proxy = wp;

            string responseString = string.Empty;

            List<Parameters> result = null;
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }
                }
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseString);
                XmlNode root = doc.ChildNodes[1];
                if(root.ChildNodes.Count>0&& root.Attributes["total"].Value != "0")
                {
                    var collection = new List<object>();
                    foreach(XmlNode node in root.ChildNodes)
                    {
                        bool goTo=await ExcelAddOrNot(specifyuser, node.SelectNodes("MsgId").Item(0).InnerText);
                        if (goTo)
                        {
                            collection.Add(new string[]
                            {
                            node.SelectNodes("MsgId").Item(0).InnerText
                            ,userobjectid
                            });
                        }
                    }
                    result= await GetMessageInfo(collection,specifyuser);
                }
            }
            catch(Exception e)
            {
            }
            return result;
        }
        private async Task<List<Parameters>> GetMessageInfo(List<object> collection,string[] specifyuser)
        {
            List<Parameters> list = new List<Parameters>();
            foreach (var item in collection)
            {
                var m = item as string[];
                string requestUri = string.Format("https://10.23.0.107/vmrest/messages/{0}/?userobjectid={1}", m[0], m[1]);
                HttpWebRequest request = null;
                request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
                request.Host = "10.23.0.107";
                request.ContentType = "application/xml;charset=UTF-8";
                request.Headers["Authorization"] = "Basic " + Token;
                WebProxy wp = new WebProxy();
                request.Proxy = wp;

                string responseString = string.Empty;

                try
                {
                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = sr.ReadToEnd();
                        }
                    }
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(responseString);
                    XmlNode root = doc.ChildNodes[1];
                    Parameters para = new Parameters();
                    para.Subject = root.SelectNodes("Subject").Item(0).InnerText;
                    para.AttachmentURI = root.SelectNodes("Attachments/Attachment/URI").Item(0).InnerText;
                    para.AttachmentContentType= root.SelectNodes("Attachments/Attachment/contentType").Item(0).InnerText;
                    para.MsgId = root.SelectNodes("MsgId").Item(0).InnerText;
                    para.UserobjectId = m[1];
                    para.Email = specifyuser[1];
                    para.Account = specifyuser[2];
                    para.PhysicsURI = await SaveAttachment(para);

                    list.Add(para);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            return list;
        }
        private async Task<string> SaveAttachment(Parameters para)
        {
            string requestUri = string.Format(@"https://10.23.0.107{0}/?userobjectid={1}", para.AttachmentURI, para.UserobjectId);
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Host = "10.23.0.107";
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

                            if (!Directory.Exists(Parameters.CurrentDirectory + "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd")))
                                Directory.CreateDirectory(Parameters.CurrentDirectory + "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd"));

                            string fnDT = DateTime.Now.ToString("yyyyMMdd") + "\\" + para.Account + "_" + DateTime.Now.ToString("HHmmssfff");

                            path = Parameters.CurrentDirectory + "\\Temp\\" + fnDT + ".wav";
                            using (FileStream fs=new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                ms.WriteTo(fs);
                            }
                            //string path2 = Parameters.CurrentDirectory + "\\Temp\\" + fnDT + ".mp3";
                            //FFmpegSerializeOperator ffmeg = new FFmpegSerializeOperator();
                            //await ffmeg.ConvertToAmr(CurrDirectory.currDirectory, path, path2);
                        }
                    }
                }
            }
            catch(Exception e)
            {

            }
            return path;
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
        private List<Object> SpeecifyUsers
        {
            get
            {
                return FunSpecifyUser(new List<object>()
                {
                    new string[]{"3226c1ac-eca9-42aa-8baa-2daa6cb1b56a","Bamboo.Sun@cn.klueber.com","cnBaSun" }//bamboo
                    ,new string[]{"1149ddb4-d1a5-443c-b66b-71052432f43e","Gavin.Wu@cn.klueber.com","cnGaWu" }//gavin
                    ,new string[]{"197707e5-3a8b-47a9-9918-b7af310c2843","Jeffrey.Hu@cn.klueber.com", "cnJeHu" }//jeffrey
                    ,new string[]{"78dd328a-437d-4e7c-9334-023c0c99664c","ji.sun@cn.klueber.com", "cnJiSun" }//edward
                    ,new string[]{"62994a46-60dc-4dfd-9edc-bbdf26cee629", "Martin.Schmidt-Amelunxen@cn.klueber.com", "cnMaSchmid" }//Schmidt-Amelunxen
                    ,new string[]{"62994a46-60dc-4dfd-9edc-bbdf26cee629", "Alucard.Li@cn.klueber.com", "cnAlLi" }
                    ,new string[]{"62994a46-60dc-4dfd-9edc-bbdf26cee629", "May.Lin@cn.klueber.com", "cnMaLin" }
                    ,new string[]{"62994a46-60dc-4dfd-9edc-bbdf26cee629", "Javi.Zhun@cn.klueber.com", "cnJaZhu" }
                    ,new string[]{"62994a46-60dc-4dfd-9edc-bbdf26cee629", "Max.Zhao@cn.klueber.com", "cnMaZhao" }
                });
            }
        }
        private List<Object> FunSpecifyUser(List<Object> users)
        {
            string requestUri = string.Format("https://10.23.0.107/vmrest/users");
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Host = "10.23.0.107";
            request.ContentType = "application/xml;charset=UTF-8";
            request.Headers["Authorization"] = "Basic " + Token;
            WebProxy wp = new WebProxy();
            request.Proxy = wp;

            string responseString = string.Empty;

            List<Object> result = new List<Object>();
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }
                }
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseString);
                XmlNode root = doc.ChildNodes[1];
                if (root.ChildNodes.Count > 0 && root.Attributes["total"].Value != "0")
                {
                    int total = users.Count;
                    int i = 0;
                    foreach(XmlNode node in root.ChildNodes)
                    {
                        string userobjectID = node.SelectNodes("ObjectId").Item(0).InnerText;
                        string account= node.SelectNodes("Alias").Item(0).InnerText;
                        foreach(string[] m in users)
                        {
                            if (m[2].ToLower() == account.ToLower())
                            {
                                m[0] = userobjectID;
                                result.Add(m);
                                i++;
                            }
                        }
                        if (i == total)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
            }
            return result;
        }
    }
    public class Parameters
    {
        public string Subject { get; set; }
        public string AttachmentURI { get; set; }
        public string AttachmentContentType { get; set; }
        public string MsgId { get; set; }
        public string UserobjectId { get; set; }
        public string PhysicsURI { get; set; }
        public string Email { get; set; }
        public string Account { get; set; }
        public static string CurrentDirectory { get; set; }
    }
}
