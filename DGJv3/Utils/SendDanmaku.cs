using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using LoginCenter.API;
using Newtonsoft.Json.Linq;
using DGJv3.InternalModule;
using System.Text.RegularExpressions;

namespace DGJv3
{
    class SendDanmaku  //以下代码大段抄自SendDanmaku  https://www.danmuji.org/plugins/SendDanmaku
    {
        public static async Task<string> SendDanmakuAsync(int roomId, string danmaku, CookieContainer cookie, int color = 16777215)
        {
            int num = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000L) / 10000000L);
            Cookie cookie2 = cookie.GetCookies(new Uri("http://live.bilibili.com/")).OfType<Cookie>().FirstOrDefault((Cookie p) => p.Name == "bili_jct");
            string value = cookie2?.Value;
            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                {
                    "color",
                    color
                },
                {
                    "fontsize",
                    25
                },
                {
                    "mode",
                    1
                },
                {
                    "msg",
                    WebUtility.UrlEncode(danmaku)
                },
                {
                    "rnd",
                    num
                },
                {
                    "roomid",
                    roomId
                },
                {
                    "csrf_token",
                    value
                },
                {
                    "csrf",
                    value
                }
            };
            string result;
            try
            {
                result = await HttpPostAsync("https://api.live.bilibili.com/msg/send", parameters, 15, null, cookie, null);
            }
            catch(Exception e)
            {
                DGJMain.SELF.Log(e.Message);
                result = null;
            }
            return result;
        }
        public static async Task<string> HttpPostAsync(string url, IDictionary<string, object> parameters = null, int timeout = 0, string userAgent = null, CookieContainer cookie = null, IDictionary<string, string> headers = null)
        {
            string formdata = string.Join("&", from p in parameters
                                               select string.Format("{0}={1}", p.Key, p.Value));
            return await SendDanmaku.HttpPostAsync(url, formdata, timeout, userAgent, cookie, headers);
        }
        public static async Task<string> HttpPostAsync(string url, string formdata, int timeout = 0, string userAgent = null, CookieContainer cookie = null, IDictionary<string, string> headers = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Accept = "*/*";
            request.Method = "POST";
            request.AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate);
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = userAgent ?? ("DGJ/" + new Regex(@"[\u4e00-\u9fa5]").Replace((BuildInfo.Version),"")+"(patched by perichr)");
            if (timeout != 0)
            {
                request.Timeout = timeout * 1000;
                request.ReadWriteTimeout = timeout * 1000;
            }
            else
            {
                request.ReadWriteTimeout = 10000;
            }
            request.CookieContainer = cookie;
            if (headers != null)
            {
                foreach (string text in headers.Keys)
                {
                    if (text.ToLower() == "accept")
                    {
                        request.Accept = headers[text];
                    }
                    else if (text.ToLower() == "host")
                    {
                        request.Host = headers[text];
                    }
                    else if (text.ToLower() == "referer")
                    {
                        request.Referer = headers[text];
                    }
                    else if (text.ToLower() == "content-type")
                    {
                        request.ContentType = headers[text];
                    }
                    else
                    {
                        request.Headers.Add(text, headers[text]);
                    }
                }
            }
            if (!string.IsNullOrEmpty(formdata))
            {
                byte[] data = Encoding.UTF8.GetBytes(formdata);
                Stream stream2 = await request.GetRequestStreamAsync();
                using (Stream stream = stream2)
                {
                    await stream.WriteAsync(data, 0, data.Length);

                }
            }
            string result;
            using (HttpWebResponse response = (await request.GetResponseAsync()) as HttpWebResponse)
            {

                using (Stream stream = response.GetResponseStream())
                {

                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        result = await reader.ReadToEndAsync();
                    }
                }
            }
            return result;
        }



    }
}
