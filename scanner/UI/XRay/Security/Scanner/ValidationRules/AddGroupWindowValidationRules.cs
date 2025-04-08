using GalaSoft.MvvmLight.Messaging;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;

namespace UI.XRay.Security.Scanner.ValidationRules
{
    public class GroupIDValidRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var groupID = value as string;
            string pattern = @"^[0-9]{1,6}$";
            ValidationResult result;
            if (groupID == null)
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group ID cannot be empty"));
            }
            else if (GroupInfoProvider.IsGroupIDExist(groupID))
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group ID already exists"));
            }
            else if (Regex.IsMatch(groupID, pattern))
            {
                result = new ValidationResult(true, null);
            }
            else
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group ID can only contain 1-6 numbers"));
            }

            Messenger.Default.Send(new ValidationResultMessage("GroupID", result));
            return result;
        }
    }

    public class GroupNameValidRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var groupName = value as string;
            ValidationResult result;
            if (groupName == null)
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group Name cannot be empty"));
            }
            else if (GroupInfoProvider.IsGroupNameExist(groupName))
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group Name already exists"));
            }
            else if (groupName.Length >= 20 || groupName.Length < 1)
            {
                result = new ValidationResult(false, TranslationService.FindTranslation("GroupInfo", "Group Name can only contain 1-20 characters"));
            }
            else
            {
                result = new ValidationResult(true, null);
            }

            Messenger.Default.Send(new ValidationResultMessage("GroupName", result));
            return result;
        }
    }
}
