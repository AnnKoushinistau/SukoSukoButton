using Microsoft.VisualStudio.TestTools.UnitTesting;
using SUKOAuto.sukoList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUKOAuto.sukoList.Tests
{
    [TestClass]
    public class SukoListUtilsTests
    {
        [TestMethod]
        public void ParsePlayListTest()
        {
            var hikaru = "UUaminwG9MTO4sLYeC3s6udA";

            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- PlayList tag with text -->
    <PlayList>UUaminwG9MTO4sLYeC3s6udA</PlayList>
    <!-- PlayList tag with channel attribute -->
    <PlayList playlist=""UUaminwG9MTO4sLYeC3s6udA""/>
    <!-- PlayList tag with text and maxSuko attribute -->
    <PlayList maxSuko=""1"">UUaminwG9MTO4sLYeC3s6udA</PlayList>
    <!-- PlayList tag with channel attribute and maxSuko attribute -->
    <PlayList maxSuko=""1"" playlist=""UUaminwG9MTO4sLYeC3s6udA""/>
</SukoList>
").ToList();
            var suko0 = parsed[0];
            Assert.IsTrue(suko0 is PlayListSukoList);
            Assert.IsTrue((suko0 as PlayListSukoList).PlayList == hikaru);
            Assert.IsTrue((suko0 as PlayListSukoList).MaxSuko == -1);

            var suko1 = parsed[1];
            Assert.IsTrue(suko1 is PlayListSukoList);
            Assert.IsTrue((suko1 as PlayListSukoList).PlayList == hikaru);
            Assert.IsTrue((suko1 as PlayListSukoList).MaxSuko == -1);

            var suko2 = parsed[2];
            Assert.IsTrue(suko2 is PlayListSukoList);
            Assert.IsTrue((suko2 as PlayListSukoList).PlayList == hikaru);
            Assert.IsTrue((suko2 as PlayListSukoList).MaxSuko == 1);

            var suko3 = parsed[3];
            Assert.IsTrue(suko3 is PlayListSukoList);
            Assert.IsTrue((suko3 as PlayListSukoList).PlayList == hikaru);
            Assert.IsTrue((suko3 as PlayListSukoList).MaxSuko == 1);

        }

        [TestMethod]
        public void ParseChannelTest()
        {
            var hikaru = "UCaminwG9MTO4sLYeC3s6udA";
            var hikaruPL = "UUaminwG9MTO4sLYeC3s6udA";

            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- Channel tag with text -->
    <Channel>UCaminwG9MTO4sLYeC3s6udA</Channel>
    <!-- Channel tag with channel attribute -->
    <Channel channel=""UCaminwG9MTO4sLYeC3s6udA""/>
    <!-- Channel tag with text and maxSuko attribute -->
    <Channel maxSuko=""1"">UCaminwG9MTO4sLYeC3s6udA</Channel>
    <!-- Channel tag with channel attribute and maxSuko attribute -->
    <Channel maxSuko=""1"" channel=""UCaminwG9MTO4sLYeC3s6udA""/>
</SukoList>
").ToList();
            var suko0 = parsed[0];
            Assert.IsTrue(suko0 is ChannelSukoList);
            Assert.IsTrue((suko0 as ChannelSukoList).PlayList == hikaruPL);
            Assert.IsTrue((suko0 as ChannelSukoList).Channel == hikaru);
            Assert.IsTrue((suko0 as ChannelSukoList).MaxSuko == -1);

            var suko1 = parsed[1];
            Assert.IsTrue(suko1 is ChannelSukoList);
            Assert.IsTrue((suko1 as ChannelSukoList).PlayList == hikaruPL);
            Assert.IsTrue((suko1 as ChannelSukoList).Channel == hikaru);
            Assert.IsTrue((suko1 as ChannelSukoList).MaxSuko == -1);

            var suko2 = parsed[2];
            Assert.IsTrue(suko2 is ChannelSukoList);
            Assert.IsTrue((suko2 as ChannelSukoList).PlayList == hikaruPL);
            Assert.IsTrue((suko2 as ChannelSukoList).Channel == hikaru);
            Assert.IsTrue((suko2 as ChannelSukoList).MaxSuko == 1);

            var suko3 = parsed[3];
            Assert.IsTrue(suko3 is ChannelSukoList);
            Assert.IsTrue((suko3 as ChannelSukoList).PlayList == hikaruPL);
            Assert.IsTrue((suko3 as ChannelSukoList).Channel == hikaru);
            Assert.IsTrue((suko3 as ChannelSukoList).MaxSuko == 1);

        }

        [TestMethod]
        public void ParseMoviesVideosTest()
        {
            var hikaruGames = new string[] {
                "XmaIkLIj_tk",
                "mgrL-cxaJ8A",
                "1lG_1duX3Uc",
                "ORxxoLbKq1A",
                "geHqQt7OxVM",
                "vouotQqdJDg",
                "KFlPBerdIGQ",
                "pSN0S9pQZq4",
                "5y28JiFcudo",
                "Nvv_t_CEVIQ",
            };

            var raphael = new string[] {
                "gZsXyLn2BfY",
                "zKoZSZDmtFU",
                "4UxwFZFrbKQ",
                "Es0u39q-rLU",
                "1FhPn8LIqkY",
                "47TeW-jLzyQ",
                "DRqgkgLfkL0",
                "SfX5QGkc--A",
                "OqSQ19UWOn4",
                "WAJji7z-49U",
            };

            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- Movies tag -->
    <Movies>
        <Movie>XmaIkLIj_tk</Movie>
        <Movie>mgrL-cxaJ8A</Movie>
        <Movie>1lG_1duX3Uc</Movie>
        <Movie>ORxxoLbKq1A</Movie>
        <Movie>geHqQt7OxVM</Movie>
        <Movie>vouotQqdJDg</Movie>
        <Movie>KFlPBerdIGQ</Movie>
        <Movie>pSN0S9pQZq4</Movie>
        <Movie>5y28JiFcudo</Movie>
        <Movie>Nvv_t_CEVIQ</Movie>
    </Movies>
    <!-- Videos tag -->
    <Videos>
        <Video>gZsXyLn2BfY</Video>
        <Video>zKoZSZDmtFU</Video>
        <Video>4UxwFZFrbKQ</Video>
        <Video>Es0u39q-rLU</Video>
        <Video>1FhPn8LIqkY</Video>
        <Video>47TeW-jLzyQ</Video>
        <Video>DRqgkgLfkL0</Video>
        <Video>SfX5QGkc--A</Video>
        <Video>OqSQ19UWOn4</Video>
        <Video>WAJji7z-49U</Video>
    </Videos>
</SukoList>
").ToList();
            var suko0 = parsed[0];
            Assert.IsTrue(suko0 is ConstantSukoList);
            Assert.IsTrue((suko0 as ConstantSukoList).GetProcessedMovies()
                .SequenceEqual(hikaruGames));

            var suko1 = parsed[1];
            Assert.IsTrue(suko1 is ConstantSukoList);
            Assert.IsTrue((suko1 as ConstantSukoList).GetProcessedMovies()
                 .SequenceEqual(raphael));
        }

        [TestMethod]
        public void ParseOmaturiTest()
        {
            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- Omaturi tag -->
    <Omaturi/>
</SukoList>
").ToList();
            var suko0 = parsed[0];
            Assert.IsTrue(suko0 is OmaturiSukoList);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
            "\"omaturi\"は2回以上入れられません。")]
        public void ParseMultipleOmaturiTest()
        {
            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- Omaturi tag -->
    <Omaturi/>
    <!-- Again: it will cause an error -->
    <Omaturi/>
</SukoList>
");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
                "チャンネル\"UUaminwG9MTO4sLYeC3s6udA\"は不正です。再生リストと間違えていませんか?")]
        public void ParseIllegalChannelTest()
        {
            var parsed = SukoListUtils.Parse(@"
<SukoList>
    <!-- Channel tag with illegal value (use PlayList form) -->
    <Channel channel=""UUaminwG9MTO4sLYeC3s6udA""/>
</SukoList>
");
        }
    }
}