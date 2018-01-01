﻿using AGV_V1._0.Agv;
using AGV_V1._0.Algorithm;
using AGV_V1._0.DataBase;
using AGV_V1._0.Event;
using AGV_V1._0.Network.ThreadCode;
using AGV_V1._0.NLog;
using AGV_V1._0.Queue;
using AGV_V1._0.Server.APM;
using AGV_V1._0.Util;
using Astar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AGV_V1._0
{
    class VehicleManager : BaseThread
    {
        //新建两个全局对象  小车
        private static Vehicle[] vehicles;
        List<Vehicle> vFinished = new List<Vehicle>();
        private bool vehicleInited = false;
        private double moveCount = 0;//统计移动的格数，当前地图一格1.5米

        private static Random rand = new Random(1);//5,/4/4 //((int)DateTime.Now.Ticks);//随机数，随机产生坐标


        private static VehicleManager instance;
        public static VehicleManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new VehicleManager();
                }
                return instance;
            }
        }
        private VehicleManager()
        {

        }
        protected override string ThreadName()
        {
            return "VehicleManager";
        }
        protected override void Run()
        {
            Thread.Sleep(ConstDefine.STEP_TIME);

            if (vehicles == null)
            {
                return;
            }
            for (int vnum = 0; vnum < vehicles.Length; vnum++)
            {
                if (vehicles[vnum].CurState == State.cannotToDestination && vehicles[vnum].Arrive == false)
                {

                    // SearchRoute(vnum, false);

                    //vehicle[vnum].LockNode.Clear();
                    //vehicle[vnum].Arrive = false;
                    //int temp = vnum;
                    //vehicle[vnum].vehical_state = State.carried;
                    //SearchRoute(temp, false);

                    ////搜索路径
                    //int temp = vnum;
                    //Task.Factory.StartNew(() =>
                    //{
                    //    Thread.Sleep(500); 
                    //    SearchRoute(temp, false);  
                    //});  

                    //  vehicles[vnum].LockNode.Clear();
                    vehicles[vnum].Arrive = true;
                    vFinished.Add(vehicles[vnum]);
                    vehicles[vnum].Route.Clear();
                    string str = string.Format("小车" + vnum + ":({0}，{1})->({2}，{3})没有搜索到路径，", vehicles[vnum].BeginX, vehicles[vnum].BeginY, vehicles[vnum].EndX, vehicles[vnum].EndY);
                    OnShowMessage(this, new MessageEventArgs(str));
                    continue;
                }
                if (vehicles[vnum].Route == null || vehicles[vnum].Route.Count <= 1)
                {
                    continue;
                }
                //if (vehicle[vnum].Arrive == true && vehicle[vnum].CurNodeTypy() == NodeType.queueEntra)
                //{
                //    MyPoint nextEnd = ElecMap.Instance.CalculateScannerPoint(new MyPoint(vehicle[vnum].BeginX, vehicle[vnum].BeginY));
                //    //ElecMap.Instance.GetScannerPoint(vehicle[vnum].BeginX,vehicle[vnum].BeginY);
                //    if (nextEnd == null)
                //    {
                //        MessageBox.Show("排队入口点未找到对应的扫描仪");
                //        continue;
                //    }
                //    vehicle[vnum].EndX = nextEnd.X;
                //    vehicle[vnum].EndY = nextEnd.Y;
                //    vehicle[vnum].SetRoute(null);
                //    vehicle[vnum].GetLockNode().Clear();
                //    vehicle[vnum].EndLoc = "ScanArea";
                //    SearchRoute(vnum, false);
                //    continue;
                //}
                if (vehicles[vnum].Arrive == true && vehicles[vnum].CurState == State.carried)
                {
                    //vehicle[vnum].BeginX = vehicle[vnum].EndX;
                    //vehicle[vnum].BeginY = vehicle[vnum].EndY;
                    vehicles[vnum].CurState = State.unloading;
                    vFinished.Add(vehicles[vnum]);
                    vehicles[vnum].Route.Clear();
                    vehicles[vnum].LockNode.Clear();
                    continue;
                }
                if (vehicles[vnum].Arrive == true)
                {
                    //vehicle[vnum].BeginX = vehicle[vnum].EndX;
                    //vehicle[vnum].BeginY = vehicle[vnum].EndY;
                    //vehicle[vnum].vehical_state = State.unloading;
                    vFinished.Add(vehicles[vnum]);
                    vehicles[vnum].Route.Clear();
                    vehicles[vnum].LockNode.Clear();

                    continue;
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //if (vehicle[vnum].StopTime < ConstDefine.STOP_TIME)
                //{
                //    int nextVnum=vehicle[vnum].Stoped;                            
                //    if ( nextVnum>= 0 && nextVnum< vehicle.Length)
                //    {
                //        int nextVnumStopedBy = vehicle[nextVnum].Stoped;
                //        if (nextVnumStopedBy == vnum)//相向冲突
                //        {
                //            //重新搜索路径
                //            SearchRoute(vnum, true);
                //        }
                //        else//交叉冲突
                //        {
                //            vehicle[vnum].Vehicle_Move2(ElecMap.Instance);
                //            moveCount++;
                //        }
                //    }
                //}
                //else
                //{
                //    vehicle[vnum].Vehicle_Move2(ElecMap.Instance);
                //}


                if (vehicles[vnum].StopTime < 0)
                {
                    if (vehicles[vnum].CurNodeTypy() != MapNodeType.queuingArea && GetDirCount(vehicles[vnum].BeginX, vehicles[vnum].BeginY) > 1)
                    {
                        if (vehicles[vnum].Stoped > -1 && vehicles[vnum].Stoped < vehicles.Length)
                        {
                            vehicles[vehicles[vnum].Stoped].StopTime = 2;
                        }
                        //重新搜索路径
                        SearchRoute(vnum, true);
                        // Task.Factory.StartNew(() => SearchRoute(vnum), TaskCreationOptions.LongRunning);
                        //  vehicle[vnum].SearchRoute(ElecMap.Instance);
                        //路径搜索完 节点开锁 ElecMap.Instance.mapnode[vehicle[i].Route[vehicle[i].Virtual_tPtr].Point.X, vehicle[i].Route[vehicle[i].Virtual_tPtr].Point.Y].LockNode = -1;
                    }
                    vehicles[vnum].StopTime = 3;
                }
                else
                {
                    bool isMove = vehicles[vnum].Move(ElecMap.Instance);
                    if (isMove)
                    {
                        AGVServerManager.Instance.Send(MessageType.Move, vehicles[vnum].BeginX + ":" + vehicles[vnum].BeginY);
                        moveCount++;
                        OnShowMessage(string.Format("{0:N} 公里", (moveCount * 1.5) / 1000.0));
                    }
                    else
                    {
                        AGVServerManager.Instance.Send(MessageType.None, vehicles[vnum].BeginX + ":" + vehicles[vnum].BeginY);
                    }

                }
            }


            //sl.SendVehicleData(vehicle);
            // QueueData.Instance.AddMyQueueList(data);

            if (vFinished != null)
            {
                for (int i = 0; i < vFinished.Count; i++)
                {
                    FinishedQueue.Instance.AddMyQueueList(vFinished[i]);
                }
                vFinished.Clear();
            }


            //小车移动
            //for (int i = 0; i < vehicle.Length; i++)
            //{
            //    if (i == 5)
            //    {
            //        sb.Append(vehicle[1].Distance + " ");
            //    }
            //    if (vehicle[i].route == null)
            //    {
            //        continue;
            //    }
            //    if (vehicle[i].stoped==-1)
            //    {
            //       int num= vehicle[i].Vehicle_Move(ElecMap.Instance);
            //       if (num >= 0 && num < vehicle.Length)
            //       {
            //           vehicle[num].stoped= vehicle[i].v_num;                   
            //           vehicle[i].SearchRouteThread(ElecMap.Instance);                       
            //       }
            //    }
            //    else
            //    {
            //        //if (vehicle[i].routeIndex + 1 < vehicle[i].route.Count)
            //        //{
            //        //    int tx = (int)vehicle[i].route[vehicle[i].routeIndex + 1].X;
            //        //    int ty = (int)vehicle[i].route[vehicle[i].routeIndex + 1].Y;
            //        //    Boolean temp = ElecMap.Instance.canMoveToNode(vehicle[i], tx, ty);
            //        //    if (temp)
            //        //    {
            //        //        vehicle[i].stoped = -1;
            //        //    }
            //        //}
            //    }



            //else
            //{
            //    vehicle[i].stopTime = 3;

            //}

            //}
            // label8.Text = sb.ToString();

            // MapText();
            //  routeUtil.CheckeConflictNode(vehicle, ElecMap.Instance);
            //对窗体进行更新
            //this.Invalidate();

        }
        int GetDirCount(int row, int col)
        {
            int dir = 0;
            if (ElecMap.Instance.mapnode[row, col].RightDifficulty < MapNode.MAX_ABLE_PASS)
            {
                dir++;
            }
            if (ElecMap.Instance.mapnode[row, col].LeftDifficulty < MapNode.MAX_ABLE_PASS)
            {
                dir++;
            }
            if (ElecMap.Instance.mapnode[row, col].DownDifficulty < MapNode.MAX_ABLE_PASS)
            {
                dir++;
            }
            if (ElecMap.Instance.mapnode[row, col].UpDifficulty < MapNode.MAX_ABLE_PASS)
            {
                dir++;
            }
            return dir;
        }
        /// <summary>
        /// 初始化小车
        /// </summary>
        public void InitialVehicle()
        {
            vehicleInited = false;
            //初始化小车位置

            if (null == FileUtil.sendData || FileUtil.sendData.Length < 1)
            {
                throw new ArgumentNullException();
            }
            int vehicleCount = FileUtil.sendData.Length;
            vehicles = new Vehicle[vehicleCount];
            int m = 0;
            for (int i = 0; i < vehicleCount; i++)
            {
                vehicles[i] = new Vehicle(FileUtil.sendData[i].BeginX, FileUtil.sendData[i].BeginY, i, false, Direction.Right);
                MyPoint endPoint = RouteUtil.RandPoint(ElecMap.Instance);
                //vehicle[i].endX = (int)endPoint.col;
                //vehicle[i].endY = (int)endPoint.row;

                MyPoint mp = SqlManager.Instance.GetVehicleCurLocationWithId(i);
                if (mp != null)
                {
                    vehicles[i].BeginX = mp.X;
                    vehicles[i].BeginY = mp.Y;
                }

                m++;
                ElecMap.Instance.mapnode[FileUtil.sendData[i].BeginX, FileUtil.sendData[i].BeginY].NodeCanUsed = i;

                vehicles[i].Speed = 0;
                vehicles[i].MaxSpeed = 4;
                vehicles[i].Acceleration = 1;

                int R = rand.Next(20, 225);
                int G = rand.Next(20, 225);
                int B = rand.Next(20, 225);
                vehicles[i].pathColor = Color.FromArgb(80, R, G, B);
                vehicles[i].showColor = Color.FromArgb(255, R, G, B);
            }

            vehicleInited = true;
            ////把小车所在的节点设为占用状态
            RouteUtil.VehicleOcuppyNode(ElecMap.Instance, vehicles);

        }

        public void RandomMove(int Id)
        {           
            MyPoint mpEnd = RouteUtil.RandRealPoint(ElecMap.Instance);
            SendData sd = new SendData(Id, vehicles[Id].BeginX, vehicles[Id].BeginY, mpEnd.X, mpEnd.Y);
            sd.Arrive = false;
            sd.EndLoc = "rest";

            SearchRouteQueue.Instance.AddMyQueueList(new SearchData(sd, false));


        }
        void SearchRoute(int num, bool isResarch)
        {
            SendData td = new SendData();
            td.Num = num;
            td.BeginX = vehicles[num].BeginX;
            td.BeginY = vehicles[num].BeginY;
            td.EndX = vehicles[num].EndX;
            td.EndY = vehicles[num].EndY;
            td.Arrive = false;
            td.EndLoc = vehicles[num].EndLoc;
            td.StartLoc = vehicles[num].StartLoc;
            td.State = vehicles[num].CurState;


            //if (!ElecMap.Instance.IsSpecialArea(td.BeginX, td.BeginY) && ElecMap.Instance.IsScanner(td.EndX, td.EndY))
            //{
            //    MessageBox.Show("起点：" + td.BeginX + "," + td.BeginY + "" + "终点：" + td.EndX + "," + td.EndY);
            //}
            SearchRouteQueue.Instance.AddMyQueueList(new SearchData(td, isResarch));

            //Task.Factory.StartNew(() => vehicle[num].SearchRoute(Elc), TaskCreationOptions.LongRunning);
        }

        private static readonly object vehicleLock = new object();
        public Vehicle[] GetVehicles()
        {
            //Vehicle[] v = null;
            //lock (vehicleLock)
            //{
            //    if (vehicles != null)
            //    {
            //        v = new Vehicle[ConstDefine.g_VehicleCount];
            //        for (int i = 0; i < vehicles.Length; i++)
            //        {
            //            v[i] = vehicles[i].CloneDeep();
            //        }
            //    }
            //}
            //return v;
            return vehicles;
        }


    }

}
