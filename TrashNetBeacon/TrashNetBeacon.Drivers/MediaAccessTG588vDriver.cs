using System;
using System.Collections.Generic;
using System.Linq;

using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;

using TrashNetBeacon.DriverInterface;

namespace TrashNetBeacon.Drivers
{
    public class MediaAccessTg588VDriver : IBeaconWebDriver
    {
        public TimeSpan MinUpdateInterval => TimeSpan.FromSeconds(70);

        public string DriverName => "Arnet TechniColor Driver";

        public string DriverProcessName => "phantomjs";

        public string DeviceName => "MediaAccess TG 588V";

        public IWebDriver Driver { get; } = new PhantomJSDriver();

        public void DriverSetup()
        {
            // throw new NotImplementedException();
        }

        public string GetStatus(string url)
        {
            return this.GetStatusFromUrl(url);
        }

        private string GetStatusFromUrl(string url)
        {
            string status = string.Empty;

            try
            {
                this.Driver.Navigate().GoToUrl($"{url}/");

                this.WaitForPageLoad();
                this.WaitForPageElements();
                status = this.WaitForStatus(status);
            }
            catch (Exception ex)
            {
                status = $"FAIL: could not obtain status: {ex}";
            }

            return status;
        }

        private string WaitForStatus(string status)
        {
            var typeElement = this.Driver.FindElement(By.Id("Type"));
            var upstreamElement = this.Driver.FindElement(By.Id("LineUS"));
            var downstreamElement = this.Driver.FindElement(By.Id("LineDS"));

            var elementDictionary = new Dictionary<string, Dictionary<IWebElement, bool>>
                                        {
                                                { "type", new Dictionary<IWebElement, bool> { { typeElement, false } } },
                                                {
                                                    "upstream",
                                                    new Dictionary<IWebElement, bool> { { upstreamElement, false } }
                                                },
                                                {
                                                    "downstream",
                                                    new Dictionary<IWebElement, bool> { { downstreamElement, false } }
                                                }
                                        };

            do
            {
                if (!elementDictionary["type"][typeElement]
                    && !upstreamElement.Text.ToLowerInvariant().Contains("loading".ToLowerInvariant()))
                {
                    status = UpdateStatus(elementDictionary);
                    elementDictionary["type"][typeElement] = true;
                }

                if (!elementDictionary["upstream"][upstreamElement]
                    && !upstreamElement.Text.ToLowerInvariant().Contains("loading".ToLowerInvariant()))
                {
                    status = UpdateStatus(elementDictionary);
                    elementDictionary["upstream"][upstreamElement] = true;
                }

                if (!elementDictionary["downstream"][downstreamElement]
                    && !downstreamElement.Text.ToLowerInvariant().Contains("loading".ToLowerInvariant()))
                {
                    status = UpdateStatus(elementDictionary);
                    elementDictionary["downstream"][downstreamElement] = true;
                }
            }
            while (!elementDictionary["type"][typeElement] && !elementDictionary["upstream"][upstreamElement]
                   && !elementDictionary["downstream"][downstreamElement]);
            return status;
        }

        private void WaitForPageElements()
        {
            this.WaitForElement("Type");
            this.WaitForElement("LineUS");
            this.WaitForElement("LineDS");
        }

        private static string UpdateStatus(Dictionary<string, Dictionary<IWebElement, bool>> elements)
        {
            var status = $"Type: {elements["type"].First().Key.Text}{Environment.NewLine}";
            status += $"Line Rate - Upstream (Kbs): {elements["upstream"].First().Key.Text}{Environment.NewLine}";
            status += $"Line Rate - Downstream (Kbs): {elements["downstream"].First().Key.Text}{Environment.NewLine}";

            return status;
        }

        private void WaitForElement(string id)
        {
            new WebDriverWait(this.Driver, TimeSpan.FromSeconds(30)).Until(ExpectedConditions.ElementExists(By.Id(id)));
        }

        private void WaitForPageLoad()
        {
            try
            {
                this.Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            }
            catch
            {
                // Current firefox version of the driver throws upon page load completion
            }

            IWait<IWebDriver> wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(30.00));
            wait.Until(
                driver1 =>
                    ((IJavaScriptExecutor)this.Driver).ExecuteScript(@"return document.readyState")
                        .ToString()
                        .ToLowerInvariant()
                        .Equals("complete".ToLowerInvariant()));
        }

        public string GetStatus(string url, string user, string password)
        {
            throw new NotImplementedException();
        }

        #region IDisposable implementation

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing && (this.Driver != null))
            {
                this.Driver.Dispose();
                this.Driver.Close();
            }
        }

        ~MediaAccessTg588VDriver()
        {
            this.Dispose(false);
        }

        #endregion
    }
}