using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVSocket.Network
{
    class CellPoint
    {
        private UInt32 x;
        private UInt32 y;

        public UInt32 Y
        {
            get { return y; }
            set { y = value; }
        }

        public UInt32 X
        {
            get { return x; }
            set { x = value; }
        }

        public CellPoint(CellPoint poUInt32)
        {
            if (poUInt32 == null)
            {
                return;
            }
            this.x = poUInt32.x;
            this.y = poUInt32.y;

        }
        public CellPoint(UInt32 x, UInt32 y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
