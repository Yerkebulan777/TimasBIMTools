using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using RevitTimasBIMTools.RevitModel;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public readonly IList<RevitElementModel> FamilySymbolCollection = null;
        public SettingsControl(IList<RevitElementModel> models)
        {
            FamilySymbolCollection = models;
            InitializeComponent();
        }

    }
}
