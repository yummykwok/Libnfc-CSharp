using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibNFC4CSharp.chips;
using System.IO.Ports;
using System.Threading;
using LibNFC4CSharp.nfc;
namespace LibNFC4CSharp.driver
{
    class pn532_uart : pn53x, nfc_driver
    {
        const int PN532_BUFFER_LEN = (PN53x_EXTENDED_FRAME__DATA_MAX_LEN + PN53x_EXTENDED_FRAME__OVERHEAD);

        /*Debug  log*/
        bool DBUG = true;

        /**/
        static private pn532_uart instance = null;
        string driver_name;
        NFC.scan_type_enum driver_scan_type;
        public string name { get { return driver_name; } }
        public NFC.scan_type_enum scan_type { get { return driver_scan_type; } }
        //APISerialPort win32sp;
        //SerialPort sp = null;
        //AutoResetEvent myResetEvent = new AutoResetEvent(false);
        Win32SerailPort win32sp = null;
        struct pn532_uart_descriptor
        {
            string port;
            UInt32 speed;
        };
        public class pn532_uart_data
        {
            // serial_port port;
            public int[] iAbortFds;
            public int port;
            public volatile bool abort_flag;
            public pn532_uart_data()
            {
                
                iAbortFds = new int[2];
                abort_flag = false;
            }
        };
        static pn532_uart_data driver_data = null;

        public object get_driver_data()
        {
            return driver_data;
        }

        public static pn532_uart getInstance()
        {
            if (null == instance)
            {
                instance = new pn532_uart();
            }
            return instance;
        }
        public static void dispose()
        {
            instance = null;
            driver_data = null;
        }
        pn532_uart()
        {
            driver_name = "pn532_uart";
            driver_scan_type = NFCInternal.scan_type_enum.INTRUSIVE;
            driver_data = new pn532_uart_data();
            win32sp = new Win32SerailPort();

        }
        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
           // myResetEvent2.WaitOne();
            //myResetEvent.Set();
            
        }

        public int scan(NFC.nfc_context context, string[] connstrings, int connstrings_len) {/*Not supported at this time*/return -1; }

        public NFC.nfc_device open(NFC.nfc_context context, string port/*change to instance of sp*/)
        {
            pn532_uart_descriptor ndd;
            NFC.nfc_device pnd = null;
            /*
            sp.PortName = port;
            sp.Open();
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
             * */
            if (!win32sp.open(port))
            {
                return null ;
            }

            pnd = NFC.nfc_device_new(context, port);



            //snprintf(pnd.name, sizeof(pnd.name), "%s:%s", PN532_UART_DRIVER_NAME, ndd.port);
            //free(ndd.port);

            pnd.driver_data = new pn532_uart_data();
            //this port would not be used
            //DRIVER_DATA(pnd).port = sp;
            //pn53x_io io = new pn53x_io(sp);
            pnd.driver = pn532_uart.getInstance();
            // Alloc and init chip's data
            pn53x_data_new(pnd/*, io*/);
            // SAMConfiguration command if needed to wakeup the chip and pn53x_SAMConfiguration check if the chip is a PN532
            ((pn53x_data)(pnd).chip_data).type = pn53x_type.PN532;
            // This device starts in LowVBat mode
            ((pn53x_data)(pnd).chip_data).power_mode = pn53x_power_mode.LOWVBAT;

            // empirical tuning
            ((pn53x_data)(pnd).chip_data).timer_correction = 48;

            /*
            #ifndef WIN32
              // pipe-based abort mecanism
              if (pipe(DRIVER_DATA(pnd).iAbortFds) < 0) {
                uart_close(DRIVER_DATA(pnd).port);
                pn53x_data_free(pnd);
                nfc_device_free(pnd);
                return NULL;
              }
            #else
              DRIVER_DATA(pnd).abort_flag = false;
            #endif
            */
            // Check communication using "Diagnose" command, with "Communication test" (0x00)
            if (pn53x_check_communication(pnd) < 0)
            {
                ////log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "pn53x_check_communication error");
                //pn532_uart_close(pnd);
                return null;
            }

            pn53x_init(pnd);
            return pnd;

        }

        public void close(NFC.nfc_device pnd)
        {
            if(pnd!=null){
            idle(pnd);//chips
            }
            if(win32sp.IsOpen){
               win32sp.close();
            }
            //if (sp.IsOpen)
            //{
            //    sp.Close();
            //}
            // Release UART port
            //uart_close(DRIVER_DATA(pnd).port);

            //#ifndef WIN32
            // Release file descriptors used for abort mecanism
            //close(DRIVER_DATA(pnd).iAbortFds[0]);
            //close(DRIVER_DATA(pnd).iAbortFds[1]);
            //#endif

            //  pn53x_data_free(pnd);
            //  nfc_device_free(pnd);

        }




        /*[[ pn53x_io */
        public int send(NFC.nfc_device pnd, byte[] pbtData, int szData, int timeout)
        {

            int res = 0;

            switch (((pn53x_data)(pnd).chip_data).power_mode)
            {
                case pn53x_power_mode.LOWVBAT:
                    {
                        /** PN532C106 wakeup. */
                        if ((res = pn532_uart_wakeup(pnd)) < 0)
                        {
                            return res;
                        }
                        // According to PN532 application note, C106 appendix: to go out Low Vbat mode and enter in normal mode we need to send a SAMConfiguration command
                        if ((res = pn532_SAMConfiguration(pnd, pn532_sam_mode.PSM_NORMAL, 1000)) < 0)
                        {
                            return res;
                        }
                    }
                    break;
                case pn53x_power_mode.POWERDOWN:
                    {
                        if ((res = pn532_uart_wakeup(pnd)) < 0)
                        {
                            return res;
                        }
                    }
                    break;
                case pn53x_power_mode.NORMAL:
                    // Nothing to do :)
                    break;
            };

            byte[] abtFrame = new byte[PN532_BUFFER_LEN];
            int szFrame = 0;

            if ((res = pn53x_build_frame(abtFrame, out szFrame, pbtData, szData)) < 0)
            {
                pnd.last_error = res;
                return pnd.last_error;
            }
            if (DBUG)
            {
                //Logger.Log(abtFrame, szFrame, 1);
                Logger.Log(abtFrame, szFrame, 1);
            }
            try
            {
                win32sp.send(abtFrame, szFrame, timeout);
                //sp.WriteTimeout = timeout;
                //sp.Write(abtFrame, 0, szFrame);
                //Thread.SpinWait(200);
                //myResetEvent2.Set();
                //myResetEvent.WaitOne();
            }
            catch (Exception e)
            {
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }
            int read = 0;
            byte[] abtRxBuf = new byte[PN53x_ACK_FRAME__LEN];
            try
            {
                win32sp.read(abtRxBuf, PN53x_ACK_FRAME__LEN, timeout);
            }
            catch (Exception e)
            {
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }


            if (pn53x_check_ack_frame(pnd, abtRxBuf, abtRxBuf.Length) == 0)
            {
                // The PN53x is running the sent command
                if (DBUG)
                {
                    Logger.Log(abtRxBuf, PN53x_ACK_FRAME__LEN, 2);
                }
            }
            else
            {
                //byte[] buf = new byte[264];
                //win32sp.read(buf, buf.Length,timeout);
                return pnd.last_error;
            }
            return NFC.NFC_SUCCESS;
        }

        public int receive(NFC.nfc_device pnd, ref byte[] pbtData, int szDataLen, int timeout)
        {
            //timeout = SerialPort.InfiniteTimeout;
            //if (timeout == 0)
            //{
            //    timeout = SerialPort.InfiniteTimeout;
            //}
            byte[] abtRxBuf = new byte[5];
            int len;

            try
            {
                win32sp.read(abtRxBuf,  5,timeout);
                if (DBUG)
                {
                    Logger.Log(abtRxBuf, 5, 2);
                }
            }
            catch (Exception e)
            {
                pnd.last_error = NFC.NFC_EIO;
            }

            if (pnd.last_error < 0)
            {
                return pnd.last_error;
            }

            byte[] pn53x_preamble = { 0x00, 0x00, 0xff };
            if (0 != (MiscTool.memcmp(abtRxBuf, pn53x_preamble, 3)))
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Frame preamble+start code mismatch");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }

            if ((0x01 == abtRxBuf[3]) && (0xff == abtRxBuf[4]))
            {
                // Error frame
                //uart_receive(DRIVER_DATA(pnd).port, abtRxBuf, 3, 0, timeout);
                try
                {
                    win32sp.read(abtRxBuf, 3, timeout);
                    if (DBUG)
                    {
                        Logger.Log(abtRxBuf, 3, 2);
                    }
                }
                catch (Exception e)
                {
                }
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Application level error detected");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }
            else if ((0xff == abtRxBuf[3]) && (0xff == abtRxBuf[4]))
            {
                // Extended frame
                //pnd.last_error = uart_receive(DRIVER_DATA(pnd).port, abtRxBuf, 3, 0, timeout);
                try
                {
                    win32sp.read(abtRxBuf,3,timeout);
                    if (DBUG)
                    {
                        Logger.Log(abtRxBuf, 3, 2);
                    }
                }
                catch (Exception e)
                {
                    pnd.last_error = NFC.NFC_EIO;
                }
                if (pnd.last_error != 0)
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Unable to receive data. (RX)");
                    return pnd.last_error;
                }
                // (abtRxBuf[0] << 8) + abtRxBuf[1] (LEN) include TFI + (CC+1)
                len = (abtRxBuf[0] << 8) + abtRxBuf[1] - 2;
                if (((abtRxBuf[0] + abtRxBuf[1] + abtRxBuf[2]) % 256) != 0)
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Length checksum mismatch");
                    pnd.last_error = NFC.NFC_EIO;
                    return pnd.last_error;
                }
            }
            else
            {
                // Normal frame
                if (256 != (abtRxBuf[3] + abtRxBuf[4]))
                {
                    // TODO: Retry
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Length checksum mismatch");
                    pnd.last_error = NFC.NFC_EIO;
                    return pnd.last_error;
                }

                // abtRxBuf[3] (LEN) include TFI + (CC+1)
                len = abtRxBuf[3] - 2;
            }

            if (len > szDataLen)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "Unable to receive data: buffer too small. (szDataLen: %" PRIuPTR ", len: %" PRIuPTR ")", szDataLen, len);
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }

            // TFI + PD0 (CC+1)
            //pnd.last_error = uart_receive(DRIVER_DATA(pnd).port, abtRxBuf, 2, 0, timeout);
            try
            {
                win32sp.read(abtRxBuf, 2,timeout);
                if (DBUG)
                {
                    Logger.Log(abtRxBuf, 2, 2);
                }
            }
            catch (Exception e)
            {
                pnd.last_error = NFC.NFC_EIO;
            }
            if (pnd.last_error != 0)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Unable to receive data. (RX)");
                return pnd.last_error;
            }

            if (abtRxBuf[0] != 0xD5)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "TFI Mismatch");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }

            if (abtRxBuf[1] != ((pn53x_data)pnd.chip_data).last_command + 1)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Command Code verification failed");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }

            if (len != 0)
            {
                //pnd.last_error = uart_receive(DRIVER_DATA(pnd).port, pbtData, len, 0, timeout);
                try
                {
                    win32sp.read(pbtData, len,timeout);
                    if (DBUG)
                    {
                        Logger.Log(pbtData, len, 2);
                    }
                }
                catch (Exception e)
                {
                    pnd.last_error = NFC.NFC_EIO;
                }
                if (pnd.last_error != 0)
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Unable to receive data. (RX)");
                    return pnd.last_error;
                }
            }

            //pnd.last_error = uart_receive(DRIVER_DATA(pnd).port, abtRxBuf, 2, 0, timeout);
            try
            {
                win32sp.read(abtRxBuf,  2,timeout);
                if (DBUG)
                {
                    Logger.Log(abtRxBuf, 2, 2);
                }
            }
            catch (Exception e)
            {
                pnd.last_error = NFC.NFC_EIO;
            }
            if (pnd.last_error != 0)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Unable to receive data. (RX)");
                return pnd.last_error;
            }

            byte btDCS = (256 - 0xD5);
            btDCS -= (byte)(((pn53x_data)pnd.chip_data).last_command + 1);
            for (int szPos = 0; szPos < len; szPos++)
            {
                btDCS -= pbtData[szPos];
            }

            if (btDCS != abtRxBuf[0])
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Data checksum mismatch");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }

            if (0x00 != abtRxBuf[1])
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_ERROR, "%s", "Frame postamble mismatch");
                pnd.last_error = NFC.NFC_EIO;
                return pnd.last_error;
            }
            // The PN53x command is done and we successfully received the reply
            return len;
        }

        /*]] pn53x_io*/


        public int pn532_uart_wakeup(NFC.nfc_device pnd)
        {
            /* High Speed Unit (HSU) wake up consist to send 0x55 and wait a "long" delay for PN532 being wakeup. */
            byte[] pn532_wakeup_preamble = { 0x55, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (DBUG)
            {
                Logger.Log(pn532_wakeup_preamble, pn532_wakeup_preamble.Length, 1);
            }
            try
            {
               win32sp.send(pn532_wakeup_preamble,  pn532_wakeup_preamble.Length,0);
                ((pn53x_data)pnd.chip_data).power_mode = pn53x_power_mode.NORMAL;
                return 0;
            }
            catch (Exception e)
            {
                return NFC.NFC_EIO;
            }
        }

        public int pn532_uart_ack(NFC.nfc_device pnd)
        {
            if (pn53x_power_mode.POWERDOWN == ((pn53x_data)pnd.chip_data).power_mode)
            {
                int res = 0;
                if ((res = pn532_uart_wakeup(pnd)) < 0)
                {
                    return res;
                }
            }
            if (DBUG)
            {
                Logger.Log(pn53x_ack_frame, pn53x_ack_frame.Length, 1);
            }
            try
            {
                win32sp.send(pn53x_ack_frame,pn53x_ack_frame.Length,0);
                ((pn53x_data)pnd.chip_data).power_mode = pn53x_power_mode.NORMAL;
                return 0;
            }
            catch (Exception e)
            {
                return NFC.NFC_EIO;
            }
        }

        public string strerror(NFC.nfc_device pnd) { return ""; }

        public int initiator_init(NFC.nfc_device pnd) { return base.pn53x_initiator_init(pnd); }

        public int initiator_init_secure_element(NFC.nfc_device pnd)
        { return base.pn532_initiator_init_secure_element(pnd); }

        public int initiator_select_passive_target(NFC.nfc_device pnd, NFC.nfc_modulation nm, byte[] pbtInitData, int szInitData, /*out*/ NFC.nfc_target pnt)
        { return base.pn53x_initiator_select_passive_target(pnd, nm, pbtInitData, szInitData, /*out*/ pnt); }

        public int initiator_poll_target(NFC.nfc_device pnd, NFC.nfc_modulation[] pnmModulations, int szModulations, byte uiPollNr, byte btPeriod, out NFC.nfc_target pnt)
        { return base.pn53x_initiator_poll_target(pnd, pnmModulations, szModulations, uiPollNr, btPeriod, out pnt); }

        public int initiator_select_dep_target(NFC.nfc_device pnd, NFC.nfc_dep_mode ndm, NFC.nfc_baud_rate nbr, NFC.nfc_dep_info pndiInitiator, ref NFC.nfc_target pnt, int timeout)
        { return base.pn53x_initiator_select_dep_target(pnd, ndm, nbr, pndiInitiator, ref pnt, timeout); }

        public int initiator_deselect_target(NFC.nfc_device pnd)
        { return base.pn53x_initiator_deselect_target(pnd); }

        public int initiator_transceive_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, int timeout)
        { return base.pn53x_initiator_transceive_bytes(pnd, pbtTx, szTx, pbtRx, szRx, timeout); }

        public int initiator_transceive_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar, byte[] pbtRx, int szRx, byte[] pbtRxPar)
        { return base.pn53x_initiator_transceive_bits(pnd, pbtTx, szTxBits, pbtTxPar, ref pbtRx, ref pbtRxPar); }//Todo check "szRx"

        public int initiator_transceive_bytes_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, ref UInt32 cycles)
        { return base.pn53x_initiator_transceive_bytes_timed(pnd, pbtTx, szTx, pbtRx, szRx, ref cycles); }

        public int initiator_transceive_bits_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar, byte[] pbtRx, int szRx, byte[] pbtRxPar, ref UInt32 cycles)
        { return base.pn53x_initiator_transceive_bits_timed(pnd, pbtTx, szTxBits, pbtTxPar, pbtRx, pbtRxPar, ref cycles); }

        public int initiator_target_is_present(NFC.nfc_device pnd, NFC.nfc_target pnt)
        { return base.pn53x_initiator_target_is_present(pnd, pnt); }

        public int target_init(NFC.nfc_device pnd, NFC.nfc_target pnt, ref byte[] pbtRx, int szRx, int timeout) //chekc if "szRx" should be ref/out
        { return base.pn53x_target_init(pnd, pnt, pbtRx, szRx, timeout); }

        public int target_send_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, int timeout)
        { return base.pn53x_target_send_bytes(pnd, pbtTx, szTx, timeout); }

        public int target_receive_bytes(NFC.nfc_device pnd, ref byte[] pbtRx, int szRxLen, int timeout)
        { return base.pn53x_target_receive_bytes(pnd, pbtRx, szRxLen, timeout); }

        public int target_send_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar)
        { return base.pn53x_target_send_bits(pnd, pbtTx, szTxBits, pbtTxPar); }

        public int target_receive_bits(NFC.nfc_device pnd, ref byte[] pbtRx, int szRxLen, byte[] pbtRxPar)
        { return base.pn53x_target_receive_bits(pnd, ref pbtRx, szRxLen, pbtRxPar); }

        public int device_set_property_bool(NFC.nfc_device pnd, NFC.nfc_property property, bool bEnable)
        { return base.pn53x_set_property_bool(pnd, property, bEnable); }

        public int device_set_property_int(NFC.nfc_device pnd, NFC.nfc_property property, int value)
        { return base.pn53x_set_property_int(pnd, property, value); }

        public int get_supported_modulation(NFC.nfc_device pnd, NFC.nfc_mode mode, out NFC.nfc_modulation_type[] supported_mt)
        { return base.pn53x_get_supported_modulation(pnd, mode, out supported_mt); }

        public int get_supported_baud_rate(NFC.nfc_device pnd, NFC.nfc_modulation_type nmt, out NFC.nfc_baud_rate[] supported_br)
        { return base.pn53x_get_supported_baud_rate(pnd, nmt, out supported_br); }

        public int device_get_information_about(NFC.nfc_device pnd, out string info)
        { return base.pn53x_get_information_about(pnd, out info); }

        public int abort_command(NFC.nfc_device pnd)
        {
            //Todo abort
            return -1;
        }

        public int idle(NFC.nfc_device pnd)
        { return base.pn53x_idle(pnd); }

        public int powerdown(NFC.nfc_device pnd)
        { return base.pn53x_PowerDown(pnd); }
    }
}
