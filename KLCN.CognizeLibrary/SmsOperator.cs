using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Yunpian.conf;
using Yunpian.model;

namespace KLCN.CognizeLibrary
{
    public class SmsOperator 
    {
        public void Sender(string subject, string content,string mobile)
        {
            //mobile = "15900467108";
            mobile = mobile.Replace(" ", "").Replace("+", "") ;
            string rm = string.Empty;
            for(int i = mobile.Length-1; i >=0; i--)
            {
                if (mobile.Substring(i, 1).Trim() != "")
                    rm = mobile.Substring(i, 1) + rm;
                if (rm.Length == 11)
                    break;
            }
            mobile = rm;
            string rc = string.Empty;
            for(int i = 0; i < content.Length; i++)
            {
                rc += content.Substring(i, 1) + " ";
            }
            mobile = HttpUtility.UrlEncode(mobile, Encoding.UTF8);
            string apikey = "66457c08d0175f4b77f2da3cecee2e7b";

            string tpl_id = "1909944";

            string url_tpl_sms = "https://sms.yunpian.com/v2/sms/tpl_single_send.json";
            string tpl_value = HttpUtility.UrlEncode(
            HttpUtility.UrlEncode("#content#", Encoding.UTF8) + "=" +
            HttpUtility.UrlEncode(content, Encoding.UTF8), Encoding.UTF8);

            string data_tpl_sms = "apikey=" + apikey + "&mobile=" + mobile + "&tpl_id=" + tpl_id + "&tpl_value=" + tpl_value;

            string restul= HttpPost(url_tpl_sms, data_tpl_sms).Result;
            if (string.Equals(restul.Trim(), ""))
            {
                Sender(subject, "由于短信关键词屏蔽，您的这条语音文字无法发送，请尽快收听语音，仅此提醒。", mobile);
            }
        }

        private async Task<string> HttpPost(string Url, string postDataStr)
        {
            byte[] dataArray = Encoding.UTF8.GetBytes(postDataStr);
            // Console.Write(Encoding.UTF8.GetString(dataArray));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataArray.Length;
            //request.CookieContainer = cookie;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(dataArray, 0, dataArray.Length);
            dataStream.Close();
            String res = string.Empty;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                res = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception e)
            {
                
            }
            return res;
        }
    }
}
