using AngleSharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AngleSharp.Network.Default;
using System.IO;
using AngleSharp.Parser.Css;
using AngleSharp.Dom.Css;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Net;
using AngleSharp;
using System.Collections.Concurrent;

namespace WebResourceCatch
{
    public class PageRequester : IRequester
    {


        private readonly static HttpRequester _default = new HttpRequester();
        public readonly static String _directoryWeb = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        private static object obj = new object();

        public static IEnumerable<IRequester> All
        {
            get { yield return new PageRequester(); }
        }

        public Boolean SupportsProtocol(String protocol)
        {
            return _default.SupportsProtocol(protocol);
        }

        public async Task<IResponse> RequestAsync(IRequest request, CancellationToken cancel)
        {
            var response = await _default.RequestAsync(request, cancel).ConfigureAwait(false);

            if (response == null)
            {
                return null;
            }

            var bytes = await GetContentAsync(response).ConfigureAwait(false);

            Console.WriteLine(response.Address.Href);

            await SetResource(bytes, response.Headers["Content-Type"]).ConfigureAwait(false);

            return await GetResponseAsync(bytes).ConfigureAwait(false);

        }
        private async Task SetResource(byte[] content, string contentType)
        {
            var response = await GetResponseAsync(content);

            if (response == null)
            {
                return;
            }

            var fp = response.Address.Path;

            string ext = ".html";
            if (contentType.IndexOf("css") > -1)
            {
                ext = ".css";
            }
            else if (contentType.IndexOf("javascript") > -1)
            {
                ext = ".js";
            }
            else if (contentType.IndexOf("html") > -1)
            {
                ext = ".html";
            }

            if (string.IsNullOrEmpty(fp) || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(response.Address.Href)))
            {
                fp += "index" + ext;
            }
            else if (!string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(response.Address.Href)) && string.IsNullOrWhiteSpace(Path.GetExtension(response.Address.Href)))
            {
                fp += Path.GetFileNameWithoutExtension(response.Address.Href) + ext;
            }



            var rootPath = Path.Combine(_directoryWeb, response.Address.HostName);

            var fullName = Path.Combine(rootPath, fp.TrimStart('/').Replace('/', '\\'));
            var path = Path.GetDirectoryName(fullName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Console.WriteLine("> " + fullName);

            using (var ms = new MemoryStream())
            {
                var ctnt = response.Content;
                await ctnt.CopyToAsync(ms).ConfigureAwait(false);
                DownResourceManager.Add(new DownResource { Content = ms.ToArray(), File = fullName });
                ms.Close();

                if (Path.GetExtension(fullName) == ".css")
                {
                    await AddCssResourceAsync(response.Address.Scheme + "://" + response.Address.Host, path, rootPath, Encoding.UTF8.GetString(ms.ToArray())).ConfigureAwait(false);
                }
            }
        }
        private async Task AddCssResourceAsync(string host, string cssPath, string rootPath, string content)
        {
            var parser = new CssParser();
            var styles = await parser.ParseStylesheetAsync(content);

            foreach (var style in styles.Rules)
            {
                var s = style as ICssStyleRule;
                if (s != null && !string.IsNullOrWhiteSpace(s.Style.BackgroundImage) && s.Style.BackgroundImage.StartsWith("url"))
                {
                    var reg = new Regex("\"([^\"]*)\"");

                    var imgUrl = reg.Match(s.Style.BackgroundImage).Groups[1].Value;
                    var img = "";
                    if (!imgUrl.StartsWith("http"))
                    {
                        img = Path.Combine(cssPath, imgUrl);
                    }
                    else
                    {
                        var tempUrl = new Uri(imgUrl);
                        img = Path.Combine(cssPath, tempUrl.AbsolutePath.TrimStart('/')); ;
                    }

                    var imgpath = Path.GetDirectoryName(img);
                    try
                    {
                        if (!Directory.Exists(imgpath))
                        {
                            Directory.CreateDirectory(imgpath);
                        }
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine("错误" + imgpath);
                    }

                    var resUrl = host + img.Replace(rootPath, "").Replace("\\", "/");
                    var re = new Request
                    {
                        Address = new Url(resUrl),
                        Method = HttpMethod.Get
                    };

                    var response = await _default.RequestAsync(re, CancellationToken.None).ConfigureAwait(false);

                    var bytes = await GetContentAsync(response).ConfigureAwait(false);
                    await SetResource(bytes, response.Headers["Content-Type"]).ConfigureAwait(false);
                }
            }
        }
        private async Task<IResponse> GetResponseAsync(Byte[] content)
        {
            if (content == null)
            {
                return null;
            }
            using (var ms = new MemoryStream(content))
            {
                var code = ms.ReadInt();
                var addr = ms.ReadString();
                var hdrs = ms.ReadDictionary();
                var ctnt = new MemoryStream();
                await ms.CopyToAsync(ctnt).ConfigureAwait(false);
                ctnt.Position = 0;
                ms.Close();
                return new Response
                {
                    StatusCode = (HttpStatusCode)code,
                    Address = new Url(addr),
                    Headers = hdrs,
                    Content = ctnt
                };
            }
        }
        private async Task<Byte[]> GetContentAsync(IResponse response)
        {
            if (response != null)
            {
                var code = (int)response.StatusCode;
                var addr = response.Address.Href;
                var hdrs = response.Headers;
                var ctnt = response.Content;
                using (var ms = new MemoryStream())
                {
                    ms.Write(code);
                    ms.Write(addr);
                    ms.Write(hdrs);
                    await ctnt.CopyToAsync(ms).ConfigureAwait(false);
                    var res = ms.ToArray();
                    ms.Close();
                    return res;
                }
            }
            return null;
            
        }
    }
}
