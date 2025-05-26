using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public enum OrderStatus
    {
        [EnumMember]
        ACTIVE = 0,
        [EnumMember]
        FILLED = 1,
        [EnumMember]
        CANCELLED = 2,
        [EnumMember]
        ERROR = 3,
    }
}
