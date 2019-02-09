using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CriptoCortex;

namespace MarketMaker_Api_Tests.Helper.RestConnector
{
    public class CryptoCortexRest
    {
        private readonly string Url = "http://18.218.20.9";
        private readonly string Authorization = "Basic d2ViOg==";

        public IMarketMakerRestService RestService { get; set; }
        public StompWebSocketServiceCrypto WebSocketService { get; set; }

        public CryptoCortexRest()
        {
            Initialize();
        }

        protected void Initialize()
        {
            RestService = MarketMakerRestServiceFactory.CreateMakerRestService(Url, "/oauth/token",
                Authorization);
            RestService.Authorize("Tester 1", "password");
            WebSocketService = new StompWebSocketServiceCrypto("ws://18.218.20.9/websocket/v1?trader_0", RestService.Token);
        }
    }
}
