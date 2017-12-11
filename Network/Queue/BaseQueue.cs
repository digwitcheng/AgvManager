using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV_V1._0
{
    public class BaseQueue<T>where T:class
    {
         Queue<T> MyQueue = new Queue<T>();

        //加一个锁
        private Object MyQueueLock = new Object();

        //判断队列里是否有数据
        public bool IsMyQueueHasData()
        {
            bool ret = false;
            lock (this.MyQueueLock)
            {
                ret = (this.MyQueue != null && this.MyQueue.Count > 0);
            }
            return ret;
        }
        public bool ClearData()
        {
            bool ret = false;
            lock (this.MyQueueLock)
            {
                if (this.MyQueue != null && this.MyQueue.Count > 0)
                {
                    this.MyQueue.Clear();
                }
                ret = true;
            }
            return ret;
        }

        //判断队列里是否有数据
        public int GetMyQueueCount()
        {
            int ret = 0;
            lock (this.MyQueueLock)
            {
                ret = this.MyQueue.Count;
            }
            return ret;
        }

        //入队操作，增加数据
        public void AddMyQueueList(T obj)
        {

            lock (this.MyQueueLock)
            {
                this.MyQueue.Enqueue(obj);
            }
        }

        //出队操作，获得数据
        public T GetMyQueueList()
        {
            T obj = default(T);
            lock (this.MyQueueLock)
            {
                bool has = (this.MyQueue != null && this.MyQueue.Count > 0);
                if (has)
                {
                    obj = this.MyQueue.Dequeue() as T;
                }

            }
            return obj;
        }

    }
}
