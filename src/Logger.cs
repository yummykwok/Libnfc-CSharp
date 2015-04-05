using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LibNFC4CSharp.nfc
{
    class Logger
    {
        static bool DebugEnabled;
        static NfcLogRecieved mHandle;
        static Thread sendThread;
        public delegate void NfcLogRecieved(string txt);
        class LogDetails
        {
            public string strTxt { get; set; }
            public byte[] txt { get; set; }
            public int length { get; set; }
            public int type { get; set; }
            public LogDetails()
            {
                strTxt = "";
                txt = null ;
                length = 0;
                type = 0;
            }
        }
        class LogBuffer
        {
            LogDetails[] buffer;// = new LogDetails[64];
            private int rear;
            private int front;
            private int size;
            public LogBuffer(int size)
            {
                this.size = size;
                buffer = new LogDetails[size];
                for (int i = 0; i < size; i++)
                {
                    buffer[i] = new LogDetails();
                }
            }
            public Boolean isEmpty()
            {
                return rear == front;
            }

            public Boolean isFull()
            {
                return (front + 1) % size == rear;
            }

            public LogDetails read()
            {
                LogDetails ret;
                if (rear == front)
                {
                    return null;
                }
                ret = buffer[rear];
                rear = (rear + 1) % size;
                return ret;
            }
            public void add(string strTxt, byte[] txt, int length, int type)
            {
                if ((front + 1) % size == rear)
                {
                    return;
                }
                buffer[front].strTxt = strTxt;
                buffer[front].txt = txt;
                buffer[front].length = length;
                buffer[front].type = type;
                front = (front + 1) % size;
            }
        }

        static LogBuffer buffer = new LogBuffer(128);
        public static void Log(byte[] txt, int length, int type)
        {
            if (!DebugEnabled) return;
            buffer.add(null, txt, length, type);
        }
        public static void Log(string txt)
        {
            if (!DebugEnabled) return;
            buffer.add(txt, null, txt.Length, 3);
        }
        public static void setEnable(bool enabled, NfcLogRecieved handle, AutoResetEvent putlock)
        {
            DebugEnabled = enabled;
            if (enabled == true)
            {
                if (null == handle) throw new Exception("NfcLogRecieved handle needed.");
                if (null == putlock) throw new Exception("AutoResetEvent putlock needed.");
                mHandle = handle;
                notifyResetEvent = putlock;
                sendThread = new Thread(new ThreadStart(delegate { send(); }));
                sendThread.Start();
            }
            else
            {
                if (sendThread != null)
                {
                    sendThread.Abort();
                }
            }
        }

        static AutoResetEvent myResetEvent = new AutoResetEvent(true);
        static AutoResetEvent notifyResetEvent;
        static Semaphore sLock = new Semaphore(1, 1);
        static void send()
        {
            LogDetails log;
            while (true)
            {
                while (buffer.isEmpty())
                {
                    Thread.Sleep(50);
                }
                log = buffer.read();
                //myResetEvent.WaitOne();
                string strLog = "";
                if (0 == log.type)
                {
                    strLog = Encoding.Default.GetString(log.txt);
                }
                else if (log.type == 1)
                {
                    strLog += "TX:";
                    for (int i = 0; i < log.length; i++)
                    {
                        strLog += log.txt[i].ToString("X2") + " ";
                    }
                }
                else if (log.type == 2)
                {
                    strLog += "RX:";
                    for (int i = 0; i < log.length; i++)
                    {
                        strLog += log.txt[i].ToString("X2") + " ";
                    }
                }
                else if (log.type == 3)
                {
                    strLog = log.strTxt;
                }
                strLog += "\r\n";
                notifyResetEvent.WaitOne();
                mHandle.BeginInvoke(strLog, null, null);
                //myResetEvent.Set();
            }
        }
    }
}
