using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;

namespace MarketMaker_Api_Tests.Helper
{
    class MarketMakerRest
    {
        private readonly string Url = "https://18.218.146.41:8990";
        private readonly string Authorization = "Basic bW13ZWJ1aTptbQ==";

        public IMarketMakerRestService RestService { get; set; }
        public SubscriptionFactory WebSocketService { get; set; }

        public MarketMakerRest()
        {
            Initialize();
        }

        protected void Initialize()
        {
            RestService = MarketMakerRestServiceFactory.CreateMakerRestService(Url, "/oauth/token",
                Authorization);
            RestService.Authorize("admin", "admin");
            WebSocketService = new SubscriptionFactory("wss://18.218.146.41:8990/websocket/v0", RestService.Token);
        }
    }
}
