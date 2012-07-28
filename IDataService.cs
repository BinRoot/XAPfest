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

namespace SmashSampleApp
{
    public interface IDataService
    {
        void GetFriends(string id, string name);
        void UpdatePushkey(string id, string pushkey);
        void GetNextEventId();
        void GetPushKey(string id, InvitationPage IP);
    }
}