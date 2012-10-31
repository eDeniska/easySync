using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;

namespace easySync
{
    public class UpdateHelper
    {
        private String currentVersionFile = String.Empty;
        private String version = String.Empty;
        private String newVersion = String.Empty;
        private String updateURL = String.Empty;
        private String downloadedFile = String.Empty;
        private WebClient client = null;


        /// <summary>
        /// Update Helper creation
        /// </summary>
        /// <param name="fileUrl">URL of current-release.txt file</param>
        /// <param name="currentVersion">Current version of application</param>
        public UpdateHelper(String fileUrl, String currentVersion)
        {
            currentVersionFile = fileUrl;
            version = currentVersion;
            client = new WebClient();
            client.Proxy = WebRequest.DefaultWebProxy;
            if (client.Proxy != null)
            {
                client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
        }

        public static bool IsVersionHigher(String versionToCheck, String versionCurrent = Consts.Application.Version)
        {
            // spliting version components
            String[] check = versionToCheck.Split(Consts.Updater.WordDividers, StringSplitOptions.None);
            String[] current = versionCurrent.Split(Consts.Updater.WordDividers, StringSplitOptions.None);

            int segments = Math.Min(check.Count(), current.Count());

            for (int i = 0; i < segments; i++)
            {
                int checkResult = check[i].CompareTo(current[i]);
                if (checkResult < 0)
                {
                    return false;
                }
                else if (checkResult > 0)
                {
                    return true;
                }
            }

            if (check.Count() > current.Count())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check, if update is available 
        /// (could throw exceptions)
        /// </summary>
        /// <returns>Is update available?</returns>
        public bool IsUpdateAvailable()
        {
            String[] infoList = client.DownloadString(currentVersionFile).Split(Consts.Updater.FieldDivider, StringSplitOptions.RemoveEmptyEntries);

            newVersion = infoList[0];
            updateURL = infoList[1];

            return IsVersionHigher(newVersion, version);
        }

        /// <summary>
        /// Start background update process
        /// (could throw expections)
        /// </summary>
        /// <param name="mainWndow">Main application form - to close application</param>
        public void StartUpdate(Window mainWndow)
        {
            client.DownloadFileCompleted += UpdateDownloaded;

            downloadedFile = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName +
                Path.DirectorySeparatorChar + updateURL.Substring(updateURL.LastIndexOf(Consts.Updater.URLPathDivider) + 1);

            client.DownloadFileAsync(new Uri(updateURL), downloadedFile, mainWndow);
        }

        /// <summary>
        /// New version available on server
        /// </summary>
        /// <returns>Version available</returns>
        public String AvailableVersion()
        {
            return newVersion;
        }

        /// <summary>
        /// When update is downloaded, starts installation and closes application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDownloaded(object sender, AsyncCompletedEventArgs e)
        {
            if ((!e.Cancelled) && (e.Error == null))
            {
                try
                {
                    Process.Start(downloadedFile);
                    Window mainForm = e.UserState as Window;
                    mainForm.Close();
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Log.Write(String.Format(Consts.Updater.ErrorFormat, updateURL, downloadedFile));
                    Log.Write(Consts.Updater.ExceptionPrefix, e);
                }
            }
        }
    }
}
