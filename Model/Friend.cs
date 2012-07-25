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

namespace SmashSampleApp.Model
{
    public class Friend
    {
        public string name { get; set; }
        public string id { get; set; }
        public string pushkey { get; set; }
        public string status { get; set; }

        public Friend(string name, string id, string pushkey)
        {
            this.name = name;
            this.id = id;
            this.pushkey = pushkey;
        }
    }
}
