using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using System.Text.RegularExpressions;


namespace SUKOAuto
{
    public class SukoSukoMachine : ISukoSukoMachine
    {
        const string URL_LOGIN = @"https://accounts.google.com/ServiceLogin";
        const string URL_MOVIE = @"https://www.youtube.com/watch?v={0}";
        const string URL_CHANNEL = @"https://www.youtube.com/channel/{0}/videos";
        const string URL_PLAYLIST = @"https://www.youtube.com/playlist?list={0}";
        public const string XPATH_1 = "//button[contains(@aria-label,'低く評価')]";
        public const string XPATH_2 = "//yt-formatted-string[contains(@aria-label,'低評価')]/..//button";
        public const string XPATH_3 = "//*[contains(@aria-label,'低評価') or contains(@aria-label,'低く評価')]/..//button";
        public const string INTEGRITY = "こう評価したら高評価になってこう評価したら低評価になる";

        public string[] FindMoviesInPlayList(IWebDriver Chrome, string Channel)
        {
            Chrome.Url = string.Format(URL_PLAYLIST, Channel);
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
                    else
                    {
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

            return Chrome.FindElements(By.XPath("//a[contains(@href,'/watch?v=')]"))
                .Select(a => a.GetAttribute("href"))
                .Distinct()
                .Select(a => RegExp(a, "(?<=.+/watch\\?v=)[\\dA-Za-z_-]+"))
                .SelectMany(A => A)
                .Distinct()
                .ToArray();
        }

        public void Suko(IWebDriver Chrome, string MovieID)
        {
            Chrome.Url = string.Format(URL_MOVIE, MovieID);
            System.Threading.Thread.Sleep(500);
            ReLogin(Chrome, Chrome.Url);

            Exception lastErr = null;

            for (int itr = 0; itr < 10; itr++)
            {
                try
                {
                    IWebElement SukoBtn;
                    try
                    {
                        SukoBtn = Chrome.FindElement(By.XPath(XPATH_1));
                    }
                    catch (Exception)
                    {
                        try
                        {
                            // MEMO: an alternative way what I found out
                            SukoBtn = Chrome.FindElement(By.XPath(XPATH_2));
                        }
                        catch
                        {
                            try
                            {
                                // MEMO: force search
                                SukoBtn = Chrome.FindElement(By.XPath(XPATH_3));
                            }
                            catch
                            {
                                throw new Exception();
                            }
                        }
                    }

                    if (SukoBtn.GetAttribute("aria-pressed") == "true")
                    {
                        // already downvoted
                        return;
                    }

                    /*
                    Actions action = new Actions(Chrome);
                    action.MoveToElement(SukoBtn).ClickAndHold().Perform();
                    System.Threading.Thread.Sleep(100);
                    action.MoveToElement(SukoBtn).Release().Perform();
                    */

                    SukoBtn.Click();
                    System.Threading.Thread.Sleep(1000);
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

        public void Login(IWebDriver Chrome, string Mail, string Pass)
        {
            Chrome.Url = URL_LOGIN;
            try
            {
                if (Chrome.Url == "https://accounts.google.com/ServiceLogin#identifier")
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
                    while (!Chrome.FindElement(By.Name("password")).Displayed) ;
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

        public void ReLogin(IWebDriver Chrome, string ContinuationURL)
        {
            if (Chrome.FindElements(By.XPath("//*[text() = \"ログイン\"]/..//paper-button")).Count() != 0)
            {
                // looks like we need to login again here
                Console.WriteLine("再ログイン中...");
                Chrome.FindElement(By.XPath("//*[text() = \"ログイン\"]/..//paper-button")).Click();
                System.Threading.Thread.Sleep(500);
                Chrome.Url = ContinuationURL;
            }
        }

        static string[] RegExp(string Content, string RegStr)
        {
            var listResult = new List<string>();
            var RegExp = new Regex(RegStr, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var Matches = RegExp.Matches(Content);

            foreach (Match Match in Matches)
            {
                listResult.Add(Match.Value);
            }

            return listResult.ToArray();
        }

        public void FindMoviesInPlayList()
        {
            throw new NotImplementedException();
        }
    }

}