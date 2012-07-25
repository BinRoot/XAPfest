// ----------------------------------------------------------
// <copyright file="AddItem.cs" company="Microsoft">
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

    using Microsoft.Phone.Controls;
    using Microsoft.Phone.UserData;

    /// <summary>
    /// 
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendText_Click(object sender, RoutedEventArgs e)
        {
            SendText(DateTime.Now.ToLongTimeString() + " " + TextEntry.Text);
        }

        private void SendText(string textStr)
        {
            if (this.chat != null)
            {
                ISmashTableChangeContext context = this.chat.GetTableChangeContext();
                context.Add(new Channels.ChatRecord(textStr));
                context.SaveChangesCompleted += new SaveChangesCompletedHandler(this.Context_SaveChangesCompleted);
                context.SaveChangesAsync(null);
                TextEntry.Text = string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Context_SaveChangesCompleted(object sender, SaveChangesCompletedArgs e)
        {
            if (e.Error != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(e.Error.ToString());
                }));
            }
        }
    }
}