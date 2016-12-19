using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Extensions;
using System.IO;

namespace WebResourceCatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var downs =  File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "DownWebPage.txt");

            foreach (var page in downs)
            {
                Start(page).Wait();
            }
            Console.ReadLine();
        }

        static async Task Start(string url)
        {
            Console.WriteLine("开始 > "+ url);

            var config = Configuration.Default.WithDefaultLoader(setup => { setup.IsResourceLoadingEnabled = true; }, PageRequester.All).WithCss().WithJavaScript();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            Console.WriteLine("-- 结束 --");



        }
    }
}
