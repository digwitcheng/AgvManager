using AGV_V1._0;
using AGV_V1._0.Agv;
using AGVSocket.Network.EnumType;
using AGVSocket.Network.MyException;
using DataBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVSocket.Network.Packet
{
    class DoneReplyPacket:ReceiveBasePacket
    {
        private OprationState doneStyle;  //操作完成标识
        public OprationState DoneStyle { get; set; }
        //private const byte NEEDLEN = 9;
        public DoneReplyPacket(byte[] data)
            : base("DoneReplyPacket", data) 
        {
            this.doneStyle = (OprationState)data[7];
            uint a = 0xB1;
        }


        public override void Receive()
        {
            Debug.WriteLine("完成标识:{0},消息是否正确：{1},序列号:{2}",doneStyle, this.IsCheckSumCorrect,this.SerialNum);
           this.ReceiveResponse();

           if (doneStyle == OprationState.Loaded)
           {
                   //string str = string.Format("update Vehicle set TrayState={0} where Id={1}",
                   //    (byte)0x05,
                   //    this.AgvId);
                   //UpdataSqlThread.Instance.Update(str);
              // Form1.cm.Send(client20710711.MessageType.reStart, this.AgvId+"");

               int id = Convert.ToInt32(this.AgvId);
               VehicleManager.Instance.GetVehicles()[id].CurState = State.Free;
           }

        }

        public override byte NeedLen()
        {
            return 9;
        }
    }
}
