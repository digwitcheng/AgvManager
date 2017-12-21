using Astar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV_V1._0.Map
{
    class NodeMethod
    {
        public static void AddRoute2CrossCount(ElecMap Elc, List<MyPoint> route)
        {
            for (int i = 0; i < route.Count; i++)
            {
                List<int> tmp = Elc.mapnode[route[i].X, route[i].Y].CrossCount;
                if (tmp.Count <= i)
                {
                    tmp.Add(1);
                }
                else
                {
                    tmp[i]++;
                }


            }
        }

        public static void RemoveNodeFromCrossCount(ElecMap Elc, int x, int y)
        {
            List<int> tmp = Elc.mapnode[x,y].CrossCount;
            if (tmp.Count > 0)
            {
                tmp[0]--;
                if (tmp[0] <= 0)
                {
                    tmp.Remove(0);
                }
            }
        }
    }
}
