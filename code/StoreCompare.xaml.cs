using Npgsql;
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
using System.Windows.Shapes;

namespace test_drivo
{
    /// <summary>
    /// Logique d'interaction pour Window2.xaml
    /// </summary>
    public partial class StoreCompare : Window
    {
        NpgsqlConnection ConnCart = null;
        public StoreCompare()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            String connString2 = "Server = localhost; Port = 5432; Database = client ; User Id = user; Password = 0000; ";
            ConnCart = new NpgsqlConnection(connString2);
            ConnCart.Open();

            ComparePrices();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void ComparePrices()
        {
            string compareSql = @"
    SELECT 
        stores.id AS store_id, 
        stores.name AS store_name, 
        SUM(products.price * shoppingcart.quantity) AS total_cart_price
    FROM dblink('dbname=Supermarche user=user password=0000',
                'SELECT product_id, store_id, name, price FROM products')
        AS products(product_id INT, store_id INT, name TEXT, price DECIMAL)
    JOIN dblink('dbname=Supermarche user=user password=0000',
                'SELECT id, name FROM stores')
        AS stores(id INT, name TEXT)
    ON products.store_id = stores.id
    JOIN shoppingcart
    ON shoppingcart.product_name = products.name -- Match by product name, NOT product_id
    GROUP BY stores.id, stores.name
    ORDER BY total_cart_price ASC;
    ";

            decimal store1Total = 0, store2Total = 0, store3Total = 0, store4Total = 0;
            string cheapestStore = "";
            decimal cheapestTotal = decimal.MaxValue;

            using (var cmd = new NpgsqlCommand(compareSql, ConnCart))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows) // 🛑 Check if query returned data
                    {
                        MessageBox.Show("No data returned from query.");
                        return;
                    }

                    while (reader.Read())
                    {
                        int storeId = Convert.ToInt32(reader["store_id"]);
                        string storeName = reader["store_name"].ToString();
                        decimal totalCartPrice = Convert.ToDecimal(reader["total_cart_price"]);

                        Application.Current.Dispatcher.Invoke(() => // ✅ Ensures UI thread update
                        {
                            switch (storeId)
                            {
                                case 1:
                                    store1Total = totalCartPrice;
                                    AuchanLabel.Content = $"The total in {storeName} is: {store1Total:C}";
                                    AuchanLabel.Visibility = Visibility.Visible;
                                    break;
                                case 2:
                                    store2Total = totalCartPrice;
                                    CarrefourLabel.Content = $"The total in {storeName} is: {store2Total:C}";
                                    CarrefourLabel.Visibility = Visibility.Visible;
                                    break;
                                case 3:
                                    store3Total = totalCartPrice;
                                    LeclercLabel.Content = $"The total in {storeName} is: {store3Total:C}";
                                    LeclercLabel.Visibility = Visibility.Visible;
                                    break;
                                case 4:
                                    store4Total = totalCartPrice;
                                    InterLabel.Content = $"The total in {storeName} is: {store4Total:C}";
                                    InterLabel.Visibility = Visibility.Visible;
                                    break;
                            }
                        });

                        if (totalCartPrice < cheapestTotal)
                        {
                            cheapestTotal = totalCartPrice;
                            cheapestStore = storeName;
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() => // ✅ Ensures UI update
            {
                CheapestLabel.Content = $"The cheapest store is: {cheapestStore} with a total of {cheapestTotal:C}";
                CheapestLabel.Visibility = Visibility.Visible;
            });

            if (cheapestStore == "")
            {
                MessageBox.Show("No price comparison available.");
            }
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }


    public class CartItem
    {
        public int StoreId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

}