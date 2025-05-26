using System;
using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class CancelOrderRequest
    {
        [DataMember(Name = "book")]
        public string Book { get; set; }

        [DataMember(Name = "userID")]
        public uint UserID { get; set; }

        [DataMember(Name = "orderID")]
        public uint OrderID { get; set; }

        public CancelOrderRequest(string book, uint userID, uint orderID)
        {
            Book = book;
            UserID = userID;
            OrderID = orderID;
        }

        public override string ToString()
        {
            return $"CancelOrderRequest [OrderID={OrderID}, Book={Book}, UserID={UserID}]";
        }

    }
}