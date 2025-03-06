using Microsoft.Edge.SeleniumTools;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using WebCrawlSubsystem.Models;

namespace WebCrawlSubsystem
{
    internal static class WebCrawler
    {
        private const string EURO_URL = @"https://www.cursbnr.ro/curs-bnr-azi";
        private const string IRCC_URL = @"https://www.cursbnr.ro/ircc";
        private const string COOKIE_CONSENT_XPATH = "/html/body/div[6]/div[2]/div[2]/div[2]/div[2]/button[1]";
        private const string EURO_RATES_DATE_XPATH = @"/html/body/div[3]/div[1]/div/main/div[2]/div/div/div/table/thead/tr/th[3]";
        private const string EURO_RON_RATE_VALUE_XPATH = @"/html/body/div[3]/div[1]/div/main/div[2]/div/div/div/table/tbody/tr[1]/td[3]";
        private const string EURO_RON_RATE_CHANGE_XPATH = @"/html/body/div[3]/div[1]/div/main/div[2]/div/div/div/table/tbody/tr[1]/td[4]";
        private const string IRCC_DAILY_INDEX_TABLE_XPATH = @"/html/body/div[3]/div[1]/div/main/div[2]/div/div/table/tbody";
        private const string IRCC_DAILY_INDEX_TD_RELATIVE_XPATH = @".//tr/td";

        internal static WebCrawlResult Crawl()
        {
            QuarterlyIndex[] quarterlyIndices = new QuarterlyIndex[0];
            EuroExchangeRate euroExchangeRate = new EuroExchangeRate
            {
                Date = DateTime.Now.Date,
                Value = 0,
                PreviousDateChange = 0,
            };

            var driverService = EdgeDriverService.CreateChromiumService();
            driverService.HideCommandPromptWindow = true;
            var options = new EdgeOptions { UseChromium = true };
            options.AddArguments("--headless=new");
            using (EdgeDriver driver = new EdgeDriver(driverService, options))
            {
                WebDriverWait webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                driver.Navigate().GoToUrl(EURO_URL);

                IWebElement webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(COOKIE_CONSENT_XPATH)));
                webElement.Click();

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RATES_DATE_XPATH)));
                euroExchangeRate.Date = DateTime.Parse(webElement.Text);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RON_RATE_VALUE_XPATH)));
                euroExchangeRate.Value = decimal.Parse(webElement.Text);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RON_RATE_CHANGE_XPATH)));
                euroExchangeRate.PreviousDateChange = decimal.Parse(webElement.Text);

                driver.Navigate().GoToUrl(IRCC_URL);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(IRCC_DAILY_INDEX_TABLE_XPATH)));
                ReadOnlyCollection<IWebElement> dailyIndexElements = webElement.FindElements(By.XPath(IRCC_DAILY_INDEX_TD_RELATIVE_XPATH));

                quarterlyIndices = new QuarterlyIndex[dailyIndexElements.Count / 2 + 1];
                quarterlyIndices[0] = new QuarterlyIndex { Year = 2024, Quarter = 4, IndexPercentage = 5.55m };
                for (int index = 0; index < dailyIndexElements.Count; index += 2)
                {
                    string quarter = dailyIndexElements[index].Text;
                    string percent = dailyIndexElements[index + 1].Text.Replace(',', '.');

                    quarterlyIndices[index/2 + 1] = QuarterlyIndexBuilder.BuildQuarterlyIndex(quarter, percent);
                }
            }

            return new WebCrawlResult
            {
                QuarterlyIndices = quarterlyIndices,
                EuroExchangeRate = euroExchangeRate
            };
        }
    }
}