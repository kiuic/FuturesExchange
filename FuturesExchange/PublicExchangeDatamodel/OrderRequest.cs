using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class OrderRequest
    {
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

        [DataMember(Name = "userID")]
        public uint UserID { get; set; }

        public OrderRequest(string book, OrderType orderType, OrderSide orderSide, double price, uint volume, uint userID)
        {
            Book = book;
            OrderType = orderType;
            OrderSide = orderSide;
            Price = price;
            Volume = volume;
            UserID = userID;
        }

        public override string ToString()
        {
            return $"OrderRequest [Book={Book}, OrderType={OrderType}, OrderSide={OrderSide}, Price={Price}, Volume={Volume}, UserID={UserID}]";
        }

    }
}