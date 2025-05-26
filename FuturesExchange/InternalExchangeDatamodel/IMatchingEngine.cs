using Microsoft.ServiceFabric.Services.Remoting;
using PublicExchangeDatamodel;

namespace InternalExchangeDatamodel
{
    public interface IMatchingEngine : IService
    {
        Task<MatchingEngineResponse> SendOrder(Order order);
        Task CancelOrder(CancelOrderRequest cancelOrderRequest);
    }
}
