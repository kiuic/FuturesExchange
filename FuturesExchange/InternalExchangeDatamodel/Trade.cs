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
    public class Trade
    {
        [DataMember]
        public string Book { get; set; }
        [DataMember]
        public uint BuyUserID { get; set; }

        [DataMember]
        public uint BuyOrderID { get; set; }

        [DataMember]
        public uint SellUserID { get; set; }

        [DataMember]
        public uint SellOrderID { get; set; }

        [DataMember]
        public double Price { get; set; }

        [DataMember]
        public uint Volume { get; set; }

        public Trade(string book, uint buyUserID, uint buyOrderID, uint sellUserID, uint sellOrderID, double price, uint volume)
        {
            Book = book;
            BuyUserID = buyUserID;
            BuyOrderID = buyOrderID;
            SellUserID = sellUserID;
            SellOrderID = sellOrderID;
            Price = price;
            Volume = volume;
        }
    }

}
