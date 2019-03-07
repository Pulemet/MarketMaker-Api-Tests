using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MarketMaker.Api.Subscriptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using WebSocketSharp;

namespace MarketMaker_Api_Tests.CriptoCortex
{
    public class StompWebSocketServiceCrypto : StompWebSocketService
    {
        protected const string statusPrefix = "status:";
        protected const int statusPrefixLength = 7;
        protected const string corrIdPrefix = "correlation-id:";
        protected const int corrIdPrefixLength = 15;
        protected Dictionary<string, Action<string>> _fastSubscribers = new Dictionary<string, Action<string>>();

        public StompWebSocketServiceCrypto(string url, string token)
            : base(url, token) { }

        public void SendMessage<T>(string additionalHeaders, string topic, T body, Action<string> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            string correlationId = GetRandomString(10);

            _socket.Send(GetSendMessage(correlationId, topic, body));
            _fastSubscribers.Add(correlationId, action);
        }

        protected override void SocketOnOnMessage(object sender, MessageEventArgs e)
        {
            int corrIdStartIndex = e.Data.IndexOf(corrIdPrefix);
            int bodyStartIndex = e.Data.IndexOf("\n\n");
            int subIdStartIndex = e.Data.IndexOf(subIdPrefix);
            int msgIdStartIndex = e.Data.IndexOf(msgIdPrefix);
            int statusStartIndex = e.Data.IndexOf(statusPrefix);
            if (subIdStartIndex > 0 && msgIdStartIndex > 0 && statusStartIndex > 0)
            {
                int s = statusStartIndex + statusPrefixLength;
                string status = e.Data.Substring(s, 3);

                s = subIdStartIndex + subIdPrefixLength;
                string subId = e.Data.Substring(s, msgIdStartIndex - s - 1/*-1 for trailing \n*/).Replace("\\c", ":");

                string message = status + e.Data.Substring(bodyStartIndex);

                Action<string> action;

                if (corrIdStartIndex > 0)
                {
                    s = corrIdStartIndex + corrIdPrefixLength;
                    string corrId = e.Data.Substring(s, 10);
                    if (_subscribers.ContainsKey(subId) && _fastSubscribers.TryGetValue(corrId, out action))
                    {
                        action(message);
                        _fastSubscribers.Remove(corrId);
                    }
                }
                else if (_subscribers.TryGetValue(subId, out action))
                    action(message);
            }
        }

        public string GetSendMessage<T>(string correlationId, string topic, T body)
        {
            return String.Format("SEND\r\ncorrelation-id:{0}\r\nX-Deltix-Nonce:{2}\r\ndestination:{1}\r\n\r\n{3}\r\n\r\n\0",
                correlationId, topic,
                StompWebSocketService.ConvertToUnixTimestamp(DateTime.Now),
                JsonConvert.SerializeObject(body));
        }

        public override string GetSubscribeMessage(string subscriptionId, string topic)
	    {
            return $"SUBSCRIBE\r\nX-Deltix-Nonce:{ConvertToUnixTimestamp(DateTime.Now)}\r\nid:{subscriptionId}\r\ndestination:{topic}\r\n\r\n\0";
        }

	    public override string GetConnectMessage(string token)
	    {
            return $"CONNECT\r\nAuthorization:{token}\r\nheart-beat:{PingIntervalMsec},{PingIntervalMsec}\r\naccept-version:1.2\r\n\r\n\0";
        }

        public static string GetRandomString(int length)
        {
            Byte[] seedBuffer = new Byte[4];
            using (var rngCryptoServiceProvider = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(seedBuffer);
                string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                Random random = new Random(System.BitConverter.ToInt32(seedBuffer, 0));
                return new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }
    }
}
