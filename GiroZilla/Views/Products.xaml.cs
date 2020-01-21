using GiroZilla;

using Serilog;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using PyroSquidUniLib.Database;
using PyroSquidUniLib.Documents;



namespace GiroZilla.Views
{
    public partial class Products
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<Products>();

        DataSet data = new DataSet();

        public Products()
        {
            InitializeComponent();
            SetData();
        }

        #region Product

        /// <summary>Sets data from the database to ProductGrid.</summary>
        private async void SetData()
        {
            try
            {
                var input = ClearTextSearch.Text;
                var query = "SELECT * FROM `all_products`";

                if (!string.IsNullOrWhiteSpace(input))
                {
                    query +=
                        "WHERE " +
                        "(" +
                        "ID  " +
                        "LIKE " +
                        $"'%{input}%'" +
                        $") " +

                        $"UNION " +

                        "SELECT * FROM `all_products` " +
                        $"WHERE " +
                        "(" +
                        "Navn " +
                        "LIKE " +
                        $"'%{input}%'" +
                        $") " +

                        $"UNION " +

                        "SELECT * FROM `all_products` " +
                        $"WHERE " +
                        "(" +
                        "Pris " +
                        "LIKE " +
                        $"'%{input}%'" +
                        $") " +

                        $"UNION " +

                        "SELECT * FROM `all_products` " +
                        $"WHERE " +
                        "(" +
                        "Beskrivelse " +
                        "LIKE " +
                        $"'%{input}%'" +
                        $") ";
                }

                data = await AsyncMySqlHelper.GetSetFromDatabase(query, "ConnString");

                ProductGrid.ItemsSource = data.Tables[0].DefaultView;
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }
        }

        /// <summary>Updates data to the database.</summary>
        private async void UpdateData()
        {
            try
            {
                AsyncMySqlHelper.UpdateSetToDatabase("SELECT * FROM `all_products`", data.Tables[0].DataSet, "ConnString");
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "An error occured while updating data!");
            }
        }

        /// <summary>
        ///   <para>Add new product data to the database</para>
        /// </summary>
        /// <returns>success</returns>
        private async Task<bool> AddNewProductData()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ProductNameTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(ProductPriceTextBox.Text))
                {
                    var query = $"INSERT INTO `products` " +
                        $"(" +
                        $"`Product_NAME`, " +
                        $"`Product_PRICE`, " +
                        $"`Product_DESCRIPTION` " +
                        $") " +
                        $"VALUES " +
                        $"(" +
                        $"'{ProductNameTextBox.Text}', " +
                        $"'{ProductPriceTextBox.Text.Replace(",", ".")}', " +
                        $"'{ProductDescriptionTextBox.Text}' " +
                        $");";

                    AsyncMySqlHelper.SetDataToDatabase(query, "ConnString").Wait();

                    await Task.FromResult(true);
                    return true;
                }
                else
                {
                    MessageBox.Show("Data Mangler");
                }
            }
            catch (Exception ex)
            {
                await Task.FromResult(false);
                Log.Error(ex, "Unexpected Error");
            }

            return false;
        }

        /// <summary>
        ///   <para>Deletes selected product from the database</para>
        /// </summary>
        /// <param name="row">The row.</param>
        private async void DeleteProduct(DataRowView row)
        {
            try
            {
                string message = "Er du sikker du vil slette dette produkt?";
                string caption = "Advarsel";
                System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.YesNo;
                System.Windows.MessageBoxResult result;

                // Displays the MessageBox.
                result = MessageBox.Show(message, caption, buttons);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var query = $"DELETE FROM `girozilla`.`products` WHERE `Product_ID` = {row.Row.ItemArray[0].ToString()}";

                    AsyncMySqlHelper.UpdateDataToDatabase(query, "ConnString").Wait();

                    MessageBox.Show($"Produktet nr:{row.Row.ItemArray[0].ToString()} er nu slettet");
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

        /// <summary>
        ///   <para>
        ///  Handles the CurrentCellChanged event of the ProductGrid control.
        /// </para>
        ///   <para>Updates product data to the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void ProductGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateData();
            ProductGrid.Columns[0].IsReadOnly = true;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the DeleteProductButton control.
        /// </para>
        ///   <para>Deletes selected product from the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductGrid.SelectedIndex != -1)
            {
                DeleteProduct(ProductGrid.SelectedItem as DataRowView);

                SetData();

                await Task.FromResult(true);
            }
            else
            {
                await Task.FromResult(false);
                MessageBox.Show("Venligst vælg et produkt");
            }
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the OpenAddProductDialogButton control.
        /// </para>
        ///   <para>Opens the AddProductDialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void OpenAddProductDialogButton_Click(object sender, RoutedEventArgs e)
        {
            AddProductDialog.IsOpen = true;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the FinalAddProduct control.
        /// </para>
        ///   <para>Add new product data to the database</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void FinalAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (AddNewProductData().Result)
            {
                AddProductDialog.IsOpen = false;

                SetData();

                await Task.FromResult(true);
            }
        }

        #endregion

        #region Other Event Handlers

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the ReloadButton control.
        /// </para>
        ///   <para>Reloads data to ProductGrid</para>
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
        ///  Handles the TextChanged event of the ClearTextSearch control.
        /// </para>
        ///   <para>Lets you search for data in clear text</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private async void ClearTextSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SetData();

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the Click event of the CancelAddProduct control.
        /// </para>
        ///   <para>Closes the AddProductDialog</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void CancelAddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProductDialog.IsOpen = false;

            await Task.FromResult(true);
        }

        /// <summary>
        ///   <para>
        ///  Handles the LostFocus event of the ProductPriceTextBox control.
        /// </para>
        ///   <para>Shows the price for the current service</para>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void ProductPriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            PrintHelper.FixPriceText(ProductPriceTextBox);

            await Task.FromResult(true);
        }

        #endregion
    }
}
