using System;
using System.Linq;
using System.Text;
using System.Drawing;
using AGV_V1._0;
using System.Collections.Generic;
using Astar;



namespace AGV_V1
{

   
   
    class MapNode
    {
        
      //  public   enum MapNodeType1 { img_Belt,img_Mid ,img_Road,img_Destination,img_ChargeStation,img_Obstacle,img_Scanner};

        private int x;               //节点的横坐标    
        private int y;               //节点的纵坐标     
        

        private int nodeCanUsed=-1;   //节点是否被占用,值表示被编号为几的小车占用，-1表示没有被占用
        public int NodeCanUsed
        {
            get
            {
                return nodeCanUsed;
            }
            set
            {
                nodeCanUsed = value;
            }
        }
       
       // public int LockNode = -1;  //-1节点没有被锁定，大于-1表示被锁定
        public int PassDifficulty { get; set; } //节点通行难度
        public int TraCongesIntensity { get; set; } //traffic congestion intensity 节点拥堵程度


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

        }
        
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public MapNode()
        { }
    }
}
