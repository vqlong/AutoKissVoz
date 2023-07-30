using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace P3_Login_Voz
{
    /// <summary>
    /// Interaction logic for EnterCode.xaml
    /// </summary>
    public partial class EnterCode : Window
    {
        public EnterCode()
        {
            InitializeComponent();
        }
        public string Code { get; set; } = "";
        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            Code = txbCode.Text;
            Close();
        }

        public static string Show()
        {
            var window = new EnterCode();
            window.ShowDialog();
            return window.Code;
        }
    }
}
