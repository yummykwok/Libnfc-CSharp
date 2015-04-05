using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibNFC4CSharp.driver;

namespace LibNFC4CSharp
{
    class NFCInternal
    {
        public const int NFC_BUFSIZE_CONNSTRING = 1024;
        /* Error codes */
        /** @ingroup error
         * @hideinitializer
         * Success (no error)
         */
        public const int NFC_SUCCESS = 0;
        /** @ingroup error
         * @hideinitializer
         * Input / output error, device may not be usable anymore without re-open it
         */
        public const int NFC_EIO = -1;
        /** @ingroup error
         * @hideinitializer
         * Invalid argument(s)
         */
        public const int NFC_EINVARG = -2;
        /** @ingroup error
         * @hideinitializer
         *  Operation not supported by device
         */
        public const int NFC_EDEVNOTSUPP = -3;
        /** @ingroup error
         * @hideinitializer
         * No such device
         */
        public const int NFC_ENOTSUCHDEV = -4;
        /** @ingroup error
         * @hideinitializer
         * Buffer overflow
         */
        public const int NFC_EOVFLOW = -5;
        /** @ingroup error
         * @hideinitializer
         * Operation timed out
         */
        public const int NFC_ETIMEOUT = -6;
        /** @ingroup error
         * @hideinitializer
         * Operation aborted (by user)
         */
        public const int NFC_EOPABORTED = -7;
        /** @ingroup error
         * @hideinitializer
         * Not (yet) implemented
         */
        public const int NFC_ENOTIMPL = -8;
        /** @ingroup error
         * @hideinitializer
         * Target released
         */
        public const int NFC_ETGRELEASED = -10;
        /** @ingroup error
         * @hideinitializer
         * Error while RF transmission
         */
        public const int NFC_ERFTRANS = -20;
        /** @ingroup error
         * @hideinitializer
         * MIFARE Classic: authentication failed
         */
        public const int NFC_EMFCAUTHFAIL = -30;
        /** @ingroup error
         * @hideinitializer
         * Software error (allocation, file/pipe creation, etc.)
         */
        public const int NFC_ESOFT = -80;
        /** @ingroup error
         * @hideinitializer
         * Device's internal chip error
         */
        public const int NFC_ECHIP = -90;


        public static string ErrorMessage(int error)
        {
            switch (error)
            {
                case NFC_SUCCESS: return "Success"; 
                case NFC_EIO: return "Input / Output Error"; 
                case NFC_EINVARG: return "Invalid argument(s)"; 
                case NFC_EDEVNOTSUPP: return "Not Supported by Device"; 
                case NFC_ENOTSUCHDEV: return "No Such Device"; 
                case NFC_EOVFLOW: return "Buffer Overflow"; 
                case NFC_ETIMEOUT: return "Timeout"; 
                case NFC_EOPABORTED: return "Operation Aborted"; 
                case NFC_ENOTIMPL: return "Not (yet) Implemented"; 
                case NFC_ETGRELEASED: return "Target Released"; 
                case NFC_EMFCAUTHFAIL: return "Mifare Authentication Failed"; 
                case NFC_ERFTRANS: return "RF Transmission Error"; 
                case NFC_ECHIP: return "Device's Internal Chip Error"; 
                default: return "Unknown Error";
            }
        }

        /**
          * Properties
          */
        public enum nfc_property
        {
            /**
             * Default command processing timeout
             * Property value's (duration) unit is ms and 0 means no timeout (infinite).
             * Default value is set by driver layer
             */
            NP_TIMEOUT_COMMAND,
            /**
             * Timeout between ATR_REQ and ATR_RES
             * When the device is in initiator mode, a target is considered as mute if no
             * valid ATR_RES is received within this timeout value.
             * Default value for this property is 103 ms on PN53x based devices.
             */
            NP_TIMEOUT_ATR,
            /**
             * Timeout value to give up reception from the target in case of no answer.
             * Default value for this property is 52 ms).
             */
            NP_TIMEOUT_COM,
            /** Let the PN53X chip handle the CRC bytes. This means that the chip appends
            * the CRC bytes to the frames that are transmitted. It will parse the last
            * bytes from received frames as incoming CRC bytes. They will be verified
            * against the used modulation and protocol. If an frame is expected with
            * incorrect CRC bytes this option should be disabled. Example frames where
            * this is useful are the ATQA and UID+BCC that are transmitted without CRC
            * bytes during the anti-collision phase of the ISO14443-A protocol. */
            NP_HANDLE_CRC,
            /** Parity bits in the network layer of ISO14443-A are by default generated and
             * validated in the PN53X chip. This is a very convenient feature. On certain
             * times though it is useful to get full control of the transmitted data. The
             * proprietary MIFARE Classic protocol uses for example custom (encrypted)
             * parity bits. For interoperability it is required to be completely
             * compatible, including the arbitrary parity bits. When this option is
             * disabled, the functions to communicating bits should be used. */
            NP_HANDLE_PARITY,
            /** This option can be used to enable or disable the electronic field of the
             * NFC device. */
            NP_ACTIVATE_FIELD,
            /** The internal CRYPTO1 co-processor can be used to transmit messages
             * encrypted. This option is automatically activated after a successful MIFARE
             * Classic authentication. */
            NP_ACTIVATE_CRYPTO1,
            /** The default configuration defines that the PN53X chip will try indefinitely
             * to invite a tag in the field to respond. This could be desired when it is
             * certain a tag will enter the field. On the other hand, when this is
             * uncertain, it will block the application. This option could best be compared
             * to the (NON)BLOCKING option used by (socket)network programming. */
            NP_INFINITE_SELECT,
            /** If this option is enabled, frames that carry less than 4 bits are allowed.
             * According to the standards these frames should normally be handles as
             * invalid frames. */
            NP_ACCEPT_INVALID_FRAMES,
            /** If the NFC device should only listen to frames, it could be useful to let
             * it gather multiple frames in a sequence. They will be stored in the internal
             * FIFO of the PN53X chip. This could be retrieved by using the receive data
             * functions. Note that if the chip runs out of bytes (FIFO = 64 bytes long),
             * it will overwrite the first received frames, so quick retrieving of the
             * received data is desirable. */
            NP_ACCEPT_MULTIPLE_FRAMES,
            /** This option can be used to enable or disable the auto-switching mode to
             * ISO14443-4 is device is compliant.
             * In initiator mode, it means that NFC chip will send RATS automatically when
             * select and it will automatically poll for ISO14443-4 card when ISO14443A is
             * requested.
             * In target mode, with a NFC chip compliant (ie. PN532), the chip will
             * emulate a 14443-4 PICC using hardware capability */
            NP_AUTO_ISO14443_4,
            /** Use automatic frames encapsulation and chaining. */
            NP_EASY_FRAMING,
            /** Force the chip to switch in ISO14443-A */
            NP_FORCE_ISO14443_A,
            /** Force the chip to switch in ISO14443-B */
            NP_FORCE_ISO14443_B,
            /** Force the chip to run at 106 kbps */
            NP_FORCE_SPEED_106,
        } ;


        /**
         * @enum nfc_dep_mode
         * @brief NFC D.E.P. (Data Exchange Protocol) active/passive mode
         */
        public enum nfc_dep_mode
        {
            NDM_UNDEFINED = 0,
            NDM_PASSIVE,
            NDM_ACTIVE,
        } ;

        /**
         * @class nfc_dep_info
         * @brief NFC target information in D.E.P. (Data Exchange Protocol) see ISO/IEC 18092 (NFCIP-1)
         */
        public class nfc_dep_info
        {
            /** NFCID3 */
            public byte[] abtNFCID3;//[10];
            /** DID */
            public byte btDID;
            /** Supported send-bit rate */
            public byte btBS;
            /** Supported receive-bit rate */
            public byte btBR;
            /** Timeout value */
            public byte btTO;
            /** PP Parameters */
            public byte btPP;
            /** General Bytes */
            public byte[] abtGB;//[48];
            public int szGB;

            /** DEP mode */
            public nfc_dep_mode ndm;
            public nfc_dep_info()
            {
                abtNFCID3 = new byte[10];
                btDID = 0x00;
                btBS = 0x00;
                btBR = 0x00;
                btTO = 0x00;
                btPP = 0x00;
                szGB = 0;
                abtGB = new byte[48];
                ndm = nfc_dep_mode.NDM_UNDEFINED;
            }
        } ;

        /**
         * @class nfc_iso14443a_info
         * @brief NFC ISO14443A tag (MIFARE) information
         */
        public class nfc_iso14443a_info
        {
            public byte[] abtAtqa;//[2];
            public byte btSak;
            public int szUidLen;
            public byte[] abtUid;//[10];
            public int szAtsLen;
            public byte[] abtAts;//[254]; // Maximal theoretical ATS is FSD-2, FSD=256 for FSDI=8 in RATS
            public nfc_iso14443a_info()
            {
                abtAtqa = new byte[2];
                btSak = 0x00;
                szUidLen = 0;
                abtUid = new byte[10];
                szAtsLen = 0;
                abtAts = new byte[254];
            }
        } ;

        /**
         * @class nfc_felica_info
         * @brief NFC FeLiCa tag information
         */
        public class nfc_felica_info
        {
            public int szLen;
            public byte btResCode;
            public byte[] abtId;//[8];
            public byte[] abtPad;//[8];
            public byte[] abtSysCode;//[2];
            public nfc_felica_info()
            {
                szLen = 0;
                btResCode = 0x00;
                abtId = new byte[8];
                abtPad = new byte[8];
                abtSysCode = new byte[2];
            }
        } ;

        /**
         * @class nfc_iso14443b_info
         * @brief NFC ISO14443B tag information
         */
        public class nfc_iso14443b_info
        {
            /** abtPupi store PUPI contained in ATQB (Answer To reQuest of type B) (see ISO14443-3) */
            public byte[] abtPupi;//[4];
            /** abtApplicationData store Application Data contained in ATQB (see ISO14443-3) */
            public byte[] abtApplicationData;//[4];
            /** abtProtocolInfo store Protocol Info contained in ATQB (see ISO14443-3) */
            public byte[] abtProtocolInfo;//[3];
            /** ui8CardIdentifier store CID (Card Identifier) attributted by PCD to the PICC */
            public byte ui8CardIdentifier;
            public nfc_iso14443b_info()
            {
                abtPupi = new byte[4];
                abtApplicationData = new byte[4];
                abtProtocolInfo = new byte[3];
                ui8CardIdentifier = 0x00;
            }
        } ;

        /**
         * @class nfc_iso14443bi_info
         * @brief NFC ISO14443B' tag information
         */
        public class nfc_iso14443bi_info
        {
            /** DIV: 4 LSBytes of tag serial number */
            public byte[] abtDIV;//[4];
            /** Software version & type of REPGEN */
            public byte btVerLog;
            /** Config Byte, present if long REPGEN */
            public byte btConfig;
            /** ATR, if any */
            public int szAtrLen;
            public byte[] abtAtr;//[33];
            public nfc_iso14443bi_info()
            {
                abtDIV = new byte[4];
                btVerLog = 0x00;
                btConfig = 0x00;
                szAtrLen = 0;
                abtAtr = new byte[33];
            }
        } ;

        /**
         * @class nfc_iso14443b2sr_info
         * @brief NFC ISO14443-2B ST SRx tag information
         */
        public class nfc_iso14443b2sr_info
        {
            public byte[] abtUID;//[8];
            public nfc_iso14443b2sr_info()
            {
                abtUID = new byte[8];
            }
        } ;

        /**
         * @class nfc_iso14443b2ct_info
         * @brief NFC ISO14443-2B ASK CTx tag information
         */
        public class nfc_iso14443b2ct_info
        {
            public byte[] abtUID;//[4];
            public byte btProdCode;
            public byte btFabCode;
            public nfc_iso14443b2ct_info()
            {
                abtUID = new byte[4];
                btProdCode = 0x00;
                btFabCode = 0x00;
            }
        } ;

        /**
         * @class nfc_jewel_info
         * @brief NFC Jewel tag information
         */
        public class nfc_jewel_info
        {
            public byte[] btSensRes;//[2];
            public byte[] btId;//[4];
            public nfc_jewel_info()
            {
                btSensRes = new byte[2];
                btId = new byte[4];
            }
        } ;

        /**
         * @union nfc_target_info
         * @brief Union between all kind of tags information classures.
         */
        public class nfc_target_info
        {
            public nfc_iso14443a_info nai; //0
            public nfc_felica_info nfi;  //1
            public nfc_iso14443b_info nbi;//2
            public nfc_iso14443bi_info nii;//3
            public nfc_iso14443b2sr_info nsi;//4
            public nfc_iso14443b2ct_info nci;//5
            public nfc_jewel_info nji;//6
            public nfc_dep_info ndi;//7
            public nfc_target_info(int i)
            {
                switch (i)
                {
                    case 0:
                        nai = new nfc_iso14443a_info();
                        break;
                    case 1:
                        nfi = new nfc_felica_info();
                        break;
                    case 2:
                        nbi = new nfc_iso14443b_info();
                        break;
                    case 3:
                        nii = new nfc_iso14443bi_info();
                        break;
                    case 4:
                        nsi = new nfc_iso14443b2sr_info();
                        break;
                    case 5:
                        nci = new nfc_iso14443b2ct_info();
                        break;
                    case 6:
                        nji = new nfc_jewel_info();
                        break;
                    case 7:
                        ndi = new nfc_dep_info();
                        break;
                }
            }
            public nfc_target_info()
            {
                /*
                nai = null;
                nfi = null;
                nbi = null;
                nii = null;
                nsi = null;
                nci = null;
                nji = null;
                ndi = null;
                 * */
                nai = new nfc_iso14443a_info();
                nfi = new nfc_felica_info();
                nbi = new nfc_iso14443b_info();
                nii = new nfc_iso14443bi_info();
                nsi = new nfc_iso14443b2sr_info();
                nci = new nfc_iso14443b2ct_info();
                nji = new nfc_jewel_info();
                ndi = new nfc_dep_info();
            }
        };

        /**
         * @enum nfc_baud_rate
         * @brief NFC baud rate enumeration
         */
        public enum nfc_baud_rate
        {
            NBR_UNDEFINED = 0,
            NBR_106,
            NBR_212,
            NBR_424,
            NBR_847,
        };

        /**
         * @enum nfc_modulation_type
         * @brief NFC modulation type enumeration
         */
        public enum nfc_modulation_type
        {
            NMT_ISO14443A = 1,
            NMT_JEWEL,
            NMT_ISO14443B,
            NMT_ISO14443BI, // pre-ISO14443B aka ISO/IEC 14443 B' or Type B'
            NMT_ISO14443B2SR, // ISO14443-2B ST SRx
            NMT_ISO14443B2CT, // ISO14443-2B ASK CTx
            NMT_FELICA,
            NMT_DEP,
        };

        /**
         * @enum nfc_mode
         * @brief NFC mode type enumeration
         */
        public enum nfc_mode
        {
            N_TARGET,
            N_INITIATOR,
        } ;

        /**
         * @class nfc_modulation
         * @brief NFC modulation classure
         */
        public struct nfc_modulation
        {
            public nfc_modulation_type nmt;
            public nfc_baud_rate nbr;
        } ;

        /**
         * @class nfc_target
         * @brief NFC target classure
         */
        public class nfc_target
        {
            public nfc_target_info nti;
            public nfc_modulation nm;
            public nfc_target(int type)
            {
                nti = new nfc_target_info(type);
                nm.nbr = nfc_baud_rate.NBR_UNDEFINED;
                nm.nmt = nfc_modulation_type.NMT_DEP;
            }
            public nfc_target()
            {
                nti = new nfc_target_info();
                nm.nbr = nfc_baud_rate.NBR_UNDEFINED;
                nm.nmt = nfc_modulation_type.NMT_DEP;
            }
        } ;

        public enum scan_type_enum
        {
            NOT_INTRUSIVE,
            INTRUSIVE,
            NOT_AVAILABLE,
        };

        public const int DEVICE_NAME_LENGTH = 256;
        public const int DEVICE_PORT_LENGTH = 64;

        public const int MAX_USER_DEFINED_DEVICES = 4;

        public struct nfc_user_defined_device
        {
            public string name;
            public string connstring;
            public bool optional;
        };

        /**
         * @class nfc_context
         * @brief NFC library context
         * class which contains internal options, references, pointers, etc. used by library
         */
        public class nfc_context
        {
            public bool allow_autoscan;
            public bool allow_intrusive_scan;
            public UInt32 log_level;
            public nfc_user_defined_device[] user_defined_devices;//MAX_USER_DEFINED_DEVICES
            public UInt32 user_defined_device_count;
            public nfc_context()
            {
                allow_autoscan = true;
                allow_intrusive_scan = true;
                user_defined_devices = new nfc_user_defined_device[MAX_USER_DEFINED_DEVICES];
                user_defined_device_count = 0;
            }
        };

        protected void nfc_context_free(nfc_context context) { }

        /**
         * @class nfc_device
         * @brief NFC device information
         */
        public class nfc_device
        {
            public nfc_context context;
            public nfc_driver driver; //abstract
            public object driver_data;//abstract
            public object chip_data;//abstract

            /** Device name string, including device wrapper firmware */
            public string name;//[DEVICE_NAME_LENGTH];
            /** Device connection string */
            public string connstring;
            /** Is the CRC automaticly added, checked and removed from the frames */
            public bool bCrc;
            /** Does the chip handle parity bits, all parities are handled as data */
            public bool bPar;
            /** Should the chip handle frames encapsulation and chaining */
            public bool bEasyFraming;
            /** Should the chip try forever on select? */
            public bool bInfiniteSelect;
            /** Should the chip switch automatically activate ISO14443-4 when
                selecting tags supporting it? */
            public bool bAutoIso14443_4;
            /** Supported modulation encoded in a byte */
            public byte btSupportByte;
            /** Last reported error */
            public int last_error;
        };

        //void        nfc_device_free(nfc_device dev);

        //void string_as_boolean(const char *s, bool *value);



        /**
         * @brief Add cascade tags (0x88) in UID
         * @see ISO/IEC 14443-3 (6.4.4 UID contents and cascade levels)
         */
        public static void
        iso14443_cascade_uid(byte[] abtUID, int szUID, ref byte[] pbtCascadedUID, out int pszCascadedUID)
        {
            switch (szUID)
            {
                case 7:
                    pbtCascadedUID[0] = 0x88;
                    MiscTool.memcpy(pbtCascadedUID, 1, abtUID, 0, 7);
                    pszCascadedUID = 8;
                    break;

                case 10:
                    pbtCascadedUID[0] = 0x88;
                    MiscTool.memcpy(pbtCascadedUID, 1, abtUID, 0, 3);
                    pbtCascadedUID[4] = 0x88;
                    MiscTool.memcpy(pbtCascadedUID, 5, abtUID, 3, 7);
                    pszCascadedUID = 12;
                    break;

                case 4:
                default:
                    MiscTool.memcpy(pbtCascadedUID, 0, abtUID, 0, szUID);
                    pszCascadedUID = szUID;
                    break;
            }
        }
        //int connstring_decode(const nfc_connstring connstring, const char *driver_name, const char *bus_name, char **pparam1, char **pparam2);

        protected static nfc_context nfc_context_new()
        {
            nfc_context res = new nfc_context();


            // Set default context values
            res.allow_autoscan = true;
            res.allow_intrusive_scan = false;
            if (true)
            { //if debug
                res.log_level = 3;
            }
            else
            {
                res.log_level = 1;
            }

            // Clear user defined devices array
            for (int i = 0; i < MAX_USER_DEFINED_DEVICES; i++)
            {
                res.user_defined_devices[i].name = "";
                res.user_defined_devices[i].connstring = "";
                res.user_defined_devices[i].optional = false;
            }
            res.user_defined_device_count = 0;

            /*
            #ifdef CONFFILES
              // Load options from configuration file (ie. /etc/nfc/libnfc.conf)
              conf_load(res);
            #endif // CONFFILES
            */

            /*
              // Initialize log before use it...
              log_init(res);

              // Debug context state
              if(DEBUG){
              log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_NONE,  "log_level is set to %"PRIu32, res->log_level);

              }else{
              log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_DEBUG,  "log_level is set to %"PRIu32, res->log_level);
              }

              log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_DEBUG, "allow_autoscan is set to %s", (res->allow_autoscan) ? "true" : "false");
              log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_DEBUG, "allow_intrusive_scan is set to %s", (res->allow_intrusive_scan) ? "true" : "false");

              log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_DEBUG, "%d device(s) defined by user", res->user_defined_device_count);

              for (UInt32 i = 0; i < res.user_defined_device_count; i++) {
                log_put(LOG_GROUP, LOG_CATEGORY, NFC_LOG_PRIORITY_DEBUG, "  #%d name: \"%s\", connstring: \"%s\"", i, res->user_defined_devices[i].name, res->user_defined_devices[i].connstring);
              }
             * */
            return res;
        }

        public static byte[] prepare_initiator_data(nfc_modulation nm)/*, out ppbtInitiatorData ,// out int pszInitiatorData  //-- ppbtInitiatorData.Length --*/
        {
            //pszInitiatorData = 0;
            byte[] ppbtInitiatorData = null;
            switch (nm.nmt)
            {
                case nfc_modulation_type.NMT_ISO14443B:
                    {
                        // Application Family Identifier (AFI) must equals 0x00 in order to wakeup all ISO14443-B PICCs (see ISO/IEC 14443-3)

                        ppbtInitiatorData = new byte[1] { 0x00 };
                        //pszInitiatorData = 1;
                    }
                    break;
                case nfc_modulation_type.NMT_ISO14443BI:
                    {
                        // APGEN
                        ppbtInitiatorData = new byte[4] { 0x01, 0x0b, 0x3f, 0x80 };
                        //pszInitiatorData = 4;
                    }
                    break;
                case nfc_modulation_type.NMT_ISO14443B2SR:
                    {
                        // Get_UID
                        ppbtInitiatorData = new byte[1] { 0x0b };
                        //pszInitiatorData = 1;
                    }
                    break;
                case nfc_modulation_type.NMT_ISO14443B2CT:
                    {
                        // SELECT-ALL
                        ppbtInitiatorData = new byte[3] { 0x9F, 0xFF, 0xFF };
                        //pszInitiatorData = 3;
                    }
                    break;
                case nfc_modulation_type.NMT_FELICA:
                    {
                        // polling payload must be present (see ISO/IEC 18092 11.2.2.5)
                        ppbtInitiatorData = new byte[5] { 0x00, 0xff, 0xff, 0x01, 0x00 };
                        //pszInitiatorData = 5;
                    }
                    break;
                case nfc_modulation_type.NMT_ISO14443A:
                case nfc_modulation_type.NMT_JEWEL:
                case nfc_modulation_type.NMT_DEP:
                    ppbtInitiatorData = null;
                    //pszInitiatorData = 0;
                    break;
            }
            return ppbtInitiatorData;
        }

        public static nfc_device nfc_device_new(nfc_context context, string connstring)
        {
            nfc_device res = new nfc_device();

            // Store associated context
            res.context = context;

            // Variables initiatialization
            // Note: Actually, these initialization will be overwritten while the device
            // will be setup. Putting them to _false_ while the default is _true_ ensure we
            // send the command to the chip
            res.bCrc = false;
            res.bPar = false;
            res.bEasyFraming = false;
            res.bInfiniteSelect = false;
            res.bAutoIso14443_4 = false;
            res.last_error = 0;
            res.connstring = connstring;
            res.driver_data = null;
            res.chip_data = null;
            return res;
        }
    }
}
