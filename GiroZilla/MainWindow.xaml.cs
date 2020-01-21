using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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



namespace GiroZilla
{
    public partial class MainWindow
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MainWindow>();

        public bool IsDialogOpen { get; set; }

        public bool IsLicenseVerified { get; set; } = true;

        private int _connectionStatus;

        private bool _isCorrect;

        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
        public MainWindow()
        {
            InitializeComponent();

            VerifyLogsFolder();
            //CheckLicense();
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
                const string getList = "SELECT * FROM licenses";
                var licenses = AsyncMySqlHelper.ReturnStringList(getList, "LicenseConnString").Result.ToList();

#if DEBUG
                    var localLicense = PropertiesExtension.Get<string>("License");
#else
                    var localLicense = RegHelper.Readvalue(@"Software\", "GiroZilla", "License");
#endif

                if (!string.IsNullOrWhiteSpace(localLicense))
                {
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
                                            IsDialogOpen = false;
                                            IsLicenseVerified = true;
                                            Log.Information(@"GiroZilla v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion + " loaded");
                                            break;

                                        case 0:
                                            IsDialogOpen = true;
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
                                            IsDialogOpen = true;
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
                    IsDialogOpen = true;
                    IsLicenseVerified = false;

                    Log.Error("The license was not found!");

                    ErrorText.Text = "Licensen blev ikke fundet!";
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
        private void Update(object sender, RoutedEventArgs e)
        {
            // Download installer and execute it.

        }
        #endregion

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter when LicenseTextBox.IsFocused:
                    VerifyBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/MinikLambrecht/GiroZilla"))
                {
                    Log.Information("Checking for updates");

                    try
                    {
                        var updateInfo = await mgr.CheckForUpdate();

                        if (updateInfo.ReleasesToApply.Any())
                        {
                            var versionCount = updateInfo.ReleasesToApply.Count;
                            Log.Information($"{versionCount} update(s) found.");

                            var versionWord = versionCount > 1 ? "versions" : "version";
                            var message = new StringBuilder().AppendLine($"App is {versionCount} {versionWord} behind.")
                                .AppendLine("If you choose to update, changes wont take affect until App is restarted.")
                                .AppendLine("Would you like to download and install them?").ToString();

                            var result = MessageBox.Show(message, "App Update", MessageBoxButton.YesNo);
                            if (result != MessageBoxResult.Yes)
                            {
                                Log.Information("update declined by user.");
                                return;
                            }

                            Log.Information("Downloading updates");
                            var updateResult = await mgr.UpdateApp();
                            Log.Information(
                                $"Download complete. Version {updateResult.Version} will take effect when App is restarted.");
                        }
                        else
                        {
                            Log.Information("No updates detected.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"There was an issue during the update process! {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Some error");
            }
        }
    }
}
