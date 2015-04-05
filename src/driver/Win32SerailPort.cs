using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibNFC4CSharp.driver
{
    class Win32SerailPort
    {
        /// <summary>
        /// Opening Testing and Closing the Port Handle.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode,
            IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        //Constants for errors:
        internal const UInt32 ERROR_FILE_NOT_FOUND = 2;
        internal const UInt32 ERROR_INVALID_NAME = 123;
        internal const UInt32 ERROR_ACCESS_DENIED = 5;
        internal const UInt32 ERROR_IO_PENDING = 997;
        internal const UInt32 ERROR_IO_INCOMPLETE = 996;

        //Constants for return value:
        internal const Int32 INVALID_HANDLE_VALUE = -1;

        //Constants for dwFlagsAndAttributes:
        internal const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;

        //Constants for dwCreationDisposition:
        internal const UInt32 OPEN_EXISTING = 3;

        //Constants for dwDesiredAccess:
        internal const UInt32 GENERIC_READ = 0x80000000;
        internal const UInt32 GENERIC_WRITE = 0x40000000;

        [DllImport("kernel32.dll")]
        internal static extern Boolean CloseHandle(IntPtr hObject);

        /// <summary>
        /// Manipulating the communications settings.
        /// </summary>

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommTimeouts(IntPtr hFile, out COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean BuildCommDCBAndTimeouts(String lpDef, ref DCB lpDCB, ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean BuildCommDCBA(String lpDef, ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommState(IntPtr hFile, [In] ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetupComm(IntPtr hFile, UInt32 dwInQueue, UInt32 dwOutQueue);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMMTIMEOUTS
        {
            internal UInt32 ReadIntervalTimeout;
            internal UInt32 ReadTotalTimeoutMultiplier;
            internal UInt32 ReadTotalTimeoutConstant;
            internal UInt32 WriteTotalTimeoutMultiplier;
            internal UInt32 WriteTotalTimeoutConstant;
        }
        internal const UInt32 MAXDWORD = 0xffffffff;

        [StructLayout(LayoutKind.Sequential)]
        internal struct DCB
        {
            internal Int32 DCBlength;
            internal Int32 BaudRate;
            internal Int32 PackedValues;
            internal Int16 wReserved;
            internal Int16 XonLim;
            internal Int16 XoffLim;
            internal Byte ByteSize;
            internal Byte Parity;
            internal Byte StopBits;
            internal Byte XonChar;
            internal Byte XoffChar;
            internal Byte ErrorChar;
            internal Byte EofChar;
            internal Byte EvtChar;
            internal Int16 wReserved1;

            internal void init(bool parity, bool outCTS, bool outDSR, int dtr, bool inDSR, bool txc, bool xOut,
                bool xIn, int rts)
            {
                DCBlength = 28; PackedValues = 0x4001;
                if (parity) PackedValues |= 0x0002;
                if (outCTS) PackedValues |= 0x0004;
                if (outDSR) PackedValues |= 0x0008;
                PackedValues |= ((dtr & 0x0003) << 4);
                if (inDSR) PackedValues |= 0x0040;
                if (txc) PackedValues |= 0x0080;
                if (xOut) PackedValues |= 0x0100;
                if (xIn) PackedValues |= 0x0200;
                PackedValues |= ((rts & 0x0003) << 12);

            }
        }

        /// <summary>
        /// Reading and writing.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WriteFile(IntPtr fFile, Byte[] lpBuffer, UInt32 nNumberOfBytesToWrite,
            out UInt32 lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        internal struct OVERLAPPED
        {
            internal UIntPtr Internal;
            internal UIntPtr InternalHigh;
            internal UInt32 Offset;
            internal UInt32 OffsetHigh;
            internal IntPtr hEvent;
        }

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommMask(IntPtr hFile, UInt32 dwEvtMask);

        // Constants for dwEvtMask:
        internal const UInt32 EV_RXCHAR = 0x0001;
        internal const UInt32 EV_RXFLAG = 0x0002;
        internal const UInt32 EV_TXEMPTY = 0x0004;
        internal const UInt32 EV_CTS = 0x0008;
        internal const UInt32 EV_DSR = 0x0010;
        internal const UInt32 EV_RLSD = 0x0020;
        internal const UInt32 EV_BREAK = 0x0040;
        internal const UInt32 EV_ERR = 0x0080;
        internal const UInt32 EV_RING = 0x0100;
        internal const UInt32 EV_PERR = 0x0200;
        internal const UInt32 EV_RX80FULL = 0x0400;
        internal const UInt32 EV_EVENT1 = 0x0800;
        internal const UInt32 EV_EVENT2 = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WaitCommEvent(IntPtr hFile, IntPtr lpEvtMask, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        internal static extern Boolean CancelIo(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean ReadFile(IntPtr hFile, [Out] Byte[] lpBuffer, UInt32 nNumberOfBytesToRead,
            out UInt32 nNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        internal static extern Boolean TransmitCommChar(IntPtr hFile, Byte cChar);

        /// <summary>
        /// Control port functions.
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern Boolean EscapeCommFunction(IntPtr hFile, UInt32 dwFunc);

        // Constants for dwFunc:
        internal const UInt32 SETXOFF = 1;
        internal const UInt32 SETXON = 2;
        internal const UInt32 SETRTS = 3;
        internal const UInt32 CLRRTS = 4;
        internal const UInt32 SETDTR = 5;
        internal const UInt32 CLRDTR = 6;
        internal const UInt32 RESETDEV = 7;
        internal const UInt32 SETBREAK = 8;
        internal const UInt32 CLRBREAK = 9;

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommModemStatus(IntPtr hFile, out UInt32 lpModemStat);

        // Constants for lpModemStat:
        internal const UInt32 MS_CTS_ON = 0x0010;
        internal const UInt32 MS_DSR_ON = 0x0020;
        internal const UInt32 MS_RING_ON = 0x0040;
        internal const UInt32 MS_RLSD_ON = 0x0080;

        /// <summary>
        /// Status Functions.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean GetOverlappedResult(IntPtr hFile, IntPtr lpOverlapped,
            out UInt32 nNumberOfBytesTransferred, Boolean bWait);

        [DllImport("kernel32.dll")]
        internal static extern Boolean ClearCommError(IntPtr hFile, out UInt32 lpErrors, IntPtr lpStat);
        [DllImport("kernel32.dll")]
        internal static extern Boolean ClearCommError(IntPtr hFile, out UInt32 lpErrors, out COMSTAT cs);

        //Constants for lpErrors:
        internal const UInt32 CE_RXOVER = 0x0001;
        internal const UInt32 CE_OVERRUN = 0x0002;
        internal const UInt32 CE_RXPARITY = 0x0004;
        internal const UInt32 CE_FRAME = 0x0008;
        internal const UInt32 CE_BREAK = 0x0010;
        internal const UInt32 CE_TXFULL = 0x0100;
        internal const UInt32 CE_PTO = 0x0200;
        internal const UInt32 CE_IOE = 0x0400;
        internal const UInt32 CE_DNS = 0x0800;
        internal const UInt32 CE_OOP = 0x1000;
        internal const UInt32 CE_MODE = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMSTAT
        {
            internal const uint fCtsHold = 0x1;
            internal const uint fDsrHold = 0x2;
            internal const uint fRlsdHold = 0x4;
            internal const uint fXoffHold = 0x8;
            internal const uint fXoffSent = 0x10;
            internal const uint fEof = 0x20;
            internal const uint fTxim = 0x40;
            internal UInt32 Flags;
            internal UInt32 cbInQue;
            internal UInt32 cbOutQue;
        }
        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommProperties(IntPtr hFile, out COMMPROP cp);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMMPROP
        {
            internal UInt16 wPacketLength;
            internal UInt16 wPacketVersion;
            internal UInt32 dwServiceMask;
            internal UInt32 dwReserved1;
            internal UInt32 dwMaxTxQueue;
            internal UInt32 dwMaxRxQueue;
            internal UInt32 dwMaxBaud;
            internal UInt32 dwProvSubType;
            internal UInt32 dwProvCapabilities;
            internal UInt32 dwSettableParams;
            internal UInt32 dwSettableBaud;
            internal UInt16 wSettableData;
            internal UInt16 wSettableStopParity;
            internal UInt32 dwCurrentTxQueue;
            internal UInt32 dwCurrentRxQueue;
            internal UInt32 dwProvSpec1;
            internal UInt32 dwProvSpec2;
            internal Byte wcProvChar;
        }

        private IntPtr hCom = IntPtr.Zero;
        public bool open(string com)
        {
            DCB PortDCB = new DCB();
            COMMTIMEOUTS CommTimeouts = new COMMTIMEOUTS();
            OVERLAPPED wo = new OVERLAPPED();
            COMMPROP cp;

            com = @"\\.\" + com.Trim().ToUpper();
            hCom = CreateFile(com, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if ((IntPtr)INVALID_HANDLE_VALUE == hCom)
            {
                return false;
            }

            CommTimeouts.ReadIntervalTimeout = 30;
            CommTimeouts.ReadTotalTimeoutMultiplier = 0;
            CommTimeouts.ReadTotalTimeoutConstant = 30;
            CommTimeouts.WriteTotalTimeoutMultiplier = 30;
            CommTimeouts.WriteTotalTimeoutConstant = 0;
            if (!SetCommTimeouts(hCom, ref CommTimeouts))
            {
                CloseHandle(hCom);
                return false;
            }
            /*Serial port init
             *Refer to the PN532 data sheet (see Error! Reference source not found.). 
             *   HSU interface default configuration is: 
             *   Data bit  : 8 bits, 
             *   Parity bit  : none, 
             *   Stop bit  : 1 bit, 
             *   Baud rate  : 115 200 bauds, 
             *  Data order  : LSB first. 
             **/
            BuildCommDCBA("baud=115200 data=8 parity=N stop=1", ref PortDCB);


            if (!SetCommState(hCom, ref PortDCB))
            {
                CloseHandle(hCom);
                return false;
            }
            isOpen = true;
            return true;
        }

        public int send(byte[] pbtTx, int szTx, int timeout)
        {
            uint dwTxLen = 0;

            COMMTIMEOUTS timeouts;
            timeouts.ReadIntervalTimeout = 0;
            timeouts.ReadTotalTimeoutMultiplier = 0;
            timeouts.ReadTotalTimeoutConstant = (uint)timeout;
            timeouts.WriteTotalTimeoutMultiplier = 0;
            timeouts.WriteTotalTimeoutConstant = (uint)timeout;

            if (!SetCommTimeouts(hCom, ref timeouts))
            {
                return -1;
            }
            if (!WriteFile(hCom, pbtTx, (uint)szTx, out dwTxLen, IntPtr.Zero))
            {
                return -1;
            }
            if (0 == dwTxLen)
                return -1;
            return 0;
        }

        public int read(byte[] pbtRx, int szRx, int timeout)
        {
            uint dwBytesToGet = (uint)szRx;
            uint dwBytesReceived = 0;
            uint dwTotalBytesReceived = 0;
            bool res;
            byte[] buf = new byte[1];
            // XXX Put this part into uart_win32_timeouts () ?

            COMMTIMEOUTS timeouts;
            timeouts.ReadIntervalTimeout = 0;
            timeouts.ReadTotalTimeoutMultiplier = 0;
            timeouts.ReadTotalTimeoutConstant = (uint)timeout;
            timeouts.WriteTotalTimeoutMultiplier = 0;
            timeouts.WriteTotalTimeoutConstant = (uint)timeout;

            if (!SetCommTimeouts(hCom, ref timeouts))
            {
                return -1;
            }

            // TODO Enhance the reception method
            // - According to MSDN, it could be better to implement nfc_abort_command() mecanism using Cancello()

            do
            {
                res = ReadFile(hCom, buf, 1, out dwBytesReceived, IntPtr.Zero);
                pbtRx[dwTotalBytesReceived] = buf[0];
                dwTotalBytesReceived += dwBytesReceived;
                
                if (!res)
                {
                    return NFC.NFC_EIO;
                }
                else if (dwBytesReceived == 0)
                {
                    return NFC.NFC_ETIMEOUT;
                }
            } while (((uint)szRx) > dwTotalBytesReceived);

            return (dwTotalBytesReceived == (uint)szRx) ? 0 : NFC.NFC_EIO;
        }
        private bool isOpen =false;
        public bool IsOpen { get{return isOpen;} }
        public void close()
        {
            if (isOpen)
            {
                CloseHandle(hCom);
                isOpen = false;
            }
        }
    }
}
