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

namespace ImageViewer.Views.Filter
{
    /// <summary>
    /// Interaction logic for TextureFilterParameterView.xaml
    /// </summary>
    public partial class TextureFilterParameterView : UserControl
    {
        public TextureFilterParameterView(Binding  enabledBinding)
        {
            InitializeComponent();

            BindingOperations.SetBinding(this, IsEnabledProperty, enabledBinding);
        }
    }
}
