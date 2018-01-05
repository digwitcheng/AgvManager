using AGV_V1._0.Event;
using AGV_V1._0.Network.Messages;
using AGV_V1._0.NLog;
using AGV_V1._0.Util;
using Cowboy.Sockets;
using CowboyTest.Server.APM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV_V1._0.Server.APM
{
    class AGVServerManager : ServerManager
    {
        private static AGVServerManager instance;
        public static AGVServerManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new AGVServerManager();
                }
                return instance;
            }
        }
        private AGVServerManager() { }

        public override void server_ClientConnected(object sender, TcpClientConnectedEventArgs e)
        {
            string str = string.Format("TCP client {0} has connected.", e.Session.RemoteEndPoint);
            Console.WriteLine(str);
            OnMessageEvent(this, new MessageEventArgs(str));

            //string pathAgv = ConstString.AGV_PATH;
            //SendTo(e.Session.SessionKey, MessageType.AgvFile, pathAgv, false);
        }

        public override void server_ClientDisconnected(object sender, TcpClientDisconnectedEventArgs e)
        {
            string str = string.Format("TCP client {0} has disconnected.", e.Session.RemoteEndPoint);
            Console.WriteLine(str);
            OnMessageEvent(this, new MessageEventArgs(str));
        }

        public override void server_ClientDataReceived(object sender, TcpClientDataReceivedEventArgs e)
        {
            MessageType type = (MessageType)e.Data[e.DataOffset];
            var text = Encoding.UTF8.GetString(e.Data, e.DataOffset + 1, e.DataLength - 1);

            //
            BaseMessage bm = BaseMessage.Factory(type, text);
            bm.ShowMessage += OnMessageEvent;
            bm.DataMessage += OnDataMessageEvent;
            bm.ReLoad += ReLoadDel;
            bm.Receive();

        }

        public void Send(MessageType type, string json)
        {
            try
            {
                //if (false == isSendFile)
                //{
                SendTo("", type, json, true);
                //}
            }
            catch
            {
                //throw;
            }

        }
    }
}
