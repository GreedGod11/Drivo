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
using Newtonsoft.Json;

namespace test_drivo
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class OpenFoodFacts : Window
    {
        public OpenFoodFacts()
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
                        
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return null;
                }
            }
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string productName = BarcodeTextBox.Text;
            List<Product> products = await SearchProductsByNameAsync(productName);

            if (products != null && products.Any())
            {
                ProductComboBox.ItemsSource = products;
                ProductComboBox.SelectedIndex = 0; 
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
   
                ProductNameTextBlock.Text = $"Name : {selectedProduct.product_name}";
                ProductDetailsTextBlock.Text = $"Brand: {selectedProduct.brands}\n\nCategories: {selectedProduct.categories}";
                ProductIngredientTextBlock.Text =$"Ingredient list : { selectedProduct.ingredients_text}";

          
                if (!string.IsNullOrEmpty(selectedProduct.image_url))
                {
                    ProductImage.Source = new BitmapImage(new Uri(selectedProduct.image_url));
                }
                else
                {
                    ProductImage.Source = null; 
                }
            }
        }

        private async void FetchProductButton_Click(object sender, RoutedEventArgs e)
        {
         
            string barcode = BarcodeTextBox.Text; 
            Product product = await FetchProductDataAsync(barcode);

            if (product != null)
            {
        
                ProductNameTextBlock.Text = $"Product Name: {product.product_name}";
                ProductDetailsTextBlock.Text = $"Brand: {product.brands}\nCategory: {product.categories}";

                if (!string.IsNullOrEmpty(product.image_url))
                {
                    ProductImage.Source = new BitmapImage(new Uri(product.image_url));
                }
                else
                {
                    ProductImage.Source = null; 
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
                    return searchResults.products; 
                }
                else
                {
                    return null;
                }
            }
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                    SearchButton_Click(sender, e);               
            }   
        }
    }



    public class SearchResponse
    {
        public List<Product> products { get; set; }
    }


    public class ApiResponse
    {
        public Product product { get; set; }
    }
}
