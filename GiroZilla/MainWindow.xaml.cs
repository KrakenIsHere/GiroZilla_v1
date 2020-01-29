using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PyroSquidUniLib;
using PyroSquidUniLib.Database;
using PyroSquidUniLib.Encryption;
using Serilog;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Squirrel;
using Application = System.Windows.Application;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using PyroSquidUniLib.Extensions;
using PyroSquidUniLib.Verification;
using Button = System.Windows.Controls.Button;
using System.ComponentModel;

namespace GiroZilla
{
    public partial class MainWindow
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MainWindow>();

        UpdateManager mgr;
        UpdateInfo updates;
        ReleaseEntry latestVersion;

        public bool IsLicenseVerified { get; set; }

        private int _connectionStatus;

        private bool _isCorrect;

        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
        public MainWindow()
        {
            InitializeComponent();

            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

            VerifyLogsFolder();
            CheckLicense();
            SetTitle();
        }

        #region Title
        /// <summary> Gets our AssemblyInformationalVersion to put the Semantic versioning into effect.</summary>
        private static string GetVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            }
            catch (Exception ex)
            {

                Log.Error(ex, "An error occured while fetching the Version!");
                return string.Empty;
            }
        }

        /// <summary>Applies our Version and changes the title of our program.</summary>        
        private void SetTitle()
        {
            try
            {
                Title = "GiroZilla V" + GetVersion();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured while setting the application title!");
            }
        }
        #endregion

        #region Logging
        /// <summary>Verifies if the logs folder exists.</summary>
        private static void VerifyLogsFolder()
        {
            try
            {
                var folderPath = PropertiesExtension.Get<string>("LogsPath");

                switch (Directory.Exists(folderPath))
                {
                    case true:
                        return;

                    case false:
                        Directory.CreateDirectory(folderPath);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "The path could not be made or found!");
            }
        }

        /// <summary>Handles the OnItemClick event of the HamburgerMenu control.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void Menu_OnItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                // Set content
                Menu.Content = e.ClickedItem;
                // Close pane
                Menu.IsPaneOpen = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error using the menuitem with index \"{Index}\"", Menu.SelectedIndex);
            }
        }
        #endregion

        #region License
        /// <summary>Verifies the program license key.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void VerifyLicense(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Verifying License");

                var verified = PyroSquidUniLib.Verification.VerifyLicense.Verify(LicenseTextBox.Text, ErrorText);

                if (!verified)
                    return;
                DialogHost.CloseDialogCommand.Execute(null, null);
                Menu.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "The license could not be verified!");
            }
        }

        /// <summary>Cancels activation of the software.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void CancelLicense(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error shutting down the application!");
            }
        }

        /// <summary>Helps a user by changing text in the textbox for easier formatting of license key.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void LicenseTextboxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                LicenseTextBox.Text = LicenseTextBox.Text.ToUpper();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Something went wrong changing the character to upper case");
            }
        }

        /// <summary>  Verifies the license to check if the license has expired or don't exist.</summary>
        private void CheckLicense()
        {
            try
            {
                Log.Information("Checking License");

                const string getList = "SELECT * FROM licenses";
                var licenses = AsyncMySqlHelper.ReturnStringList(getList, "LicenseConnString").Result.ToList();

#if DEBUG
                var localLicense = PropertiesExtension.Get<string>("License");
#else
                    var localLicense = RegHelper.Readvalue(@"Software\", "GiroZilla", "License");
#endif

                if (!string.IsNullOrWhiteSpace(localLicense))
                {
                    Log.Information("Local license found");

                    var count = 1;

                    foreach (var s in licenses)
                    {
                        if (!IsLicenseVerified)
                        {
                            _isCorrect = Hashing.Confirm(s, localLicense);

                            switch (_isCorrect)
                            {
                                case true:
                                    var searchLicenseId = $"SELECT `License_VALUE` FROM `licenses` WHERE `License_ID`='{count}'";

                                    var license = AsyncMySqlHelper.GetString(searchLicenseId, "LicenseConnString").Result;
                                    var query = $"SELECT * FROM `licenses` WHERE `License_VALUE`='{license}' AND `License_USED` > 0";
                                    var canConnect = AsyncMySqlHelper.CheckDataFromDatabase(query, "LicenseConnString").Result;

                                    switch (canConnect)
                                    {
                                        case true:
                                            _connectionStatus = 1;
                                            break;

                                        case false:
                                            _connectionStatus = 0;
                                            break;
                                    }

                                    switch (_connectionStatus)
                                    {
                                        case 1:
                                            LicenseDialog.IsOpen = false;
                                            IsLicenseVerified = true;
                                            Log.Information(@"GiroZilla v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion + " loaded");
                                            break;

                                        case 0:
                                            LicenseDialog.IsOpen = true;
                                            IsLicenseVerified = false;

                                            if (!string.IsNullOrWhiteSpace(localLicense))
                                            {
                                                Log.Information("This license is invalid and wil be reset!");

                                                ErrorText.Text = "Din nuværende licens er ugyldig & vil blive nulstillet";

#if DEBUG
                                                PropertiesExtension.Set("License", "");
#else
                                                RegHelper.SetRegValue(@"Software\GiroZilla", "License", "", RegistryValueKind.String);
#endif
                                            }

                                            break;

                                        default:
                                            LicenseDialog.IsOpen = true;
                                            IsLicenseVerified = false;

                                            if (!string.IsNullOrWhiteSpace(localLicense))
                                            {
                                                Log.Information("Something went wrong validating this license, try again later!");

                                                ErrorText.Text = "Kunne ikke validere din licens prøv igen senere";
                                            }
                                            break;
                                    }
                                    break;

                                case false:
                                    count++;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    LicenseDialog.IsOpen = true;
                    IsLicenseVerified = false;

                    Log.Error("The license was not found!");

                    ErrorText.Text = "Licensen blev ikke fundet!";
                }
            }
            catch (Exception ex)
            {
                _connectionStatus = 2;

                Log.Error(ex, "Something went wrong!");
            }
        }
        #endregion

        #region WindowCommands
        private async void Update(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Manual update requested");
                CheckForUpdates(true, true);
                mgr.Dispose();
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't do a manual update");
            }
        }
        #endregion

        #region Update

        private void ResultBtn_Click(object sender, EventArgs e)
        {
            switch ((sender as Button)?.Name)
            {
                case "ResultYes":
                    PropertiesExtension.Set("ShowUpdatePromptOnStart", "Yes");
                    DoUpdate();
                    mgr.Dispose();
                    UpdateDialog.IsOpen = false;
                    break;

                case "ResultNo":
                    UpdateDialog.IsOpen = false;
                    mgr.Dispose();
                    break;

                case "ResultDontRemind":
                    PropertiesExtension.Set("ShowUpdatePromptOnStart", "Disabled");
                    Log.Information("Update check on start was disabled by the user.");
                    mgr.Dispose();
                    UpdateDialog.IsOpen = false;
                    break;
            }
        }

        private async void DoUpdate()
        {
            PropertiesExtension.Set("ShowUpdatePromptOnStart", "");

            Log.Information("Update accepted by the user.");

            try
            {
                Log.Information("Downloading updates.");
                await mgr.DownloadReleases(updates.ReleasesToApply);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading the release!");
                // Notify user of the error
            }

            try
            {
                Log.Information("Applying updates.");
                await mgr.ApplyReleases(updates);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while applying updates!");
                // Notify user of the error
            }

            try
            {
                await mgr.CreateUninstallerRegistryEntry();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while trying to create uninstaller registry entry!");
                // Notify user of the error
            }

            var latestExe = Path.Combine(mgr.RootAppDirectory, string.Concat("app-", latestVersion.Version.Version.Major, ".", latestVersion.Version.Version.Minor, ".", latestVersion.Version.Version.Build), "GiroZilla.exe");
            Log.Information("Updates applied successfully.");

            Log.Information($"New exe path: {latestExe}");

            UpdateManager.RestartApp(latestExe);
        }

        private async void DoCheck(bool manualUpdate = false)
        {
            try
            {
                try
                {
                    mgr = await UpdateManager.GitHubUpdateManager("https://github.com/TheWickedKraken/GiroZilla_V1");
                    updates = await mgr.CheckForUpdate();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Something went wrong getting the repository. Check for trailing slashes or if the repository is hosted on an enterprise server!");
                }

                Log.Information($"Updates available: {updates.ReleasesToApply.Any()} Current version: {mgr.CurrentlyInstalledVersion()}");

                switch (updates.ReleasesToApply.Any())
                {
                    case true:
                        {
                            latestVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                            Log.Information("Version {0} is available", latestVersion.Version.ToString());
                            UpdateDialog.IsOpen = true;
                            break;
                        }
                    case false:
                        {
                            Log.Information("No updates detected.");
                            switch (manualUpdate)
                            {
                                case true:
                                    {
                                        MessageBox.Show("Ingen opdateringer fundet");
                                        break;
                                    }
                            }
                            mgr.Dispose();
                            UpdateDialog.IsOpen = false;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Got error while checking for updates");
            }
        }

        private async void CheckForUpdates(bool check = false, bool manualUpdate = false)
        {
            if (!check) return;

            try
            {
                switch (IsLicenseVerified)
                {
                    case true:
                        {
                            var result = PropertiesExtension.Get<string>("ShowUpdatePromptOnStart");

                            switch (result)
                            {
                                case "Yes":
                                    {
                                        DoCheck(manualUpdate);
                                        break;
                                    }
                                case "Disabled":
                                    {
                                        switch (manualUpdate)
                                        {
                                            case true:
                                                {
                                                    DoCheck(manualUpdate);
                                                    break;
                                                }
                                            default:
                                                {
                                                    Log.Information("Update check is disabled.");
                                                    UpdateDialog.IsOpen = false;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong while checking for updates");
                UpdateDialog.IsOpen = false;
            }
        }

        #endregion

        #region MainWindows Events

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
#if DEBUG
            switch (e.Key)
            {
                case Key.NumPad0:
                    UpdateDialog.IsOpen = !UpdateDialog.IsOpen;
                    Console.WriteLine($@"Update dialog is open: {UpdateDialog.IsOpen}");
                    break;

                case Key.Enter when LicenseTextBox.IsFocused:
                    VerifyBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
#endif
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckForUpdates(true);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                mgr.Dispose();
            }
            catch (NullReferenceException)
            {
                //Do Nothing
            }
            //Your code to handle the event
        }

        #endregion
    }
}
