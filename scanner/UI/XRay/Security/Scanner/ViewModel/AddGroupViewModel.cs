using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Windows;
using System.Windows.Input;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class AddGroupViewModel : ViewModelBase
    {
        #region Properties & Fields
        #region Commands
        public RelayCommand AddCommand { get; set; }

        public RelayCommand CancelCommand { get; set; }

        public RelayCommand KeyboardCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }
        #endregion

        #region Binding
        private bool _isAddButtonEnabled;
        public bool IsAddButtonEnabled
        {
            get { return _isAddButtonEnabled; }
            set { _isAddButtonEnabled = value; RaisePropertyChanged(); }
        }

        private string _groupID;
        public string GroupID
        {
            get { return _groupID; }
            set
            {
                _groupID = value;
                RaisePropertyChanged();
            }
        }

        private string _groupName;
        public string GroupName
        {
            get { return _groupName; }
            set
            {
                _groupName = value;
                RaisePropertyChanged();
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        private Visibility _idDuplicatedTextVisibility;
        public Visibility IdDuplicatedTextVisibility
        {
            get { return _idDuplicatedTextVisibility; }
            set { _idDuplicatedTextVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _nameDuplicatedTextVisibility;
        public Visibility NameDuplicatedTextVisibility
        {
            get { return _nameDuplicatedTextVisibility; }
            set { _nameDuplicatedTextVisibility = value; RaisePropertyChanged(); }
        }
        #endregion

        private bool isNumLocked = true;
        private bool isGroupIDValid = false;
        private bool isGroupNameValid = false;
        #endregion

        #region Constructor
        public AddGroupViewModel()
        {
            AddCommand = new RelayCommand(AddCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            KeyboardCommand = new RelayCommand(KeyboardCommandExecute);
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);

            Messenger.Default.Register<ValidationResultMessage>(this, OnValidationResultMessageReceived);

            IdDuplicatedTextVisibility = Visibility.Collapsed;
            NameDuplicatedTextVisibility = Visibility.Collapsed;
            IsAddButtonEnabled = false;
        }
        #endregion

        #region Methods
        #region Commands
        private void AddCommandExecute()
        {
            this.MessengerInstance.Send(new AccountGroupMessage(new Business.Entities.AccountGroup(GroupID, GroupName, Description)));
        }

        private void CancelCommandExecute()
        {
            this.MessengerInstance.Send(new AccountGroupMessage(null));
        }

        private void KeyboardCommandExecute()
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    if (IsAddButtonEnabled)
                    {
                        AddCommandExecute();
                    }
                    args.Handled = true;
                    break;

                case Key.F3:
                case Key.Escape:
                    CancelCommandExecute();
                    args.Handled = true;
                    break;
                case Key.System:
                    isNumLocked = true;
                    break;
                case Key.F9:
                    isNumLocked = false;
                    break;
            }

            bool isNumber = args.Key >= Key.D0 && args.Key <= Key.D9 || args.Key >= Key.NumPad0 && args.Key <= Key.NumPad9;
            bool isLetter = args.Key >= Key.A && args.Key <= Key.Z && args.KeyboardDevice.Modifiers != ModifierKeys.Shift;

            if (ScannerKeyboardPart.Keyboard.IsUSBCommonKeyboard)
            {
                if (isNumLocked)
                {
                    if (isLetter)
                    {
                        args.Handled = true;
                        return;
                    }
                }

                if (isNumber && !isNumLocked)
                {
                    ScannerKeyboardPart.Keyboard.AddKey((byte)args.Key);
                    args.Handled = true;
                    return;
                }
            }
        }
        #endregion

        private void OnValidationResultMessageReceived(ValidationResultMessage msg)
        {
            if (msg.TargetName == "GroupID")
            {
                isGroupIDValid = msg.Result.IsValid;
            }
            else if (msg.TargetName == "GroupName")
            {
                isGroupNameValid = msg.Result.IsValid;
            }
            IsAddButtonEnabled = isGroupIDValid && isGroupNameValid;
        }
        #endregion
    }
}
