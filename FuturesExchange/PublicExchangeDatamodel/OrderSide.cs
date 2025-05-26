using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public enum OrderSide
    {
        [EnumMember]
        BUY = 0,
        [EnumMember]
        SELL = 1
    }
}
