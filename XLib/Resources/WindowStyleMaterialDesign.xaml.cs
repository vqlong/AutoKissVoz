using MaterialDesignThemes.Wpf;
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
    public partial class WindowStyleMaterialDesign : ResourceDictionary
    {
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var obj = ((FrameworkElement)sender).TemplatedParent;

            if (obj is DialogBox dialog)
            {
                dialog.Close();
            }
            else if (obj is Window window)
            {
                if (DialogBox.Show("Bạn có thực sự muốn thoát?", "Thông báo", DialogBoxButton.YesNo, DialogBoxIcon.Question) == DialogBoxResult.Yes) window.Close();
            }
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                if (sender is Button button) button.Content = new PackIcon { Kind = PackIconKind.CheckboxBlankOutline, Width = 20 };
            }
            else if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
                if (sender is Button button) button.Content = new PackIcon { Kind = PackIconKind.CheckboxMultipleBlankOutline, Width = 20 };
            }
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Window window) window.DragMove();
        }
    }
}
