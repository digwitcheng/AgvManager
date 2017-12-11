using AGV_V1._0.Agv;
using Astar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astar 
{
    struct Node
    {
        public int x;               //节点的横坐标    
        public int y;               //节点的纵坐标
        public bool node_Type;      //节点可不可达,true表示可达，false表示不可达  
        public int adjoinNodeCount;  //邻接点的个数
        public int value;           //节点的值
        public Direction direction; //当前节点的方向
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
    }
}
