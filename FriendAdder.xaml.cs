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
using Microsoft.Phone.Controls;
using System.Threading;
using SmashSampleApp.Model;
using System.Collections;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Phone.Shell;

namespace SmashSampleApp
{
    public partial class FriendAdder : PhoneApplicationPage
    {
        System.Collections.ObjectModel.ObservableCollection<Model.FriendCategory> foodCategories;
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public FriendAdder()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            List<Friend> FriendsList = DataUse.Instance.FriendsList;
            if (FriendsList == null) FriendsList = new List<Friend>();

            if (GroupedList.ItemsSource == null)
            {
                foodCategories = new System.Collections.ObjectModel.ObservableCollection<Model.FriendCategory>();
                for (char i = 'A'; i <= 'Z'; i++)
                {
                    foodCategories.Add(new Model.FriendCategory(i.ToString()));
                }

                foreach (Friend f in FriendsList)
                {
                    AddItemToCategory(f.id, f.name, f.pushkey);
                }

                GroupedList.ItemsSource = foodCategories;
            }
        }

        private void AddItemToCategory(string id, string name, string pushkey)
        {
            name = name.ToUpper();
            char FirstLetter = name.ToCharArray()[0];
            foodCategories[FirstLetter - 'A'].AddFoodItem(new Model.Friend(name, id, pushkey));
        }



        private void GroupedList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GroupedList.SelectedItem is Model.Friend)
            {
                Friend f = ((Model.Friend)GroupedList.SelectedItem);

                inviteFriendToLyncup(f);

            }
        }



        private void TextBlock_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            ((UIElement)sender).RenderTransform = new System.Windows.Media.TranslateTransform() { X = 2, Y = 2 };
        }

        private void TextBlock_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            ((UIElement)sender).RenderTransform = null;
        }




        private void SendToastToUser(Friend f)
        {
            string Title = "LyncUp";
            string SubTitle = DataUse.Instance.MyUserName + " wants to lync up!";
            string FriendName = DataUse.Instance.MyUserName;
            string FriendId = DataUse.Instance.MyUserId;
            string EventId = DataUse.Instance.RoomName + "";
            IDictionary DataTable = new Dictionary<string, string>();
            DataTable["friend"] = FriendName;
            DataTable["eventid"] = EventId;
            DataTable["friendid"] = FriendId;
            DataTable["myid"] = f.id;
            DataTable["myname"] = f.name;

            PushAPI.SendToastToUser(f.pushkey, Title, SubTitle, DataTable, "InvitationPage");

        }

        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            string TextBoxTitle = "LyncUp";
            string TextBoxSubTitle = DataUse.Instance.MyUserName + " wants to lync up!";
            string FriendName = DataUse.Instance.MyUserName;
            string EventId = DataUse.Instance.RoomName;

            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            string postData = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<wp:Notification xmlns:wp=\"WPNotification\">" +
                   "<wp:Toast>" +
                        "<wp:Text1>" + TextBoxTitle + "</wp:Text1>" +
                        "<wp:Text2>" + TextBoxSubTitle + "</wp:Text2>" +
                        "<wp:Param>/InvitationPage.xaml?friend=" + FriendName + "&amp;eventid=" + EventId + "</wp:Param>" +
                   "</wp:Toast> " +
                "</wp:Notification>";




            // Convert the string into a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);



            // Write to the request stream.
            postStream.Write(byteArray, 0, postData.Length);
            postStream.Close();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                string responseString = streamRead.ReadToEnd();
                Console.WriteLine(responseString);
                // Close the stream object
                streamResponse.Close();
                streamRead.Close();

                // Release the HttpWebResponse
                response.Close();
                allDone.Set();
            }
            catch (Exception e)
            {
                Debug.WriteLine("PUSH ERR: " + e.Message);
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            SearchPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;

            ApplicationBar.Buttons.Clear();
            ApplicationBarIconButton button1 = new ApplicationBarIconButton();
            button1.IconUri = new Uri("/Images/Icons/back_icon.png", UriKind.Relative);
            button1.Text = "Browse";
            button1.Click += new EventHandler(Back_clicked);
            ApplicationBar.Buttons.Add(button1);
        }

        private void Back_clicked(object sender, EventArgs e)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;

            ApplicationBar.Buttons.Clear();
            ApplicationBarIconButton button1 = new ApplicationBarIconButton();
            button1.IconUri = new Uri("/Images/Icons/search_icon.png", UriKind.Relative);
            button1.Text = "Search";
            button1.Click += new EventHandler(Search_Click);
            ApplicationBar.Buttons.Add(button1);
        }

        public void RespondToSearchResults(List<Friend> fList)
        {
            FriendSearchListBox.DataContext = null;
            FriendSearchListBox.DataContext = fList;
            
        }

        private void Search_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DataUse.Instance.DS.GetSearchResults(SearchBox.Text, this);
        }

        public void AddFriend_Tap(object sender, EventArgs e)
        {
            Button b = (Button) sender;
            Friend f = (Friend)b.Tag;

            bool found = false;
            foreach (Friend ff in DataUse.Instance.FriendsList)
            {
                if (ff.id == f.id || f.id == DataUse.Instance.MyUserId)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                DataUse.Instance.DS.AddFriend(DataUse.Instance.MyUserId, f.id);

                DataUse.Instance.FriendsList.Add(f);

                inviteFriendToLyncup(f);
            }
        }

        private void inviteFriendToLyncup(Friend f)
        {
            if (DataUse.Instance.ActiveFriends == null)
            {
                DataUse.Instance.ActiveFriends = new List<Friend>();
            }

            bool found = false;
            foreach (Friend ff in DataUse.Instance.ActiveFriends)
            {
                if (ff.id == f.id || f.id == DataUse.Instance.MyUserId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                DataUse.Instance.ActiveFriends.Add(f);
                SendToastToUser(f);
                NavigationService.GoBack();
            }
            else
            {
                // already exists error
                MessageBox.Show(f.name + " has already been added");
            }


            
        }

        // friendlsit, activefriends
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            Friend f = (Friend)mi.Tag;

            DataUse.Instance.DS.RemoveFriend(DataUse.Instance.MyUserId, f.id);

            DataUse.Instance.ActiveFriends.Remove(f);
            DataUse.Instance.FriendsList.Remove(f);

            List<Friend> newActiveFriends = new List<Friend>();
            foreach(Friend ff in DataUse.Instance.ActiveFriends)
            {
                if (ff.id != f.id)
                {
                    newActiveFriends.Add(ff);
                }
            }
            DataUse.Instance.ActiveFriends = newActiveFriends;

            List<Friend> newFriendsList = new List<Friend>();
            foreach (Friend ff in DataUse.Instance.FriendsList)
            {
                if (ff.id != f.id)
                {
                    newFriendsList.Add(ff);
                }
            }
            DataUse.Instance.FriendsList = newFriendsList;

            NavigationService.GoBack();
        }
    }
}