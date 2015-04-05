using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibNFC4CSharp.driver;
using LibNFC4CSharp.nfc;
namespace LibNFC4CSharp.chips
{
    class pn53x : pn53x_internal
    {
        // Registers and symbols masks used to covers parts within a register
        //   PN53X_REG_CIU_TxMode
        public const byte SYMBOL_TX_CRC_ENABLE = 0x80;
        public const byte SYMBOL_TX_SPEED = 0x70;
        // TX_FRAMING bits explanation:
        //   00 : ISO/IEC 14443A/MIFARE and Passive Communication mode 106 kbit/s
        //   01 : Active Communication mode
        //   10 : FeliCa and Passive Communication mode at 212 kbit/s and 424 kbit/s
        //   11 : ISO/IEC 14443B
        public const byte SYMBOL_TX_FRAMING = 0x03;

        //   PN53X_REG_Control_switch_rng
        public const byte SYMBOL_CURLIMOFF = 0x08;    /* When set to 1, the 100 mA current limitations is desactivated. */
        public const byte SYMBOL_SIC_SWITCH_EN = 0x10;    /* When set to logic 1, the SVDD switch is enabled and the SVDD output delivers power to secure IC and internal pads (SIGIN, SIGOUT and P34). */
        public const byte SYMBOL_RANDOM_DATAREADY = 0x02;  /* When set to logic 1, a new random number is available. */

        //   PN53X_REG_CIU_RxMode
        public const byte SYMBOL_RX_CRC_ENABLE = 0x80;
        public const byte SYMBOL_RX_SPEED = 0x70;
        public const byte SYMBOL_RX_NO_ERROR = 0x08;
        public const byte SYMBOL_RX_MULTIPLE = 0x04;
        // RX_FRAMING follow same scheme than TX_FRAMING
        public const byte SYMBOL_RX_FRAMING = 0x03;

        //   PN53X_REG_CIU_TxAuto
        public const byte SYMBOL_FORCE_100_ASK = 0x40;
        public const byte SYMBOL_AUTO_WAKE_UP = 0x20;
        public const byte SYMBOL_INITIAL_RF_ON = 0x04;

        //   PN53X_REG_CIU_ManualRCV
        public const byte SYMBOL_PARITY_DISABLE = 0x10;

        //   PN53X_REG_CIU_TMode
        public const byte SYMBOL_TAUTO = 0x80;
        public const byte SYMBOL_TPRESCALERHI = 0x0F;

        //   PN53X_REG_CIU_TPrescaler
        public const byte SYMBOL_TPRESCALERLO = 0xFF;

        //   PN53X_REG_CIU_Command
        public const byte SYMBOL_COMMAND = 0x0F;
        public const byte SYMBOL_COMMAND_TRANSCEIVE = 0xC;

        //   PN53X_REG_CIU_Status2
        public const byte SYMBOL_MF_CRYPTO1_ON = 0x08;

        //   PN53X_REG_CIU_FIFOLevel
        public const byte SYMBOL_FLUSH_BUFFER = 0x80;
        public const byte SYMBOL_FIFO_LEVEL = 0x7F;

        //   PN53X_REG_CIU_Control
        public const byte SYMBOL_INITIATOR = 0x10;
        public const byte SYMBOL_RX_LAST_BITS = 0x07;

        //   PN53X_REG_CIU_BitFraming
        public const byte SYMBOL_START_SEND = 0x80;
        public const byte SYMBOL_RX_ALIGN = 0x70;
        public const byte SYMBOL_TX_LAST_BITS = 0x07;

        // PN53X Support Byte flags
        public const byte SUPPORT_ISO14443A = 0x01;
        public const byte SUPPORT_ISO14443B = 0x02;
        public const byte SUPPORT_ISO18092 = 0x04;

        // Internal parameters flags
        public const byte PARAM_NONE = 0x00;
        public const byte PARAM_NAD_USED = 0x01;
        public const byte PARAM_DID_USED = 0x02;
        public const byte PARAM_AUTO_ATR_RES = 0x04;
        public const byte PARAM_AUTO_RATS = 0x10;
        public const byte PARAM_14443_4_PICC = 0x20; /* Only for PN532 */
        public const byte PARAM_NFC_SECURE = 0x20;/* Only for pn53x_type.PN533 */
        public const byte PARAM_NO_AMBLE = 0x40;/* Only for PN532 */

        // Radio Field Configure Items           // Configuration Data length
        public const byte RFCI_FIELD = 0x01;   //  1
        public const byte RFCI_TIMING = 0x02;    //  3
        public const byte RFCI_RETRY_DATA = 0x04;   //  1
        public const byte RFCI_RETRY_SELECT = 0x05;    //  3
        public const byte RFCI_ANALOG_TYPE_A_106 = 0x0A;    // 11
        public const byte RFCI_ANALOG_TYPE_A_212_424 = 0x0B;    //  8
        public const byte RFCI_ANALOG_TYPE_B = 0x0C;    //  3
        public const byte RFCI_ANALOG_TYPE_14443_4 = 0x0D;      //  9

        /**
         * @enum pn53x_power_mode
         * @brief PN53x power mode enumeration
         */
        public enum pn53x_power_mode
        {
            NORMAL,	// In that case, there is no power saved but the PN53x reacts as fast as possible on the host controller interface.
            POWERDOWN,	// Only on PN532, need to be wake up to process commands with a long preamble
            LOWVBAT	// Only on PN532, need to be wake up to process commands with a long preamble and SAMConfiguration command
        } ;

        /**
         * @enum pn53x_operating_mode
         * @brief PN53x operatin mode enumeration
         */
        public enum pn53x_operating_mode
        {
            IDLE,
            INITIATOR,
            TARGET,
        } ;

        /**
         * @enum pn532_sam_mode
         * @brief PN532 SAM mode enumeration
         */
        public enum pn532_sam_mode
        {
            PSM_NORMAL = 0x01,
            PSM_VIRTUAL_CARD = 0x02,
            PSM_WIRED_CARD = 0x03,
            PSM_DUAL_CARD = 0x04
        } ;



        /* defines */
        public const UInt32 PN53X_CACHE_REGISTER_MIN_ADDRESS = PN53X_REG_CIU_Mode;
        public const UInt32 PN53X_CACHE_REGISTER_MAX_ADDRESS = PN53X_REG_CIU_Coll;
        public const UInt32 PN53X_CACHE_REGISTER_SIZE = ((PN53X_CACHE_REGISTER_MAX_ADDRESS - PN53X_CACHE_REGISTER_MIN_ADDRESS) + 1);

        /**
         * @internal
         * @struct pn53x_data
         * @brief PN53x data structure
         */
        public class pn53x_data
        {
            /** Chip type (PN531, PN532 or pn53x_type.PN533) */
            public byte type;//pn53x_type
            /** Chip firmware text */
            public string firmware_text;//[22];
            /** Current power mode */
            public pn53x_power_mode power_mode;
            /** Current operating mode */
            public pn53x_operating_mode operating_mode;
            /** Current emulated target */
            public NFC.nfc_target current_target;
            /** Current sam mode (only applicable for PN532) */
            public pn532_sam_mode sam_mode;
            /** PN53x I/O functions stored in struct */
            //public pn532_uart.pn53x_io io;
            /** Last status byte returned by PN53x */
            public byte last_status_byte;
            /** Register cache for REG_CIU_BIT_FRAMING, SYMBOL_TX_LAST_BITS: The last TX bits setting, we need to reset this if it does not apply anymore */
            public byte ui8TxBits;
            /** Register cache for SetParameters function. */
            public byte ui8Parameters;
            /** Last sent command */
            public byte last_command;
            /** Interframe timer correction */
            public UInt16 timer_correction;
            /** Timer prescaler */
            public UInt16 timer_prescaler;
            /** WriteBack cache */
            public byte[] wb_data = new byte[PN53X_CACHE_REGISTER_SIZE];
            public byte[] wb_mask = new byte[PN53X_CACHE_REGISTER_SIZE];
            public bool wb_trigged;
            /** Command timeout */
            public int timeout_command;
            /** ATR timeout */
            public int timeout_atr;
            /** Communication timeout */
            public int timeout_communication;
            /** Supported modulation type */
            public NFC.nfc_modulation_type[] supported_modulation_as_initiator = null;
            public NFC.nfc_modulation_type[] supported_modulation_as_target = null;
        }



        /**
         * @enum pn53x_modulation
         * @brief NFC modulation enumeration
         */
        public enum pn53x_modulation
        {
            /** Undefined modulation */
            PM_UNDEFINED = -1,
            /** ISO14443-A (NXP MIFARE) http://en.wikipedia.org/wiki/MIFARE */
            PM_ISO14443A_106 = 0x00,
            /** JIS X 6319-4 (Sony Felica) http://en.wikipedia.org/wiki/FeliCa */
            PM_FELICA_212 = 0x01,
            /** JIS X 6319-4 (Sony Felica) http://en.wikipedia.org/wiki/FeliCa */
            PM_FELICA_424 = 0x02,
            /** ISO14443-B http://en.wikipedia.org/wiki/ISO/IEC_14443 (Not supported by PN531) */
            PM_ISO14443B_106 = 0x03,
            /** Jewel Topaz (Innovision Research & Development) (Not supported by PN531) */
            PM_JEWEL_106 = 0x04,
            /** ISO14443-B http://en.wikipedia.org/wiki/ISO/IEC_14443 (Not supported by PN531 nor PN532) */
            PM_ISO14443B_212 = 0x06,
            /** ISO14443-B http://en.wikipedia.org/wiki/ISO/IEC_14443 (Not supported by PN531 nor PN532) */
            PM_ISO14443B_424 = 0x07,
            /** ISO14443-B http://en.wikipedia.org/wiki/ISO/IEC_14443 (Not supported by PN531 nor PN532) */
            PM_ISO14443B_847 = 0x08,
        } ;

        /**
         * @enum pn53x_target_type
         * @brief NFC target type enumeration
         */
        public enum pn53x_target_type
        {
            /** Undefined target type */
            PTT_UNDEFINED = -1,
            /** Generic passive 106 kbps (ISO/IEC14443-4A, mifare, DEP) */
            PTT_GENERIC_PASSIVE_106 = 0x00,
            /** Generic passive 212 kbps (FeliCa, DEP) */
            PTT_GENERIC_PASSIVE_212 = 0x01,
            /** Generic passive 424 kbps (FeliCa, DEP) */
            PTT_GENERIC_PASSIVE_424 = 0x02,
            /** Passive 106 kbps ISO/IEC14443-4B */
            PTT_ISO14443_4B_106 = 0x03,
            /** Innovision Jewel tag */
            PTT_JEWEL_106 = 0x04,
            /** Mifare card */
            PTT_MIFARE = 0x10,
            /** FeliCa 212 kbps card */
            PTT_FELICA_212 = 0x11,
            /** FeliCa 424 kbps card */
            PTT_FELICA_424 = 0x12,
            /** Passive 106 kbps ISO/IEC 14443-4A */
            PTT_ISO14443_4A_106 = 0x20,
            /** Passive 106 kbps ISO/IEC 14443-4B with TCL flag */
            PTT_ISO14443_4B_TCL_106 = 0x23,
            /** DEP passive 106 kbps */
            PTT_DEP_PASSIVE_106 = 0x40,
            /** DEP passive 212 kbps */
            PTT_DEP_PASSIVE_212 = 0x41,
            /** DEP passive 424 kbps */
            PTT_DEP_PASSIVE_424 = 0x42,
            /** DEP active 106 kbps */
            PTT_DEP_ACTIVE_106 = 0x80,
            /** DEP active 212 kbps */
            PTT_DEP_ACTIVE_212 = 0x81,
            /** DEP active 424 kbps */
            PTT_DEP_ACTIVE_424 = 0x82,
        } ;

        /**
         * @enum pn53x_target_mode
         * @brief PN53x target mode enumeration
         */
        public enum pn53x_target_mode
        {
            /** Configure the PN53x to accept all initiator mode */
            PTM_NORMAL = 0x00,
            /** Configure the PN53x to accept to be initialized only in passive mode */
            PTM_PASSIVE_ONLY = 0x01,
            /** Configure the PN53x to accept to be initialized only as DEP target */
            PTM_DEP_ONLY = 0x02,
            /** Configure the PN532 to accept to be initialized only as ISO/IEC14443-4 PICC */
            PTM_ISO14443_4_PICC_ONLY = 0x04
        } ;

        public static readonly byte[] pn53x_ack_frame = { 0x00, 0x00, 0xff, 0x00, 0xff, 0x00 };
        public static readonly byte[] pn53x_nack_frame = { 0x00, 0x00, 0xff, 0xff, 0x00, 0x00 };
        public static readonly byte[] pn53x_error_frame = { 0x00, 0x00, 0xff, 0x01, 0xff, 0x7f, 0x81, 0x00 };
        static readonly NFC.nfc_baud_rate[] pn53x_iso14443a_supported_baud_rates = { NFC.nfc_baud_rate.NBR_106, 0 };
        static readonly NFC.nfc_baud_rate[] pn53x_felica_supported_baud_rates = { NFC.nfc_baud_rate.NBR_424, NFC.nfc_baud_rate.NBR_212, 0 };
        static readonly NFC.nfc_baud_rate[] pn53x_dep_supported_baud_rates = { NFC.nfc_baud_rate.NBR_424, NFC.nfc_baud_rate.NBR_212, NFC.nfc_baud_rate.NBR_106, 0 };
        static readonly NFC.nfc_baud_rate[] pn53x_jewel_supported_baud_rates = { NFC.nfc_baud_rate.NBR_106, 0 };
        static readonly NFC.nfc_baud_rate[] pn532_iso14443b_supported_baud_rates = { NFC.nfc_baud_rate.NBR_106, 0 };
        static readonly NFC.nfc_baud_rate[] pn533_iso14443b_supported_baud_rates = { NFC.nfc_baud_rate.NBR_847, NFC.nfc_baud_rate.NBR_424, NFC.nfc_baud_rate.NBR_212, NFC.nfc_baud_rate.NBR_106, 0 };
        static readonly NFC.nfc_modulation_type[] pn53x_supported_modulation_as_target = { NFC.nfc_modulation_type.NMT_ISO14443A, NFC.nfc_modulation_type.NMT_FELICA, NFC.nfc_modulation_type.NMT_DEP, 0 };


        static BufferBuilder abtReadRegisterCmd = null;
        static BufferBuilder abtWriteRegisterCmd = null;


        /* implementations */
        protected int
        pn53x_init(NFC.nfc_device pnd)
        {
            int res = 0;
            // GetFirmwareVersion command is used to set PN53x chips type (PN531, PN532 or pn53x_type.PN533)
            if ((res = pn53x_decode_firmware_version(pnd)) < 0)
            {
                return res;
            }
            if (null == ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator)
            {
                ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator = new NFCInternal.nfc_modulation_type[9];
                int nbSupportedModulation = 0;
                if ((pnd.btSupportByte & SUPPORT_ISO14443A) != 0)
                {
                    ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = NFCInternal.nfc_modulation_type.NMT_ISO14443A;
                    nbSupportedModulation++;
                    ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = NFCInternal.nfc_modulation_type.NMT_FELICA;
                    nbSupportedModulation++;
                }
                if ((pnd.btSupportByte & SUPPORT_ISO14443B) != 0)
                {
                    ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = NFCInternal.nfc_modulation_type.NMT_ISO14443B;
                    nbSupportedModulation++;
                }
                if (((pn53x_data)pnd.chip_data).type != pn53x_type.PN531)
                {
                    ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = NFCInternal.nfc_modulation_type.NMT_JEWEL;
                    nbSupportedModulation++;
                }
                ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = NFCInternal.nfc_modulation_type.NMT_DEP;
                nbSupportedModulation++;
                ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator[nbSupportedModulation] = 0;
            }

            if (null == ((pn53x_data)pnd.chip_data).supported_modulation_as_target)
            {
                ((pn53x_data)pnd.chip_data).supported_modulation_as_target = pn53x_supported_modulation_as_target;
            }

            // CRC handling should be enabled by default as declared in NFC.nfc_device_new
            // which is the case by default for pn53x, so nothing to do here
            // Parity handling should be enabled by default as declared in NFC.nfc_device_new
            // which is the case by default for pn53x, so nothing to do here

            // We can't read these parameters, so we set a default config by using the SetParameters wrapper
            // Note: pn53x_SetParameters() will save the sent value in pnd.ui8Parameters cache
            if ((res = pn53x_SetParameters(pnd, PARAM_AUTO_ATR_RES | PARAM_AUTO_RATS)) < 0)
            {
                return res;
            }

            if ((res = pn53x_reset_settings(pnd)) < 0)
            {
                return res;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_reset_settings(NFC.nfc_device pnd)
        {
            int res = 0;
            // Reset the ending transmission bits register, it is unknown what the last tranmission used there
            ((pn53x_data)pnd.chip_data).ui8TxBits = 0;
            if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_BitFraming, SYMBOL_TX_LAST_BITS, 0x00)) < 0)
            {
                return res;
            }
            // Make sure we reset the CRC and parity to chip handling.
            if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_HANDLE_CRC, true)) < 0)
                return res;
            if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_HANDLE_PARITY, true)) < 0)
                return res;
            // Activate "easy framing" feature by default
            if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true)) < 0)
                return res;
            // Deactivate the CRYPTO1 cipher, it may could cause problems when still active
            if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_ACTIVATE_CRYPTO1, false)) < 0)
                return res;

            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_transceive(NFC.nfc_device pnd, byte[] pbtTx, int szTx, /*out*/ byte[] pbtRx, int szRxLen, int timeout)
        {
            bool mi = false;
            int res = 0;
            if (((pn53x_data)pnd.chip_data).wb_trigged)
            {
                if ((res = pn53x_writeback_register(pnd)) < 0)
                {
                    return res;
                }
            }

            //PNCMD_TRACE(pbtTx[0]);
            if (timeout > 0)
            {
                ////log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "Timeout value: %d", timeout);
            }
            else if (timeout == 0)
            {
                ////log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "No timeout");
            }
            else if (timeout == -1)
            {
                timeout = ((pn53x_data)pnd.chip_data).timeout_command;
            }
            else
            {
                ////log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "Invalid timeout value: %d", timeout);
            }

            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;

            // Check if receiving buffers are available, if not, replace them
            if (szRxLen == 0 || null == pbtRx)
            {
                pbtRx = abtRx;
            }
            else
            {
                szRx = szRxLen;
            }

            // Call the send/receice callback functions of the current driver
            if ((res = /*((pn53x_data)pnd.chip_data).io.*/pnd.driver.send(pnd, pbtTx, szTx, timeout)) < 0)
            {
                return res;
            }

            // Command is sent, we store the command
            ((pn53x_data)pnd.chip_data).last_command = pbtTx[0];

            // Handle power mode for PN532
            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (TgInitAsTarget == pbtTx[0]))
            {  // PN532 automatically goes into PowerDown mode when TgInitAsTarget command will be sent
                ((pn53x_data)pnd.chip_data).power_mode = pn53x_power_mode.POWERDOWN;
            }

            if ((res = /*((pn53x_data)pnd.chip_data).io.*/pnd.driver.receive(pnd, ref pbtRx, szRx, timeout)) < 0)
            {
                return res;
            }

            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (TgInitAsTarget == pbtTx[0]))
            { // PN532 automatically wakeup on external RF field
                ((pn53x_data)pnd.chip_data).power_mode = pn53x_power_mode.NORMAL; // When TgInitAsTarget reply that means an external RF have waken up the chip
            }

            switch (pbtTx[0])
            {
                case PowerDown:
                case InDataExchange:
                case InCommunicateThru:
                case InJumpForPSL:
                case InPSL:
                case InATR:
                case InSelect:
                case InJumpForDEP:
                case TgGetData:
                case TgGetInitiatorCommand:
                case TgSetData:
                case TgResponseToInitiator:
                case TgSetGeneralBytes:
                case TgSetMetaData:
                    if ((pbtRx[0] & 0x80) != 0) { throw new Exception("NAD detected"); } // NAD detected
                    //      if (pbtRx[0] & 0x40) { abort(); } // MI detected
                    mi = (pbtRx[0] & 0x40) != 0;
                    ((pn53x_data)pnd.chip_data).last_status_byte = (byte)(pbtRx[0] & 0x3f);
                    break;
                case Diagnose:
                    if (pbtTx[1] == 0x06)
                    { // Diagnose: Card presence detection
                        ((pn53x_data)pnd.chip_data).last_status_byte = (byte)(pbtRx[0] & 0x3f);
                    }
                    else
                    {
                        ((pn53x_data)pnd.chip_data).last_status_byte = 0;
                    };
                    break;
                case InDeselect:
                case InRelease:
                    if (((pn53x_data)pnd.chip_data).type == pn53x_type.RCS360)
                    {
                        // Error code is in pbtRx[1] but we ignore error code anyway
                        // because other PN53x chips always return 0 on those commands
                        ((pn53x_data)pnd.chip_data).last_status_byte = 0;
                        break;
                    }
                    ((pn53x_data)pnd.chip_data).last_status_byte = (byte)(pbtRx[0] & 0x3f);
                    break;
                case ReadRegister:
                case WriteRegister:
                    if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
                    {
                        // pn53x_type.PN533 prepends its answer by the status byte
                        ((pn53x_data)pnd.chip_data).last_status_byte = (byte)(pbtRx[0] & 0x3f);
                    }
                    else
                    {
                        ((pn53x_data)pnd.chip_data).last_status_byte = 0;
                    }
                    break;
                default:
                    ((pn53x_data)pnd.chip_data).last_status_byte = 0;
                    break;
            }

            while (mi)
            {
                int res2;
                byte[] abtRx2 = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                // Send empty command to card
                if ((res2 = /*((pn53x_data)pnd.chip_data).io.*/pnd.driver.send(pnd, pbtTx, 2, timeout)) < 0)
                {
                    return res2;
                }
                if ((res2 = /*((pn53x_data)pnd.chip_data).io.*/pnd.driver.receive(pnd, ref abtRx2, abtRx2.Length, timeout)) < 0)
                {
                    return res2;
                }
                mi = (abtRx2[0] & 0x40) != 0;
                if ((int)(res + res2 - 1) > szRx)
                {
                    ((pn53x_data)pnd.chip_data).last_status_byte = ESMALLBUF;
                    break;
                }
                MiscTool.memcpy(pbtRx , res, abtRx2 , 1, res2 - 1);
                // Copy last status byte
                pbtRx[0] = abtRx2[0];
                res += res2 - 1;
            }

            szRx = (int)res;

            switch (((pn53x_data)pnd.chip_data).last_status_byte)
            {
                case 0:
                    res = (int)szRx;
                    break;
                case ETIMEOUT:
                case ECRC:
                case EPARITY:
                case EBITCOUNT:
                case EFRAMING:
                case EBITCOLL:
                case ERFPROTO:
                case ERFTIMEOUT:
                case EDEPUNKCMD:
                case EDEPINVSTATE:
                case ENAD:
                case ENFCID3:
                case EINVRXFRAM:
                case EBCC:
                case ECID:
                    res = NFC.NFC_ERFTRANS;
                    break;
                case ESMALLBUF:
                case EOVCURRENT:
                case EBUFOVF:
                case EOVHEAT:
                case EINBUFOVF:
                    res = NFC.NFC_ECHIP;
                    break;
                case EINVPARAM:
                case EOPNOTALL:
                case ECMD:
                case ENSECNOTSUPP:
                    res = NFC.NFC_EINVARG;
                    break;
                case ETGREL:
                case ECDISCARDED:
                    res = NFC.NFC_ETGRELEASED;
                    pn53x_current_target_free(pnd);
                    break;
                case EMFAUTH:
                    // When a MIFARE Classic AUTH fails, the tag is automatically in HALT state
                    res = NFC.NFC_EMFCAUTHFAIL;
                    break;
                default:
                    res = NFC.NFC_ECHIP;
                    break;
            };

            if (res < 0)
            {
                pnd.last_error = res;
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "Chip error: \"%s\" (%02x), returned error: \"%s\" (%d))", pn53x_strerror(pnd), ((pn53x_data)pnd.chip_data).last_status_byte, NFC.nfc_strerror(pnd), res);
                Logger.Log("[Error] Chip error: " + pn53x_strerror(pnd) + "(" + ((pn53x_data)pnd.chip_data).last_status_byte.ToString("X2") + "), returned error:" + NFC.nfc_strerror(pnd) + "(" + res + ")");
            }
            else
            {
                pnd.last_error = 0;
            }
            return res;
        }

        protected int
        pn53x_set_parameters(NFC.nfc_device pnd, byte ui8Parameter, bool bEnable)
        {
            byte ui8Value = (byte)((bEnable) ? (((pn53x_data)pnd.chip_data).ui8Parameters | ui8Parameter) : (((pn53x_data)pnd.chip_data).ui8Parameters & ~(ui8Parameter)));
            if (ui8Value != ((pn53x_data)pnd.chip_data).ui8Parameters)
            {
                return pn53x_SetParameters(pnd, ui8Value);
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_set_tx_bits(NFC.nfc_device pnd, byte ui8Bits)
        {
            // Test if we need to update the transmission bits register setting
            if (((pn53x_data)pnd.chip_data).ui8TxBits != ui8Bits)
            {
                int res = 0;
                // Set the amount of transmission bits in the PN53X chip register
                if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_BitFraming, SYMBOL_TX_LAST_BITS, ui8Bits)) < 0)
                    return res;

                // Store the new setting
                ((pn53x_data)pnd.chip_data).ui8TxBits = ui8Bits;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_wrap_frame(byte[] pbtTx, int szTxBits, byte[] pbtTxPar,
                         ref BufferBuilder pbtFrame)
        {
            byte btFrame;
            byte btData;
            UInt32 uiBitPos;
            UInt32 uiDataPos = 0;
            int szBitsLeft = szTxBits;
            int szFrameBits = 0;
            // Make sure we should frame at least something
            if (szBitsLeft == 0)
                return NFC.NFC_ECHIP;

            // Handle a short response (1byte) as a special case
            if (szBitsLeft < 9)
            {
                pbtFrame.append(pbtTx, szTxBits);
                szFrameBits = szTxBits;
                return szFrameBits;
            }
            // We start by calculating the frame length in bits
            szFrameBits = szTxBits + (szTxBits / 8);

            // Parse the data bytes and add the parity bits
            // This is really a sensitive process, Mirror.mirror the frame bytes and append parity bits
            // buffer = Mirror.mirror(frame-byte) + parity + Mirror.mirror(frame-byte) + parity + ...
            // split "buffer" up in segments of 8 bits again and Mirror.mirror them
            // air-bytes = Mirror.mirror(buffer-byte) + Mirror.mirror(buffer-byte) + Mirror.mirror(buffer-byte) + ..
            while (true)
            {
                // Reset the temporary frame byte;
                btFrame = 0;

                for (uiBitPos = 0; uiBitPos < 8; uiBitPos++)
                {
                    // Copy as much data that fits in the frame byte
                    btData = Mirror.mirror(pbtTx[uiDataPos]);
                    btFrame |= (byte)(btData >> (int)uiBitPos);
                    // Save this frame byte
                    pbtFrame.append(Mirror.mirror(btFrame));
                    // Set the remaining bits of the date in the new frame byte and append the parity bit
                    btFrame = (byte)(btData << (int)(8 - uiBitPos));
                    btFrame |= (byte)((pbtTxPar[uiDataPos] & 0x01) << (int)(7 - uiBitPos));
                    // Increase the data (without parity bit) position
                    uiDataPos++;
                    // Test if we are done
                    if (szBitsLeft < 9)
                    {
                        // Backup the frame bits we have so far
                        pbtFrame.append(Mirror.mirror(btFrame));
                        return szFrameBits;
                    }
                    szBitsLeft -= 8;
                }
                // Every 8 data bytes we lose one frame byte to the parities
                pbtFrame.append(Mirror.mirror(btFrame));
            }
        }

        protected int
        pn53x_unwrap_frame(byte[] pbtFrame, int szFrameBits, byte[] pbtRx, byte[] pbtRxPar)
        {
            byte btFrame;
            byte btData;
            byte uiBitPos;
            UInt32 uiDataPos = 0;
            UInt32 pbtFramePos = 0;
            int szBitsLeft = szFrameBits;
            int szRxBits = 0;

            // Make sure we should frame at least something
            if (szBitsLeft == 0)
                return NFC.NFC_ECHIP;

            // Handle a short response (1byte) as a special case
            if (szBitsLeft < 9)
            {
                pbtRx = pbtFrame;
                szRxBits = szFrameBits;
                return szRxBits;
            }
            // Calculate the data length in bits
            szRxBits = szFrameBits - (szFrameBits / 9);

            // Parse the frame bytes, remove the parity bits and store them in the parity array
            // This process is the reverse of WrapFrame(), look there for more info
            while (true)
            {
                for (uiBitPos = 0; uiBitPos < 8; uiBitPos++)
                {
                    btFrame = Mirror.mirror(pbtFrame[pbtFramePos+uiDataPos]);
                    btData = (byte)(btFrame << uiBitPos);
                    btFrame = Mirror.mirror(pbtFrame[pbtFramePos+uiDataPos + 1]);
                    btData |= (byte)(btFrame >> (8 - uiBitPos));
                    pbtRx[uiDataPos] = Mirror.mirror(btData);
                    if (pbtRxPar != null)
                        pbtRxPar[uiDataPos] = (byte)((btFrame >> (7 - uiBitPos)) & 0x01);
                    // Increase the data (without parity bit) position
                    uiDataPos++;
                    // Test if we are done
                    if (szBitsLeft < 9)
                        return szRxBits;
                    szBitsLeft -= 9;
                }
                // Every 8 data bytes we lose one frame byte to the parities
                pbtFramePos++;
            }
        }

        protected int
        pn53x_decode_target_data(byte[] pbtRawData, int szRawData, byte chip_type, NFC.nfc_modulation_type nmt,
                                ref NFC.nfc_target_info pnti)
        {
            byte szAttribRes;
            byte[] pbtUid;
            int offset = 0;
            switch (nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    // We skip the first byte: its the target number (Tg)
                    offset++;//pbtRawData++;

                    // Somehow they switched the lower and upper ATQA bytes around for the PN531 chipset
                    if (chip_type == pn53x_type.PN531)
                    {
                        pnti.nai.abtAtqa[1] = pbtRawData[offset++];
                        pnti.nai.abtAtqa[0] = pbtRawData[offset++];
                    }
                    else
                    {
                        pnti.nai.abtAtqa[0] = pbtRawData[offset++];
                        pnti.nai.abtAtqa[1] = pbtRawData[offset++];
                    }
                    pnti.nai.btSak = pbtRawData[offset++];
                    // Copy the NFCID1
                    pnti.nai.szUidLen = pbtRawData[offset++];
                    pbtUid = pbtRawData;
                    offset += pnti.nai.szUidLen;

                    // Did we received an optional ATS (Smardcard ATR)
                    if (szRawData > (pnti.nai.szUidLen + 5))
                    {
                        pnti.nai.szAtsLen = (pbtRawData[offset++] - 1);     // In pbtRawData, ATS Length byte is counted in ATS Frame.
                        MiscTool.memcpy(pnti.nai.abtAts,0, pbtRawData, offset,pnti.nai.szAtsLen);
                    }
                    else
                    {
                        pnti.nai.szAtsLen = 0;
                    }

                    // For PN531, strip CT (Cascade Tag) to retrieve and store the _real_ UID
                    // (e.g. 0x8801020304050607 is in fact 0x01020304050607)
                    if ((pnti.nai.szUidLen == 8) && (pbtUid[0] == 0x88))
                    {
                        pnti.nai.szUidLen = 7;
                        MiscTool.memcpy(pnti.nai.abtUid,0, pbtUid , 1, 7);
                        //      } else if ((pnti.nai.szUidLen == 12) && (pbtUid[0] == 0x88) && (pbtUid[4] == 0x88)) {
                    }
                    else if (pnti.nai.szUidLen > 10)
                    {
                        pnti.nai.szUidLen = 10;
                        MiscTool.memcpy(pnti.nai.abtUid,0, pbtUid , 1, 3);
                        MiscTool.memcpy(pnti.nai.abtUid , 3, pbtUid , 5, 3);
                        MiscTool.memcpy(pnti.nai.abtUid , 6, pbtUid , 8, 4);
                    }
                    else
                    {
                        // For PN532, pn53x_type.PN533
                        MiscTool.memcpy(pnti.nai.abtUid,0, pbtUid,0, pnti.nai.szUidLen);
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443B:
                    // We skip the first byte: its the target number (Tg)
                    offset++;//pbtRawData++;

                    // Now we are in ATQB, we skip the first ATQB byte always equal to 0x50
                    offset++;//pbtRawData++;

                    // Store the PUPI (Pseudo-Unique PICC Identifier)
                    MiscTool.memcpy(pnti.nbi.abtPupi,0, pbtRawData,offset, 4);
                    offset+=4;//pbtRawData += 4;

                    // Store the Application Data
                    MiscTool.memcpy(pnti.nbi.abtApplicationData,0, pbtRawData,offset, 4);
                    offset+=4;//pbtRawData += 4;

                    // Store the Protocol Info
                    MiscTool.memcpy(pnti.nbi.abtProtocolInfo,0, pbtRawData, offset, 3);
                    offset+=3;//pbtRawData += 3;

                    // We leave the ATQB field, we now enter in Card IDentifier
                    szAttribRes = pbtRawData[offset++];
                    if (szAttribRes != 0x00)
                    {
                        pnti.nbi.ui8CardIdentifier = pbtRawData[offset++];
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                    // Skip V & T Addresses
                    offset++;//pbtRawData++;
                    if (pbtRawData[offset] != 0x07)
                    { // 0x07 = REPGEN
                        return NFC.NFC_ECHIP;
                    }
                    offset++;//pbtRawData++;
                    // Store the UID
                    MiscTool.memcpy(pnti.nii.abtDIV,0, pbtRawData,offset, 4);
                    offset+=4;//pbtRawData += 4;
                    pnti.nii.btVerLog = pbtRawData[offset++];
                    if ((pnti.nii.btVerLog & 0x80)!=0)
                    { // Type = long?
                        pnti.nii.btConfig = pbtRawData[offset++];
                        if ((pnti.nii.btConfig & 0x40)!=0)
                        {
                            MiscTool.memcpy(pnti.nii.abtAtr, 0,pbtRawData,offset, szRawData - 8);
                            pnti.nii.szAtrLen = szRawData - 8;
                        }
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                    // Store the UID
                    MiscTool.memcpy(pnti.nsi.abtUID,0, pbtRawData, offset,8);
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                    // Store UID LSB
                    MiscTool.memcpy(pnti.nci.abtUID, 0, pbtRawData, offset, 2);
                    offset+=2;//pbtRawData += 2;
                    // Store Prod Code & Fab Code
                    pnti.nci.btProdCode = pbtRawData[offset++];
                    pnti.nci.btFabCode = pbtRawData[offset++];
                    // Store UID MSB
                    MiscTool.memcpy(pnti.nci.abtUID , 2, pbtRawData,offset, 2);
                    break;

                case NFC.nfc_modulation_type.NMT_FELICA:
                    // We skip the first byte: its the target number (Tg)
                    offset++;//pbtRawData++;

                    // Store the mandatory info
                    pnti.nfi.szLen = pbtRawData[offset++];
                    pnti.nfi.btResCode = pbtRawData[offset++];
                    // Copy the NFCID2t
                    MiscTool.memcpy(pnti.nfi.abtId,0, pbtRawData,offset, 8);
                    offset+=8;//pbtRawData += 8;
                    // Copy the felica padding
                    MiscTool.memcpy(pnti.nfi.abtPad,0, pbtRawData, offset,8);
                    offset+=8;//pbtRawData += 8;
                    // Test if the System code (SYST_CODE) is available
                    if (pnti.nfi.szLen > 18)
                    {
                        MiscTool.memcpy(pnti.nfi.abtSysCode, 0,pbtRawData,offset, 2);
                    }
                    break;
                case NFC.nfc_modulation_type.NMT_JEWEL:
                    // We skip the first byte: its the target number (Tg)
                    offset++;//pbtRawData++;

                    // Store the mandatory info
                    MiscTool.memcpy(pnti.nji.btSensRes, 0,pbtRawData, offset,2);
                    offset+=2;//pbtRawData += 2;
                    MiscTool.memcpy(pnti.nji.btId,0, pbtRawData,offset, 4);
                    break;
                // Should not happend...
                case NFC.nfc_modulation_type.NMT_DEP:
                    return NFC.NFC_ECHIP;
                    break;
            }
            return NFC.NFC_SUCCESS;
        }
        /*
        static void MiscTool.memcpy(byte[] dest, byte[] src, int size)
        {

            for (int i = 0; i < size; i++)
            {
                dest[i] = src[i];
            }
        }*/

        protected int
        pn53x_ReadRegister(NFC.nfc_device pnd, UInt16 ui16RegisterAddress, out byte ui8Value)
        {
            byte[] abtCmd = { ReadRegister, (byte)(ui16RegisterAddress >> 8), (byte)(ui16RegisterAddress & 0xff) };
            byte[] abtRegValue = new byte[2];
            int res = 0;
            ui8Value = 0x00;
            //PNREG_TRACE(ui16RegisterAddress);
            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtRegValue, abtRegValue.Length, -1)) < 0)
            {
                return res;
            }
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                // pn53x_type.PN533 prepends its answer by a status byte
                ui8Value = abtRegValue[1];
            }
            else
            {
                ui8Value = abtRegValue[0];
            }
            return NFC.NFC_SUCCESS;
        }
        protected 
        int pn53x_read_register(NFC.nfc_device pnd, UInt16 ui16RegisterAddress, out byte ui8Value)
        {
            return pn53x_ReadRegister(pnd, ui16RegisterAddress, out ui8Value);
        }

        protected int
        pn53x_WriteRegister(NFC.nfc_device pnd, UInt16 ui16RegisterAddress, byte ui8Value)
        {
            byte[] abtCmd = { WriteRegister, (byte)(ui16RegisterAddress >> 8), (byte)(ui16RegisterAddress & 0xff), ui8Value };
            //PNREG_TRACE(ui16RegisterAddress);
            return pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
        }

        protected int
        pn53x_write_register(NFC.nfc_device pnd, UInt16 ui16RegisterAddress, byte ui8SymbolMask, byte ui8Value)
        {
            if ((ui16RegisterAddress < PN53X_CACHE_REGISTER_MIN_ADDRESS) || (ui16RegisterAddress > PN53X_CACHE_REGISTER_MAX_ADDRESS))
            {
                // Direct write
                if (ui8SymbolMask != 0xff)
                {
                    int res = 0;
                    byte ui8CurrentValue;
                    if ((res = pn53x_read_register(pnd, ui16RegisterAddress, out ui8CurrentValue)) < 0)
                        return res;
                    byte ui8NewValue = (byte)((ui8Value & ui8SymbolMask) | (ui8CurrentValue & (~ui8SymbolMask)));
                    if (ui8NewValue != ui8CurrentValue)
                    {
                        return pn53x_WriteRegister(pnd, ui16RegisterAddress, ui8NewValue);
                    }
                }
                else
                {
                    return pn53x_WriteRegister(pnd, ui16RegisterAddress, ui8Value);
                }
            }
            else
            {
                // Write-back cache area
                UInt16 internal_address = (UInt16)(ui16RegisterAddress - PN53X_CACHE_REGISTER_MIN_ADDRESS);
                ((pn53x_data)pnd.chip_data).wb_data[internal_address] = (byte)((((pn53x_data)pnd.chip_data).wb_data[internal_address] & ((pn53x_data)pnd.chip_data).wb_mask[internal_address] & (~ui8SymbolMask)) | (ui8Value & ui8SymbolMask));
                ((pn53x_data)pnd.chip_data).wb_mask[internal_address] = (byte)(((pn53x_data)pnd.chip_data).wb_mask[internal_address] | ui8SymbolMask);
                ((pn53x_data)pnd.chip_data).wb_trigged = true;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_writeback_register(NFC.nfc_device pnd)
        {
            int res = 0;
            // TODO Check at each step (ReadRegister, WriteRegister) if we didn't exceed max supported frame length
            abtReadRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtReadRegisterCmd.append((byte)ReadRegister);

            // First step, it looks for registers to be read before applying the requested mask
            ((pn53x_data)pnd.chip_data).wb_trigged = false;
            for (int n = 0; n < PN53X_CACHE_REGISTER_SIZE; n++)
            {
                if ((((pn53x_data)pnd.chip_data).wb_mask[n]) != 0x00 && (((pn53x_data)pnd.chip_data).wb_mask[n] != 0xff))
                {
                    // This register needs to be read: mask is present but does not cover full data width (ie. mask != 0xff)
                    UInt16 pn53x_register_address = (UInt16)(PN53X_CACHE_REGISTER_MIN_ADDRESS + n);
                    abtReadRegisterCmd.append((byte)(pn53x_register_address >> 8));
                    abtReadRegisterCmd.append((byte)(pn53x_register_address & 0xff));
                }
            }

            if (abtReadRegisterCmd.Offset() > 1)
            {
                // It needs to read some registers
                byte[] abtRes = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                int szRes = abtRes.Length;
                // It transceives the previously ructed ReadRegister command
                if ((res = pn53x_transceive(pnd, abtReadRegisterCmd.bytes(), abtReadRegisterCmd.Offset(), abtRes, szRes, -1)) < 0)
                {
                    return res;
                }
                int i = 0;
                if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
                {
                    // pn53x_type.PN533 prepends its answer by a status byte
                    i = 1;
                }
                for (int n = 0; n < PN53X_CACHE_REGISTER_SIZE; n++)
                {
                    if ((((pn53x_data)pnd.chip_data).wb_mask[n]) != 0 && (((pn53x_data)pnd.chip_data).wb_mask[n] != 0xff))
                    {
                        ((pn53x_data)pnd.chip_data).wb_data[n] = (byte)((((pn53x_data)pnd.chip_data).wb_data[n] & ((pn53x_data)pnd.chip_data).wb_mask[n]) | (abtRes[i] & (~((pn53x_data)pnd.chip_data).wb_mask[n])));
                        if (((pn53x_data)pnd.chip_data).wb_data[n] != abtRes[i])
                        {
                            // Requested value is different from read one
                            ((pn53x_data)pnd.chip_data).wb_mask[n] = 0xff;  // We can now apply whole data bits
                        }
                        else
                        {
                            ((pn53x_data)pnd.chip_data).wb_mask[n] = 0x00;  // We already have the right value
                        }
                        i++;
                    }
                }
            }
            // Now, the writeback-cache only has masks with 0xff, we can start to WriteRegister
            abtWriteRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtWriteRegisterCmd.append(WriteRegister);
            for (int n = 0; n < PN53X_CACHE_REGISTER_SIZE; n++)
            {
                if (((pn53x_data)pnd.chip_data).wb_mask[n] == 0xff)
                {
                    UInt16 pn53x_register_address = (UInt16)(PN53X_CACHE_REGISTER_MIN_ADDRESS + n);
                    //PNREG_TRACE(pn53x_register_address);
                    abtWriteRegisterCmd.append((byte)(pn53x_register_address >> 8));
                    abtWriteRegisterCmd.append((byte)(pn53x_register_address & 0xff));
                    abtWriteRegisterCmd.append(((pn53x_data)pnd.chip_data).wb_data[n]);
                    // This register is handled, we reset the mask to prevent
                    ((pn53x_data)pnd.chip_data).wb_mask[n] = 0x00;
                }
            }

            if (abtWriteRegisterCmd.Offset() > 1)
            {
                // We need to write some registers
                if ((res = pn53x_transceive(pnd, abtWriteRegisterCmd.bytes(), abtWriteRegisterCmd.Offset(), null, 0, -1)) < 0)
                {
                    return res;
                }
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_decode_firmware_version(NFC.nfc_device pnd)
        {
            byte[] abtCmd = { GetFirmwareVersion };
            byte[] abtFw = new byte[4];
            int szFwLen = abtFw.Length;
            int res = 0;
            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtFw, szFwLen, -1)) < 0)
            {
                return res;
            }
            szFwLen = (int)res;
            // Determine which version of chip it is: PN531 will return only 2 bytes, while others return 4 bytes and have the first to tell the version IC
            if (szFwLen == 2)
            {
                ((pn53x_data)pnd.chip_data).type = pn53x_type.PN531;
            }
            else if (szFwLen == 4)
            {
                if (abtFw[0] == 0x32)
                { // PN532 version IC
                    ((pn53x_data)pnd.chip_data).type = pn53x_type.PN532;
                }
                else if (abtFw[0] == 0x33)
                { // pn53x_type.PN533 version IC
                    if (abtFw[1] == 0x01)
                    { // Sony ROM code
                        ((pn53x_data)pnd.chip_data).type = pn53x_type.RCS360;
                    }
                    else
                    {
                        ((pn53x_data)pnd.chip_data).type = pn53x_type.PN533;
                    }
                }
                else
                {
                    // Unknown version IC
                    return NFC.NFC_ENOTIMPL;
                }
            }
            else
            {
                // Unknown chip
                return NFC.NFC_ENOTIMPL;
            }
            // Convert firmware info in text, PN531 gives 2 bytes info, but PN532 and pn53x_type.PN533 gives 4
            switch (((pn53x_data)pnd.chip_data).type)
            {
                case pn53x_type.PN531:
                    //snprintf(((pn53x_data)pnd.chip_data).firmware_text, sizeof(((pn53x_data)pnd.chip_data).firmware_text), "PN531 v%d.%d", abtFw[0], abtFw[1]);
                    pnd.btSupportByte = SUPPORT_ISO14443A | SUPPORT_ISO18092;
                    break;
                case pn53x_type.PN532:
                    //snprintf(((pn53x_data)pnd.chip_data).firmware_text, sizeof(((pn53x_data)pnd.chip_data).firmware_text), "PN532 v%d.%d", abtFw[1], abtFw[2]);
                    pnd.btSupportByte = abtFw[3];
                    break;
                case pn53x_type.PN533:
                case pn53x_type.RCS360:
                    //snprintf(((pn53x_data)pnd.chip_data).firmware_text, sizeof(((pn53x_data)pnd.chip_data).firmware_text), "PN533 v%d.%d", abtFw[1], abtFw[2]);
                    pnd.btSupportByte = abtFw[3];
                    break;
                case pn53x_type.PN53X:
                    // Could not happend
                    break;
            }
            return NFC.NFC_SUCCESS;
        }

        protected byte
        pn53x_int_to_timeout(int ms)
        {
            byte res = 0;
            if (ms!=0)
            {
                res = 0x10;
                for (int i = 3280; i > 1; i /= 2)
                {
                    if (ms > i)
                        break;
                    res--;
                }
            }
            return res;
        }

        protected int
        pn53x_set_property_int(NFC.nfc_device pnd, NFC.nfc_property property, int value)
        {
            switch (property)
            {
                case NFC.nfc_property.NP_TIMEOUT_COMMAND:
                    ((pn53x_data)pnd.chip_data).timeout_command = value;
                    break;
                case NFC.nfc_property.NP_TIMEOUT_ATR:
                    ((pn53x_data)pnd.chip_data).timeout_atr = value;
                    return pn53x_RFConfiguration__Various_timings(pnd, pn53x_int_to_timeout(((pn53x_data)pnd.chip_data).timeout_atr), pn53x_int_to_timeout(((pn53x_data)pnd.chip_data).timeout_communication));
                    break;
                case NFC.nfc_property.NP_TIMEOUT_COM:
                    ((pn53x_data)pnd.chip_data).timeout_communication = value;
                    return pn53x_RFConfiguration__Various_timings(pnd, pn53x_int_to_timeout(((pn53x_data)pnd.chip_data).timeout_atr), pn53x_int_to_timeout(((pn53x_data)pnd.chip_data).timeout_communication));
                    break;
                // Following properties are invalid (not integer)
                case NFC.nfc_property.NP_HANDLE_CRC:
                case NFC.nfc_property.NP_HANDLE_PARITY:
                case NFC.nfc_property.NP_ACTIVATE_FIELD:
                case NFC.nfc_property.NP_ACTIVATE_CRYPTO1:
                case NFC.nfc_property.NP_INFINITE_SELECT:
                case NFC.nfc_property.NP_ACCEPT_INVALID_FRAMES:
                case NFC.nfc_property.NP_ACCEPT_MULTIPLE_FRAMES:
                case NFC.nfc_property.NP_AUTO_ISO14443_4:
                case NFC.nfc_property.NP_EASY_FRAMING:
                case NFC.nfc_property.NP_FORCE_ISO14443_A:
                case NFC.nfc_property.NP_FORCE_ISO14443_B:
                case NFC.nfc_property.NP_FORCE_SPEED_106:
                    return NFC.NFC_EINVARG;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_set_property_bool(NFC.nfc_device pnd, NFC.nfc_property property, bool bEnable)
        {
            byte btValue;
            int res = 0;
            switch (property)
            {
                case NFC.nfc_property.NP_HANDLE_CRC:
                    // Enable or disable automatic receiving/sending of CRC bytes
                    if (bEnable == pnd.bCrc)
                    {
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    }
                    // TX and RX are both represented by the symbol 0x80
                    btValue = (bEnable) ? (byte)0x80 : (byte)0x00;
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_TxMode, SYMBOL_TX_CRC_ENABLE, btValue)) < 0)
                        return res;
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_CRC_ENABLE, btValue)) < 0)
                        return res;
                    pnd.bCrc = bEnable;
                    return NFC.NFC_SUCCESS;
                    break;

                case NFC.nfc_property.NP_HANDLE_PARITY:
                    // Handle parity bit by PN53X chip or parse it as data bit
                    if (bEnable == pnd.bPar)
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    btValue = (bEnable) ? (byte)0x00 : SYMBOL_PARITY_DISABLE;
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_ManualRCV, SYMBOL_PARITY_DISABLE, btValue)) < 0)
                        return res;
                    pnd.bPar = bEnable;
                    return NFC.NFC_SUCCESS;
                    break;

                case NFC.nfc_property.NP_EASY_FRAMING:
                    pnd.bEasyFraming = bEnable;
                    return NFC.NFC_SUCCESS;
                    break;

                case NFC.nfc_property.NP_ACTIVATE_FIELD:
                    return pn53x_RFConfiguration__RF_field(pnd, bEnable);
                    break;

                case NFC.nfc_property.NP_ACTIVATE_CRYPTO1:
                    btValue = (bEnable) ? SYMBOL_MF_CRYPTO1_ON : (byte)0x00;
                    return pn53x_write_register(pnd, PN53X_REG_CIU_Status2, SYMBOL_MF_CRYPTO1_ON, btValue);
                    break;

                case NFC.nfc_property.NP_INFINITE_SELECT:
                    // TODO Made some research around this point:
                    // timings could be tweak better than this, and maybe we can tweak timings
                    // to "gain" a sort-of hardware polling (ie. like PN532 does)
                    pnd.bInfiniteSelect = bEnable;
                    return pn53x_RFConfiguration__MaxRetries(pnd,
                                                             (bEnable) ? (byte)0xff : (byte)0x00,        // MxRtyATR, default: active = 0xff, passive = 0x02
                                                             (bEnable) ? (byte)0xff : (byte)0x01,        // MxRtyPSL, default: 0x01
                                                             (bEnable) ? (byte)0xff : (byte)0x02         // MxRtyPassiveActivation, default: 0xff (0x00 leads to problems with PN531)
                                                            );
                    break;

                case NFC.nfc_property.NP_ACCEPT_INVALID_FRAMES:
                    btValue = (bEnable) ? SYMBOL_RX_NO_ERROR : (byte)0x00;
                    return pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_NO_ERROR, btValue);
                    break;

                case NFC.nfc_property.NP_ACCEPT_MULTIPLE_FRAMES:
                    btValue = (bEnable) ? SYMBOL_RX_MULTIPLE : (byte)0x00;
                    return pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_MULTIPLE, btValue);
                    break;

                case NFC.nfc_property.NP_AUTO_ISO14443_4:
                    if (bEnable == pnd.bAutoIso14443_4)
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    pnd.bAutoIso14443_4 = bEnable;
                    return pn53x_set_parameters(pnd, PARAM_AUTO_RATS, bEnable);
                    break;

                case NFC.nfc_property.NP_FORCE_ISO14443_A:
                    if (!bEnable)
                    {
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    }
                    // Force pn53x to be in ISO14443-A mode
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_TxMode, SYMBOL_TX_FRAMING, 0x00)) < 0)
                    {
                        return res;
                    }
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_FRAMING, 0x00)) < 0)
                    {
                        return res;
                    }
                    // Set the PN53X to force 100% ASK Modified miller decoding (default for 14443A cards)
                    return pn53x_write_register(pnd, PN53X_REG_CIU_TxAuto, SYMBOL_FORCE_100_ASK, 0x40);
                    break;

                case NFC.nfc_property.NP_FORCE_ISO14443_B:
                    if (!bEnable)
                    {
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    }
                    // Force pn53x to be in ISO14443-B mode
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_TxMode, SYMBOL_TX_FRAMING, 0x03)) < 0)
                    {
                        return res;
                    }
                    return pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_FRAMING, 0x03);
                    break;

                case NFC.nfc_property.NP_FORCE_SPEED_106:
                    if (!bEnable)
                    {
                        // Nothing to do
                        return NFC.NFC_SUCCESS;
                    }
                    // Force pn53x to be at 106 kbps
                    if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_TxMode, SYMBOL_TX_SPEED, 0x00)) < 0)
                    {
                        return res;
                    }
                    return pn53x_write_register(pnd, PN53X_REG_CIU_RxMode, SYMBOL_RX_SPEED, 0x00);
                    break;
                // Following properties are invalid (not boolean)
                case NFC.nfc_property.NP_TIMEOUT_COMMAND:
                case NFC.nfc_property.NP_TIMEOUT_ATR:
                case NFC.nfc_property.NP_TIMEOUT_COM:
                    return NFC.NFC_EINVARG;
                    break;
            }

            return NFC.NFC_EINVARG;
        }

        protected int
        pn53x_idle(NFC.nfc_device pnd)
        {
            int res = 0;
            switch (((pn53x_data)pnd.chip_data).operating_mode)
            {
                case pn53x_operating_mode.TARGET:
                    // InRelease used in target mode stops the target emulation and no more
                    // tag are seen from external initiator
                    if ((res = pn53x_InRelease(pnd, 0)) < 0)
                    {
                        return res;
                    }
                    if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (pnd.driver.powerdown(pnd) != 0))
                    {
                        // Use PowerDown to go in "Low VBat" power mode
                        if ((res = pnd.driver.powerdown(pnd)) < 0)
                        {
                            return res;
                        }
                    }
                    break;
                case pn53x_operating_mode.INITIATOR:
                    // Use InRelease to go in "Standby mode"
                    if ((res = pn53x_InRelease(pnd, 0)) < 0)
                    {
                        return res;
                    }
                    // Disable RF field to avoid heating
                    if ((res = NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_ACTIVATE_FIELD, false)) < 0)
                    {
                        return res;
                    }
                    if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (pnd.driver.powerdown(pnd) != 0))
                    {
                        // Use PowerDown to go in "Low VBat" power mode
                        if ((res = pnd.driver.powerdown(pnd)) < 0)
                        {
                            return res;
                        }
                    }
                    break;
                case pn53x_operating_mode.IDLE: // Nothing to do.
                    break;
            };
            // Clear the current NFC.nfc_target
            pn53x_current_target_free(pnd);
            ((pn53x_data)pnd.chip_data).operating_mode = pn53x_operating_mode.IDLE;
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_check_communication(NFC.nfc_device pnd)
        {
            byte[] abtCmd = { Diagnose, 0x00, (byte)'l', (byte)'i', (byte)'b', (byte)'n', (byte)'f', (byte)'c' };
            byte[] abtExpectedRx = { 0x00, (byte)'l', (byte)'i', (byte)'b', (byte)'n', (byte)'f', (byte)'c' };
            byte[] abtRx = new byte[abtExpectedRx.Length];
            int szRx = abtRx.Length;
            int res = 0;

            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtRx, szRx, 500)) < 0)
                return res;
            szRx = (int)res;
            if ((abtExpectedRx.Length == szRx) && (0 == MiscTool.memcmp(abtRx, abtExpectedRx, abtExpectedRx.Length)))
                return NFC.NFC_SUCCESS;

            return NFC.NFC_EIO;
        }

        protected int
        pn53x_initiator_init(NFC.nfc_device pnd)
        {
            pn53x_reset_settings(pnd);
            int res;
            if (((pn53x_data)pnd.chip_data).sam_mode != pn532_sam_mode.PSM_NORMAL)
            {
                if ((res = pn532_SAMConfiguration(pnd, pn532_sam_mode.PSM_NORMAL, -1)) < 0)
                {
                    return res;
                }
            }

            // Configure the PN53X to be an Initiator or Reader/Writer
            if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_Control, SYMBOL_INITIATOR, 0x10)) < 0)
                return res;

            ((pn53x_data)pnd.chip_data).operating_mode = pn53x_operating_mode.INITIATOR;
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn532_initiator_init_secure_element(NFC.nfc_device pnd)
        {
            return pn532_SAMConfiguration(pnd, pn532_sam_mode.PSM_WIRED_CARD, -1);
        }

        protected int
        pn53x_initiator_select_passive_target_ext(NFC.nfc_device pnd,
                                                  NFC.nfc_modulation nm,
                                                   byte[] pbtInitData, int szInitData,
                                                  /*ref out*/ NFC.nfc_target pnt,
                                                  int timeout)
        {
            byte[] abtTargetsData = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szTargetsData = abtTargetsData.Length;
            int res = 0;
            NFC.nfc_target nttmp = new NFCInternal.nfc_target() ;
            pnt = null;
            if (nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443BI || nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443B2SR || nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443B2CT)
            {
                if (((pn53x_data)pnd.chip_data).type == pn53x_type.RCS360)
                {
                    // TODO add support for RC-S360, at the moment it refuses to send raw frames without a first select
                    pnd.last_error = NFC.NFC_ENOTIMPL;
                    return pnd.last_error;
                }
                // No native support in InListPassiveTarget so we do discovery by hand
                if ((res = NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_FORCE_ISO14443_B, true)) < 0)
                {
                    return res;
                }
                if ((res = NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_FORCE_SPEED_106, true)) < 0)
                {
                    return res;
                }
                if ((res = NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_HANDLE_CRC, true)) < 0)
                {
                    return res;
                }
                if ((res = NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false)) < 0)
                {
                    return res;
                }
                bool found = false;
                do
                {
                    if (nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443B2SR)
                    {
                        // Some work to do before getting the UID...
                        byte[] abtInitiate = { 0x06, 0x00 };
                        int szInitiateLen = 2;
                        byte[] abtSelect = { 0x0e, 0x00 };
                        byte[] abtRx = new byte[1];
                        // Getting random Chip_ID
                        if ((res = pn53x_initiator_transceive_bytes(pnd, abtInitiate, szInitiateLen, abtRx, abtRx.Length, timeout)) < 0)
                        {
                            if ((res == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                            { // Chip timeout
                                continue;
                            }
                            else
                                return res;
                        }
                        abtSelect[1] = abtRx[0];
                        if ((res = pn53x_initiator_transceive_bytes(pnd, abtSelect, abtSelect.Length, abtRx, abtRx.Length, timeout)) < 0)
                        {
                            return res;
                        }
                        szTargetsData = (int)res;
                    }
                    else if (nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443B2CT)
                    {
                        // Some work to do before getting the UID...
                        byte[] abtReqt = { 0x10 };
                        byte[] abtTargetsDataTemp = new byte[abtTargetsData.Length - 2];
                        // Getting product code / fab code & store it in output buffer after the serial nr we'll obtain later
                        if ((res = pn53x_initiator_transceive_bytes(pnd, abtReqt, abtReqt.Length, abtTargetsDataTemp, abtTargetsDataTemp.Length, timeout)) < 0)
                        {
                            if ((res == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                            { // Chip timeout
                                continue;
                            }
                            else
                                return res;
                        }
                        szTargetsData = (int)res;
                        MiscTool.memcpy(abtTargetsData, 2, abtTargetsDataTemp, 0,szTargetsData);
                    }

                    if ((res = pn53x_initiator_transceive_bytes(pnd, pbtInitData, szInitData, abtTargetsData, abtTargetsData.Length, timeout)) < 0)
                    {
                        if ((res == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                        { // Chip timeout
                            continue;
                        }
                        else
                            return res;
                    }
                    szTargetsData = (int)res;
                    if (nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443B2CT)
                    {
                        if (szTargetsData != 2)
                            return 0; // Target is not ISO14443B2CT
                        byte[] abtRead = { 0xC4 }; // Reading UID_MSB (Read address 4)
                        byte[] abtTargetsDataTemp = new byte[abtTargetsData.Length - 4];
                        if ((res = pn53x_initiator_transceive_bytes(pnd, abtRead, abtRead.Length, abtTargetsDataTemp, abtTargetsDataTemp.Length, timeout)) < 0)
                        {
                            return res;
                        }
                        szTargetsData = 6; // u16 UID_LSB, u8 prod code, u8 fab code, u16 UID_MSB
                        MiscTool.memcpy(abtTargetsData, 4, abtTargetsDataTemp, 0, abtTargetsDataTemp.Length);
                    }
                    nttmp.nm = nm;
                    if ((res = pn53x_decode_target_data(abtTargetsData, szTargetsData, ((pn53x_data)pnd.chip_data).type, nm.nmt,ref (nttmp.nti))) < 0)
                    {
                        return res;
                    }
                    if (nm.nmt == NFC.nfc_modulation_type.NMT_ISO14443BI)
                    {
                        // Select tag
                        byte[] abtAttrib = new byte[6];
                        MiscTool.memcpy(abtAttrib,0, abtTargetsData, 0,abtAttrib.Length);
                        abtAttrib[1] = 0x0f; // ATTRIB
                        if ((res = pn53x_initiator_transceive_bytes(pnd, abtAttrib, abtAttrib.Length, null, 0, timeout)) < 0)
                        {
                            return res;
                        }
                        szTargetsData = (int)res;
                    }
                    found = true;
                    break;
                } while (pnd.bInfiniteSelect);
                if (!found)
                    return 0;
            }
            else
            {

                pn53x_modulation pm = pn53x_nm_to_pm(nm);
                if (pn53x_modulation.PM_UNDEFINED == pm)
                {
                    pnd.last_error = NFC.NFC_EINVARG;
                    return pnd.last_error;
                }

                if ((res = pn53x_InListPassiveTarget(pnd, pm, 1, pbtInitData, szInitData, abtTargetsData, ref szTargetsData, timeout)) <= 0)
                    return res;

                if (szTargetsData <= 1) // For Coverity to know szTargetsData is always > 1 if res > 0
                    return 0;

                nttmp.nm = nm;
                if ((res = pn53x_decode_target_data(MiscTool.SubBytes( abtTargetsData , 1), szTargetsData - 1, ((pn53x_data)pnd.chip_data).type, nm.nmt,ref nttmp.nti)) < 0)
                {
                    return res;
                }
            }
            if (pn53x_current_target_new(ref pnd, nttmp) == null)
            {
                pnd.last_error = NFC.NFC_ESOFT;
                return pnd.last_error;
            }
            // Is a tag info struct available
            //if (pnt != null)
            {
                pnt = nttmp;
            }
            return abtTargetsData[0];
        }

        protected int
        pn53x_initiator_select_passive_target(NFC.nfc_device pnd,
                                               NFC.nfc_modulation nm,
                                               byte[] pbtInitData, int szInitData,
                                              /*out*/ NFC.nfc_target pnt)
        {
            return pn53x_initiator_select_passive_target_ext(pnd, nm, pbtInitData, szInitData,/*ref out*/ pnt, 0);
        }

        protected int
        pn53x_initiator_poll_target(NFC.nfc_device pnd,
                                     NFC.nfc_modulation[] pnmModulations, int szModulations,
                                     byte uiPollNr, byte uiPeriod,
                                    out NFC.nfc_target pnt)
        {
            int res = 0;
            pnt = null;
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN532)
            {
                int szTargetTypes = 0;
                pn53x_target_type[] apttTargetTypes = new pn53x_target_type[32];
                MiscTool.memset < pn53x_target_type>(apttTargetTypes, pn53x_target_type.PTT_UNDEFINED, apttTargetTypes.Length);
                for (int n = 0; n < szModulations; n++)
                {
                    pn53x_target_type ptt = pn53x_nm_to_ptt(pnmModulations[n]);
                    if (pn53x_target_type.PTT_UNDEFINED == ptt)
                    {
                        pnd.last_error = NFC.NFC_EINVARG;
                        return pnd.last_error;
                    }
                    apttTargetTypes[szTargetTypes] = ptt;
                    if ((pnd.bAutoIso14443_4) && (ptt == pn53x_target_type.PTT_MIFARE))
                    { // Hack to have ATS
                        apttTargetTypes[szTargetTypes] = pn53x_target_type.PTT_ISO14443_4A_106;
                        szTargetTypes++;
                        apttTargetTypes[szTargetTypes] = pn53x_target_type.PTT_MIFARE;
                    }
                    szTargetTypes++;
                }
                NFC.nfc_target[] ntTargets = new NFC.nfc_target[2];
                if ((res = pn53x_InAutoPoll(pnd, apttTargetTypes, szTargetTypes, uiPollNr, uiPeriod,ref ntTargets, 0)) < 0)
                    return res;
                switch (res)
                {
                    case 1:
                        pnt = ntTargets[0];
                        if (pn53x_current_target_new(ref pnd,pnt) == null)
                        {
                            return pnd.last_error = NFC.NFC_ESOFT;
                        }
                        return res;
                        break;
                    case 2:
                        pnt = ntTargets[1]; // We keep the selected one
                        if (pn53x_current_target_new(ref pnd,pnt) == null)
                        {
                            return pnd.last_error = NFC.NFC_ESOFT;
                        }
                        return res;
                        break;
                    default:
                        return NFC.NFC_ECHIP;
                        break;
                }
            }
            else
            {
                bool bInfiniteSelect = pnd.bInfiniteSelect;
                int result = 0;
                if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_INFINITE_SELECT, true)) < 0)
                    return res;
                // FIXME It does not support DEP targets
                do
                {
                    for (int p = 0; p < uiPollNr; p++)
                    {
                        for (int n = 0; n < szModulations; n++)
                        {
                            byte[] pbtInitiatorData;
                            //int szInitiatorData;
                            pbtInitiatorData = NFC.prepare_initiator_data(pnmModulations[n]);
                            int timeout_ms = uiPeriod * 150;

                            if ((res = pn53x_initiator_select_passive_target_ext(pnd, pnmModulations[n], pbtInitiatorData, pbtInitiatorData.Length,/*refout*/ pnt, timeout_ms)) < 0)
                            {
                                if (pnd.last_error != NFC.NFC_ETIMEOUT)
                                {
                                    result = pnd.last_error;
                                    goto end;
                                }
                            }
                            else
                            {
                                result = res;
                                goto end;
                            }
                        }
                    }
                } while (uiPollNr == 0xff); // uiPollNr==0xff means infinite polling
            // We reach this point when each listing give no result, we simply have to return 0
            end:
                if (!bInfiniteSelect)
                {
                    if ((res = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_INFINITE_SELECT, false)) < 0)
                        return res;
                }
                return result;
            }
            return NFC.NFC_ECHIP;
        }

        protected int
        pn53x_initiator_select_dep_target(NFC.nfc_device pnd,
                                           NFC.nfc_dep_mode ndm, NFC.nfc_baud_rate nbr,
                                           NFC.nfc_dep_info pndiInitiator,
                                         ref NFC.nfc_target pnt,
                                           int timeout)
        {
            byte[] abtPassiveInitiatorData = { 0x00, 0xff, 0xff, 0x00, 0x0f }; // Only for 212/424 kpbs: First 4 bytes shall be set like this according to NFCIP-1, last byte is TSN (Time Slot Number)
            byte[] pbtPassiveInitiatorData = null;

            switch (nbr)
            {
                case NFC.nfc_baud_rate.NBR_212:
                case NFC.nfc_baud_rate.NBR_424:
                    // Only use this predefined bytes array when we are at 212/424kbps
                    pbtPassiveInitiatorData = abtPassiveInitiatorData;
                    break;

                case NFC.nfc_baud_rate.NBR_106:
                    // Nothing to do
                    break;
                case NFC.nfc_baud_rate.NBR_847:
                case NFC.nfc_baud_rate.NBR_UNDEFINED:
                    return NFC.NFC_EINVARG;
                    break;
            }

            pn53x_current_target_free(pnd);//Todo remove it;
            int res;
            if (null != pndiInitiator)
            {
                res = pn53x_InJumpForDEP(pnd, ndm, nbr, pbtPassiveInitiatorData, pndiInitiator.abtNFCID3, pndiInitiator.abtGB, pndiInitiator.szGB, pnt, timeout);
            }
            else
            {
                res = pn53x_InJumpForDEP(pnd, ndm, nbr, pbtPassiveInitiatorData, null, null, 0, pnt, timeout);
            }
            if (res > 0)
            {
                if (pn53x_current_target_new(ref pnd, pnt) == null)
                {
                    return NFC.NFC_ESOFT;
                }
            }
            return res;
        }

        protected int
        pn53x_initiator_transceive_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits,
                                         byte[] pbtTxPar,ref byte[] pbtRx,ref byte[] pbtRxPar)
        {
            int res = 0;
            int szFrameBits = 0;
            int szFrameBytes = 0;
            int szRxBits = 0;
            byte ui8rcc;
            byte ui8Bits = 0;
            //byte[] abtCmd = { InCommunicateThru };/*[PN53x_EXTENDED_FRAME__DATA_MAX_LEN]*/
            BufferBuilder abtCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtCmd.append(InCommunicateThru);
            // Check if we should prepare the parity bits ourself
            if (!pnd.bPar)
            {
                // Convert data with parity to a frame
                if ((res = pn53x_wrap_frame(pbtTx, szTxBits, pbtTxPar,ref abtCmd)) < 0)
                    return res;
                szFrameBits = res;
            }
            else
            {
                szFrameBits = szTxBits;
            }

            // Retrieve the leading bits
            ui8Bits = (byte)(szFrameBits % 8);

            // Get the amount of frame bytes + optional (1 byte if there are leading bits)
            szFrameBytes = (szFrameBits / 8) + ((ui8Bits == 0) ? 0 : 1);

            // When the parity is handled before us, we just copy the data
            if (pnd.bPar)
                abtCmd.append( pbtTx, szFrameBytes);

            // Set the amount of transmission bits in the PN53X chip register
            if ((res = pn53x_set_tx_bits(pnd, ui8Bits)) < 0)
                return res;

            // Send the frame to the PN53X chip and get the answer
            // We have to give the amount of bytes + (the command byte 0x42)
            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            if ((res = pn53x_transceive(pnd, abtCmd.bytes(), szFrameBytes + 1, abtRx, szRx, -1)) < 0)
                return res;
            szRx = (int)res;
            // Get the last bit-count that is stored in the received byte
            if ((res = pn53x_read_register(pnd, PN53X_REG_CIU_Control, out ui8rcc)) < 0)
                return res;
            ui8Bits = (byte)(ui8rcc & SYMBOL_RX_LAST_BITS);

            // Recover the real frame length in bits
            szFrameBits = ((szRx - 1 - ((ui8Bits == 0) ? 0 : 1)) * 8) + ui8Bits;

            if (pbtRx != null)
            {
                // Ignore the status byte from the PN53X here, it was checked earlier in pn53x_transceive()
                // Check if we should recover the parity bits ourself
                if (!pnd.bPar)
                {
                    // Unwrap the response frame
                    if ((res = pn53x_unwrap_frame(MiscTool.SubBytes( abtRx , 1), szFrameBits, pbtRx, pbtRxPar)) < 0)
                        return res;
                    szRxBits = res;
                }
                else
                {
                    // Save the received bits
                    szRxBits = szFrameBits;
                    // Copy the received bytes
                    MiscTool.memcpy(pbtRx, 0,abtRx , 1, szRx - 1);
                }
            }
            // Everything went successful
            return szRxBits;
        }

        protected int
        pn53x_initiator_transceive_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx,
                                          int szRx, int timeout)
        {
            int szExtraTxLen;
            byte[] abtCmd = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int res = 0;

            // We can not just send bytes without parity if while the PN53X expects we handled them
            if (!pnd.bPar)
            {
                pnd.last_error = NFC.NFC_EINVARG;
                return pnd.last_error;
            }

            // Copy the data into the command frame
            if (pnd.bEasyFraming)
            {
                abtCmd[0] = InDataExchange;
                abtCmd[1] = 1;              /* target number */
                MiscTool.memcpy(abtCmd , 2, pbtTx, 0,szTx);
                szExtraTxLen = 2;
            }
            else
            {
                abtCmd[0] = InCommunicateThru;
                MiscTool.memcpy(abtCmd , 1, pbtTx,0,szTx);
                szExtraTxLen = 1;
            }

            // To transfer command frames bytes we can not have any leading bits, reset this to zero
            if ((res = pn53x_set_tx_bits(pnd, 0)) < 0)
            {
                pnd.last_error = res;
                return pnd.last_error;
            }

            // Send the frame to the PN53X chip and get the answer
            // We have to give the amount of bytes + (the two command bytes 0xD4, 0x42)
            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            if ((res = pn53x_transceive(pnd, abtCmd, szTx + szExtraTxLen, abtRx, abtRx.Length, timeout)) < 0)
            {
                pnd.last_error = res;
                return pnd.last_error;
            }
            int szRxLen = (int)res - 1;
            if (pbtRx != null)
            {
                if (szRxLen > szRx)
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "Buffer size is too short: %" PRIuPTR " available(s), %" PRIuPTR " needed", szRx, szRxLen);
                    Logger.Log("Buffer size is too short: " + szRx + " available, " + szRxLen + " needed");
                    return NFC.NFC_EOVFLOW;
                }
                // Copy the received bytes
                MiscTool.memcpy(pbtRx, 0,abtRx , 1, szRxLen);
            }
            // Everything went successful, we return received bytes count
            return szRxLen;
        }

        protected void __pn53x_init_timer(NFC.nfc_device pnd, UInt32 max_cycles)
        {
            // The prescaler will dictate what will be the precision and
            // the largest delay to measure before saturation. Some examples:
            // prescaler =  0 => precision:  ~73ns  timer saturates at    ~5ms
            // prescaler =  1 => precision: ~221ns  timer saturates at   ~15ms
            // prescaler =  2 => precision: ~369ns  timer saturates at   ~25ms
            // prescaler = 10 => precision: ~1.5us  timer saturates at  ~100ms
            if (max_cycles > 0xFFFF)
            {
                ((pn53x_data)pnd.chip_data).timer_prescaler = (UInt16)(((max_cycles / 0xFFFF) - 1) / 2);
            }
            else
            {
                ((pn53x_data)pnd.chip_data).timer_prescaler = 0;
            }
            UInt16 reloadval = 0xFFFF;
            // Initialize timer
            pn53x_write_register(pnd, PN53X_REG_CIU_TMode, 0xFF, (byte)(SYMBOL_TAUTO | ((((pn53x_data)pnd.chip_data).timer_prescaler >> 8) & SYMBOL_TPRESCALERHI)));
            pn53x_write_register(pnd, PN53X_REG_CIU_TPrescaler, 0xFF, (byte)((((pn53x_data)pnd.chip_data).timer_prescaler & SYMBOL_TPRESCALERLO)));
            pn53x_write_register(pnd, PN53X_REG_CIU_TReloadVal_hi, 0xFF, (byte)((reloadval >> 8) & 0xFF));
            pn53x_write_register(pnd, PN53X_REG_CIU_TReloadVal_lo, (byte)0xFF, (byte)(reloadval & 0xFF));
        }

        protected UInt32 __pn53x_get_timer(NFC.nfc_device pnd, byte last_cmd_byte)
        {
            byte parity;
            byte counter_hi, counter_lo;
            UInt16 counter, u16cycles;
            UInt32 u32cycles;
            int off = 0;
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                // pn53x_type.PN533 prepends its answer by a status byte
                off = 1;
            }
            // Read timer
            abtReadRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtReadRegisterCmd.append(ReadRegister);
            abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_TCounterVal_hi >> 8));
            abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_TCounterVal_hi & 0xff));
            abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_TCounterVal_lo >> 8));
            abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_TCounterVal_lo & 0xff));
            byte[] abtRes = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRes = abtRes.Length;
            // Let's send the previously ructed ReadRegister command
            if (pn53x_transceive(pnd, abtReadRegisterCmd.bytes(), abtReadRegisterCmd.Offset(), abtRes, szRes, -1) < 0)
            {
                return 0;
            }
            counter_hi = abtRes[off];
            counter_lo = abtRes[off + 1];
            counter = counter_hi;
            counter = (UInt16)((counter << 8) + counter_lo);
            if (counter == 0)
            {
                // counter saturated
                u32cycles = 0xFFFFFFFF;
            }
            else
            {
                u16cycles = (UInt16)(0xFFFF - counter);
                u32cycles = u16cycles;
                u32cycles *= (UInt16)(((pn53x_data)pnd.chip_data).timer_prescaler * 2 + 1);
                u32cycles++;
                // Correction depending on PN53x Rx detection handling:
                // timer stops after 5 (or 2 for PN531) bits are received
                if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN531)
                {
                    u32cycles -= (2 * 128);
                }
                else
                {
                    u32cycles -= (5 * 128);
                }
                // Correction depending on last parity bit sent
                parity = (byte)((last_cmd_byte >> 7) ^ ((last_cmd_byte >> 6) & 1) ^
                         ((last_cmd_byte >> 5) & 1) ^ ((last_cmd_byte >> 4) & 1) ^
                         ((last_cmd_byte >> 3) & 1) ^ ((last_cmd_byte >> 2) & 1) ^
                         ((last_cmd_byte >> 1) & 1) ^ (last_cmd_byte & 1));
                parity = parity != 0x00 ? (byte)0 : (byte)0x01;
                // When sent ...YY (cmd ends with logical 1, so when last parity bit is 1):
                if (parity!=0x00)
                {
                    // it finishes 64us sooner than a ...ZY signal
                    u32cycles += 64;
                }
                // Correction depending on device design
                u32cycles += ((pn53x_data)pnd.chip_data).timer_correction;
            }
            return u32cycles;
        }

        protected int
        pn53x_initiator_transceive_bits_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits,
                                               byte[] pbtTxPar, byte[] pbtRx, byte[] pbtRxPar, ref UInt32 cycles)
        {
            // TODO Do something with these bytes...
            if (null == pbtTxPar) throw new Exception("Invalid argument: pbtTxPar == null");
            if (null == pbtRxPar) throw new Exception("Invalid argument: pbtTxPar == null");
            UInt16 i;
            byte sz = 0;
            int res = 0;
            int szRxBits = 0;

            // Sorry, no arbitrary parity bits support for now
            if (!pnd.bPar)
            {
                pnd.last_error = NFC.NFC_ENOTIMPL;
                return pnd.last_error;
            }
            // Sorry, no easy framing support
            if (pnd.bEasyFraming)
            {
                pnd.last_error = NFC.NFC_ENOTIMPL;
                return pnd.last_error;
            }
            // TODO CRC support but it probably doesn't make sense for (szTxBits % 8 != 0) ...
            if (pnd.bCrc)
            {
                pnd.last_error = NFC.NFC_ENOTIMPL;
                return pnd.last_error;
            }

            __pn53x_init_timer(pnd, cycles);

            // Once timer is started, we cannot use Tama commands anymore.
            // E.g. on SCL3711 timer settings are reset by 0x42 InCommunicateThru command to:
            //  631a=82 631b=a5 631c=02 631d=00
            // Prepare FIFO
            abtWriteRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtWriteRegisterCmd.append(WriteRegister);

            abtWriteRegisterCmd.append(PN53X_REG_CIU_Command >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_Command & 0xff);
            abtWriteRegisterCmd.append(SYMBOL_COMMAND & SYMBOL_COMMAND_TRANSCEIVE);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOLevel >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOLevel & 0xff);
            abtWriteRegisterCmd.append(SYMBOL_FLUSH_BUFFER);
            for (i = 0; i < ((szTxBits / 8) + 1); i++)
            {
                abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOData >> 8);
                abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOData & 0xff);
                abtWriteRegisterCmd.append(pbtTx[i]);
            }
            // Send data
            abtWriteRegisterCmd.append(PN53X_REG_CIU_BitFraming >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_BitFraming & 0xff);
            abtWriteRegisterCmd.append((byte)(SYMBOL_START_SEND | ((szTxBits % 8) & SYMBOL_TX_LAST_BITS)));
            // Let's send the previously ructed WriteRegister command
            if ((res = pn53x_transceive(pnd, abtWriteRegisterCmd.bytes(), abtWriteRegisterCmd.Offset(), null, 0, -1)) < 0)
            {
                return res;
            }

            // Recv data
            // we've to watch for coming data until we decide to timeout.
            // our PN53x timer saturates after 4.8ms so this function shouldn't be used for
            // responses coming very late anyway.
            // Ideally we should implement a real timer here too but looping a few times is good enough.
            for (i = 0; i < (3 * (((pn53x_data)pnd.chip_data).timer_prescaler * 2 + 1)); i++)
            {
                pn53x_read_register(pnd, PN53X_REG_CIU_FIFOLevel, out sz);
                if (sz > 0)
                    break;
            }
            int off = 0;
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                // pn53x_type.PN533 prepends its answer by a status byte
                off = 1;
            }
            while (true)
            {
                abtReadRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
                abtReadRegisterCmd.append((byte)ReadRegister);
                for (i = 0; i < sz; i++)
                {
                    abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOData >> 8));
                    abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOData & 0xff));
                }
                abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOLevel >> 8));
                abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOLevel & 0xff));
                byte[] abtRes = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                int szRes = abtRes.Length;
                // Let's send the previously ructed ReadRegister command
                if ((res = pn53x_transceive(pnd, abtReadRegisterCmd.bytes(), abtReadRegisterCmd.Offset(), abtRes, szRes, -1)) < 0)
                {
                    return res;
                }
                for (i = 0; i < sz; i++)
                {
                    pbtRx[i + szRxBits] = abtRes[i + off];
                }
                szRxBits += (int)(sz & SYMBOL_FIFO_LEVEL);
                sz = abtRes[sz + off];
                if (sz == 0)
                    break;
            }
            szRxBits *= 8; // in bits, not bytes

            // Recv corrected timer value
            cycles = __pn53x_get_timer(pnd, pbtTx[szTxBits / 8]);

            return szRxBits;
        }


        public class BufferBuilder
        {
            int offset = 0;
            byte[] buf = null;
            public BufferBuilder(uint size)
            {
                buf = new byte[size];
                offset = 0;
            }
            public BufferBuilder(byte[] alia)
            {
                buf = alia;
                offset = 0;
            }
            public int Length()
            {
                return buf.Length;
            }
            public int Offset()
            {
                return offset;
            }

            public byte[] bytes()
            {
                byte[] b = new byte[offset];
                for (int i = 0; i < offset; i++)
                {
                    b[i] = buf[i];
                }
                return b;
            }

            public byte[] buffer()
            {
                return buf;
            }
            public int append(byte b)
            {
                if (offset >= buf.Length)
                {
                    return 0;
                }
                buf[offset] = b;
                offset++;
                return 1;
            }

            public int append(byte[] b)
            {
                int i = 0;
                for (i = 0; (i < b.Length) && (i + offset < buf.Length); i++)
                {
                    buf[i + offset] = b[i];
                }
                return i;
            }
            public int append(byte[] b, int length)
            {
                int i = 0;
                for (i = 0; (i < length) && (i + offset < buf.Length); i++)
                {
                    buf[i + offset] = b[i];
                }
                return i;
            }
            public void clear()
            {
                for (int i = 0; i < offset; i++) buf[i] = (byte)0x00;
                offset = 0;
            }

            protected void append(byte[] dest, int offset, byte[] src, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    dest[offset + i] = src[i];
                }
            }
        }

        protected int
        pn53x_initiator_transceive_bytes_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, ref UInt32 cycles)
        {
            UInt16 i;
            byte sz = 0;
            int res = 0;

            // We can not just send bytes without parity while the PN53X expects we handled them
            if (!pnd.bPar)
            {
                pnd.last_error = NFC.NFC_EINVARG;
                return pnd.last_error;
            }
            // Sorry, no easy framing support
            // TODO to be changed once we'll provide easy framing support from libnfc itself...
            if (pnd.bEasyFraming)
            {
                pnd.last_error = NFC.NFC_ENOTIMPL;
                return pnd.last_error;
            }

            byte txmode = 0;
            if (pnd.bCrc)
            { // check if we're in TypeA or TypeB mode to compute right CRC later
                if ((res = pn53x_read_register(pnd, PN53X_REG_CIU_TxMode, out txmode)) < 0)
                {
                    return res;
                }
            }

            __pn53x_init_timer(pnd, cycles);

            // Once timer is started, we cannot use Tama commands anymore.
            // E.g. on SCL3711 timer settings are reset by 0x42 InCommunicateThru command to:
            //  631a=82 631b=a5 631c=02 631d=00
            // Prepare FIFO
            abtWriteRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtWriteRegisterCmd.append(WriteRegister);

            abtWriteRegisterCmd.append(PN53X_REG_CIU_Command >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_Command & 0xff);
            abtWriteRegisterCmd.append(SYMBOL_COMMAND & SYMBOL_COMMAND_TRANSCEIVE);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOLevel >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOLevel & 0xff);
            abtWriteRegisterCmd.append(SYMBOL_FLUSH_BUFFER);
            for (i = 0; i < szTx; i++)
            {
                abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOData >> 8);
                abtWriteRegisterCmd.append(PN53X_REG_CIU_FIFOData & 0xff);
                abtWriteRegisterCmd.append(pbtTx[i]);
            }
            // Send data
            abtWriteRegisterCmd.append(PN53X_REG_CIU_BitFraming >> 8);
            abtWriteRegisterCmd.append(PN53X_REG_CIU_BitFraming & 0xff);
            abtWriteRegisterCmd.append(SYMBOL_START_SEND);
            // Let's send the previously ructed WriteRegister command
            if ((res = pn53x_transceive(pnd, abtWriteRegisterCmd.bytes(), abtWriteRegisterCmd.Offset(), null, 0, -1)) < 0)
            {
                return res;
            }

            // Recv data
            int szRxLen = 0;
            // we've to watch for coming data until we decide to timeout.
            // our PN53x timer saturates after 4.8ms so this function shouldn't be used for
            // responses coming very late anyway.
            // Ideally we should implement a real timer here too but looping a few times is good enough.
            for (i = 0; i < (3 * (((pn53x_data)pnd.chip_data).timer_prescaler * 2 + 1)); i++)
            {
                pn53x_read_register(pnd, PN53X_REG_CIU_FIFOLevel, out sz);
                if (sz > 0)
                    break;
            }
            int off = 0;
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                // pn53x_type.PN533 prepends its answer by a status byte
                off = 1;
            }
            while (true)
            {
                abtReadRegisterCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
                abtReadRegisterCmd.append(ReadRegister);
                for (i = 0; i < sz; i++)
                {
                    abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOData >> 8));
                    abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOData & 0xff));
                }
                abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOLevel >> 8));
                abtReadRegisterCmd.append((byte)(PN53X_REG_CIU_FIFOLevel & 0xff));
                byte[] abtRes = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                int szRes = abtRes.Length;
                // Let's send the previously ructed ReadRegister command
                if ((res = pn53x_transceive(pnd, abtReadRegisterCmd.bytes(), abtReadRegisterCmd.Offset(), abtRes, szRes, -1)) < 0)
                {
                    return res;
                }
                if (pbtRx != null)
                {
                    if ((szRxLen + sz) > szRx)
                    {
                        //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "Buffer size is too short: %" PRIuPTR " available(s), %" PRIuPTR " needed", szRx, szRxLen + sz);
                        return NFC.NFC_EOVFLOW;
                    }
                    // Copy the received bytes
                    for (i = 0; i < sz; i++)
                    {
                        pbtRx[i + szRxLen] = abtRes[i + off];
                    }
                }
                szRxLen += (int)(sz & SYMBOL_FIFO_LEVEL);
                sz = abtRes[sz + off];
                if (sz == 0)
                    break;
            }

            // Recv corrected timer value
            if (pnd.bCrc)
            {
                // We've to compute CRC ourselves to know last byte actually sent
                byte[] pbtTxRaw = new byte[szTx + 2];
                MiscTool.memcpy(pbtTxRaw,0, pbtTx,0, szTx);
                if ((txmode & SYMBOL_TX_FRAMING) == 0x00)
                    ISO14443Subr.iso14443a_crc_append(pbtTxRaw, szTx);
                else if ((txmode & SYMBOL_TX_FRAMING) == 0x03)
                    ISO14443Subr.iso14443b_crc_append(pbtTxRaw, szTx);
                else
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "Unsupported framing type %02X, cannot adjust CRC cycles", txmode & SYMBOL_TX_FRAMING);
                    cycles = __pn53x_get_timer(pnd, pbtTxRaw[szTx + 1]);
                //free(pbtTxRaw);
            }
            else
            {
                cycles = __pn53x_get_timer(pnd, pbtTx[szTx - 1]);
            }
            return szRxLen;
        }

        protected int
        pn53x_initiator_deselect_target(NFC.nfc_device pnd)
        {
            pn53x_current_target_free(pnd);
            return pn53x_InDeselect(pnd, 0);    // 0 mean deselect all selected targets
        }

        protected int pn53x_Diagnose06(NFC.nfc_device pnd)
        {
            // Send Card Presence command
            byte[] abtCmd = { Diagnose, 0x06 };
            byte[] abtRx = new byte[1];
            int ret = 0;
            int failures = 0;

            // Card Presence command can take more time than default one: when a card is
            // removed from the field, the PN53x took few hundred ms more to reply
            // correctly. Longest delay observed was with a JCOP31 on a PN532.
            // 1000 ms should be enough to detect all tested cases
            while (failures < 2)
            {
                if ((ret = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtRx, abtRx.Length, 1000)) != 1)
                {
                    // When it fails with a timeout (0x01) chip error, it means the target is not reacheable anymore
                    if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                    {
                        return NFC.NFC_ETGRELEASED;
                    }
                    else
                    { // Other errors can appear when card is tired-off, let's try again
                        failures++;
                    }
                }
                else
                {
                    return NFC.NFC_SUCCESS;
                }
            }
            return ret;
        }

        protected int pn53x_ISO14443A_4_is_present(NFC.nfc_device pnd)
        {
            int ret;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping -4A");
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                ret = pn53x_Diagnose06(pnd);
                if ((ret == NFC.NFC_ETIMEOUT) || (ret == NFC.NFC_ETGRELEASED))
                {
                    // This happens e.g. when a JCOP31 is removed from pn53x_type.PN533
                    // InRelease takes an abnormal time to reply so let's take care of it now with large timeout:
                    byte[] abtCmd = { InRelease, 0x00 };
                    pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, 2000);
                    ret = NFC.NFC_ETGRELEASED;
                }
            }
            else if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN532)
            {
                // Diagnose06 failed completely with a JCOP31 on a PN532 so let's do it manually
                if ((ret = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false)) < 0)
                    return ret;
                byte[] abtCmd = new byte[1] { 0xb2 }; // CID=0
                int failures = 0;
                while (failures < 2)
                {
                    if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length, null, 0, 300)) < 1)
                    {
                        if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                        { // Timeout
                            ret = NFC.NFC_ETGRELEASED;
                            break;
                        }
                        else
                        { // Other errors can appear when card is tired-off, let's try again
                            failures++;
                        }
                    }
                    else
                    {
                        ret = NFC.NFC_SUCCESS;
                        break;
                    }
                }
                int ret2;
                if ((ret2 = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true)) < 0)
                    ret = ret2;
            }
            else
            {
                ret = NFC.NFC_EDEVNOTSUPP;
            }
            return ret;
        }

        protected int pn53x_ISO14443A_Jewel_is_present(NFC.nfc_device pnd)
        {
            int ret = 0;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping Jewel");
            byte[] abtCmd = new byte[1] { 0x78 };
            int failures = 0;
            while (failures < 2)
            {
                if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length,null, 0, -1)) < 1)
                {
                    if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                    { // Timeout
                        return NFC.NFC_ETGRELEASED;
                    }
                    else
                    { // Other errors can appear when card is tired-off, let's try again
                        failures++;
                    }
                }
                else
                {
                    return NFC.NFC_SUCCESS;
                }
            }
            return ret;
        }

        protected int pn53x_ISO14443A_MFUL_is_present(NFC.nfc_device pnd)
        {
            int ret = 0;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping MFUL");
            // Limitation: test on MFULC non-authenticated with read of first sector forbidden will fail
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            {
                ret = pn53x_Diagnose06(pnd);
            }
            else
            {
                byte[] abtCmd = new byte[2] { 0x30, 0x00 };
                int failures = 0;
                while (failures < 2)
                {
                    if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length, null, 0, -1)) < 1)
                    {
                        if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                        { // Timeout
                            return NFC.NFC_ETGRELEASED;
                        }
                        else
                        { // Other errors can appear when card is tired-off, let's try again
                            failures++;
                        }
                    }
                    else
                    {
                        return NFC.NFC_SUCCESS;
                    }
                }
            }
            return ret;
        }

        protected int pn53x_ISO14443A_MFC_is_present(NFC.nfc_device pnd)
        {
            int ret;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping MFC");
            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN533) && (((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak != 0x09))
            {
                // MFC Mini (atqa0004/sak09) fails on pn53x_type.PN533, so we exclude it
                ret = pn53x_Diagnose06(pnd);
            }
            else
            {
                // Limitation: re-select will lose authentication of already authenticated sector
                bool bInfiniteSelect = pnd.bInfiniteSelect;
                byte[] pbtInitiatorData = new byte[12];
                int szInitiatorData = 0;
                NFC.nfc_target pnt = null;
                NFC.iso14443_cascade_uid(((pn53x_data)pnd.chip_data).current_target.nti.nai.abtUid, ((pn53x_data)pnd.chip_data).current_target.nti.nai.szUidLen,ref pbtInitiatorData,out szInitiatorData);
                if ((ret = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_INFINITE_SELECT, false)) < 0)
                    return ret;
                if ((ret = pn53x_initiator_select_passive_target_ext(pnd, ((pn53x_data)pnd.chip_data).current_target.nm, pbtInitiatorData, szInitiatorData,/*out*/ pnt, 300)) == 1)
                {
                    ret = NFC.NFC_SUCCESS;
                }
                else if ((ret == 0) || (ret == NFC.NFC_ETIMEOUT))
                {
                    ret = NFC.NFC_ETGRELEASED;
                }
                if (bInfiniteSelect)
                {
                    int ret2;
                    if ((ret2 = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_INFINITE_SELECT, true)) < 0)
                        return ret2;
                }
            }
            return ret;
        }

        protected int pn53x_DEP_is_present(NFC.nfc_device pnd)
        {
            int ret;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping DEP");
            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN531) || (((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) || (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533))
                ret = pn53x_Diagnose06(pnd);
            else
                ret = NFC.NFC_EDEVNOTSUPP;
            return ret;
        }

        protected int pn53x_Felica_is_present(NFC.nfc_device pnd)
        {
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping Felica");
            // if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533) { ret = pn53x_Diagnose06(pnd); } else...
            // Because ping fails now & then, better not to use Diagnose at all
            // Limitation: does not work on Felica Lite cards (neither Diagnose nor our method)
            //byte[] abtCmd=new byte[10];//{0x0A, 0x04};
            BufferBuilder abtCmd = new BufferBuilder(10);
            abtCmd.append(0x0A);
            abtCmd.append(0x04);
            abtCmd.append(((pn53x_data)pnd.chip_data).current_target.nti.nfi.abtId, 8);
            int failures = 0;
            // Sometimes ping fails so we want to give the card some more chances...
            while (failures < 3)
            {
                if (NFC.nfc_initiator_transceive_bytes(pnd, abtCmd.buffer(), abtCmd.Length(), null, 0, 300) == 11)
                {
                    return NFC.NFC_SUCCESS;
                }
                else
                {
                    failures++;
                }
            }
            return NFC.NFC_ETGRELEASED;
        }

        protected int pn53x_ISO14443B_4_is_present(NFC.nfc_device pnd)
        {
            int ret;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping -4B");
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN533)
            { // Not supported on PN532 even if the doc is same as for pn53x_type.PN533
                ret = pn53x_Diagnose06(pnd);
            }
            else
            {
                // Sending R(NACK) in raw:
                if ((ret = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false)) < 0)
                    return ret;
                // byte[]  abtCmd=new byte[1] = {0xb2}; // if on pn53x_type.PN533, CID=0
                byte[] abtCmd = { 0xba, 0x01 }; // if on PN532, CID=1
                int failures = 0;
                while (failures < 2)
                {
                    if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length, null, 0, 300)) < 1)
                    {
                        if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                        { // Timeout
                            ret = NFC.NFC_ETGRELEASED;
                            break;
                        }
                        else
                        { // Other errors can appear when card is tired-off, let's try again
                            failures++;
                        }
                    }
                    else
                    {
                        ret = NFC.NFC_SUCCESS;
                        break;
                    }
                }
                int ret2;
                if ((ret2 = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true)) < 0)
                    ret = ret2;
            }
            return ret;
        }

        protected int pn53x_ISO14443B_I_is_present(NFC.nfc_device pnd)
        {
            int ret;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping B'");
            // Sending ATTRIB in raw:
            if ((ret = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false)) < 0)
                return ret;
            //byte[]  abtCmd=new byte[6]  {0x01, 0x0f};
            BufferBuilder abtCmd = new BufferBuilder(6);
            abtCmd.append(0x01);
            abtCmd.append(0x0f);
            abtCmd.append(((pn53x_data)pnd.chip_data).current_target.nti.nii.abtDIV, 4);
            int failures = 0;
            while (failures < 2)
            {
                if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd.buffer(), abtCmd.Length(), null, 0, 300)) < 1)
                {
                    if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                    { // Timeout
                        ret = NFC.NFC_ETGRELEASED;
                        break;
                    }
                    else
                    { // Other errors can appear when card is tired-off, let's try again
                        failures++;
                    }
                }
                else
                {
                    ret = NFC.NFC_SUCCESS;
                    break;
                }
            }
            int ret2;
            if ((ret2 = pn53x_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true)) < 0)
                ret = ret2;
            return ret;
        }

        protected int pn53x_ISO14443B_SR_is_present(NFC.nfc_device pnd)
        {
            int ret = 0;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping B2 ST SRx");
            // Sending Get_UID in raw: (EASY_FRAMING is already supposed to be false)
            byte[] abtCmd = { 0x0b };
            int failures = 0;
            while (failures < 2)
            {
                if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length, null, 0, 300)) < 1)
                {
                    if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                    { // Timeout
                        ret = NFC.NFC_ETGRELEASED;
                        break;
                    }
                    else
                    { // Other errors can appear when card is tired-off, let's try again
                        failures++;
                    }
                }
                else
                {
                    ret = NFC.NFC_SUCCESS;
                    break;
                }
            }
            return ret;
        }

        protected int pn53x_ISO14443B_CT_is_present(NFC.nfc_device pnd)
        {
            int ret=0;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): Ping B2 ASK CTx");
            // Sending SELECT in raw: (EASY_FRAMING is already supposed to be false)
            byte[] abtCmd = new byte[3];
            abtCmd[0]= 0x9f;
            MiscTool.memcpy(abtCmd , 1, ((pn53x_data)pnd.chip_data).current_target.nti.nci.abtUID,0, 2);
            int failures = 0;
            while (failures < 2)
            {
                if ((ret = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, abtCmd.Length, null, 0, 300)) < 1)
                {
                    if ((ret == NFC.NFC_ERFTRANS) && (((pn53x_data)pnd.chip_data).last_status_byte == 0x01))
                    { // Timeout
                        ret = NFC.NFC_ETGRELEASED;
                        break;
                    }
                    else
                    { // Other errors can appear when card is tired-off, let's try again
                        failures++;
                    }
                }
                else
                {
                    ret = NFC.NFC_SUCCESS;
                    break;
                }
            }
            return ret;
        }

        protected int
        pn53x_initiator_target_is_present(NFC.nfc_device pnd, NFC.nfc_target pnt)
        {
            // Check if there is a saved target
            if (((pn53x_data)pnd.chip_data).current_target == null)
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): no saved target");
                return pnd.last_error = NFC.NFC_EINVARG;
            }

            // Check if the argument target nt is equals to current saved target
            if ((pnt != null) && (!pn53x_current_target_is(pnd, pnt)))
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): another target");
                return pnd.last_error = NFC.NFC_ETGRELEASED;
            }

            // Ping target
            int ret;
            switch (((pn53x_data)pnd.chip_data).current_target.nm.nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    if (0!=(((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak & 0x20))
                    {
                        ret = pn53x_ISO14443A_4_is_present(pnd);
                    }
                    else if ((((pn53x_data)pnd.chip_data).current_target.nti.nai.abtAtqa[0] == 0x00) &&
                             (((pn53x_data)pnd.chip_data).current_target.nti.nai.abtAtqa[1] == 0x44) &&
                             (((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak == 0x00))
                    {
                        ret = pn53x_ISO14443A_MFUL_is_present(pnd);
                    }
                    else if (0!=(((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak & 0x08))
                    {
                        ret = pn53x_ISO14443A_MFC_is_present(pnd);
                    }
                    else
                    {
                        //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): card type A not supported");
                        ret = NFC.NFC_EDEVNOTSUPP;
                    }
                    break;
                case NFC.nfc_modulation_type.NMT_DEP:
                    ret = pn53x_DEP_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_FELICA:
                    ret = pn53x_Felica_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_JEWEL:
                    ret = pn53x_ISO14443A_Jewel_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B:
                    ret = pn53x_ISO14443B_4_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                    ret = pn53x_ISO14443B_I_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                    ret = pn53x_ISO14443B_SR_is_present(pnd);
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                    ret = pn53x_ISO14443B_CT_is_present(pnd);
                    break;
                default:
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "target_is_present(): card type not supported");
                    ret = NFC.NFC_EDEVNOTSUPP;
                    break;
            }
            if (ret == NFC.NFC_ETGRELEASED)
                pn53x_current_target_free(pnd);
            return pnd.last_error = ret;
        }

        static byte SAK_ISO14443_4_COMPLIANT = 0x20;
        static byte SAK_ISO18092_COMPLIANT = 0x40;
        protected int
        pn53x_target_init(NFC.nfc_device pnd, NFC.nfc_target pnt, byte[] pbtRx, int szRxLen, int timeout)
        {
            pn53x_reset_settings(pnd);

            ((pn53x_data)pnd.chip_data).operating_mode = pn53x_operating_mode.TARGET;

            pn53x_target_mode ptm = pn53x_target_mode.PTM_NORMAL;
            int res = 0;

            switch (pnt.nm.nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    ptm = pn53x_target_mode.PTM_PASSIVE_ONLY;
                    if ((pnt.nti.nai.abtUid[0] != 0x08) || (pnt.nti.nai.szUidLen != 4))
                    {
                        pnd.last_error = NFC.NFC_EINVARG;
                        return pnd.last_error;
                    }
                    pn53x_set_parameters(pnd, PARAM_AUTO_ATR_RES, false);
                    if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN532)
                    { // We have a PN532
                        if ((pnt.nti.nai.btSak & SAK_ISO14443_4_COMPLIANT) != 0 && (pnd.bAutoIso14443_4))
                        {
                            // We have a ISO14443-4 tag to emulate and NFC.nfc_property.NP_AUTO_14443_4A option is enabled
                            ptm |= pn53x_target_mode.PTM_ISO14443_4_PICC_ONLY; // We add ISO14443-4 restriction
                            pn53x_set_parameters(pnd, PARAM_14443_4_PICC, true);
                        }
                        else
                        {
                            pn53x_set_parameters(pnd, PARAM_14443_4_PICC, false);
                        }
                    }
                    break;
                case NFC.nfc_modulation_type.NMT_FELICA:
                    ptm = pn53x_target_mode.PTM_PASSIVE_ONLY;
                    break;
                case NFC.nfc_modulation_type.NMT_DEP:
                    pn53x_set_parameters(pnd, PARAM_AUTO_ATR_RES, true);
                    ptm = pn53x_target_mode.PTM_DEP_ONLY;
                    if (pnt.nti.ndi.ndm == NFC.nfc_dep_mode.NDM_PASSIVE)
                    {
                        ptm |= pn53x_target_mode.PTM_PASSIVE_ONLY; // We add passive mode restriction
                    }
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B:
                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                case NFC.nfc_modulation_type.NMT_JEWEL:
                    pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                    return pnd.last_error;
                    break;
            }

            // Let the PN53X be activated by the RF level detector from power down mode
            if ((res = pn53x_write_register(pnd, PN53X_REG_CIU_TxAuto, SYMBOL_INITIAL_RF_ON, 0x04)) < 0)
                return res;

            byte[] abtMifareParams = new byte[6];
            byte[] pbtMifareParams = null;
            byte[] pbtTkt = null;
            int szTkt = 0;

            byte[] abtFeliCaParams = new byte[18];
            byte[] pbtFeliCaParams = null;

            byte[] pbtNFCID3t = null;
            byte[] pbtGBt = null;
            int szGBt = 0;

            switch (pnt.nm.nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    {
                        // Set ATQA (SENS_RES)
                        abtMifareParams[0] = pnt.nti.nai.abtAtqa[1];
                        abtMifareParams[1] = pnt.nti.nai.abtAtqa[0];
                        // Set UID
                        // Note: in this mode we can only emulate a single size (4 bytes) UID where the first is hard-wired by PN53x as 0x08
                        abtMifareParams[2] = pnt.nti.nai.abtUid[1];
                        abtMifareParams[3] = pnt.nti.nai.abtUid[2];
                        abtMifareParams[4] = pnt.nti.nai.abtUid[3];
                        // Set SAK (SEL_RES)
                        abtMifareParams[5] = pnt.nti.nai.btSak;

                        pbtMifareParams = abtMifareParams;

                        // Historical Bytes
                        pbtTkt = ISO14443Subr.iso14443a_locate_historical_bytes(pnt.nti.nai.abtAts, pnt.nti.nai.szAtsLen,out szTkt);
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_FELICA:
                    // Set NFCID2t
                    MiscTool.memcpy(abtFeliCaParams,0, pnt.nti.nfi.abtId,0, 8);
                    // Set PAD
                    MiscTool.memcpy(abtFeliCaParams , 8, pnt.nti.nfi.abtPad,0, 8);
                    // Set SystemCode
                    MiscTool.memcpy(abtFeliCaParams , 16, pnt.nti.nfi.abtSysCode,0, 2);
                    pbtFeliCaParams = abtFeliCaParams;
                    break;

                case NFC.nfc_modulation_type.NMT_DEP:
                    // Set NFCID3
                    pbtNFCID3t = pnt.nti.ndi.abtNFCID3;
                    // Set General Bytes, if relevant
                    szGBt = pnt.nti.ndi.szGB;
                    if (szGBt != 0) pbtGBt = pnt.nti.ndi.abtGB;

                    // Set ISO/IEC 14443 part
                    // Set ATQA (SENS_RES)
                    abtMifareParams[0] = 0x08;
                    abtMifareParams[1] = 0x00;
                    // Set UID
                    // Note: in this mode we can only emulate a single size (4 bytes) UID where the first is hard-wired by PN53x as 0x08
                    abtMifareParams[2] = 0x12;
                    abtMifareParams[3] = 0x34;
                    abtMifareParams[4] = 0x56;
                    // Set SAK (SEL_RES)
                    abtMifareParams[5] = SAK_ISO18092_COMPLIANT; // Allow ISO/IEC 18092 in DEP mode

                    pbtMifareParams = abtMifareParams;

                    // Set FeliCa part
                    // Set NFCID2t
                    abtFeliCaParams[0] = 0x01;
                    abtFeliCaParams[1] = 0xfe;
                    abtFeliCaParams[2] = 0x12;
                    abtFeliCaParams[3] = 0x34;
                    abtFeliCaParams[4] = 0x56;
                    abtFeliCaParams[5] = 0x78;
                    abtFeliCaParams[6] = 0x90;
                    abtFeliCaParams[7] = 0x12;
                    // Set PAD
                    abtFeliCaParams[8] = 0xc0;
                    abtFeliCaParams[9] = 0xc1;
                    abtFeliCaParams[10] = 0xc2;
                    abtFeliCaParams[11] = 0xc3;
                    abtFeliCaParams[12] = 0xc4;
                    abtFeliCaParams[13] = 0xc5;
                    abtFeliCaParams[14] = 0xc6;
                    abtFeliCaParams[15] = 0xc7;
                    // Set System Code
                    abtFeliCaParams[16] = 0x0f;
                    abtFeliCaParams[17] = 0xab;

                    pbtFeliCaParams = abtFeliCaParams;
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B:
                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                case NFC.nfc_modulation_type.NMT_JEWEL:
                    pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                    return pnd.last_error;
                    break;
            }

            bool targetActivated = false;
            int szRx =0;
            while (!targetActivated)
            {
                byte btActivatedMode;

                if ((res = pn53x_TgInitAsTarget(pnd, ptm, pbtMifareParams, pbtTkt, szTkt, pbtFeliCaParams, pbtNFCID3t, pbtGBt, szGBt, pbtRx, szRxLen, out btActivatedMode, timeout)) < 0)
                {
                    if (res == NFC.NFC_ETIMEOUT)
                    {
                        pn53x_idle(pnd);
                    }
                    return res;
                }
                szRx = (int)res;
                NFC.nfc_modulation nm;
                nm.nmt = NFC.nfc_modulation_type.NMT_DEP; // Silent compilation warnings
                nm.nbr = NFC.nfc_baud_rate.NBR_UNDEFINED;

                NFC.nfc_dep_mode ndm = NFC.nfc_dep_mode.NDM_UNDEFINED;
                // Decode activated "mode"
                switch (btActivatedMode & 0x70)
                { // Baud rate
                    case 0x00: // 106kbps
                        nm.nbr = NFC.nfc_baud_rate.NBR_106;
                        break;
                    case 0x10: // 212kbps
                        nm.nbr = NFC.nfc_baud_rate.NBR_212;
                        break;
                    case 0x20: // 424kbps
                        nm.nbr = NFC.nfc_baud_rate.NBR_424;
                        break;
                };

                if ((btActivatedMode & 0x04) != 0)
                { // D.E.P.
                    nm.nmt = NFC.nfc_modulation_type.NMT_DEP;
                    if ((btActivatedMode & 0x03) == 0x01)
                    { // Active mode
                        ndm = NFC.nfc_dep_mode.NDM_ACTIVE;
                    }
                    else
                    { // Passive mode
                        ndm = NFC.nfc_dep_mode.NDM_PASSIVE;
                    }
                }
                else
                { // Not D.E.P.
                    if ((btActivatedMode & 0x03) == 0x00)
                    { // MIFARE
                        nm.nmt = NFC.nfc_modulation_type.NMT_ISO14443A;
                    }
                    else if ((btActivatedMode & 0x03) == 0x02)
                    { // FeliCa
                        nm.nmt = NFC.nfc_modulation_type.NMT_FELICA;
                    }
                }

                if (pnt.nm.nmt == nm.nmt)
                { // Actual activation have the right modulation type
                    if ((pnt.nm.nbr == NFC.nfc_baud_rate.NBR_UNDEFINED) || (pnt.nm.nbr == nm.nbr))
                    { // Have the right baud rate (or undefined)
                        if ((pnt.nm.nmt != NFC.nfc_modulation_type.NMT_DEP) || (pnt.nti.ndi.ndm == NFC.nfc_dep_mode.NDM_UNDEFINED) || (pnt.nti.ndi.ndm == ndm))
                        { // Have the right DEP mode (or is not a DEP)
                            targetActivated = true;
                        }
                    }
                }
                if (targetActivated)
                {
                    pnt.nm.nbr = nm.nbr; // Update baud rate
                    if (pnt.nm.nmt == NFC.nfc_modulation_type.NMT_DEP)
                    {
                        pnt.nti.ndi.ndm = ndm; // Update DEP mode
                    }
                    if (pn53x_current_target_new(ref pnd, pnt) == null)
                    {
                        pnd.last_error = NFC.NFC_ESOFT;
                        return pnd.last_error;
                    }

                    if (0!=(ptm & pn53x_target_mode.PTM_ISO14443_4_PICC_ONLY))
                    {
                        // When PN532 is in PICC target mode, it automatically reply to RATS so
                        // we don't need to forward this command
                        szRx = 0;
                    }
                }
            }

            return szRx;
        }

        protected int
        pn53x_target_receive_bits(NFC.nfc_device pnd,ref byte[] pbtRx, int szRxLen, byte[] pbtRxPar)
        {
            int szRxBits = 0;
            byte[] abtCmd = { TgGetInitiatorCommand };

            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            int res = 0;

            // Try to gather a received frame from the reader
            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtRx, szRx, -1)) < 0)
                return res;
            szRx = (int)res;
            // Get the last bit-count that is stored in the received byte
            byte ui8rcc;
            if ((res = pn53x_read_register(pnd, PN53X_REG_CIU_Control, out ui8rcc)) < 0)
                return res;
            byte ui8Bits = (byte)(ui8rcc & SYMBOL_RX_LAST_BITS);

            // Recover the real frame length in bits
            int szFrameBits = ((szRx - 1 - ((ui8Bits == 0) ? 0 : 1)) * 8) + ui8Bits;

            // Ignore the status byte from the PN53X here, it was checked earlier in pn53x_transceive()
            // Check if we should recover the parity bits ourself
            if (!pnd.bPar)
            {
                // Unwrap the response frame
                if ((res = pn53x_unwrap_frame(MiscTool.SubBytes( abtRx , 1), szFrameBits, pbtRx, pbtRxPar)) < 0)
                    return res;
                szRxBits = res;
            }
            else
            {
                // Save the received bits
                szRxBits = szFrameBits;

                if ((szRx - 1) > szRxLen)
                    return NFC.NFC_EOVFLOW;
                // Copy the received bytes
                MiscTool.memcpy(pbtRx,0, abtRx , 1, szRx - 1);
            }
            // Everyting seems ok, return received bits count
            return szRxBits;
        }

        protected int
        pn53x_target_receive_bytes(NFC.nfc_device pnd, byte[] pbtRx, int szRxLen, int timeout)
        {
            byte[] abtCmd = new byte[1];

            // XXX I think this is not a clean way to provide some kind of "EasyFraming"
            // but at the moment I have no more better than this
            if (pnd.bEasyFraming)
            {
                switch (((pn53x_data)pnd.chip_data).current_target.nm.nmt)
                {
                    case NFC.nfc_modulation_type.NMT_DEP:
                        abtCmd[0] = TgGetData;
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443A:
                        if (0!=(((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak & SAK_ISO14443_4_COMPLIANT))
                        {
                            // We are dealing with a ISO/IEC 14443-4 compliant target
                            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (pnd.bAutoIso14443_4))
                            {
                                // We are using ISO/IEC 14443-4 PICC emulation capability from the PN532
                                abtCmd[0] = TgGetData;
                                break;
                            }
                            else
                            {
                                // TODO Support EasyFraming for other cases by software
                                pnd.last_error = NFC.NFC_ENOTIMPL;
                                return pnd.last_error;
                            }
                        }
                        else
                        {
                            abtCmd[0] = TgGetInitiatorCommand;
                        }
                        break;
                    case NFC.nfc_modulation_type.NMT_JEWEL:
                    case NFC.nfc_modulation_type.NMT_ISO14443B:
                    case NFC.nfc_modulation_type.NMT_ISO14443BI:
                    case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                    case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                    case NFC.nfc_modulation_type.NMT_FELICA:
                        abtCmd[0] = TgGetInitiatorCommand;
                        break;
                }
            }
            else
            {
                abtCmd[0] = TgGetInitiatorCommand;
            }

            // Try to gather a received frame from the reader
            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            int res = 0;
            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, abtRx, szRx, timeout)) < 0)
                return pnd.last_error;
            szRx = (int)res;
            // Save the received bytes count
            szRx -= 1;

            if (szRx > szRxLen)
                return NFC.NFC_EOVFLOW;

            // Copy the received bytes
            MiscTool.memcpy(pbtRx,0, abtRx , 1, szRx);

            // Everyting seems ok, return received bytes count
            return szRx;
        }

        protected int
        pn53x_target_send_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar)
        {
            int szFrameBits = 0;
            int szFrameBytes = 0;
            byte ui8Bits = 0;
            //byte[] abtCmd = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN] { TgResponseToInitiator };
            BufferBuilder abtCmd = new BufferBuilder(PN53x_EXTENDED_FRAME__DATA_MAX_LEN);
            abtCmd.append(TgResponseToInitiator);
            int res = 0;

            // Check if we should prepare the parity bits ourself
            if (!pnd.bPar)
            {
                // Convert data with parity to a frame
                if ((res = pn53x_wrap_frame(pbtTx, szTxBits, pbtTxPar,ref abtCmd )) < 0)
                    return res;
                szFrameBits = res;
            }
            else
            {
                szFrameBits = szTxBits;
            }

            // Retrieve the leading bits
            ui8Bits = (byte)(szFrameBits % 8);

            // Get the amount of frame bytes + optional (1 byte if there are leading bits)
            szFrameBytes = (szFrameBits / 8) + ((ui8Bits == 0) ? 0 : 1);

            // When the parity is handled before us, we just copy the data
            if (pnd.bPar)
                abtCmd.append(pbtTx, szFrameBytes);//MiscTool.memcpy(abtCmd + 1, pbtTx, szFrameBytes);

            // Set the amount of transmission bits in the PN53X chip register
            if ((res = pn53x_set_tx_bits(pnd, ui8Bits)) < 0)
                return res;

            // Try to send the bits to the reader
            if ((res = pn53x_transceive(pnd, abtCmd.bytes(), szFrameBytes + 1, null, 0, -1)) < 0)
                return res;

            // Everyting seems ok, return return sent bits count
            return szTxBits;
        }

        protected int
        pn53x_target_send_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, int timeout)
        {
            byte[] abtCmd = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int res = 0;

            // We can not just send bytes without parity if while the PN53X expects we handled them
            if (!pnd.bPar)
                return NFC.NFC_ECHIP;

            // XXX I think this is not a clean way to provide some kind of "EasyFraming"
            // but at the moment I have no more better than this
            if (pnd.bEasyFraming)
            {
                switch (((pn53x_data)pnd.chip_data).current_target.nm.nmt)
                {
                    case NFC.nfc_modulation_type.NMT_DEP:
                        abtCmd[0] = TgSetData;
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443A:
                        if (0 != (((pn53x_data)pnd.chip_data).current_target.nti.nai.btSak & SAK_ISO14443_4_COMPLIANT))
                        {
                            // We are dealing with a ISO/IEC 14443-4 compliant target
                            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN532) && (pnd.bAutoIso14443_4))
                            {
                                // We are using ISO/IEC 14443-4 PICC emulation capability from the PN532
                                abtCmd[0] = TgSetData;
                                break;
                            }
                            else
                            {
                                // TODO Support EasyFraming for other cases by software
                                pnd.last_error = NFC.NFC_ENOTIMPL;
                                return pnd.last_error;
                            }
                        }
                        else
                        {
                            abtCmd[0] = TgResponseToInitiator;
                        }
                        break;
                    case NFC.nfc_modulation_type.NMT_JEWEL:
                    case NFC.nfc_modulation_type.NMT_ISO14443B:
                    case NFC.nfc_modulation_type.NMT_ISO14443BI:
                    case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                    case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                    case NFC.nfc_modulation_type.NMT_FELICA:
                        abtCmd[0] = TgResponseToInitiator;
                        break;
                }
            }
            else
            {
                abtCmd[0] = TgResponseToInitiator;
            }

            // Copy the data into the command frame
            MiscTool.memcpy(abtCmd , 1, pbtTx,0, szTx);

            // Try to send the bits to the reader
            if ((res = pn53x_transceive(pnd, abtCmd, szTx + 1, null, 0, timeout)) < 0)
                return res;

            // Everyting seems ok, return sent byte count
            return szTx;
        }

        struct sErrorMessage
        {
            int errorCode;
            string errorMsg;
        } ;
        protected string sErrorMessages(int error)
        {
            switch (error)
            {
                /* Chip-level errors (internal errors, RF errors, etc.) */
                case 0x00: return "Success";
                case ETIMEOUT: return "Timeout";      // Time Out: return  the target has not answered
                case ECRC: return "CRC Error";      // A CRC error has been detected by the CIU
                case EPARITY: return "Parity Error";     // A Parity error has been detected by the CIU
                case EBITCOUNT: return "Erroneous Bit Count";   // During an anti-collision/select operation (ISO/IEC14443-3 Type A and ISO/IEC18092 106 kbps passive mode): return  an erroneous Bit Count has been detected
                case EFRAMING: return "Framing Error";    // Framing error during MIFARE operation
                case EBITCOLL: return "Bit-collision";    // An abnormal bit-collision has been detected during bit wise anti-collision at 106 kbps
                case ESMALLBUF: return "Communication Buffer Too Small";  // Communication buffer size insufficient
                case EBUFOVF: return "Buffer Overflow";   // RF Buffer overflow has been detected by the CIU (bit BufferOvfl of the register CIU_Error)
                case ERFPROTO: return "RF Protocol Error";    // RF Protocol error (see PN53x manual)
                case EOVHEAT: return "Chip Overheating";    // Temperature error: the internal temperature sensor has detected overheating: return  and therefore has automatically switched off the antenna drivers
                case EINBUFOVF: return "Internal Buffer overflow.";  // Internal buffer overflow
                case EINVPARAM: return "Invalid Parameter";    // Invalid parameter (range: return  format: return  …)
                case EOPNOTALL: return "Operation Not Allowed"; // Operation not allowed in this configuration (host controller interface)
                case ECMD: return "Command Not Acceptable";   // Command is not acceptable due to the current context
                case EOVCURRENT: return "Over Current";
                /* DEP errors */
                case ERFTIMEOUT: return "RF Timeout";     // In active communication mode: return  the RF field has not been switched on in time by the counterpart (as defined in NFCIP-1 standard)
                case EDEPUNKCMD: return "Unknown DEP Command";
                case EDEPINVSTATE: return "Invalid DEP State";  // DEP Protocol: Invalid device state: return  the system is in a state which does not allow the operation
                case ENAD: return "NAD Missing in DEP Frame";
                /* MIFARE */
                case EMFAUTH: return "Mifare Authentication Error";
                /* Misc */
                case EINVRXFRAM: return "Invalid Received Frame"; // DEP Protocol: return  Mifare or ISO/IEC14443-4: The data format does not match to the specification.
                case ENSECNOTSUPP: return "NFC Secure not supported"; // Target or Initiator does not support NFC Secure
                case EBCC: return "Wrong UID Check Byte (BCC)"; // ISO/IEC14443-3: UID Check byte is wrong
                case ETGREL: return "Target Released";    // Target have been released by initiator
                case ECID: return "Card ID Mismatch";     // ISO14443 type B: Card ID mismatch: return  meaning that the expected card has been exchanged with another one.
                case ECDISCARDED: return "Card Discarded";    // ISO/IEC14443 type B: the card previously activated has disappeared.
                case ENFCID3: return "NFCID3 Mismatch";
                default: return "Unknown error";
            }
        }

        protected string pn53x_strerror(NFC.nfc_device pnd)
        {
            return sErrorMessages(((pn53x_data)pnd.chip_data).last_status_byte);
        }

        protected int
        pn53x_RFConfiguration__RF_field(NFC.nfc_device pnd, bool bEnable)
        {
            byte[] abtCmd = { RFConfiguration, RFCI_FIELD, (bEnable) ? (byte)0x01 : (byte)0x00 };
            return pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
        }

        protected int
        pn53x_RFConfiguration__Various_timings(NFC.nfc_device pnd, byte fATR_RES_Timeout, byte fRetryTimeout)
        {
            byte[] abtCmd = {
                                RFConfiguration,
                                RFCI_TIMING,
                                0x00,		 // RFU
                                fATR_RES_Timeout,	 // ATR_RES timeout (default: 0x0B 102.4 ms)
                                fRetryTimeout	 // TimeOut during non-DEP communications (default: 0x0A 51.2 ms)
                            };
            return pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
        }

        protected int
        pn53x_RFConfiguration__MaxRtyCOM(NFC.nfc_device pnd, byte MaxRtyCOM)
        {
            byte[] abtCmd = {
                                RFConfiguration,
                                RFCI_RETRY_DATA,
                                MaxRtyCOM         // MaxRtyCOM, default: 0x00 (no retry, only one try), inifite: 0xff
                              };
            return pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
        }

        protected int
        pn53x_RFConfiguration__MaxRetries(NFC.nfc_device pnd, byte MxRtyATR, byte MxRtyPSL, byte MxRtyPassiveActivation)
        {
            // Retry format: 0x00 means only 1 try, 0xff means infinite
            byte[] abtCmd = {
                    RFConfiguration,
                    RFCI_RETRY_SELECT,
                    MxRtyATR,        // MxRtyATR, default: active = 0xff, passive = 0x02
                    MxRtyPSL,        // MxRtyPSL, default: 0x01
                    MxRtyPassiveActivation         // MxRtyPassiveActivation, default: 0xff (0x00 leads to problems with PN531)
                  };
            return pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
        }

        protected int
        pn53x_SetParameters(NFC.nfc_device pnd, byte ui8Value)
        {
            byte[] abtCmd = { SetParameters, ui8Value };
            int res = 0;

            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1)) < 0)
            {
                return res;
            }
            // We save last parameters in register cache
            ((pn53x_data)pnd.chip_data).ui8Parameters = ui8Value;
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn532_SAMConfiguration(NFC.nfc_device pnd, pn532_sam_mode sam_mode, int timeout)
        {
            byte[] abtCmd = { SAMConfiguration, (byte)sam_mode, 0x00, 0x00 };
            int szCmd = abtCmd.Length;

            if (((pn53x_data)pnd.chip_data).type != pn53x_type.PN532)
            {
                // This function is not supported by pn531 neither pn533
                pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                return pnd.last_error;
            }

            switch (sam_mode)
            {
                case pn532_sam_mode.PSM_NORMAL: // Normal mode
                case pn532_sam_mode.PSM_WIRED_CARD: // Wired card mode
                    szCmd = 2;
                    break;
                case pn532_sam_mode.PSM_VIRTUAL_CARD: // Virtual card mode
                case pn532_sam_mode.PSM_DUAL_CARD: // Dual card mode
                    // TODO Implement timeout handling
                    szCmd = 3;
                    break;
                default:
                    pnd.last_error = NFC.NFC_EINVARG;
                    return pnd.last_error;
            }
            ((pn53x_data)pnd.chip_data).sam_mode = sam_mode;
            return (pn53x_transceive(pnd, abtCmd, szCmd, null, 0, timeout));
        }

        protected int
        pn53x_PowerDown(NFC.nfc_device pnd)
        {
            byte[] abtCmd = { PowerDown, 0xf0 };
            int res;
            if ((res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1)) < 0)
                return res;
            ((pn53x_data)pnd.chip_data).power_mode = pn53x_power_mode.LOWVBAT;
            return res;
        }

        /**
         * @brief C wrapper to InListPassiveTarget command
         * @return Returns selected targets count on success, otherwise returns libnfc's error code (negative value)
         *
         * @param pnd struct NFC.nfc_device struct pointer that represent currently used device
         * @param pmInitModulation Desired modulation
         * @param pbtInitiatorData Optional initiator data used for Felica, ISO14443B, Topaz Polling or for ISO14443A selecting a specific UID
         * @param szInitiatorData Length of initiator data \a pbtInitiatorData
         * @param pbtTargetsData pointer on a pre-allocated byte array to receive TargetData[n] as described in pn53x user manual
         * @param pszTargetsData int pointer where size of \a pbtTargetsData will be written
         *
         * @note Selected targets count can be found in \a pbtTargetsData[0] if available (i.e. \a pszTargetsData content is more than 0)
         * @note To decode theses TargetData[n], there is @fn pn53x_decode_target_data
         */
        protected int
        pn53x_InListPassiveTarget(NFC.nfc_device pnd,
                                   pn53x_modulation pmInitModulation, byte szMaxTargets,
                                   byte[] pbtInitiatorData, int szInitiatorData,
                                  byte[] pbtTargetsData,ref int pszTargetsData,
                                  int timeout)
        {
            byte[] abtCmd = new byte[15];// {  };
            abtCmd[0] = InListPassiveTarget;
            abtCmd[1] = szMaxTargets;     // MaxTg

            switch (pmInitModulation)
            {
                case pn53x_modulation.PM_ISO14443A_106:
                case pn53x_modulation.PM_FELICA_212:
                case pn53x_modulation.PM_FELICA_424:
                    // all gone fine.
                    break;
                case pn53x_modulation.PM_ISO14443B_106:
                    if (0 != (pnd.btSupportByte & SUPPORT_ISO14443B))
                    {
                        // Eg. Some PN532 doesn't support type B!
                        pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                        return pnd.last_error;
                    }
                    break;
                case pn53x_modulation.PM_JEWEL_106:
                    if (((pn53x_data)pnd.chip_data).type == pn53x_type.PN531)
                    {
                        // These modulations are not supported by pn531
                        pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                        return pnd.last_error;
                    }
                    break;
                case pn53x_modulation.PM_ISO14443B_212:
                case pn53x_modulation.PM_ISO14443B_424:
                case pn53x_modulation.PM_ISO14443B_847:
                    if ((((pn53x_data)pnd.chip_data).type != pn53x_type.PN533) || (0==(pnd.btSupportByte & SUPPORT_ISO14443B)))
                    {
                        // These modulations are not supported by pn531 neither pn532
                        pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                        return pnd.last_error;
                    }
                    break;
                case pn53x_modulation.PM_UNDEFINED:
                    pnd.last_error = NFC.NFC_EINVARG;
                    return pnd.last_error;
            }
            abtCmd[2] =(byte) pmInitModulation; // BrTy, the type of init modulation used for polling a passive tag

            // Set the optional initiator data (used for Felica, ISO14443B, Topaz Polling or for ISO14443A selecting a specific UID).
            if (pbtInitiatorData!=null)
                MiscTool.memcpy(abtCmd , 3, pbtInitiatorData,0, szInitiatorData);
            int res = 0;
            if ((res = pn53x_transceive(pnd, abtCmd, 3 + szInitiatorData, pbtTargetsData, pszTargetsData, timeout)) < 0)
            {
                return res;
            }
            pszTargetsData = res;
            return pbtTargetsData[0];
        }

        protected int
        pn53x_InDeselect(NFC.nfc_device pnd, byte ui8Target)
        {
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.RCS360)
            {
                // We should do act here *only* if a target was previously selected
                byte[] abtStatus = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                int szStatus = abtStatus.Length;
                byte[] abtCmdGetStatus = { GetGeneralStatus };
                int res = 0;
                if ((res = pn53x_transceive(pnd, abtCmdGetStatus, abtCmdGetStatus.Length, abtStatus, szStatus, -1)) < 0)
                {
                    return res;
                }
                szStatus = (int)res;
                if ((szStatus < 3) || (abtStatus[2] == 0))
                {
                    return NFC.NFC_SUCCESS;
                }
                // No much choice what to deselect actually...
                byte[] abtCmdRcs360 = { InDeselect, 0x01, 0x01 };
                return (pn53x_transceive(pnd, abtCmdRcs360, abtCmdRcs360.Length, null, 0, -1));
            }
            byte[] abtCmd = { InDeselect, ui8Target };
            return (pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1));
        }

        protected int
        pn53x_InRelease(NFC.nfc_device pnd, byte ui8Target)
        {
            int res = 0;
            if (((pn53x_data)pnd.chip_data).type == pn53x_type.RCS360)
            {
                // We should do act here *only* if a target was previously selected
                byte[] abtStatus = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
                int szStatus = abtStatus.Length;
                byte[] abtCmdGetStatus = { GetGeneralStatus };
                if ((res = pn53x_transceive(pnd, abtCmdGetStatus, abtCmdGetStatus.Length, abtStatus, szStatus, -1)) < 0)
                {
                    return res;
                }
                szStatus = (int)res;
                if ((szStatus < 3) || (abtStatus[2] == 0))
                {
                    return NFC.NFC_SUCCESS;
                }
                // No much choice what to release actually...
                byte[] abtCmdRcs360 = { InRelease, 0x01, 0x01 };
                res = pn53x_transceive(pnd, abtCmdRcs360, abtCmdRcs360.Length, null, 0, -1);
                return (res >= 0) ? NFC.NFC_SUCCESS : res;
            }
            byte[] abtCmd = { InRelease, ui8Target };
            res = pn53x_transceive(pnd, abtCmd, abtCmd.Length, null, 0, -1);
            return (res >= 0) ? NFC.NFC_SUCCESS : res;
        }

        protected int
        pn53x_InAutoPoll(NFC.nfc_device pnd,
                          pn53x_target_type[] ppttTargetTypes, int szTargetTypes,
                          byte btPollNr, byte btPeriod,ref NFC.nfc_target[] pntTargets, int timeout)
        {
            int szTargetFound = 0;
            if (((pn53x_data)pnd.chip_data).type != pn53x_type.PN532)
            {
                // This function is not supported by pn531 neither pn533
                pnd.last_error = NFC.NFC_EDEVNOTSUPP;
                return pnd.last_error;
            }

            // InAutoPoll frame looks like this { 0xd4, 0x60, 0x0f, 0x01, 0x00 } => { direction, command, pollnr, period, types... }
            int szTxInAutoPoll = 3 + szTargetTypes;
            byte[] abtCmd = new byte[3 + 15];
            abtCmd[0] = InAutoPoll;
            abtCmd[1] = btPollNr; 
            abtCmd[2] = btPeriod;
            for (int n = 0; n < szTargetTypes; n++)
            {
                abtCmd[3 + n] =(byte) ppttTargetTypes[n];
            }

            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            int res = pn53x_transceive(pnd, abtCmd, szTxInAutoPoll, abtRx, szRx, timeout);
            szRx = (int)res;
            if (res < 0)
            {
                return res;
            }
            else if (szRx > 0)
            {
                szTargetFound = abtRx[0];
                if (szTargetFound > 0)
                {
                    byte ln;
                    int pbt = 1;
                    /* 1st target */
                    // Target type
                    pn53x_target_type ptt = (pn53x_target_type)abtRx[pbt++];
                    pntTargets[0].nm = pn53x_ptt_to_nm(ptt);
                    // AutoPollTargetData length
                    ln = abtRx[pbt++];
                    if ((res = pn53x_decode_target_data(MiscTool.SubBytes(abtRx, pbt), ln, ((pn53x_data)pnd.chip_data).type, pntTargets[0].nm.nmt,ref (pntTargets[0].nti))) < 0)
                    {
                        return res;
                    }
                    pbt += ln;

                    if (abtRx[0] > 1)
                    {
                        /* 2nd target */
                        // Target type
                        ptt = (pn53x_target_type)abtRx[pbt++];
                        pntTargets[1].nm = pn53x_ptt_to_nm(ptt);
                        // AutoPollTargetData length
                        ln = abtRx[pbt++];
                        pn53x_decode_target_data(MiscTool.SubBytes(abtRx, pbt), ln, ((pn53x_data)pnd.chip_data).type, pntTargets[1].nm.nmt,ref (pntTargets[1].nti));
                    }
                }
            }
            return szTargetFound;
        }



        /**
         * @brief Wrapper for InJumpForDEP command
         * @param pmInitModulation desired initial modulation
         * @param pbtPassiveInitiatorData NFCID1 (4 bytes) at 106kbps (optionnal, see NFCIP-1: 11.2.1.26) or Polling Request Frame's payload (5 bytes) at 212/424kbps (mandatory, see NFCIP-1: 11.2.2.5)
         * @param szPassiveInitiatorData size of pbtPassiveInitiatorData content
         * @param pbtNFCID3i NFCID3 of the initiator
         * @param pbtGBi General Bytes of the initiator
         * @param szGBi count of General Bytes
         * @param[out] pnt \a NFC.nfc_target which will be filled by this function
         */
        protected int
        pn53x_InJumpForDEP(NFC.nfc_device pnd,
                            NFC.nfc_dep_mode ndm,
                            NFC.nfc_baud_rate nbr,
                            byte[] pbtPassiveInitiatorData,
                            byte[] pbtNFCID3i,
                            byte[] pbtGBi, int szGBi,
                            NFC.nfc_target pnt,
                            int timeout)
        {
            // Max frame size = 1 (Command) + 1 (ActPass) + 1 (Baud rate) + 1 (Next) + 5 (PassiveInitiatorData) + 10 (NFCID3) + 48 (General bytes) = 67 bytes
            byte[] abtCmd = new byte[67];// { InJumpForDEP, (ndm == NFC.nfc_dep_mode.NDM_ACTIVE) ? (byte)0x01 : (byte)0x00 };
            abtCmd[0] = InJumpForDEP;
            abtCmd[1] = (ndm == NFC.nfc_dep_mode.NDM_ACTIVE) ? (byte)0x01 : (byte)0x00;
            int offset = 4; // 1 byte for command, 1 byte for DEP mode (Active/Passive), 1 byte for baud rate, 1 byte for following parameters flag

            switch (nbr)
            {
                case NFC.nfc_baud_rate.NBR_106:
                    abtCmd[2] = 0x00; // baud rate is 106 kbps
                    if (pbtPassiveInitiatorData != null && (ndm == NFC.nfc_dep_mode.NDM_PASSIVE))
                    {        /* can't have passive initiator data when using active mode */
                        abtCmd[3] |= 0x01;
                        MiscTool.memcpy(abtCmd, offset, pbtPassiveInitiatorData,0, 4);
                        offset += 4;
                    }
                    break;
                case NFC.nfc_baud_rate.NBR_212:
                    abtCmd[2] = 0x01; // baud rate is 212 kbps
                    if (pbtPassiveInitiatorData != null && (ndm == NFC.nfc_dep_mode.NDM_PASSIVE))
                    {
                        abtCmd[3] |= 0x01;
                        MiscTool.memcpy(abtCmd , offset, pbtPassiveInitiatorData,0, 5);
                        offset += 5;
                    }
                    break;
                case NFC.nfc_baud_rate.NBR_424:
                    abtCmd[2] = 0x02; // baud rate is 424 kbps
                    if (pbtPassiveInitiatorData != null && (ndm == NFC.nfc_dep_mode.NDM_PASSIVE))
                    {
                        abtCmd[3] |= 0x01;
                        MiscTool.memcpy(abtCmd , offset, pbtPassiveInitiatorData,0, 5);
                        offset += 5;
                    }
                    break;
                case NFC.nfc_baud_rate.NBR_847:
                case NFC.nfc_baud_rate.NBR_UNDEFINED:
                    pnd.last_error = NFC.NFC_EINVARG;
                    return pnd.last_error;
                    break;
            }

            if (pbtNFCID3i != null)
            {
                abtCmd[3] |= 0x02;
                MiscTool.memcpy(abtCmd , offset, pbtNFCID3i,0, 10);
                offset += 10;
            }

            if (szGBi != 0 && pbtGBi != null)
            {
                abtCmd[3] |= 0x04;
                MiscTool.memcpy(abtCmd , offset, pbtGBi,0, szGBi);
                offset += szGBi;
            }

            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            int res = 0;
            // Try to find a target, call the transceive callback function of the current device
            if ((res = pn53x_transceive(pnd, abtCmd, offset, abtRx, szRx, timeout)) < 0)
                return res;
            szRx = (int)res;
            // Make sure one target has been found, the PN53X returns 0x00 if none was available
            if (abtRx[1] >= 1)
            {
                // Is a target struct available
                if (pnt != null)
                {
                    pnt.nm.nmt = NFC.nfc_modulation_type.NMT_DEP;
                    pnt.nm.nbr = nbr;
                    pnt.nti.ndi.ndm = ndm;
                    MiscTool.memcpy(pnt.nti.ndi.abtNFCID3, 0,abtRx , 2, 10);
                    pnt.nti.ndi.btDID = abtRx[12];
                    pnt.nti.ndi.btBS = abtRx[13];
                    pnt.nti.ndi.btBR = abtRx[14];
                    pnt.nti.ndi.btTO = abtRx[15];
                    pnt.nti.ndi.btPP = abtRx[16];
                    if (szRx > 17)
                    {
                        pnt.nti.ndi.szGB = szRx - 17;
                        MiscTool.memcpy(pnt.nti.ndi.abtGB,0, abtRx , 17, pnt.nti.ndi.szGB);
                    }
                    else
                    {
                        pnt.nti.ndi.szGB = 0;
                    }
                }
            }
            return abtRx[1];
        }

        protected int
        pn53x_TgInitAsTarget(NFC.nfc_device pnd, pn53x_target_mode ptm,
                              byte[] pbtMifareParams,
                              byte[] pbtTkt, int szTkt,
                              byte[] pbtFeliCaParams,
                              byte[] pbtNFCID3t, byte[] pbtGBt, int szGBt,
                            /*out*/ byte[] pbtRx, int szRxLen, out byte pbtModeByte, int timeout)
        {
            byte[] abtCmd = new byte[39 + 47 + 48];// Worst case: 39-byte base, 47 bytes max. for General Bytes, 48 bytes max. for Historical Bytes
            // Clear the target init struct, reset to all zeros
            MiscTool.memset(abtCmd, 0x00, abtCmd.Length);
            abtCmd[0]=TgInitAsTarget ; 
            int szOptionalBytes = 0;
            int res = 0;
            pbtModeByte = 0x00;
 

            // Store the target mode in the initialization params
            abtCmd[1] = (byte)ptm;

            // MIFARE part
            if (pbtMifareParams!=null)
            {
                MiscTool.memcpy(abtCmd , 2, pbtMifareParams,0, 6);
            }
            // FeliCa part
            if (pbtFeliCaParams!=null)
            {
                MiscTool.memcpy(abtCmd , 8, pbtFeliCaParams,0, 18);
            }
            // DEP part
            if (pbtNFCID3t!=null)
            {
                MiscTool.memcpy(abtCmd , 26, pbtNFCID3t, 0,10);
            }
            // General Bytes (ISO/IEC 18092)
            if ((((pn53x_data)pnd.chip_data).type == pn53x_type.PN531) || (((pn53x_data)pnd.chip_data).type == pn53x_type.RCS360))
            {
                if (szGBt != 0)
                {
                    MiscTool.memcpy(abtCmd , 36, pbtGBt,0, szGBt);
                    szOptionalBytes = szGBt;
                }
            }
            else
            {
                abtCmd[36] = (byte)(szGBt);
                if (szGBt != 0)
                {
                    MiscTool.memcpy(abtCmd , 37, pbtGBt,0, szGBt);
                }
                szOptionalBytes = szGBt + 1;
            }
            // Historical bytes (ISO/IEC 14443-4)
            if ((((pn53x_data)pnd.chip_data).type != pn53x_type.PN531) && (((pn53x_data)pnd.chip_data).type != pn53x_type.RCS360))
            { // PN531 does not handle Historical Bytes
                abtCmd[36 + szOptionalBytes] = (byte)(szTkt);
                if (szTkt != 0)
                {
                    MiscTool.memcpy(abtCmd ,37 + szOptionalBytes, pbtTkt,0, szTkt);
                }
                szOptionalBytes += szTkt + 1;
            }

            // Request the initialization as a target
            byte[] abtRx = new byte[PN53x_EXTENDED_FRAME__DATA_MAX_LEN];
            int szRx = abtRx.Length;
            if ((res = pn53x_transceive(pnd, abtCmd, 36 + szOptionalBytes, abtRx, szRx, timeout)) < 0)
                return res;
            szRx = (int)res;

            // Note: the first byte is skip:
            //       its the "mode" byte which contains baudrate, DEP and Framing type (Mifare, active or FeliCa) datas.

            pbtModeByte = abtRx[0];


            // Save the received byte count
            szRx -= 1;

            if ((szRx - 1) > szRxLen)
                return NFC.NFC_EOVFLOW;
            // Copy the received bytes
            MiscTool.memcpy(pbtRx,0, abtRx , 1, szRx);

            return szRx;
        }

        protected int
        pn53x_check_ack_frame(NFC.nfc_device pnd, byte[] pbtRxFrame, int szRxFrameLen)
        {
            if (szRxFrameLen >= pn53x_ack_frame.Length)
            {
                if (0 == MiscTool.memcmp(pbtRxFrame, pn53x_ack_frame, pn53x_ack_frame.Length))
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "PN53x ACKed");
                    return NFC.NFC_SUCCESS;
                }
            }
            pnd.last_error = NFC.NFC_EIO;
            //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "%s", "Unexpected PN53x reply!");
            return pnd.last_error;
        }

        protected int
        pn53x_check_error_frame(NFC.nfc_device pnd, byte[] pbtRxFrame, int szRxFrameLen)
        {
            if (szRxFrameLen >= pn53x_error_frame.Length)
            {
                if (0 == MiscTool.memcmp(pbtRxFrame, pn53x_error_frame, pn53x_error_frame.Length))
                {
                    //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_DEBUG, "%s", "PN53x sent an error frame");
                    pnd.last_error = NFC.NFC_EIO;
                    return pnd.last_error;
                }
            }
            return NFC.NFC_SUCCESS;
        }

        /**
         * @brief Build a PN53x frame
         *
         * @param pbtData payload (bytes array) of the frame, will become PD0, ..., PDn in PN53x frame
         * @note The first byte of pbtData is the Command Code (CC)
         */
        protected int
        pn53x_build_frame(byte[] pbtFrame, out int pszFrame, byte[] pbtData, int szData)
        {
            pszFrame = 0;

            // Every packet must start with "00 00 ff"
            pbtFrame[0] = 0x00;
            pbtFrame[1] = 0x00;
            pbtFrame[2] = 0xff;

            if (szData <= PN53x_NORMAL_FRAME__DATA_MAX_LEN)
            {
                // LEN - Packet length = data length (len) + checksum (1) + end of stream marker (1)
                pbtFrame[3] = (byte)(szData + 1);
                // LCS - Packet length checksum
                pbtFrame[4] = (byte)(256 - (szData + 1));
                // TFI
                pbtFrame[5] = 0xD4;
                // DATA - Copy the PN53X command into the packet buffer
                MiscTool.memcpy(pbtFrame, 6, pbtData, 0, szData);

                // DCS - Calculate data payload checksum
                byte btDCS = (256 - 0xD4);
                for (int szPos = 0; szPos < szData; szPos++)
                {
                    btDCS -= pbtData[szPos];
                }
                pbtFrame[6 + szData] = btDCS;

                // 0x00 - End of stream marker
                pbtFrame[szData + 7] = 0x00;

                pszFrame = szData + PN53x_NORMAL_FRAME__OVERHEAD;
            }
            else if (szData <= PN53x_EXTENDED_FRAME__DATA_MAX_LEN)
            {
                // Extended frame marker
                pbtFrame[3] = 0xff;
                pbtFrame[4] = 0xff;
                // LENm
                pbtFrame[5] = (byte)((szData + 1) >> 8);
                // LENl
                pbtFrame[6] = (byte)((szData + 1) & 0xff);
                // LCS
                pbtFrame[7] = (byte)(256 - ((pbtFrame[5] + pbtFrame[6]) & 0xff));
                // TFI
                pbtFrame[8] = 0xD4;
                // DATA - Copy the PN53X command into the packet buffer
                MiscTool.memcpy(pbtFrame, 9, pbtData, 0, szData);

                // DCS - Calculate data payload checksum
                byte btDCS = (256 - 0xD4);
                for (int szPos = 0; szPos < szData; szPos++)
                {
                    btDCS -= pbtData[szPos];
                }
                pbtFrame[9 + szData] = btDCS;

                // 0x00 - End of stream marker
                pbtFrame[szData + 10] = 0x00;

                pszFrame = szData + PN53x_EXTENDED_FRAME__OVERHEAD;
            }
            else
            {
                //log_put(LOG_GROUP, LOG_CATEGORY, NFC.NFC_LOG_PRIORITY_ERROR, "We can't send more than %d bytes in a raw (requested: %" PRIdPTR ")", PN53x_EXTENDED_FRAME__DATA_MAX_LEN, szData);
                return NFC.NFC_ECHIP;
            }
            return NFC.NFC_SUCCESS;
        }
        protected pn53x_modulation
        pn53x_nm_to_pm(NFC.nfc_modulation nm)
        {
            switch (nm.nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    return pn53x_modulation.PM_ISO14443A_106;
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443B:
                    switch (nm.nbr)
                    {
                        case NFC.nfc_baud_rate.NBR_106:
                            return pn53x_modulation.PM_ISO14443B_106;
                            break;
                        case NFC.nfc_baud_rate.NBR_212:
                            return pn53x_modulation.PM_ISO14443B_212;
                            break;
                        case NFC.nfc_baud_rate.NBR_424:
                            return pn53x_modulation.PM_ISO14443B_424;
                            break;
                        case NFC.nfc_baud_rate.NBR_847:
                            return pn53x_modulation.PM_ISO14443B_847;
                            break;
                        case NFC.nfc_baud_rate.NBR_UNDEFINED:
                            // Nothing to do...
                            break;
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_JEWEL:
                    return pn53x_modulation.PM_JEWEL_106;
                    break;

                case NFC.nfc_modulation_type.NMT_FELICA:
                    switch (nm.nbr)
                    {
                        case NFC.nfc_baud_rate.NBR_212:
                            return pn53x_modulation.PM_FELICA_212;
                            break;
                        case NFC.nfc_baud_rate.NBR_424:
                            return pn53x_modulation.PM_FELICA_424;
                            break;
                        case NFC.nfc_baud_rate.NBR_106:
                        case NFC.nfc_baud_rate.NBR_847:
                        case NFC.nfc_baud_rate.NBR_UNDEFINED:
                            // Nothing to do...
                            break;
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                case NFC.nfc_modulation_type.NMT_DEP:
                    // Nothing to do...
                    break;
            }
            return pn53x_modulation.PM_UNDEFINED;
        }

        protected NFC.nfc_modulation
        pn53x_ptt_to_nm(pn53x_target_type ptt)
        {
            NFC.nfc_modulation nm;
            nm.nbr = NFC.nfc_baud_rate.NBR_106;
            nm.nmt = NFC.nfc_modulation_type.NMT_ISO14443A;
            switch (ptt)
            {
                case pn53x_target_type.PTT_GENERIC_PASSIVE_106:
                case pn53x_target_type.PTT_GENERIC_PASSIVE_212:
                case pn53x_target_type.PTT_GENERIC_PASSIVE_424:
                case pn53x_target_type.PTT_UNDEFINED:
                    // XXX This should not happend, how handle it cleanly ?
                    break;

                case pn53x_target_type.PTT_MIFARE:
                case pn53x_target_type.PTT_ISO14443_4A_106:
                    /* same as default
                        nm.nbr = NFC.nfc_baud_rate.NBR_106;
                        nm.nmt = NFC.nfc_modulation_type.NMT_ISO14443A;
                     * */
                    break;

                case pn53x_target_type.PTT_ISO14443_4B_106:
                case pn53x_target_type.PTT_ISO14443_4B_TCL_106:
                    nm.nbr = NFC.nfc_baud_rate.NBR_106;
                    nm.nmt = NFC.nfc_modulation_type.NMT_ISO14443B;
                    //return new NFC.nfc_modulation {,  };
                    break;

                case pn53x_target_type.PTT_JEWEL_106:
                    nm.nbr = NFC.nfc_baud_rate.NBR_106;
                    nm.nmt = NFC.nfc_modulation_type.NMT_JEWEL;
                    //return new NFC.nfc_modulation {,  };
                    break;

                case pn53x_target_type.PTT_FELICA_212:
                    nm.nbr = NFC.nfc_baud_rate.NBR_212;
                    nm.nmt = NFC.nfc_modulation_type.NMT_FELICA;
                    //return new NFC.nfc_modulation {,  };
                    break;
                case pn53x_target_type.PTT_FELICA_424:
                    nm.nbr = NFC.nfc_baud_rate.NBR_424;
                    nm.nmt = NFC.nfc_modulation_type.NMT_FELICA;
                    //return new NFC.nfc_modulation {,  };
                    break;

                case pn53x_target_type.PTT_DEP_PASSIVE_106:
                case pn53x_target_type.PTT_DEP_ACTIVE_106:
                    nm.nbr = NFC.nfc_baud_rate.NBR_106;
                    nm.nmt = NFC.nfc_modulation_type.NMT_DEP;
                    //return new NFC.nfc_modulation {,  };
                    break;
                case pn53x_target_type.PTT_DEP_PASSIVE_212:
                case pn53x_target_type.PTT_DEP_ACTIVE_212:
                    nm.nbr = NFC.nfc_baud_rate.NBR_212;
                    nm.nmt = NFC.nfc_modulation_type.NMT_DEP;
                    //return new NFC.nfc_modulation {, };
                    break;
                case pn53x_target_type.PTT_DEP_PASSIVE_424:
                case pn53x_target_type.PTT_DEP_ACTIVE_424:
                    nm.nbr = NFC.nfc_baud_rate.NBR_424;
                    nm.nmt = NFC.nfc_modulation_type.NMT_DEP;
                    //return new NFC.nfc_modulation {,  };
                    break;
            }
            // We should never be here, this line silent compilation warning
            //return new NFC.nfc_modulation {NFC.nfc_modulation_type.NMT_ISO14443A, NFC.nfc_baud_rate.NBR_106 };
            return nm;
        }

        protected pn53x_target_type
        pn53x_nm_to_ptt(NFC.nfc_modulation nm)
        {
            switch (nm.nmt)
            {
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    return pn53x_target_type.PTT_MIFARE;
                    // return PTT_ISO14443_4A_106;
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443B:
                    switch (nm.nbr)
                    {
                        case NFC.nfc_baud_rate.NBR_106:
                            return pn53x_target_type.PTT_ISO14443_4B_106;
                            break;
                        case NFC.nfc_baud_rate.NBR_UNDEFINED:
                        case NFC.nfc_baud_rate.NBR_212:
                        case NFC.nfc_baud_rate.NBR_424:
                        case NFC.nfc_baud_rate.NBR_847:
                            // Nothing to do...
                            break;
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_JEWEL:
                    return pn53x_target_type.PTT_JEWEL_106;
                    break;

                case NFC.nfc_modulation_type.NMT_FELICA:
                    switch (nm.nbr)
                    {
                        case NFC.nfc_baud_rate.NBR_212:
                            return pn53x_target_type.PTT_FELICA_212;
                            break;
                        case NFC.nfc_baud_rate.NBR_424:
                            return pn53x_target_type.PTT_FELICA_424;
                            break;
                        case NFC.nfc_baud_rate.NBR_UNDEFINED:
                        case NFC.nfc_baud_rate.NBR_106:
                        case NFC.nfc_baud_rate.NBR_847:
                            // Nothing to do...
                            break;
                    }
                    break;

                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                case NFC.nfc_modulation_type.NMT_DEP:
                    // Nothing to do...
                    break;
            }
            return pn53x_target_type.PTT_UNDEFINED;
        }

        protected int
        pn53x_get_supported_modulation(NFC.nfc_device pnd, NFC.nfc_mode mode,out NFC.nfc_modulation_type[] supported_mt)
        {
            supported_mt = null;
            switch (mode)
            {
                case NFC.nfc_mode.N_TARGET:
                    supported_mt = ((pn53x_data)pnd.chip_data).supported_modulation_as_target;
                    break;
                case NFC.nfc_mode.N_INITIATOR:
                    supported_mt = ((pn53x_data)pnd.chip_data).supported_modulation_as_initiator;
                    break;
                default:
                    return NFC.NFC_EINVARG;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_get_supported_baud_rate(NFC.nfc_device pnd, NFC.nfc_modulation_type nmt,out NFC.nfc_baud_rate[] supported_br)
        {
            supported_br = null;
            switch (nmt)
            {
                case NFC.nfc_modulation_type.NMT_FELICA:
                    supported_br = pn53x_felica_supported_baud_rates;
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443A:
                    supported_br = pn53x_iso14443a_supported_baud_rates;
                    break;
                case NFC.nfc_modulation_type.NMT_ISO14443B:
                case NFC.nfc_modulation_type.NMT_ISO14443BI:
                case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                    {
                        if ((((pn53x_data)pnd.chip_data).type != pn53x_type.PN533))
                        {
                            supported_br = pn532_iso14443b_supported_baud_rates;
                        }
                        else
                        {
                            supported_br = pn533_iso14443b_supported_baud_rates;
                        }
                    }
                    break;
                case NFC.nfc_modulation_type.NMT_JEWEL:
                    supported_br = pn53x_jewel_supported_baud_rates;
                    break;
                case NFC.nfc_modulation_type.NMT_DEP:
                    supported_br = pn53x_dep_supported_baud_rates;
                    break;
                default:
                    return NFC.NFC_EINVARG;
            }
            return NFC.NFC_SUCCESS;
        }

        protected int
        pn53x_get_information_about(NFC.nfc_device pnd, out string info)
        {
            int res = 0;
            StringBuilder txt = new StringBuilder();
            txt.Append("Chip: " + ((pn53x_data)pnd.chip_data).firmware_text + "\n");
            txt.Append("Initator mode modulations:\n");

            NFC.nfc_modulation_type[] nmt;
            if ((res = NFC.nfc_device_get_supported_modulation(pnd, NFC.nfc_mode.N_INITIATOR, out nmt)) < 0)
            {
                info = txt.ToString();
                return res;
            }

            for (int i = 0; i < nmt.Length/* nmt[i]*/; i++)
            {
                txt.Append("\t" + NFC.str_nfc_modulation_type(nmt[i]) + " - [");
                NFC.nfc_baud_rate[] nbr;
                if ((res = NFC.nfc_device_get_supported_baud_rate(pnd, nmt[i], out nbr)) < 0)
                {
                    info = txt.ToString();
                    return res;
                }
                for (int j = 0; j < nbr.Length; j++)
                {
                    txt.Append(NFC.str_nfc_baud_rate(nbr[j]) + " ");
                }
                txt.Append("]\n");
            }

            txt.Append("Target mode modulations:\n");

            if ((res = NFC.nfc_device_get_supported_modulation(pnd, NFC.nfc_mode.N_TARGET, out nmt)) < 0)
            {
                info = txt.ToString();
                return res;
            }

            for (int i = 0; i < nmt.Length; i++)
            {
                //snprintf(buf, buflen, "%s%s (", (i == 0) ? "" : ", ", )) < 0) {
                txt.Append("\t" + NFC.str_nfc_modulation_type(nmt[i]) + " - [");
                NFC.nfc_baud_rate[] nbr;
                if ((res = NFC.nfc_device_get_supported_baud_rate(pnd, nmt[i], out nbr)) < 0)
                {
                    info = txt.ToString();
                    return res;
                }

                for (int j = 0; j < nbr.Length; j++)
                {
                    txt.Append(NFC.str_nfc_baud_rate(nbr[j]) + " ");
                }
                txt.Append("]\n");
            }
            info = txt.ToString();
            return NFC.NFC_SUCCESS;
        }

        protected NFC.nfc_target pn53x_current_target_new(ref NFC.nfc_device pnd, NFC.nfc_target pnt)
        {
            if (pnt == null)
            {
                return null;
            }
            ((pn53x_data)pnd.chip_data).current_target = pnt;
            return ((pn53x_data)pnd.chip_data).current_target;
        }

        protected void
        pn53x_current_target_free(NFC.nfc_device pnd)
        {
            ((pn53x_data)pnd.chip_data).current_target = null;
        }

        protected bool
        pn53x_current_target_is(NFC.nfc_device pnd, NFC.nfc_target pnt)
        {
            if ((((pn53x_data)pnd.chip_data).current_target == null) || (pnt == null))
            {
                return false;
            }
            return MiscTool.CompareType<NFC.nfc_target>(pnt, ((pn53x_data)pnd.chip_data).current_target);
        }

        protected pn53x_data pn53x_data_new(NFC.nfc_device pnd/*, pn532_uart.pn53x_io io*/)
        {
            pn53x_data chip_data = new pn53x_data();
            // Keep I/O functions
            //chip_data.io = io;

            // Set type to generic (means unknown)
            chip_data.type = pn53x_type.PN53X;

            // Set power mode to normal, if your device starts in LowVBat (ie. PN532
            // UART) the driver layer have to correctly set it.
            chip_data.power_mode = pn53x_power_mode.NORMAL;

            // PN53x starts in initiator mode
            chip_data.operating_mode = pn53x_operating_mode.INITIATOR;

            // Clear last status byte
            chip_data.last_status_byte = 0x00;

            // Set current target to null
            chip_data.current_target = null;

            // Set current sam_mode to normal mode
            chip_data.sam_mode = pn532_sam_mode.PSM_NORMAL;

            // WriteBack cache is clean
            chip_data.wb_trigged = false;
            MiscTool.memset(chip_data.wb_mask, (byte)0x00, (int)PN53X_CACHE_REGISTER_SIZE);

            // Set default command timeout (350 ms)
            chip_data.timeout_command = 350;

            // Set default ATR timeout (103 ms)
            chip_data.timeout_atr = 103;

            // Set default communication timeout (52 ms)
            chip_data.timeout_communication = 52;

            chip_data.supported_modulation_as_initiator = null;

            chip_data.supported_modulation_as_target = null;
            pnd.chip_data = chip_data;
            return chip_data;//pnd.chip_data;
        }

        protected void
        pn53x_data_free(NFC.nfc_device pnd)
        {
            // Free current target
            pn53x_current_target_free(pnd);

            // Free supported modulation(s)
            if (((pn53x_data)pnd.chip_data).supported_modulation_as_initiator != null)
            {
                //free(((pn53x_data)pnd.chip_data).supported_modulation_as_initiator);
            }
            //free(pnd.chip_data);
        }

    }
}




