﻿using System.Windows;

namespace ORR.ui
{
    /// <summary>
    /// Interaction logic for UiShowLists.xaml
    /// </summary>
    public partial class UiShowLists : Window
    {
        public UiShowLists()
        {
            InitializeComponent();
            stack_items.MaxHeight= SystemParameters.FullPrimaryScreenHeight;
        }
    }
}
