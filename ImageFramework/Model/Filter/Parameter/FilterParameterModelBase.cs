﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ImageFramework.Annotations;

namespace ImageFramework.Model.Filter.Parameter
{
    /// <summary>
    /// filter parameter information which is not dependent from the parameter type
    /// </summary>
    public abstract class FilterParameterModelBase : INotifyPropertyChanged
    {
        public FilterParameterModelBase(string name, string variableName)
        {
            Name = name;
            VariableName = variableName;
        }

        /// name for the filter menu
        public string Name { get; }
        /// name within the shader
        public string VariableName { get; }

        public abstract string StringValue { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
