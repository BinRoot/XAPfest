// ----------------------------------------------------------
// <copyright file="MainPage.Xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ----------------------------------------------------------

namespace SmashSampleApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;

    using Microsoft.Hawaii.Smash.Client;
    using Microsoft.Phone.Controls.Primitives;

    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.UserData;
    using System.Device.Location;
    using SmashSampleApp.Model;
    using System.Windows.Controls.Primitives;
    using Microsoft.Phone.Controls.Maps;
    using Microsoft.Phone.Tasks;
    using Microsoft.Phone.Notification;
    using System.Text;
    using System.IO.IsolatedStorage;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.ServiceModel;
    using System.Collections.ObjectModel;
    using System.Text.RegularExpressions;



    /// <summary>
    /// 
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// 
        /// </summary>
        private const string ApplicationSecret = "0B797E34-905A-406D-B8CF-F57DC6EB0839";

        /// <summary>
        /// Just for this sample application, we hard-code a managementID.
        /// In a real application, this should be generated once and kept securely as an owner-specific secrect.
        /// </summary>
        private string ManagementID = "0B797E34-905A-406D-B8CF-F57DC6EB083A";

        /// <summary>
        /// 
        /// </summary>
        private static MainPage mainPage;

        /// <summary>
        /// 
        /// </summary>
        private static IDictionary<string, object> state;

        /// <summary>
        /// 
        /// </summary>
        private SmashSession session;

        /// <summary>
        /// 
        /// </summary>
        private SmashTable<Channels.ChatRecord> chat;

        private bool firstPlot = true;
        public List<Venue> venues;
        public Venue selectedVenue = null;
        public List<string> transportationOptions = new List<string>() { "car", "bike", "walk" };
        bool firstTimeLaunch = true;
        double currentLat = 0.0;
        double currentLon = 0.0;
        /// <summary>
        /// Constructor
        /// </summary>
        ///

        private bool InSetupMode
        {
            get
            {
                bool setupMode = false;
                try
                {
                    setupMode = (Boolean)settings["setupMode"];
                }
                catch (KeyNotFoundException knfe)
                {
                    setupMode = true;
                }
                return setupMode;
            }
        }

        Dictionary<String, GeoCoordinate> friendMap = new Dictionary<string, GeoCoordinate>();
        GeoCoordinateWatcher watcher;
        IDataService DS;

        // MY GOD THIS IS GOING TO NEED REFACTORING!!!
        public MainPage(bool createNewEvent)
        {
            this.InitializeComponent();

            this.VerifyHawaiiId();

            ManagementID = Guid.NewGuid().ToString();

            DataUse.Instance.ActiveLocationMode = false;

            #region [MAP]
            // The watcher variable was previously declared as type GeoCoordinateWatcher. 
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();

            LyncUpMap.Tap += this.map_Tap;
            #endregion


        }

        public MainPage()
        {
            this.InitializeComponent();

            this.VerifyHawaiiId();

            DataUse.Instance.ActiveLocationMode = true;


            AddOrUpdateSettings("radius", 1);

            try
            {
                string trans = (string)settings["transportation"];
                if (string.IsNullOrEmpty(trans))
                {
                    AddOrUpdateSettings("transportation", "car");
                }
                else
                {
                    TransportationText.Text = (string)settings["transportation"];
                    TransportationOption.Source = new BitmapImage(new Uri("Images/Icons/" + (string)settings["transportation"] + "_icon_white.png", UriKind.Relative));
                }
            }
            catch
            {
                AddOrUpdateSettings("transportation", "car");
            }
            

            if (InSetupMode == false)
            {
                setUpDone();
            }
            else
            {
                ApplicationBar.IsVisible = false;
            }

            FinalizeList.DataContext = DataUse.Instance.ActiveFriends;

            //TestImage.Source = new BitmapImage(new Uri("https://apis.live.net/v5.0/" + DataUse.Instance.MyUserId + "/picture", UriKind.Absolute));

            //this.SendText.Click += new RoutedEventHandler(this.SendText_Click);
            //this.Join.Click += new RoutedEventHandler(this.Join_Click);
            //this.Create.Click += new RoutedEventHandler(this.Create_Click);

            ManagementID = Guid.NewGuid().ToString();

            mainPage = this;
            if (state != null)
            {
                this.chat = new SmashTable<Channels.ChatRecord>("Chat");
                this.Dispatcher.BeginInvoke(() =>
                {
                    ChatText.DataContext = this.chat;
                });
                this.session = SmashSession.JoinSessionFromState(HawaiiClient.HawaiiApplicationId, this.Dispatcher, state, new ISmashTable[] { this.chat });
            }


            DS = new DataService(this);
            DataUse.Instance.DS = DS;
            DataUse.Instance.DS.GetFriends(DataUse.Instance.MyUserId, DataUse.Instance.MyUserName);

            // Set the data context of the listbox control to the sample data

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            PeopleList.DataContext = DataUse.Instance.ActiveFriends;


            #region [MAP]
            // The watcher variable was previously declared as type GeoCoordinateWatcher. 
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();

            LyncUpMap.Tap += this.map_Tap;
            #endregion


            #region [PUSH]
            /// Holds the push channel that is created or found.
            HttpNotificationChannel pushChannel;

            // The name of our push channel.
            string channelName = "ToastSampleChannel";

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                pushChannel.Open();

                // Bind this new channel for toast events.
                pushChannel.BindToShellToast();

            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                // MessageBox.Show(String.Format("Channel Uri is {0}", pushChannel.ChannelUri.ToString()));
                DataUse.Instance.DS.UpdatePushkey(DataUse.Instance.MyUserId, pushChannel.ChannelUri.ToString());
            }
            #endregion

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationService.RemoveBackEntry();
            FinalizeList.DataContext = null;
            FinalizeList.DataContext = DataUse.Instance.ActiveFriends;

            FriendsOnMapList.DataContext = null;
            FriendsOnMapList.DataContext = DataUse.Instance.ActiveFriends;

            if (this.NavigationContext.QueryString.ContainsKey("friendid")
                && this.NavigationContext.QueryString.ContainsKey("eventname")
                && this.NavigationContext.QueryString.ContainsKey("eventloc"))
            {
                string friendid = this.NavigationContext.QueryString["friendid"];
                string eventname = this.NavigationContext.QueryString["eventname"];
                string eventloc = this.NavigationContext.QueryString["eventloc"];

                AddOrUpdateSettings("ready", true);
                AddOrUpdateSettings("friendid", friendid);
                AddOrUpdateSettings("eventname", eventname);
                AddOrUpdateSettings("eventloc", eventloc);

                try
                {
                    DataUse.Instance.MyUserName = (string)settings["myname"];
                    DataUse.Instance.MyUserId = (string)settings["myid"];
                }
                catch (Exception err)
                {
                    MessageBox.Show("onnav err: " + err.Message);
                }




                // TODO: plot eventloc

                JoinMeeting((string)settings["eventid"]);

                setUpDone();
            }
            else if (firstTimeLaunch)
            {
                DataUse.Instance.DS.GetNextEventId();
            }

            firstTimeLaunch = false;

            base.OnNavigatedTo(e);
        }


        #region [PUSH]
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Display the new URI for testing purposes.   Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
                //MessageBox.Show(String.Format("Channel Uri is {0}", e.ChannelUri.ToString()));
                DataUse.Instance.DS.UpdatePushkey(DataUse.Instance.MyUserId, e.ChannelUri.ToString());

            });
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys)
            {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];

                    if (relativeUri.Contains("InvitationPage.xaml"))
                    {
                        Uri uri = new Uri(relativeUri, UriKind.Relative);
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(uri); });
                        return;
                    }

                    if (relativeUri.Contains("eventname"))
                    {
                        Uri uri = new Uri(relativeUri, UriKind.Relative);
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(uri); });
                        return;
                    }



                    string[] dataParts = (relativeUri.Split('?')[1]).Split('&');

                    string Status = "";
                    string FriendId = "";
                    string TransportationType = "";
                    for (int i = 0; i < dataParts.Length; i++)
                    {
                        string[] keyval = dataParts[i].Split('=');
                        if (keyval[0] == "friendacceptid")
                        {
                            FriendId = keyval[1];
                        }
                        else if (keyval[0] == "friendstatus")
                        {
                            Status = keyval[1];
                        }
                        else if (keyval[0] == "transportation")
                        {
                            TransportationType = keyval[1];
                        }
                    }

                    if (Status == "yes")
                    {
                        for (int i = 0; i < DataUse.Instance.ActiveFriends.Count; i++)
                        {
                            Friend f1 = DataUse.Instance.ActiveFriends[i];
                            if (f1.id == FriendId)
                            {
                                DataUse.Instance.ActiveFriends[i].status = "yes";
                                DataUse.Instance.ActiveFriends[i].transportation = TransportationType;
                            }
                        }

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            PeopleList.DataContext = null;
                            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
                        });
                    }
                }
            }

            // Display a dialog of all the fields in the toast.
            // Dispatcher.BeginInvoke(() => MessageBox.Show(message.ToString()));

        }
        #endregion





        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="statePreserved"></param>
        public static void ActivateSession(IDictionary<string, object> s, bool statePreserved)
        {
            if (statePreserved && mainPage != null)
            {
                state = null;
                mainPage.session.ResumeSession();
            }
            else
            {
                state = s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        public static void DeactivateSession(IDictionary<string, object> s)
        {
            if (mainPage != null && mainPage.session != null)
            {
                mainPage.session.DeactivateSession(s);
            }
        }



        public void RespondToGetFriends(List<Friend> Friends)
        {
            DataUse.Instance.FriendsList = Friends;

        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            PeopleList.DataContext = null;
            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
        }

        private void AddPerson_Button(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri("/FriendAdder.xaml", UriKind.Relative);
            NavigationService.Navigate(uri);
        }


        #region [MAP]
        private void NamePanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Popup popup = new Popup();
            popup.Height = 300;
            popup.Width = 400;
            popup.VerticalOffset = System.Windows.Application.Current.Host.Content.ActualHeight / 2.5;
            DisplayNamePopupUserControl control = new DisplayNamePopupUserControl();
            popup.Child = control;
            popup.IsOpen = true;


            control.btnOK.Click += (s, args) =>
            {
                popup.IsOpen = false;
                //name.Text = control.tbx.Text;
            };

            control.btnCancel.Click += (s, args) =>
            {
                popup.IsOpen = false;
            };
        }

        private void TransportationPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            settings["transportation"] = transportationOptions[(transportationOptions.IndexOf((string)settings["transportation"]) + 1) % 3];
            switch ((string)settings["transportation"])
            {
                case "bike":
                    TransportationOption.Source = new BitmapImage(new Uri("Images/Icons/bike_icon_white.png", UriKind.Relative));
                    AddOrUpdateSettings("transportation", "bike");
                    DataUse.Instance.ActiveFriends[0].transportation = "bike";
                    break;
                case "car":
                    TransportationOption.Source = new BitmapImage(new Uri("Images/Icons/car_icon_white.png", UriKind.Relative));
                    DataUse.Instance.ActiveFriends[0].transportation = "car";
                    AddOrUpdateSettings("transportation", "car");
                    break;
                case "walk":
                    TransportationOption.Source = new BitmapImage(new Uri("Images/Icons/walk_icon_white.png", UriKind.Relative));
                    DataUse.Instance.ActiveFriends[0].transportation = "walk";
                    AddOrUpdateSettings("transportation", "walk");
                    break;
                default:
                    break;
            }
            TransportationText.Text = (string)settings["transportation"];
        }

        private void RadiusPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            settings["radius"] = ((int)settings["radius"] % 10) + 1;
            radiusText.Text = (int)settings["radius"] + ((int)settings["radius"] != 1 ? " miles" : " mile");
            plotLocation();
        }

        private void RemovePerson_Button(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            string id = (string)b.Tag;
            List<Friend> newActiveFriends = new List<Friend>();
            foreach (Friend f in DataUse.Instance.ActiveFriends)
            {
                if (f.id != id)
                {
                    newActiveFriends.Add(f);
                }
            }

            DataUse.Instance.ActiveFriends = null;
            DataUse.Instance.ActiveFriends = newActiveFriends;

            FinalizeList.DataContext = null;
            FinalizeList.DataContext = DataUse.Instance.ActiveFriends;

            PeopleList.DataContext = null;
            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
        }

        // Event handler for the GeoCoordinateWatcher.StatusChanged event.
        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see whether the user has disabled the Location Service.
                    if (watcher.Permission == GeoPositionPermission.Denied)
                    {
                        // The user has disabled the Location Service on their device.
                        MessageBox.Show("you have blocked this application from access to location.");
                    }
                    else
                    {
                        MessageBox.Show("location is not functioning on this device");
                    }
                    break;

                case GeoPositionStatus.Initializing:
                    // The Location Service is initializing.
                    // Disable the Start Location button.
                    break;

                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    // Alert the user and enable the Stop Location button.
                    MessageBox.Show("location data is not available.");
                    break;

                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    // Show the current position and enable the Stop Location button.
                    break;
            }
        }

        public void plotVenues(List<Venue> venues)
        {
            this.venues = venues;
            var list = (from x in venues select x.location).ToArray();


            for (int i = 0; i < venues.Count; i++)
            {
                Pushpin venuePushpin = new Pushpin();
                venuePushpin.Background = new SolidColorBrush(Colors.Blue);
                venuePushpin.Opacity = 0.6;
                venuePushpin.Location = venues[i].location;
                venuePushpin.Tap += this.pin_Tap;

                venuePushpin.Content = new TextBlock();
                ((TextBlock)venuePushpin.Content).Text = venues[i].name;
                ((TextBlock)venuePushpin.Content).Visibility = Visibility.Collapsed;

                LyncUpMap.Children.Add(venuePushpin);
            }

            LyncUpMap.SetView(LocationRect.CreateLocationRect(list));
        }

        private void clearTooltips()
        {
            Map m = MainMap;
            if (InSetupMode)
            {
                m = LyncUpMap;
            }

            if (m.Children.Count != 0)
            {
                List<UIElement> pushpins = m.Children.ToList();

                foreach (var item in pushpins)
                {
                    if (item != null && item.GetType() == typeof(Pushpin))
                    {
                        ((TextBlock)((Pushpin)item).Content).Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void pin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            clearTooltips();

            try
            {
                if (venues != null)
                {
                    selectedVenue = (from x in venues
                                     where ((Venue)x).name == ((TextBlock)((Pushpin)sender).Content).Text
                                     select x).FirstOrDefault();

                    goButton.IsEnabled = true;

                    foreach (var friend in DataUse.Instance.ActiveFriends)
	                {
                        BingAPICall call = new BingAPICall(friendMap[friend.id], selectedVenue.location, friend.transportation, this, friend);
                        call.GetData();
                    }
                    FinalizeList.DataContext = null;
                    FinalizeList.DataContext = DataUse.Instance.ActiveFriends;
                }

                RouteLayer.Children.Clear();
                plotRoute(selectedVenue.location, watcher.Position.Location, RouteLayer);
            }
            catch { };


            //So I heard you like casting...
            ((TextBlock)((Pushpin)sender).Content).Visibility = Visibility.Visible;
            e.Handled = true;
        }

        private void pin_Tap2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            clearTooltips();

            //So I heard you like casting...
            ((TextBlock)((Pushpin)sender).Content).Visibility = Visibility.Visible;
            e.Handled = true;
        }

        public void plotRoute(GeoCoordinate start, GeoCoordinate end, MapLayer ml)
        {
            List<GeoCoordinate> locations = new List<GeoCoordinate>();

            locations.Add(start);
            locations.Add(end);

            RouteService.RouteServiceClient routeService = new RouteService.RouteServiceClient("BasicHttpBinding_IRouteService");

            routeService.CalculateRouteCompleted += (sender2, e2) =>
            {
                var points = e2.Result.Result.RoutePath.Points;
                var coordinates = points.Select(x => new GeoCoordinate(x.Latitude, x.Longitude));

                var routeColor = Colors.Blue;
                var routeBrush = new SolidColorBrush(routeColor);

                var routeLine = new MapPolyline()
                {
                    Locations = new LocationCollection(),
                    Stroke = routeBrush,
                    Opacity = 0.65,
                    StrokeThickness = 5.0,
                };

                foreach (var location in points)
                {
                    routeLine.Locations.Add(new GeoCoordinate(location.Latitude, location.Longitude));
                }

                ml.Children.Add(routeLine);
            };

            MainMap.SetView(LocationRect.CreateLocationRect(locations));

            routeService.CalculateRouteAsync(new RouteService.RouteRequest()
            {
                Credentials = new RouteService.Credentials()
                {
                    ApplicationId = "AhrsbBfWVAnxFwOsw5ARNmg2r_rjHf5nNTKa-bsdhKUZaLSwIsLi7m5_lo86b2XL"
                },
                Options = new RouteService.RouteOptions()
                {
                    RoutePathType = RouteService.RoutePathType.Points
                },
                Waypoints = new ObservableCollection<RouteService.Waypoint>(
                    locations.Select(x => new RouteService.Waypoint()
                    {
                        Location = new RouteService.Location() { Latitude = x.Latitude, Longitude = x.Longitude }
                    }))
            });
        }

        public void plotFriendRoutes()
        {
            try
            {
                RouteLayerMain.Children.Clear();
                foreach (var item in friendMap.Values)
                {
                    GeoCoordinate gEL = getEventLoc();
                    if (gEL != null)
                    {
                        plotRoute(item, gEL, RouteLayerMain); 
                    }
                }
                
            }
            catch (Exception e)
            {
                MessageBox.Show(">< " + e.Message);
            }
        }

        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            clearTooltips();
            goButton.IsEnabled = false;
        }

        private void clearMap(Type t, Map m)
        {
            //Add logic only to remove location pins versus venue pins
            if (LyncUpMap.Children.Count != 0)
            {
                List<UIElement> pushpins = m.Children.ToList();

                foreach (var item in pushpins)
                {
                    if (item != null && t == item.GetType())
                    {
                        m.Children.Remove(item);
                    }
                }
            }
        }


        public void positionChanged(double lat, double lon)
        {
            LyncUpMap.Center = new GeoCoordinate(lat, lon);

            clearMap(typeof(Object), LyncUpMap);


            DataUse.Instance.MessageToSend = (string)settings["transportation"] + "," + DataUse.Instance.MyUserName + "," + DataUse.Instance.MyUserId + "," + lat + "," + lon;
            if (DataUse.Instance.RoomCreated && DataUse.Instance.ActiveLocationMode)
            {
                //MessageBox.Show("sending: " + DataUse.Instance.MyUserName + "," + DataUse.Instance.MyUserId + "," + lat + "," + lon);
                SendText((string)settings["transportation"] + "," + DataUse.Instance.MyUserName + "," + DataUse.Instance.MyUserId + "," + lat + "," + lon);
            }
            else
            {
                //MessageBox.Show(DataUse.Instance.MyUserName + " loc changed, but not sent, " + DataUse.Instance.RoomCreated + "-" + DataUse.Instance.ActiveLocationMode);
            }

        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            currentLat = e.Position.Location.Latitude;
            currentLon = e.Position.Location.Longitude;

            positionChanged(e.Position.Location.Latitude, e.Position.Location.Longitude);
        }

        private void drawCircle(GeoCoordinate center, double radius)
        {
            MapPolygon polygon = new MapPolygon();
            polygon.Fill = new SolidColorBrush(Colors.Green);
            polygon.Stroke = new SolidColorBrush(Colors.Blue);
            polygon.StrokeThickness = 2;
            polygon.Opacity = .1;

            polygon.Visibility = Visibility.Collapsed;
            if (watcher.Position.Location == null)
                return;

            polygon.Visibility = Visibility.Visible;
            polygon.Locations = new LocationCollection();

            var earthRadius = 6371;
            var lat = center.Latitude * Math.PI / 180.0; //radians
            var lon = center.Longitude * Math.PI / 180.0; //radians
            var d = radius / 1000 / earthRadius; // d = angular distance covered on earths surface

            for (int x = 0; x <= 360; x++)
            {
                var brng = x * Math.PI / 180.0; //radians
                var latRadians = Math.Asin(Math.Sin(lat) * Math.Cos(d) + Math.Cos(lat) * Math.Sin(d) * Math.Cos(brng));
                var lngRadians = lon + Math.Atan2(Math.Sin(brng) * Math.Sin(d) * Math.Cos(lat), Math.Cos(d) - Math.Sin(lat) * Math.Sin(latRadians));



                var pt = new GeoCoordinate(180.0 * latRadians / Math.PI, 180.0 * lngRadians / Math.PI);
                polygon.Locations.Add(pt);
            }

            LyncUpMap.Children.Insert(0, polygon);
        }



        private void plotLocation()
        {
            if (DataUse.Instance.ActiveLocationMode)
            {
                clearMap(typeof(MapPolygon), LyncUpMap);
                clearMap(typeof(Pushpin), LyncUpMap);

                if (firstPlot)
                {
                    try
                    {
                        LyncUpMap.SetView(watcher.Position.Location, 12);
                        MainMap.SetView(LocationRect.CreateLocationRect(friendMap.Values));
                    }
                    catch (NullReferenceException)
                    {
                        return;
                    }
                    firstPlot = false;
                }

                List<Pushpin> friendLocations = new List<Pushpin>();

                try
                {
                    double averageLat = 0.0;
                    double averageLong = 0.0;

                    foreach (var id in friendMap.Keys)
                    {

                        GeoCoordinate loc = friendMap[id];


                        //MessageBox.Show("displaying " + f.id + ": " + loc.Latitude + ", " + loc.Longitude);


                        averageLat += loc.Latitude;
                        averageLong += loc.Longitude;

                        Pushpin locationPushpin = new Pushpin();
                        locationPushpin.Background = new SolidColorBrush(Colors.Green);
                        locationPushpin.Location = loc;

                        locationPushpin.Tap += this.pin_Tap;

                        locationPushpin.Content = new TextBlock();
                        ((TextBlock)locationPushpin.Content).Text = friendIdToNameMap[id];
                        ((TextBlock)locationPushpin.Content).Visibility = Visibility.Collapsed;

                        friendLocations.Add(locationPushpin);
                    }

                    //Pick up radius from settings
                    if (!(averageLat == 0.0) && !(averageLong == 0.0))
                    {
                        drawCircle(new GeoCoordinate(averageLat / friendLocations.Count, averageLong / friendLocations.Count), (int)settings["radius"] * 1609.34);
                    }

                    clearMap(typeof(Pushpin), MainMap);

                    plotEvent();

                    foreach (var item in friendLocations)
                    {
                        if (InSetupMode)
                        {

                            LyncUpMap.Children.Add(item);
                        }
                        else
                        {
                            MainMap.Children.Add(item);
                        }
                    }

                    MainMap.SetView(LocationRect.CreateLocationRect(friendLocations.Select(x => x.Location)));
                    MainMap.ZoomLevel = MainMap.ZoomLevel + 0.00001;
                }
                catch (Exception er)
                {
                    MessageBox.Show("shit2: " + er.Message);
                }
            }
        }

        #endregion

        private void plotEvent()
        {
            Pushpin venuePushpin = new Pushpin();
            venuePushpin.Background = new SolidColorBrush(Colors.Blue);
            venuePushpin.Opacity = 0.6;
            venuePushpin.Location = getEventLoc();
            venuePushpin.Tap += this.pin_Tap2;

            venuePushpin.Content = new TextBlock();
            ((TextBlock)venuePushpin.Content).Text = (string)settings["eventname"];
            ((TextBlock)venuePushpin.Content).Visibility = Visibility.Collapsed;

            MainMap.Children.Add(venuePushpin);
        }

        private void FoodButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap(typeof(Pushpin), LyncUpMap);

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.food
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void BarsButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap(typeof(Pushpin), LyncUpMap);

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.bars
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void ShoppingButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap(typeof(Pushpin), LyncUpMap);

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.shopping
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void AttractionsButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap(typeof(Pushpin), LyncUpMap);

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.entertainment
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void ServiceButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap(typeof(Pushpin), LyncUpMap);

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.parks,
                    FourSquareAPICall.animalShelters,
                    FourSquareAPICall.cafeterias,
                    FourSquareAPICall.medicalCenters,
                    FourSquareAPICall.schools
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        /// <summary>
        /// Varify that the Hawaii application Id is set correctly.
        /// </summary>
        private void VerifyHawaiiId()
        {
            if (string.IsNullOrEmpty(HawaiiClient.HawaiiApplicationId))
            {
                MessageBox.Show("The Hawaii Application Id is missing. In order to run this sample you need to obtain a Hawaii Application Id from http://hawaiiguidgen.cloudapp.net. Use that Id in the source file HawaiiClient.cs");
            }
        }

        bool smashScanned = false;
        Dictionary<string, string> friendIdToNameMap = new Dictionary<string, string>();
        private void ChatText_LayoutUpdated(object sender, EventArgs e)
        {
            SmashTable<Channels.ChatRecord> v = (SmashTable<Channels.ChatRecord>)ChatText.DataContext;

            if (v != null)
            {
                if (v.Count != 0)
                {

                    if (!smashScanned)
                    {
                        for (int i = 0; i < v.Count; i++)
                        {
                            string oldMessageEntry = v[i].ChatEntry;
                            readMessage(oldMessageEntry);
                        }

                        smashScanned = true;
                    }
                    else
                    {
                        string newMessage = v[v.Count - 1].ChatEntry;
                        readMessage(newMessage);
                    }
                }
            }
        }

        private void readMessage(string newMessage)
        {
            string[] strSplit = newMessage.Split(',');
            if (strSplit.Length == 5)
            {
                // MessageBox.Show("msg received: " + newMessage);
                string transportation = strSplit[0].Replace("(): ", "").TrimStart();
                string friendname = strSplit[1];
                string friendid = strSplit[2];
                string latStr = strSplit[3];
                string lonStr = strSplit[4];

                
                bool found = false;
                foreach (Friend fa in DataUse.Instance.ActiveFriends)
                {
                    if (fa.id == friendid)
                    {
                        found = true;
                        //MessageBox.Show("found "+fa.name +", ActiveFriends size: "+DataUse.Instance.ActiveFriends.Count);
                        break;
                    }
                }
                if (!found)
                {

                    Friend fa = new Friend(friendname, friendid, "");
                    fa.transportation = transportation;
                    DataUse.Instance.ActiveFriends.Add(fa);

                    FriendsOnMapList.DataContext = null;
                    FriendsOnMapList.DataContext = DataUse.Instance.ActiveFriends;

                    //MessageBox.Show("didn't find " + friendid +", ActiveFriends size: "+DataUse.Instance.ActiveFriends.Count);

                }


                friendIdToNameMap[friendid] = friendname;

                try
                {
                    double latd = double.Parse(latStr);
                    double lond = double.Parse(lonStr);
                    GeoCoordinate g = new GeoCoordinate(latd, lond);

                    try
                    {
                        GeoCoordinate oldLoc = friendMap[friendid];

                        if (oldLoc != g)
                        {
                            friendMap[friendid] = g;
                            friendLocationUpdated();
                        }
                    }
                    catch // not found
                    {
                        friendMap[friendid] = g;
                        friendLocationUpdated();
                    }

                }
                catch (Exception er)
                {
                    MessageBox.Show("**" + er.Message);
                }

            }
            else
            {
                MessageBox.Show("msg received, but incorrect format: " + newMessage);
            }
        }

        private void setUpDone()
        {
            MainMap.Tap += this.map_Tap;
            AddOrUpdateSettings("setupMode", false);
            ApplicationBar.IsVisible = true;
            MainPanorama.Visibility = Visibility.Collapsed;

            plotLocation();

            try
            {
                Boolean isReady = (Boolean)settings["ready"];
                if (isReady)
                {
                    MainMap.Visibility = Visibility.Visible;
                    //DebugButtons.Visibility = Visibility.Visible;
                    FriendsOnMapList.Visibility = Visibility.Visible;
                    NotReadyText.Visibility = Visibility.Collapsed;

                    string eventname = (string)settings["eventname"];
                    string eventloc = (string)settings["eventloc"];

                    // MessageBox.Show(eventname + " at " + eventloc);

                    plotEvent();

                }
                else
                {
                    NotReadyText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("*>* "+e.Message);
            }



        }

        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        public bool AddOrUpdateSettings(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Contains(Key))
            {
                // If the value has changed
                if (settings[Key] != value)
                {
                    // Store the new value
                    settings[Key] = value;
                    settings.Save();
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Add(Key, value);
                settings.Save();
                valueChanged = true;
            }
            return valueChanged;
        }


        private void friendLocationUpdated()
        {
            // MessageBox.Show("friend location updated");
            plotLocation();

            if (friendMap.Keys.Count > 0 && !InSetupMode)
            {
                plotFriendRoutes();
            }
        }

        private void Leave_MenuItem_Click(object sender, EventArgs e)
        {
            AddOrUpdateSettings("ready", false);
            AddOrUpdateSettings("setupMode", true);
            MainPanorama.Visibility = Visibility.Visible;
            MainMap.Visibility = Visibility.Collapsed;
            //DebugButtons.Visibility = Visibility.Collapsed;
            FriendsOnMapList.Visibility = Visibility.Collapsed;
            ApplicationBar.IsVisible = false;

        }

        private void Go_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            string eventloc = selectedVenue.location.Latitude + "," + selectedVenue.location.Longitude;
            AddOrUpdateSettings("eventloc", eventloc);
            AddOrUpdateSettings("eventname", selectedVenue.name);

            AddOrUpdateSettings("ready", true);
            InformActiveFriends();
            setUpDone();

        }

        private void InformActiveFriends()
        {
            if (selectedVenue != null)
            {
                foreach (Friend f in DataUse.Instance.ActiveFriends)
                {
                    if (f.status != "no")
                    {
                        string title = "LyncUp";
                        string subtitle = DataUse.Instance.MyUserName + " is ready to go! ...";


                        Dictionary<string, string> dataDic = new Dictionary<string, string>();
                        dataDic["friendid"] = DataUse.Instance.MyUserId;
                        dataDic["eventname"] = selectedVenue.name;
                        dataDic["eventloc"] = selectedVenue.location.Latitude + "," + selectedVenue.location.Longitude;

                        if (f.pushkey != "")
                        {
                            PushAPI.SendToastToUser(f.pushkey, title, subtitle, dataDic, "MainPage");
                        }

                    }
                }
            }

        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            currentLat = currentLat - 0.001;
            positionChanged(currentLat - 0.001, currentLon);
        }

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            currentLat = currentLat + 0.001;
            positionChanged(currentLat + 0.001, currentLon);
        }

        private void Friend_Map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image i = (Image)sender;
            Friend f = (Friend)i.Tag;

            friendOnMapTapped(f);
        }

        private void friendOnMapTapped(Friend f)
        {
            
            // TODO: ask bing api for data update

            if (friendMap.ContainsKey(f.id))
            {
                // MessageBox.Show(f.name + ", " + friendMap[f.id].Latitude + ", " + getEventLoc().Latitude + ", [" + f.transportation +"]");
                BingAPICall BAPI = new BingAPICall(friendMap[f.id], getEventLoc(), f.transportation, this, f);
                BAPI.GetData();

                MainMap.SetView((friendMap[f.id]), 16);
            }

        }

        private GeoCoordinate getEventLoc()
        {
            try
            {
                string eventloc = (string)settings["eventloc"];
                string latstr = eventloc.Split(',')[0];
                string lonstr = eventloc.Split(',')[1];

                double latd = double.Parse(latstr);
                double lond = double.Parse(lonstr);

                return new GeoCoordinate(latd, lond);
            }
            catch
            {
                return null;
            }
        }
    }
}