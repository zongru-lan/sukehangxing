namespace UI.XRay.Gui.Framework
{
    /// <summary>
    /// Metro对话框的执行结果：分别表示用户点击了对应的处理按键
    /// </summary>
    public enum MetroDialogResult
    {
        Ok,
        No,
        Cancel
    }

    /// <summary>
    /// 输入对话框的结果：包括用户操作的结果，以及
    /// </summary>
    public class InputMetroDialogResult
    {
        public InputMetroDialogResult(MetroDialogResult result, string input = null)
        {
            DialogResult = result;
            Input = input;
        }

        public MetroDialogResult DialogResult { get; private set; }

        public string Input { get; private set; }
    }

    /// <summary>
    /// 输入对话框的结果：包括用户操作的结果，以及
    /// </summary>
    public class PasswordMetroDialogResult
    {
        public PasswordMetroDialogResult(MetroDialogResult result, string password = null)
        {
            DialogResult = result;
            Password = password;
        }

        public MetroDialogResult DialogResult { get; private set; }

        public string Password { get; private set; }
    }
}
