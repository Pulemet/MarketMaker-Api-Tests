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
        LIMIT,
        MARKET
    }

    public enum TimeInForce
    {
        GTD,
        DAY,
        GTC,
        FOK,
        IOC
    }

    public class OrderCrypto
    {
        internal class EnumConverter : StringEnumConverter
        {
            public EnumConverter() : base(new SnakeCaseNamingStrategy(), false)
            {
            }
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
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
        public string SecurityId { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("time_in_force")]
        [JsonConverter(typeof(EnumConverter))]
        public TimeInForce? TimeInForce { get; set; }

        [JsonProperty("price")]
        public double? Price { get; set; }

        [JsonProperty("expire_time")]
        public long? ExpireTime { get; set; }
    }
}
