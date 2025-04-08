namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 用于绑定到ComboBox的用户类型及其文字表示对象
    /// </summary>
    public class AccountRoleEnumString
    {
        public string Translation { get; set; }

        public AccountRole Role { get; set; }

        public AccountRoleEnumString(AccountRole role, string str)
        {
            Role = role;
            Translation = str;
        }
    }
}