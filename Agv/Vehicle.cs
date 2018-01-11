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
using AGV_V1._0.DataBase;
using AGVSocket.Network;
using AGVSocket.Network.EnumType;
using AGV_V1._0.NLog;

namespace AGV_V1._0
{

    class Vehicle
    {
        public AgvInfo agvInfo { get; set; }
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

        public Color pathColor = Color.Red;
        public Color showColor = Color.Pink;


        private int realTPtr;
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


        public Vehicle(int x, int y, int v_num, bool arrive, Direction direction)
        {
            this.BeginX = x;
            this.BeginY = y;
            this.X = y * ConstDefine.g_NodeLength;
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
                DrawUtil.FillRectangle(g, showColor, rect);

                PointF p = new PointF((int)((BeginY) * ConstDefine.g_NodeLength), (int)((BeginX) * ConstDefine.g_NodeLength));
                DrawUtil.DrawString(g, this.Id, ConstDefine.g_NodeLength / 2, Color.Black, p);


            }
        }
        public MapNodeType CurNodeTypy()
        {
            return ElecMap.Instance.mapnode[BeginX, BeginY].Type;
        }
        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="Elc"></param>
        /// <returns>是否移动了</returns>
        public bool Move(ElecMap Elc)
        {
            lock (RouteLock)
            {
                //if (BeginX == EndX && BeginY == EndY)
                //{
                //    Elc.mapnode[BeginX, BeginY].NodeCanUsed = this.v_num;
                //    Arrive = true;
                //    return;
                //}
                if (route == null || route.Count < 1)
                {
                    return false;
                }
                if (!ShouldMove(BeginX, BeginY))
                {
                    return false;
                }

                if (tPtr == 0)// ConstDefine.FORWORD_STEP)
                {

                    for (VirtualTPtr = 1; VirtualTPtr < ConstDefine.FORWORD_STEP; VirtualTPtr++)
                    {
                        if (tPtr + VirtualTPtr <= route.Count - 1)
                        {
                            int tx = (int)route[VirtualTPtr].X;
                            int ty = (int)route[VirtualTPtr].Y;
                            int temp = -1;// Elc.mapnode[tx, ty].NodeCanUsed;
                            if (temp > -1)
                            {
                                Stoped = temp;
                                StopTime--;
                                return false;
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

                }
                else if (tPtr > 0)
                {

                    if (VirtualTPtr <= route.Count - 1)
                    {
                        int tx = (int)route[VirtualTPtr].X;
                        int ty = (int)route[VirtualTPtr].Y;
                        int temp = -1;// Elc.mapnode[tx, ty].NodeCanUsed;
                        if (temp > -1)
                        {
                            Stoped = temp;
                            StopTime--;
                            return false;
                        }
                        else
                        {
                            //if (ShouldMove(route[tPtr + 1].X, route[tPtr + 1].Y))
                            //{
                            Elc.mapnode[tx, ty].NodeCanUsed = this.Id;
                            StopTime = ConstDefine.STOP_TIME;
                            Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                            tPtr++;
                            VirtualTPtr++;
                            //}
                            //else
                            //{
                            //    return false;
                            //}
                        }

                    }
                    else
                    {
                        Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                        StopTime = ConstDefine.STOP_TIME;
                        tPtr++;
                    }


                }
                BeginX = route[tPtr].X;
                BeginY = route[tPtr].Y;
                return true;
                //if (tPtr >= route.Count)
                //{
                //    Elc.mapnode[route[route.Count - 1].X, route[route.Count - 1].Y].NodeCanUsed = this.Id;
                //    BeginX = route[route.Count - 1].X;
                //    BeginY = route[route.Count - 1].Y;
                //    Arrive = true;
                //    return true;
                //}
                //if (ShouldMove(route[tPtr].X, route[tPtr].Y))
                //{
                //    BeginX = route[tPtr].X;
                //    BeginY = route[tPtr].Y;
                //    return true;
                //}
                //else
                //{
                //    //int tx = (int)route[VirtualTPtr].X;
                //    //int ty = (int)route[VirtualTPtr].Y;
                //    //Elc.mapnode[tx, ty].NodeCanUsed = -1;
                //    //Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = -1;
                //    //tPtr--;
                //    //VirtualTPtr--;
                //    //tx = (int)route[VirtualTPtr].X;
                //    //ty = (int)route[VirtualTPtr].Y;
                //    //Elc.mapnode[tx, ty].NodeCanUsed = this.Id;
                //    //Elc.mapnode[route[tPtr].X, route[tPtr].Y].NodeCanUsed = this.Id;

                //    tPtr--;
                //    VirtualTPtr--;
                //    return false;
                //}

            }
        }

        public bool SimpleMove()
        {
            lock (RouteLock)
            {
                if (route == null || route.Count < 1)
                {
                    return false;
                }
                if (tPtr < route.Count)
                {
                    if (ShouldMove(route[tPtr].X, route[tPtr].Y))
                    {
                        tPtr++;
                        BeginX = route[tPtr].X;
                        BeginY = route[tPtr].Y;
                        return true;
                    }
                    return false;
                }
                else
                {
                    this.Arrive = true;
                    return false;
                }
            }
        }
         bool ShouldMove(int nextX, int nextY)
        {
            if (agvInfo == null)
            {
                return false;
            }
            if (agvInfo.Alarm != AlarmState.Normal)
            {
                return false;
            }
            //if (agvInfo.Electricity < 20)
            //{
            //    return false;
            //}
            int tempX = (int)Math.Round(agvInfo.CurLocation.CurNode.X/1000.0);
            int tempY = (int)Math.Round(agvInfo.CurLocation.CurNode.Y/1000.0);
            if (Math.Abs(nextX - tempX) < ConstDefine.DEVIATION + ConstDefine.FORWORD_STEP - 1 && Math.Abs(nextY - tempY) < ConstDefine.DEVIATION)
            {
                return true;
            }
            if (Math.Abs(nextX - tempX) < ConstDefine.DEVIATION && Math.Abs(nextY - tempY
                ) < ConstDefine.DEVIATION + ConstDefine.FORWORD_STEP - 1)
            {
                return true;
            }
            return false;

        }
        public bool EqualWithRealLocation(int srcX, int srcY)
        {
            if (agvInfo == null)
            {
                return false;
            }
            if (agvInfo.Alarm != AlarmState.Normal)
            {
                return false;
            }
            //if (agvInfo.Electricity < 20)
            //{
            //    return false;
            //}
            int tempX = (int)Math.Round(agvInfo.CurLocation.CurNode.X / 1000.0);
            int tempY = (int)Math.Round(agvInfo.CurLocation.CurNode.Y / 1000.0);
            if (Math.Abs(srcX - tempX) < ConstDefine.DEVIATION && Math.Abs(srcY - tempY) < ConstDefine.DEVIATION)
            {
                return true;
            }
            if (Math.Abs(srcX - tempX) < ConstDefine.DEVIATION && Math.Abs(srcY - tempY
                ) < ConstDefine.DEVIATION)
            {
                return true;
            }
            return false;

        }
    }
}
