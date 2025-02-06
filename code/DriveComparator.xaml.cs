using Npgsql;
using OpenFoodFacts4Net.Json.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace test_drivo
{
    /// <summary>
    /// Logique d'interaction pour Window2.xaml
    /// </summary>
    public partial class DriveComparator : Window
    {
        private CollectionViewSource catalogViewSource;
        NpgsqlConnection Myconnection = null;
        NpgsqlConnection ConnCart = null;
        private bool ignoreSelectionChange = false;
       // private int previousStoreIndex = -1;
        private object previousSelectedStore = null;
        public DriveComparator()
        {
            InitializeComponent();
        }
        public List<Store> Stores { get; set; }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            String connString1 = "Server = localhost; Port = 5432; Database = Supermarche; User Id = user; Password = 0000; ";
            Myconnection = new NpgsqlConnection(connString1);
            Myconnection.Open();
            // Initialization logic here

            String connString2 = "Server = localhost; Port = 5432; Database = client ; User Id = user; Password = 0000; ";
            ConnCart = new NpgsqlConnection(connString2);
            ConnCart.Open();


            LoadStores();
            CategoryComboBox.Items.Refresh();

        }




        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChange) return;

            var selectedStore = storeComboBox.SelectedItem as Store;

            // Prevent action if the store is already selected
            if (selectedStore == null || selectedStore == previousSelectedStore)
                return;

            // Ask the user for confirmation before changing the store
            MessageBoxResult response = MessageBox.Show(
                "Changing store will reset your current cart. Do you want to continue?",
                "Confirm Store Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (response == MessageBoxResult.Yes)
            {
                ignoreSelectionChange = true; // Prevent recursive calls
                EmptyDatabase(); // Clear the shopping cart
                LoadCatalog(selectedStore.StoreID); // Load the new store's catalog
                previousSelectedStore = selectedStore; // Update previous store
                ignoreSelectionChange = false; // Re-enable selection change event
            }
            else
            {
                // Revert selection back to the previous store
                ignoreSelectionChange = true;
                storeComboBox.SelectedItem = previousSelectedStore;
                ignoreSelectionChange = false;
            }
        }

    

    private void LoadCatalog(int storeID)
        {
            try
            {
                //  ListTest.ItemsSource = null;
                // Define the SQL command to select products from the inventory based on the store
                string selectSql = "SELECT product_id, store_id, name, category, price FROM products WHERE store_id = @storeID ";

                var productList = new List<InventoryItem>();
                using (var cmd = new NpgsqlCommand(selectSql, Myconnection))
                {
                    cmd.Parameters.AddWithValue("@storeID", storeID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            productList.Add(new InventoryItem
                            {
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                StoreId = Convert.ToInt32(reader["store_id"]),

                                Product = reader["name"].ToString(),
                                Category = reader["category"].ToString(),
                                Price = Convert.ToDecimal(reader["price"])
                            });
                        }
                    }
                }

                // Bind the catalog data to the DataGrid
                catalogViewSource = new CollectionViewSource { Source = productList };
                catalogViewSource.Filter += CatalogFilter;
                catalogDataGrid.ItemsSource = catalogViewSource.View;

                //  ListTest.ItemsSource = productList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading catalog: {ex.Message}");
            }
        }



        private void LoadStores()
        {
            try
            {
                storeComboBox.ItemsSource = null;

                // Define the SQL command to select data from your table
                string selectSql = "SELECT name, address, id FROM stores";

                // Create a list to hold the data
                var StoreList = new List<Store>();

                // Execute the query and populate the list
                using (var cmd = new NpgsqlCommand(selectSql, Myconnection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        StoreList.Add(new Store
                        {
                            StoreName = reader["name"].ToString(),
                            StoreLocation = reader["address"].ToString(),
                            StoreID = (int)reader["id"]
                        });
                    }

                }

                // Bind the list to the ComboBox
                storeComboBox.ItemsSource = StoreList;




                //    storeComboBox.DisplayMemberPath = "StoreName"; // Show StoreName in ComboBox
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }


            try
            {
                string selectSql = "SELECT DISTINCT category FROM products"; // DISTINCT avoids duplicates
                var CategoryList = new List<string>(); // Use a List<string> instead of InventoryItem

                using (var cmd = new NpgsqlCommand(selectSql, Myconnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CategoryList.Add(reader["category"].ToString()); // Add category as a string
                        }
                    }
                }
                //  ignoreSelectionChange = true;

                // Bind the ComboBox correctly
                CategoryComboBox.ItemsSource = CategoryList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading catalog: {ex.Message}");
            }

            CategoryComboBox.UpdateLayout();
            CategoryComboBox.Items.Refresh();
        }


        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            catalogViewSource?.View.Refresh();
        }

        private void CatalogFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is Product product)
            {
                string searchText = searchTextBox.Text.ToLower();
                e.Accepted = product.product_name.ToLower().Contains(searchText) ||
                             product.categories.ToLower().Contains(searchText);
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string selectedCategory = (string)CategoryComboBox.SelectedItem;
            if (!String.IsNullOrEmpty(selectedCategory))
            {
                try
                {
                    //  ListTest.ItemsSource = null;
                    // Define the SQL command to select products from the inventory based on the store
                    string selectSql = "SELECT store_id, name, category, price FROM products WHERE category = @category";

                    var productList = new List<InventoryItem>();

                    using (var cmd = new NpgsqlCommand(selectSql, Myconnection))
                    {
                        cmd.Parameters.AddWithValue("@category", selectedCategory);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productList.Add(new InventoryItem
                                {
                                    Product = reader["name"].ToString(),
                                    Price = Convert.ToDecimal(reader["price"]),
                                    Category = reader["category"].ToString()
                                });
                            }
                        }
                    }

                    // Bind the catalog data to the DataGrid
                    catalogViewSource = new CollectionViewSource { Source = productList };
                    catalogViewSource.Filter += CatalogFilter;
                    catalogDataGrid.ItemsSource = catalogViewSource.View;

                    ignoreSelectionChange = true;  // Prevent event firing while we clear the selection
                    catalogDataGrid.SelectedItem = null;

                    //  ListTest.ItemsSource = productList;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading catalog: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Error loading catalog");
            }
        }


        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string selectedProduct = searchTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(selectedProduct))
            {
                try
                {
                    // Define the SQL command to select products, ignoring case using ILIKE (PostgreSQL)
                    string selectSql = "SELECT product_id store_id, name, category, price FROM products WHERE name ILIKE @name";

                    var productList = new List<InventoryItem>();

                    using (var cmd = new NpgsqlCommand(selectSql, Myconnection))
                    {
                        cmd.Parameters.AddWithValue("@name", "%" + selectedProduct + "%"); // Allows partial matches

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productList.Add(new InventoryItem
                                {


                                    StoreId = Convert.ToInt32(reader["store_id"]),
                                    //       ProductId = Convert.ToInt32(reader["product_id"]),
                                    Product = reader["name"].ToString(),
                                    Category = reader["category"].ToString(),
                                    Price = Convert.ToDecimal(reader["price"])
                                });
                            }
                        }
                    }

                    if (productList.Count > 0)
                    {
                        // Bind the data to the DataGrid
                        catalogViewSource = new CollectionViewSource { Source = productList };
                        catalogViewSource.Filter += CatalogFilter;
                        catalogDataGrid.ItemsSource = catalogViewSource.View;
                    }
                    else
                    {
                        // Show message if no product is found
                        MessageBox.Show("Product not found.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
                        catalogDataGrid.ItemsSource = null; // Clear DataGrid
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading catalog: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


            }
            else
            {
                MessageBox.Show("Please enter a product name.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void catalogDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChange) return;
            if (catalogDataGrid.SelectedItem is InventoryItem selectedProduct)
            {
                MessageBox.Show("selected product : " + selectedProduct.Product);
                try
                {
                    // ✅ Insert or update product in the cart
                    string insertOrUpdateSql = @"
                INSERT INTO shoppingcart (product_id, product_name, store_id, price, quantity, category)
                VALUES (@product_id, @product_name, @store_id, @price, 1, @category)
                ON CONFLICT (product_id, store_id) 
                DO UPDATE SET quantity = shoppingcart.quantity + 1;";

                    using (var cmd = new NpgsqlCommand(insertOrUpdateSql, ConnCart))
                    {
                        cmd.Parameters.AddWithValue("@product_id", selectedProduct.ProductId);
                        cmd.Parameters.AddWithValue("@product_name", selectedProduct.Product);
                        cmd.Parameters.AddWithValue("@store_id", selectedProduct.StoreId);
                        cmd.Parameters.AddWithValue("@price", Convert.ToDecimal(selectedProduct.Price));
                        cmd.Parameters.AddWithValue("@category", selectedProduct.Category);
                        cmd.ExecuteNonQuery();
                    }

                    // ✅ Refresh DataGrid to show the updated quantity
                    RefreshCartDataGrid();

                    // ✅ Update total price after modification
                    UpdateTotalPrice();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding to cart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ignoreSelectionChange = true;  // Prevent event firing while we clear the selection
                    catalogDataGrid.SelectedItem = null;
                    ignoreSelectionChange = false; // Re-enable event firing
                }
            }
        }



        private void RefreshCartDataGrid()
        {
            try
            {
                string selectSql = "SELECT product_id, product_name, category, quantity FROM shoppingcart";

                var itemList = new List<InventoryItem>();

                using (var cmd = new NpgsqlCommand(selectSql, ConnCart))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            itemList.Add(new InventoryItem
                            {
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                Product = reader["product_name"].ToString(),
                                Category = reader["category"].ToString(),
                                Quantity = Convert.ToInt32(reader["quantity"])
                            });
                        }
                    }
                }

                cartDataGrid.ItemsSource = null;
                cartDataGrid.ItemsSource = itemList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing cart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void UpdateTotalPrice()
        {
            string totalSql = "SELECT SUM(price * quantity) FROM shoppingcart";

            using (var cmd = new NpgsqlCommand(totalSql, ConnCart))
            {
                var result = cmd.ExecuteScalar();
                decimal totalPrice = result != DBNull.Value ? Convert.ToDecimal(result) : 0;

                totalPriceLabel.Content = $"Total: {totalPrice:C}"; // Display total price in UI
            }
        }


       


        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            StoreCompare window = new StoreCompare();
            window.Show();
        }

        private void cartDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void EmptyButton_Click(object sender, RoutedEventArgs e)
        {
            EmptyDatabase();
        }

        private void EmptyDatabase()
        {
            try
            {
                //  SQL command to delete all rows from the shopping cart table
                string deleteSql = "DELETE FROM shoppingcart";

                using (var cmd = new NpgsqlCommand(deleteSql, ConnCart))
                {
                    cmd.ExecuteNonQuery();
                }

                //  Refresh the DataGrid after clearing the cart
                RefreshCartDataGrid();

                //  Update total price after clearing
                UpdateTotalPrice();

                MessageBox.Show("Shopping cart has been emptied.", "Cart Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing cart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }
    }


}

public class InventoryItem
{
    public int ProductId { get; set; }
    public string Product { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public int Quantity { get; set; }
    // Hidden properties
    public int StoreId { get; set; }

}
public class Store
{
    public string StoreName { get; set; }
    public string StoreLocation { get; set; }
    public int StoreID { get; set; }

    // Optional: Override ToString for debugging or ComboBox display
    public override string ToString()
    {
        return $"{StoreName} - {StoreLocation}";
    }
}


