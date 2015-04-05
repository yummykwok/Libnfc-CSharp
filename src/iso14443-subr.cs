using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp
{
    class ISO14443Subr
    {
        /**
 * @brief CRC_A
 *
 */
        public static void
        iso14443a_crc(byte[] pbtData, int szLen, byte[] pbtCrc)
        {
            byte bt;
            UInt32 wCrc = 0x6363;
            int offset = 0;
            do
            {
                bt = pbtData[offset++];
                bt = (byte)(bt ^ (byte)(wCrc & 0x00FF));
                bt = (byte)(bt ^ (bt << 4));
                wCrc = (wCrc >> 8) ^ ((UInt32)bt << 8) ^ ((UInt32)bt << 3) ^ ((UInt32)bt >> 4);
            } while ((--szLen) != 0);

            pbtCrc[0] = (byte)(wCrc & 0xFF);
            pbtCrc[1] = (byte)((wCrc >> 8) & 0xFF);
        }

        /**
         * @brief Append CRC_A
         *
         */
        public static void
        iso14443a_crc_append(byte[] pbtData, int szLen)
        {
            byte[] crc = new byte[2];
            iso14443a_crc(pbtData, szLen, crc);
            pbtData[szLen] = crc[0];
            pbtData[szLen + 1] = crc[1];
        }

        /**
         * @brief CRC_B
         *
         */
        public static void
        iso14443b_crc(byte[] pbtData, int szLen, byte[] pbtCrc)
        {
            byte bt;
            UInt32 wCrc = 0xFFFF;
            int offset = 0;
            do
            {
                bt = pbtData[offset++];
                bt = (byte)(bt ^ (byte)(wCrc & 0x00FF));
                bt = (byte)(bt ^ (bt << 4));
                wCrc = (wCrc >> 8) ^ ((UInt32)bt << 8) ^ ((UInt32)bt << 3) ^ ((UInt32)bt >> 4);
            } while ((--szLen) != 0);
            wCrc = ~wCrc;
            pbtCrc[0] = (byte)(wCrc & 0xFF);
            pbtCrc[1] = (byte)((wCrc >> 8) & 0xFF);
        }

        /**
         * @brief Append CRC_B
         *
         */
        public static void
        iso14443b_crc_append(byte[] pbtData, int szLen)
        {
            byte[] crc = new byte[2];

            iso14443b_crc(pbtData, szLen, crc);
            pbtData[szLen] = crc[0];
            pbtData[szLen + 1] = crc[1];
        }

        /**
         * @brief Locate historical bytes
         * @see ISO/IEC 14443-4 (5.2.7 Historical bytes)
         */
        public static byte[]
        iso14443a_locate_historical_bytes(byte[] pbtAts, int szAts, out int pszTk)
        {
            if (szAts != 0)
            {
                int offset = 1;
                if (0 != (pbtAts[0] & 0x10))
                { // TA
                    offset++;
                }
                if (0 != (pbtAts[0] & 0x20))
                { // TB
                    offset++;
                }
                if (0 != (pbtAts[0] & 0x40))
                { // TC
                    offset++;
                }
                if (szAts > offset)
                {
                    pszTk = (szAts - offset);
                    return MiscTool.SubBytes(pbtAts, offset);
                }
            }
            pszTk = 0;
            return null;
        }

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

    }
}
