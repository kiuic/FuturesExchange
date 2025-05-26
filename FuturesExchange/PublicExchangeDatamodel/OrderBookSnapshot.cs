using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class OrderBookSnapshot
    {
        [DataMember(Name = "book")]
        public string Book { get; set; }

        [DataMember(Name = "buyOrdersPriceToVolume")]
        public SortedDictionary<double, uint> BuyOrdersPriceToVolume { get; set; } = new SortedDictionary<double, uint>();

        [DataMember(Name = "sellOrdersPriceToVolume")]
        public SortedDictionary<double, uint> SellOrdersPriceToVolume { get; set; } = new SortedDictionary<double, uint>();

        public OrderBookSnapshot(
            string book,
            SortedDictionary<double, uint> buyOrdersPriceToVolume,
            SortedDictionary<double, uint> sellOrdersPriceToVolume)
        {
            Book = book;
            BuyOrdersPriceToVolume = buyOrdersPriceToVolume;
            SellOrdersPriceToVolume = sellOrdersPriceToVolume;
        }

        public override string ToString()
        {
            string buyOrders = string.Join(", ", BuyOrdersPriceToVolume.Select(kv => $"{kv.Key}->{kv.Value}"));
            string sellOrders = string.Join(", ", SellOrdersPriceToVolume.Select(kv => $"{kv.Key}->{kv.Value}"));
            return $"{Book}, Bids:[{buyOrders}], Asks:[{sellOrders}]";
        }
        
    }
}