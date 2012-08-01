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
using System.Diagnostics;
using System.Collections.Generic;
using SmashSampleApp.Model;
using Newtonsoft.Json;

namespace SmashSampleApp
{
    public class DataService : IDataService
    {

        MainPage MP;
        InvitationPage IP;
        FriendAdder FA;

        public DataService(MainPage MP)
        {
            this.MP = MP;
        }

        public void GetFriends(string id, string name)
        {
            send("http://lyncapi.appspot.com/user?id=" + id + "&name=" + name);
        }

        public void UpdatePushkey(string id, string pushkey)
        {
            send("http://lyncapi.appspot.com/user?id=" + id + "&pushkey=" + pushkey);
        }

        public void GetNextEventId()
        {
            send("http://lyncapi.appspot.com/event?op=n");
        }

        public void GetPushKey(string id, InvitationPage IP)
        {
            this.IP = IP;
            send("http://lyncapi.appspot.com/user?pid=" + id);
        }

        public void GetPushKey(string id, MainPage MP)
        {
            this.MP = MP;
            send("http://lyncapi.appspot.com/user?pid=" + id);
        }

        public void GetSearchResults(string searchStr, FriendAdder FA)
        {
            this.FA = FA;
            send("http://lyncapi.appspot.com/search?str=" + searchStr);
        }   

        public void GetProfile(string id)
        {
            send("https://apis.live.net/v5.0/" + id +"/picture");
        }

        public void AddFriend(string myid, string friendid)
        {
            send("http://lyncapi.appspot.com/friend?id=" + myid + "&friend=" + friendid);
        }

        public void RemoveFriend(string myid, string friendid)
        {
            send("http://lyncapi.appspot.com/friend?id=" + myid + "&friend=" + friendid + "&remove=true");
        }


        public void send(string url)
        {
            WebClient c = new WebClient();
            c.BaseAddress = url;
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
            c.DownloadStringAsync(new Uri(url));
        }

        /* parses the JSON response from the server */
        public void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            WebClient WC = (WebClient)sender;

            if (WC.BaseAddress.Contains("id=") && WC.BaseAddress.Contains("name="))
            {
                try
                {
                    Debug.WriteLine("d:" + e.Result);
                    List<Friend> Friends = JsonConvert.DeserializeObject<List<Friend>>(e.Result);

                    MP.RespondToGetFriends(Friends);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERR on DownloadStringCompleted: " + ex.Message);
                }
            }
            else if (WC.BaseAddress.Contains("id=") && WC.BaseAddress.Contains("pushkey="))
            {
                // Do nothing
            }
            else if (WC.BaseAddress.Contains("pid="))
            {
                string pushkey = e.Result;
                if (IP != null)
                {
                    IP.SendToastToUser(pushkey);
                    IP = null;
                }
                else
                {
                    MP.SendQuickToastToUser(pushkey);
                    MP = null;
                }
                
            }
            else if (WC.BaseAddress.Contains("/event"))
            {
                long EventId = long.Parse(e.Result);
                DataUse.Instance.EventId = EventId;
                MP.CreateMeeting();
            }
            else if (WC.BaseAddress.Contains("apis.live.net"))
            {
                // do nothing
            }
            else if (WC.BaseAddress.Contains("/search"))
            {
                List<Friend> Friends = JsonConvert.DeserializeObject<List<Friend>>(e.Result);
                FA.RespondToSearchResults(Friends);
                FA = null;
            }
        }
    }
}
