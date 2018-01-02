using AGV_V1._0.NLog;
using AGV_V1._0.Util;
using Astar;
using DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AGV_V1._0.DataBase
{
    class SqlManager
    {
            private static SqlConnection sqlConn;
            private const int MAX_TRY_CONN_COUNT = 10;
            private int connCount = 1;
            private static SqlManager instance;
            public static SqlManager Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new SqlManager();
                    }
                    return instance;
                }
            }
            private SqlManager()
            {
                
            }
            public void Connect2DataBase()
            {
                //Task.Factory.StartNew(() => TryConnect2DataBase());//启动线程            
                TryConnect2DataBase();
            }
            void TryConnect2DataBase()
            {
                while (sqlConn == null && connCount < MAX_TRY_CONN_COUNT)
                {
                    sqlConn = SqlHelper.GetSqlConnection();
                    Thread.Sleep(connCount * 100);
                    connCount++;
                }
                if (connCount >= MAX_TRY_CONN_COUNT)
                {
                    Logs.Warn("未能连上数据库");
                }
            }
            public MyPoint GetVehicleCurLocationWithId(int Id)
            {
                MyPoint point = null;
                DataTable table = GetVehicleInfoWithId(Id);
                try
                {
                    if (table != null && table.Rows.Count > 0)
                    {
                        point = new MyPoint((int)Math.Round(float.Parse(table.Rows[0]["CurX"].ToString()) / ConstDefine.CELL_UNIT), (int)Math.Round(float.Parse(table.Rows[0]["CurY"].ToString()) / ConstDefine.CELL_UNIT));
                    }
                }
                catch
                {
                    point = null;
                }
                return point;
            }
            public DataTable GetVehicleInfoWithId(int Id)
            {
                DataTable data = null;
                if (sqlConn != null && sqlConn.State == ConnectionState.Open)
                {
                    string cmdTxt=string.Format("select * from Vehicle where Id={0}",Id);
                    data = SqlHelper.GetDataTable(sqlConn,cmdTxt );
                    
                }
                else
                {
                    Console.WriteLine("数据库未连接");
                }
                return data;
            }
            public void GetElecMapInfo()
            {
                if (sqlConn != null && sqlConn.State == ConnectionState.Open)
                {
                    DataTable data = SqlHelper.GetDataTable(sqlConn, "select * from ElecMap");
                    if (data != null)
                    {
                        Console.WriteLine(data.Rows[0]["Info"]);
                    }
                }
                else
                {
                    Console.WriteLine("数据库未连接");
                }
            }
        
    }
}
