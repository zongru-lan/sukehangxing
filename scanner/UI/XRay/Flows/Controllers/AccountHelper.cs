using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    public class AccountHelper
    {
        /// <summary>
        /// 获取所有的用户角色字符串，其中包括null值，表示所有用户角色
        /// </summary>
        /// <returns></returns>
        public static List<ValueStringItem<AccountRole?>> GetAccountRoleStringList()
        {
            var list = new List<ValueStringItem<AccountRole?>>();
            if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.System ) { 
                list.Add(new ValueStringItem<AccountRole?>(null,""));
            }
            foreach (var value in Enum.GetValues(typeof(AccountRole)))
            {
                var role = (AccountRole) value;
                if(LoginAccountManager.Service.IsEqualOrLessThanCurrentRole(role))
                {
                    list.Add(new ValueStringItem<AccountRole?>(role, TranslationService.FindTranslation(role)));
                }                
            }

            return list;
        }
    }
}
