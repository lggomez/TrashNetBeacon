using System;

using OpenQA.Selenium;

namespace TrashNetBeacon.DriverInterface
{
    public interface IBeaconWebDriver : IDisposable
    {
        TimeSpan MinUpdateInterval { get; }

        string DriverName { get; }

        string DriverProcessName { get; }

        string DeviceName { get; }

        IWebDriver Driver { get; }

        void DriverSetup();

        string GetStatus(string url);

        string GetStatus(string url, string user, string password);
    }
}