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
    public class Order
    {
        [DataMember] public string Book { get; set; }
        [DataMember] public OrderType OrderType { get; set; }
        [DataMember] public OrderSide OrderSide { get; set; }
        [DataMember] public double Price { get; set; }
        [DataMember] public uint Volume { get; set; }
        [DataMember] public uint UserID { get; set; }
        [DataMember] public uint OrderID { get; set; }

        public Order(string book, OrderType orderType, OrderSide orderSide, double price, uint volume, uint userId, uint orderId)
        {
            Book = book;
            OrderType = orderType;
            OrderSide = orderSide;
            Price = price;
            Volume = volume;
            UserID = userId;
            OrderID = orderId;
        }
    }


}
