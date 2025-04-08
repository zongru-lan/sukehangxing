using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    public class PermissionService
    {
        public static PermissionService Service { get; private set; }

        static PermissionService()
        {
            Service = new PermissionService();
        }
        public PermissionService()
        {
            CurrentAccount = new Account();
        }

        public Account CurrentAccount { get; set; }

        public PermissionList GetPermissionList(Account account)
        {
            if (account == null) return null;
            int value = account.PermissionValue;
            var temp = new PermissionList();
            if ((value & 0x01) == 0x01)
            {
                temp.CanTraining = true;
            }
            if ((value & 0x02) == 0x02)
            {
                temp.CanChangeImageSettings = true;
            }
            if ((value & 0x04) == 0x04)
            {
                temp.CanManageDisk = true;
            }
            if ((value & 0x08) == 0x08)
            {
                temp.CanManageLog = true;
            }
            return temp;
        }

        public string GetPermissionString(Account account)
        {
            if (account == null) return string.Empty;
            int value = account.PermissionValue;
            string temp = string.Empty;
            if ((value & 0x01) == 0x01)
            {
                temp += ",CanTraining";
            }
            if ((value & 0x02) == 0x02)
            {
                temp += ",CanChangeImageSettings";
            }
            if ((value & 0x04) == 0x04)
            {
                temp += ",CanManageDisk";
            }
            if ((value & 0x08) == 0x08)
            {
                temp += ",CanManageLog";
            }
            if (temp != string.Empty)
                temp.Remove(0, 1);
            return temp;
        }

        public int GetPermissionValue(PermissionList pl)
        {
            int temp = 0;
            if (pl.CanTraining)
            {
                temp += 0x01;
            }
            if (pl.CanChangeImageSettings)
            {
                temp += 0x02;
            }
            if (pl.CanManageDisk)
            {
                temp += 0x04;
            }
            if (pl.CanManageLog)
            {
                temp += 0x08;
            }
            return temp;
        }

        public void ExportAccountList(ObservableCollection<Business.Entities.Account> ManageableAccounts, string savepath)
        {
            List<Account> accountList = new List<Account>();
            accountList = ManageableAccounts.ToList();
            //int index = 0;
            //for (index = 0; index < accountList.Count; index++)
            //{
            //    if (accountList[index].Role == AccountRole.System)
            //    {
            //        accountList.RemoveAt(index);
            //        break;
            //    }
            //}

            if (File.Exists(savepath)) File.Delete(savepath);
            ReadWriteXml.WriteXmlToFile<List<Account>>(accountList, savepath);
        }

        public bool ExportCurrentAccount(string savepath)
        {
            if (CurrentAccount != null)
            {
                if (File.Exists(savepath)) File.Delete(savepath);
                ReadWriteXml.WriteXmlToFile<Account>(CurrentAccount, savepath);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ImportAccountList(ObservableCollection<Business.Entities.Account> ManageableAccounts, string openPath)
        {
            if (!File.Exists(openPath))
                return;
            List<Account> accountList = ReadWriteXml.ReadXmlFromFile<List<Account>>(openPath);
            Db.AccountGroupDbSet accountGroupDbSet = new Db.AccountGroupDbSet();
            bool hasExit;
            foreach (var item in accountList)
            {
                hasExit = false;
                if (item.GroupName == null)
                {
                    item.GroupName = "";
                }
                //if (IsLessThanCurrentAccount(item))
                {
                    for (int i = 0; i < ManageableAccounts.Count; i++)
                    {
                        if (ManageableAccounts[i].AccountId == item.AccountId)
                        {
                            ManageableAccounts[i].Description = item.Description;
                            ManageableAccounts[i].DisplayPassword = item.DisplayPassword;
                            ManageableAccounts[i].EmployeeId = item.EmployeeId;
                            ManageableAccounts[i].IsActive = item.IsActive;
                            ManageableAccounts[i].IsEnable = item.IsEnable;
                            ManageableAccounts[i].Name = item.Name;
                            ManageableAccounts[i].Password = item.Password;
                            ManageableAccounts[i].PermissionValue = item.PermissionValue;
                            ManageableAccounts[i].Role = item.Role;
                            ManageableAccounts[i].GroupName = accountGroupDbSet.HasByName(item.GroupName) ? item.GroupName : "";
                            ManageableAccounts[i].ActionTypes = item.ActionTypes;
                            ManageableAccounts[i].EffectsCompositions= item.EffectsCompositions;
                            hasExit = true;
                            break;
                        }
                    }
                    if (!hasExit)
                    {
                        ManageableAccounts.Add(item);
                    }
                }
            }
        }

        private bool IsLessThanCurrentAccount(Account item)
        {
            switch (CurrentAccount.Role)
            {
                case AccountRole.System:
                    if (item.Role == AccountRole.System)
                    {
                        return false;
                    }
                    break;
                case AccountRole.Maintainer:
                    if (item.Role == AccountRole.Maintainer || item.Role == AccountRole.System)
                    {
                        return false;
                    }
                    break;
                case AccountRole.Admin:
                    if (item.Role == AccountRole.Maintainer || item.Role == AccountRole.System || item.Role == AccountRole.Admin)
                    {
                        return false;
                    }
                    break;
                case AccountRole.Operator:
                    return false;
                default:
                    break;
            }
            return true;
        }

        public int GetRolePermission(AccountRole role)
        {
            if (role == AccountRole.Operator)
            {
                return 0x01;
            }
            else if (role == AccountRole.Admin)
            {
                return 0x02 | 0x04 | 0x08;
            }
            return 0x00;
        }
    }

}
