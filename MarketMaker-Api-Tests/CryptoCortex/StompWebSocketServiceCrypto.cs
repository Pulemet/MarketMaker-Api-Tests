using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MarketMaker.Api.Subscriptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSocketSharp;

namespace MarketMaker_Api_Tests.CriptoCortex
{
    public class StompWebSocketServiceCrypto : StompWebSocketService
    {
        protected const string statusPrefix = "status:";
        protected const int statusPrefixLength = 7;

        public StompWebSocketServiceCrypto(string url, string token)
            : base(url, token) { }

        public void SendMessage(string message)
        {
            //Debug.WriteLine("SEND\r\n" + message + "\r\n\r\n\0");
            _socket.Send("SEND\r\n" + message + "\r\n\r\n\0");
        }

        protected override void SocketOnOnMessage(object sender, MessageEventArgs e)
        {
            //Debug.WriteLine(e.Data);
            int subIdStartIndex = e.Data.IndexOf(subIdPrefix);
            int msgIdStartIndex = e.Data.IndexOf(msgIdPrefix);
            int statusStartIndex = e.Data.IndexOf(statusPrefix);
            if (subIdStartIndex > 0 && msgIdStartIndex > 0 && statusStartIndex > 0)
            {
                int s = statusStartIndex + statusPrefixLength;
                string status = e.Data.Substring(s, 3);

                s = subIdStartIndex + subIdPrefixLength;
                string subId = e.Data.Substring(s, msgIdStartIndex - s - 1/*-1 for trailing \n*/).Replace("\\c", ":");

                Action<string> action;
                if (_subscribers.TryGetValue(subId, out action))
                    action(status);
            }
            else
            {
                Debug.WriteLine("Message can't be parsed, ignore.");
            }
        }

        public override string GetSubscribeMessage(string subscriptionId, string topic)
	    {
            return $"SUBSCRIBE\r\nX-Deltix-Nonce:{ConvertToUnixTimestamp(DateTime.Now)}\r\nid:{subscriptionId}\r\ndestination:{topic}\r\n\r\n\0";
        }

	    public override string GetConnectMessage(string token)
	    {
            return $"CONNECT\r\nAuthorization:{token}\r\nheart-beat:{PingIntervalMsec},{PingIntervalMsec}\r\naccept-version:1.2\r\n\r\n\0";
        }
    }
}
