using AGVSocket.Network.EnumType;
using AGVSocket.Network.MyException;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVSocket.Network.Packet
{
    class AgvResponsePacket:ReceiveBasePacket
    {
        private byte responseType; //需要应答报文类型
        private ResponseState responseState;//需要应答报文状态

        #region Properties
        public byte ResponseType { get { return responseType; } }
        public ResponseState ResponseState {
            get { return responseState; }
            private set {
              
            } 
        
        }
        #endregion
        public AgvResponsePacket(byte[] data)
            : base("AgvResponsePacket", data)
        {
                this.responseType = data[7];
                this.responseState = (ResponseState)data[8];
            
        }
      

        public override void Receive()
        {
            Debug.WriteLine("小车{0}应答报文，应答类型{1},是否正确收到：{2},序列号：{3}", this.AgvId,this.responseType, this.responseState,this.SerialNum);
        }

        public override byte NeedLen()
        {
            return 10;
        }
    }
}
