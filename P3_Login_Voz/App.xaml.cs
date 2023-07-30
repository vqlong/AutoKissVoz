using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using XLib.UserControls;

namespace P3_Login_Voz
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var error = "Exception: " + e.Exception.Message;
            if (e.Exception.InnerException != null) error += "\nInnerException: " + e.Exception.InnerException.Message;
            DialogBox.Show(error, "AutoKissVoz");
            e.Handled = true;
        }
    }
}
