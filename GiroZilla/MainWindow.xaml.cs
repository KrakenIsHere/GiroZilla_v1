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
        public static MainWindow mainWindow;

        private UpdateManager _mgr;
        private UpdateInfo _updates;
        private ReleaseEntry _latestVersion;

        public bool IsLicenseVerified { get; set; }

        private int _connectionStatus;

        private bool _isCorrect;

        private bool _isManualChangeLicense;

        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Closing += MainWindow_Closing;

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

                Log.Error(ex, "An error occured while fetching the version");
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
                Log.Error(ex, "An error occured while setting the application title");
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
                Log.Warning(ex, "The path could not be made or found");
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

        public void OpenLicenseDialog()
        {
            _isManualChangeLicense = true;
            LicenseDialog.IsOpen = true;
        }

        /// <summary>Verifies the program license key.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void VerifyLicense(object sender, RoutedEventArgs e)
        {
            try
            {
                _isManualChangeLicense = false;
                Log.Information("Verifying License");

                var verified = PyroSquidUniLib.Verification.VerifyLicense.Verify(LicenseTextBox.Text, ErrorText);

                switch (verified)
                {
                    case true:
                        {
                            DialogHost.CloseDialogCommand.Execute(null, null);
                            Menu.IsEnabled = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "The license could not be verified");
            }
        }

        /// <summary>Cancels activation of the software.</summary>
        /// <param name="sender">The source of the event.</param>,
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void CancelLicense(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_isManualChangeLicense)
                {
                    default:
                        {
                            Application.Current.Shutdown();
                            break;
                        }
                    case true:
                        {
                            LicenseDialog.IsOpen = false;
                            _isManualChangeLicense = false;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error shutting down the application");
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

                switch (!string.IsNullOrWhiteSpace(localLicense))
                {
                    case true:
                        {
                            Log.Information("Local license found");

                            var count = 1;

                            foreach (var s in licenses)
                            {
                                switch (!IsLicenseVerified)
                                {
                                    case true:
                                        {
                                            _isCorrect = Hashing.Confirm(s, localLicense);

                                            switch (_isCorrect)
                                            {
                                                case true:
                                                    {
                                                        var searchLicenseId = $"SELECT `License_VALUE` FROM `licenses` WHERE `License_ID`='{count}'";

                                                        var license = AsyncMySqlHelper.GetString(searchLicenseId, "LicenseConnString").Result;
                                                        var query = $"SELECT * FROM `licenses` WHERE `License_VALUE`='{license}' AND `License_USED` > 0";
                                                        var canConnect = AsyncMySqlHelper.CheckDataFromDatabase(query, "LicenseConnString").Result;

                                                        switch (canConnect)
                                                        {
                                                            case true:
                                                                {
                                                                    _connectionStatus = 1;
                                                                    break;
                                                                }
                                                            case false:
                                                                {
                                                                    _connectionStatus = 0;
                                                                    break;
                                                                }
                                                        }

                                                        switch (_connectionStatus)
                                                        {
                                                            case 1:
                                                                {
                                                                    LicenseDialog.IsOpen = false;
                                                                    IsLicenseVerified = true;
                                                                    Log.Information(@"GiroZilla v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion + " loaded");
                                                                    break;
                                                                }
                                                            case 0:
                                                                {
                                                                    LicenseDialog.IsOpen = true;
                                                                    IsLicenseVerified = false;

                                                                    switch (!string.IsNullOrWhiteSpace(localLicense))
                                                                    {
                                                                        case true:
                                                                            {

                                                                                Log.Warning("This license is invalid and wil be reset");

                                                                                ErrorText.Text = "Din nuværende licens er ugyldig & vil blive nulstillet";

#if DEBUG
                                                                                PropertiesExtension.Set("License", "");
#else
                                                                        RegHelper.SetRegValue(@"Software\GiroZilla", "License", "", RegistryValueKind.String);
#endif
                                                                                break;
                                                                            }
                                                                    }

                                                                    break;
                                                                }
                                                            default:
                                                                {
                                                                    LicenseDialog.IsOpen = true;
                                                                    IsLicenseVerified = false;

                                                                    switch (!string.IsNullOrWhiteSpace(localLicense))
                                                                    {
                                                                        case true:
                                                                            {
                                                                                Log.Warning("Something went wrong validating this license");

                                                                                ErrorText.Text = "Kunne ikke validere din licens prøv igen senere";
                                                                                break;
                                                                            }
                                                                        default:
                                                                            {
                                                                                Log.Warning("Something went wrong validating this license (String empty or null)");

                                                                                ErrorText.Text = "Kunne ikke validere din licens";
                                                                                break;
                                                                            }
                                                                    }
                                                                    break;
                                                                }
                                                        }
                                                        break;
                                                    }
                                                case false:
                                                    {
                                                        count++;
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            LicenseDialog.IsOpen = true;
                            IsLicenseVerified = false;

                            Log.Warning("The license was not found");

                            ErrorText.Text = "Licensen blev ikke fundet";
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _connectionStatus = 2;

                Log.Error(ex, "Unexpected Error");
            }
        }
        #endregion

        #region WindowCommands
        private async void Update(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Manual update requested");
                CheckForUpdates(true);
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
            try
            {
                switch ((sender as Button)?.Name)
                {
                    case "ResultYes":
                        PropertiesExtension.Set("ShowUpdatePromptOnStart", "Yes");
                        DoUpdate();
                        _mgr.Dispose();
                        UpdateDialog.IsOpen = false;
                        break;

                    case "ResultNo":
                        UpdateDialog.IsOpen = false;
                        _mgr.Dispose();
                        break;

                    case "ResultDontRemind":
                        PropertiesExtension.Set("ShowUpdatePromptOnStart", "Disabled");
                        Log.Information("Update check on start was disabled by the user.");
                        _mgr.Dispose();
                        UpdateDialog.IsOpen = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private async void DoUpdate()
        {
            try
            {
                PropertiesExtension.Set("ShowUpdatePromptOnStart", "");

                Log.Information("Update accepted by the user.");

                try
                {
                    Log.Information("Downloading updates.");
                    await _mgr.DownloadReleases(_updates.ReleasesToApply);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error downloading the release");
                    // Notify user of the error
                }

                try
                {
                    Log.Information("Applying updates.");
                    await _mgr.ApplyReleases(_updates);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while applying updates");
                    // Notify user of the error
                }

                try
                {
                    await _mgr.CreateUninstallerRegistryEntry();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while trying to create uninstaller registry entry");
                    // Notify user of the error
                }


                var latestExe = Path.Combine(_mgr.RootAppDirectory, string.Concat("app-", _latestVersion.Version.Version.Major, ".", _latestVersion.Version.Version.Minor, ".", _latestVersion.Version.Version.Build), "GiroZilla.exe");
                Log.Information("Updates applied successfully.");

                Log.Information($"New exe path: {latestExe}");

                UpdateManager.RestartApp(latestExe);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private async void DoCheck(bool manualUpdate = false)
        {
            try
            {
                try
                {
                    _mgr = await UpdateManager.GitHubUpdateManager("https://github.com/TheWickedKraken/GiroZilla_V1");
                    _updates = await _mgr.CheckForUpdate();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Something went wrong getting the repository. Check for trailing slashes or if the repository is hosted on an enterprise server");
                }

                Log.Information($"Updates available: {_updates.ReleasesToApply.Any()} Current version: {_mgr.CurrentlyInstalledVersion()}");

                switch (_updates.ReleasesToApply.Any())
                {
                    case true:
                        {
                            _latestVersion = _updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                            Log.Information("Version {0} is available", _latestVersion.Version.ToString());
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
                            _mgr.Dispose();
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

        private async void CheckForUpdates(bool manualUpdate = false)
        {
            try
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
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Something went wrong while checking for updates");
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

                    break;

                case Key.Enter when LicenseTextBox.IsFocused:
                    VerifyBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
#endif
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _mgr.Dispose();
            }
            catch (NullReferenceException)
            {
                //Do Nothing
            }
            //Your code to handle the event
        }

        #endregion

        private void EULA(object sender, RoutedEventArgs e)
        {
            //https://drive.google.com/file/d/1vsE3ZIjDY9sEnh_tGcfKN-f3PC39U6LF/view?usp=sharing

            System.Diagnostics.Process.Start("https://drive.google.com/file/d/1vsE3ZIjDY9sEnh_tGcfKN-f3PC39U6LF/view?usp=sharing");
        }
    }
}
