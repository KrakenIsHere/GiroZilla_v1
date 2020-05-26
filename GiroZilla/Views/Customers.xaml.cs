using GiroZilla;
using System;
using Serilog;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using PyroSquidUniLib.Database;
using PyroSquidUniLib.Documents;
using PyroSquidUniLib.Extensions;
using PyroSquidUniLib.Models;

namespace GiroZilla.Views
{
    public partial class Customers
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<Customers>();

        DataSet data = new DataSet();
        DataSet Servicedata = new DataSet();

        public Customers()
        {
            InitializeComponent();

            InsertDataToCountyDropdown();
            AddObjectsToPaymentCombo();
            AddObjectsToInvoiceCombo();
            PrintHelper.FillInvoiceDesignCombo(InvoiceDesignCombo);
            SetData();
        }

        #region Customer

        /// <summary>Sets the data for the CustomerGrid ServiceGrid.</summary>
        private async void SetData()
        {
            try
            {
                var input = ClearTextSearch.Text;
                var query = "SELECT * FROM `all_customers` ";

                switch (!string.IsNullOrWhiteSpace(input))
                {
                    case true:
                        {
                            query +=
                                "WHERE " +
                                "(" +
                                "Fornavn  " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `all_customers` " +
                                $"WHERE " +
                                "(" +
                                "Efternavn " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `all_customers` " +
                                $"WHERE " +
                                "(" +
                                "ID " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `all_customers` " +
                                $"WHERE " +
                                "(" +
                                "Adresse " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `all_customers` " +
                                $"WHERE " +
                                "(" +
                                "Fejninger " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") ";

                            switch (CountySearch.SelectedIndex != 0 && CountySearch.SelectedIndex != -1)
                            {
                                case true:
                                    {
                                        query = query.Replace(") ", $" AND Postnr = {CountySearch.SelectedItem.ToString().Split(' ')[0]}) ");
                                        break;
                                    }
                            }
                            switch (MonthSearch.SelectedIndex != 0 && MonthSearch.SelectedIndex != -1)
                            {
                                case true:
                                    {
                                        query = query.Replace(") ", $" AND Måned = '{MonthSearch.SelectedItem.ToString().Split(' ')[0]}') ");
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (CountySearch.SelectedIndex != 0 && CountySearch.SelectedIndex != -1)
                            {
                                case true:
                                    {
                                        query += $"WHERE (Postnr = {CountySearch.SelectedItem.ToString().Split(' ')[0]}) ";

                                        switch (MonthSearch.SelectedIndex != 0 && MonthSearch.SelectedIndex != -1)
                                        {
                                            case true:
                                                {
                                                    query = query.Replace(") ", $" AND Måned = '{MonthSearch.SelectedItem.ToString().Split(' ')[0]}') ");
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        switch (MonthSearch.SelectedIndex != 0 && MonthSearch.SelectedIndex != -1)
                                        {
                                            case true:
                                                {
                                                    query += $"WHERE (Måned = '{MonthSearch.SelectedItem.ToString().Split(' ')[0]}') ";
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }
                            
                            break;
                        }
                }

                data = await AsyncMySqlHelper.GetSetFromDatabase(query, "ConnString");

                CustomerGrid.ItemsSource = data.Tables[0].DefaultView;

                ServiceGrid.ItemsSource = null;

                Log.Information("Successfully filled CustomerGrid ItemSource");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Something went wrong setting the data for the CustomerGrid");
            }
        }

        /// <summary>Updates the cutomer data to the database.</summary>
        private async void UpdateData()
        {
            try
            {
                AsyncMySqlHelper.UpdateSetToDatabase($"SELECT * FROM `all_customers`", data.Tables[0].DataSet, "ConnString");
                await Task.FromResult(true);
                Log.Information("Successfully updated customer data");
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Something went wrong updating customer data");
            }
        }

        /// <summary>Deletes selected customers from CustomerGrid</summary>
        /// <param name="row">The row.</param>
        private async void DeleteCustomers(DataRowView[] rows)
        {
            try
            {
                string message = $"Er du sikker du vil slette {rows.Count()} kunder?";
                string caption = "Advarsel";
                System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                System.Windows.MessageBoxResult result;

                // Displays the MessageBox.
                result = MessageBox.Show(message, caption, buttons);
                switch (result == System.Windows.MessageBoxResult.Yes)
                {
                    case true:
                        {
                            foreach (DataRowView row in rows)
                            {
                                var query = $"DELETE FROM `girozilla`.`customers` WHERE `Customer_ID` = {row.Row.ItemArray[0].ToString()}";

                                AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                                query = $"DELETE FROM `girozilla`.`route-customers` WHERE `Route-Customer_CUSTOMERID` = {row.Row.ItemArray[0].ToString()}";

                                AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                                Log.Information($"Successfully deleted a customer #{row.Row.ItemArray[0].ToString()}");
                            }

                            MessageBox.Show($"{rows.Length} Kunder blev slettet");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                MessageBox.Show("En uventet fejl er sket", "FEJL");
                Log.Error(ex, "An error occured while deleting a customer");
            }
        }

        /// <summary>Deletes selected customer from CustomerGrid</summary>
        /// <param name="row">The row.</param>
        private async void DeleteCustomer(DataRowView row)
        {
            try
            {
                string message = "Er du sikker du vil slette denne kunde?";
                string caption = "Advarsel";
                System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                System.Windows.MessageBoxResult result;

                // Displays the MessageBox.
                result = MessageBox.Show(message, caption, buttons);
                switch (result == System.Windows.MessageBoxResult.Yes)
                {
                    case true:
                        {
                            var query = $"DELETE FROM `girozilla`.`customers` WHERE `Customer_ID` = {row.Row.ItemArray[0].ToString()}";

                            AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                            query = $"DELETE FROM `girozilla`.`route-customers` WHERE `Route-Customer_CUSTOMERID` = {row.Row.ItemArray[0].ToString()}";

                            AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                            MessageBox.Show($"Kunde nr:{row.Row.ItemArray[0].ToString()} er nu slettet");
                            Log.Information($"Successfully deleted a customer #{row.Row.ItemArray[0].ToString()}");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                MessageBox.Show("En uventet fejl er sket", "FEJL");
                Log.Error(ex, "An error occured while deleting a customer");
            }
        }

        private async void ClearAddCustomerDialog()
        {
            NewCustomersFirstname.Text = "";
            NewCustomersLastname.Text = "";
            NewCustomersAdress.Text = "";
            NewCustomersZipCode.Text = "";
            NewCustomersCity.Text = "";
            NewCustomersMail.Text = "";
            NewCustomersHome.Text = "";
            NewCustomersMobile.Text = "";
            NewCustomersComment.Text = "";
            NewCustomersServices.Text = "1";

            NewCustomersMonth1.SelectedIndex = 1;
            NewCustomersMonth2.SelectedIndex = 0;
            NewCustomersMonth3.SelectedIndex = 0;
            NewCustomersMonth4.SelectedIndex = 0;
            NewCustomersMonth5.SelectedIndex = 0;
            NewCustomersMonth6.SelectedIndex = 0;

            await Task.FromResult(true);
        }

        /// <summary>Adds new customer data to the database</summary>
        /// <returns>success</returns>
        private async Task<bool> AddNewCustomerData()
        {
            try
            {
                switch (!string.IsNullOrWhiteSpace(NewCustomersAdress.Text) &&
                        !string.IsNullOrWhiteSpace(NewCustomersZipCode.Text) &&
                        !string.IsNullOrWhiteSpace(NewCustomersCity.Text))
                {
                    case true:
                        {
                            string[] months = new string[] {
                                NewCustomersMonth1.SelectedValue.ToString(),
                                NewCustomersMonth2.SelectedValue.ToString(),
                                NewCustomersMonth3.SelectedValue.ToString(),
                                NewCustomersMonth4.SelectedValue.ToString(),
                                NewCustomersMonth5.SelectedValue.ToString(),
                                NewCustomersMonth6.SelectedValue.ToString()
                            };

                            var monthResult = "";

                            foreach (string str in months)
                            {
                                switch (!string.IsNullOrWhiteSpace(str) && str != "System.Windows.Controls.ComboBoxItem: Ingen Valgt")
                                {
                                    case true:
                                        {
                                            monthResult += $"{str}, ";
                                            break;
                                        }
                                }
                            }

                            monthResult = monthResult.Remove((monthResult.Length - 2), 2);

                            var NewServiceNum = NewCustomersServices.Text;

                            switch (string.IsNullOrWhiteSpace(NewServiceNum))
                            {
                                case true:
                                    {
                                        NewServiceNum = "0";
                                        break;
                                    }
                            }

                            var query = $"INSERT INTO `customers` " +
                                $"(" +
                                $"`Customer_FIRSTNAME`, " +
                                $"`Customer_LASTNAME`, " +
                                $"`Customer_ADDRESS`, " +
                                $"`Customer_ZIPCODE`, " +
                                $"`Customer_CITY`, " +
                                $"`Customer_SERVICES_NEEDED`, " +
                                $"`Customer_COMMENT`, " +
                                $"`Customer_PHONE_MOBILE`, " +
                                $"`Customer_PHONE_HOME`, " +
                                $"`Customer_EMAIL`, " +
                                $"`Customer_MONTH` " +
                                $") " +
                                $"VALUES " +
                                $"(" +
                                $"'{NewCustomersFirstname.Text}', " +
                                $"'{NewCustomersLastname.Text}', " +
                                $"'{NewCustomersAdress.Text}', " +
                                $"'{NewCustomersZipCode.Text}', " +
                                $"'{NewCustomersCity.Text}', " +
                                $"'{NewServiceNum}', " +
                                $"'{NewCustomersComment.Text}', " +
                                $"'{NewCustomersMobile.Text}', " +
                                $"'{NewCustomersHome.Text}', " +
                                $"'{NewCustomersMail.Text}', " +
                                $"'{monthResult}'" +
                                $");";

                            AsyncMySqlHelper.SetDataToDatabase(query, "ConnString").Wait();

                            await Task.FromResult(true);
                            Log.Information("Successfully Added a new customer");
                            return true;
                        }
                    default:
                        {
                            MessageBox.Show("Data Mangler");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "An error occured while adding a new customer");
            }

            return false;
        }

        /// <summary>
        ///   <para>
        ///  Handles the SelectedCellsChanged event of the CustomerGrid control.
        /// </para>
        ///   <para>Updates ServiceGrid data</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectedCellsChangedEventArgs"/> instance containing the event data.</param>
        private async void CustomerGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SetServiceData(CustomerGrid.SelectedItem as DataRowView);
            DisableCollumnsForDataGrid();

            await Task.FromResult(true);
        }


        /// <summary>
        ///   <para>
        ///  Handles the CurrentCellChanged event of the CustomerGrid control.
        /// </para>
        ///   <para>Updates changed cell to database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void CustomerGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateData();

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the DeleteCustomerButton control.
        /// </para>
        ///   <para>Deletes selected customer from CustomerGrid</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void DeleteCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            switch (CustomerGrid.SelectedIndex != -1)
            {
                case true:
                    {
                        switch (CustomerGrid.SelectedItems.Count > 1)
                        {
                            case true:
                                {
                                    List<DataRowView> dataList = new List<DataRowView>();
                                    foreach(object obj in CustomerGrid.SelectedItems)
                                    {
                                        dataList.Add(obj as DataRowView);
                                    }

                                    DeleteCustomers(dataList.ToArray());
                                    break;
                                }
                            default:
                                {
                                    DeleteCustomer(CustomerGrid.SelectedItem as DataRowView);
                                    break;
                                }
                        }

                        SetData();
                        break;
                    }
                default:
                    {
                        MessageBox.Show("Venligst vælg en kunde");
                        break;
                    }
            }
            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the AddCustomerButton control.
        /// </para>
        ///   <para>Opens the AddCustomerDialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void AddCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            AddMonthsToCombobox();
            SetCountyDataToCombos();

            ClearAddCustomerDialog();

            AddCustomerDialog.IsOpen = true;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the CancelAddCustomer control.
        /// </para>
        ///   <para>Closes the AddCustomerDialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void CancelAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            AddCustomerDialog.IsOpen = false;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the FinalAddCustomer control.
        /// </para>
        ///   <para>Adds new customer data to the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void FinalAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            switch (AddNewCustomerData().Result)
            {
                case true:
                    {
                        AddCustomerDialog.IsOpen = false;
                        break;
                    }
            }

            SetData();

            await Task.FromResult(true);
        }

        #endregion

        #region Service

        /// <summary>Adds the new service data.</summary>
        private async Task<bool> AddNewServiceData()
        {
            try
            {
                var queryFind = $"SELECT * FROM `user_services` WHERE `Nummer` = '{InvoiceMethod.SelectedItem.ToString()} {InvoiceNum.Text}'";

                Console.WriteLine(queryFind); 

                switch (await AsyncMySqlHelper.CheckDataFromDatabase(queryFind, "ConnString"))
                {
                    case true:
                        {
                            MessageBox.Show($"{InvoiceMethod.SelectedItem.ToString()} {InvoiceNum.Text}\nExistere allerede!");
                            return false;
                        }
                }


                var year = 0;

                switch (DateTime.Now.Month == 12 && DateTime.Now.Day > 26)
                {
                    case true:
                        {
                            year = DateTime.Now.Year + 1;
                            break;
                        }
                    default:
                        {
                            year = DateTime.Now.Year;
                            break;
                        }
                }

                var payment = "";

                switch (PaymentMethod.SelectedIndex != 0 && PaymentMethod.SelectedIndex != -1)
                {
                    case true:
                        {
                            payment = PaymentMethod.SelectedValue.ToString();
                            break;
                        }
                }

                var query = $"INSERT INTO `services` " +
                    $"(" +
                    $"`Service_ID`, " +
                    $"`Customer_ID`, " +
                    $"`Service_DATE`, " +
                    $"`Service_YEAR`, " +
                    $"`Service_PAYMENTFORM`, " +
                    $"`Service_INVOICE_NUMBER`, " +
                    $"`Service_NUMBER`" +
                    $") " +
                    $"VALUES " +
                    $"(" +
                    $"{NewServiceID.Text}, " +
                    $"'{CustomerID.Text}', " +
                    $"'{DateSelect.Text}', " +
                    $"{year}, " +
                    $"'{payment}', " +
                    $"'{InvoiceMethod.SelectedItem.ToString()} {InvoiceNum.Text}', " +
                    $"'{Times.Text}'" +
                    $");";

                switch (!AsyncMySqlHelper.SetDataToDatabase(query, "ConnString").Result)
                {
                    case true:
                        {
                            throw new Exception("Unable to post new Service to database under at AddNewServiceData() Customers.xaml.cs");
                        }
                }

                foreach (ServiceProduct obj in ProductList.Items)
                {
                    query = $"INSERT INTO `service-products` " +
                    $"(" +
                    $"`Service-Product_SERVICEID`, " +
                    $"`Service-Product_PRODUCTID`, " +
                    $"`Service-Product_PRODUCTNAME`, " +
                    $"`Service-Product_PRODUCTPRICE`, " +
                    $"`Service-Product_PRODUCTDESCRIPTION`" +
                    $") " +
                    $"VALUES " +
                    $"(" +
                    $"{int.Parse(NewServiceID.Text)}, " +
                    $"{int.Parse(obj.ID)}, " +
                    $"'{obj.Name}', " +
                    $"{obj.Price.Replace(',', '.')}, " +
                    $"'{obj.Description}'" +
                    $");";

                    AsyncMySqlHelper.SetDataToDatabase(query, "ConnString").Wait();
                }

                PropertiesExtension.Set("LastInvoiceNum", InvoiceNum.Text);

                await Task.FromResult(true);
                Log.Information($"Successfully added a service to customer #{CustomerID.Text}");
                return true;
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Something went wrong adding new service data");
            }
            return false;
        }

        /// <summary>Sets the data to ServiceGrid.</summary>
        /// <param name="row">The row.</param>
        private async void SetServiceData(DataRowView row)
        {
            try
            {
                var query = $"SELECT * FROM `user_services` WHERE `Kunde ID` = {row.Row.ItemArray[0].ToString()}";

                switch (ContainServicesToYearOnly.IsChecked == true)
                {
                    case true:
                        {
                            query += $" AND `Aar` = {DateTime.Now.Year}";
                            break;
                        }
                }

                Servicedata = AsyncMySqlHelper.GetSetFromDatabase(query, "ConnString").Result;

                ServiceGrid.ItemsSource = Servicedata.Tables[0].DefaultView;

                await Task.FromResult(true);
                Log.Information("Successfully filled ServiceGrid ItemSource");
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Something went wrong setting the ServiceGrid data");
            }
        }

        private void ContainServicesToYearOnly_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                SetServiceData(CustomerGrid.SelectedItem as DataRowView);
            }
            catch (Exception)
            {
                //Do Nothing
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the CancelAddService control.
        /// </para>
        ///   <para>Closes the AddService Dialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void CancelAddService_Click(object sender, RoutedEventArgs e)
        {
            AddServiceDialog.IsOpen = false;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the FinalAddService control.
        /// </para>
        ///   <para>Adds the service to the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void FinalAddService_Click(object sender, RoutedEventArgs e)
        {
            switch (AddNewServiceData().Result)
            {
                case true:
                    {
                        AddServiceDialog.IsOpen = false;
                        SetServiceData(CustomerGrid.SelectedItem as DataRowView);
                        break;
                    }
            }
            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the FinalAddAndPrintStandardService control.
        /// </para>
        ///   <para>Adds the service to the database and prints invoice</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void FinalAddAndPrintStandardService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (InvoiceDesignCombo.SelectedIndex != 0 && InvoiceDesignCombo.SelectedIndex != -1)
                {
                    case true:
                        {
                            switch (!string.IsNullOrWhiteSpace(PayDateSelect.Text) && !string.IsNullOrWhiteSpace(PriceTextBox.Text))
                            {
                                case true:
                                    {
                                        switch (AddNewServiceData().Result)
                                        {
                                            case true:
                                                {
                                                    PrintHelper.SetupStandardInvoicePrint(
                                                        InvoiceDesignCombo.SelectedValue.ToString(),
                                                        PriceTextBox.Text,
                                                        PayDateSelect.Text,
                                                        long.Parse(InvoiceNum.Text),
                                                        ContainPayDateCheckBox,
                                                        ContainPriceCheckBox,
                                                        TaxWithPriceCheckBox,
                                                        DateSelect.Text,
                                                        InvoiceNum.Text,
                                                        int.Parse(CustomerID.Text),
                                                        ProductList,
                                                        IncludeProductsCheckBox
                                                        );

                                                    AddServiceDialog.IsOpen = false;
                                                    Log.Information("Successfully printed standard invoice");
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        MessageBox.Show("Data mangler");
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            MessageBox.Show("Venligst vælg et design");
                            break;
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
        ///  Handles the Click event of the FinalAddAndPrintService control.
        /// </para>
        ///   <para>Adds the service to the database and prints Giro-card</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void FinalAddAndPrintService_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                switch (AddNewServiceData().Result)
                {
                    case true:
                        {
                            PrintHelper.SetupGiroPrint(
                            PriceTextBox.Text,
                            PayDateSelect.Text,
                            long.Parse(InvoiceNum.Text),
                            int.Parse(CustomerID.Text),
                            ContainPriceCheckBox,
                            ContainPayDateCheckBox,
                            TaxWithPriceCheckBox,
                            ProductList,
                            IncludeProductsCheckBox
                            );

                            AddServiceDialog.IsOpen = false;
                            Log.Information("Successfully printed giro invoice");
                            break;
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
        ///  Handles the Click event of the OpenAddServiceDialogButton control.
        /// </para>
        ///   <para>Opens the AddService dialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void OpenAddServiceDialogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (CustomerGrid.SelectedIndex != -1)
                {
                    case true:
                        {
                            var row = CustomerGrid.SelectedItem as DataRowView;

                            var CID = row["ID"].ToString();
                            int ServicesNeeded;
                            try
                            {
                                ServicesNeeded = int.Parse(row["Fejninger"].ToString());
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "No service amount");
                                ServicesNeeded = 0;
                            }

                            CustomerID.Text = CID;

                            var query = $"SELECT * FROM `services` WHERE Service_YEAR = {DateTime.Now.Year} AND Customer_ID = {CID};";

                            var amountOfRows = await AsyncMySqlHelper.CheckDataRowsFromDatabase(query, "ConnString");

                            Times.Text = (amountOfRows + 1).ToString();

                            string message = "Denne kunde har allerede alle deres fejninger\nVil du fortsætte?";
                            string caption = "Advarsel";
                            System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                            System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.Yes;

                            switch (amountOfRows >= ServicesNeeded)
                            {
                                case true:
                                    {
                                        // Displays the MessageBox.
                                        result = MessageBox.Show(message, caption, buttons);
                                        break;
                                    }
                            }

                            switch (result == System.Windows.MessageBoxResult.Yes)
                            {
                                case true:
                                    {
                                        var date = DateTime.Now.Date;

                                        query = $"SELECT * FROM `services`;";

                                        var TotalServiceRows = await AsyncMySqlHelper.GetDataFromDatabase<Service>(query, "ConnString");

                                        int amountOfTotalServices = 1;

                                        try
                                        {
                                            amountOfTotalServices = int.Parse(TotalServiceRows.Last().Service_ID.ToString()) + 1;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex, "Unable to get any Service Data");
                                        }

                                        NewServiceID.Text = amountOfTotalServices.ToString();

                                        DateSelect.Text = date.ToString("dd-MMM-yy");
                                        date = date.AddMonths(1);
                                        PayDateSelect.Text = date.ToString("dd-MMM-yy");

                                        //This number can be 16 digits long!!
                                        var newInvoiceNumber = long.Parse(PropertiesExtension.Get<string>("LastInvoiceNum")) + 1;

                                        InvoiceNum.Text = newInvoiceNumber.ToString();
                                        ProductList.Items.Clear();
                                        AddProductsToCombo();

                                        AddServiceDialog.IsOpen = true;
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            System.Windows.MessageBox.Show("Venligst vælg en kunde");
                            break;
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

        #endregion

        #region City/County

        private async void SetCountyDataToCombos()
        {
            try
            {
                var query = "SELECT * FROM cities";

                var data = await AsyncMySqlHelper.GetDataFromDatabase<City>(query, "ConnString");

                NewCustomersCity.Items.Clear();
                NewCustomersZipCode.Items.Clear();

                foreach (City city in data)
                {
                    NewCustomersCity.Items.Add(city.City_NAME);
                    NewCustomersZipCode.Items.Add(city.City_ZIP);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Adds the new county data.
        /// </para>
        ///   <para>Adds new county data to the database if it doesn't already exist</para>
        /// </summary>
        /// <returns>success</returns>
        private async Task<bool> AddNewCountyData()
        {
            try
            {
                switch (!string.IsNullOrWhiteSpace(NewCustomersZipCode.Text) &&
                        !string.IsNullOrWhiteSpace(NewCustomersCity.Text))
                {
                    case true:
                        {
                            var query = $"SELECT * FROM `cities`";

                            var rows = AsyncMySqlHelper.GetDataFromDatabase<City>(query, "ConnString");

                            var doesExist = false;

                            foreach (City row in rows.Result)
                            {
                                switch (row.City_ZIP.ToString() == NewCustomersZipCode.Text)
                                {
                                    case true:
                                        {
                                            doesExist = true;
                                            break;
                                        }
                                }
                            }

                            switch (!doesExist)
                            {
                                case true:
                                    {
                                        string message = "Dette Område existere ikke i databasen, vil du tilføje den?";
                                        string caption = "Tilføj Område";
                                        System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                                        System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.Yes;

                                        // Displays the MessageBox.
                                        result = MessageBox.Show(message, caption, buttons);

                                        switch (result == System.Windows.MessageBoxResult.Yes)
                                        {
                                            case true:
                                                {
                                                    query = $"INSERT INTO `cities` " +
                                                    $"(" +
                                                    $"`City_ZIP`, " +
                                                    $"`City_NAME` " +
                                                    $") " +
                                                    $"VALUES " +
                                                    $"(" +
                                                    $"{NewCustomersZipCode.Text}, " +
                                                    $"'{NewCustomersCity.Text}' " +
                                                    $");";

                                                    AsyncMySqlHelper.SetDataToDatabase(query, "ConnString").Wait();

                                                    await Task.FromResult(true);
                                                    Log.Information("Successfully added new city data");
                                                    return true;
                                                }
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                MessageBox.Show("En uventet fejl er sket", "FEJL");
                Log.Error(ex, "An error occured while adding new county data");
            }
            return false;
        }

        /// <summary>
        ///   <para>
        ///  Handles the LostFocus event of the AddCounty control.
        /// </para>
        ///   <para>Adds new county data to the database if it doesn't already exist</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void AddCounty_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (AddNewCountyData().Result)
                {
                    case true:
                        {
                            MessageBox.Show("Det nye Område er nu tilføjet");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        #endregion

        #region Page Setup

        /// <summary>Adds the objects to payment combobox.</summary>
        private async void AddObjectsToPaymentCombo()
        {
            try
            {
                string[] payment = new string[]
                {
                "Bank Overførsel",
                "Girokort",
                "Faktura"
                };

                foreach (object obj in payment)
                {
                    PaymentMethod.Items.Add(obj);
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Adds the objects to invoice combobox.</summary>
        private async void AddObjectsToInvoiceCombo()
        {
            try
            {
                string[] payment = new string[]
                {
                "Gironr.",
                "Faktura."
                };

                foreach (object obj in payment)
                {
                    InvoiceMethod.Items.Add(obj);
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Inserts the data to county dropdown.</summary>
        private async void InsertDataToCountyDropdown()
        {
            try
            {
                var query = "SELECT * FROM `cities`";

                var data = AsyncMySqlHelper.GetDataFromDatabase<City>(query, "ConnString");

                foreach (City row in data.Result)
                {
                    CountySearch.Items.Add(row.City_ZIP.ToString() + " " + row.City_NAME);
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Disables the collumns for datagrids.</summary>
        private async void DisableCollumnsForDataGrid()
        {
            try
            {
                CustomerGrid.Columns[0].IsReadOnly = true;

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "An error occured while disabling columns");
            }
        }

        /// <summary>Adds months from database to combobox.</summary>
        private async void AddMonthsToCombobox()
        {
            try
            {
                var query = "SELECT * FROM `months`";

                var set = AsyncMySqlHelper.GetSetFromDatabase(query, "ConnString");

                var months = VariableManipulation.SortDataSetToStringArray(set.Result, "Month_ID", "Month_NAME");

                NewCustomersMonth1.Items.Clear();
                NewCustomersMonth2.Items.Clear();
                NewCustomersMonth3.Items.Clear();
                NewCustomersMonth4.Items.Clear();
                NewCustomersMonth5.Items.Clear();
                NewCustomersMonth6.Items.Clear();
                MonthSearch.Items.Clear();

                foreach (string month in months)
                {
                    NewCustomersMonth1.Items.Add(month);
                    NewCustomersMonth2.Items.Add(month);
                    NewCustomersMonth3.Items.Add(month);
                    NewCustomersMonth4.Items.Add(month);
                    NewCustomersMonth5.Items.Add(month);
                    NewCustomersMonth6.Items.Add(month);
                    MonthSearch.Items.Add(month);
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        #endregion

        #region Product List

        /// <summary>Adds the products from the database to the ProductComboBox.</summary>
        private async void AddProductsToCombo()
        {
            try
            {
                var query = "SELECT * FROM `products`";

                var data = AsyncMySqlHelper.GetDataFromDatabase<Product>(query, "ConnString");

                ProductCombo.Items.Clear();

                switch (ProductCombo.Items.Count <= 0)
                {
                    case true:
                        {
                            foreach (Product row in data.Result)
                            {
                                ProductCombo.Items.Add(row.Product_NAME.ToString());
                            }

                            ProductCombo.SelectedIndex = 0;
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(true);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the AddProduct control.
        /// </para>
        ///   <para>Adds a product to a list for a new service</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var query = "" +
                    $"SELECT * FROM `products` " +
                    $"WHERE Product_NAME = '{ProductCombo.SelectedValue.ToString()}'";

                var data = await AsyncMySqlHelper.GetDataFromDatabase<Product>(query, "ConnString");

                foreach (Product row in data)
                {
                    switch (string.IsNullOrWhiteSpace(row.Product_DESCRIPTION))
                    {
                        case true:
                        {
                            row.Product_DESCRIPTION = "";
                            break;
                        }
                    }

                    ProductList.Items.Add(new ServiceProduct { ID = row.Product_ID.ToString(), Name = row.Product_NAME, Price = row.Product_PRICE.Replace(',', '.'), Description = row.Product_DESCRIPTION.ToString() });
                }
                PrintHelper.CalculatePrice(ProductList, PriceTextBox);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }


        /// <summary>
        ///   <para>
        ///  Handles the Click event of the RemoveProduct control.
        /// </para>
        ///   <para>Removes a selected product from a list for a new service</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductList.Items.RemoveAt(ProductList.SelectedIndex);

                PrintHelper.CalculatePrice(ProductList, PriceTextBox);

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        #endregion

        #region Other Event Handlers

        private async void ContainPriceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TaxWithPriceCheckBox.IsEnabled = true;
            await Task.FromResult(true);
        }

        private async void ContainPriceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TaxWithPriceCheckBox.IsEnabled = false;
            TaxWithPriceCheckBox.IsChecked = false;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the TextChanged event of the ClearTextSearch control.
        /// </para>
        ///   <para>Lets you search for data in clear text</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private async void ClearTextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetData();

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the SelectionChanged event of the CountySearch control.
        /// </para>
        ///   <para>Lets you search by county</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private async void CountySearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetData();

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the ReloadButton control.
        /// </para>
        ///   <para>Gets the data from the database with the current search parameters</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            SetData();

            await Task.FromResult(true);
        }


        /// <summary>
        ///   <para>
        ///  Handles the LostFocus event of the PriceTextBox control.
        /// </para>
        ///   <para>Shows the price for the current service</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintHelper.FixPriceText(PriceTextBox);

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the PreviewTextInput event of the NumbersOnly control.
        /// </para>
        ///   <para>Gets only number input</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TextCompositionEventArgs"/> instance containing the event data.</param>
        private async void NumbersOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the SelectionChanged event of the MonthSearch control.
        /// </para>
        ///   <para>Changes search by month</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private async void MonthSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetData();

            await Task.FromResult(true);
        }

        private void ListViewPriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintHelper.CalculatePrice(ProductList, PriceTextBox);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private void NewCustomersCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (NewCustomersCity.SelectedIndex != -1)
                {
                    case true:
                        {
                            NewCustomersZipCode.SelectedIndex = NewCustomersCity.SelectedIndex;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private void NewCustomersZipCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (NewCustomersZipCode.SelectedIndex != -1)
                {
                    case true:
                        {
                            NewCustomersCity.SelectedIndex = NewCustomersZipCode.SelectedIndex;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        #endregion
    }
}
