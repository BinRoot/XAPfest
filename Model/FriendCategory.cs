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
    public class FriendCategory : System.Collections.ObjectModel.ObservableCollection<Friend>
    {
        public string Name { get; private set; }
        public bool Empty { get; set; }

        public FriendCategory(string categoryName)
        {
            Name = categoryName;
            Empty = true;
        }

        public void AddFoodItem(Friend foodItem)
        {
            Items.Add(foodItem);
            Empty = false;
        }

    }
}
