using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using Astar;
using AGV_V1._0.Properties;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using AGV_V1._0.Algorithm;
using Newtonsoft.Json;
using AGV_V1._0.Queue;
using AGV_V1._0.ThreadCode;
using AGV_V1._0.Event;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Net;
using CowboyTest.Server.APM;
using AGV_V1._0.Server.APM;
using System.Diagnostics;
using AGV_V1._0.NLog;
using AGV_V1._0.Util;
using AGV_V1._0.Network.Messages;
using AGV_V1._0.Network.ThreadCode;
using AGV_V1._0.DataBase;
using System.Collections.Concurrent;

namespace AGV_V1._0
{
    public partial class Form1 : Form
    {
        private Bitmap surface = null;
        private Graphics g = null;
        //private Graphics gg = null;
        GuiServerManager gm;
        TaskServerManager tm;
        AGVServerManager am;
        public static Random rand = new Random(5);//5,/4/4 //((int)DateTime.Now.Ticks);//随机数，随机产生坐标
        private ElecMap Elc = ElecMap.Instance;

        private System.Threading.Timer timer;

        public Form1()
        {
            InitializeComponent();
            InitServer();//初始化服务器
            ConnectDataBase(); //连接数据库

            InitUiView();//绘制界面
            StartThread();//启动发送，接收，搜索等线程
            InitialSystem();//初始化小车


        }

        private void ConnectDataBase()
        {
           SqlManager.Instance.Connect2DataBase();
        }
        void StartThread()
        {
            TaskSendThread.Instance.Start();
            TaskReceiveThread.Instance.Start();
            GuiSendThread.Instance.Start();
            SearchRouteThread.Instance.Start();
            CheckCongestionThread.Instance.Start();



            TaskSendThread.Instance.ShowMessage += OnShowMessageFinishCount;
            TaskReceiveThread.Instance.ShowMessage += OnShowMessageWithPicBox;
            SearchRouteThread.Instance.ShowMessage += OnShowMessageWithPicBox;
            GuiSendThread.Instance.ShowMessage += OnShowMessageWithPicBox;
            CheckCongestionThread.Instance.ShowMessage += OnShowMessageWithPicBox;

        }
        void EndThread()
        {
            TaskSendThread.Instance.ShowMessage -= OnShowMessageFinishCount;
            TaskReceiveThread.Instance.ShowMessage -= OnShowMessageWithPicBox;
            SearchRouteThread.Instance.ShowMessage -= OnShowMessageWithPicBox;
            GuiSendThread.Instance.ShowMessage -= OnShowMessageWithPicBox;
            VehicleManager.Instance.ShowMessage -= OnShowMessageDistanceCount;
            CheckCongestionThread.Instance.ShowMessage -= OnShowMessageWithPicBox;

            CheckCongestionThread.Instance.End();
            VehicleManager.Instance.End();
            GuiSendThread.Instance.End();
            SearchRouteThread.Instance.End();//启动路径搜索线程
            TaskSendThread.Instance.End();
            TaskReceiveThread.Instance.End();


        }
        void InitServer()
        {
            gm = GuiServerManager.Instance;
            gm.ShowMessage += OnShowMessageWithPicBox;
            gm.ReLoad += ReInitialSystem;
            gm.DataMessage += OnTransmitToTask;
            gm.StartServer(Convert.ToInt32(txtPort.Text));

            tm = TaskServerManager.Instance;
            tm.ShowMessage += OnShowMessageWithPicBox;
            tm.DataMessage += ReceveTask;
            tm.StartServer(Convert.ToInt32(txtPort.Text) + 1);

            am = AGVServerManager.Instance;
            am.ShowMessage += OnShowMessageWithPicBox;
            am.ReLoad += ReInitialSystem;
            am.DataMessage += OnTransmitToTask;
            am.StartServer(Convert.ToInt32(txtPort.Text) + 2);
        }
        void DisposeServer()
        {
            gm.ShowMessage -= OnShowMessageWithPicBox;
            gm.ReLoad -= ReInitialSystem;
            gm.DataMessage -= OnTransmitToTask;
            gm.Dispose();


            am.ShowMessage -= OnShowMessageWithPicBox;
            am.ReLoad -= ReInitialSystem;
            am.DataMessage -= OnTransmitToTask;
            am.Dispose();


            tm.ShowMessage -= OnShowMessageWithPicBox;
            tm.DataMessage -= ReceveTask;
            tm.Dispose();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        private void InitialSystem()
        {


            timer1.Stop();
            FinishedQueue.Instance.ClearData();
            TaskRecvQueue.Instance.ClearData();
            SearchRouteQueue.Instance.ClearData();
            Thread.Sleep(100);

            InitialAgv();
            timer1.Start();


        }
        void InitialAgv()
        {
            try
            {
                FileUtil.LoadAgvXml(); //初始化agv配置文件

                VehicleManager.Instance.InitialVehicle();
                VehicleManager.Instance.Start();
                VehicleManager.Instance.ShowMessage += OnShowMessageDistanceCount;


                label1.Text = "当前工作小车" + ConstDefine.g_VehicleCount + "辆";
                label2.Text = "开始运行时间：" + DateTime.Now.ToString();

                Logs.Info("*--------------------------------------------------------------*");
                Logs.Info("当前工作小车" + ConstDefine.g_VehicleCount + "辆");
                Logs.Info("开始运行时间：" + DateTime.Now.ToString());

            }
            catch (FileNotFoundException ex)
            {
                Logs.Fatal("agvFile未找到" + ex);
            }
            catch (FileLoadException ex)
            {
                Logs.Fatal("agvFile 加载异常：" + ex);
            }
            catch (ArgumentNullException ex)
            {
                Logs.Error("小车初始化失败" + ex);
            }
            catch (IndexOutOfRangeException ex)
            {
                MessageBox.Show("小车位置超出了地图范围，初始化小车失败");
                Logs.Error("小车初始化失败" + ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("小车文件加载失败");
                Logs.Fatal("小车文件加载失败");
            }

        }
        //private void ReInitialAgv()
        //{
        //    if (tableLayoutPanel1.InvokeRequired)
        //    {
        //        // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
        //        Action actionDelegate = () => { InitialAgv(); };
        //        // 或者
        //        // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
        //        this.tableLayoutPanel1.Invoke(actionDelegate, null);
        //    }
        //    else
        //    {
        //        InitialAgv();
        //    }

        //}
        private void ReInitialSystem()
        {
            if (tableLayoutPanel1.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action actionDelegate = () => { InitialSystem(); };
                // 或者
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.tableLayoutPanel1.Invoke(actionDelegate, null);
            }
            else
            {
                InitialSystem();
            }

        }
        private int MAX_NODE_LENGTH;
        private int MIN_NODE_LENGTH;
        void InitUiView()
        {
            Elc.InitialElc();

            this.WindowState = FormWindowState.Maximized;
            ConstDefine.g_NodeLength = (int)(ConstDefine.FORM_WIDTH * ConstDefine.PANEL_RADIO) / ConstDefine.g_WidthNum;
            MAX_NODE_LENGTH = ConstDefine.g_NodeLength * 2;
            MIN_NODE_LENGTH = ConstDefine.g_NodeLength / 2;

            ////设置滚动条滚动的区域
            //this.AutoScrollMinSize = new Size(ConstDefine.WIDTH + ConstDefine.BEGIN_X, ConstDefine.HEIGHT);

          


            SetMapView();
            SetInfoShowView();



        }
        ///// <summary>
        ///// 减少界面闪烁
        ///// </summary>
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000;
        //        return cp;
        //    }
        //}
        void SetInfoShowView()
        {
            //设置显示文字图片的属性
            int w2 = (int)(ConstDefine.FORM_WIDTH * 0.14);
            int h2 = (int)(ConstDefine.FORM_WIDTH);
            picShow.Location = Point.Empty;
            // picShow.Size = new Size(w2, h2);
            picShowSurface = new Bitmap(w2, h2);
            picg = Graphics.FromImage(picShowSurface);
            picShow.Image = picShowSurface;
        }
        void SetMapView()
        {
            int w = ConstDefine.g_NodeLength * (ConstDefine.g_WidthNum);
            int h = ConstDefine.g_NodeLength * (ConstDefine.g_HeightNum);
            //设置pictureBox的尺寸和位置
            pic.Location = Point.Empty;
            pic.Size = new Size(w, h);
            // pic.ClientSize = new System.Drawing.Size(w,h);
            surface = new Bitmap(w, h);
            g = Graphics.FromImage(surface);
            //Bitmap bit = new Bitmap(surface);
            //设置panel的尺寸
            // splitContainer1.Panel1.ClientSize = new System.Drawing.Size(ConstDefine.WIDTH, ConstDefine.HEIGHT);

            //将pictureBox加入到panel上
            pic.Image = newSurface;
            pic.BackColor = Color.FromArgb(100, 0, 0, 0);

            //设置滚动条滚动的区域
            panel1.AutoScrollMinSize = new Size(w, h);
            DrawMap();

        }



        public void DrawMap()
        {
            //横纵坐标的控制变量
            int point_x, point_y;

            //节点类型

            point_x = 0;
            point_y = 0;

            for (int i = 0; i < Elc.HeightNum; i++)
            {
                point_x = 0;
                for (int j = 0; j < Elc.WidthNum; j++)
                {
                    //Elc.mapnode[i, j] = new MapNode(point_x, point_y, Node_number, point_type);
                    Elc.mapnode[i, j].X = point_x;
                    Elc.mapnode[i, j].Y = point_y;
                    point_x += ConstDefine.g_NodeLength;

                }
                point_y += ConstDefine.g_NodeLength;
            }

            for (int i = 0; i < Elc.HeightNum; i++)
            {
                for (int j = 0; j < Elc.WidthNum; j++)
                {
                    drawArrow(i, j);

                    //绘制标尺
                    if (i == 0 || i == Elc.HeightNum - 1)
                    {
                        DrawUtil.FillRectangle(g, Color.FromArgb(180, 0, 0, 0), Elc.mapnode[i, j].X - 1, Elc.mapnode[i, j].Y - 1, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2);
                        DrawUtil.DrawString(g, j, ConstDefine.g_NodeLength / 2, Color.Yellow, Elc.mapnode[i, j].X - 1, Elc.mapnode[i, j].Y - 1);
                    }
                    if (j == 0 || j == Elc.WidthNum - 1)
                    {
                        DrawUtil.FillRectangle(g, Color.FromArgb(180, 0, 0, 0), Elc.mapnode[i, j].X - 1, Elc.mapnode[i, j].Y - 1, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2);
                        DrawUtil.DrawString(g, i, ConstDefine.g_NodeLength / 2, Color.Yellow, Elc.mapnode[i, j].X - 1, Elc.mapnode[i, j].Y - 1);
                    }
                }
            }

        }



        Bitmap newSurface;
        /// <summary>
        /// 绘制电子地图
        /// </summary>
        /// <param name="e"></param>
        public void Draw(Graphics g)
        {
            newSurface = new Bitmap(surface);
            Graphics gg = Graphics.FromImage(newSurface);
            ////绘制探测节点
            //for (int i = 0; i < Elc.HeightNum; i++)
            //{
            //    for (int j = 0; j < Elc.WidthNum; j++)
            //    {
            //        if (Elc.mapnode[i, j].NodeCanUsed > -1)
            //        {
            //            gg.FillRectangle(new SolidBrush(Color.DarkOliveGreen), new Rectangle(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y, ConstDefine.nodeLength, ConstDefine.nodeLength));
            //            Font font = new Font(new System.Drawing.FontFamily("宋体"), ConstDefine.nodeLength / 2);
            //            Brush brush = Brushes.Black;
            //            PointF pf = new PointF(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y);
            //            gg.DrawString(Elc.mapnode[i, j].NodeCanUsed + "", font, brush, pf);
            //        }
            //    }
            //}

            //绘制锁住的节点            
            //for (int num = 0; num < VehicleManager.Instance.GetVehicles().Length; num++)
            //{
            //    List<MyPoint> listNode = new List<MyPoint>(VehicleManager.Instance.GetVehicles()[num].LockNode);
            //    for (int q = 0; q < listNode.Count; q++)
            //    {

            //        int i = listNode[q].X;
            //        int j = listNode[q].Y;
            //        gg.FillRectangle(new SolidBrush(Color.Red), new Rectangle(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y, ConstDefine.g_NodeLength, ConstDefine.g_NodeLength));
            //        Font font = new Font(new System.Drawing.FontFamily("宋体"), ConstDefine.g_NodeLength / 2);
            //        Brush brush = Brushes.DarkMagenta;
            //        PointF pf = new PointF(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y);
            //        gg.DrawString(VehicleManager.Instance.GetVehicles()[num].Id + "", font, brush, pf);

            //    }
            //}

            //绘制拥堵的节点
            for (int i = 0; i < Elc.mapnode.GetLength(0); i++)
            {
                for (int j = 0; j < Elc.mapnode.GetLength(1); j++)
                {
                    if (Elc.mapnode[i, j].TraCongesIntensity > 0)
                    {
                        gg.FillRectangle(new SolidBrush(Color.Red), new Rectangle(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y, ConstDefine.g_NodeLength, ConstDefine.g_NodeLength));
                        Font font = new Font(new System.Drawing.FontFamily("宋体"), ConstDefine.g_NodeLength / 2);
                        Brush brush = Brushes.DarkMagenta;
                        PointF pf = new PointF(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y);
                        gg.DrawString(Elc.mapnode[i, j].TraCongesIntensity + "", font, brush, pf);
                    }
                }
            }

            //绘制小车
            Vehicle[] v = VehicleManager.Instance.GetVehicles();
            if (v != null)
            {
                for (int i = 0; i < v.Length; i++)
                {
                    v[i].Draw(gg);
                    v[0].X = 1111;
                }
            }
            //if (first)
            //{
            //    int index = 2;
            //    if (vehicle[index].Route != null)
            //    {
            //        OnShowMessageWithPicBox(this, new MessageEventArgs("第"+index + "辆小车:(" + vehicle[index].BeginX+ ","+vehicle[index].BeginY+")->("+ vehicle[index].EndX+ ","+vehicle[index].EndY+")"));
            //        for (int i = 0; i < vehicle[index].Route.Count; i++)
            //        {
            //            OnShowMessageWithPicBox(this, new MessageEventArgs(i + ":" + vehicle[index].Route[i].Dir + ""));
            //        }
            //    }
            //    first = false;
            //}



            //vehicle[0].Draw(e.Graphics);
            //vehicle[1].Draw(e.Graphics);



            DrawMsgOnPic();
            pic.Image = newSurface;
        }

        void drawArrow(int y, int x)
        {
            if (Elc.mapnode[y, x].IsAbleCross)
            {
                g.DrawImage(ConstDefine.IMAGE_DICT[15], new Rectangle(Elc.mapnode[y, x].X - 1, Elc.mapnode[y, x].Y - 1, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2));
            }
            else
            {
                g.DrawImage(ConstDefine.IMAGE_DICT[0], new Rectangle(Elc.mapnode[y, x].X - 1, Elc.mapnode[y, x].Y - 1, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2));
            }
        }

        bool first = true;
        private void button12_Click(object sender, EventArgs e)
        {

        }

        StringBuilder sb = new StringBuilder();
        /// <summary>
        /// 定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            Draw(g);
            //this.Invalidate();
        }
        long time = 0;
        long oldTime = 0;
        public void OnTransmitToTask(object sender, MessageEventArgs e)
        {
            //OnShowMessageWithPicBox(sender,e);
            // OnShowMessageWithPicBox(sender, new MessageEventArgs("归位"));
            try
            {
                tm.Send(e.Type, e.Message);
            }
            catch (Exception ex)
            {
                Logs.Error("转发异常:" + ex.Message);
                OnShowMessageWithPicBox(this, new MessageEventArgs("转发异常:" + ex.Message));
            }
        }
        public void OnShowMessageFinishCount(object sender, MessageEventArgs e)
        {
            if (finishCountLabel.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action actionDelegate = () => { ShowFinishCount(e.Message); };

                //    IAsyncResult asyncResult =actionDelegate.BeginInvoke()

                // 或者 
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.finishCountLabel.Invoke(actionDelegate, null);
            }
            else
            {
                ShowFinishCount(e.Message);
                // update(e.ShowMessage);
            }
        }
        void ShowFinishCount(string str)
        {
            finishCountLabel.Text = str;
        }
        public void OnShowMessageDistanceCount(object sender, MessageEventArgs e)
        {
            if (finishCountLabel.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action actionDelegate = () => { ShowDistanceCount(e.Message); };

                //    IAsyncResult asyncResult =actionDelegate.BeginInvoke()

                // 或者 
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.finishCountLabel.Invoke(actionDelegate, null);
            }
            else
            {
                ShowDistanceCount(e.Message);
                // update(e.ShowMessage);
            }
        }
        void ShowDistanceCount(string str)
        {
            distanceTotal.Text = str;
        }
        //BlockingCollection<string>onShowMessageList=new BlockingCollection<string>();
        // ConcurrentBag<string> onShowMessageList = new ConcurrentBag<string>();
        //List<string> onShowMessageList = new List<string>();
        ConcurrentQueue<string> onShowMessageList = new ConcurrentQueue<string>();
        public void OnShowMessageWithPicBox(object sender, MessageEventArgs e)
        {
            onShowMessageList.Enqueue(e.Message);
        }

        private Bitmap picShowSurface = null;
        private Graphics picg = null;
        int rowCount = 0;
        void DrawMsgOnPic()
        {

            //int w2 = (int)(ConstDefine.FORM_WIDTH * 0.14);
            //int h2 = (int)(rowCount + 1) * ConstDefine.FONT_SISE;
            //// pic.Size = new Size(w, h);
            ////picShowSurface = new Bitmap(w2, h2);
            ////picShow.Size = new Size(w2, h2);
            //picShowSurface = new Bitmap(w2, h2);
            //picg = Graphics.FromImage(picShowSurface);

            if (!onShowMessageList.IsEmpty)
            {
                string tmp = "";
                bool success = onShowMessageList.TryDequeue(out tmp);
                if (success)
                {
                    DrawUtil.DrawString(picg, tmp, ConstDefine.FONT_SISE, Color.White, 0, (rowCount++) * (ConstDefine.FONT_SISE + ConstDefine.ROW_BOARD));
                }
            }

            // splitContainer1.Panel2.AutoScrollMinSize = new Size(w2, h2);
            // splitContainer1.Panel2.Invalidate();

            this.picShow.Image = picShowSurface;


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //显示本机监听地址
            string ipv4 = GetHostAdress();
            txtServer.Text = ipv4;

        }

        //获得本机ip地址
        private string GetHostAdress()
        {
            //获取本机的ip地址
            string name = Dns.GetHostName();
            string ipStr = "没有获取到本机的ipv4地址";
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipStr = ipa.ToString();
                }
            }
            return ipStr;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeServer();
            Logs.Info("总任务数："+finishCountLabel.Text+" "+distanceTotal.Text);
            EndThread();
            //Environment.Exit(0); 

        }

        public void ReceveTask(object sender, MessageEventArgs e)
        {
            TaskRecvQueue.Instance.AddMyQueueList(e.Message);
        }

        public void MapText()
        {

            StringBuilder sb = new StringBuilder();

            //for (int i = 0; i < vehicle[3].route.Count-1; i++)
            //{                
            //    sb.Append("[" + vehicle[3].route[i].col + "," + vehicle[3].route[i].row + "] ");                
            //}
            //sb.Append("\r\n");
            //if (vehicle[6].route != null)
            //{
            //    for (int i = 0; i < vehicle[6].route.Count - 1; i++)
            //    {
            //        sb.Append("[" + vehicle[6].route[i].col + "," + vehicle[6].route[i].row + "] ");
            //    }
            //}
            //  label8.Text = sb.ToString();
        }

        /// <summary>
        /// 放大键触发的函数
        /// Stack没有获取栈顶元素的函数，所以先弹出栈顶元素，然后弹出第二个元素并获取，然后将第二个元素压入栈
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (ConstDefine.g_NodeLength < MAX_NODE_LENGTH)
            {
                ConstDefine.g_NodeLength = (int)(ConstDefine.g_NodeLength * ConstDefine.ENLARGER_RADIO);
                // Elc.InitialElc(); //初始化电子地图
                SetMapView();
                this.Invalidate();
            }
        }

        /// <summary>
        /// 缩小键触发的函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (ConstDefine.g_NodeLength > MIN_NODE_LENGTH && ConstDefine.g_NodeLength > 1)
            {
                ConstDefine.g_NodeLength = (int)(ConstDefine.g_NodeLength * ConstDefine.NARROW_RADIO);
                //  Elc.InitialElc(); //初始化电子地图
                SetMapView();
                this.Invalidate();
            }
        }

        /// <summary>
        /// 正常大小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click_1(object sender, EventArgs e)
        {
            ConstDefine.g_NodeLength = (int)(ConstDefine.FORM_WIDTH * ConstDefine.PANEL_RADIO) / ConstDefine.g_WidthNum;
            //Elc.InitialElc(); //初始化电子地图
            SetMapView();
            this.Invalidate();
        }

        /// <summary>
        /// 随机走
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click_1(object sender, EventArgs e)
        {
            //InitialAgv();

            VehicleManager.Instance.RandomMove(4);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SqlManager.Instance.GetElecMapInfo();
        }


    }
}
