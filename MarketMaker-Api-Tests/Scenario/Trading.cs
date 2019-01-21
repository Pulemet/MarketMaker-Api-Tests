using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static WebSocketEventHandler _wsEvent;

        public static void Initialize()
        {
            _mmRest = MakerMakerRestServiceFactory.CreateMakerRestService(_url, "/oauth/token",
                _authorization);
            _mmRest.Authorize("admin", "admin");
            _wsFactory = new SubscriptionFactory("wss://18.218.146.41:8990/websocket/v0", _mmRest.Token);

            _wsEvent = new WebSocketEventHandler();

            // Initializing algorithms for testing.
            _wsEvent.Algorithms = new List<AlgorithmInfo>()
            {
                new AlgorithmInfo("instrument.json", "pricer.json", "hedger.json", "risklimit.json"),
                new AlgorithmInfo("instrument2.json", "pricer2.json", "hedger.json", "risklimit2.json")
            };
        }

        [TestMethod]
        public void AddChangeStopFullInstrumentConfig()
        {
            Initialize();

            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);

            _wsEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(_wsEvent.Algorithms[0].HedgerConfigInfo);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_wsEvent.Algorithms[0].RiskLimitConfigInfo);
            Thread.Sleep(1000);
            // Check full config
            bool testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId));

            _mmRest.StartInstrument(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].InstrumentConfigInfo.Running = true;
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo.Running = true;
            _mmRest.StartHedger(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].HedgerConfigInfo.Running = true;
            Thread.Sleep(1000);
            // Check start
            testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId)) && testResult;

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            // Change hedger
            HedgerConfigDto newHedgerConfig = AlgorithmInfo.CreateConfig<HedgerConfigDto>("hedger2.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].HedgerConfigInfo = _mmRest.SaveHedger(newHedgerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Thread.Sleep(1000);
            // Check after change
            testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.StopHedger(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].InstrumentConfigInfo.Running = false;
            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo.Running = false;
            _mmRest.StopInstrument(_wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].HedgerConfigInfo.Running = false;
            Thread.Sleep(1000);
            // Check stop
            testResult = _wsEvent.Algorithms[0].Equals(_mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId)) && testResult;

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);
            // Check delete and test result
            Assert.AreEqual(true, _mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, testResult, "Tests are failed");
        }

        [TestMethod]
        public void CheckQuotesBook()
        {
            Initialize();
            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);

            var quoteBookListener = _wsFactory.CreateQuotesSubscription();
            try
            {
                _wsEvent.Algorithms[0].QuoteMessageHandler += _wsEvent.CompareQuoteAgainstParams;
                quoteBookListener.Subscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnQuoteMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            Thread.Sleep(5000);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            Debug.WriteLine("Pricer is changed");

            Thread.Sleep(7000);

            quoteBookListener.Unsubscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnQuoteMessage);
            _wsEvent.Algorithms[0].QuoteMessageHandler -= _wsEvent.CompareQuoteAgainstParams;

            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareSourceAgainstTargetBook()
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

            Thread.Sleep(1000);

            var sourceBookListener = _wsFactory.CreateSourceMarketDataSubscription();
            var targetBookListener = _wsFactory.CreateTargetMarketDataSubscription();
            try
            {
                targetBookListener.Subscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnTargetMessage);
                _wsEvent.Algorithms[1].SourceMessageHandler += _wsEvent.CompareSourceAgainstTargetBook;
                sourceBookListener.Subscribe(_wsEvent.Algorithms[1].AlgoId, _wsEvent.Algorithms[1].OnSourceMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            Thread.Sleep(5000);

            // Change pricer
            PricerConfigDto newPricerConfig = AlgorithmInfo.CreateConfig<PricerConfigDto>("pricer3.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(newPricerConfig);
            // Change riskLimits
            RiskLimitsConfigDto newRiskLimitsConfig = AlgorithmInfo.CreateConfig<RiskLimitsConfigDto>("riskLimit3.json", _wsEvent.Algorithms[0].AlgoId);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(newRiskLimitsConfig);
            Debug.WriteLine("Pricer and limits are changed");

            Thread.Sleep(8000);

            targetBookListener.Unsubscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnTargetMessage);
            sourceBookListener.Unsubscribe(_wsEvent.Algorithms[1].AlgoId, _wsEvent.Algorithms[1].OnSourceMessage);
            _wsEvent.Algorithms[1].SourceMessageHandler -= _wsEvent.CompareSourceAgainstTargetBook;

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

            Assert.AreEqual(true, _mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
            Assert.AreEqual(true, _mmRest.GetInstrument(_wsEvent.Algorithms[1].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }

        [TestMethod]
        public void CompareStatisticAgainstTargetBook()
        {
            Initialize();
            _wsEvent.Algorithms[0].InstrumentConfigInfo = _mmRest.CreateInstrument(_wsEvent.Algorithms[0].InstrumentConfigInfo);
            _wsEvent.Algorithms[0].SetAlgoId(_wsEvent.Algorithms[0].InstrumentConfigInfo.AlgoId);
            _wsEvent.Algorithms[0].PricerConfigInfo = _mmRest.SavePricer(_wsEvent.Algorithms[0].PricerConfigInfo);
            _wsEvent.Algorithms[0].RiskLimitConfigInfo = _mmRest.SaveRiskLimitsConfig(_wsEvent.Algorithms[0].RiskLimitConfigInfo);

            _mmRest.StartInstrument(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StartPricer(_wsEvent.Algorithms[0].AlgoId);

            //_wsEvent.Algorithms[0] = new AlgorithmInfo(_mmRest.GetInstrument(16));

            Thread.Sleep(2000);

            var targetBookLister = _wsFactory.CreateTargetMarketDataSubscription();
            var statisticListener = _wsFactory.CreateTradingStatisticsSubscription();

            try
            {
                targetBookLister.Subscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnTargetMessage);
                _wsEvent.Algorithms[0].TargetMessageHandler += _wsEvent.CompareStatisticAgainstTargetBook;
                statisticListener.Subscribe(_wsEvent.Algorithms[0].OnTradeStatisticMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            Thread.Sleep(10000);

            targetBookLister.Unsubscribe(_wsEvent.Algorithms[0].AlgoId, _wsEvent.Algorithms[0].OnTargetMessage);
            statisticListener.Unsubscribe(_wsEvent.Algorithms[0].OnTradeStatisticMessage);
            _wsEvent.Algorithms[0].TargetMessageHandler -= _wsEvent.CompareStatisticAgainstTargetBook;

            _mmRest.StopPricer(_wsEvent.Algorithms[0].AlgoId);
            _mmRest.StopInstrument(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            _mmRest.DeleteAlgorithm(_wsEvent.Algorithms[0].AlgoId);
            Thread.Sleep(1000);

            Assert.AreEqual(true, _mmRest.GetInstrument(_wsEvent.Algorithms[0].AlgoId) == null, "Deleted algorithm doesn't equal to null");
        }
    }
}
