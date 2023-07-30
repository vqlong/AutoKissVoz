using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XLib.UserControls;

namespace XLib.Resources
{
    public partial class WindowStyle : ResourceDictionary
    {
        public WindowStyle()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var obj = ((FrameworkElement)sender).TemplatedParent;

            if(obj is DialogBox dialog)
            {
                dialog.Close();
            }
            else if (obj is Window window)
            {
                if (DialogBox.Show("Bạn có thực sự muốn thoát?", "Thông báo", DialogBoxButton.YesNo, DialogBoxIcon.Question) == DialogBoxResult.No) return;
                else window.Close();
            }
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {

            var window = (Window)((FrameworkElement)sender).TemplatedParent;

            //if (window.ResizeMode == ResizeMode.CanMinimize) return;

            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                //((Button)sender).Content = "🗖";
            }
            else if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
                //((Button)sender).Content = "🗗"; 
            }
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = WindowState.Minimized;
        }
    }
}
