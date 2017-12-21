using AGV_V1._0;
using AGV_V1._0.Agv;
using AGV_V1._0.Algorithm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Astar
{

    class AstarSearch
    {
        public const int DIR_COST = 2;
        public const int CROSS_COST = 2;

        public const int MaxLength = 6000;   //用于优先队列（Open表）的数组
        public int Height = 15;       //地图高度
        public int Width = 20;       //地图宽度

        public const int Reachable = 0;       //可以到达的结点
        public const int Bar = 1;             //障碍物
        public const int Pass = 2;            //需要走的步数
        public const int Source = 3;          //起点
        public const int Destination = 4;     //终点

        public const int Sequential = 0;    //顺序遍历
        public const int NoSolution = 2;    //无解决方案
        public const int Infinity = 0xfffffff;

        public const int Right = (1 << 0);
        //   public const int South_East = (1 << 1);
        public const int Down = (1 << 1);
        //  public const int South_West = (1 << 3);
        public const int Left = (1 << 2);
        //  public const int North_West = (1 << 5);
        public const int Up = (1 << 3);
        //  public const int North_East = (1 << 7);

        static MyPoint[] dir = new MyPoint[4]{	
        new MyPoint( 0, 1),   //,Direction.Right // East 0
	 //   new myPoint( 1, 1, ),   // South_East 1
	    new MyPoint( 1, 0),  //,Direction.Down // South 2
	  //  new myPoint(1, -1 ),  // South_West 3
	    new MyPoint( 0, -1 ), //,Direction.Left // West 4
	 //   new myPoint( -1, -1 ), // North_West 5
        new MyPoint( -1, 0), //,Direction.Up  // North 6
	 //   new myPoint( -1, 1)   // North_East 7
};

        public bool within(int x, int y)
        {
            if ((x >= 0 && y >= 0
                && x < Height && y < Width))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        Node[,] graph = null;

        int srcX, srcY, dstX, dstY;    //起始点、终点
        Close[,] close = null;

        Direction searchDir; //当前搜索方向


        // 优先队列基本操作
        public void initOpen(Open q)    //优先队列初始化
        {
            q.length = 0;        // 队内元素数初始为0
        }

        public void push(Open q, Close[,] cls, int x, int y, float g)
        {    //向优先队列（Open表）中添加元素
            Close t;
            int i, mintag;
            cls[x, y].G = g;    //所添加节点的坐标
            cls[x, y].F = cls[x, y].G + cls[x, y].H;

            q.Array[q.length++] = (cls[x, y]);
            mintag = q.length - 1;
            for (i = 0; i < q.length - 1; i++)
            {
                if (q.Array[i].F < q.Array[mintag].F)
                {
                    mintag = i;
                }
            }
            t = q.Array[q.length - 1];
            q.Array[q.length - 1] = q.Array[mintag];
            q.Array[mintag] = t;    //将评价函数值最小节点置于队头
        }
        public Direction getDirection(Close from, Close curPoint)
        {

            if (from.node.x - curPoint.node.x == 1)
            {
                return Direction.Up;// 3 North;
            }
            if (from.node.x - curPoint.node.x == -1)
            {
                return Direction.Down;// 1;//South;
            }
            if (from.node.y - curPoint.node.y == 1)
            {
                return Direction.Left;// 2;//West;
            }
            if (from.node.y - curPoint.node.y == -1)
            {
                return Direction.Right;// 0;// East;
            }
            return from.node.direction;



        }

        public Close shift(Open q)
        {
            return q.Array[--q.length];
        }

        // 地图初始化操作
        public void initClose(Close[,] cls, int sx, int sy, int dx, int dy)
        {    // 地图Close表初始化配置
            int i, j;
            for (i = 0; i < Height; i++)
            {
                for (j = 0; j < Width; j++)
                {
                    cls[i, j] = new Close { };
                    cls[i, j].node = graph[i, j];               // Close表所指节点
                    cls[i, j].vis = !(graph[i, j].node_Type);  // 是否被访问
                    cls[i, j].from = null;                    // 所来节点
                    cls[i, j].G = cls[i, j].F = 0;
                    cls[i, j].H = Math.Abs(dx - i) + Math.Abs(dy - j);    // 评价函数值
                }
            }
            cls[sx, sy].F = cls[sx, sy].H;            //起始点评价初始值
            //    cls[sy,sy].G = 0;                        //移步花费代价值
            cls[dx, dy].G = Infinity;
        }


        void initGraph(ElecMap elc, List<MyPoint> scanner, List<MyPoint> lockNode, int v_num, int sx, int sy, int dx, int dy, Direction direction)
        //  public void initGraph(ElecMap elc, List<MyPoint> scanner,ConcurrentQueue<MyPoint> lockNode, int v_num, int sx, int sy, int dx, int dy, Direction direction)
        {

            //地图发生变化时重新构造地
            int i, j;
            srcX = sx;    //起点X坐标
            srcY = sy;    //起点Y坐标
            dstX = dx;    //终点X坐标
            dstY = dy;    //终点Y坐标
            this.searchDir = direction;
            //switch (direction)
            //{
            //    case Direction.East:
            //        this.searchDir = 0;
            //        break;
            //    case Direction.South:
            //        this.searchDir = 2;
            //        break;
            //    case Direction.West:
            //        this.searchDir = 4;
            //        break;
            //    case Direction.North:
            //        this.searchDir =6;
            //        break;
            //}

            //电子地图的长、宽被分割的个数
            Height = elc.HeightNum;
            Width = elc.WidthNum;
            //Width = width;
            //Height = height;

            graph = new Node[Height, Width];

            for (i = 0; i < Height; i++)
            {
                for (j = 0; j < Width; j++)
                {
                    graph[i, j] = new Node { };
                    int value = 1;
                    if (elc.mapnode[i, j].IsAbleCross == true)
                    {//&&elc.mapnode[i, j].LockNode != v_num){
                        value = 0;
                    }
                    graph[i, j].value = value;//&&elc.mapnode[i,j].NodeCanUsed==-1
                    graph[i, j].x = i; //地图坐标X
                    graph[i, j].y = j; //地图坐标Y

                    graph[i, j].node_Type = (graph[i, j].value == Reachable);    // 节点可到达性
                    graph[i, j].adjoinNodeCount = 0; //邻接节点个数

                    graph[i, j].Left = elc.mapnode[i, j].Left;
                    graph[i, j].Right = elc.mapnode[i, j].Right;
                    graph[i, j].Up = elc.mapnode[i, j].Up;
                    graph[i, j].Down = elc.mapnode[i, j].Down;

                }
            }
            if (NodeDirCount(sx, sy) <= lockNode.Count)
            {
                lockNode.Clear();
            }
            for (int index = 0; index < lockNode.Count; index++)
            {
                graph[lockNode[index].X, lockNode[index].Y].node_Type = false;
            }
            //Parallel.For(0, lockNode.Count, (int ii) =>
            //{
            //    MyPoint point = null;
            //    if(lockNode.TryPeek(out point)){
            //        graph[point.X, point.Y].node_Type = false;
            //    }
            //});

            for (int index = 0; index < scanner.Count; index++)
            {
                graph[scanner[index].X, scanner[index].Y].node_Type = false;
            }

            for (i = 0; i < Height; i++)
            {
                for (j = 0; j < Width; j++)
                {

                    if ((!graph[i, j].node_Type) && (i != srcX && j != srcY))//&&(i!=srcX&&j!=srcY)即使起点不可达也计算它的邻接点数sur
                    {
                        continue;
                    }
                    if (j > 0)
                    {
                        if (graph[i, j - 1].node_Type && graph[i, j].Left == true)    // left节点可以到达
                        {
                            graph[i, j].adjoinNodeCount |= Left;
                            // graph[i, j - 1].adjoinNodeCount |= Right;
                        }
                        //if (i > 0)
                        //{
                        //    if (graph[i - 1, j - 1].Node_Type
                        //        && graph[i - 1, j].Node_Type
                        //        && graph[i, j - 1].Node_Type)    // up-left节点可以到达
                        //    {
                        //        graph[i, j].sur |= North_West;
                        //        graph[i - 1, j - 1].sur |= South_East;
                        //    }
                        //}
                    }
                    if (j < Width - 1)
                    {
                        if (graph[i, j + 1].node_Type && graph[i, j].Right == true)    // right节点可以到达
                        {
                            graph[i, j].adjoinNodeCount |= Right;
                            // graph[i, j - 1].adjoinNodeCount |= Right;
                        }
                        //if (i > 0)
                        //{
                        //    if (graph[i - 1, j - 1].Node_Type
                        //        && graph[i - 1, j].Node_Type
                        //        && graph[i, j - 1].Node_Type)    // up-left节点可以到达
                        //    {
                        //        graph[i, j].sur |= North_West;
                        //        graph[i - 1, j - 1].sur |= South_East;
                        //    }
                        //}
                    }
                    if (i > 0)
                    {
                        if (graph[i - 1, j].node_Type && graph[i, j].Up == true)    // up节点可以到达
                        {
                            graph[i, j].adjoinNodeCount |= Up;
                            // graph[i - 1, j].adjoinNodeCount |= Down;
                        }
                        //if (j < Width - 1)
                        //{
                        //    if (graph[i - 1, j + 1].Node_Type
                        //        && graph[i - 1, j].Node_Type
                        //        && map[i, j + 1] == Reachable) // up-right节点可以到达
                        //    {
                        //        graph[i, j].sur |= North_East;
                        //        graph[i - 1, j + 1].sur |= South_West;
                        //    }
                        //}
                    }
                    if (i < Height - 1)
                    {
                        if (graph[i + 1, j].node_Type && graph[i, j].Down == true)    // down节点可以到达
                        {
                            graph[i, j].adjoinNodeCount |= Down;
                            // graph[i - 1, j].adjoinNodeCount |= Down;
                        }
                        //if (j < Width - 1)
                        //{
                        //    if (graph[i - 1, j + 1].Node_Type
                        //        && graph[i - 1, j].Node_Type
                        //        && map[i, j + 1] == Reachable) // up-right节点可以到达
                        //    {
                        //        graph[i, j].sur |= North_East;
                        //        graph[i - 1, j + 1].sur |= South_West;
                        //    }
                        //}
                    }
                }
            }
        }
        int NodeDirCount(int x, int y)
        {
            int count = 0;
            if (graph[x, y].Up)
            {
                count++;
            }
            if (graph[x, y].Down)
            {
                count++;
            }
            if (graph[x, y].Left)
            {
                count++;
            }
            if (graph[x, y].Right)
            {
                count++;
            }

            return count;
        }

        int astar(ElecMap elc)
        {    // A*算法遍历
            //int times = 0; 
            int i, curX, curY, surX, surY;
            float surG;
            Open open = new Open(); //Open表
            Close curPoint = new Close();
            // curPoint.node = new MapNode();
            //curPoint.node.direction = this.searchDir;
            //curPoint.node.stopTime = 1;


            close = new Close[Height, Width];
            initOpen(open);
            initClose(close, srcX, srcY, dstX, dstY);
            close[srcX, srcY].vis = true;
            push(open, close, srcX, srcY, 0);


            while (open.length > 0)
            {    //times++;
                curPoint = shift(open);
                curX = curPoint.node.x;
                curY = curPoint.node.y;

                if (curPoint.from == null)
                {
                    curPoint.node.direction = this.searchDir;

                }
                else
                {
                    curPoint.node.direction = getDirection(curPoint.from, curPoint);//0525
                }
                for (i = 0; i < 4; i++)
                {
                    if ((curPoint.node.adjoinNodeCount & (1 << i)) == 0)
                    {
                        continue;
                    }
                    surX = curX + (int)dir[i].X;
                    surY = curY + (int)dir[i].Y;
                    //if (surX < 0 || surY < 0)
                    //{
                    //    Console.WriteLine("走出场地外");
                    //    continue;
                    //} 
                    if (!close[surX, surY].vis)
                    {
                        close[surX, surY].vis = true;
                        close[surX, surY].from = curPoint;
                        Direction tempDir = new Direction();
                        switch (i)
                        {
                            case 0:
                                tempDir = Direction.Right;
                                break;
                            case 1:
                                tempDir = Direction.Down;
                                break;
                            case 2:
                                tempDir = Direction.Left;
                                break;
                            case 3:
                                tempDir = Direction.Up;
                                break;
                        }
                        int directionCost = (tempDir == curPoint.node.direction) ? 0 : 1;
                        //  curPoint.node.stopTime = 1+directionCost * 2; 

                        int crossCost = (elc.mapnode[curX, curY].CrossCount.Count > 0 )? elc.mapnode[curX, curY].CrossCount.Count : 0;

                        //curPoint.searchDir = close[surX, surY].searchDir;
                        surG = curPoint.G + (float)(Math.Abs(curX - surX) + Math.Abs(curY - surY)) + DIR_COST * directionCost+CROSS_COST*crossCost;
                        push(open, close, surX, surY, surG);
                    }
                }
                if (curPoint.H == 0)
                {
                    return Sequential;
                }
            }
            //System.Console.Write("times: %d\n", times);
            return NoSolution; //无结果
        }



        string[] Symbol = new string[5] { "□", "▓", "▽", "☆", "◎" };


        public Close getShortest(ElecMap elc)
        {    // 获取最短路径


            int result = astar(elc);
            Close p, t, q = null;
            switch (result)
            {
                case Sequential:  //顺序最近
                    p = (close[dstX, dstY]);
                    while (p != null)    //转置路径
                    {
                        t = p.from;
                        p.from = q;
                        q = p;
                        p = t;
                    }
                    close[srcX, srcY].from = q.from;
                    return (close[srcX, srcY]);
                case NoSolution:
                    return null;
            }
            return null;
        }

        Close start;
        int m = 0;
        //int shortestep;
        // public int printShortest(List<MyPoint> route)
        public int PrintRoute(ElecMap elc, List<MyPoint> route)
        {
            Close p;
            int step = 0;

            p = getShortest(elc);
            start = p;
            if (p == null)
            {
                return 0;
            }
            else
            {
                while (p.from != null)
                {
                    graph[p.node.x, p.node.y].value = Pass;
                    // System.Console.WriteLine("({0}，{1}）→", p.node.col, p.node.row);
                    route.Add(new MyPoint(p.node.x, p.node.y));
                    m++;
                    p = p.from;
                    step++;
                }
                // System.Console.WriteLine("→（{0}，{1}）", p.node.col, p.node.row);
                //route.Add(new MyPoint(p.node.col, p.node.row));
                route.Add(new MyPoint(p.node.x, p.node.y));
                m++;
                graph[srcX, srcY].value = Source;
                graph[dstX, dstY].value = Destination;
                return step;
            }
        }



        //public void printSur()
        //{
        //    int i, j;
        //    for (i = 0; i < Height; i++)
        //    {
        //        for (j = 0; j < Width; j++)
        //        {
        //            System.Console.Write("%02x ", graph[i, j].adjoinNodeCount);
        //        }
        //        System.Console.WriteLine("");
        //    }
        //    System.Console.WriteLine("");
        //}

        //public void printH()
        //{
        //    int i, j;
        //    for (i = 0; i < Height; i++)
        //    {
        //        for (j = 0; j < Width; j++)
        //        {
        //            System.Console.Write("%02d ", close[i, j].H);
        //        }
        //        System.Console.WriteLine("");
        //    }
        //    System.Console.WriteLine("");
        //}
        //public int bfs()
        //{
        //    int times = 0;
        //    int i, curX, curY, surX, surY;
        //    int f = 0, r = 1;
        //    Close p = new Close();
        //    Close[] q = new Close[Height * Width];
        //    int w = 0;
        //    for (int m = 0; m < Height; m++)
        //    {
        //        for (int n = 0; n < Width; n++)
        //        {
        //            q[w] = close[m, n];
        //            w++;
        //        }
        //    }

        //    initClose(close, srcX, srcY, dstX, dstY);
        //    close[srcX, srcY].vis = true;

        //    while (r != f)
        //    {
        //        p = q[f];
        //        f = (f + 1) % MaxLength;
        //        curX = p.node.col;
        //        curY = p.node.row;
        //        for (i = 0; i < 8; i++)
        //        {
        //            if ((p.node.adjoinNodeCount & (1 << i)) == 0)
        //            {
        //                continue;
        //            }
        //            surX = curX + (int)dir[i].col;
        //            surY = curY + (int)dir[i].row;
        //            if (surX < 0 || surY < 0)
        //            {
        //                Console.WriteLine("走出场地外");
        //                continue;
        //            } 
        //            if (!close[surX, surY].vis)
        //            {
        //                close[surX, surY].from = p;
        //                close[surX, surY].vis = true;
        //                close[surX, surY].G = p.G + 1;
        //                q[r] = close[surX, surY];
        //                r = (r + 1) % MaxLength;
        //            }
        //        }
        //        times++;
        //    }
        //    return times;
        //}
        // static int[,] map = null;
        //public void ChangeMap(ElecMap elc, int width, int height)
        //{
        //    //电子地图的长、宽被分割的个数
        //    Height = elc.HeightNum;
        //    Width = elc.WidthNum;
        //    //Width = width;
        //    //Height = height;
        //    map = new int[Height, Width];
        //    for (int i = 0; i < Height; i++)
        //    {
        //        for (int j = 0; j < Width; j++)
        //        {
        //            if (elc.mapnode[i, j].node_Type == true)//&&elc.mapnode[i,j].NodeCanUsed==-1
        //            {
        //                map[i, j] = 0;
        //            }
        //            else
        //            {
        //                map[i, j] = 1;
        //            }

        //        }
        //    }
        //}
        public Node[,] GetGraph()
        {
            return graph;
        }
        // public List< MyPoint> search(ElecMap elc, int width, int height, int firstX, int firstY, int endX, int endY,Direction direction)
        public List<MyPoint> Search(ElecMap elc, List<MyPoint> scannerNode, List<MyPoint> lockNode, int v_num, int width, int height, int firstX, int firstY, int endX, int endY, Direction direction)
        //  public List<MyPoint> Search(ElecMap elc, List<MyPoint> scannerNode, ConcurrentQueue<MyPoint> lockNode, int v_num, int width, int height, int firstX, int firstY, int endX, int endY, Direction direction)
        {

            // ChangeMap(elc, width, height);  // 转换寻找路径的可达还是不可达
            initGraph(elc, scannerNode, lockNode, v_num, firstX, firstY, endX, endY, direction);
            List<MyPoint> route = new List<MyPoint>();
            PrintRoute(elc,route);
            return route;
            //  }
        }



    }
}
