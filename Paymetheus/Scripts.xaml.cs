﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Paymetheus
{
    /// <summary>
    /// Interaction logic for Scripts.xaml
    /// </summary>
    public partial class Scripts : Page
    {
        public Scripts()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var dataContext = this.DataContext;
            if (dataContext != null)
            {
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
