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
using GMap.NET.WindowsForms;
using GMap.NET;
using GMap.NET.MapProviders;
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
            InitUiView();//绘制界面
            StartThread();//启动发送，接收，搜索等线程
            InitialSystem();//初始化小车

        }
        void StartThread()
        {
            TaskSendThread.Instance.Start();
            TaskReceiveThread.Instance.Start();
            GuiSendThread.Instance.Start();
            SearchRouteThread.Instance.Start();



            TaskSendThread.Instance.ShowMessage += ShowMsg;
            TaskReceiveThread.Instance.ShowMessage += ShowMsg;
            SearchRouteThread.Instance.ShowMessage += ShowMsg;
            GuiSendThread.Instance.ShowMessage += ShowMsg;

        }
        void EndThread()
        {
            TaskSendThread.Instance.ShowMessage -= ShowMsg;
            TaskReceiveThread.Instance.ShowMessage -= ShowMsg;
            SearchRouteThread.Instance.ShowMessage -= ShowMsg;
            GuiSendThread.Instance.ShowMessage -= ShowMsg;

            VehicleManager.Instance.ShowMessage -= ShowMsg;
            VehicleManager.Instance.End();

            GuiSendThread.Instance.End();
            SearchRouteThread.Instance.End();//启动路径搜索线程
            TaskSendThread.Instance.End();
            TaskReceiveThread.Instance.End();


        }
        void InitServer()
        {
            gm = GuiServerManager.Instance;
            gm.ShowMessage += ShowMsg;
            gm.ReLoad += ReInitialSystem;
            gm.DataMessage += TransmitToTask;
            gm.StartServer(Convert.ToInt32(txtPort.Text));

            tm = TaskServerManager.Instance;
            tm.ShowMessage += ShowMsg;
            tm.DataMessage += ReceveTask;
            tm.StartServer(Convert.ToInt32(txtPort.Text) + 1);

            am = AGVServerManager.Instance;
            am.ShowMessage += ShowMsg;
            am.ReLoad += ReInitialSystem;
            am.DataMessage += TransmitToTask;
            am.StartServer(Convert.ToInt32(txtPort.Text) + 2);
        }
        void DisposeServer()
        {
            gm.ShowMessage -= ShowMsg;
            gm.ReLoad -= ReInitialSystem;
            gm.DataMessage -= TransmitToTask;
            gm.Dispose();


            am.ShowMessage -= ShowMsg;
            am.ReLoad -= ReInitialSystem;
            am.DataMessage -= TransmitToTask;
            am.Dispose();


            tm.ShowMessage -= ShowMsg;
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

            int res = InitialAgv();
            timer1.Start();


        }
        int InitialAgv()
        {

            int res = FileUtil.LoadAgvXml(); //初始化agv配置文件
            if (-1 == res)
            {
                MessageBox.Show("小车文件加载失败");
                Logs.Fatal("小车文件加载失败");
            }
            else if (0 == res)
            {
                MessageBox.Show("小车文件中小车数量为0");
                Logs.Warn("小车文件中小车数量为0");
            }
            else
            {
                VehicleManager.Instance.InitialVehicle();
                VehicleManager.Instance.Start();
                VehicleManager.Instance.ShowMessage += ShowMsg;

            }
            label1.Text = "当前工作小车" + ConstDefine.g_VehicleCount + "辆";
            label2.Text = "开始运行时间：" + DateTime.Now.ToString();

            return res;
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

            panel2.BackColor = Color.FromArgb(100, 0, 0, 0);
            panel1.BackColor = Color.FromArgb(180, 0, 0, 0);
            //pic.BackColor = Color.FromArgb(255, 0, 0, 0);
            // listView1.BackColor = Color.FromArgb(100, 0, 0, 0);
            splitContainer1.BackColor = Color.FromArgb(100, 0, 0, 0);


            SetMapView();
            SetInfoShowView();



        }
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

            ////Bitmap newSurface = new Bitmap(surface);
            //Graphics gg = Graphics.FromImage(newSurface);

            //for (int i = 0; i < Elc.HeightNum; i++)
            //{
            //    for (int j = 0; j < Elc.WidthNum; j++)
            //    {
            //        drawArrow(i, j);
            //    }
            //}
            ////绘制标尺
            //int w = Elc.WidthNum;
            //int h = Elc.HeightNum;
            //Font font = new Font(new System.Drawing.FontFamily("宋体"), ConstDefine.nodeLength / 2);
            //Brush brush = Brushes.Yellow;
            //for (int i = 0; i < h + 1; i++)
            //{
            //    PointF pf = new PointF(0, i * ConstDefine.nodeLength);
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Rectangle(0, i * ConstDefine.nodeLength, ConstDefine.nodeLength, ConstDefine.nodeLength));
            //    g.DrawString(i + "", font, brush, pf);

            //    PointF pf2 = new PointF(w * ConstDefine.nodeLength, i * ConstDefine.nodeLength);
            //   g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), new Rectangle(w * ConstDefine.nodeLength, i * ConstDefine.nodeLength, ConstDefine.nodeLength , ConstDefine.nodeLength));
            //    g.DrawString(i + "", font, brush, pf2);

            //}
            //for (int i = 1; i < w; i++)
            //{
            //    PointF pf = new PointF(i * ConstDefine.nodeLength, 0);
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), new Rectangle(i * ConstDefine.nodeLength, 0, ConstDefine.nodeLength - 2, ConstDefine.nodeLength - 2));
            //    g.DrawString(i + "", font, brush, pf);

            //    PointF pf2 = new PointF(i * ConstDefine.nodeLength, h * ConstDefine.nodeLength);
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), new Rectangle(i * ConstDefine.nodeLength, h * ConstDefine.nodeLength, ConstDefine.nodeLength - 2, ConstDefine.nodeLength - 2));
            //    g.DrawString(i + "", font, brush, pf2);

            //}



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

            ////绘制锁住的节点
            //if (VehicleManager.Instance.vehicleInited)
            //{
            //    for (int num = 0; num < VehicleManager.vehicle.Length; num++)
            //    {
            //        List<MyPoint> listNode = new List<MyPoint>(VehicleManager.vehicle[num].LockNode);
            //        for (int q = 0; q < listNode.Count; q++)
            //        {

            //            int i = listNode[q].X;
            //            int j = listNode[q].Y;
            //            gg.FillRectangle(new SolidBrush(Color.Red), new Rectangle(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y, ConstDefine.g_NodeLength, ConstDefine.g_NodeLength));
            //            Font font = new Font(new System.Drawing.FontFamily("宋体"), ConstDefine.g_NodeLength / 2);
            //            Brush brush = Brushes.DarkMagenta;
            //            PointF pf = new PointF(Elc.mapnode[i, j].X, Elc.mapnode[i, j].Y);
            //            gg.DrawString(VehicleManager.vehicle[num].Id + "", font, brush, pf);

            //        }
            //    }
            //}

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
            //        ShowMsg(this, new MessageEventArgs("第"+index + "辆小车:(" + vehicle[index].BeginX+ ","+vehicle[index].BeginY+")->("+ vehicle[index].EndX+ ","+vehicle[index].EndY+")"));
            //        for (int i = 0; i < vehicle[index].Route.Count; i++)
            //        {
            //            ShowMsg(this, new MessageEventArgs(i + ":" + vehicle[index].Route[i].Dir + ""));
            //        }
            //    }
            //    first = false;
            //}



            //vehicle[0].Draw(e.Graphics);
            //vehicle[1].Draw(e.Graphics);




            pic.Image = newSurface;
        }

        void drawArrow(int y, int x)
        {
            int dir = 0;
            if (Elc.mapnode[y, x].Right == true)
            {
                dir |= ConstDefine.Right;
            }
            if (Elc.mapnode[y, x].Left == true)
            {
                dir |= ConstDefine.Left;
            }
            if (Elc.mapnode[y, x].Down == true)
            {
                dir |= ConstDefine.Down;
            }
            if (Elc.mapnode[y, x].Up == true)
            {
                dir |= ConstDefine.Up;
            }

            //if (dir == 0)
            //{
            //    dir = -1;
            //}
            //else
            //{
            //    dir = 0;
            //}
            Image img = ConstDefine.IMAGE_DICT[dir];
            if (img != null)
            {
                g.DrawImage(img, new Rectangle(Elc.mapnode[y, x].X - 1, Elc.mapnode[y, x].Y - 1, ConstDefine.g_NodeLength - 2, ConstDefine.g_NodeLength - 2));

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
        public void TransmitToTask(object sender, MessageEventArgs e)
        {
            //ShowMsg(sender,e);
            // ShowMsg(sender, new MessageEventArgs("归位"));
            try
            {
                tm.Send(e.Type, e.Message);
            }
            catch (Exception ex)
            {
                Logs.Error("转发异常:" + ex.Message);
                ShowMsg(this, new MessageEventArgs("转发异常:" + ex.Message));
            }
        }
        public void ShowMsg(object sender, MessageEventArgs e)
        {

            if (pic.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action actionDelegate = () => { DrawPicShow(e.Message); };

                //    IAsyncResult asyncResult =actionDelegate.BeginInvoke()

                // 或者 
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.pic.Invoke(actionDelegate, null);
            }
            else
            {
                DrawPicShow(e.Message);
                // update(e.ShowMessage);
            }

        }
        private Bitmap picShowSurface = null;
        private Graphics picg = null;
        private int rowCount = 0;
        private static readonly object picgLock = new object();
        void DrawPicShow(string msg)
        {
            lock (picgLock)
            {
                int w2 = (int)(ConstDefine.FORM_WIDTH * 0.14);
                int h2 = (int)(rowCount + 1) * ConstDefine.FONT_SISE;
                // pic.Size = new Size(w, h);
                //picShowSurface = new Bitmap(w2, h2);
                //picShow.Size = new Size(w2, h2);
                picShowSurface = new Bitmap(w2, h2);
                //picg = Graphics.FromImage(picShowSurface);


                if (rowCount > 250)
                {
                    rowCount = 0;
                    picg.Clear(Color.FromArgb(0, 0, 0, 0));
                    return;
                }
                DrawUtil.DrawString(picg, msg, ConstDefine.FONT_SISE, Color.White, 0, rowCount++ * (ConstDefine.FONT_SISE + ConstDefine.ROW_BOARD));

                splitContainer1.Panel2.AutoScrollMinSize = new Size(w2, h2);
                splitContainer1.Panel2.Invalidate();
            }

        }
        //public void ShowSocketState(object sender,MessageEventArgs e)
        //{
        //    listBox1.Items.Add(new ListViewItem(DateTime.Now.ToLongTimeString().ToString()+" "+e.ShowMessage));
        //}
        // GuiLisenter sl;
        //  TaskLisenter tl;


        private void Form1_Load(object sender, EventArgs e)
        {
            //让控件不闪烁
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);

            ////监听端口
            //sl = new GuiLisenter(Convert.ToInt32(txtPort.Text));
            //sl._del = UpdateUI;
            //sl.Transmit += TransmitToTask;
            //sl.ShowMessage += ShowMsg;

            //tl = new TaskLisenter();
            //tl.ShowMessage += ShowMessage;
            //tl.TaskMessage += ReceveTask;
            //tl.StartLisent(txtPort.Text);



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
            InitialAgv();
            VehicleManager.Instance.RandomMove(11);
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }











    }
}
