using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace RbxChecker
{
    internal static class Program
    {
        private static readonly List<string> Cookies = new List<string>();
        private static int _cookiesAlive;
        private static int _cookiesDead;
        
        private static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = false;
            
            if (!File.Exists("cookies.txt"))
                Console.WriteLine("[RBXCHECKER]: Please load the cookies in a file called `cookies.txt`");
            else
            {
                Console.WriteLine("[RBXCHECKER]: Loading `cookies.txt`");
                string[] cookies = File.ReadAllLines("cookies.txt");

                Parallel.For(0, cookies.Length, i =>
                {
                    string[] line = cookies[i].Split(new []{":"}, StringSplitOptions.RemoveEmptyEntries);
                    Cookies.Add(line.Length <= 2 ? cookies[i] : $"_|WARNING:{line[3]}");
                });
                
                Console.WriteLine($"[RBXCHECKER]: Loaded {Cookies.Count} cookies");

                Timer t = new Timer(TimerCallback, null, 0, 1);
                Parallel.ForEach(Cookies, async cookie => { await CheckCookie(cookie); });

                for (;;) {}
            }
        }

        private static void TimerCallback(object state) => Console.Title = $"[RBXCHECKER]: Working Cookies: {_cookiesAlive} || Dead Cookies: {_cookiesDead}";

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1,1);
        private static async Task CheckCookie(string cookie)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                HttpResponseMessage response = await "https://api.roblox.com/currency/balance"
                    .WithCookie(".ROBLOSECURITY", cookie)
                    .AllowHttpStatus("403")
                    .GetAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                    _cookiesAlive++;
                else
                    _cookiesDead++;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
    }
}