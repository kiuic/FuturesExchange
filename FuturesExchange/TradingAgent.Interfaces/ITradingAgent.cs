using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternalExchangeDatamodel;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;
using PublicExchangeDatamodel;

[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace TradingAgent.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ITradingAgent : IActor
    {
        Task<OrderStatusResponse> SendOrderRequest(OrderRequest orderRequest, CancellationToken cancellationToken);
        Task SendCancelOrderRequest(CancelOrderRequest cancelOrderRequest, CancellationToken cancellationToken);
        Task SendCancelAllOrders(string book, CancellationToken cancellationToken);
        Task<PnlResult> GetPnl(string book, CancellationToken cancellationToken);
        Task<HashSet<OrderStatusResponse>> GetAllOrders(CancellationToken cancellationToken);
        Task RegisterTrade(Trade trade, CancellationToken cancellationToken);
        Task RegisterOrderCancellation(uint orderID, CancellationToken cancellationToken);
    }
}
