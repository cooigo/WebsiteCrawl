using AngleSharp;
using AngleSharp.Dom.Css;
using AngleSharp.Network;
using AngleSharp.Network.Default;
using AngleSharp.Parser.Css;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebResourceCatch
{
    public static class DownResourceManager
    {
        static readonly ConcurrentQueue<DownResource> downResourceQueue = new ConcurrentQueue<DownResource>();
        static readonly String _directoryWeb = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");

        static DownResourceManager()
        {
            Task.Factory.StartNew(() => {
                while (true)
                {
                    if (downResourceQueue.Count > 0)
                    {
                        var res = GetDownResource();
                        if (res != null)
                        {
                            SetResource(res.File, res.Content);
                        }
                    }
                    else
                    {
                        Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }

            }, TaskCreationOptions.LongRunning);
        }

        public static void Add(DownResource resource)
        {
            downResourceQueue.Enqueue(resource);
        }

        public static DownResource GetDownResource()
        {
            DownResource resource;
            if (!downResourceQueue.TryDequeue(out resource))
            {
                return null;
            }
            return resource;
        }

        private static void SetResource(string fullName, byte[] content)
        {
            File.WriteAllBytes(fullName, content);
        }

    }
    public class DownResource
    {
        public string File { get; set; }
        public byte[] Content { get; set; }
    }
}
