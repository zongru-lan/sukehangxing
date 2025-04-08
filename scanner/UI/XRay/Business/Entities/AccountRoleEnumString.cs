namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// ���ڰ󶨵�ComboBox���û����ͼ������ֱ�ʾ����
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