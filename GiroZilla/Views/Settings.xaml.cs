using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using PyroSquidUniLib.Database;
using PyroSquidUniLib.Extensions;
using PyroSquidUniLib.Verification;
using Serilog;

namespace GiroZilla.Views
{
    public partial class Settings
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<Settings>();

        public Settings()
        {
            InitializeComponent();

            GetLogsPath();
            InsertCurrentDatabaseData();
            InsertLicenseData();
            GetAndInsertGiroSettings();
        }

        #region Database Settings
        /// <summary>Gets the database connection status.</summary>
        /// <returns>Status String</returns>
        private async Task<string> GetDatabaseConnectionStatus()
        {
            var output = "";

            try
            {
                var isWorking = await AsyncMySqlHelper.CheckDatabaseConnection("ConnString");

                switch (isWorking)
                {
                    case true:
                        {
                            output = "Forbundet";
                            break;
                        }
                    case false:
                        {
                            output = "Ingen Forbindelse";
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }

            return output;
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the CheckDatabaseButton control.
        /// </para>
        ///   <para>Checks the status of the connection to the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void CheckDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckDatabaseButton.IsEnabled = false;

                ConnectionStatusLabel.Content = await GetDatabaseConnectionStatus();
 
                CheckDatabaseButton.IsEnabled = true;
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "something went wrong!");
            }
        }

        /// <summary>Sets the new connection string to user settings.</summary>
        private async void SetNewConnectionString()
        {
            try
            { 
                PropertiesExtension.Set("ConnString", $"server={DatabaseIpBox.Text};user={DataUserNameBox.Text};database=GiroZilla;port={DatabasePortBox.Text};password={DataUserPasswordBox.Password};");
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Inserts the current database data to textboxes.</summary>
        private async void InsertCurrentDatabaseData()
        {
            try
            {
                var connString = PropertiesExtension.Get<string>("ConnString");

                var stringArray1 = connString.Split(';');

                CurrentDatabaseIp.Text = stringArray1[0].Split('=')[1];
                CurrentDatabasePort.Text = stringArray1[3].Split('=')[1];
                CurrentDatabaseUser.Text = stringArray1[1].Split('=')[1];

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the SetNewDatabaseButton control.
        /// </para>
        ///   <para>Sets new connection string in user settings</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void SetNewDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            SetNewConnectionString();

            InsertCurrentDatabaseData();

            await Task.FromResult(true);
        }
        #endregion

        #region License
        /// <summary>Gets the license data from the license database.</summary>
        /// <returns></returns>
        private static async Task<IEnumerable<DataRow>> GetLicenseData()
        {
            try
            {
#if DEBUG
                var qp = $@"SELECT `License_VALUE` FROM `licenses` WHERE `License_HASH`='{PropertiesExtension.Get<string>("License")}'";
                var license = await AsyncMySqlHelper.GetString(qp, "LicenseConnString");
                #else
                var qr = $@"SELECT `License_VALUE` FROM `licenses` WHERE `License_HASH`='{RegHelper.Readvalue(@"Software\", "GiroZilla", "License")}'";
                var license = await AsyncMySqlHelper.GetString(qr, "LicenseConnString"); 
                #endif

                var query = $"SELECT * FROM `licenses` WHERE `License_VALUE`='{license}'";

                var data = await AsyncMySqlHelper.GetDataFromDatabase(query, "LicenseConnString");

                await Task.FromResult(true);

                return data;
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "An error occured while fetching the license from the database!");
                return null;
            }
        }

        /// <summary>Inserts the license data  from the license database to textboxes.</summary>
        private async void InsertLicenseData()
        {
            try
            {
                var rows = await GetLicenseData();

                foreach (var row in rows)
                {
                    LicenseValue.Text = row["License_VALUE"].ToString();
                    LicenseValueTotalUses.Text = row["License_USES"].ToString();
                    LicenseValueUsesLeft.Text = (int.Parse(row["License_USES"].ToString()) - int.Parse(row["License_USED"].ToString())).ToString();

                    if (!string.IsNullOrWhiteSpace(row["License_MOBILE"].ToString()))
                    {
                        MobileLicenseValue.Text = row["License_MOBILE"].ToString();
                        MobileLicenseValueTotalUses.Text = row["License_MUSES"].ToString();
                        MobileLicenseValueUsesLeft.Text = (int.Parse(row["License_MUSES"].ToString()) - int.Parse(row["License_MUSED"].ToString())).ToString();
                    }
                    else
                    {
                        MobileLicenseValue.Text = "Ingen";
                        MobileLicenseValueTotalUses.Text = "Ingen";
                        MobileLicenseValueUsesLeft.Text = "Ingen";
                    }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the RefreshLicenseButton control.
        /// </para>
        ///   <para>Reloads the data from the license database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void RefreshLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            InsertLicenseData();

            await Task.FromResult(true);
        }

        private async void OverrideLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            VerifyLicense.Verify(NewLicenseTextBox.Text, NewLicenseError);

            await Task.FromResult(true);
        }

        #endregion

        #region Giro & Invoice Settings
        /// <summary>
        ///   <para>
        ///  Handles the Click event of the SaveInvoiceSettingsButton control.
        /// </para>
        ///   <para>Saves the giro setup to user settings</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void SaveInvoiceSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SetGiroSettings();

            await Task.FromResult(true);
        }

        /// <summary>Gets the and insert giro setup from user settings.</summary>
        private async void GetAndInsertGiroSettings()
        {
            var creditor = PropertiesExtension.Get<string>("CreditorNum");
            var invoice = PropertiesExtension.Get<string>("LastInvoiceNum");
            var address = PropertiesExtension.Get<string>("AddressLine");
            var reg = PropertiesExtension.Get<string>("Regnr");
            var account = PropertiesExtension.Get<string>("Accountnr");
            var message = PropertiesExtension.Get<string>("InvoiceMessage");

            var company = address.Split('+')[0];
            var road = address.Split('+')[1];
            var zip = address.Split('+')[2];

            CreditorNumTextBox.Text = creditor;
            InvoiceNumTextBox.Text = invoice;
            CompanyNameTextBox.Text = company;
            AddressLineTextBox.Text = road;
            ZipCodeAndCityTextBox.Text = zip;
            RegNumTextBox.Text = reg;
            AccountNumTextBox.Text = account;
            InvoiceMessageTextBox.Text = message.Replace("+","\n");

            await Task.FromResult(true);
        }

        private static async Task<string> InvoiceMessageFormat(string input)
        {
            await Task.FromResult(true);
            return input.Replace(Environment.NewLine, "+");
        }

        /// <summary>Sets the new giro setup to user settings.</summary>
        private async void SetGiroSettings()
        {
            try
            {
                PropertiesExtension.Set("CreditorNum", CreditorNumTextBox.Text);
                PropertiesExtension.Set("LastInvoiceNum", InvoiceNumTextBox.Text);
                PropertiesExtension.Set("AddressLine", $"{CompanyNameTextBox.Text}+{AddressLineTextBox.Text}+{ZipCodeAndCityTextBox.Text}");
                PropertiesExtension.Set("Regnr", RegNumTextBox.Text);
                PropertiesExtension.Set("Accountnr", AccountNumTextBox.Text);
                PropertiesExtension.Set("InvoiceMessage", await InvoiceMessageFormat(InvoiceMessageTextBox.Text));

                GiroErrorLabel.Content = "Ændringerne er gemt";

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Warning(ex, "Chagnes could not be saved!");
                GiroErrorLabel.Content = "Kunne ikke gemme ændringer";
            }
        }
        #endregion

        #region Logs
        private async void GetLogsPath()
        {
            LogsPath.Text = PropertiesExtension.Get<string>("LogsPath");
            await Task.FromResult(true);
        }

        private async void ClearLogs_OnClick(object sender, RoutedEventArgs e)
        {
            var currentLog = "GiroZilla_" + DateTime.Today.ToString("yyyyMMdd") + ".log";
            var info = new DirectoryInfo(PropertiesExtension.Get<string>("LogsPath"));

            foreach (var file in info.GetFiles())
            {
                if (file.Name == currentLog) continue;
                file.Delete();
            }

            await Task.FromResult(true);
            Log.Information("Logs was cleared");
        }

        private async void LogsPathBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            var currentLog = "GiroZilla_" + DateTime.Today.ToString("yyyyMMdd") + ".log";
            var info = new DirectoryInfo(PropertiesExtension.Get<string>("LogsPath"));

            using (var fd = new FolderBrowserDialog())
            {
                var result = fd.ShowDialog();

                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(fd.SelectedPath)) return;

                foreach (var file in info.GetFiles())
                {
                    if (file.Name == currentLog) continue;
                    File.Move(file.FullName, fd.SelectedPath + "/" + file.Name);
                }
                
                LogsPath.Text = fd.SelectedPath;
                PropertiesExtension.Set("LogsPath", fd.SelectedPath);
            }
            await Task.FromResult(true);
        }
        #endregion
    }
}
