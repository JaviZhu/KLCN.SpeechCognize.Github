using KLCN.CognizeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace KLCN.SpeechCognizeWebService
{
    /// <summary>
    /// Summary description for Speech
    /// </summary>
    public class Recognition : System.Web.Services.WebService
    {
        [WebMethod]
        public void Action()
        {
            Parameters.CurrentDirectory = System.Web.HttpContext.Current.Server.MapPath("~/");
            Parameters.CurrentDirectory = Parameters.CurrentDirectory.Substring(0, Parameters.CurrentDirectory.Length - 1);
            Cisco cisco = new Cisco();
            List<Parameters> list = cisco.GetVoiceURI().Result;

            Speech speech = new Speech();

            foreach (var item in list)
            {
                string content = speech.Micro_RecognizeByAPI(item.PhysicsURI);
                if (string.Equals(content, string.Empty))
                    content = speech.Baidu_RecognizeByAPI(item.PhysicsURI).Replace("[", "").Replace("]", "").Trim();
            }
        }
    }
}
