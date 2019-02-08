using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MarketMaker_Api_Tests.CryptoCortex.Models
{
    public enum OrderType
    {
        MARKET,
        LIMIT
    }

    public class OrderCrypto
    {
        internal class EnumConverter : StringEnumConverter
        {
            public EnumConverter() : base(new SnakeCaseNamingStrategy(), false)
            {
            }
        }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(EnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(EnumConverter))]
        public OrderType Type { get; set; }

        [JsonProperty("security_id")]
        public String SecurityId { get; set; }

        [JsonProperty("destination")]
        public String Destination { get; set; }
    }
}
