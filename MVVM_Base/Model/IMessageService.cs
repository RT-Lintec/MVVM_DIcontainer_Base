namespace MVVM_Base.Model
{
    public interface IMessageService
    {
        Task ShowMessage(string message);
        Task CloseWithFade();
    }
}
