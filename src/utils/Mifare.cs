using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp.nfc.utils
{
    class Mifare
    {
        public enum mifare_cmd
        {
            MC_AUTH_A = 0x60,
            MC_AUTH_B = 0x61,
            MC_READ = 0x30,
            MC_WRITE = 0xA0,
            MC_TRANSFER = 0xB0,
            MC_DECREMENT = 0xC0,
            MC_INCREMENT = 0xC1,
            MC_STORE = 0xC2
        } ;

        // MIFARE command params
        public class mifare_param_auth
        {
            public byte[] abtKey;//[6];
            public byte[] abtAuthUid;//[4];
            public int Length()
            {
                return abtAuthUid.Length + abtKey.Length;
            }
            public void FillRawData(byte[] dst, int offset)
            {
                MiscTool.memcpy(dst, offset, abtKey, 0, abtKey.Length);
                offset += abtKey.Length;
                MiscTool.memcpy(dst, offset, abtAuthUid, 0, abtAuthUid.Length);

            }
            public mifare_param_auth()
            {
                abtKey = new byte[6];
                abtAuthUid = new byte[4];
            }
        };

        public class mifare_param_data
        {
            public byte[] abtData;//[16];
            public int Length()
            {
                return abtData.Length;
            }
            public void FillRawData(byte[] dst, int offset)
            {
                MiscTool.memcpy(dst, offset, abtData, 0, abtData.Length);
            }
            public mifare_param_data()
            {
                abtData = new byte[16];
            }
        };

        public class mifare_param_value
        {
            public byte[] abtValue;//[4];
            public int Length()
            {
                return abtValue.Length;
            }
            public void FillRawData(byte[] dst, int offset)
            {
                MiscTool.memcpy(dst, offset, abtValue, 0, abtValue.Length);
            }
            public mifare_param_value()
            {
                abtValue = new byte[4];
            }
        };

        public class mifare_param
        {
            public mifare_param_auth mpa;
            public mifare_param_data mpd;
            public mifare_param_value mpv;
            public mifare_param()
            {
                mpa = new mifare_param_auth();
                mpd = new mifare_param_data();
                mpv = new mifare_param_value();
            }
        } ;


        // MIFARE Classic
        public class mifare_classic_block_manufacturer
        {
            public byte[] abtUID;//[4];  // beware for 7bytes UID it goes over next fields
            public byte btBCC;
            public byte btSAK;      // beware it's not always exactly SAK
            public byte[] abtATQA;//[2];
            public byte[] abtManufacturer;//[8];
            public mifare_classic_block_manufacturer()
            {
                abtUID = new byte[4];
                abtATQA = new byte[2];
                abtManufacturer = new byte[8];
            }
        } ;

        public class mifare_classic_block_data
        {
            public byte[] abtData;//[16];
            public mifare_classic_block_data()
            {
                abtData = new byte[16];
            }
        } ;

        public class mifare_classic_block_trailer
        {
            public byte[] abtKeyA;//[6];
            public byte[] abtAccessBits;//[4];
            public byte[] abtKeyB;//[6];
            public mifare_classic_block_trailer()
            {
                abtKeyA = new byte[6];
                abtAccessBits = new byte[4];
                abtKeyB = new byte[6];
            }
        } ;

        public class mifare_classic_block
        {
            public mifare_classic_block_manufacturer mbm;
            public mifare_classic_block_data mbd;
            public mifare_classic_block_trailer mbt;
            public mifare_classic_block()
            {
                mbm = new mifare_classic_block_manufacturer();
                mbd = new mifare_classic_block_data();
                mbt = new mifare_classic_block_trailer();
            }
        } ;

        public class mifare_classic_tag
        {
            public mifare_classic_block[] amb;//[256];
            public mifare_classic_tag()
            {
                amb = new mifare_classic_block[256];
            }
        } ;

        // MIFARE Ultralight
        public class mifareul_block_manufacturer
        {
            public byte[] sn0;//[3];
            public byte btBCC0;
            public byte[] sn1;//[4];
            public byte btBCC1;
            public byte internal_n;
            public byte[] lock_n;//[2];
            public byte[] otp;//[4];
            public mifareul_block_manufacturer()
            {
                sn0 = new byte[3];
                sn1 = new byte[4];
                lock_n = new byte[2];
                otp = new byte[4];
            }
        } ;

        public class mifareul_block_data
        {
            public byte[] abtData;//[16];
            public mifareul_block_data()
            {
                abtData = new byte[16];
            }
        };

        public class mifareul_block
        {
            public mifareul_block_manufacturer mbm;
            public mifareul_block_data mbd;
            public mifareul_block()
            {
                mbm = new mifareul_block_manufacturer();
                mbd = new mifareul_block_data();
            }
        } ;

        public class mifareul_tag
        {
            public mifareul_block[] amb;//[4];
            public mifareul_tag()
            {
                amb = new mifareul_block[4];
            }
        } ;



        /**
         * @brief Execute a MIFARE Classic Command
         * @return Returns true if action was successfully performed; otherwise returns false.
         * @param pmp Some commands need additional information. This information should be supplied in the mifare_param union.
         *
         * The specified MIFARE command will be executed on the tag. There are different commands possible, they all require the destination block number.
         * @note There are three different types of information (Authenticate, Data and Value).
         *
         * First an authentication must take place using Key A or B. It requires a 48 bit Key (6 bytes) and the UID.
         * They are both used to initialize the internal cipher-state of the PN53X chip.
         * After a successful authentication it will be possible to execute other commands (e.g. Read/Write).
         * The MIFARE Classic Specification (http://www.nxp.com/acrobat/other/identification/M001053_MF1ICS50_rev5_3.pdf) explains more about this process.
         */
        public static bool
        nfc_initiator_mifare_cmd(NFCInternal.nfc_device pnd, mifare_cmd mc, byte ui8Block, mifare_param pmp)
        {
            byte[] abtRx = new byte[265];
            byte szParamLen;
            byte[] abtCmd = new byte[265];
            //bool    bEasyFraming;

            abtCmd[0] = (byte)mc;               // The MIFARE Classic command
            abtCmd[1] = ui8Block;         // The block address (1K=0x00..0x39, 4K=0x00..0xff)

            switch (mc)
            {
                // Read and store command have no parameter
                case mifare_cmd.MC_READ:
                case mifare_cmd.MC_STORE:
                    szParamLen = 0;
                    break;

                // Authenticate command
                case mifare_cmd.MC_AUTH_A:
                case mifare_cmd.MC_AUTH_B:
                    szParamLen = (byte)pmp.mpa.Length();//sizeof( mifare_param_auth);
                    if (szParamLen > 0)
                    {
                        pmp.mpa.FillRawData(abtCmd, 2);
                    }
                    break;

                // Data command
                case mifare_cmd.MC_WRITE:
                    szParamLen = (byte)pmp.mpd.Length();//sizeof( mifare_param_data);
                    if (szParamLen > 0)
                    {
                        pmp.mpd.FillRawData(abtCmd, 2);
                    }
                    break;

                // Value command
                case mifare_cmd.MC_DECREMENT:
                case mifare_cmd.MC_INCREMENT:
                case mifare_cmd.MC_TRANSFER:
                    szParamLen = (byte)pmp.mpv.Length();//sizeof( mifare_param_value);
                    if (szParamLen > 0)
                    {
                        pmp.mpv.FillRawData(abtCmd, 2);
                    }
                    break;

                // Please fix your code, you never should reach this statement
                default:
                    return false;
                    break;
            }


            // FIXME: Save and restore bEasyFraming
            // bEasyFraming = nfc_device_get_property_bool (pnd, NP_EASY_FRAMING, &bEasyFraming);
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true) < 0)
            {
                //nfc_perror(pnd, "nfc_device_set_property_bool");
                return false;
            }
            // Fire the mifare command
            int res;
            if ((res = NFC.nfc_initiator_transceive_bytes(pnd, abtCmd, 2 + szParamLen, abtRx, abtRx.Length, -1)) < 0)
            {
                if (res == NFC.NFC_ERFTRANS)
                {
                    // "Invalid received frame",  usual means we are
                    // authenticated on a sector but the requested MIFARE cmd (read, write)
                    // is not permitted by current acces bytes;
                    // So there is nothing to do here.
                }
                else
                {
                    //nfc_perror(pnd, "nfc_initiator_transceive_bytes");
                }
                // XXX nfc_device_set_property_bool (pnd, NP_EASY_FRAMING, bEasyFraming);
                return false;
            }
            /* XXX
            if (nfc_device_set_property_bool (pnd, NP_EASY_FRAMING, bEasyFraming) < 0) {
              nfc_perror (pnd, "nfc_device_set_property_bool");
              return false;
            }
            */

            // When we have executed a read command, copy the received bytes into the param
            if (mc == mifare_cmd.MC_READ)
            {
                if (res == 16)
                {
                    MiscTool.memcpy(pmp.mpd.abtData, 0, abtRx, 0, 16);
                }
                else
                {
                    return false;
                }
            }
            // Command succesfully executed
            return true;
        }

        private static NFC.nfc_modulation nmMfc;
        public static NFC.nfc_modulation nmMfClassic
        {
            get{return nmMfc;}
        }
        static Mifare()
        {
            nmMfc.nbr = NFCInternal.nfc_baud_rate.NBR_106;
            nmMfc.nmt = NFCInternal.nfc_modulation_type.NMT_ISO14443A;
        }

        public static bool
        is_first_block(uint uiBlock)
        {
            // Test if we are in the small or big sectors
            if (uiBlock < 128)
                return ((uiBlock) % 4 == 0);
            else
                return ((uiBlock) % 16 == 0);
        }

        public static bool
        is_trailer_block(uint uiBlock)
        {
            // Test if we are in the small or big sectors
            if (uiBlock < 128)
                return ((uiBlock + 1) % 4 == 0);
            else
                return ((uiBlock + 1) % 16 == 0);
        }

        public static uint
        get_trailer_block(uint uiFirstBlock)
        {
            // Test if we are in the small or big sectors
            uint trailer_block = 0;
            if (uiFirstBlock < 128)
            {
                trailer_block = uiFirstBlock + (3 - (uiFirstBlock % 4));
            }
            else
            {
                trailer_block = uiFirstBlock + (15 - (uiFirstBlock % 16));
            }
            return trailer_block;
        }

        public static int get_rats(NFC.nfc_device pnd, NFC.nfc_target pnt, byte[] abtRx)
        {
            int res;
            byte[] abtRats = { 0xe0, 0x50 };
            // Use raw send/receive methods
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false) < 0)
            {
                NFC.nfc_perror(pnd, "nfc_configure");
                return -1;
            }
            res = NFC.nfc_initiator_transceive_bytes(pnd, abtRats, abtRats.Length, abtRx, abtRx.Length, 0);
            if (res > 0)
            {
                // ISO14443-4 card, turn RF field off/on to access ISO14443-3 again
                if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_ACTIVATE_FIELD, false) < 0)
                {
                    NFC.nfc_perror(pnd, "nfc_configure");
                    return -1;
                }
                if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_ACTIVATE_FIELD, true) < 0)
                {
                    NFC.nfc_perror(pnd, "nfc_configure");
                    return -1;
                }
            }
            // Reselect tag after using get_rats, example:
            if (NFC.nfc_initiator_select_passive_target(pnd, nmMfClassic, null, 0, pnt) <= 0)
            {
                //printf("Error: tag disappeared\n");
                //NFC.nfc_close(pnd);
                //NFC.nfc_exit(context);
                //exit(EXIT_FAILURE);
                return -1;
            }
            return res;
        }

        public static bool unlock_card(NFC.nfc_device pnd)
        {
            byte[] abtRx = new byte[264];
            int szRx;
            /*
            if (magic2)
            {
                //printf("Don't use R/W with this card, this is not required!\n");
                return false;
            }*/
            byte[] abtHalt = { 0x50, 0x00, 0x00, 0x00 };
            // special unlock command
            byte[] abtUnlock1 = { 0x40 };
            byte[] abtUnlock2 = { 0x43 };
            // Configure the CRC
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_HANDLE_CRC, false) < 0)
            {
                NFC.nfc_perror(pnd, "nfc_configure");
                return false;
            }
            // Use raw send/receive methods
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, false) < 0)
            {
                NFC.nfc_perror(pnd, "nfc_configure");
                return false;
            }

            ISO14443Subr.iso14443a_crc_append(abtHalt, 2);
            szRx = NFC.nfc_initiator_transceive_bytes(pnd, abtHalt, 4, abtRx, abtRx.Length, 0);//transmit_bytes(abtHalt, 4);
            
            // now send unlock
            if ((szRx = NFC.nfc_initiator_transceive_bits(pnd, abtUnlock1, 7, null, abtRx, abtRx.Length, null))<0)//!transmit_bits(abtUnlock1, 7)
            {
                //printf("unlock failure!\n");
                return false;
            }
            if ((szRx = NFC.nfc_initiator_transceive_bits(pnd, abtUnlock2, 1, null, abtRx, abtRx.Length, null)) < 0)//(!transmit_bytes(abtUnlock2, 1))
            {
                //printf("unlock failure!\n");
                return false;
            }

            // reset reader
            // Configure the CRC
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_HANDLE_CRC, true) < 0)
            {
                NFC.nfc_perror(pnd, "nfc_device_set_property_bool");
                return false;
            }
            // Switch off raw send/receive methods
            if (NFC.nfc_device_set_property_bool(pnd, NFC.nfc_property.NP_EASY_FRAMING, true) < 0)
            {
                NFC.nfc_perror(pnd, "nfc_device_set_property_bool");
                return false;
            }
            return true;
        }

        public static bool authenticate(NFC.nfc_device pnd, NFC.nfc_target pnt, uint uiBlock, byte[] key, mifare_cmd mc)
        {
            mifare_param mp = new mifare_param();
            // Set the authentication information (uid)
            MiscTool.memcpy(mp.mpa.abtAuthUid, 0, pnt.nti.nai.abtUid, pnt.nti.nai.szUidLen - 4, 4);
            MiscTool.memcpy(mp.mpa.abtKey, 0, key, 0, 6);
            if (nfc_initiator_mifare_cmd(pnd, mc, (byte)uiBlock, mp))
            {
                return true;
            }
            if (NFC.nfc_initiator_select_passive_target(pnd, nmMfClassic, pnt.nti.nai.abtUid, pnt.nti.nai.szUidLen, null) <= 0)
            {
                //ERR("tag was removed");
                return false;
            }
            return true;

        }

    }
}
