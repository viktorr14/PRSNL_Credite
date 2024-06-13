using WebCrawlSubsystem.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Microsoft.Edge.SeleniumTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WebCrawlSubsystem
{
    internal static class WebCrawlPayloadProvider
    {
        private const string IRCC_URL = @"https://www.bnr.ro/Indicele-de-referința-pentru-creditele-consumatorilor--19492.aspx";
        private const string IRCC_SHOW_ALL_VALUES_ELEMENT_XPATH = @"//*[@id='contentDiv']/p[2]/a[2]";
        private const string IRCC_ALL_DATA_ELEMENT_ID = "alldata";
        private const string IRCC_DAILY_INDEX_TD_RELATIVE_XPATH = @".//tr/td[not(@scope)]";

        private const string EURO_URL = @"https://www.bnr.ro/Home.aspx";
        private const string EURO_RATES_DATE_XPATH = @"//div[@id='rates']/span[@class='date']";
        private const string EURO_RON_RATE_VALUE_XPATH = @"//div[@id='rates']/table/tbody/tr[1]/td[1]/span";
        private const string EURO_RON_RATE_PREVIOUS_DATE_RISE_XPATH = @"//div[@id='rates']/table/tbody/tr[1]/td[2]/span";

        internal static WebCrawlPayload Provide()
        {
            List<DailyIndex> dailyIndices = new List<DailyIndex>();
            EuroExchangeRate euroExchangeRate = new EuroExchangeRate
            {
                Date = DateTime.Now.Date,
                Value = 0,
                PreviousDateChange = 0,
            };

            var driverService = EdgeDriverService.CreateChromiumService();
            driverService.HideCommandPromptWindow = true;
            var options = new EdgeOptions { UseChromium = true };
            options.AddArgument("headless");
            using (EdgeDriver driver = new EdgeDriver(driverService, options))
            {
                WebDriverWait webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                driver.Navigate().GoToUrl(IRCC_URL);

                IWebElement webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath(IRCC_SHOW_ALL_VALUES_ELEMENT_XPATH)));
                webElement.Click();

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.Id(IRCC_ALL_DATA_ELEMENT_ID)));
                ReadOnlyCollection<IWebElement> dailyIndexElements = webElement.FindElements(By.XPath(IRCC_DAILY_INDEX_TD_RELATIVE_XPATH));

                dailyIndices = new List<DailyIndex>(dailyIndexElements.Count / 2);

                for (int index = 0; index < dailyIndexElements.Count; index += 2)
                {
                    string date = dailyIndexElements[index].Text;
                    string value = dailyIndexElements[index + 1].Text.Replace(',', '.');

                    dailyIndices.Add(new DailyIndex
                    {
                        Date = DateTime.Parse(date),
                        IndexPercentage = decimal.TryParse(value, out decimal percentage) ? percentage : decimal.Zero,
                    });
                }

                driver.Navigate().GoToUrl(EURO_URL);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RATES_DATE_XPATH)));
                euroExchangeRate.Date = DateTime.Parse(webElement.Text);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RON_RATE_VALUE_XPATH)));
                euroExchangeRate.Value = decimal.Parse(webElement.Text);

                webElement = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(EURO_RON_RATE_PREVIOUS_DATE_RISE_XPATH)));
                euroExchangeRate.PreviousDateChange = decimal.Parse(webElement.Text);
            }

            return new WebCrawlPayload
            {
                DailyIndices = dailyIndices.ToArray(),
                EuroExchangeRate = euroExchangeRate
            };
        }
    }
}
