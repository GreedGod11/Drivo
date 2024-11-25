using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace test_drivo
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public async Task<Product> FetchProductDataAsync(string barcode)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);

                    return apiResponse?.product;
                }
                else
                {
                    // Handle error response
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return null;
                }
            }
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string productName = BarcodeTextBox.Text; // You can use any input field, e.g., a TextBox
            List<Product> products = await SearchProductsByNameAsync(productName);

            if (products != null && products.Any())
            {
                ProductComboBox.ItemsSource = products; // Bind the list to the ComboBox
                ProductComboBox.SelectedIndex = 0; // Select the first item by default
            }
            else
            {
                MessageBox.Show("No products found.");
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Product selectedProduct = (Product)ProductComboBox.SelectedItem;

            if (selectedProduct != null)
            {
                // Display product details
                ProductNameTextBlock.Text = $"Name : {selectedProduct.product_name}";
                ProductDetailsTextBlock.Text = $"Brand: {selectedProduct.brands}\n\nCategories: {selectedProduct.categories}";
                ProductIngredientTextBlock.Text =$"Ingredient list : { selectedProduct.ingredients_text}";

                // Display product image
                if (!string.IsNullOrEmpty(selectedProduct.image_url))
                {
                    ProductImage.Source = new BitmapImage(new Uri(selectedProduct.image_url));
                }
                else
                {
                    ProductImage.Source = null; // Or set a placeholder image
                }
            }
        }

        private async void FetchProductButton_Click(object sender, RoutedEventArgs e)
        {
            // Your asynchronous code here, e.g., calling an API
            string barcode = BarcodeTextBox.Text; // Example barcode or get this from a TextBox
            Product product = await FetchProductDataAsync(barcode);

            if (product != null)
            {
                // Update UI with product details
                ProductNameTextBlock.Text = $"Product Name: {product.product_name}";
                ProductDetailsTextBlock.Text = $"Brand: {product.brands}\nCategory: {product.categories}";

                // Display product image
                if (!string.IsNullOrEmpty(product.image_url))
                {
                    ProductImage.Source = new BitmapImage(new Uri(product.image_url));
                }
                else
                {
                    ProductImage.Source = null; // Clear the image if no URL is available
                }

            }
            else
            {
                ProductNameTextBlock.Text = "Product not found.";
                ProductDetailsTextBlock.Text = string.Empty;
                ProductImage.Source = null;
            }
        }
        private async Task<List<Product>> SearchProductsByNameAsync(string productName)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={productName}&action=process&json=true";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var searchResults = JsonConvert.DeserializeObject<SearchResponse>(json);
                    return searchResults.products; // Return the list of products
                }
                else
                {
                    return null;
                }
            }
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Check if the Enter key was pressed
            {
         
             
                    SearchButton_Click(sender, e); // Call your search function
                
            }
            
        }


        // Root object for the API response


    }



    public class SearchResponse
    {
        public List<Product> products { get; set; }
    }

    // Root object for the API response
    public class ApiResponse
    {
        public Product product { get; set; }
    }
}
