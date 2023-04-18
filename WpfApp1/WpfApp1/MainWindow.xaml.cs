using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;



namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window

    {
        // An array to store the list of restaurants
        private string[] _restaurants;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Get the location of the user
            string location = await GetLocation();
            LocationTextBlock.Text = location;
            // Get the list of restaurants in the location
            _restaurants = await GetRestaurants(location);
            // Clear the previous list of restaurants in the ResultsListBox
            ResultsListBox.Items.Clear();
            // Add the new list of restaurants to the ResultsListBox
            foreach (string restaurant in _restaurants)
            {
                ResultsListBox.Items.Add(restaurant);
            }
        }
        // A method to get the location of the user
        private async Task<string> GetLocation()
        {
            using (var client = new HttpClient())

            {   // Get the response from the location API
                string response = await client.GetStringAsync("http://ip-api.com/json");

                // Parse the response as a JSON object
                JObject json = JObject.Parse(response);

                // Extract the city, region, and country from the JSON object
                string city = (string)json["city"];
                string region = (string)json["regionName"];
                string country = (string)json["country"];

                // Return the location in the format "city, region, country"
                return $"{city}, {region}, {country}";
            }
        }
        // The event handler for the "Pick for Me" button click
        private async void PickForMeButton_Click(object sender, RoutedEventArgs e)
        {

    if (_restaurants == null || _restaurants.Length == 0)
    {
        MessageBox.Show("Please search for restaurants first.");
        return;
    }
            // Generate a random index
            int index = new Random().Next(_restaurants.Length);

            // Select the restaurant in the ResultsListBox
            string restaurant = _restaurants[index];

            // Show a message box with the selected restaurant
            ResultsListBox.SelectedIndex = index;
            MessageBox.Show($"How about: {restaurant}");
}

        private async Task<string[]> GetRestaurants(string location)
        {
            using (var client2 = new HttpClient())
            {
                // Add authorization header to the client
                client2.DefaultRequestHeaders.Add("Authorization", "Bearer 0B0nln3vXhrnfy2wELDtoYSEc1q8bIbo_bKfrgeqcfgf88YMpcS2ge9T9oK0sPRoNH38tlX0AEcxnJzFLguGcqQSJWkUxsP179-KilrCatxgjKxSJXoHSa9XClczZHYx");

                // Get the response from the Yelp API
                string response = await client2.GetStringAsync($"https://api.yelp.com/v3/businesses/search?term=food&location={location}");

                // Parse the response into a JObject
                JObject json = JObject.Parse(response);

                // Get the "businesses" JArray from the response
                JArray businesses = (JArray)json["businesses"];

                // Create an array to store the names and addresses of the restaurants
                string[] restaurants = new string[businesses.Count];

                // Loop through each business in the "businesses" JArray
                for (int i = 0; i < businesses.Count; i++)
                {
                    // Get the current business as a JObject
                    JObject business = (JObject)businesses[i];

                    // Get the name of the business
                    string name = (string)business["name"];

                    // Get the address of the business
                    string address = string.Join(", ", business["location"]["address1"], business["location"]["city"], business["location"]["state"], business["location"]["zip_code"]);

                    // Add the name and address of the business to the array
                    restaurants[i] = $"{name} ({address})";
                }

                // Return the array of restaurants
                return restaurants;
            }
        }

    }
}

    
    

        

    

