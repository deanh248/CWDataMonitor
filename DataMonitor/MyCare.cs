using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DataMonitor
{
    static class MyCare
    {

        public static readonly MyCareToken Token = new MyCareToken();

        public static async Task<string> HTTPRequestString(string content)
        {
            try
            {
                Console.WriteLine("Creating HTTP POST Request..");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mycare.cwseychelles.com/fcgi-bin/miCare");
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Method = "POST";
                request.Accept = "*/*";
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Referer = "https://mycare.cwseychelles.com/fcgi-bin/miCare";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36";
                request.Host = "mycare.cwseychelles.com";

                byte[] byteArray = Encoding.UTF8.GetBytes(content);
                request.ContentLength = byteArray.Length;
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                WebResponse wr = await request.GetResponseAsync();

                using (var reader = new StreamReader(wr.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public static bool ValidateLoginToken(string token){
            // example;
            // G2.BB+22FJ05AJ80BE59JE88AF08FG46+FB.1I|32383545
            // TODO: assuming valid tokens always have a single '|' divider and magic number

            if (token == null)
                return false;

            if (token.Length <= 0)
                return false;

            return token.Split('|').Length == 2;
        }

        public static string[] SplitDelimiter(string s)
        {
            return s.Split(new String[]{"|#|"}, StringSplitOptions.None);
        }
    }
}
