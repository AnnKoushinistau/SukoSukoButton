using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace SUKOAuto
{
    class Generic
    {

        const string CHARGROUP = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん"
            + "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン"
            + "0123456789"
            + "abcdefghijklmnopqrstuvwxyz";

        public static string HttpRequest(string Url)
        {
            WebClient WClient = new WebClient();
            WClient.Encoding = Encoding.UTF8;
            string Result = WClient.DownloadString(Url);
            WClient.Dispose();

            return Result;
        }

        public static string ExeCommand(string command)
        {
            string Result;

            try
            {
                System.Diagnostics.Process Proc = new System.Diagnostics.Process();

                Proc.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
                Proc.StartInfo.UseShellExecute = false;
                Proc.StartInfo.RedirectStandardOutput = true;
                Proc.StartInfo.RedirectStandardInput = false;
                Proc.StartInfo.CreateNoWindow = true;
                Proc.StartInfo.Arguments = @"/c " + command;

                Proc.Start();
                Result = Proc.StandardOutput.ReadToEnd();

                Proc.WaitForExit();
                Proc.Close();

            }
            catch
            {
                Result = "";
            }
            return Result;
        }

        public static byte[][] SplitBytes(byte[] Content, int SplitLength)
        {
            var listResult = new List<byte[]>();

            long ResultCount = RepeatTimes(Content.Length, SplitLength);

            for (int i = 0; i < ResultCount; ++i)
            {
                int Start = i * SplitLength;
                int Length = (SplitLength > Content.Length - i * SplitLength) ? Content.Length - i * SplitLength : SplitLength;

                listResult.Add(Content.Skip(Start).Take(Length).ToArray());

            }

            return listResult.ToArray();

        }

        public static long RepeatTimes(long AllNumber, long EachNumber)
        {
            long Result;
            if (AllNumber % EachNumber == 0)
            {
                Result = AllNumber / EachNumber;
            }
            else
            {
                Result = AllNumber / EachNumber + 1;
            }
            return Result;
        }

        public static string RandStr(int MinLength, int MaxLength, string Chars = CHARGROUP)
        {
            int Length = new Random().Next(MinLength, MaxLength + 1);

            string Result = "";
            var Rand = new Random();

            for (int i = 0; i < Length; ++i)
            {
                Result += Chars[Rand.Next(0, Chars.Length)];
            }

            return Result;
        }

        public static string[] RegExp(string Content, string RegStr)
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


        public static string RSAEncryptString(string SourceString, string Pass)
        {
            byte[][] bContents = SplitBytes(Encoding.UTF8.GetBytes(SourceString), 117);
            var listResult = new List<string>();

            foreach (byte[] bContent in bContents)
            {
                listResult.Add(Convert.ToBase64String(RSAEncrypt(bContent, Pass)));
            }

                return string.Join("-", listResult);
        }

        public static string RSAEncryptFile(byte[] SourceFile, string Pass)
        {
            byte[][] bContents = SplitBytes(SourceFile, 117);
            var listResult = new List<string>();

            foreach (byte[] bContent in bContents)
            {
                listResult.Add(Convert.ToBase64String(RSAEncrypt(bContent, Pass)));
            }

            return string.Join("-", listResult);
        
        }


        static byte[] RSAEncrypt(byte[] bContent, string PublicKey)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSA = new System.Security.Cryptography.RSACryptoServiceProvider();
            RSA.FromXmlString(PublicKey);
            byte[] encryptedData = RSA.Encrypt(bContent, false);

            return encryptedData;
        }

        public static byte[] PrtScrn(int Size = 100)
        {
            byte[] Result = null;
            try
            {
                var Bmp1 = new Bitmap(Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
                var Graph1 = Graphics.FromImage(Bmp1);

                Graph1.CopyFromScreen(new Point(0, 0), new Point(0, 0), Bmp1.Size);
                Graph1.Dispose();


                var Bmp2 = new Bitmap(Screen.PrimaryScreen.Bounds.Width * Size / 100, Screen.PrimaryScreen.Bounds.Height * Size / 100);
                var Graph2 = Graphics.FromImage(Bmp2);
                Graph2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Graph2.DrawImage(Bmp1, 0, 0, Screen.PrimaryScreen.Bounds.Width * Size / 100, Screen.PrimaryScreen.Bounds.Height * Size / 100);

                var MemStream = new MemoryStream();
                Bmp2.Save(MemStream, ImageFormat.Png);

                Result = MemStream.GetBuffer();
                MemStream.Close();
            }
            catch
            {

            }
            return Result;
        }
    }
}
