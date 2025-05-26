using System;
using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class OrderStatusResponse
    {
        [DataMember(Name = "orderStatus")]
        public OrderStatus OrderStatus { get; set; }

        [DataMember(Name = "userID")]
        public uint UserID { get; set; }

        [DataMember(Name = "orderID")]
        public uint OrderID { get; set; }

        [DataMember(Name = "book")]
        public string Book { get; set; }

        [DataMember(Name = "orderType")]
        public OrderType OrderType { get; set; }

        [DataMember(Name = "orderSide")]
        public OrderSide OrderSide { get; set; }

        [DataMember(Name = "price")]
        public double Price { get; set; }

        [DataMember(Name = "volume")]
        public uint Volume { get; set; }

        [DataMember(Name = "filledVolume")]
        public uint FilledVolume { get; set; }

        [DataMember(Name = "averageFilledPrice")]
        public double AverageFilledPrice { get; set; }

        public OrderStatusResponse(
            OrderStatus orderStatus,
            uint userID,
            uint orderID,
            string book,
            OrderType orderType,
            OrderSide orderSide,
            double price,
            uint volume,
            uint filledVolume,
            double averageFilledPrice)
        {
            OrderStatus = orderStatus;
            UserID = userID;
            OrderID = orderID;
            Book = book;
            OrderType = orderType;
            OrderSide = orderSide;
            Price = price;
            Volume = volume;
            FilledVolume = filledVolume;
            AverageFilledPrice = averageFilledPrice;
        }

        public override string ToString()
        {
            return $"OrderStatusResponse [OrderStatus={OrderStatus}, UserID={UserID}, OrderID={OrderID}, Book={Book}, OrderType={OrderType}, OrderSide={OrderSide}, Price={Price}, Volume={Volume}, FilledVolume={FilledVolume}, AverageFilledPrice={AverageFilledPrice}]";
        }

    }
}