using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using RevitTimasBIMTools.RevitModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class WorkerViewModel : ObservableObject
    {
        private readonly BackgroundWorker worker;
        public static IMessenger Messenger { get; } = new WeakReferenceMessenger();

        private ICommand startCmd;
        public ICommand StartCommand
        {
            get
            {
                return startCmd ?? (startCmd = new RelayCommand(() => worker.RunWorkerAsync(), () => !worker.IsBusy));
            }
        }

        private int progress;
        public int ProgressValue
        {
            get { return progress; }
            private set { SetProperty(ref progress, value); }
        }

        private bool visibility;
        public bool ProgressVisibility
        {
            get { return visibility; }
            set { SetProperty(ref visibility, value); }
        }

        private ObservableCollection<RevitElementModel> items;
        public ObservableCollection<RevitElementModel> Items
        {
            get { return items; }
            set
            {
                if (SetProperty(ref items, value))
                {
                    OnPropertyChanged(nameof(Items));
                }
            }
        }


        public WorkerViewModel()
        {
            Items = new ObservableCollection<RevitElementModel>();
            worker = new BackgroundWorker();
            worker.DoWork += DoWork;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerCompleted += (o, e) => { ProgressVisibility = false; };
            worker.ProgressChanged += (o, e) => { ProgressValue = e.ProgressPercentage; };
            ProgressVisibility = false;
        }


        private void DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressVisibility = true;
            BackgroundWorker worker = sender as BackgroundWorker;
            for (int percent = 0; percent <= 100; percent++)
            {
                //Reports the progress
                //Simulate a long running revitTask
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        //RevitElementModel elem = new RevitElementModel();
                        //{
                        //    //IdInt = percent,
                        //    //SymbolName = "SymbolName" + percent.ToString()
                        //};
                        //Items.Add(elem);
                    }));
                Thread.Sleep(100);
                worker.ReportProgress(percent);
            }
            ProgressVisibility = false;
        }
    }
}
