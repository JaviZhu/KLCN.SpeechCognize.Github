using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace KLCN.CognizeLibrary
{
    internal struct CurrDirectory
    {
        public static string currDirectory
        {
            get
            {
                return Parameters.CurrentDirectory;
            }
        }
    }
    public class WeChatOperator
    {
        public WeChatOperator()
        {

        }
        private Dictionary<string,string> Parameters
        {
            get
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("CorpID", "CorpID");
                dic.Add("Secret", "Secret-0nTM");
                dic.Add("AgentId", "AgentId");
                dic.Add("Voice_Secret", "Voice_Secret");
                return dic;
            }
        }
        private string ExcelPath
        {
            get
            {
                return CurrDirectory.currDirectory + "\\db\\token.txt";
            }
        }
        private string VoicePath
        {
            get
            {
                return CurrDirectory.currDirectory + "\\db\\voice_token.txt";
            }
        }
        private string ExcelFolder
        {
            get
            {
                return CurrDirectory.currDirectory + "\\db";
            }
        }
        private async Task<string> Token(string secret)
        {
            string requestUri = string.Format("https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={0}&corpsecret={1}", Parameters["CorpID"], secret);
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.ContentType = "application/xml;charset=UTF-8";

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
            }
            catch (Exception e)
            {
            }
            return JObject.Parse(responseString).GetValue("access_token").ToString();
        }
        public string Sender(Parameters item)
        {
            string result = string.Empty;
            try
            {
                string token = CheckIfGetNewToken(ExcelPath,Parameters["Secret"]).Result;
                string voice_token = CheckIfGetNewToken(VoicePath, Parameters["Voice_Secret"]).Result;
                //string media_id = UploadMedia(item, token).Result;
                string media_id = JObject.Parse(WeChatUpload(item.PhysicsURI, token, "voice", item.Account).Result).GetValue("media_id").ToString();
                result = JObject.Parse(SendAudio(voice_token, "cnjazhu", Parameters["AgentId"], media_id).Result).GetValue("errmsg").ToString();
            }
            catch(Exception e)
            {
                result = e.Message;
            }
            return result;
        }
        private async Task<string> CheckIfGetNewToken(string path,string secret)
        {
            string result = string.Empty;
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                string token = content.Split(';')[0];
                DateTime cd = Convert.ToDateTime(content.Split(';')[1]);

                if (cd.AddHours(2) <= DateTime.Now)
                {
                    File.Delete(path);
                    StreamWriter sw = File.CreateText(path);
                    result = await Token(secret);
                    sw.Write(result+";"+DateTime.Now.ToString());
                    sw.Close();
                }
                else
                    result = token;
            }
            else
            {
                StreamWriter sw = File.CreateText(path);
                result = await Token(secret);
                sw.Write(result + ";" + DateTime.Now.ToString());
                sw.Close();
            }
            #region ignore
            //var fileName = ExcelPath;
            //var connectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0; data source={0}; Extended Properties=Excel 8.0;", fileName);
            //OleDbConnection myConn = new OleDbConnection(connectionString);
            //myConn.Open();
            //DataTable sheetNames = myConn.GetOleDbSchemaTable
            //        (System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

            //var adapter = new OleDbDataAdapter("SELECT * FROM [" + sheetNames.Rows[0]["TABLE_NAME"].ToString() + "]", connectionString);
            //var ds = new DataSet();
            //adapter.Fill(ds, "anyNameHere");
            //DataTable data = ds.Tables["anyNameHere"];
            //string result = string.Empty;
            //if (data!=null && data.Rows.Count > 0)
            //{
            //    DateTime cd = Convert.ToDateTime(data.Rows[0]["datetime"]);
            //    if (cd.AddHours(2) <= DateTime.Now)
            //    {
            //        string sql = "delete from [" + sheetNames.Rows[0]["TABLE_NAME"].ToString() + "] where token='" + data.Rows[0]["token"].ToString() + "'";
            //        OleDbCommand cmd = new OleDbCommand(sql, myConn);
            //        cmd.ExecuteNonQuery();
            //        result = await Token();
            //        sql = "Insert into [" + sheetNames.Rows[0]["TABLE_NAME"].ToString() + "] (token,datetime) values('" + result + "','" + DateTime.Now + "')";
            //        cmd = new OleDbCommand(sql, myConn);
            //        cmd.ExecuteNonQuery();
            //    }
            //    else
            //        result = data.Rows[0]["token"].ToString();
            //}
            //else if (data == null || data.Rows.Count == 0)
            //{
            //    result = await Token();
            //    string sql = "Insert into [" + sheetNames.Rows[0]["TABLE_NAME"].ToString() + "] (token,datetime) values('" + result + "','" + DateTime.Now + "')";
            //    OleDbCommand cmd = new OleDbCommand(sql, myConn);
            //    try
            //    {
            //        cmd.ExecuteNonQuery();
            //    }
            //    catch(Exception e)
            //    {

            //    }
            //}
            //myConn.Close();
#endregion
            return result;
        }
        private async Task<string> UploadMedia(Parameters item,string token)
        {
            FileStream fs;
            string requestUri = @"https://qyapi.weixin.qq.com/cgi-bin/media/upload?access_token="+token+"&type=voice";//en-US
            string audioFile = item.PhysicsURI;
            string responseString = string.Empty;

            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Accept = @"application/json;text/xml";
            request.Method = "POST";

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
            return JObject.Parse(responseString).GetValue("media_id").ToString();
        }
        private async Task<string> WeChatUpload(string filepath, string token, string mt,string account)
        {
            FFmpegSerializeOperator ffmeg = new FFmpegSerializeOperator();
            string targetName = CurrDirectory.currDirectory+ "\\Temp\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + account + "_" + DateTime.Now.ToString("HHmmssfff") + ".amr";

            //targetName= @"C:\Z_Disk\document\source\KLCN.SpeechCognize\KLCN.SpeechCognizeConsole\bin\Debug\Temp\20170821\cnBaSun_104215065.amr";
            await ffmeg.ConvertToAmr(CurrDirectory.currDirectory, filepath, targetName);

            string retdata = string.Empty;
            using (WebClient client = new WebClient())
            {
                byte[] b = client.UploadFile(string.Format("https://qyapi.weixin.qq.com/cgi-bin/media/upload?access_token={0}&type={1}", token, mt), targetName);
                retdata = Encoding.Default.GetString(b);
            }
            return retdata;
        }
        private async Task<string> SendAudio(string token,string touser, string agentid, string media_id)
        {
            string result = string.Empty;
            using(HttpClient client=new HttpClient())
            {
                string requesturi = @"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token=" + token + "";
                //var content = new FormUrlEncodedContent(new[]{
                //    new KeyValuePair<string,string>("touser","zhu_jiahui")
                //    ,new KeyValuePair<string, string>("msgtype","voice")
                //    ,new KeyValuePair<string, string>("agentid",agentid)
                //    ,new KeyValuePair<string, string>("voice","{media_id:"+media_id+"}")
                //    });
                WeChatParameters1 p = new WeChatParameters1();
                WeChatParameters2 p2 = new WeChatParameters2();
                p2.media_id = media_id;
                p.agentid = agentid;
                p.msgtype = "voice";
                p.touser = touser;
                p.voice = p2;
                string jsondata = new JavaScriptSerializer().Serialize(p);
                PostWebRequest(requesturi, jsondata, Encoding.Default);
                //result = await client.PostAsync(requesturi, content).Result.Content.ReadAsStringAsync();
            }
            return result;
        }
        private string PostWebRequest(string postUrl, string paramData, Encoding dataEncode)
        {
            string ret = string.Empty;
            try
            {
                byte[] byteArray = dataEncode.GetBytes(paramData); 
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";

                webReq.ContentLength = byteArray.Length;
                Stream newStream = webReq.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Default);
                ret = sr.ReadToEnd();
                sr.Close();
                response.Close();
                newStream.Close();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return ret;
        }
    }
    public class WeChatParameters1
    {
        public string touser { get; set; }
        public string msgtype { get; set; }
        public string agentid { get; set; }
        public WeChatParameters2 voice { get; set; }
    }
    public class WeChatParameters2
    {
        public string media_id { get; set; }
    }
}
