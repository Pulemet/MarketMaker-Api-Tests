﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Rest;
using MarketMaker_Api_Tests.CriptoCortex;
using MarketMaker_Api_Tests.CryptoCortex;
using MarketMaker_Api_Tests.CryptoCortex.Models;
using MarketMaker_Api_Tests.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SubscriptionFactory = MarketMaker.Api.Subscriptions.SubscriptionFactory;
using Timer = System.Timers.Timer;

namespace MarketMaker_Api_Tests.Scenario
{
    [TestClass]
    public class Trading
    {
        private const string _url = "https://18.191.68.137:8990";
        private const string _authorization = "Basic bW13ZWJ1aTptbQ==";
        private const string _subscribeUrl = "wss://18.191.68.137:8990/websocket/v0";
        private const string _cryptoUrl = "http://18.218.20.9";
        private const string _cryptoAuthorization = "Basic d2ViOg==";
        private const string _cryptoTraderSubscribeUrl = "ws://18.218.20.9/websocket/v1?trader_0";
        private const string _ordersDestination = "/app/v1/orders/create";
        private const string _responsesDestination = "/user/v1/responses";
        private const string TestFail = "Test is failed!";

        private static string DestinationPath = Environment.CurrentDirectory + "\\Params\\";
        private static string SourcePath = Environment.CurrentDirectory + "\\..\\..\\..\\Params\\";

        private static IMarketMakerRestService _mmRest;
        private static SubscriptionFactory _wsFactory;
        private static TestEventHandler _testEvent;

        private static IMarketMakerRestService _restCrypto;
        private static TraderSubscription _wsCrypto;

        public static void Initialize()
        {
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
                foreach (string newPath in Directory.GetFiles(SourcePath, "*.json*",
                    SearchOption.TopDirectoryOnly))
                    File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
            }
                

            _mmRest = MarketMakerRestServiceFactory.CreateMakerRestService(_url, "/oauth/token",
                _authorization);
            _mmRest.Authorize("admin", "admin");
            _wsFactory = new SubscriptionFactory(_subscribeUrl, _mmRest.Token);

            _testEvent = new TestEventHandler();

            // Initializing algorithms for testing.
            _testEvent.Algorithms = new List<AlgorithmInfo>()
            {
                new AlgorithmInfo("instrument.json", "pricer.json", "hedger.json", "risklimit.json"),
                new AlgorithmInfo("instrument2.json", "pricer2.json", "hedger.json", "risklimit2.json")
            };
        }

        public static void ConnectToCrypto()
        {
            _restCrypto = MarketMakerRestServiceFactory.CreateMakerRestService(_cryptoUrl, "/oauth/token",
                _cryptoAuthorization);
            _restCrypto.Authorize("Tester 1", "password");
            _wsCrypto = new TraderSubscription(_cryptoTraderSubscribeUrl, _restCrypto.Token);
            _wsCrypto.ResponsesSubscribe();
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
            Assert.AreEqual(true, testResult, TestFail);
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

            _testEvent.Algorithms[0].QuoteMessageHandler += _testEvent.CompareQuoteAgainstParams;
            quoteBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);

            WaitTestEvents(5);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            Debug.WriteLine("Pricer is changed");

            WaitTestEvents(7);

            quoteBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnQuoteMessage);
            _testEvent.Algorithms[0].QuoteMessageHandler -= _testEvent.CompareQuoteAgainstParams;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
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

            // 2
            _testEvent.Algorithms[1].InstrumentConfigInfo = _mmRest.CreateInstrument(_testEvent.Algorithms[1].InstrumentConfigInfo);
            _testEvent.Algorithms[1].SetAlgoId(_testEvent.Algorithms[1].InstrumentConfigInfo.AlgoId);
            _testEvent.Algorithms[1].PricerConfigInfo = _mmRest.SavePricer(_testEvent.Algorithms[1].PricerConfigInfo);

            _testEvent.Algorithms[1].HedgerConfigInfo = _mmRest.SaveHedger(_testEvent.Algorithms[1].HedgerConfigInfo);
            _testEvent.Algorithms[1].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_testEvent.Algorithms[1].RiskLimitConfigInfo);
            _mmRest.StartInstrument(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StartPricer(_testEvent.Algorithms[1].AlgoId);

            Thread.Sleep(1000);

            var sourceBookListener = _wsFactory.CreateSourceMarketDataSubscription();
            var targetBookListener = _wsFactory.CreateTargetMarketDataSubscription();

            targetBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            _testEvent.StartTestTime = DateTime.Now.AddSeconds(-2);
            _testEvent.Algorithms[1].SourceMessageHandler += _testEvent.CompareSourceAgainstTargetBook;
            sourceBookListener.Subscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);

            WaitTestEvents(4);

            _testEvent.StartTestTime = DateTime.Now.AddSeconds(-1);
            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _testEvent.Algorithms[0].AlgoId);
            _testEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Debug.WriteLine("Pricer and limits are changed");

            WaitTestEvents(7);

            targetBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            sourceBookListener.Unsubscribe(_testEvent.Algorithms[1].AlgoId, _testEvent.Algorithms[1].OnSourceMessage);
            _testEvent.Algorithms[1].SourceMessageHandler -= _testEvent.CompareSourceAgainstTargetBook;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[0].AlgoId);

            _mmRest.StopPricer(_testEvent.Algorithms[1].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[1].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[1].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
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

            var targetBookListener = _wsFactory.CreateTargetMarketDataSubscription();
            var statisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            statisticListener.Subscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);
            Thread.Sleep(1000);
            _testEvent.Algorithms[0].TargetMessageHandler += _testEvent.CompareStatisticAgainstTargetBook;
            targetBookListener.Subscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);

            WaitTestEvents(8);

            targetBookListener.Unsubscribe(_testEvent.Algorithms[0].AlgoId, _testEvent.Algorithms[0].OnTargetMessage);
            statisticListener.Unsubscribe(_testEvent.Algorithms[0].OnTradeStatisticMessage);
            _testEvent.Algorithms[0].TargetMessageHandler -= _testEvent.CompareStatisticAgainstTargetBook;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(_testEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(_testEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_testEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        [TestMethod]
        public void CheckSpread()
        {
            Initialize();
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.CreateInstrument(algo.InstrumentConfigInfo);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.StartPricer(algo.AlgoId);
            _mmRest.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            var quotesBookListener = _wsFactory.CreateQuotesSubscription();
            var targetBookListener = _wsFactory.CreateTargetMarketDataSubscription();

            targetBookListener.Subscribe(algo.AlgoId, algo.OnTargetMessage);
            algo.QuoteMessageHandler += _testEvent.CompareSpread;
            Thread.Sleep(1000);

            quotesBookListener.Subscribe(algo.AlgoId, algo.OnQuoteMessage);
            WaitTestEvents(3);

            // Change pricer
            _testEvent.StartTestTime = DateTime.Now;
            algo.PricerConfigInfo.Running = true;
            algo.PricerConfigInfo.MinSpread = "0.0004";
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);
            Debug.WriteLine("Pricer is changed");

            WaitTestEvents(6);

            quotesBookListener.Unsubscribe(algo.AlgoId, algo.OnQuoteMessage);
            targetBookListener.Unsubscribe(algo.AlgoId, algo.OnTargetMessage);
            algo.QuoteMessageHandler -= _testEvent.CompareSpread;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(algo.AlgoId);
            _mmRest.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        [TestMethod]
        public void CompareOrdersAgainstOpenQty()
        {
            Initialize();
            //_testEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.CreateInstrument(algo.InstrumentConfigInfo);
            algo.PricerConfigInfo = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer6.json", algo.AlgoId);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.StartPricer(algo.AlgoId);
            _mmRest.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 3, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
            ConnectToCrypto();

            var ordersListener = _wsFactory.CreateOrdersSubscription();
            var tradeStatisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            algo.OrdersHandler += _testEvent.CalculateSizeOrders;
            algo.TradeStatisticHandler += _testEvent.CompareOrdersAgainstOpenQty;

            ordersListener.Subscribe(algo.AlgoId, algo.OnOrderMessage);
            Thread.Sleep(1000);
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(4);

            // Buy Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(5);

            algo.OrderToSend.Side = Side.SELL;
            // Sell Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(5);
            _wsCrypto.StopResponses();
            _wsCrypto.Close();

            ordersListener.Unsubscribe(algo.AlgoId, algo.OnOrderMessage);
            algo.OrdersHandler -= _testEvent.CalculateSizeOrders;
            Thread.Sleep(500);

            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.CompareOrdersAgainstOpenQty;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(algo.AlgoId);
            _mmRest.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        [TestMethod]
        public void CompareExecutionsAgainstTradeQty()
        {
            Initialize();
            //_testEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.CreateInstrument(algo.InstrumentConfigInfo);
            algo.PricerConfigInfo = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer6.json", algo.AlgoId);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.StartPricer(algo.AlgoId);
            _mmRest.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 5, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
            ConnectToCrypto();

            var executionsListener = _wsFactory.CreateExecutionsSubscription();
            var tradeStatisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            algo.ExecutionsHandler += _testEvent.CalculateExecutions;
            executionsListener.Subscribe(algo.AlgoId, algo.OnExecutionMessage);

            algo.TradeStatisticHandler += _testEvent.MonitorChangesPosition;
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(2);

            Thread.Sleep(500);

            // Buy Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(6);

            algo.OrderToSend.Side = Side.SELL;
            // Sell Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(5);
            _wsCrypto.StopResponses();
            _wsCrypto.Close();

            executionsListener.Unsubscribe(algo.AlgoId, algo.OnExecutionMessage);
            algo.ExecutionsHandler -= _testEvent.CalculateExecutions;
            Thread.Sleep(500);

            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.MonitorChangesPosition;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(algo.AlgoId);
            _mmRest.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        [TestMethod]
        public void CompareAlertsAgainstTradeQty()
        {
            Initialize();
            //_testEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(240));
            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo = _mmRest.CreateInstrument(algo.InstrumentConfigInfo);
            algo.PricerConfigInfo = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer6.json", algo.AlgoId);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);
            algo.RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);

            _mmRest.StartPricer(algo.AlgoId);
            _mmRest.StartInstrument(algo.AlgoId);

            Thread.Sleep(1000);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 5, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
            ConnectToCrypto();

            var alertsListener = _wsFactory.CreateAlertsSubscription();
            var tradeStatisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            algo.AlertsHandler += _testEvent.CalculateExecutionsFromAlerts;
            alertsListener.Subscribe(algo.OnExecutionAlertMessage);

            algo.TradeStatisticHandler += _testEvent.MonitorChangesPosition;
            tradeStatisticListener.Subscribe(algo.OnTradeStatisticMessage);

            WaitTestEvents(2);

            Thread.Sleep(500);

            // Buy Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(6);

            algo.OrderToSend.Side = Side.SELL;
            // Sell Order
            _wsCrypto.SendOrder(algo.OrderToSend, _testEvent.CheckOrderSent);
            WaitTestEvents(5);
            _wsCrypto.StopResponses();
            _wsCrypto.Close();

            alertsListener.Unsubscribe(algo.OnExecutionAlertMessage);
            algo.AlertsHandler -= _testEvent.CalculateExecutionsFromAlerts;
            Thread.Sleep(500);

            tradeStatisticListener.Unsubscribe(algo.OnTradeStatisticMessage);
            algo.TradeStatisticHandler -= _testEvent.MonitorChangesPosition;
            Thread.Sleep(500);
            _wsFactory.Close();

            _mmRest.StopPricer(algo.AlgoId);
            _mmRest.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);

            Assert.AreEqual(true, _mmRest.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        [TestMethod]
        public void LeanHedger()
        {
            Initialize();

            AlgorithmInfo algo = _testEvent.Algorithms[0];
            algo.InstrumentConfigInfo =
                _mmRest.CreateInstrument(algo.InstrumentConfigInfo);
            algo.SetAlgoId(algo.InstrumentConfigInfo.AlgoId);
            algo.PricerConfigInfo = _mmRest.SavePricer(algo.PricerConfigInfo);

            algo.HedgerConfigInfo = _mmRest.SaveHedger(algo.HedgerConfigInfo);
            algo.RiskLimitConfigInfo =
                _mmRest.SaveRiskLimitsConfig(algo.RiskLimitConfigInfo);
            Thread.Sleep(1000);

            _mmRest.StartInstrument(algo.AlgoId);
            _mmRest.StartPricer(algo.AlgoId);
            Thread.Sleep(1000);

            var alertsListener = _wsFactory.CreateAlertsSubscription();

            algo.AlertsHandler += _testEvent.CheckExecutionsInEvents;
            alertsListener.Subscribe(algo.OnExecutionAlertMessage);

            algo.OrderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 10, Side = Side.SELL, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
            ConnectToCrypto();
            Thread.Sleep(1000);
            _wsCrypto.OrdersReceiver(algo.CheckOrderStatus);
            Thread.Sleep(2000);

            // Sell Order
            _testEvent.WaitOrderFill(algo, _wsCrypto);

            algo.OrderToSend.Type = OrderType.LIMIT;
            algo.OrderToSend.Side = Side.BUY;
            algo.OrderToSend.Quantity = 1.0;
            algo.OrderToSend.TimeInForce = TimeInForce.DAY;
            algo.OrderToSend.Price = 0.033;
            _mmRest.StopPricer(algo.AlgoId);
            _mmRest.StartHedger(algo.AlgoId);
            Thread.Sleep(1000);

            // Buy Order for hedger
            _testEvent.WaitOrderFill(algo, _wsCrypto);
            WaitTestEvents(3);

            if (_testEvent.TestResult)
                _testEvent.CheckFillExecutions(algo);

            _wsCrypto.OrdersUnsubscribe(algo.CheckOrderStatus);
            _wsCrypto.StopResponses();
            _wsCrypto.Close();
            alertsListener.Unsubscribe(algo.OnExecutionAlertMessage);
            algo.AlertsHandler -= _testEvent.CheckExecutionsInEvents;
            Thread.Sleep(500);

            _wsFactory.Close();

            FinishAlgo(true, false, true, algo);
            Assert.AreEqual(true, _mmRest.GetInstrument(algo.AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _testEvent.TestResult, TestFail);
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void WaitTestEvents(int seconds)
        {
            int currentSecond = 0;
            while (currentSecond++ < seconds * 2)
            {
                if (!_testEvent.TestResult)
                {
                    return;
                }
                Thread.Sleep(500);
            }
        }

        public void FinishAlgo(bool isInstrument, bool isPricer, bool isHedger, AlgorithmInfo algo)
        {
            if (isHedger)
                _mmRest.StopHedger(algo.AlgoId);
            if(isPricer)
                _mmRest.StopPricer(algo.AlgoId);
            if(isInstrument)
                _mmRest.StopInstrument(algo.AlgoId);
            Thread.Sleep(500);

            _mmRest.DeleteAlgorithm(algo.AlgoId);
            Thread.Sleep(500);
        }
    }
}
