using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.CryptoCortex.Models
{
    public class OrderResponce
    {
        [JsonProperty("order")]
        public Order Order { get; set; }

        [JsonProperty("events")]
        public Event[] Events { get; set; }
    }
}
