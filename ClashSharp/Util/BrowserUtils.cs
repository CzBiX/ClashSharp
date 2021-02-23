using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClashSharp.Util
{
    public static class BrowserUtils
    {
        private const string RegUserChoice =
            @"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";

        private static readonly string[] SupportedBrowsers =
        {
            "Chrome",
            "Chromium",
            "MSEdge",
        };

        [SuppressMessage("ReSharper", "UseNullPropagation")]
        [SuppressMessage("ReSharper", "UseNegatedPatternMatching")]
        public static string? FindAppBrowser()
        {
            using var userChoice = Registry.CurrentUser.OpenSubKey(RegUserChoice);

            var progId = userChoice?.GetValue("ProgId") as string;
            if (progId == null)
            {
                return null;
            }

            using var clsKey = Registry.ClassesRoot.OpenSubKey(progId);
            if (clsKey == null)
            {
                return null;
            }

            var appUserModelId = clsKey.GetValue("AppUserModelId") as string;
            if (appUserModelId == null || !SupportedBrowsers.Contains(appUserModelId))
            {
                return null;
            }

            using var applicationKey = clsKey.OpenSubKey("Application");
            if (applicationKey == null)
            {
                return null;
            }

            var iconPath = applicationKey.GetValue("ApplicationIcon") as string;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (iconPath == null)
            {
                return null;
            }

            return iconPath.Substring(0, iconPath.LastIndexOf(','));
        }

        public static void OpenLinkInAppMode(string url)
        {
            var browser = FindAppBrowser();
            ProcessStartInfo info;
            if (browser == null)
            {
                info = new ProcessStartInfo(url)
                {
                    UseShellExecute = true,
                };
            }
            else
            {
                info = new ProcessStartInfo(browser, $"--app={url}");
            }

            Process.Start(info);
        }
    }
}
