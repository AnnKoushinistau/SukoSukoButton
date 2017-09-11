using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SUKOAuto.sukoList
{
    public interface ISukoListEntry
    {
        bool IsConstant();
        void SetMovies(string[] movies);
        string[] GetMovies();
        IEnumerable<string> GetProcessedMovies();
    }

    public abstract class AbstractSukoListEntry : ISukoListEntry
    {
        private string[] Movies;

        public abstract IEnumerable<string> GetProcessedMovies();
        public abstract bool IsConstant();

        public virtual string[] GetMovies()
        {
            return Movies;
        }

        public virtual void SetMovies(string[] movies)
        {
            Movies=movies;
        }
    }

    /**
     * チャンネル(ただしWebすこ砲と同じ方法でプレイリスト化して扱う)
     */
    public class ChannelSukoList : PlayListSukoList
    {
        private string ChannelId;

        public ChannelSukoList(string PlayList, int Head = -1) : base(ChannelToPlayList(PlayList),Head)
        {
            ChannelId = PlayList;
        }
        
        static string ChannelToPlayList(string Channel)
        {
            if (!Channel.StartsWith("UC"))
                throw new ArgumentException($"チャンネル\"{Channel}\"は不正です。再生リストと間違えていませんか?");
            var Buf=Channel.ToCharArray();
            Buf[1] = 'U';
            return new string(Buf);
        }

        public string Channel => ChannelId;
    }

    /**
     * 再生リスト
     */
    public class PlayListSukoList : AbstractSukoListEntry
    {
        private string PlayListId;
        private int Head = -1;

        public PlayListSukoList(string PlayList, int Head = -1)
        {
            this.PlayListId = PlayList;
            this.Head = Head;
        }

        public override IEnumerable<string> GetProcessedMovies()
        {
            return Head<0? GetMovies():GetMovies().Take(Head);
        }

        public override bool IsConstant()
        {
            return false;
        }

        public string PlayList => PlayListId;
        public int MaxSuko => Head;
    }

    /**
     * 狙い撃ち
     */
    public class ConstantSukoList : AbstractSukoListEntry
    {
        string[] Movies;

        public ConstantSukoList(string[] Movies) {
            this.Movies = Movies;
        }

        public override string[] GetMovies() {
            return Movies;
        }

        public override IEnumerable<string> GetProcessedMovies()
        {
            return Movies;
        }

        public override bool IsConstant()
        {
            return true;
        }
    }

    /**
     * Webすこ砲のomaturi
     */
    public class OmaturiSukoList : AbstractSukoListEntry
    {
        public override string[] GetMovies()
        {
            JObject json;
            using (var wc=new WebClient()) {
                json = JObject.Parse(wc.DownloadString("http://websuko.xyz/v1/target.php"));
            }
            return json["videos"].Select(a=>(string)a).ToArray();
        }

        public override IEnumerable<string> GetProcessedMovies()
        {
            return GetMovies();
        }

        public override bool IsConstant()
        {
            return false;
        }
    }

    public class SukoListUtils {
        public static IEnumerable<ISukoListEntry> Parse(TextReader reader) {
            XDocument doc = XDocument.Load(reader);
            var root = doc.Element("SukoList");
            var elements = root.Elements();
            var entries = new List<ISukoListEntry>();
            var omaturiAdded = false;
            foreach (var element in elements)
            {
                switch (element.Name.LocalName) {
                    case "omaturi":
                    case "Omaturi":
                        if (omaturiAdded)
                            throw new ArgumentException("\"omaturi\"は2回以上入れられません。");
                        entries.Add(new OmaturiSukoList());
                        omaturiAdded = true;
                        break;
                    case "movies":
                    case "Movies":
                    case "videos":
                    case "Videos":
                        var regex = "^([vV]ideo|[mM]ovie)$";
                        var Movies = element.Elements()
                            .Where(a=>Regex.IsMatch(a.Name.LocalName,regex))
                            .Select(a=>a.Value)
                            .ToArray();
                        entries.Add(new ConstantSukoList(Movies));
                        break;
                    case "Channel":
                    case "channel":
                        {
                            int Head = -1;
                            if (element.Attribute("count") != null)
                            {
                                Head = int.Parse(element.Attribute("count").Value);
                            }
                            else if (element.Attribute("maxSuko") != null)
                            {
                                Head = int.Parse(element.Attribute("maxSuko").Value);
                            }
                            string Channel;
                            if (element.Attribute("channel") != null)
                            {
                                Channel = element.Attribute("channel").Value;
                            }
                            else
                            {
                                Channel = element.Value;
                            }
                            entries.Add(new ChannelSukoList(Channel, Head));
                        }
                        break;
                    case "Playlist":
                    case "playlist":
                    case "PlayList":
                    case "playList":
                        {
                            int Head = -1;
                            if (element.Attribute("count") != null)
                            {
                                Head = int.Parse(element.Attribute("count").Value);
                            }
                            else if (element.Attribute("maxSuko") != null)
                            {
                                Head = int.Parse(element.Attribute("maxSuko").Value);
                            }
                            string PlayList;
                            if (element.Attribute("playlist") != null)
                            {
                                PlayList = element.Attribute("playlist").Value;
                            }
                            else
                            {
                                PlayList = element.Value;
                            }
                            entries.Add(new PlayListSukoList(PlayList, Head));
                        }
                        break;
                }
            }
            return entries;
        }

        public static IEnumerable<ISukoListEntry> Parse(string xml)
        {
            return Parse(new StringReader(xml));
        }
    }
}