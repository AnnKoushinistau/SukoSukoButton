using OpenQA.Selenium;

namespace SUKOAuto
{
    public interface ISukoSukoMachine
    {
        string[] FindMoviesInPlayList(IWebDriver Chrome, string Channel);
        void Login(IWebDriver Chrome, string Mail, string Pass);
        void ReLogin(IWebDriver Chrome, string ContinuationURL);
        void Suko(IWebDriver Chrome, string MovieID);
    }
}