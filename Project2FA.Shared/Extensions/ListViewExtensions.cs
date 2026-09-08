using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
#if WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
#endif

namespace Project2FA.Extensions
{
    public static class ListViewExtensions
    {
        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(IList),
                typeof(ListViewExtensions),
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        public static void SetBindableSelectedItems(DependencyObject obj, IList value)
            => obj.SetValue(BindableSelectedItemsProperty, value);

        public static IList GetBindableSelectedItems(DependencyObject obj)
            => (IList)obj.GetValue(BindableSelectedItemsProperty);

        private static void OnBindableSelectedItemsChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = d as ListView;
            listView.SelectionChanged += (s, args) =>
            {
                var boundList = GetBindableSelectedItems(listView);
                boundList.Clear();
                foreach (var item in listView.SelectedItems)
                    boundList.Add(item);
            };
        }
    }

}
