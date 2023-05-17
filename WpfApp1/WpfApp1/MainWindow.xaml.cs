using System;
using System.Diagnostics;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Data.SQLite;



namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window

    {
        // An array to store the list of restaurants
        private string[] _restaurants;

        // db connection string
        string connectionString = "Data Source=C:\\Users\\Emmet\\Desktop\\capstone_restaurantpicker\\WpfApp1\\WpfApp1\\RestaurantPicker.db;Version=3";  


        public MainWindow()
        {
            InitializeComponent();

            // clear suggested restaruants table on initial load
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                SQLiteCommand dropCommand = new SQLiteCommand("DROP TABLE IF EXISTS PreviousSuggestions", connection);

                dropCommand.ExecuteNonQuery();

                connection.Close();
            }
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

            // Show a message box with the number of results
            MessageBox.Show($"These are the {_restaurants.Length} top rated restaurant results for your location.");
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
            if (_restaurants != null && _restaurants.Length != 0)
            {
                // Generate a random index
                int index = new Random().Next(_restaurants.Length);

                // Select the restaurant in the ResultsListBox
                string restaurant = _restaurants[index];

                // Remove the URL from the restaurant string
                string restaurantWithoutUrl = RemoveUrlFromRestaurant(restaurant);

                // Show a message box with the selected restaurant
                MessageBox.Show($"{restaurantWithoutUrl}\nGo check out this restaurant or click OK on this dialogue box and then click Random Restaurant for a new suggestion.");

                // Set the SelectedIndex of the ListBox to highlight the item
                ResultsListBox.SelectedIndex = index;

                // Move the cursor focus to the ListBox
                Keyboard.Focus(ResultsListBox);

                // Scroll the selected item into view
                ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);

                // store suggested restaurant in db
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // create table if it doesn't already exist
                    SQLiteCommand createCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS \"PreviousSuggestions\" (\"RestaurantString\" BLOB)", connection);
                    createCommand.ExecuteNonQuery();

                    // insert suggested restaurant into db
                    string query = "INSERT INTO PreviousSuggestions (RestaurantString) VALUES (@Value)";
                    SQLiteCommand insertCommand = new SQLiteCommand(query, connection);
                    insertCommand.Parameters.AddWithValue("@Value",restaurantWithoutUrl);
                    insertCommand.ExecuteNonQuery();

                    connection.Close();
                }

                // refresh listbox to show previous suggested restaurants
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // query db for results
                    SQLiteCommand selectCommand = new SQLiteCommand("SELECT * FROM PreviousSuggestions", connection);
                    SQLiteDataReader reader = selectCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        string restaurantString = reader.GetString(0);

                        PreviousSuggestionsListbox.Items.Add(restaurantString);
                    }
                    reader.Close();

                    connection.Close();
                }

                // Clear the previous list of restaurants in the ResultsListBox
                ResultsListBox.Items.Clear();

                // Add the new list of restaurants to the ResultsListBox
                foreach (string restaurants in _restaurants)
                {
                    ResultsListBox.Items.Add(restaurants);
                }
            }
            else
            {
                MessageBox.Show("Please perform a search for restaurants first.");
                return;
            }
        }

        private string RemoveUrlFromRestaurant(string restaurant)
        {
            // Find the index of the URL within the restaurant information
            int urlIndex = restaurant.IndexOf("https://www.yelp.com/");

            if (urlIndex != -1)
            {
                // Remove the URL from the restaurant string
                string restaurantWithoutUrl = restaurant.Substring(0, urlIndex);
                return restaurantWithoutUrl;
            }

            return restaurant;
        }

        private async Task<string[]> GetRestaurants(string location)
        {
            using (var client2 = new HttpClient())
            {
                // Add authorization header to the client
                client2.DefaultRequestHeaders.Add("Authorization", "Bearer 0B0nln3vXhrnfy2wELDtoYSEc1q8bIbo_bKfrgeqcfgf88YMpcS2ge9T9oK0sPRoNH38tlX0AEcxnJzFLguGcqQSJWkUxsP179-KilrCatxgjKxSJXoHSa9XClczZHYx");

                // Get the selected value of the NumRestaurantsComboBox as a ComboBoxItem
                ComboBoxItem selectedItem = NumRestaurantsComboBox.SelectedItem as ComboBoxItem;

                // Extract the integer value from the Content property of the selected ComboBoxItem
                int numRestaurants = Int32.Parse(selectedItem.Content.ToString());

                // Use the selected value in the API request
                string response = await client2.GetStringAsync($"https://api.yelp.com/v3/businesses/search?term=restaurants&location={location}&limit={numRestaurants}&sort_by=rating");

                // Parse the response into a JObject
                JObject json = JObject.Parse(response);

                // Get the "businesses" JArray from the response
                JArray businesses = (JArray)json["businesses"];

                // Create an array to store the names, addresses, ratings, and review counts of the restaurants
                string[] restaurants = new string[businesses.Count];

                // Loop through each business in the "businesses" JArray
                for (int i = 0; i < businesses.Count; i++)
                {
                    // Get the current business as a JObject
                    JObject business = (JObject)businesses[i];

                    // Get the name of the business
                    string name = (string)business["name"];

                    // Get the address of the business
                    string address = string.Join(", ", business["location"]["address1"] + " " + business["location"]["city"], business["location"]["state"] + " " + business["location"]["zip_code"]);

                    // Get the phone number of the business
                    string phone = (string)business["phone"];

                    // Remove any non-numeric characters from the phone number
                    string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

                    // Format the phone number with hyphens
                    string formattedPhone = $"{digitsOnly.Substring(0, 1)}-{digitsOnly.Substring(1, 3)}-{digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7)}";

                    // Get the price of the business (or set it to "unavailable" if the field doesn't exist
                    string? price = business.ContainsKey("price") ? (string)business["price"] : "unavailable";

                    // Get the rating of the business
                    double rating = (double)business["rating"];

                    //Get the url of the business
                    string url = (string)business["url"];

                    // Get the number of reviews of the business
                    int reviewCount = (int)business["review_count"];

                    // Add the name, address, phone number, price, ratings, review counts, and Yelp URL of the restaurant to the array
                    restaurants[i] = $"Restaurant Name: {name} {(price == "unavailable" ? "\nPrice: Unavailable" : "\nPrice: " + price)}" + $"\nAddress: " +
                        $"{address}\nPhone Number: {formattedPhone}\nRating: ({rating} stars)\nNumber of Reviews: ({reviewCount})\n{url}\n";
                }

                // Return the array of restaurants
                return restaurants;
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Welcome to our restaurant recommendation app! \n\nTo get started, choose the number of restaurants from the drop-down. Click the 'Search Restaurants' button to initiate a location search.\n\nWe'll show you a list of the top-rated restaurants, based on the number of restaurants you chose, that are in your area. The results display based on an adjusted rating value that takes into account the number of ratings.\n\nChoose the number of restaurants and click the 'Hot and New' button to see the newest restaurants added to Yelp in your area.\n\nChoose the number of restaurants and click the 'Open Now' button to see restaurants that are open, if you happen to be looking for a late night snack!\n\nClick on any restaurant from the list to open the Yelp page.\n\nIf you're feeling indecisive on where to eat, click the 'Random Restaurant' button and we'll randomly select a restaurant from the list for you, and open the Yelp page of that restaurant!\n\nEnjoy your meal!");
        }

        private async void HotAndNewButton_Click(object sender, RoutedEventArgs e)
        {
            // Get location of user
            string location = await GetLocation();
            LocationTextBlock.Text = location;

            // Get list of hot_and_new restaurants
            string[] hotAndNewRestaurants = await getHotAndNewRestaurants(location);

            // Update the _restaurants array with hot and new restaurants
            _restaurants = hotAndNewRestaurants;

            // Clear existing list of restaurants that are shown in the ResultsListBox
            ResultsListBox.Items.Clear();

            // Add new list of restaurants to ResultsListBox
            foreach (string restaurants in hotAndNewRestaurants)
            {
                ResultsListBox.Items.Add(restaurants);
            }

            // Show a message box with the total number of results
            MessageBox.Show($"These are the {hotAndNewRestaurants.Length} newest restaurants that have been added to Yelp in your location.");
        }

        private async void dealsButton_click(object sender, RoutedEventArgs e)
        {
            // Get location of user
            string location = await GetLocation();
            LocationTextBlock.Text = location;

            // Get list of restaurants with deals
            string[] restaurants = await getDeals(location);

            // Update the _restaurants array 
            _restaurants = restaurants;

            // Clear existing list of restaurants that are shown in the ResultsListBox
            ResultsListBox.Items.Clear();

            // Add new list of restaurants to ResultsListBox
            foreach (string restaurant in restaurants)
            {
                ResultsListBox.Items.Add(restaurant);
            }

            // Show a message box with the total number of results
            MessageBox.Show($"These are the {restaurants.Length} restaurants that are currently offering deals.");
        }

        private async Task<string[]> getDeals(string location)
        {
            using (var client3 = new HttpClient())
            {
                // Add authorization header to the client
                client3.DefaultRequestHeaders.Add("Authorization", "Bearer 0B0nln3vXhrnfy2wELDtoYSEc1q8bIbo_bKfrgeqcfgf88YMpcS2ge9T9oK0sPRoNH38tlX0AEcxnJzFLguGcqQSJWkUxsP179-KilrCatxgjKxSJXoHSa9XClczZHYx");

                // Get the selected value of the NumRestaurantsComboBox as a ComboBoxItem
                ComboBoxItem selectedItem = NumRestaurantsComboBox.SelectedItem as ComboBoxItem;

                // Extract the integer value from the Content property of the selected ComboBoxItem
                int numRestaurants = Int32.Parse(selectedItem.Content.ToString());

                // Use open to filter search results to only get resteraunts with deals
                string response = await client3.GetStringAsync($"https://api.yelp.com/v3/businesses/search?term=restaurants&location={location}&limit={numRestaurants}&deals=true");

                // Put response into a JObject
                JObject json = JObject.Parse(response);

                // Get "businesses" JArray from the response
                JArray businesses = (JArray)json["businesses"];

                // If no results, return empty array (or something else if we want that)
                if (businesses.Count == 0)
                {
                    return new string[0];
                }

                // Create an array to store names/addresses/ratings/review counts of restaurants
                string[] deals = new string[businesses.Count];

                // Loop through each business in "businesses" JArray
                for (int i = 0; i < businesses.Count; i++)
                {

                    // Get the current business as a JObject
                    JObject business = (JObject)businesses[i];

                    // Get the name of the business
                    string name = (string)business["name"];

                    // Get the address of the business
                    string address = string.Join(", ", business["location"]["address1"] + " " + business["location"]["city"],
                        business["location"]["state"] + " " + business["location"]["zip_code"]);

                    // Get the rating of the business
                    double rating = (double)business["rating"];

                    // Get review count of the business
                    int reviewCount = (int)business["review_count"];

                    //Get the url of the business
                    string url = (string)business["url"];

                    // Add restaurant to array of restaurants
                    deals[i] = $"{name}\n{address}\nRating: {rating} stars\n({reviewCount} reviews)\n{url}\n";
                }
                return deals;
            }
        }

        private async void onlyOpenButton_click(object sender, RoutedEventArgs e)
        {
            // Get location of user
            string location = await GetLocation();
            LocationTextBlock.Text = location;

            // Get list of open restaurants
            string[] openRestaurants = await getOpenRestaurants(location);

            // Update the _restaurants array
            _restaurants = openRestaurants;

            // Clear existing list of restaurants that are shown in the ResultsListBox
            ResultsListBox.Items.Clear();

            // Add new list of restaurants to ResultsListBox
            foreach (string restaurants in openRestaurants)
            {
                ResultsListBox.Items.Add(restaurants);
            }

            // Show a message box with the total number of results
            MessageBox.Show($"These are the {openRestaurants.Length} restaurants that are currently open in your location.");
        }

        private async Task<string[]> getHotAndNewRestaurants(string location)
        {
            using (var client3 = new HttpClient())
            {
                // Add authorization header to the client
                client3.DefaultRequestHeaders.Add("Authorization", "Bearer 0B0nln3vXhrnfy2wELDtoYSEc1q8bIbo_bKfrgeqcfgf88YMpcS2ge9T9oK0sPRoNH38tlX0AEcxnJzFLguGcqQSJWkUxsP179-KilrCatxgjKxSJXoHSa9XClczZHYx");

                // Get the selected value of the NumRestaurantsComboBox as a ComboBoxItem
                ComboBoxItem selectedItem = NumRestaurantsComboBox.SelectedItem as ComboBoxItem;

                // Extract the integer value from the Content property of the selected ComboBoxItem
                int numRestaurants = Int32.Parse(selectedItem.Content.ToString());

                // Use hot_and_new to filter search results to only get hot_and_new restaurants
                string response = await client3.GetStringAsync($"https://api.yelp.com/v3/businesses/search?term=restaurants&location={location}&limit={numRestaurants}&hot_and_new=true");

                // Put response into a JObject
                JObject json = JObject.Parse(response);

                // Get "businesses" JArray from the response
                JArray businesses = (JArray)json["businesses"];

                // If no results, return empty array (or something else if we want that)
                if (businesses.Count == 0)
                {
                    return new string[0];
                }

                // Create an array to store names/addresses/ratings/review counts of restaurants
                string[] hotAndNewRestaurants = new string[businesses.Count];

                // Loop through each business in "businesses" JArray
                for (int i = 0; i < businesses.Count; i++)
                {

                    // Get the current business as a JObject
                    JObject business = (JObject)businesses[i];

                    // Get the name of the business
                    string name = (string)business["name"];

                    // Get the address of the business
                    string address = string.Join(", ", business["location"]["address1"] + " " + business["location"]["city"],
                        business["location"]["state"] + " " + business["location"]["zip_code"]);

                    // Get the rating of the business
                    double rating = (double)business["rating"];

                    // Get review count of the business
                    int reviewCount = (int)business["review_count"];

                    //Get the url of the business
                    string url = (string)business["url"];

                    // Add restaurant to array of restaurants
                    hotAndNewRestaurants[i] = $"{name}\n{address}\nRating: {rating} stars\n({reviewCount} reviews)\n{url}\n";
                }
                return hotAndNewRestaurants;
            }
        }

        private async Task<string[]> getOpenRestaurants(string location)
        {
            using (var client3 = new HttpClient())
            {
                // Add authorization header to the client
                client3.DefaultRequestHeaders.Add("Authorization", "Bearer 0B0nln3vXhrnfy2wELDtoYSEc1q8bIbo_bKfrgeqcfgf88YMpcS2ge9T9oK0sPRoNH38tlX0AEcxnJzFLguGcqQSJWkUxsP179-KilrCatxgjKxSJXoHSa9XClczZHYx");

                // Get the selected value of the NumRestaurantsComboBox as a ComboBoxItem
                ComboBoxItem selectedItem = NumRestaurantsComboBox.SelectedItem as ComboBoxItem;

                // Extract the integer value from the Content property of the selected ComboBoxItem
                int numRestaurants = Int32.Parse(selectedItem.Content.ToString());

                // Use open to filter search results to only get open restaurants
                string response = await client3.GetStringAsync($"https://api.yelp.com/v3/businesses/search?term=restaurants&location={location}&limit={numRestaurants}&open_now=true");

                // Put response into a JObject
                JObject json = JObject.Parse(response);

                // Get "businesses" JArray from the response
                JArray businesses = (JArray)json["businesses"];

                // If no results, return empty array (or something else if we want that)
                if (businesses.Count == 0)
                {
                    return new string[0];
                }

                // Create an array to store names/addresses/ratings/review counts of restaurants
                string[] hotAndNewRestaurants = new string[businesses.Count];

                // Loop through each business in "businesses" JArray
                for (int i = 0; i < businesses.Count; i++)
                {

                    // Get the current business as a JObject
                    JObject business = (JObject)businesses[i];

                    // Get the name of the business
                    string name = (string)business["name"];

                    // Get the address of the business
                    string address = string.Join(", ", business["location"]["address1"] + " " + business["location"]["city"],
                        business["location"]["state"] + " " + business["location"]["zip_code"]);

                    // Get the rating of the business
                    double rating = (double)business["rating"];

                    // Get review count of the business
                    int reviewCount = (int)business["review_count"];

                    //Get the url of the business
                    string url = (string)business["url"];

                    // Add restaurant to array of restaurants
                    hotAndNewRestaurants[i] = $"{name}\n{address}\nRating: {rating} stars\n({reviewCount} reviews)\n{url}\n";
                }
                return hotAndNewRestaurants;
            }
        }

        private void ResultsListBox_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsListBox.SelectedItem != null)
            {
                // Retrieve the selected item
                string selectedItem = ResultsListBox.SelectedItem.ToString();

                // Extract the URL from the selected item
                string selectedUrl = GetRestaurantUrl(selectedItem);

                if (!string.IsNullOrEmpty(selectedUrl))
                {
                    // Open the URL in the default web browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedUrl,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("The selected item does not have a URL.");
                }

                // save viewed restaurant to db
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // create table if it doesn't already exist
                    SQLiteCommand createCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS \"PreviousViewed\" (\"RestaurantString\" BLOB)", connection);
                    createCommand.ExecuteNonQuery();

                    // insert suggested restaurant into db
                    string query = "INSERT INTO PreviousViewed (RestaurantString) VALUES (@Value)";
                    SQLiteCommand insertCommand = new SQLiteCommand(query, connection);
                    insertCommand.Parameters.AddWithValue("@Value", selectedItem);
                    insertCommand.ExecuteNonQuery();

                    connection.Close();
                }

                // refresh listbox to show previous suggested restaurants
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // query db for results
                    SQLiteCommand selectCommand = new SQLiteCommand("SELECT * FROM PreviousViewed", connection);
                    SQLiteDataReader reader = selectCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        string restaurantString = reader.GetString(0);

                        ViewedListbox.Items.Add(restaurantString);
                    }
                    reader.Close();

                    connection.Close();
                }
            }
        }

        private string GetRestaurantUrl(string restaurantInfo)
        {
            // Find the index of the URL within the restaurant information
            int urlIndex = restaurantInfo.IndexOf("https://www.yelp.com/");

            if (urlIndex != -1)
            {
                // Extract the URL from the restaurant information
                string selectedUrl = restaurantInfo.Substring(urlIndex);
                return selectedUrl;
            }

            return null;
        }

   
    }
}

