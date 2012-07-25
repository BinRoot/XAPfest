using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using System.Collections;

namespace SmashSampleApp
{
    public partial class InvitationPage : PhoneApplicationPage
    {
        string eventid;
        string friend;
        string friendid;
        string myid;

        MainPage MP;

        public InvitationPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (this.NavigationContext.QueryString.ContainsKey("eventid")
                && this.NavigationContext.QueryString.ContainsKey("friend")
                && this.NavigationContext.QueryString.ContainsKey("friendid")
                && this.NavigationContext.QueryString.ContainsKey("myid"))
            {
                eventid = this.NavigationContext.QueryString["eventid"];
                friend = this.NavigationContext.QueryString["friend"];
                friendid = this.NavigationContext.QueryString["friendid"];
                myid = this.NavigationContext.QueryString["myid"];

                PageTitle.Text = friend;
            }

            //DataUse.Instance.EventId = long.Parse(eventid);

            MP = new MainPage();
            MP.JoinMeeting(eventid);

            base.OnNavigatedTo(e);
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataUse.Instance.DS == null)
            {
                DataUse.Instance.DS = new DataService(null);
            }

            DataUse.Instance.DS.GetPushKey(friendid, this);

            string message = friend+" hello from invitation page";
            MP.SendText(message);
        }

        public void SendToastToUser(string pushkey)
        {
            Debug.WriteLine("sending push to " + pushkey);
            string Title = "LyncUp";
            string SubTitle = friend + " accepted your Lync Up request";
            IDictionary DataTable = new Dictionary<String, String>();
            DataTable["friendacceptid"] = myid;
            DataTable["friendstatus"] = "yes";
            PushAPI.SendToastToUser(pushkey, Title, SubTitle, DataTable, "MainPage");

            
        }


    }
}