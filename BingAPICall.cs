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
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Device.Location;
using SmashSampleApp.Model;

namespace SmashSampleApp
{
    public class BingAPICall
    {
        public static readonly string apiKey = "AhrsbBfWVAnxFwOsw5ARNmg2r_rjHf5nNTKa-bsdhKUZaLSwIsLi7m5_lo86b2XL";
        public GeoCoordinate start { get; set; }
        public GeoCoordinate end { get; set; }
        public decimal distance { get; set; }
        public int time { get; set; }
        MainPage MP;
        public Friend f;
        public string transportation { get; set; }

        public BingAPICall(GeoCoordinate start, GeoCoordinate end, string transportation, MainPage MP, Friend f)
        {
            this.start = start;
            this.end = end;
            this.MP = MP;
            this.transportation = transportation;
            this.f = f;
        }

        public void GetData()
        {
            //http://dev.virtualearth.net/REST/V1/Routes/Driving?wp.0=47.63895,-122.322609&wp.1=47.605353,-122.327641&du=mi&key=AhrsbBfWVAnxFwOsw5ARNmg2r_rjHf5nNTKa-bsdhKUZaLSwIsLi7m5_lo86b2XL
            StringBuilder url = new StringBuilder("http://dev.virtualearth.net/REST/V1/Routes/");
            switch (transportation)
            {
                case "car":
                    url.Append("Driving?");
                    break;
                case "walk":
                    url.Append("Walking?");
                    break;
                default:
                    url.Append("Driving?");
                    break;
            }
            url.Append("wp.0=" + start.Latitude + "," + start.Longitude + "&wp.1=" + end.Latitude + "," + end.Longitude);
            url.Append("&du=mi&key=" + apiKey);

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
            JArray resourceSets = (JArray)allObject["resourceSets"];
            JObject firstResource = (JObject)resourceSets[0];
            JArray innerResource = (JArray)firstResource["resources"];
            JObject desiredObject = (JObject)innerResource[0];

            distance = Math.Round((decimal)desiredObject["travelDistance"], 2);
            if (transportation == "bike")
            {
                time = (int)Math.Ceiling((double)distance) * 360;
            }
            else
            {
                time = (int)desiredObject["travelDuration"];
            }
            f.dataString = distance + " mi" + "\n" + time + " s";
            MessageBox.Show(f.dataString);

            MP.FinalizeList.DataContext = null;
            MP.FinalizeList.DataContext = DataUse.Instance.ActiveFriends;
        } 
    }
}