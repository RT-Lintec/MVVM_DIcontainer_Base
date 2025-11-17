namespace MVVM_Base.Model
{
    public interface IMessageService
    {
        void ShowMessage(string message);
        void CloseWithFade();
    }
}
