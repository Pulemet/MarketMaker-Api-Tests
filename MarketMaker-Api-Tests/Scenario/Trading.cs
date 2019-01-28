using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CriptoCortex;
using MarketMaker_Api_Tests.CryptoCortex.Models;
using MarketMaker_Api_Tests.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MarketMaker_Api_Tests.Scenario
{
    [TestClass]
    public class Trading
    {
        private const string _url = "https://18.218.146.41:8990";
        private const string _authorization = "Basic bW13ZWJ1aTptbQ==";

        private static IMarketMakerRestService _mmRest;
        private static SubscriptionFactory _wsFactory;
        private static TestEventHandler _testEvent;

        public static void Initialize()
        {
            _mmRest = MakerMakerRestServiceFactory.CreateMakerRestService(_url, "/oauth/token",
                _authorization);
            _mmRest.Authorize("admin", "admin");
            _wsFactory = new SubscriptionFactory("wss://18.218.146.41:8990/websocket/v0", _mmRest.Token);

            _testEvent = new TestEventHandler();

            // Initializing algorithms for testing.
            _testEvent.Algorithms = new List<AlgorithmInfo>()
            {
                new AlgorithmInfo("instrument.json", "pricer.json", "hedger.json", "risklimit.json"),
                new AlgorithmInfo("instrument2.json", "pricer2.json", "hedger.json", "risklimit2.json")
            };
        }

        [TestMethod]
        public void AddChangeStopFullInstrumentConfig()
        {
            Initialize();

            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);

            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(_testEvent.Algorithms[0].HedgerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);
            Thread.Sleep(1000);
            // Check full config
            bool testResult = _testEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId));

            _mmRest.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].InstrumentConfigInfo.Running = true;
            _mmRest.StartPricer(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo.Running = true;
            _mmRest.StartHedger(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo.Running = true;
            Thread.Sleep(1000);
            // Check start
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            // Change hedger
            HedgerConfigDto newHedgerConfig = AlgorithmInfo.CreateConfig<HedgerConfigDto>("hedger2.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(newHedgerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Thread.Sleep(1000);
            // Check after change
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.StopHedger(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].InstrumentConfigInfo.Running = false;
            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo.Running = false;
            _mmRest.StopInstrument(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo.Running = false;
            Thread.Sleep(1000);
            // Check stop
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);
            // Check delete and test result
            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, testResult, "Tests are failed");
        }

        [TestMethod]
        public void CheckQuotesBook()
        {
            Initialize();
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);
            _mmRest.StartPricer(_testEvent.Algorithms[0].AlgoId);

            var quoteBookListener = _wsFactory.CreateQuotesSubscription();
            try
            {
                _testEvent.Algorithms[0].QuoteMessageHandler += _testEvent.CompareQuoteAgainstParams;
                quoteBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            WaitTestEvents(5);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            Debug.WriteLine("Pricer is changed");

            WaitTestEvents(7);

            quoteBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);
            _testEvent.Algorithms[0].QuoteMessageHandler -= _testEvent.CompareQuoteAgainstParams;

            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareSourceAgainstTargetBook()
        {
            Initialize();
            // 1
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);

            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(_testEvent.Algorithms[0].HedgerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);
            _mmRest.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StartPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StartHedger(_testEvent.Algorithms[0].AlgoId);

            // 2
            _testEvent.Algorithms[1].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[1].InstrumentConfigInfo);
            _testEvent.Algorithms[1].SetAlgoId(_testEvent.Algorithms[1].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[1].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[1].PricerConfigInfo);

            _testEvent.Algorithms[1].HedgerConfigInfo = _mmRest.SaveHedger(_testEvent.Algorithms[1].HedgerConfigInfo);
            _testEvent.Algorithms[1].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_testEvent.Algorithms[1].RiskLimitConfigInfo);
            _mmRest.StartInstrument(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StartPricer(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StartHedger(_testEvent.Algorithms[1].AlgoId);

            Thread.Sleep(1000);

            var sourceBookListener = _wsFactory.CreateSourceMarketDataSubscription();
            var targetBookListener = _wsFactory.CreateTargetMarketDataSubscription();
            try
            {
                targetBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
                _testEvent.Algorithms[1].SourceMessageHandler += _testEvent.CompareSourceAgainstTargetBook;
                sourceBookListener.Subscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            WaitTestEvents(5);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Debug.WriteLine("Pricer and limits are changed");

            WaitTestEvents(8);

            targetBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            sourceBookListener.Unsubscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);
            _testEvent.Algorithms[1].SourceMessageHandler -= _testEvent.CompareSourceAgainstTargetBook;

            _mmRest.StopHedger(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[0].AlgoId);

            _mmRest.StopHedger(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StopPricer(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[1].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareStatisticAgainstTargetBook()
        {
            Initialize();
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);

            _mmRest.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StartPricer(_testEvent.Algorithms[0].AlgoId);

            //_wsEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(16));

            Thread.Sleep(1000);

            var targetBookLister = _wsFactory.CreateTargetMarketDataSubscription();
            var statisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            try
            {
                targetBookLister.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
                _testEvent.Algorithms[0].TargetMessageHandler += _testEvent.CompareStatisticAgainstTargetBook;
                statisticListener.Subscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            WaitTestEvents(10);

            targetBookLister.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            statisticListener.Unsubscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);
            _testEvent.Algorithms[0].TargetMessageHandler -= _testEvent.CompareStatisticAgainstTargetBook;

            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CreateOrderCC()
        {
            Initialize();
            _testEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.OrderToSend = new OrderDbo() { Destination = "DLTXMM", Quantity = 10, Side = Side.SELL, Type = OrderDbo.OrderType.MARKET, SecurityId = "ETHBTC" };

            var executionsListener = _wsFactory.CreateExecutionsSubscription();
            var tradeStatisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            algo.ExecutionsHandler += _testEvent.CalculateSizeExecutions;
            executionsListener.Subscribe(240, algo.OnExecutionMessage);

            algo.TradeStatisticHandler += _testEvent.MonitorChangesPosition;
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(2);

            string authorization = "Basic d2ViOg==";
            string urlCrypto = "http://18.218.20.9";
            IMarketMakerRestService restCrypto = MakerMakerRestServiceFactory.CreateMakerRestService(urlCrypto, "/oauth/token",
                authorization);
            restCrypto.Authorize("Tester 1", "password");
            StompWebSocketServiceCC wsCrypto = new StompWebSocketServiceCC("ws://18.218.20.9/websocket/v1?trader_0", restCrypto.Token);
            wsCrypto.Subscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);
            Thread.Sleep(500);

            string orderRequest = String.Format("correlation-id:ioeswd7t9m\r\nX-Deltix-Nonce:{0}\r\ndestination:/app/v1/orders/create\r\n\r\n{1}",
                                                 StompWebSocketService.ConvertToUnixTimestamp(DateTime.Now),
                                                 JsonConvert.SerializeObject(algo.OrderToSend));

            wsCrypto.SendMessage(orderRequest);

            WaitTestEvents(7);
            executionsListener.Unsubscribe(6, algo.OnExecutionMessage);
            algo.ExecutionsHandler -= _testEvent.CompareStatisticAgainstTargetBook;
            
            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.MonitorChangesPosition;
        }

        public void WaitTestEvents(int seconds)
        {
            int currentSecond = 0;
            while (currentSecond++ < seconds * 2)
            {
                if (!_testEvent.TestResult)
                {
                    _testEvent.Algorithms.ForEach(a => a.StopDeleteAll(_mmRest));
                    Assert.Fail("Test is failed");
                }
                Thread.Sleep(500);
            }
        }
    }
}
