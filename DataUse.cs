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
using SmashSampleApp.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace SmashSampleApp
{
    public class DataUse
    {
        public List<Friend> FriendsList { get; set; }
        public List<Friend> ActiveFriends { get; set; }
        public string MyUserId { get; set; }
        public string MyUserName { get; set; }
        public long EventId { get; set; }
        public IDataService DS { get; set; }
        public string RoomName { get; set; }
        

        private static DataUse instance;
        private DataUse() { }

        public static DataUse Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataUse();
                }
                return instance;
            }
        }


    }
}
