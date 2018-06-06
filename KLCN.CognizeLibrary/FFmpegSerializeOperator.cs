using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KLCN.CognizeLibrary
{
    public class FFmpegSerializeOperator
    {
        public async Task ConvertToAmr(string applicationPath, string fileName, string targetFilName)
        {
            string c = applicationPath + @"\ffmpeg.exe -y -i " + fileName + " -ar 8000 -ab 12.2k -ac 1 " + targetFilName;
            c = applicationPath+  @"\ffmpeg.exe -i " + fileName + " -ar 8000 -ac 1 " + targetFilName + " -y";
            Cmd(c);
        }
        public async Task ConvertToMP3(string applicationPath, string fileName, string targetFilName)
        {
            string c = applicationPath + @"\ffmpeg.exe -i " + fileName + " -acodec libmp3lame " + targetFilName + " -y";
            Cmd(c);
        }
        private void Cmd(string c)
        {
            try
            {
                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.Start();

                    process.StandardInput.WriteLine(c);
                    process.StandardInput.AutoFlush = true;
                    process.StandardInput.WriteLine("exit");

                    StreamReader reader = process.StandardOutput;
                    Thread.Sleep(1000);
                    process.Close();
                    //process.WaitForExit();
                    
                }
            }
            catch
            { }
        }
        public byte[] GetFileByte(string fileName)
        {
            FileStream pFileStream = null;
            byte[] pReadByte = new byte[0];
            try
            {
                pFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader r = new BinaryReader(pFileStream);
                r.BaseStream.Seek(0, SeekOrigin.Begin);    
                pReadByte = r.ReadBytes((int)r.BaseStream.Length);
                return pReadByte;
            }
            catch
            {
                return pReadByte;
            }
            finally
            {
                if (pFileStream != null)
                    pFileStream.Close();
            }
        }
        public bool writeFile(byte[] pReadByte, string fileName)
        {
            FileStream pFileStream = null;
            try
            {
                pFileStream = new FileStream(fileName, FileMode.OpenOrCreate);
                pFileStream.Write(pReadByte, 0, pReadByte.Length);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (pFileStream != null)
                    pFileStream.Close();
            }
            return true;

        }
    }
}
