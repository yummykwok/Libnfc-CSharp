using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp
{
    class TargetSubr
    {

        class card_atqa
        {
            public UInt16 atqa;
            public UInt16 mask;
            public string type;//[128];
            // list of up to 8 SAK values compatible with this ATQA
            public int[] saklist;//[8];
            public card_atqa(UInt16 atqa, UInt16 mask, string type, int[] saklist)
            {
                this.atqa = atqa;
                this.mask = mask;
                this.type = type;
                this.saklist = saklist;
            }
        };

        class card_sak
        {
            public byte sak;
            public byte mask;
            public string type;//[128];
            public card_sak(byte sak, byte mask, string type)
            {
                this.sak = sak;
                this.mask = mask;
                this.type = type;
            }
        };
        static List<card_atqa> const_ca = new List<card_atqa>();
        static List<card_sak> const_cs = new List<card_sak>();
        static TargetSubr()
        {
            const_ca.Add(new card_atqa(0x0044, 0xffff, "MIFARE Ultralight", new int[] { 0, -1 }));
            const_ca.Add(new card_atqa(0x0044, 0xffff, "MIFARE Ultralight C", new int[] { 0, -1 }));
            const_ca.Add(new card_atqa(0x0004, 0xff0f, "MIFARE Mini 0.3K", new int[] { 1, -1 }));
            const_ca.Add(new card_atqa(0x0004, 0xff0f, "MIFARE Classic 1K", new int[] { 2, -1 }));
            const_ca.Add(new card_atqa(0x0002, 0xff0f, "MIFARE Classic 4K", new int[] { 3, -1 }));
            const_ca.Add(new card_atqa(0x0004, 0xffff, "MIFARE Plus (4 Byte UID or 4 Byte RID)", new int[] { 4, 5, 6, 7, 8, 9, -1 }));
            const_ca.Add(new card_atqa(0x0002, 0xffff, "MIFARE Plus (4 Byte UID or 4 Byte RID)", new int[] { 4, 5, 6, 7, 8, 9, -1 }));
            const_ca.Add(new card_atqa(0x0044, 0xffff, "MIFARE Plus (7 Byte UID)", new int[] { 4, 5, 6, 7, 8, 9, -1 }));
            const_ca.Add(new card_atqa(0x0042, 0xffff, "MIFARE Plus (7 Byte UID)", new int[] { 4, 5, 6, 7, 8, 9, -1 }));
            const_ca.Add(new card_atqa(0x0344, 0xffff, "MIFARE DESFire", new int[] { 10, 11, -1 }));
            const_ca.Add(new card_atqa(0x0044, 0xffff, "P3SR008", new int[] { -1 }));
            const_ca.Add(new card_atqa(0x0004, 0xf0ff, "SmartMX with MIFARE 1K emulation", new int[] { 12, -1 }));
            const_ca.Add(new card_atqa(0x0002, 0xf0ff, "SmartMX with MIFARE 4K emulation", new int[] { 12, -1 }));
            const_ca.Add(new card_atqa(0x0048, 0xf0ff, "SmartMX with 7 Byte UID", new int[] { 12, -1 }));

            const_cs.Add(new card_sak(0x00, 0xff, ""));// 00 MIFARE Ultralight / Ultralight C
            const_cs.Add(new card_sak(0x09, 0xff, "")); // 01 MIFARE Mini 0.3K
            const_cs.Add(new card_sak(0x08, 0xff, ""));// 02 MIFARE Classic 1K
            const_cs.Add(new card_sak(0x18, 0xff, ""));// 03 MIFARE Classik 4K
            const_cs.Add(new card_sak(0x08, 0xff, " 2K, Security level 1"));// 04 MIFARE Plus
            const_cs.Add(new card_sak(0x18, 0xff, " 4K, Security level 1"));// 05 MIFARE Plus
            const_cs.Add(new card_sak(0x10, 0xff, " 2K, Security level 2"));// 06 MIFARE Plus
            const_cs.Add(new card_sak(0x11, 0xff, " 4K, Security level 2"));// 07 MIFARE Plus
            const_cs.Add(new card_sak(0x20, 0xff, " 2K, Security level 3"));// 08 MIFARE Plus
            const_cs.Add(new card_sak(0x20, 0xff, " 4K, Security level 3"));// 09 MIFARE Plus
            const_cs.Add(new card_sak(0x20, 0xff, " 4K"));// 10 MIFARE DESFire
            const_cs.Add(new card_sak(0x20, 0xff, " EV1 2K/4K/8K"));// 11 MIFARE DESFire
            const_cs.Add(new card_sak(0x00, 0x00, "")); // 12 SmartMX
        }

        public static string
        snprint_hex(byte[] pbtData, int szBytes)
        {
            int szPos;
            string strHex = "";
            for (szPos = 0; szPos < szBytes; szPos++)
            {
                strHex += pbtData[szPos].ToString("X2") + " ";
            }
            strHex += "\r\n";
            return strHex;
        }
        public static string
        snprint_hex(byte[] pbtData, int offset, int szBytes)
        {
            int szPos;
            string strHex = "";
            for (szPos = 0; szPos < szBytes; szPos++)
            {
                strHex += pbtData[offset + szPos].ToString("X2") + " ";
            }
            strHex += "\r\n";
            return strHex;
        }
        const byte SAK_UID_NOT_COMPLETE = 0x04;
        const byte SAK_ISO14443_4_COMPLIANT = 0x20;
        const byte SAK_ISO18092_COMPLIANT = 0x40;

        public static void
        snprint_nfc_iso14443a_info(out string dst, NFC.nfc_iso14443a_info pnai, bool verbose)
        {
            dst = "";
            dst += "    ATQA (SENS_RES): ";
            dst += snprint_hex(pnai.abtAtqa, 2);
            if (verbose)
            {
                dst += "* UID size: ";
                switch ((pnai.abtAtqa[1] & 0xc0) >> 6)
                {
                    case 0:
                        dst += "single\r\n";
                        break;
                    case 1:
                        dst += "double\r\n";
                        break;
                    case 2:
                        dst += "triple\r\n";
                        break;
                    case 3:
                        dst += "RFU\r\n";
                        break;
                }
                dst += "* bit frame anticollision ";
                switch (pnai.abtAtqa[1] & 0x1f)
                {
                    case 0x01:
                    case 0x02:
                    case 0x04:
                    case 0x08:
                    case 0x10:
                        dst += "supported\r\n";
                        break;
                    default:
                        dst += "not supported\r\n";
                        break;
                }
            }
            dst += "       UID (NFCID" + (pnai.abtUid[0] == 0x08 ? '3' : '1') + "): ";
            dst += snprint_hex(pnai.abtUid, pnai.szUidLen);
            if (verbose)
            {
                if (pnai.abtUid[0] == 0x08)
                {
                    dst += "* Random UID\r\n";
                }
            }
            dst += "      SAK (SEL_RES): ";
            dst += pnai.btSak.ToString("2X");
            if (verbose)
            {
                if (0 != (pnai.btSak & SAK_UID_NOT_COMPLETE))
                {
                    dst += "* Warning! Cascade bit set: UID not complete\r\n";
                }
                if (0 != (pnai.btSak & SAK_ISO14443_4_COMPLIANT))
                {
                    dst += "* Compliant with ISO/IEC 14443-4\r\n";
                }
                else
                {
                    dst += "* Not compliant with ISO/IEC 14443-4\r\n";
                }
                if (0 != (pnai.btSak & SAK_ISO18092_COMPLIANT))
                {
                    dst += "* Compliant with ISO/IEC 18092\r\n";
                }
                else
                {
                    dst += "* Not compliant with ISO/IEC 18092\r\n";
                }
            }
            if (0 != pnai.szAtsLen)
            {
                dst += "                ATS: ";
                dst += snprint_hex(pnai.abtAts, pnai.szAtsLen);
            }
            if (0 != pnai.szAtsLen && verbose)
            {
                // Decode ATS according to ISO/IEC 14443-4 (5.2 Answer to select)
                int[] iMaxFrameSizes = new int[9] { 16, 24, 32, 40, 48, 64, 96, 128, 256 };
                dst += "* Max Frame Size accepted by PICC: " + iMaxFrameSizes[pnai.abtAts[0] & 0x0F] + " bytes\r\n";

                int offset = 1;
                if (0 != (pnai.abtAts[0] & 0x10))
                { // TA(1) present
                    byte TA = pnai.abtAts[offset];
                    offset++;
                    dst += "* Bit Rate Capability:\r\n";
                    if (TA == 0)
                    {
                        dst += "  * PICC supports only 106 kbits/s in both directions\r\n";
                    }
                    if (0 != (TA & 1 << 7))
                    {
                        dst += "  * Same bitrate in both directions mandatory\r\n";
                    }
                    if (0 != (TA & 1 << 4))
                    {
                        dst += "  * PICC to PCD, DS=2, bitrate 212 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 5))
                    {
                        dst += "  * PICC to PCD, DS=4, bitrate 424 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 6))
                    {
                        dst += "  * PICC to PCD, DS=8, bitrate 847 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 0))
                    {
                        dst += "  * PCD to PICC, DR=2, bitrate 212 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 1))
                    {
                        dst += "  * PCD to PICC, DR=4, bitrate 424 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 2))
                    {
                        dst += "  * PCD to PICC, DR=8, bitrate 847 kbits/s supported\r\n";
                    }
                    if (0 != (TA & 1 << 3))
                    {
                        dst += "  * ERROR unknown value\r\n";
                    }
                }
                if (0 != (pnai.abtAts[0] & 0x20))
                { // TB(1) present
                    byte TB = pnai.abtAts[offset];
                    offset++;
                    dst += "* Frame Waiting Time:" + 256.0 * 16.0 * (1 << ((TB & 0xf0) >> 4)) / 13560.0 + " ms\r\n";
                    if ((TB & 0x0f) == 0)
                    {
                        dst += "* No Start-up Frame Guard Time required\r\n";
                    }
                    else
                    {
                        dst += "* Start-up Frame Guard Time: " + 256.0 * 16.0 * (1 << (TB & 0x0f)) / 13560.0 + " ms\r\n";
                    }
                }
                if (0 != (pnai.abtAts[0] & 0x40))
                { // TC(1) present
                    byte TC = pnai.abtAts[offset];
                    offset++;
                    if (0 != (TC & 0x1))
                    {
                        dst += "* Node Address supported\r\n";
                    }
                    else
                    {
                        dst += "* Node Address not supported\r\n";
                    }
                    if (0 != (TC & 0x2))
                    {
                        dst += "* Card IDentifier supported\r\n";
                    }
                    else
                    {
                        dst += "* Card IDentifier not supported\r\n";
                    }
                }
                if (pnai.szAtsLen > offset)
                {
                    dst += "* Historical bytes Tk: ";
                    dst += snprint_hex(pnai.abtAts, offset, (pnai.szAtsLen - offset));
                    byte CIB = pnai.abtAts[offset];
                    offset++;
                    if (CIB != 0x00 && CIB != 0x10 && (CIB & 0xf0) != 0x80)
                    {
                        dst += "  * Proprietary format\r\n";
                        if (CIB == 0xc1)
                        {
                            dst += "    * Tag byte: Mifare or virtual cards of various types\r\n";
                            byte L = pnai.abtAts[offset];
                            offset++;
                            if (L != (pnai.szAtsLen - offset))
                            {
                                dst += "    * Warning: Type Identification Coding length (" + (int)L + ")";
                                dst += " not matching Tk length (%" + (pnai.szAtsLen - offset) + ")\r\n";
                            }
                            if ((pnai.szAtsLen - offset - 2) > 0)
                            { // Omit 2 CRC bytes
                                byte CTC = pnai.abtAts[offset];
                                offset++;
                                dst += "    * Chip Type: ";
                                switch (CTC & 0xf0)
                                {
                                    case 0x00:
                                        dst += "(Multiple) Virtual Cards\r\n";
                                        break;
                                    case 0x10:
                                        dst += "Mifare DESFire\r\n";
                                        break;
                                    case 0x20:
                                        dst += "Mifare Plus\r\n";
                                        break;
                                    default:
                                        dst += "RFU\r\n";
                                        break;
                                }
                                dst += "    * Memory size: ";
                                switch (CTC & 0x0f)
                                {
                                    case 0x00:
                                        dst += "<1 kbyte\r\n";
                                        break;
                                    case 0x01:
                                        dst += "1 kbyte\r\n";
                                        break;
                                    case 0x02:
                                        dst += "2 kbyte\r\n";
                                        break;
                                    case 0x03:
                                        dst += "4 kbyte\r\n";
                                        break;
                                    case 0x04:
                                        dst += "8 kbyte\r\n";
                                        break;
                                    case 0x0f:
                                        dst += "Unspecified\r\n";
                                        break;
                                    default:
                                        dst += "RFU\r\n";
                                        break;
                                }
                            }
                            if ((pnai.szAtsLen - offset) > 0)
                            { // Omit 2 CRC bytes
                                byte CVC = pnai.abtAts[offset];
                                offset++;
                                dst += "    * Chip Status: ";
                                switch (CVC & 0xf0)
                                {
                                    case 0x00:
                                        dst += "Engineering sample\r\n";
                                        break;
                                    case 0x20:
                                        dst += "Released\r\n";
                                        break;
                                    default:
                                        dst += "RFU\r\n";
                                        break;
                                }
                                dst += "    * Chip Generation: ";
                                switch (CVC & 0x0f)
                                {
                                    case 0x00:
                                        dst += "Generation 1\r\n";
                                        break;
                                    case 0x01:
                                        dst += "Generation 2\r\n";
                                        break;
                                    case 0x02:
                                        dst += "Generation 3\r\n";
                                        break;
                                    case 0x0f:
                                        dst += "Unspecified\r\n";
                                        break;
                                    default:
                                        dst += "RFU\r\n";
                                        break;
                                }
                            }
                            if ((pnai.szAtsLen - offset) > 0)
                            { // Omit 2 CRC bytes
                                byte VCS = pnai.abtAts[offset];
                                offset++;
                                dst += "    * Specifics (Virtual Card Selection):\r\n";
                                if ((VCS & 0x09) == 0x00)
                                {
                                    dst += "      * Only VCSL supported\r\n";
                                }
                                else if ((VCS & 0x09) == 0x01)
                                {
                                    dst += "      * VCS, VCSL and SVC supported\r\n";
                                }
                                if ((VCS & 0x0e) == 0x00)
                                {
                                    dst += "      * SL1, SL2(?), SL3 supported\r\n";
                                }
                                else if ((VCS & 0x0e) == 0x02)
                                {
                                    dst += "      * SL3 only card\r\n";
                                }
                                else if ((VCS & 0x0f) == 0x0e)
                                {
                                    dst += "      * No VCS command supported\r\n";
                                }
                                else if ((VCS & 0x0f) == 0x0f)
                                {
                                    dst += "      * Unspecified\r\n";
                                }
                                else
                                {
                                    dst += "      * RFU\r\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (CIB == 0x00)
                        {
                            dst += "  * Tk after 0x00 consist of optional consecutive COMPACT-TLV data objects\r\n";
                            dst += "    followed by a mandatory status indicator (the last three bytes, not in TLV)\r\n";
                            dst += "    See ISO/IEC 7816-4 8.1.1.3 for more info\r\n";
                        }
                        if (CIB == 0x10)
                        {
                            dst += "  * DIR data reference: " + pnai.abtAts[offset].ToString("X2") + "\r\n";
                        }
                        if (CIB == 0x80)
                        {
                            if (pnai.szAtsLen == offset)
                            {
                                dst += "  * No COMPACT-TLV objects found, no status found\r\n";
                            }
                            else
                            {
                                dst += "  * Tk after 0x80 consist of optional consecutive COMPACT-TLV data objects;\r\n";
                                dst += "    the last data object may carry a status indicator of one, two or three bytes.\r\n";
                                dst += "    See ISO/IEC 7816-4 8.1.1.3 for more info\r\n";
                            }
                        }
                    }
                }
            }
            if (verbose)
            {
                dst += "\r\nFingerprinting based on MIFARE type Identification Procedure:\r\n"; // AN10833
                UInt16 atqa = 0;
                byte sak = 0;
                byte i, j;
                bool found_possible_match = false;

                atqa = (UInt16)((((UInt16)pnai.abtAtqa[0] & 0xff) << 8));
                atqa += (UInt16)((((UInt16)pnai.abtAtqa[1] & 0xff)));
                sak = (byte)(((byte)pnai.btSak & 0xff));

                for (i = 0; i < const_ca.Count; i++)
                {
                    if ((atqa & const_ca[i].mask) == const_ca[i].atqa)
                    {
                        for (j = 0; (j < const_ca[i].saklist.Length) && (const_ca[i].saklist[j] >= 0); j++)
                        {
                            int sakindex = const_ca[i].saklist[j];
                            if ((sak & const_cs[sakindex].mask) == const_cs[sakindex].sak)
                            {
                                dst += "* " + const_ca[i].type + const_cs[sakindex].type + "\r\n";
                                found_possible_match = true;
                            }
                        }
                    }
                }
                // Other matches not described in
                // AN10833 MIFARE Type Identification Procedure
                // but seen in the field:
                dst += "Other possible matches based on ATQA & SAK values:\r\n";
                UInt32 atqasak = 0;
                atqasak += (((UInt32)pnai.abtAtqa[0] & 0xff) << 16);
                atqasak += (((UInt32)pnai.abtAtqa[1] & 0xff) << 8);
                atqasak += ((UInt32)pnai.btSak & 0xff);
                switch (atqasak)
                {
                    case 0x000488:
                        dst += "* Mifare Classic 1K Infineon\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000298:
                        dst += "* Gemplus MPCOS\r\n";
                        found_possible_match = true;
                        break;
                    case 0x030428:
                        dst += "* JCOP31\r\n";
                        found_possible_match = true;
                        break;
                    case 0x004820:
                        dst += "* JCOP31 v2.4.1\r\n";
                        dst += "* JCOP31 v2.2\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000428:
                        dst += "* JCOP31 v2.3.1\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000453:
                        dst += "* Fudan FM1208SH01\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000820:
                        dst += "* Fudan FM1208\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000238:
                        dst += "* MFC 4K emulated by Nokia 6212 Classic\r\n";
                        found_possible_match = true;
                        break;
                    case 0x000838:
                        dst += "* MFC 4K emulated by Nokia 6131 NFC\r\n";
                        found_possible_match = true;
                        break;
                }
                if (!found_possible_match)
                {
                    dst += "* Unknown card, sorry\r\n";
                }
            }
        }

        public static void
        snprint_nfc_felica_info(out string dst, NFC.nfc_felica_info pnfi, bool verbose)
        {
            dst = "";
            dst += "        ID (NFCID2): ";
            dst += snprint_hex(pnfi.abtId, 8);
            dst += "    Parameter (PAD): ";
            dst += snprint_hex(pnfi.abtPad, 8);
            dst += "   System Code (SC): ";
            dst += snprint_hex(pnfi.abtSysCode, 2);
        }

        public static void
        snprint_nfc_jewel_info(out string dst, NFC.nfc_jewel_info pnji, bool verbose)
        {
            dst = "";
            dst += "    ATQA (SENS_RES): ";
            dst += snprint_hex(pnji.btSensRes, 2);
            dst += "      4-LSB JEWELID: ";
            dst += snprint_hex(pnji.btId, 4);
        }

        const byte PI_ISO14443_4_SUPPORTED = 0x01;
        const byte PI_NAD_SUPPORTED = 0x01;
        const byte PI_CID_SUPPORTED = 0x02;

        public static void
        snprint_nfc_iso14443b_info(out string dst, NFC.nfc_iso14443b_info pnbi, bool verbose)
        {
            dst = "";
            dst += "               PUPI: ";
            dst += snprint_hex(pnbi.abtPupi, 4);
            dst += "   Application Data: ";
            dst += snprint_hex(pnbi.abtApplicationData, 4);
            dst += "      Protocol Info: ";
            dst += snprint_hex(pnbi.abtProtocolInfo, 3);
            if (verbose)
            {
                dst += "* Bit Rate Capability:\r\n";
                if (pnbi.abtProtocolInfo[0] == 0)
                {
                    dst += " * PICC supports only 106 kbits/s in both directions\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 7))
                {
                    dst += " * Same bitrate in both directions mandatory\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 4))
                {
                    dst += " * PICC to PCD, 1etu=64/fc, bitrate 212 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 5))
                {
                    dst += " * PICC to PCD, 1etu=32/fc, bitrate 424 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 6))
                {
                    dst += " * PICC to PCD, 1etu=16/fc, bitrate 847 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 0))
                {
                    dst += " * PCD to PICC, 1etu=64/fc, bitrate 212 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 1))
                {
                    dst += " * PCD to PICC, 1etu=32/fc, bitrate 424 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 2))
                {
                    dst += " * PCD to PICC, 1etu=16/fc, bitrate 847 kbits/s supported\r\n";
                }
                if (0 != (pnbi.abtProtocolInfo[0] & 1 << 3))
                {
                    dst += " * ERROR unknown value\r\n";
                }
                if ((pnbi.abtProtocolInfo[1] & 0xf0) <= 0x80)
                {
                    int[] iMaxFrameSizes = { 16, 24, 32, 40, 48, 64, 96, 128, 256 };
                    dst += "* Maximum frame sizes: " + iMaxFrameSizes[((pnbi.abtProtocolInfo[1] & 0xf0) >> 4)] + " bytes\r\n";
                }
                if ((pnbi.abtProtocolInfo[1] & 0x01) == PI_ISO14443_4_SUPPORTED)
                {
                    // in principle low nibble could only be 0000 or 0001 and other values are RFU
                    // but in practice we found 0011 so let's use only last bit for -4 compatibility
                    dst += "* Protocol types supported: ISO/IEC 14443-4\r\n";
                }
                dst += "* Frame Waiting Time:" + 256.0 * 16.0 * (1 << ((pnbi.abtProtocolInfo[2] & 0xf0) >> 4)) / 13560.0 + " ms\r\n";
                if ((pnbi.abtProtocolInfo[2] & (PI_NAD_SUPPORTED | PI_CID_SUPPORTED)) != 0)
                {
                    dst += "* Frame options supported: ";
                    if ((pnbi.abtProtocolInfo[2] & PI_NAD_SUPPORTED) != 0) dst += "NAD ";
                    if ((pnbi.abtProtocolInfo[2] & PI_CID_SUPPORTED) != 0) dst += "CID ";
                    dst += "\r\n";
                }
            }
        }

        public static void
        snprint_nfc_iso14443bi_info(out string dst, NFC.nfc_iso14443bi_info pnii, bool verbose)
        {
            dst = "";
            dst += "                DIV: ";
            dst += snprint_hex(pnii.abtDIV, 4);
            if (verbose)
            {
                int version = (pnii.btVerLog & 0x1e) >> 1;
                dst += "   Software Version: ";
                if (version == 15)
                {
                    dst += "Undefined\r\n";
                }
                else
                {
                    dst += version + "\r\n";
                }

                if (0 != (pnii.btVerLog & 0x80) && 0 != (pnii.btConfig & 0x80))
                {
                    dst += "        Wait Enable: yes";
                }
            }
            if (0 != (pnii.btVerLog & 0x80) && 0 != (pnii.btConfig & 0x40))
            {
                dst += "                ATS: ";
                snprint_hex(pnii.abtAtr, pnii.szAtrLen);
            }
        }

        public static void
        snprint_nfc_iso14443b2sr_info(out string dst, NFC.nfc_iso14443b2sr_info pnsi, bool verbose)
        {
            dst = "";
            dst += "                UID: ";
            dst += snprint_hex(pnsi.abtUID, 8);
        }

        public static void
        snprint_nfc_iso14443b2ct_info(out string dst, NFC.nfc_iso14443b2ct_info pnci, bool verbose)
        {
            UInt32 uid;
            uid = (UInt32)((pnci.abtUID[3] << 24) + (pnci.abtUID[2] << 16) + (pnci.abtUID[1] << 8) + pnci.abtUID[0]);
            dst = "";
            dst += "                UID: ";
            dst += snprint_hex(pnci.abtUID, pnci.abtUID.Length);
            dst += "      UID (decimal): " + uid + "\r\n";
            dst += "       Product Code: " + pnci.btProdCode.ToString("2X") + "\r\n";
            dst += "           Fab Code: " + pnci.btFabCode.ToString("2X") + "\r\n";
        }

        public static void
        snprint_nfc_dep_info(out string dst, NFC.nfc_dep_info pndi, bool verbose)
        {
            dst = "";
            dst += "       NFCID3: ";
            dst += snprint_hex(pndi.abtNFCID3, 10);
            dst += "           BS: " + pndi.btBS.ToString("2X") + "\r\n";
            dst += "           BR: " + pndi.btBR.ToString("2X") + "\r\n";
            dst += "           TO: " + pndi.btTO.ToString("2X") + "\r\n";
            dst += "           PP: " + pndi.btPP.ToString("2X") + "\r\n";
            if (0 != (pndi.szGB))
            {
                dst += "General Bytes: ";
                snprint_hex(pndi.abtGB, pndi.szGB);
            }
        }

        public static void
        snprint_nfc_target(out string strdst, NFC.nfc_target pnt, bool verbose)
        {
            strdst = "";
            if (null != pnt)
            {
                strdst = "";
                strdst += NFC.str_nfc_modulation_type(pnt.nm.nmt) + " ";
                strdst += "(" + NFC.str_nfc_baud_rate(pnt.nm.nbr);
                strdst += (pnt.nm.nmt != NFC.nfc_modulation_type.NMT_DEP) ? "" : (pnt.nti.ndi.ndm == NFC.nfc_dep_mode.NDM_ACTIVE) ? "active mode" : "passive mode" + ")";
                strdst += " target:\r\n";
                string dst = "";
                switch (pnt.nm.nmt)
                {
                    case NFC.nfc_modulation_type.NMT_ISO14443A:
                        snprint_nfc_iso14443a_info(out  dst,  pnt.nti.nai, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_JEWEL:
                        snprint_nfc_jewel_info(out  dst,  pnt.nti.nji, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_FELICA:
                        snprint_nfc_felica_info(out  dst,  pnt.nti.nfi, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443B:
                        snprint_nfc_iso14443b_info(out  dst,  pnt.nti.nbi, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443BI:
                        snprint_nfc_iso14443bi_info(out  dst,  pnt.nti.nii, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443B2SR:
                        snprint_nfc_iso14443b2sr_info(out  dst,  pnt.nti.nsi, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_ISO14443B2CT:
                        snprint_nfc_iso14443b2ct_info(out  dst,  pnt.nti.nci, verbose);
                        break;
                    case NFC.nfc_modulation_type.NMT_DEP:
                        snprint_nfc_dep_info(out  dst,  pnt.nti.ndi, verbose);
                        break;
                }
                strdst += dst;
            }
        }

    }
}
