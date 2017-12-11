using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Data;
using AGV_V1._0.Properties;
using Astar;
using System.Threading;
using System.Collections.Concurrent;
using AGV_V1._0.Algorithm;
using System.Threading.Tasks;
using System.Windows.Forms;
using AGV_V1._0.Agv;
using AGV_V1._0.Util;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AGV_V1._0
{
    [Serializable]
    class Vehicle
    {

        private int routeIndex = 0;
        public int RouteIndex
        {
            get { return routeIndex; }
            set
            {
                if (route != null)
                {
                    if (value > route.Count - 1)
                    {
                        value = route.Count - 1;
                    }
                    if (value < 0)
                    {
                        value = 0;
                    }
                }
                else
                {
                    value = 0;
                }

                routeIndex = value;
            }
        }

        private int stopTime = ConstDefine.STOP_TIME;//0406 等待时长，超过则重新规划路线；
        public int StopTime
        {
            get { return stopTime; }
            set { stopTime = value; }
        }

        private int stoped = -1;//大于0表示被某个小车锁死，停止了。
        public int Stoped { get; set; }

        //判断小车是否到终点
        public bool Arrive
        {
            get;
            set;
        }

        //小车编号
        public int Id
        {
            get;
            private set;
        }

        public State CurState
        {
            get;
            set;
        }

        //public List<myPoint> route;//起点到终点的路线
        //public ConcurrentDictionary<int, MyLocation> Route { get; set; }//起点到终点的路线, 键表示时钟指针
        private static Object RouteLock = new Object();
        //private  int LockNode = -1;  //-1节点没有被锁定，大于-1表示被锁定


        private static object lockNodeLock = new object();
        private List<MyPoint> lockNode = new List<MyPoint>();
        public List<MyPoint> LockNode
        {
            get
            {
                return lockNode;
            }
            set
            {
                this.lockNode = value;

            }
            //get
            //{
            //    lock (lockNodeLock)
            //    {
            //        return lockNode;
            //    }
            //}
            //set
            //{
            //    lock (lockNodeLock)
            //    {
            //        this.lockNode = value;

            //    }
            //}
        }
        private List<MyPoint> route = new List<MyPoint>();
        public List<MyPoint> Route
        {
            get
            {
                lock (RouteLock)
                {
                    return route;
                }
            }
            set
            {
                lock (RouteLock)
                {
                    this.route = value;

                }
            }
        }
        //起点到终点的路线, 键表示时钟指针
        public int cost;   //截止到当前时间，总共的花费

        public Direction Dir { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int BeginX { get; set; }
        public int BeginY { get; set; }
        public int DestX { get; set; }
        public int DestY { get; set; }

        //  public float Distance;//上一个节拍所走的距离；
        // public int stopTime;//停留时钟数

        public MyPoint Location
        {
            get;
            set;
        }
        //小车的电量
        public int Electricity
        {
            get;
            set;
        }

        //小车的加速度
        public float Acceleration
        {
            get;
            set;
        }

        //小车的速度
        public float Speed
        {
            get;
            set;
        }

        //小车的最大速度
        public float MaxSpeed
        {
            get;
            set;
        }
        //车的横坐标
        public int X
        {
            get;
            set;
        }

        //车的纵坐标
        public int Y
        {
            get;
            set;
        }

        public Color pathColor=Color.Red;
        public Color showColor=Color.Pink;


        public int VirtualTPtr
        {
            get;
            set;
        }

        private int tPtr;
        public int TPtr
        {
            get
            {
                return tPtr;
            }
            set
            {
                if (value < 0)
                {
                    tPtr = 0;
                }
                else
                {
                    tPtr = value;
                }
            }

        }//时钟指针

        public string StartLoc
        {
            get;
            set;
        }
        public string EndLoc
        {
            get;
            set;
        }

        /// <summary>
        /// 构造函数，初始化所有变量
        /// </summary>
        /// <param name="speed">速度</param>
        /// <param name="electricity">电量</param>
        /// <param name="acceleration">加速度</param>
        /// <param name="maxspeed">最大速度</param>
        public Vehicle(int x, int y, int speed, int electricity, int acceleration, int maxspeed, Direction direction)
        {
            this.BeginX = x;
            this.BeginY = y;
            this.X = x;
            this.Y = y;
            this.Speed = speed;
            this.MaxSpeed = maxspeed;

            this.Electricity = electricity;
            this.Acceleration = acceleration;
            this.CurState = State.Free;
            this.Dir = direction;
        }

        public Vehicle(int x, int y, int v_num, bool arrive, Direction direction)
        {
            this.BeginX = x;
            this.BeginY = y;
            this.X = y * ConstDefine.g_NodeLength ;
            this.Y = x * ConstDefine.g_NodeLength;
            this.Id = v_num;
            this.Arrive = arrive;
            this.Dir = direction;
        }
        public Vehicle()
        {

        }
        //public int TPtr
        //{
        //    get { return tPtr; }
        //    set { tPtr = value; }
        //}

        /// <summary>
        /// 重绘函数
        /// </summary>
        /// <param name="g"></param>
        public void Draw(Graphics g)
        {

            lock (RouteLock)
            {

                Rectangle rect = new Rectangle(BeginY * ConstDefine.g_NodeLength, (int)BeginX * ConstDefine.g_NodeLength, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2);
                DrawUtil.FillRectangle(g,showColor,rect);

                PointF p = new PointF((int)((BeginY) * ConstDefine.g_NodeLength), (int)((BeginX) * ConstDefine.g_NodeLength));
                DrawUtil.DrawString(g, this.Id, ConstDefine.g_NodeLength / 2, Color.Black, p);

               
            }
        }
        public MapNodeType CurNodeTypy()
        {
            return ElecMap.Instance.mapnode[BeginX, BeginY].Type;
        }
        public void Move(ElecMap Elc)
        {
            lock (RouteLock)
            {
                //if (BeginX == EndX && BeginY == EndY)
                //{
                //    Elc.mapnode[BeginX, BeginY].NodeCanUsed = this.v_num;
                //    Arrive = true;
                //    return;
                //}
                if (route == null || route.Count < 2)
                {
                    return;
                }
                if (tPtr == 0)// ConstDefine.FORWORD_STEP)
                {

                    for (VirtualTPtr = 1; VirtualTPtr < ConstDefine.FORWORD_STEP; VirtualTPtr++)
                    {
                        if (tPtr + VirtualTPtr <= route.Count - 1)
                        {
                            int tx = (int)route[VirtualTPtr].X;
                            int ty = (int)route[VirtualTPtr].Y;
                            // Boolean temp = Elc.canMoveToNode(this, tx, ty);
                            int temp = Elc.mapnode[tx, ty].NodeCanUsed;
                            if (temp > -1)
                            {
                                Stoped = temp;
                                StopTime--;
                                return;
                            }
                            else
                            {
                                Elc.mapnode[tx, ty].NodeCanUsed = this.Id;
                            }
                        }
                    }
                    Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                    StopTime = ConstDefine.STOP_TIME;
                    tPtr++;

                    BeginX = route[tPtr].X;
                    BeginY = route[tPtr].Y;

                }
                if (tPtr > 0)
                {

                    if (tPtr >= route.Count - 1)
                    {
                        Elc.mapnode[route[route.Count - 1].X, route[route.Count - 1].Y].NodeCanUsed = this.Id;
                        Arrive = true;
                        return;
                    }

                    if (VirtualTPtr <= route.Count - 1)
                    {
                        int tx = (int)route[VirtualTPtr].X;
                        int ty = (int)route[VirtualTPtr].Y;
                        // Boolean temp = Elc.canMoveToNode(this, tx, ty);
                        int temp = Elc.mapnode[tx, ty].NodeCanUsed;
                        if (temp > -1)
                        {
                            Stoped = temp;
                            StopTime--;
                            return;
                        }
                        else
                        {
                            Elc.mapnode[tx, ty].NodeCanUsed = this.Id;
                            StopTime = ConstDefine.STOP_TIME;
                            Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                            tPtr++;
                            VirtualTPtr++;
                        }

                    }
                    else
                    {
                        Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                        StopTime = ConstDefine.STOP_TIME;
                        tPtr++;
                    }

                    //    else
                    //    {
                    //        Arrive = true;
                    //    }

                    //tPtr++;

                    BeginX = route[tPtr].X;
                    BeginY = route[tPtr].Y;
                }
            }
        }

        public Vehicle CloneDeep() //深clone
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as Vehicle;
        }     
    }
}
