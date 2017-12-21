using System;
using System.Linq;
using System.Text;
using System.Drawing;
using AGV_V1._0;
using System.Collections.Generic;
using Astar;
using System.Collections.Concurrent;



namespace AGV_V1
{

   
   
    class MapNode
    {
        
      //  public   enum MapNodeType1 { img_Belt,img_Mid ,img_Road,img_Destination,img_ChargeStation,img_Obstacle,img_Scanner};

        private int x;               //节点的横坐标    
        private int y;               //节点的纵坐标     
        

        private int nodeCanUsed=-1;   //节点是否被占用,值表示被编号为几的小车占用，-1表示没有被占用
        private static readonly object canUsedLock = new object();
        public int NodeCanUsed
        {
            get
            {
                lock (canUsedLock)
                {
                    return nodeCanUsed;
                }
            }
            set
            {
                lock (canUsedLock)
                {
                    nodeCanUsed = value;
                }
            }
        }
        private static readonly object crossLock = new object();
        private List<int> crossCount = new List<int>();//各个时间点经过当前点的数量
        public List<int> CrossCount
        {
            get
            {
                lock (crossLock)
                {
                    return crossCount;
                }
            }
            set
            {
                lock (crossLock)
                {
                    crossCount = value;
                }
            }
        }

       
       // public int LockNode = -1;  //-1节点没有被锁定，大于-1表示被锁定
        public List<int> vehiclePriority{get;set;} //通过节点的小车优先级序列如{1,4,6},数字为小车编号；


        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }


        public bool IsAbleCross    //节点可不可达,true表示可走，false表示不可走 
        {
            get;
            set;
        }
        public MapNodeType Type    //节点可不可达,true表示可达，false表示不可达 
        {
            get;
            set;
        }

        public int Id  //节点的编号
        { get; private set; }
        public int X
        {
            get { return x; }
            set { x = value; }
        }
        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// 含参构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reachable"></param>
        public MapNode( int id, bool node_type)
        {
            this.Id = id;
            this.IsAbleCross = node_type;
            this.vehiclePriority = new List<int>();

        }
        
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public MapNode()
        { }
    }
}
