using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 用户账户类型
    /// </summary>
    [Serializable]
    public enum AccountRole
    {
        System = 0,
        Maintainer = 1,
        Admin = 2,
        Operator = 3
    }
}
