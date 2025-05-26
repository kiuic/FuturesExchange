using System;
using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class CityTemperature
    {
        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "temperature")]
        public double Temperature { get; set; }

        public CityTemperature(string city, double temperature)
        {
            City = city;
            Temperature = temperature;
        }

        public override string ToString()
        {
            return $"CityTemperature [City={City}, Temperature={Temperature}]";
        }

    }
}