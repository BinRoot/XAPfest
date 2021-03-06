﻿using System;
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
using System.IO.IsolatedStorage;

namespace SmashSampleApp
{
    public class DataUse
    {
        public List<Friend> FriendsList { get; set; }

        public string MyUserId { get; set; }
        public string MyUserName { get; set; }
        public long EventId { get; set; }
        public IDataService DS { get; set; }
        public string RoomName { get; set; }
        public bool RoomCreated { get; set; }
        public string MessageToSend { get; set; }
        public bool ActiveLocationMode { get; set; }

        public MainPage MP;

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
        private List<Friend> activeFriends;
        public List<Friend> ActiveFriends
        {
            get
            {
                if (activeFriends == null)
                {
                    activeFriends = new List<Friend>();
                    Friend me = new Friend(MyUserName, MyUserId, "");
                    me.status = "yes";
                    me.transportation = (string)IsolatedStorageSettings.ApplicationSettings["transportation"];
                    activeFriends.Add(me);
                }
                return activeFriends;
            }
            set
            {
                activeFriends = value;
            }
        }

    }
}
