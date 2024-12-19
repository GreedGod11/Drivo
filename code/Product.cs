using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace test_drivo
{
    public class Product
    {
        public string code { get; set; }
        public string product_name { get; set; }
        public string generic_name { get; set; }
        public string quantity { get; set; }
        public string brands { get; set; }
        public string categories { get; set; }
        public string ingredients_text { get; set; }
        public string allergens { get; set; }
        public string image_url { get; set; }
        public Nutriments nutriments { get; set; }
    }
    
    
}
