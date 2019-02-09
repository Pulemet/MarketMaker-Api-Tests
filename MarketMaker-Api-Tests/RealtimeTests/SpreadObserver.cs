using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.Helper;
using System.Timers;

namespace MarketMaker_Api_Tests
{
    public class SpreadObserver
    {
        public SpreadObserver()
        {

        }
        public SpreadObserver(SubscriptionFactory subscriptions, AlgorithmInfo algo, AlgorithmInfo origQuotesAlgo)
        {
            OrigQuotesAlgo = origQuotesAlgo;
            Algo = algo;
            Subscriptions = subscriptions;
        }
        public AlgorithmInfo Algo { get; set; }
        public AlgorithmInfo OrigQuotesAlgo { get; set; }
        public SubscriptionFactory Subscriptions { get; set; }
        protected System.Timers.Timer _pingTimer;

        public void Observe()
        {
            var quotesSubscription = Subscriptions.CreateQuotesSubscription();
            var targetBookListener = Subscriptions.CreateTargetMarketDataSubscription();

            quotesSubscription.Subscribe(OrigQuotesAlgo.AlgoId, OrigQuotesAlgo.OnQuoteMessage);
            quotesSubscription.Subscribe(Algo.AlgoId, Algo.OnQuoteMessage);
            targetBookListener.Subscribe(Algo.AlgoId, Algo.OnTargetMessage);

            _pingTimer = new System.Timers.Timer(3000);
            _pingTimer.Elapsed += _pingTimer_Elapsed;
            _pingTimer.Start();

            Console.ReadLine();

            _pingTimer.Elapsed -= _pingTimer_Elapsed;
            _pingTimer.Stop();
            quotesSubscription.Unsubscribe(OrigQuotesAlgo.AlgoId, OrigQuotesAlgo.OnQuoteMessage);
            quotesSubscription.Unsubscribe(Algo.AlgoId, Algo.OnQuoteMessage);
            targetBookListener.Unsubscribe(Algo.AlgoId, Algo.OnTargetMessage);
        }

        protected void _pingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _pingTimer.Stop();
            Console.Clear();

            try
            {
                PrintSpread();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _pingTimer.Start();
            }
        }

        protected void PrintSpread()
        {
            double quoteSpread = AlgorithmInfo.CalculateSpread(Algo.AlgoDictionary[BookType.QUOTE].Entries);
            double targetSpread = AlgorithmInfo.CalculateSpread(Algo.AlgoDictionary[BookType.TARGET].Entries);
            double paramSpread = AlgorithmInfo.GetSpreadFromParams(OrigQuotesAlgo.AlgoDictionary[BookType.QUOTE].Entries, Algo.PricerConfigInfo.MinSpread);
            Console.WriteLine("Quote Spread: {0}, Target Spread: {1}, Parse spread: {2}, MinSpread: {3}",
                quoteSpread, targetSpread, paramSpread, Algo.PricerConfigInfo.MinSpread);
        }

    }
}
