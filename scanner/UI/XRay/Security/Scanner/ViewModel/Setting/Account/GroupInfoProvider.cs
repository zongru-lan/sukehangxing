using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    static class GroupInfoProvider
    {
        private static AccountGroupDbSet accountGroupManager;

        static GroupInfoProvider()
        {
            accountGroupManager = new AccountGroupDbSet();
        }

        public static List<string> GetGroupNames()
        {
            var result = accountGroupManager.SelectGroupNames();
            result.Insert(0, "");
            return result;
        }

        public static List<Business.Entities.AccountGroup> GetAllGroups()
        {
            return accountGroupManager.SelectAll();
        }

        public static bool IsGroupIDExist(string groupID)
        {
            return accountGroupManager.Has(groupID);
        }

        public static bool IsGroupNameExist(string groupName)
        {
            return accountGroupManager.HasByName(groupName);
        }
    }
}
