using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketMaker_Api_Tests.Scenario
{
    [TestClass]
    public class Trading
    {
        private const string _url = "https://18.218.146.41:8990";
        private const string _authorization = "Basic bW13ZWJ1aTptbQ==";

        private static IMarketMakerRestService _mmRest;
        private static SubscriptionFactory _wsFactory;
        private static WebSocketEvent _wsEvent;

        public static void Initialize()
        {
            _mmRest = MakerMakerRestServiceFactory.CreateMakerRestService(_url, "/oauth/token",
                _authorization);
            _mmRest.Authorize("admin", "admin");
            _wsFactory = new SubscriptionFactory("wss://18.218.146.41:8990/websocket/v0", _mmRest.Token);

            _wsEvent = new WebSocketEvent();

            // Initializing algorithms for testing.
            _wsEvent.Algorithms = new List<AlgorithmInfo>()
            {
                new AlgorithmInfo("instrument.json", "pricer.json", "hedger.json", "risklimit.json"),
                new AlgorithmInfo("instrument2.json", "pricer2.json", "hedger.json", "risklimit2.json")
            };
        }

        [TestMethod]
        public void AddFullInstrumentConfig()
        {
            Initialize();
            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);

            _wsEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(_wsEvent.Algorithms[0].HedgerConfigInfo);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_wsEvent.Algorithms[0].RiskLimitConfigInfo);
            Thread.Sleep(1000);
            // 1
            bool testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId));

            _mmRest.StartInstrument(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].InstrumentConfigInfo.Running = true;
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo.Running = true;
            _mmRest.StartHedger(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].HedgerConfigInfo.Running = true;
            Thread.Sleep(1000);
            // 2
            testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.StopHedger(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].InstrumentConfigInfo.Running = false;
            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo.Running = false;
            _mmRest.StopInstrument(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].HedgerConfigInfo.Running = false;
            Thread.Sleep(1000);
            // 3
            testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);
            // 4
            Assert.AreEqual(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, true, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(testResult, true, "Tests are failed");
        }

        [TestMethod]
        public void AddTwoInstruments()
        {
            Initialize();
            // 1
            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);

            _wsEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(_wsEvent.Algorithms[0].HedgerConfigInfo);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_wsEvent.Algorithms[0].RiskLimitConfigInfo);
            _mmRest.StartInstrument(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StartHedger(_wsEvent.Algorithms[0].AlgoId);

            // 2
            _wsEvent.Algorithms[1].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[1].InstrumentConfigInfo);
            _wsEvent.Algorithms[1].SetAlgoId(_wsEvent.Algorithms[1].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[1].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[1].PricerConfigInfo);

            _wsEvent.Algorithms[1].HedgerConfigInfo = _mmRest.SaveHedger(_wsEvent.Algorithms[1].HedgerConfigInfo);
            _wsEvent.Algorithms[1].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_wsEvent.Algorithms[1].RiskLimitConfigInfo);
            _mmRest.StartInstrument(_wsEvent.Algorithms[1].AlgoId);
            _mmRest.StartPricer(_wsEvent.Algorithms[1].AlgoId);
            _mmRest.StartHedger(_wsEvent.Algorithms[1].AlgoId);

            var tradeListener = _wsFactory.CreateTradingStatisticsSubscription();
            try
            {
                tradeListener.Subscribe(_wsEvent.OnStatisticsMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Thread.Sleep(5000);

            PricerConfigDto newPricerConfig = AlgorithmInfo.CreatePricerConfig("pricer3.json");
            newPricerConfig.AlgoId = _wsEvent.Algorithms[0].AlgoId;
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            //Console.WriteLine("Pricer is changed!");

            Thread.Sleep(8000);

            tradeListener.Unsubscribe(_wsEvent.OnStatisticsMessage);

            _mmRest.StopHedger(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_wsEvent.Algorithms[0].AlgoId);

            _mmRest.StopHedger(_wsEvent.Algorithms[1].AlgoId);
            _mmRest.StopPricer(_wsEvent.Algorithms[1].AlgoId);
            _mmRest.StopInstrument(_wsEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, true, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(_mmRest.GetInstrument(_wsEvent.Algorithms[1].AlgoId) == null, true, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CheckQuotesBook()
        {
            Initialize();
            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);

            var marketBookSubs = _wsFactory.CreateSourceMarketDataSubscription();
            try
            {
                marketBookSubs.Subscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.OnQuoteMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Thread.Sleep(5000);

            PricerConfigDto newPricerConfig = AlgorithmInfo.CreatePricerConfig("pricer4.json");
            newPricerConfig.AlgoId = _wsEvent.Algorithms[0].AlgoId;
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            //Console.WriteLine("Pricer is changed!");

            Thread.Sleep(7000);

            marketBookSubs.Unsubscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.OnQuoteMessage);

            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, true, "Deleted algorithm doesn't equal to null");
        }
    }
}
