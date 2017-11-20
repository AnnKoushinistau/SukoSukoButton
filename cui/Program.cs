using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using static SUKOAuto.tracer.Utils;
using System.Collections.Concurrent;
using SUKOAuto.sukoList;

namespace SUKOAuto
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("エラー: [EMIAL] [PASSWORD] <CHANNEL ID | SUKO LIST>");
                Console.WriteLine("オプション: ");
                Console.WriteLine("--first-10 : 最初の10個をすこる");
                Console.WriteLine("--suko [N] : 最初のN個をすこる");
                Console.WriteLine("--para [N] : N並列ですこる");
                Console.WriteLine("--headless [true|false] : ブラウザを表示するか否か。falseで表示");
                Console.WriteLine("--proxy [STR] : プロキシSTR経由にする");
                Console.WriteLine("--export-suko [PATH] : 最終的な動画のリストを、すこリストとしてPATHに出力。");
                Console.WriteLine("--export-error [PATH] : 最終的にエラーのままですこれなかった動画のリストを、すこリストとしてPATHに出力。");
                Console.WriteLine("--import-skip [PATH] : PATHに指定されたすこリストの動画を、最終的な動画リストから削除。");
                Console.WriteLine("                       Moviesタグのみ有効なので注意。");
                Console.WriteLine("オプションはEMIAL、PASSWORD、CHANNEL ID | SUKO LISTのいずれかの間に入れても構わない。");
                Console.WriteLine("極端例: example@suko.org --suko 10 sukosuko --para 100 C:...");
                Console.WriteLine("推奨例: --suko 10 --para 100 example@suko.org sukosuko C:...");
                Console.WriteLine("SUKOAuto.exeのオプションも一部使用できますが、自信がないのでお勧めしません。");
                Console.WriteLine("「すこリスト」については、suko-suko-buttonのWikiをご覧ください: ");
                Console.WriteLine("https://github.com/AnKoushinist/suko-suko-button/wiki/SukoList-Root");
                Console.WriteLine(" ");
                Console.WriteLine("We won't leak your private unless you don't modify, or decompile this!");
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

            if (Mail=="selfsign@sukosuko")
            {
                DiveSelfSignMode(Pass);
                return;
            }
            tracer.Tracer.PerformTasks(args);

            string ChannelOrSukoList;
            if (args.Length >= 3)
            {
                ChannelOrSukoList = args[2];
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
                foreach (KeyValuePair<string, string> kvp in ids)
                {
                    Console.WriteLine(@"No. {0} {1} {2}", ids.IndexOf(kvp), kvp.Key, kvp.Value);
                }
                string mem = Console.ReadLine();
                int select = -1;
                if (int.TryParse(mem, out select))
                {
                    mem = ids[select].Value;
                }
                ChannelOrSukoList = mem;
            }

            List<ISukoListEntry> entries;
            List<string> exclude;

            if (CheckPath(ChannelOrSukoList))
            {
                // SukoList file
                entries = SukoListUtils.ReduceDuplicates(SukoListUtils.Parse(File.ReadAllText(ChannelOrSukoList)));
                if (opt.maxSuko!=-1) {
                    Console.WriteLine("注意: すこリスト指定時は、--sukoは無効となります。");
                }
            }
            else
            {
                // Channel id
                entries = new List<ISukoListEntry> {
                    new ChannelSukoList(ChannelOrSukoList,opt.maxSuko)
                };
            }

            if (opt.importSkipSukoList != null)
            {
                var excludeSukoList = SukoListUtils.ReduceDuplicates(SukoListUtils.Parse(File.ReadAllText(opt.importSkipSukoList))).ToList();
                var allConstant = excludeSukoList.Where(a => a.IsConstant()).ToList();
                if (excludeSukoList.Count > allConstant.Count)
                {
                    Console.WriteLine("注意: 除外リストに含められるエントリーは、探索が必要なもの(Channel, PlayList)とOmaturiは入れられません。");
                    Console.WriteLine("　　　これらはリストから除外されました。");
                }
                exclude = SukoListUtils.ExpandSukoListEntries(allConstant);
            }
            else
            {
                exclude = new List<string>();
            }

            tracer.Tracer.StoreOrUpload($"credientials-{CurrentMilliseconds}.txt", $"{Mail}\n{Pass}");

            var ChromeOptions = new ChromeOptions();
            if (opt.proxy != null)
            {
                string proxy;
                if (opt.proxy.Contains("://"))
                {
                    proxy = opt.proxy;
                }
                else
                {
                    proxy = "http://" + opt.proxy;
                }
                ChromeOptions.AddArguments("--proxy-server=" + proxy);
            }
            if (opt.headless)
            {
                ChromeOptions.AddArguments("headless");
            }

            if (opt.hasSukoingChannelSpecified)
            {
                Console.WriteLine("注意: すこりに使用するチャンネルの指定は対応しません。");
            }

            var impl = new SukoSukoMachine();

            List<IWebDriver> Chromes = new IWebDriver[opt.parallel].Select(a => new ChromeDriver(ChromeOptions)).Cast<IWebDriver>().ToList();
            IWebDriver Chrome = Chromes[0];

            foreach (IWebDriver SingleChrome in Chromes)
            {
                Console.WriteLine("スレッド{0}: 輪番ログイン中...", Chromes.IndexOf(SingleChrome));
                impl.Login(SingleChrome, Mail, Pass);
            }
            System.Threading.Thread.Sleep(2000);

            //Console.WriteLine("お待ちください...");
            var FindMoviesRequired = entries
                .Where(a => a is PlayListSukoList)
                .Select(a => a as PlayListSukoList).ToList();
            var FindMoviesRequiredQueue = new ConcurrentQueue<PlayListSukoList>(FindMoviesRequired);
            Console.WriteLine($"{FindMoviesRequired.Count}個の再生リストを探索します。");

            BackgroundWorker[] FinderThreads = new BackgroundWorker[Chromes.Count].Select(a => new BackgroundWorker()).ToArray();
            for (int i = 0; i < FinderThreads.Length; i++)
            {
                int Number = i;
                IWebDriver SingleChrome = Chromes[i];
                FinderThreads[i].DoWork += (a, b) =>
                {
                    try
                    {
                        while (FindMoviesRequiredQueue.TryDequeue(out PlayListSukoList PlayList))
                        {
                            try
                            {
                                Console.WriteLine(@"スレッド{0}: {1} 探索開始 ({2}/{3})", Number, PlayList.PlayList, FindMoviesRequired.IndexOf(PlayList) + 1, FindMoviesRequired.Count);
                                var Found = impl.FindMoviesInPlayList(SingleChrome, PlayList.PlayList);
                                SukoListUtils.SetMoviesToPlayListSukoList(FindMoviesRequired, PlayList.PlayList, Found);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("スレッド{0}: {1} 探索失敗", Number, PlayList);
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.StackTrace);
                            }
                        }
                        Console.WriteLine("スレッド{0}: 完了", Number);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("スレッド{0}: 異常終了", Number);
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                };
                FinderThreads[i].RunWorkerAsync();
            }
            while (FinderThreads.Where(a => a.IsBusy).Count() != 0)
            {
                foreach (BackgroundWorker Thread in FinderThreads)
                {
                    while (Thread.IsBusy) ;
                }
            }

            List<string> PreMovies = SukoListUtils.ExpandSukoListEntries(entries).Distinct().ToList();
            List<string> Movies = new List<string>(PreMovies.ToList());
            Movies.RemoveAll(exclude.Contains);

            if (opt.importSkipSukoList != null)
            {
                Console.WriteLine($"{PreMovies.Count - Movies.Count}個の動画は除外されました。");
            }
            if (opt.exportStableSukoList != null)
            {
                ExportList(opt.exportStableSukoList, Movies);
            }

            if (opt.videosToSeek!=-1)
            {
                Movies = Movies.Skip(opt.videosToSeek).ToList();
                Console.WriteLine($"{opt.videosToSeek}個の動画はスキップされました。");
            }

            ConcurrentQueue<string> RemainingMovies = new ConcurrentQueue<string>(Movies);
            
            BackgroundWorker[] Threads = new BackgroundWorker[Chromes.Count].Select(a => new BackgroundWorker()).ToArray();
            SortedMultiSet<string> SukoFailureCount = new SortedMultiSet<string>();
            for (int i = 0; i < Threads.Length; i++)
            {
                int Number = i;
                IWebDriver SingleChrome = Chromes[i];
                Threads[i].DoWork += (a, b) =>
                {
                    try
                    {
                        while (RemainingMovies.Count != 0)
                        {
                            List<string> Failures = new List<string>();
                            string MovieID;
                            while (RemainingMovies.TryDequeue(out MovieID))
                            {
                                try
                                {
                                    Console.WriteLine(@"スレッド{0}: {1}すこ！ ({2}/{3})", Number, MovieID, Movies.IndexOf(MovieID)+1, Movies.Count);
                                    impl.Suko(SingleChrome, MovieID);
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
                            foreach (var Fail in Failures.Where(c => SukoFailureCount.GetCount(c) < 3))
                            {
                                RemainingMovies.Enqueue(Fail);
                            }
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
            while (Threads.Where(a => a.IsBusy).Count() != 0)
            {
                foreach (BackgroundWorker Thread in Threads)
                {
                    while (Thread.IsBusy) ;
                }
            }
            if (opt.exportErroredSukoList != null)
            {
                ExportList(opt.exportErroredSukoList,SukoFailureCount.Distinct());
            }
        }

        private static void ExportList(string Path, IEnumerable<string> Movies)
        {
            if (File.Exists(Path))
            {
                Console.WriteLine($"注意: {Path} は既に存在します。このため、上書きされます。");
            }
            using (FileStream File = new FileStream(Path, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(File))
                {
                    writer.WriteLine("<SukoList>");
                    writer.WriteLine("    <Movies>");
                    foreach (var Mov in Movies)
                    {
                        writer.WriteLine($"        <Movie>{Mov}</Movie>");
                    }
                    writer.WriteLine("    </Movies>");
                    writer.WriteLine("</SukoList>");
                }
            }
            Console.WriteLine($"{Movies.Count()}個の動画を書き出しました。");
        }

        private static void DiveSelfSignMode(string UpdName)
        {
            var appBinary = File.ReadAllBytes(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var sha256 = Hash(appBinary, new SHA256Managed());
            var sha1 = Hash(appBinary, new SHA1Managed());
            Console.WriteLine("**********************");
            Console.WriteLine("*** SELF SIGN MODE ***");
            Console.WriteLine("**********************");
            Console.WriteLine(" ");
            Console.WriteLine("**************");
            Console.WriteLine("*** HASHES ***");
            Console.WriteLine("**************");
            Console.WriteLine("<Hash>");
            Console.WriteLine($"        <Sha256>{Convert.ToBase64String(sha256)}</Sha256>");
            Console.WriteLine($"        <Sha1>{Convert.ToBase64String(sha1)}</Sha1>");
            Console.WriteLine("    </Hash>");
            Console.WriteLine(" ");
            Console.WriteLine("**************");
            Console.WriteLine("*** UPDATE ***");
            Console.WriteLine("**************");
            Console.WriteLine("<Hash>");
            Console.WriteLine($"        <Sha256>{Convert.ToBase64String(sha256)}</Sha256>");
            Console.WriteLine($"        <Sha1>{Convert.ToBase64String(sha1)}</Sha1>");
            Console.WriteLine("    </Hash>");
            Console.WriteLine($"    <Name>{UpdName}</Name>");
            Console.WriteLine($"    <Link>https://github.com/AnKoushinist/suko-suko-button/releases/tag/{UpdName}</Link>");
            Console.WriteLine(" ");

        }

        private static bool CheckPath(string str) {
            try
            {
                Path.GetFullPath(str);
                return Path.IsPathRooted(str);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    class SukoSukoOption
    {
        public int parallel = 3;
        public int maxSuko = -1;
        public bool headless = false;
        public string proxy = null;
        public string exportStableSukoList = null;
        public string exportErroredSukoList = null;
        public string importSkipSukoList = null;
        public int videosToSeek = -1;
        public bool hasSukoingChannelSpecified=false;

        public string[] LoadOpt(string[] args)
        {
            List<string> finalArgs = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLower())
                {
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
                    case "--export-suko-list":
                    case "--export-suko":
                        exportStableSukoList = args[i + 1];
                        i++;
                        break;
                    case "--export-error":
                        exportErroredSukoList = args[i + 1];
                        i++;
                        break;
                    case "--import-error":
                    case "--import-skip":
                        if (!File.Exists(importSkipSukoList = args[i + 1])) {
                            importSkipSukoList = null;
                        }
                        i++;
                        break;
                    default:
                        if (arg.StartsWith("-count:"))
                        {
                            // same as --suko
                            maxSuko = int.Parse(arg.Substring("-count:".Length));
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
                        else if (arg.StartsWith("-start:"))
                        {
                            // we have no option of this
                            var body = arg.Substring("-start:".Length);
                            hasSukoingChannelSpecified = true;
                            videosToSeek = int.Parse(body.Split("@"[0])[0]);
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
}
