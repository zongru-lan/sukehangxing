using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class ManageGroupsPageViewModel : PageViewModelBase
    {
        #region Properties & Fields
        #region Commands
        public RelayCommand CellEditEndingEventCommand { get; set; }

        public RelayCommand AddNewGroupCommand { get; set; }

        public RelayCommand DeleteGroupCommand { get; set; }

        public RelayCommand SaveChangesCommand { get; set; }

        public RelayCommand DiscardChangesCommand { get; set; }

        public RelayCommand ExportCommand { get; set; }

        public RelayCommand ExportToNetCommand { get; set; }

        public RelayCommand ImportCommand { get; set; }
        #endregion

        #region Binding
        private ObservableCollection<AccountGroup> _manageableGroups;
        public ObservableCollection<AccountGroup> ManageableGroups
        {
            get { return _manageableGroups; }
            set
            {
                _manageableGroups = value;
                RaisePropertyChanged();
            }
        }

        private AccountGroup _currentGroup;
        public AccountGroup CurrentGroup
        {
            get { return _currentGroup; }
            set
            {
                _currentGroup = value;
                RaisePropertyChanged();
            }
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get { return _hasChanges; }
            set
            {
                _hasChanges = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region
        private AccountGroupDbSet groupDbManager;

        /// <summary>修改之前的组信息</summary>
        private List<AccountGroup> uneditedGroups = new List<AccountGroup>();
        #endregion
        #endregion

        #region Constructor
        public ManageGroupsPageViewModel()
        {
            groupDbManager = new AccountGroupDbSet();
            CellEditEndingEventCommand = new RelayCommand(CellEditEndingEventCommandExecute);
            AddNewGroupCommand = new RelayCommand(AddNewGroupCommandExecute);
            DeleteGroupCommand = new RelayCommand(DeleteGroupCommandExecute);
            SaveChangesCommand = new RelayCommand(SaveChangesCommandExecute);
            DiscardChangesCommand = new RelayCommand(DiscardChangesCommandExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);
            ExportToNetCommand = new RelayCommand(ExportToNetCommandExecute);
            ImportCommand = new RelayCommand(ImportCommandExecute);
            Init();
        }
        #endregion

        #region Methods
        private void Init()
        {
            ManageableGroups = new ObservableCollection<AccountGroup>(groupDbManager.SelectAll());
            GetCopyOfUneditedGroups(ManageableGroups, ref uneditedGroups);
            HasChanges = false;
        }

        private void GetCopyOfUneditedGroups(IEnumerable<AccountGroup> groups, ref List<AccountGroup> dstGroups)
        {
            foreach (var group in groups)
            {
                dstGroups.Add(new AccountGroup(group.GroupID, group.GroupName, group.Description));
            }
        }

        #region Commands
        private void CellEditEndingEventCommandExecute()
        {
            HasChanges = true;
        }

        private void AddNewGroupCommandExecute()
        {
            var msg = new ShowAddGroupWindowAction("SettingWindow", group1 =>
            {
                if (group1 != null)
                {
                    string GroupIDTranslation =TranslationService.FindTranslation("GroupInfo", "Group ID");
                    string GroupNameTranslation = TranslationService.FindTranslation("GroupInfo", "Group Name");

                    ManageableGroups.Add(group1);
                    // 直接提交
                    groupDbManager.AddOrUpdate(group1);

                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.ManageGroups,
                        OperateTime = DateTime.Now,
                        OperateObject = "Group",
                        OperateCommand = OperationCommand.Create,
                        OperateContent = $"{GroupIDTranslation}:{group1.GroupID},{GroupNameTranslation}:{group1.GroupName},Description:{group1.Description}",
                    });
                }
            });

            Messenger.Default.Send(msg);

        }

        private void DeleteGroupCommandExecute()
        {
            try
            {
                // 移除组成员
                var accountDbManager = new AccountDbSet();
                var groupMembers = accountDbManager.SelectAccountsByGroup(CurrentGroup.GroupName);
                if (groupMembers != null && groupMembers.Count > 0)
                {
                    groupMembers.ForEach(account => account.GroupName = "");
                    accountDbManager.AddOrUpdate(groupMembers);
                }
                var groupToBeRemoved = CurrentGroup;

                // 直接提交到数据库
                groupDbManager.Remove(groupToBeRemoved);
                // 更新界面，需要先更新数据库，因为更新界面后会同步将groupToBeRemoved更新为null
                ManageableGroups.Remove(groupToBeRemoved);

                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                    OperateUI = OperationUI.ManageGroups,
                    OperateTime = DateTime.Now,
                    OperateObject = "Group",
                    OperateCommand = OperationCommand.Delete,
                    OperateContent = $"GroupID:{groupToBeRemoved.GroupID},GroupName:{groupToBeRemoved.GroupName},Description:{groupToBeRemoved.Description}",
                });
            }
            catch (Exception ex)
            {
                Tracer.TraceException(ex);
            }
        }

        private void SaveChangesCommandExecute()
        {
            groupDbManager.AddOrUpdate(ManageableGroups);

            Init();

            #region OperationRecord
            foreach (var group in ManageableGroups)
            {
                var uneditedGroup = uneditedGroups.Find(g => g.GroupID == group.GroupID);
                string content = "";
                bool modified = false;
                if (group.GroupName != uneditedGroup.GroupName)
                {
                    content += $"GroupName:{uneditedGroup.GroupName}-{group.GroupName}";
                    modified = true;
                }
                if (group.Description != uneditedGroup.Description)
                {
                    content += modified ? " " : "" + $"Description:{uneditedGroup.Description}-{group.Description}";
                    modified = true;
                }
                if (modified)
                {
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.ManageGroups,
                        OperateTime = DateTime.Now,
                        OperateObject = group.GroupID,
                        OperateCommand = OperationCommand.Setting,
                        OperateContent = content,
                    });
                }
            }
            #endregion
        }

        private void DiscardChangesCommandExecute()
        {
            Init();
        }

        private void ExportCommandExecute()
        {
            Init();
            string filename = null;
            SaveFileDialog s = new SaveFileDialog
            {
                Filter = TranslationService.FindTranslation("xml files") + "|*.xml",
                Title = TranslationService.FindTranslation("Export"),
                OverwritePrompt = true,
                AddExtension = true,
                FileName = ConfigHelper.GetExportFileName("", "Groups"),
            };
            if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = s.FileName;
                //if (File.Exists(filename)) File.Delete(filename);
                ReadWriteXml.WriteXmlToFile<List<AccountGroup>>(ManageableGroups.ToList(), filename);
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                        "", TranslationService.FindTranslation("Dump completely"), MetroDialogButtons.Ok,
                        result =>
                        {

                        }));
                });
                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                    OperateUI = OperationUI.ManageGroups,
                    OperateTime = DateTime.Now,
                    OperateObject = "Group",
                    OperateCommand = OperationCommand.Export,
                    OperateContent = ConfigHelper.AddQuotationForPath(filename),
                });
            }
        }

        private void ExportToNetCommandExecute()
        {

        }

        private void ImportCommandExecute()
        {
            var msg = new ShowOpenFilesDialogMessageAction("SettingWindow", "Xml file | *.xml", false, s =>
            {
                if (s != null && s.Length > 0)
                {
                    var path = s[0];
                    Init();
                    if (!File.Exists(path))
                    {
                        return;
                    }
                    List<AccountGroup> groups = ReadWriteXml.ReadXmlFromFile<List<AccountGroup>>(path);
                    var groupNameList = GroupInfoProvider.GetGroupNames();
                    bool isNewGroup;
                    foreach (var group in groups)
                    {
                        // 如果出现同Name的情况，则不导入
                        if (groupNameList.Contains(group.GroupName))
                        {
                            continue;
                        }
                        isNewGroup = true;
                        for (int i = 0; i < ManageableGroups.Count(); i++)
                        {
                            if (ManageableGroups[i].GroupID == group.GroupID)
                            {
                                isNewGroup = false;
                                ManageableGroups[i] = group;
                                break;
                            }
                        }
                        if (isNewGroup)
                        {
                            ManageableGroups.Add(group);
                        }
                    }
                }
            });
            MessengerInstance.Send(msg);
            HasChanges = true;
        }
        #endregion

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            // TODO:
        }
        #endregion
    }
}
