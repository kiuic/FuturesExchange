using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternalExchangeDatamodel;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using PublicExchangeDatamodel;

namespace ExchangeState
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class ExchangeState : StatefulService, IExchangeState
    {

        private static readonly string ORDER_BOOK_SNAPSHOTS = "ORDER_BOOK_SNAPSHOTS";

        public ExchangeState(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<OrderBookSnapshot> GetOrderBookSnapshot(string bookName)
        {
            using(var tx = this.StateManager.CreateTransaction())
            {
                var orderBookSnapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<String, OrderBookSnapshot>>(ORDER_BOOK_SNAPSHOTS);
                return await orderBookSnapshots.GetOrAddAsync(tx, bookName, new OrderBookSnapshot(bookName, [], []));
            }
        }

        public async Task<List<string>> GetTradableCities()
        {
            var tradableCities = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings.Sections["TradableCities"].Parameters["TradableCities"].Value;
            return tradableCities.Split(';').ToList();
        }

        public async Task RegisterOrderBookSnapshot(OrderBookSnapshot orderBookSnapshot)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                var orderBookSnapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<String, OrderBookSnapshot>>(ORDER_BOOK_SNAPSHOTS);
                await orderBookSnapshots.SetAsync(tx, orderBookSnapshot.Book, orderBookSnapshot);
                await tx.CommitAsync();
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}
