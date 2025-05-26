using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PublicExchangeDatamodel;

namespace InternalExchangeDatamodel
{
    [DataContract]
    public class MatchingEngineResponse
    {
        [DataMember] public Order Order { get; set; }
        [DataMember] public OrderStatus OrderStatus { get; set; }

        public MatchingEngineResponse(Order order, OrderStatus orderStatus)
        { 
            Order = order;
            OrderStatus = orderStatus;
        }
    }

}
