using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MarketMaker_Api_Tests.CryptoCortex.Models
{
    public class Event
    {
        internal class EnumConverter : StringEnumConverter
        {
            public EnumConverter() : base(new SnakeCaseNamingStrategy(), false)
            {
            }
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("properties")]
        public string Properties { get; set; }

        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        [JsonProperty("security_id")]
        public string SecurityId { get; set; }

        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; set; }

        [JsonProperty("cumulative_quantity")]
        public double CumulativeQuantity { get; set; }

        [JsonProperty("remaining_quantity")]
        public double RemainingQuantity { get; set; }

        [JsonProperty("average_price")]
        public double AveragePrice { get; set; }

        [JsonProperty("price")]
        public double? Price { get; set; }

        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        [JsonProperty("expire_time")]
        public string ExpireTime { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("time_in_force")]
        [JsonConverter(typeof(EnumConverter))]
        public TimeInForce? TimeInForce { get; set; }

        [JsonProperty("order_side")]
        [JsonConverter(typeof(EnumConverter))]
        public Side OrderSide { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("order_type")]
        [JsonConverter(typeof(EnumConverter))]
        public OrderType OrderType { get; set; }

        [JsonProperty("order_status")]
        [JsonConverter(typeof(EnumConverter))]
        public Status OrderStatus { get; set; }

        [JsonProperty("original_client_order_id")]
        public string OriginalClientOrderId { get; set; }

        [JsonProperty("trade_quantity")]
        public double? TradeQuantity { get; set; }

        [JsonProperty("trade_price")]
        public double? TradePrice { get; set; }

        [JsonProperty("is_agressor")]
        public string IsAgressor { get; set; }
    }
}
