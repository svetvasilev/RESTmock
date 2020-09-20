namespace RESTMock.Core
{
    public interface IOperationRequestReceivedHandler
    {
        void RequestReceived(object sender, HttpContextArgs args);
    }
}