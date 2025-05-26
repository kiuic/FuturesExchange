using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using PublicExchangeDatamodel;

namespace InternalExchangeDatamodel
{
    public interface IExchangeState : IService
    {
        public Task<OrderBookSnapshot> GetOrderBookSnapshot(string bookName);
        public Task RegisterOrderBookSnapshot(OrderBookSnapshot orderBookSnapshot);
        public Task<List<string>> GetTradableCities();
    }
}
