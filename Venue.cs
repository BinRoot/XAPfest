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
using System.Device.Location;

namespace SmashSampleApp
{
    public class Venue
    {
        public GeoCoordinate location { get; set; }
        public string name { get; set; }

        public Venue(GeoCoordinate location, string name)
        {
            this.location = location;
            this.name = name;
        }
    }
}
