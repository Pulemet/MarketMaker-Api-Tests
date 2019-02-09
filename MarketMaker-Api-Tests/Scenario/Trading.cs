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
using MarketMaker_Api_Tests.Helper.RestConnector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MarketMaker_Api_Tests.Scenario
{
    [TestClass]
    public class Trading
    {
        private static MarketMakerRest _mmRest;
        private static TestEventHandler _testEvent = new TestEventHandler();
        private const string AuthorizationCrypto = "Basic d2ViOg==";
        private const string UrlCrypto = "http://18.218.20.9";

        public static void InitAlgorithms()
        {
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
            InitAlgorithms();
            _mmRest = new MarketMakerRest();

            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);

            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.RestService.SaveHedger(_testEvent.Algorithms[0].HedgerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);
            Thread.Sleep(1000);
            // Check full config
            bool testResult = _testEvent.Algorithms[0].Equals(_mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId));

            _mmRest.RestService.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].InstrumentConfigInfo.Running = true;
            _mmRest.RestService.StartPricer(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo.Running = true;
            _mmRest.RestService.StartHedger(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo.Running = true;
            Thread.Sleep(1000);
            // Check start
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(newPricerConfig);
            // Change hedger
            HedgerConfigDto newHedgerConfig = AlgorithmInfo.CreateConfig<HedgerConfigDto>("hedger2.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.RestService.SaveHedger(newHedgerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Thread.Sleep(1000);
            // Check after change
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.RestService.StopHedger(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].InstrumentConfigInfo.Running = false;
            _mmRest.RestService.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo.Running = false;
            _mmRest.RestService.StopInstrument(_testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].HedgerConfigInfo.Running = false;
            Thread.Sleep(1000);
            // Check stop
            testResult = _testEvent.Algorithms[0].Equals(_mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.RestService.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);
            // Check delete and test result
            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, testResult, "Tests are failed");
        }

        [TestMethod]
        public void CheckQuotesBook()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);
            _mmRest.RestService.StartPricer(_testEvent.Algorithms[0].AlgoId);

            var quoteBookListener = _mmRest.WebSocketService.CreateQuotesSubscription();

            _testEvent.Algorithms[0].QuoteMessageHandler += _testEvent.CompareQuoteAgainstParams;
            quoteBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);

            WaitTestEvents(5);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(newPricerConfig);
            Debug.WriteLine("Pricer is changed");

            WaitTestEvents(7);

            quoteBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);
            _testEvent.Algorithms[0].QuoteMessageHandler -= _testEvent.CompareQuoteAgainstParams;

            _mmRest.RestService.StopPricer(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.RestService.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareSourceAgainstTargetBook()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            // 1
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);

            _testEvent.Algorithms[0].HedgerConfigInfo = _mmRest.RestService.SaveHedger(_testEvent.Algorithms[0].HedgerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);
            _mmRest.RestService.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _mmRest.RestService.StartPricer(_testEvent.Algorithms[0].AlgoId);

            // 2
            _testEvent.Algorithms[1].InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(_testEvent.Algorithms[1].InstrumentConfigInfo);
            _testEvent.Algorithms[1].SetAlgoId(_testEvent.Algorithms[1].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[1].PricerConfigInfo = _mmRest.RestService.SavePricer(_testEvent.Algorithms[1].PricerConfigInfo);

            _testEvent.Algorithms[1].HedgerConfigInfo = _mmRest.RestService.SaveHedger(_testEvent.Algorithms[1].HedgerConfigInfo);
            _testEvent.Algorithms[1].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(_testEvent.Algorithms[1].RiskLimitConfigInfo);
            _mmRest.RestService.StartInstrument(_testEvent.Algorithms[1].AlgoId);
            _mmRest.RestService.StartPricer(_testEvent.Algorithms[1].AlgoId);

            Thread.Sleep(1000);

            var sourceBookListener = _mmRest.WebSocketService.CreateSourceMarketDataSubscription();
            var targetBookListener = _mmRest.WebSocketService.CreateTargetMarketDataSubscription();

            targetBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            _testEvent.StartTestTime = DateTime.Now.AddSeconds(-2);
            _testEvent.Algorithms[1].SourceMessageHandler += _testEvent.CompareSourceAgainstTargetBook;
            sourceBookListener.Subscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);

            WaitTestEvents(4);

            _testEvent.StartTestTime = DateTime.Now.AddSeconds(-1);
            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(newPricerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Debug.WriteLine("Pricer and limits are changed");

            WaitTestEvents(7);

            targetBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            sourceBookListener.Unsubscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);
            _testEvent.Algorithms[1].SourceMessageHandler -= _testEvent.CompareSourceAgainstTargetBook;

            _mmRest.RestService.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.RestService.StopInstrument(_testEvent.Algorithms[0].AlgoId);

            _mmRest.RestService.StopPricer(_testEvent.Algorithms[1].AlgoId);
            _mmRest.RestService.StopInstrument(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            _mmRest.RestService.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            _mmRest.RestService.DeleteAlgorithm(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(_testEvent.Algorithms[1].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareStatisticAgainstTargetBook()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            _testEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(_testEvent.Algorithms[0].InstrumentConfigInfo);
            _testEvent.Algorithms[0].SetAlgoId(_testEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.RestService.SavePricer(_testEvent.Algorithms[0].PricerConfigInfo);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(_testEvent.Algorithms[0].RiskLimitConfigInfo);

            _mmRest.RestService.StartInstrument(_testEvent.Algorithms[0].AlgoId);
            _mmRest.RestService.StartPricer(_testEvent.Algorithms[0].AlgoId);

            //_wsEvent.Algorithms[0] = new AlgorithmInfo(Rest.RestService.GetInstrument(16));

            Thread.Sleep(1000);

            var targetBookListener = _mmRest.WebSocketService.CreateTargetMarketDataSubscription();
            var statisticListener = _mmRest.WebSocketService.CreateTradingStatisticsSubscription();

            targetBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            _testEvent.Algorithms[0].TargetMessageHandler += _testEvent.CompareStatisticAgainstTargetBook;
            statisticListener.Subscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);

            WaitTestEvents(8);

            targetBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            statisticListener.Unsubscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);
            _testEvent.Algorithms[0].TargetMessageHandler -= _testEvent.CompareStatisticAgainstTargetBook;

            _mmRest.RestService.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.RestService.StopInstrument(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.RestService.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CheckSpread()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(algo.InstrumentConfigInfo);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.RestService.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.RestService.StartPricer(algo.AlgoId);
            _mmRest.RestService.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            var quotesBookListener = _mmRest.WebSocketService.CreateQuotesSubscription();
            var targetBookListener = _mmRest.WebSocketService.CreateTargetMarketDataSubscription();

            targetBookListener.Subscribe(algo.AlgoId, algo.OnTargetMessage);
            algo.QuoteMessageHandler += _testEvent.CompareSpread;
            Thread.Sleep(1000);

            quotesBookListener.Subscribe(algo.AlgoId, algo.OnQuoteMessage);
            WaitTestEvents(3);

            // Change pricer
            _testEvent.StartTestTime = DateTime.Now;
            algo.PricerConfigInfo.Running = true;
            algo.PricerConfigInfo.MinSpread = "0.0004";
            algo.PricerConfigInfo = _mmRest.RestService.SavePricer(algo.PricerConfigInfo);
            Debug.WriteLine("Pricer is changed");

            WaitTestEvents(6);

            quotesBookListener.Unsubscribe(algo.AlgoId, algo.OnQuoteMessage);
            targetBookListener.Unsubscribe(algo.AlgoId, algo.OnTargetMessage);
            algo.QuoteMessageHandler -= _testEvent.CompareSpread;
            Thread.Sleep(1000);

            _mmRest.RestService.StopPricer(algo.AlgoId);
            _mmRest.RestService.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.RestService.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareOrdersAgainstOpenQty()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            //_testEvent.Algorithms[0] = new AlgorithmInfo(Rest.RestService.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(algo.InstrumentConfigInfo);
            algo.PricerConfigInfo = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer6.json", algo.AlgoId);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.RestService.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.RestService.StartPricer(algo.AlgoId);
            _mmRest.RestService.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 3, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
            var cryptoRest = new CryptoCortexRest();
            cryptoRest.WebSocketService.Subscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);

            var ordersListener = _mmRest.WebSocketService.CreateOrdersSubscription();
            var tradeStatisticListener = _mmRest.WebSocketService.CreateTradingStatisticsSubscription();

            algo.OrdersHandler += _testEvent.CalculateSizeOrders;
            algo.TradeStatisticHandler += _testEvent.CompareOrdersAgainstOpenQty;

            ordersListener.Subscribe(algo.AlgoId, algo.OnOrderMessage);
            Thread.Sleep(1000);
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(4);

            // Buy Order
            cryptoRest.WebSocketService.SendMessage(Util.GetSendOrderRequest(algo.OrderToSend));
            WaitTestEvents(5);

            algo.OrderToSend.Side = Side.SELL;
            // Sell Order
            cryptoRest.WebSocketService.SendMessage(Util.GetSendOrderRequest(algo.OrderToSend));
            WaitTestEvents(5);

            cryptoRest.WebSocketService.Unsubscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);
            ordersListener.Unsubscribe(algo.AlgoId, algo.OnOrderMessage);
            algo.OrdersHandler -= _testEvent.CalculateSizeOrders;
            Thread.Sleep(500);

            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.CompareOrdersAgainstOpenQty;
            Thread.Sleep(500);

            _mmRest.RestService.StopPricer(algo.AlgoId);
            _mmRest.RestService.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.RestService.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareExecutionsAgainstTradeQty()
        {
            InitAlgorithms();
            _mmRest = new MarketMakerRest();
            //_testEvent.Algorithms[0] = new AlgorithmInfo(Rest.RestService.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.RestService.CreateInstrument(algo.InstrumentConfigInfo);
            algo.PricerConfigInfo = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer6.json", algo.AlgoId);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.RestService.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.RestService.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.RestService.StartPricer(algo.AlgoId);
            _mmRest.RestService.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 5, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };

            var executionsListener = _mmRest.WebSocketService.CreateExecutionsSubscription();
            var tradeStatisticListener = _mmRest.WebSocketService.CreateTradingStatisticsSubscription();

            algo.ExecutionsHandler += _testEvent.CalculateSizeExecutions;
            executionsListener.Subscribe(algo.AlgoId, algo.OnExecutionMessage);

            algo.TradeStatisticHandler += _testEvent.MonitorChangesPosition;
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(2);

            var cryptoRest = new CryptoCortexRest();
            cryptoRest.WebSocketService.Subscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);
            Thread.Sleep(500);

            // Buy Order
            cryptoRest.WebSocketService.SendMessage(Util.GetSendOrderRequest(algo.OrderToSend));
            WaitTestEvents(5);

            algo.OrderToSend.Side = Side.SELL;
            // Sell Order
            cryptoRest.WebSocketService.SendMessage(Util.GetSendOrderRequest(algo.OrderToSend));
            WaitTestEvents(5);

            cryptoRest.WebSocketService.Unsubscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);
            executionsListener.Unsubscribe(algo.AlgoId, algo.OnExecutionMessage);
            algo.ExecutionsHandler -= _testEvent.CompareStatisticAgainstTargetBook;
            Thread.Sleep(500);

            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.MonitorChangesPosition;
            Thread.Sleep(500);

            _mmRest.RestService.StopPricer(algo.AlgoId);
            _mmRest.RestService.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.RestService.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.RestService.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void WaitTestEvents(int seconds)
        {
            int currentSecond = 0;
            while (currentSecond++ < seconds * 2)
            {
                if (!_testEvent.TestResult)
                {
                    _testEvent.Algorithms.ForEach(a => a.StopDeleteAll(_mmRest.RestService));
                    Assert.Fail("Test is failed");
                }
                Thread.Sleep(500);
            }
        }
    }
}
