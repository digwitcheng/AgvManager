using AGV_V1._0;
using AGV_V1._0.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;


namespace AGV_V1._0.Util
{
     class ConstDefine
    {

         public const int REMOTE_PORT = 8081;//远程过程调用端口
         public const string REMOTE_NAME = "RouteSearch";//远程过程调用名称

         public const int minX = 5;
         public const int maxX = 7;
         public const int minY = 6;
         public const int maxY = 10;
         public const int CELL_UNIT = 1000;//格和毫格的转换单位
         public const float DEVIATION = 0.02f;//坐标相差在DEVIATION以内就看作在一个点
         public const int UPDATA_SQL_TIME = 50;



        public static int g_MapWidth = (int)(FORM_WIDTH * PANEL_RADIO);
        public static int g_MapHeight = (int)(FORM_HEIGHT);
        public static int g_WidthNum    = 100;        //地图格子的个数，默认150*150
        public static int g_HeightNum   = 100;       //
        public static int g_NodeLength  = 12;       //默认边长
        public static int g_VehicleCount = 10;     //小车数量



        public const float PANEL_RADIO = 0.7f;   //界面布局，中间场地占屏比
        public const float ENLARGER_RADIO = 1.2f;//每次放大的比率
        public const float NARROW_RADIO = 0.8f; //每次缩小的比率
        public const int FONT_SISE = 10; //消息显示面板字体大小
        public const int ROW_BOARD = 4;  //消息显示上下行空白大小
        public const int STOP_TIME = 6; //设置等待时间片段
        public const int STEP_TIME = 1000;//小车每走一格的间隔
        public const int GUI_TIME = 100;//发送给界面的间隔
        public const int CHECK_CONGESTION = 3000;//检测拥堵情况
        public const int SEARCH_TIME = 3;//搜索路径的间隔
        public const int TASK_TIME = 5;  //处理任务的间隔
        public const int FORWORD_STEP =2; //锁定多少格(包括自己所在的位置)
        public const int RESEARCH_COUNT = 10;
        public const int UNLOADING_TIME = 2000;//在投放口停留的时间
        public const int Up = (1 << 3);
        public const int Down = (1 << 2);
        public const int Left = (1 << 1);
        public const int Right = (1 << 0);
        public static readonly int FORM_WIDTH = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;   //框体的宽度
        public static readonly int FORM_HEIGHT = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;    //框体的长度       


        public const string IP_ADRESS = "127.0.0.1";
        public const string GUI_PORT_ADRESS = "5555";
        public const string TASK_PORT_ADRESS = "5556";
        public const string AGV_PORT_ADRESS = "12321";
        public const string AGV_PATH = "..\\..\\Agv\\AGV.xml";
        public const string MAP_PATH = "..\\..\\Map\\ElcMap.xml";
        public const string CONFIG_PATH = "..\\..\\NLog\\NLog.config";
    }
   

   
}
