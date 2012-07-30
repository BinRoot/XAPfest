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
using System.Windows.Media.Imaging;

namespace SmashSampleApp.Model
{
    public class Friend
    {
        public string name { get; set; }
        public string id { get; set; }
        public string pushkey { get; set; }
        public string status { get; set; }
        public string transportation { get; set; }
        public string dataString { get; set; }

        public BitmapImage ImgSrc
        {
            get
            {
                return new BitmapImage(new Uri("https://apis.live.net/v5.0/" + id + "/picture", UriKind.Absolute));
            }
        }

        public Uri ImgUri
        {
            get
            {
                return new Uri("https://apis.live.net/v5.0/" + id + "/picture", UriKind.Absolute);
            }
        }


        public Friend(string name, string id, string pushkey)
        {
            this.name = name;
            this.id = id;
            this.pushkey = pushkey;
        }
    }
}
