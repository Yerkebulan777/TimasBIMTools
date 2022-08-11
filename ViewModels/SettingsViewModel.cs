using Microsoft.Toolkit.Mvvm.ComponentModel;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.ObjectModel;

namespace RevitTimasBIMTools.ViewModels
{
    public class SettingsViewModel : ObservableObject, IDisposable
    {
        private ObservableCollection<RevitElementModel> simbolList = new ObservableCollection<RevitElementModel>();
        public ObservableCollection<RevitElementModel> SimbolList
        {
            get => simbolList;
            set
            {
                SetProperty(ref simbolList, value);
            }
        }


        private RevitElementModel rectangSymbolModel = null;
        public RevitElementModel RectangSimbolModel
        {
            get => rectangSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref rectangSymbolModel, value);
                    if (rectangSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RectangOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        LogManager.Info(model.SymbolName);
                    }
                }
            }
        }


        private RevitElementModel roundSymbolModel = null;
        public RevitElementModel RoundSimbolModel
        {
            get => roundSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref roundSymbolModel, value);
                    if (roundSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RoundOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        LogManager.Info(model.SymbolName);
                    }
                }
            }
        }


        private int minElementHight = 50;
        public int MinElementHight
        {
            get => minElementHight;
            set
            {
                if (value != minElementHight)
                {
                    minElementHight = NormilizeIntValue(value);
                    OnPropertyChanged(nameof(MinElementHight));
                }
            }
        }

        private static int NormilizeIntValue(int value)
        {
            if (value < 0)
            {
                value = 0;
            }
            if (value > 100)
            {
                value = 100;
            }
            return value;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
