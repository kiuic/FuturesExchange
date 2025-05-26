using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class CreateTradingClientRequest
    {
        [DataMember(Name = "userID")]
        public uint UserID { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        public CreateTradingClientRequest(uint userID, string type)
        {
            UserID = userID;
            Type = type;
        }
    }
}
