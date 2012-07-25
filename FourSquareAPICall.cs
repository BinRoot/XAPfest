using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Device.Location;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace SmashSampleApp
{
    public class FourSquareAPICall
    {

        public static readonly string food = "4d4b7105d754a06374d81259";
        public static readonly string entertainment = "4d4b7104d754a06370d81259";
        public static readonly string nightlife = "4d4b7105d754a06376d81259";
        public static readonly string shopping = "4d4b7105d754a06378d81259";
        public static readonly string coffee = "4bf58dd8d48988d1e0931735";
        public static readonly string bars = "4bf58dd8d48988d116941735";
        public static readonly string parks = "4bf58dd8d48988d163941735";
        public static readonly string animalShelters = "4e52d2d203646f7c19daa8ae";
        public static readonly string medicalCenters = "4bf58dd8d48988d104941735";
        public static readonly string cafeterias = "4bf58dd8d48988d128941735";
        public static readonly string schools = "4bf58dd8d48988d13b941735";

        public int radius { get; set; }
        public List<string> categories { get; set; } //For future extensibility to multiple categories
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public GeoCoordinate position { get; set; }
        MainPage MP;

        public FourSquareAPICall()
        {
            client_id = "A3U5IEYF33OTLYAMC5AYOF0YSZ33ZYV2THOJJA2LHEXLSXQF";
            client_secret = "IC0GXGTW5B4WM2C3LT4RQ2RRE21V4GVWESJJ3FTNDXCLSZNG&v=20120706";
            radius = 800;
            categories = null;
            position = null;
        }

        public FourSquareAPICall(int rad, List<string> cat, GeoCoordinate pos, MainPage MP)
        {
            client_id = "A3U5IEYF33OTLYAMC5AYOF0YSZ33ZYV2THOJJA2LHEXLSXQF";
            client_secret = "IC0GXGTW5B4WM2C3LT4RQ2RRE21V4GVWESJJ3FTNDXCLSZNG&v=20120706";
            radius = rad;
            categories = cat;
            position = pos;
            this.MP = MP;
        }

        public void GetVenues()
        {
            StringBuilder url = new StringBuilder("https://api.foursquare.com/v2/venues/search?ll=");
            url.Append(position.Latitude + "," + position.Longitude);

            if (categories.Count != 0)
            {
                url.Append("&categoryId=");
                for (int i = 0; i < categories.Count - 1; i++)
                {
                    url.Append(categories[i] + ",");
                }
                url.Append(categories[categories.Count - 1]);
            }
            url.Append("&client_id=" + client_id + "&client_secret=" + client_secret);

            Debug.WriteLine(url);

            send(url.ToString());
        }


        public void send(string url)
        {
            WebClient c = new WebClient();
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
            c.DownloadStringAsync(new Uri(url));
        }

        /* parses the JSON response from the server */
        public void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Debug.WriteLine("d:" + e.Result);

            JObject allObject = JObject.Parse(e.Result);
            JObject response = (JObject)allObject["response"];
            JArray venues = (JArray)response["venues"];
            List<Venue> temp = new List<Venue>();
            for (int i = 0; i < venues.Count; i++)
            {
                JObject j = (JObject)venues[i];
                string name = (string)j["name"];
                JObject loc = (JObject)j["location"];
                temp.Add(new Venue(new GeoCoordinate((double)loc["lat"], (double)loc["lng"]), name));
                //Debug.WriteLine((double)loc["lat"] + "," + (double)loc["lng"]);
            }

            MP.plotVenues(temp);
        }
    }
}
