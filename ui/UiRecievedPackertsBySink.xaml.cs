﻿using ORR.Dataplane;
using System.Windows;

namespace ORR.ui
{
    /// <summary>
    /// Interaction logic for UiRecievedPackertsBySink.xaml
    /// </summary>
    public partial class UiRecievedPackertsBySink : Window
    {
       
        public UiRecievedPackertsBySink()
        {
            InitializeComponent();
            dg_packets.ItemsSource = PublicParamerters.FinishedRoutedPackets;
        }
    }
    
}
