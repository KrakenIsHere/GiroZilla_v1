using GiroZilla;

using Serilog;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PyroSquidUniLib.Database;
using PyroSquidUniLib.Documents;
using PyroSquidUniLib.Extensions;
using PyroSquidUniLib.Models;

namespace GiroZilla.Views
{
    public partial class Invoice
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<Invoice>();

        DataSet data = new DataSet();
        DataSet productData = new DataSet();
        bool customerExists;

        public Invoice()
        {
            InitializeComponent();
            InvoiceSearchYearValue.Value = DateTime.Now.Year;
            AddObjectsToPaymentCombo();
            PrintHelper.FillInvoiceDesignCombo(InvoiceDesignCombo);
            SetData();
        }

        #region Service

        /// <summary>Sets up the PrintService dialog.</summary>
        /// <param name="ServiceRow">The service row.</param>
        /// <param name="ProductsTable">The products table.</param>
        private async void SetupPrintServiceDialog(DataRowView ServiceRow, DataTable ProductTable)
        {
            //ServiceRow Array form:
            //"ID = 0"
            //"Kunde ID = 1"
            //"Dato = 2"
            //"Antal Gange = 3"
            //"Betaling = 5"
            //"Nummer = 6"

            try
            {
                var giro = ServiceRow.Row["Nummer"].ToString().Replace(" ", "").Split('.')[0];
                var number = "";

                switch (ServiceRow.Row["Nummer"].ToString().Replace(" ", "").Split('.')[1] != null)
                {
                    case true:
                        {
                            number = ServiceRow.Row["Nummer"].ToString().Replace(" ", "").Split('.')[1];
                            break;
                        }
                }

                ServiceID.Text = ServiceRow.Row["ID"].ToString();
                CustomerID.Text = ServiceRow.Row["Kunde ID"].ToString();
                Times.Text = ServiceRow.Row["Antal Gange"].ToString();
                DateSelect.Text = ServiceRow.Row["Dato"].ToString();
                InvoiceNum.Text = number;

                var date = DateTime.Now.Date;
                date = date.AddMonths(1);
                PayDateSelect.Text = date.ToString("dd-MMM-yy");

                InvoiceMethod.Items.Add(giro);
                InvoiceMethod.SelectedIndex = InvoiceMethod.Items.Count - 1;

                switch (!string.IsNullOrWhiteSpace(ServiceRow.Row["Betaling"].ToString()))
                {
                    case true:
                        {
                            PaymentMethodText.Items.Add(ServiceRow.Row["Betaling"].ToString());
                            PaymentMethodText.SelectedIndex = PaymentMethodText.Items.Count - 1;
                            break;
                        }
                }

                ProductList.Items.Clear();

                switch (ProductTable != null)
                {
                    case true:
                        {
                            foreach (DataRow row in ProductTable.Rows)
                            {
                                ProductList.Items.Add(new ServiceProduct
                                {
                                    ID = row["Produkt ID"].ToString(),
                                    Name = row["Produkt Navn"].ToString(),
                                    Price = row["Pris"].ToString(),
                                    Description = row["Beskrivelse"].ToString()
                                });
                            }
                            break;
                        }
                }

                PrintHelper.CalculatePrice(ProductList, PriceTextBox);
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(true);
                Log.Error(ex, "An error occured while setting up the PrintService Dialog!");
            }
        }

        /// <summary>Updates the database with the changes made to the ServiceGrid.</summary>
        private async void UpdateServiceData()
        {
            try
            {
                AsyncMySqlHelper.UpdateSetToDatabase($"SELECT * FROM `User_services`", data.Tables[0].DataSet, "ConnString");
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Something went wrong updating the service data!");
            }
        }

        /// <summary>Sets the data to the ServiceGrid.</summary>
        private async void SetData()
        {
            try
            {
                var input = ClearTextSearch.Text;
                var query = "SELECT * FROM `user_services` ";

                switch (!string.IsNullOrWhiteSpace(input))
                {
                    case true:
                        {
                            query +=
                                $"WHERE " +
                                "(" +
                                "`ID` " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `user_services` " +
                                $"WHERE " +
                                "(" +
                                "`Kunde ID` " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") " +

                                $"UNION " +

                                "SELECT * FROM `user_services` " +
                                "WHERE " +
                                "(" +
                                "`Nummer` " +
                                "LIKE " +
                                $"'%{input}%'" +
                                $") ";

                            UnpayedOnlyCheck.IsEnabled = true;
                            switch (PaySearch.SelectedIndex != 0 && PaySearch.SelectedIndex != -1)
                            {
                                case true:
                                    {
                                        UnpayedOnlyCheck.IsEnabled = false;
                                        UnpayedOnlyCheck.IsChecked = false;
                                        query = query.Replace(") ", $" AND Betaling = '{PaySearch.SelectedItem.ToString()}') ");
                                        break;
                                    }
                                default:
                                    {
                                        switch (UnpayedOnlyCheck.IsChecked == true)
                                        {
                                            case true:
                                                {
                                                    query = query.Replace(") ", $" AND Betaling = '') ");
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }

                            switch (ThisYearOnlyCheck.IsChecked == true)
                            {
                                case true:
                                    {
                                        query = query.Replace(") ", $" AND Aar = {InvoiceSearchYearValue.Value.ToString()}) ");
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (PaySearch.SelectedIndex != 0 && PaySearch.SelectedIndex != -1)
                            {
                                case true:
                                    {
                                        query += $"WHERE Betaling = '{PaySearch.SelectedItem.ToString()}'";
                                        break;
                                    }
                                default:
                                    {
                                        switch (UnpayedOnlyCheck.IsChecked == true)
                                        {
                                            case true:
                                                {
                                                    query += $"WHERE Betaling = '' ";

                                                    switch (ThisYearOnlyCheck.IsChecked == true)
                                                    {
                                                        case true:
                                                            {
                                                                query += $"AND Aar = {InvoiceSearchYearValue.Value.ToString()} ";
                                                                break;
                                                            }
                                                    }
                                                    break;
                                                }
                                            default:
                                                {
                                                    switch (ThisYearOnlyCheck.IsChecked == true)
                                                    {
                                                        case true:
                                                            {
                                                                query += $"WHERE Aar = {InvoiceSearchYearValue.Value.ToString()} ";
                                                                break;
                                                            }
                                                    }
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

                ServiceGrid.ItemsSource = data.Tables[0].DefaultView;

                ProductGrid.ItemsSource = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured while setting ServiceGrid data!");
            }
        }

        /// <summary>Disables the collumns for datagrids.</summary>
        private async void DisableCollumnsForDataGrid()
        {
            try
            {
                ServiceGrid.Columns[0].IsReadOnly = true;

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "An error occured while disabling columns!");
            }
        }

        /// <summary>Marks the selected service data as payed.</summary>
        private async void MarkServiceDataAsPayed(DataRowView row)
        {
            try
            {
                switch (string.IsNullOrWhiteSpace(row["Betaling"].ToString()))
                {
                    case true:
                        {
                            var id = row["ID"].ToString();
                            var paymentFormIndex = PaymentMethod.SelectedIndex;

                            switch (paymentFormIndex != 0 && paymentFormIndex != -1)
                            {
                                case true:
                                    {
                                        var paymentForm = PaymentMethod.SelectedValue;

                                        var query = $"UPDATE `GiroZilla`.`services` SET `service_PAYMENTFORM` = '{paymentForm}' WHERE (`service_ID` = '{id}');";

                                        AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                                        UpdateServiceData();

                                        MessageBox.Show($"Betaling til fejning nr:{id} er nu opdateret");
                                        break;
                                    }
                                default:
                                    {
                                        MessageBox.Show($"Venligst vælg en betalingsform");
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            MessageBox.Show($"Denne Fejning allerede står som betalt");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                MessageBox.Show("Venligst vælg en fejning");
                Log.Warning(ex, "No service picked!");
            }
        }

        private async void InsertInvoiceCustomerInfo(DataRowView rowView)
        {
            try
            {
                CustomerInfoAddress.Text = "";
                CustomerInfoFirstname.Text = "";
                CustomerInfoLastname.Text = "";
                CustomerInfoID.Text = "";
                CustomerInfoNeededServices.Text = "";
                CustomerInfoMail.Text = "";
                CustomerInfoMobile.Text = "";
                CustomerInfoHome.Text = "";
                CustomerInfoServicesGotten.Text = "";

                var query = $"SELECT * FROM `customers` WHERE `Customer_ID` = {rowView.Row.ItemArray[1].ToString()}";

                customerExists = await AsyncMySqlHelper.CheckDataFromDatabase(query, "ConnString");
                var userData = await AsyncMySqlHelper.GetDataFromDatabase<Customer>(query, "ConnString");

                foreach (Customer row in userData)
                {
                    CustomerInfoAddress.Text = row.Customer_ADDRESS; // 1
                    CustomerInfoFirstname.Text = row.Customer_FIRSTNAME; // 2
                    CustomerInfoLastname.Text = row.Customer_LASTNAME; // 3
                    CustomerInfoID.Text = row.Customer_ID.ToString(); // 4
                    CustomerInfoNeededServices.Text = row.Customer_SERVICES_NEEDED.ToString(); // 5
                    CustomerInfoMail.Text = row.Customer_EMAIL; // 6
                    CustomerInfoMobile.Text = row.Customer_PHONE_MOBILE; // 7
                    CustomerInfoHome.Text = row.Customer_PHONE_HOME; // 8
                }

                query = $"SELECT * FROM `services` WHERE `Customer_ID` = {rowView.Row.ItemArray[1].ToString()} AND `Service_YEAR` = {DateTime.Now.Year}";

                var serviceData = await AsyncMySqlHelper.GetDataFromDatabase<Service>(query, "ConnString");

                CustomerInfoServicesGotten.Text = serviceData.Count.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Deletes the selected service from ServiceGrid.</summary>
        /// <param name="row">The row.</param>
        private async void DeleteSelectedService(DataRowView row)
        {
            try
            {
                string message = "Er du sikker du vil slette denne fejning?";
                string caption = "Advarsel";
                System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                System.Windows.MessageBoxResult result;

                // Displays the MessageBox.
                result = MessageBox.Show(message, caption, buttons);
                switch (result == System.Windows.MessageBoxResult.Yes)
                {
                    case true:
                        {
                            var query = $"DELETE FROM `girozilla`.`service-products` WHERE `Service-Product_SERVICEID` = {row.Row.ItemArray[0].ToString()}";

                            AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                            query = $"DELETE FROM `girozilla`.`services` WHERE `Service_ID` = {row.Row.ItemArray[0].ToString()}";

                            AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                            MessageBox.Show($"Fejning nr:{row.Row.ItemArray[0].ToString()} er nu slettet");
                            break;
                        }
                }
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                MessageBox.Show("En uventet fejl er sket", "FEJL");
                Log.Error(ex, "Unexpected Error");
            }
        }

        private void ThisYearOnlyCheck_Checked(object sender, RoutedEventArgs e)
        {
            SetData();
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the InvoicePrintButton control.
        /// </para>
        ///   <para>Gets service information</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void InvoicePrintButton_Click(object sender, RoutedEventArgs e)
        {
            switch (customerExists)
            {
                case true:
                    {
                        switch (ServiceGrid.SelectedIndex != -1)
                        {
                            case true:
                                {
                                    PrintServiceDialog.IsOpen = true;

                                    var table = VariableManipulation.DataGridtoDataTable(ProductGrid);

                                    SetupPrintServiceDialog(ServiceGrid.SelectedItem as DataRowView, table);

                                    switch (table != null)
                                    {
                                        case true:
                                            {
                                                table.Dispose();
                                                break;
                                            }
                                    }
                                    await Task.FromResult(true);
                                    break;
                                }
                            default:
                                {
                                    await Task.FromResult(false);
                                    MessageBox.Show("Venligst vælg en fejning");
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        MessageBox.Show("Kunden for denne fejning er blevet slettet");
                        break;
                    }
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the FinalAddAndPrintStandardService control.
        /// </para>
        ///   <para>Adds the service to the database and prints invoice</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void PrintStandardService_Click(object sender, RoutedEventArgs e)
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

                                        PrintServiceDialog.IsOpen = false;
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
        private async void PrintService_Click(object sender, RoutedEventArgs e)
        {
            try
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

                PrintServiceDialog.IsOpen = false;

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
        ///  Handles the Click event of the CancelAddService control.
        /// </para>
        ///   <para>Closes the PrintService dialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void CancelAddService_Click(object sender, RoutedEventArgs e)
        {
            PrintServiceDialog.IsOpen = false;
            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the InvoiceDeleteButton control.
        /// </para>
        ///   <para>Deletes a selected service from the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void InvoiceDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            switch (ServiceGrid.SelectedIndex != -1)
            {
                case true:
                    {
                        DeleteSelectedService(ServiceGrid.SelectedItem as DataRowView);

                        SetData();
                        await Task.FromResult(true);
                        break;
                    }
                default:
                    {
                        await Task.FromResult(false);
                        MessageBox.Show("Venligst vælg en fejning");
                        break;
                    }
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the SelectedCellsChanged event of the ServiceGrid control.
        /// </para>
        ///   <para>Updates ProductsGrid data</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectedCellsChangedEventArgs"/> instance containing the event data.</param>
        private async void ServiceGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                SetProductsInGrid(ServiceGrid.SelectedItem as DataRowView);
            }
            catch (NullReferenceException NREx)
            {
                Log.Warning(NREx, "No products for invoice");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }

            UpdateServiceData();
            InsertInvoiceCustomerInfo(ServiceGrid.SelectedItem as DataRowView);
            DisableCollumnsForDataGrid();

            await Task.FromResult(true);
        }

        #endregion

        #region Products

        /// <summary>Sets the products from the selected service to ProductsGrid.</summary>
        /// <param name="row">The Selected Row in the ServiceGrid.</param>
        private async void SetProductsInGrid(DataRowView row)
        {
            try
            {
                var query = $"SELECT * FROM `all_service-products` WHERE `Fejnings ID` = {row.Row.ItemArray[0].ToString()}";

                productData = AsyncMySqlHelper.GetSetFromDatabase(query, "ConnString").Result;

                ProductGrid.ItemsSource = productData.Tables[0].DefaultView;

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        #endregion

        #region Setup

        /// <summary>Adds the payment methods to payment combo.</summary>
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
                    PaySearch.Items.Add(obj);
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
        ///  Handles the SelectionChanged event of the PaySearch control.
        /// </para>
        ///   <para>Lets you search by payment method</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private async void PaySearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UnpayedOnlyCheck.IsChecked = false;
            SetData();

            await Task.FromResult(true);
        }


        /// <summary>
        ///   <para>
        ///  Handles the Checked event of the UnpayedCheckBox control.
        /// </para>
        ///   <para>Lets you search by unpayed services only</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void UnpayedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PaySearch.SelectedIndex = 0;
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
        ///  Handles the Click event of the MarkAsPayedButton control.
        /// </para>
        ///   <para>Marks the selected service data as payed.</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void MarkAsPayedButton_Click(object sender, RoutedEventArgs e)
        {
            MarkServiceDataAsPayed(ServiceGrid.SelectedItem as DataRowView);
            SetData();

            await Task.FromResult(true);
        }

        #endregion

        private void InvoiceSearchYearValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            SetData();
        }
    }
}
