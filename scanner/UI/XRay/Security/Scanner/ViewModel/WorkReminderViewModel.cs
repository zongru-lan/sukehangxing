using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.SettingViews;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class WorkReminderViewModel
    {
        public RelayCommand ClosedCommand { get; set; }

        public string WorkReminderCount { get; set; }

        public double WorkReminderHour { get; set; }

        public WorkReminderWindow WorkReminderWindow { get; set; }

        public WorkReminderViewModel(WorkReminderWindow window)
        {
            ClosedCommand = new RelayCommand(ClosedCommandExecute);
            WorkReminderWindow = window;
        }

        private void ClosedCommandExecute()
        {
            Messenger.Default.Send<CloseWindowMessage>(new CloseWindowMessage("WorkReminderWindow"));
        }

    }
}