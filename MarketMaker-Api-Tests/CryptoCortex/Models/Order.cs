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
    public enum Status
    {
        PENDING_NEW,
        NEW,
        COMPLETELY_FILLED,
        CANCELED,
        PARTIALLY_FILLED
    }
    public class Order
    {
        internal class EnumConverter : StringEnumConverter
        {
            public EnumConverter() : base(new SnakeCaseNamingStrategy(), false)
            {
            }
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(EnumConverter))]
        public Status Status { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(EnumConverter))]
        public OrderType Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(EnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("properties")]
        public string Properties { get; set; }

        [JsonProperty("cumulative_quantity")]
        public double CumulativeQuantity { get; set; }

        [JsonProperty("remaining_quantity")]
        public double RemainingQuantity { get; set; }

        [JsonProperty("average_price")]
        public double AveragePrice { get; set; }

        [JsonProperty("receipt_time")]
        public long ReceiptTime { get; set; }

        [JsonProperty("security_id")]
        public string SecurityId { get; set; }

        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; set; }

        [JsonProperty("price")]
        public double? Price { get; set; }

        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        [JsonProperty("expire_time")]
        public string ExpireTime { get; set; }

        [JsonProperty("submission_time")]
        public string SubmissionTime { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("time_in_force")]
        [JsonConverter(typeof(EnumConverter))]
        public TimeInForce TimeInForce { get; set; }
    }
}
