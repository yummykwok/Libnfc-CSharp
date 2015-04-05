using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp.nfc.utils
{
    class nfc_mfclassic
    {


        private static NFC.nfc_context context;
        private static NFC.nfc_device pnd;
        private static NFC.nfc_target nt;
        private static Mifare.mifare_param mp;
        private static Mifare.mifare_classic_tag mtKeys;
        private static Mifare.mifare_classic_tag mtDump;
        private static bool bUseKeyA;
        private static bool bUseKeyFile;
        private static bool bForceKeyFile;
        private static bool bTolerateFailures;
        private static bool bFormatCard;
        private static bool magic2 = false;
        private static byte uiBlocks;
        private static byte[] keys = {
  0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
  0xd3, 0xf7, 0xd3, 0xf7, 0xd3, 0xf7,
  0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5,
  0xb0, 0xb1, 0xb2, 0xb3, 0xb4, 0xb5,
  0x4d, 0x3a, 0x99, 0xc3, 0x51, 0xdd,
  0x1a, 0x98, 0x2c, 0x7e, 0x45, 0x9a,
  0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0xab, 0xcd, 0xef, 0x12, 0x34, 0x56
};
        private static byte[] default_key = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        private static byte[] default_acl = { 0xff, 0x07, 0x80, 0x69 };

        private static NFC.nfc_modulation nmMifare;

        private static int num_keys;

        const int MAX_FRAME_LEN = 264;

        private static byte[] abtRx;//[MAX_FRAME_LEN];
        private static int szRxBits;

        static byte[] abtHalt = { 0x50, 0x00, 0x00, 0x00 };

        // special unlock command
        static byte[] abtUnlock1 = { 0x40 };
        static byte[] abtUnlock2 = { 0x43 };
        static nfc_mfclassic()
        {
            //Const values
            nmMifare.nbr = NFCInternal.nfc_baud_rate.NBR_106;
            nmMifare.nmt = NFCInternal.nfc_modulation_type.NMT_ISO14443A;
            num_keys = keys.Length;
            abtRx = new byte[MAX_FRAME_LEN];
        }
        private static bool
        transmit_bits(byte[] pbtTx, int szTxBits)//const const
        {
            // Show transmitted command
            //printf("Sent bits:     ");
            //print_hex_bits(pbtTx, szTxBits);
            // Transmit the bit frame command, we don't use the arbitrary parity feature
            if ((szRxBits = NFC.nfc_initiator_transceive_bits(pnd, pbtTx, szTxBits, null, abtRx, abtRx.Length, null)) < 0)
                return false;

            // Show received answer
            //printf("Received bits: ");
            //print_hex_bits(abtRx, szRxBits);
            // Succesful transfer
            return true;
        }


        private static bool
        transmit_bytes(byte[] pbtTx, int szTx)
        {
            // Show transmitted command
            //printf("Sent bits:     ");
            //print_hex(pbtTx, szTx);
            // Transmit the command bytes
            int res;
            if ((res = NFC.nfc_initiator_transceive_bytes(pnd, pbtTx, szTx, abtRx, abtRx.Length, 0)) < 0)
                return false;

            // Show received answer
            //printf("Received bits: ");
            //print_hex(abtRx, res);
            // Succesful transfer
            return true;
        }

        private static void
        print_success_or_failure(bool bFailure, ref uint uiBlockCounter)
        {
            //printf("%c", (bFailure) ? 'x' : '.');
            if (uiBlockCounter != 0 && !bFailure)
                uiBlockCounter += 1;
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



        public static bool
        authenticate(uint uiBlock)
        {
            Mifare.mifare_cmd mc;
            uint uiTrailerBlock;

            // Set the authentication information (uid)
            MiscTool.memcpy(mp.mpa.abtAuthUid, 0, nt.nti.nai.abtUid, nt.nti.nai.szUidLen - 4, 4);

            // Should we use key A or B?
            mc = (bUseKeyA) ? Mifare.mifare_cmd.MC_AUTH_A : Mifare.mifare_cmd.MC_AUTH_B;

            // Key file authentication.
            if (bUseKeyFile)
            {

                // Locate the trailer (with the keys) used for this sector
                uiTrailerBlock = get_trailer_block(uiBlock);

                // Extract the right key from dump file
                if (bUseKeyA)
                    MiscTool.memcpy(mp.mpa.abtKey, 0, mtKeys.amb[uiTrailerBlock].mbt.abtKeyA, 0, 6);
                else
                    MiscTool.memcpy(mp.mpa.abtKey, 0, mtKeys.amb[uiTrailerBlock].mbt.abtKeyB, 0, 6);

                // Try to authenticate for the current sector
                if (Mifare.nfc_initiator_mifare_cmd(pnd, mc, (byte)uiBlock, mp))
                    return true;
            }

            // If formatting or not using key file, try to guess the right key
            if (bFormatCard || !bUseKeyFile)
            {
                for (int key_index = 0; key_index < num_keys; key_index++)
                {
                    MiscTool.memcpy(mp.mpa.abtKey, 0, keys, (key_index * 6), 6);
                    if (Mifare.nfc_initiator_mifare_cmd(pnd, mc, (byte)uiBlock, mp))
                    {
                        if (bUseKeyA)
                            MiscTool.memcpy(mtKeys.amb[uiBlock].mbt.abtKeyA, 0, mp.mpa.abtKey, 0, 6);
                        else
                            MiscTool.memcpy(mtKeys.amb[uiBlock].mbt.abtKeyB, 0, mp.mpa.abtKey, 0, 6);
                        return true;
                    }
                    if (NFC.nfc_initiator_select_passive_target(pnd, nmMifare, nt.nti.nai.abtUid, nt.nti.nai.szUidLen, null) <= 0)
                    {
                        //ERR("tag was removed");
                        return false;
                    }
                }
            }

            return false;
        }

        public static bool
        unlock_card()
        {
            /*
            if (magic2)
            {
                //printf("Don't use R/W with this card, this is not required!\n");
                return false;
            }*/

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
            transmit_bytes(abtHalt, 4);
            // now send unlock
            if (!transmit_bits(abtUnlock1, 7))
            {
                //printf("unlock failure!\n");
                return false;
            }
            if (!transmit_bytes(abtUnlock2, 1))
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



        public static bool
        read_card(int read_unlocked)
        {
            uint iBlock;
            bool bFailure = false;
            uint uiReadBlocks = 0;

            if (read_unlocked != 0)
                if (!unlock_card())
                    return false;

            //printf("Reading out %d blocks |", uiBlocks + 1);
            // Read the card from end to begin
            for (iBlock = uiBlocks; iBlock >= 0; iBlock--)
            {
                // Authenticate everytime we reach a trailer block
                if (is_trailer_block(iBlock))
                {
                    if (bFailure)
                    {
                        // When a failure occured we need to redo the anti-collision
                        if (NFC.nfc_initiator_select_passive_target(pnd, nmMifare, null, 0, nt) <= 0)
                        {
                            //printf("!\nError: tag was removed\n");
                            return false;
                        }
                        bFailure = false;
                    }

                    //fflush(stdout);

                    // Try to authenticate for the current sector
                    if (0 == read_unlocked && !authenticate(iBlock))
                    {
                        //printf("!\nError: authentication failed for block 0x%02x\n", iBlock);
                        return false;
                    }
                    // Try to read out the trailer
                    if (Mifare.nfc_initiator_mifare_cmd(pnd, Mifare.mifare_cmd.MC_READ, (byte)iBlock, mp))
                    {
                        if (0 != read_unlocked)
                        {
                            MiscTool.memcpy(mtDump.amb[iBlock].mbd.abtData, 0, mp.mpd.abtData, 0, 16);
                        }
                        else
                        {
                            // Copy the keys over from our key dump and store the retrieved access bits
                            MiscTool.memcpy(mtDump.amb[iBlock].mbt.abtKeyA, 0, mtKeys.amb[iBlock].mbt.abtKeyA, 0, 6);
                            MiscTool.memcpy(mtDump.amb[iBlock].mbt.abtAccessBits, 0, mp.mpd.abtData, 6, 4);
                            MiscTool.memcpy(mtDump.amb[iBlock].mbt.abtKeyB, 0, mtKeys.amb[iBlock].mbt.abtKeyB, 0, 6);
                        }
                    }
                    else
                    {
                        //printf("!\nfailed to read trailer block 0x%02x\n", iBlock);
                        bFailure = true;
                    }
                }
                else
                {
                    // Make sure a earlier readout did not fail
                    if (!bFailure)
                    {
                        // Try to read out the data block
                        if (Mifare.nfc_initiator_mifare_cmd(pnd, Mifare.mifare_cmd.MC_READ, (byte)iBlock, mp))
                        {
                            MiscTool.memcpy(mtDump.amb[iBlock].mbd.abtData, 0, mp.mpd.abtData, 0, 16);
                        }
                        else
                        {
                            //printf("!\nError: unable to read block 0x%02x\n", iBlock);
                            bFailure = true;
                        }
                    }
                }
                // Show if the readout went well for each block
                print_success_or_failure(bFailure, ref uiReadBlocks);
                if ((!bTolerateFailures) && bFailure)
                    return false;
            }
            //printf("|\n");
            //printf("Done, %d of %d blocks read.\n", uiReadBlocks, uiBlocks + 1);
            //fflush(stdout);

            return true;
        }

        public static bool
        write_card(int write_block_zero)
        {
            uint uiBlock;
            bool bFailure = false;
            uint uiWriteBlocks = 0;

            if (write_block_zero != 0)
                if (!unlock_card())
                    return false;

            //printf("Writing %d blocks |", uiBlocks + 1);
            // Write the card from begin to end;
            for (uiBlock = 0; uiBlock <= uiBlocks; uiBlock++)
            {
                // Authenticate everytime we reach the first sector of a new block
                if (is_first_block(uiBlock))
                {
                    if (bFailure)
                    {
                        // When a failure occured we need to redo the anti-collision
                        if (NFC.nfc_initiator_select_passive_target(pnd, nmMifare, null, 0, nt) <= 0)
                        {
                            //printf("!\nError: tag was removed\n");
                            return false;
                        }
                        bFailure = false;
                    }

                    //fflush(stdout);

                    // Try to authenticate for the current sector
                    if (0 == write_block_zero && !authenticate(uiBlock))
                    {
                        //printf("!\nError: authentication failed for block %02x\n", uiBlock);
                        return false;
                    }
                }

                if (is_trailer_block(uiBlock))
                {
                    if (bFormatCard)
                    {
                        // Copy the default key and reset the access bits
                        MiscTool.memcpy(mp.mpd.abtData, 0, default_key, 0, 6);
                        MiscTool.memcpy(mp.mpd.abtData, 6, default_acl, 0, 4);
                        MiscTool.memcpy(mp.mpd.abtData, 10, default_key, 0, 6);
                    }
                    else
                    {
                        // Copy the keys over from our key dump and store the retrieved access bits
                        MiscTool.memcpy(mp.mpd.abtData, 0, mtDump.amb[uiBlock].mbt.abtKeyA, 0, 6);
                        MiscTool.memcpy(mp.mpd.abtData, 6, mtDump.amb[uiBlock].mbt.abtAccessBits, 0, 4);
                        MiscTool.memcpy(mp.mpd.abtData, 10, mtDump.amb[uiBlock].mbt.abtKeyB, 0, 6);
                    }

                    // Try to write the trailer
                    if (Mifare.nfc_initiator_mifare_cmd(pnd, Mifare.mifare_cmd.MC_WRITE, (byte)uiBlock, mp) == false)
                    {
                        //printf("failed to write trailer block %d \n", uiBlock);
                        bFailure = true;
                    }
                }
                else
                {
                    // The first block 0x00 is read only, skip this
                    if (uiBlock == 0 && 0 == write_block_zero && !magic2)
                        continue;


                    // Make sure a earlier write did not fail
                    if (!bFailure)
                    {
                        // Try to write the data block
                        if (bFormatCard && uiBlock != 0)
                            MiscTool.memset(mp.mpd.abtData, 0x00, 16);
                        else
                            MiscTool.memcpy(mp.mpd.abtData, 0, mtDump.amb[uiBlock].mbd.abtData, 0, 16);
                        // do not write a block 0 with incorrect BCC - card will be made invalid!
                        if (uiBlock == 0)
                        {
                            if ((mp.mpd.abtData[0] ^ mp.mpd.abtData[1] ^ mp.mpd.abtData[2] ^ mp.mpd.abtData[3] ^ mp.mpd.abtData[4]) != 0x00 && !magic2)
                            {
                                //printf("!\nError: incorrect BCC in MFD file!\n");
                                //printf("Expecting BCC=%02X\n", mp.mpd.abtData[0] ^ mp.mpd.abtData[1] ^ mp.mpd.abtData[2] ^ mp.mpd.abtData[3]);
                                return false;
                            }
                        }
                        if (!Mifare.nfc_initiator_mifare_cmd(pnd, Mifare.mifare_cmd.MC_WRITE, (byte)uiBlock, mp))
                            bFailure = true;
                    }
                }
                // Show if the write went well for each block
                print_success_or_failure(bFailure, ref uiWriteBlocks);
                if ((!bTolerateFailures) && bFailure)
                    return false;
            }
            //printf("|\n");
            //printf("Done, %d of %d blocks written.\n", uiWriteBlocks, uiBlocks + 1);
            //fflush(stdout);

            return true;
        }



        /*
        private static void
        print_usage(const char *pcProgramName)
        {
          //printf("Usage: ");
          //printf("%s f|r|R|w|W a|b <dump.mfd> [<keys.mfd> [f]]\n", pcProgramName);
          //printf("  f|r|R|w|W     - Perform format (f) or read from (r) or unlocked read from (R) or write to (w) or unlocked write to (W) card\n");
          //printf("                  *** format will reset all keys to FFFFFFFFFFFF and all data to 00 and all ACLs to default\n");
          //printf("                  *** unlocked read does not require authentication and will reveal A and B keys\n");
          //printf("                  *** note that unlocked write will attempt to overwrite block 0 including UID\n");
          //printf("                  *** unlocking only works with special Mifare 1K cards (Chinese clones)\n");
          //printf("  a|A|b|B       - Use A or B keys for action; Halt on errors (a|b) or tolerate errors (A|B)\n");
          //printf("  <dump.mfd>    - MiFare Dump (MFD) used to write (card to MFD) or (MFD to card)\n");
          //printf("  <keys.mfd>    - MiFare Dump (MFD) that contain the keys (optional)\n");
          //printf("  f             - Force using the keyfile even if UID does not match (optional)\n");
          //printf("Examples: \n\n");
          //printf("  Read card to file, using key A:\n\n");
          //printf("    %s r a mycard.mfd\n\n", pcProgramName);
          //printf("  Write file to blank card, using key A:\n\n");
          //printf("    %s w a mycard.mfd\n\n", pcProgramName);
          //printf("  Write new data and/or keys to previously written card, using key A:\n\n");
          //printf("    %s w a newdata.mfd mycard.mfd\n\n", pcProgramName);
          //printf("  Format/wipe card (note two passes required to ensure writes for all ACL cases):\n\n");
          //printf("    %s f A dummy.mfd keyfile.mfd f\n", pcProgramName);
          //printf("    %s f B dummy.mfd keyfile.mfd f\n\n", pcProgramName);
        }
        */

        /*
        int
        main(int argc, const char *argv[])
        {
          action_t atAction = ACTION_USAGE;
          byte *pbtUID;
          int    unlock = 0;

          if (argc < 2) {
            print_usage(argv[0]);
            exit(EXIT_FAILURE);
          }
          const char *command = argv[1];

          if (strcmp(command, "r") == 0 || strcmp(command, "R") == 0) {
            if (argc < 4) {
              print_usage(argv[0]);
              exit(EXIT_FAILURE);
            }
            atAction = ACTION_READ;
            if (strcmp(command, "R") == 0)
              unlock = 1;
            bUseKeyA = tolower((int)((unsigned char) * (argv[2]))) == 'a';
            bTolerateFailures = tolower((int)((unsigned char) * (argv[2]))) != (int)((unsigned char) * (argv[2]));
            bUseKeyFile = (argc > 4);
            bForceKeyFile = ((argc > 5) && (strcmp((char *)argv[5], "f") == 0));
          } else if (strcmp(command, "w") == 0 || strcmp(command, "W") == 0 || strcmp(command, "f") == 0) {
            if (argc < 4) {
              print_usage(argv[0]);
              exit(EXIT_FAILURE);
            }
            atAction = ACTION_WRITE;
            if (strcmp(command, "W") == 0)
              unlock = 1;
            bFormatCard = (strcmp(command, "f") == 0);
            bUseKeyA = tolower((int)((unsigned char) * (argv[2]))) == 'a';
            bTolerateFailures = tolower((int)((unsigned char) * (argv[2]))) != (int)((unsigned char) * (argv[2]));
            bUseKeyFile = (argc > 4);
            bForceKeyFile = ((argc > 5) && (strcmp((char *)argv[5], "f") == 0));
          }

          if (atAction == ACTION_USAGE) {
            print_usage(argv[0]);
            exit(EXIT_FAILURE);
          }
          // We don't know yet the card size so let's read only the UID from the keyfile for the moment
          if (bUseKeyFile) {
            FILE *pfKeys = fopen(argv[4], "rb");
            if (pfKeys == null) {
              //printf("Could not open keys file: %s\n", argv[4]);
              exit(EXIT_FAILURE);
            }
            if (fread(&mtKeys, 1, 4, pfKeys) != 4) {
              //printf("Could not read UID from key file: %s\n", argv[4]);
              fclose(pfKeys);
              exit(EXIT_FAILURE);
            }
            fclose(pfKeys);
          }
          NFC.nfc_init(&context);
          if (context == null) {
            ERR("Unable to init libnfc (malloc)");
            exit(EXIT_FAILURE);
          }

        // Try to open the NFC reader
          pnd = NFC.nfc_open(context, null);
          if (pnd == null) {
            ERR("Error opening NFC reader");
            NFC.nfc_exit(context);
            exit(EXIT_FAILURE);
          }

          if (nfc_initiator_init(pnd) < 0) {
            NFC.nfc_perror(pnd, "nfc_initiator_init");
            NFC.nfc_close(pnd);
            NFC.nfc_exit(context);
            exit(EXIT_FAILURE);
          };

        // Let the reader only try once to find a tag
          if (nfc_device_set_property_bool(pnd, NP_INFINITE_SELECT, false) < 0) {
            NFC.nfc_perror(pnd, "nfc_device_set_property_bool");
            NFC.nfc_close(pnd);
            NFC.nfc_exit(context);
            exit(EXIT_FAILURE);
          }
        // Disable ISO14443-4 switching in order to read devices that emulate Mifare Classic with ISO14443-4 compliance.
          if (nfc_device_set_property_bool(pnd, NP_AUTO_ISO14443_4, false) < 0) {
            NFC.nfc_perror(pnd, "nfc_device_set_property_bool");
            NFC.nfc_close(pnd);
            NFC.nfc_exit(context);
            exit(EXIT_FAILURE);
          }

          //printf("NFC reader: %s opened\n", NFC.nfc_device_get_name(pnd));

        // Try to find a MIFARE Classic tag
          if (nfc_initiator_select_passive_target(pnd, nmMifare, null, 0, &nt) <= 0) {
            //printf("Error: no tag was found\n");
            NFC.nfc_close(pnd);
            NFC.nfc_exit(context);
            exit(EXIT_FAILURE);
          }
        // Test if we are dealing with a MIFARE compatible tag
          if ((nt.nti.nai.btSak & 0x08) == 0) {
            //printf("Warning: tag is probably not a MFC!\n");
          }

        // Get the info from the current tag
          pbtUID = nt.nti.nai.abtUid;

          if (bUseKeyFile) {
            byte fileUid[4];
            memcpy(fileUid, mtKeys.amb[0].mbm.abtUID, 4);
        // Compare if key dump UID is the same as the current tag UID, at least for the first 4 bytes
            if (memcmp(pbtUID, fileUid, 4) != 0) {
              //printf("Expected MIFARE Classic card with UID starting as: %02x%02x%02x%02x\n",
                     fileUid[0], fileUid[1], fileUid[2], fileUid[3]);
              //printf("Got card with UID starting as:                     %02x%02x%02x%02x\n",
                     pbtUID[0], pbtUID[1], pbtUID[2], pbtUID[3]);
              if (! bForceKeyFile) {
                //printf("Aborting!\n");
                NFC.nfc_close(pnd);
                NFC.nfc_exit(context);
                exit(EXIT_FAILURE);
              }
            }
          }
          //printf("Found MIFARE Classic card:\n");
          print_nfc_target(&nt, false);

        // Guessing size
          if ((nt.nti.nai.abtAtqa[1] & 0x02) == 0x02)
        // 4K
            uiBlocks = 0xff;
          else if ((nt.nti.nai.btSak & 0x01) == 0x01)
        // 320b
            uiBlocks = 0x13;
          else
        // 1K/2K, checked through RATS
            uiBlocks = 0x3f;
        // Testing RATS
          int res;
          if ((res = get_rats()) > 0) {
            if ((res >= 10) && (abtRx[5] == 0xc1) && (abtRx[6] == 0x05)
                && (abtRx[7] == 0x2f) && (abtRx[8] == 0x2f)
                && ((nt.nti.nai.abtAtqa[1] & 0x02) == 0x00)) {
              // MIFARE Plus 2K
              uiBlocks = 0x7f;
            }
            // Chinese magic emulation card, ATS=0978009102:dabc1910
            if ((res == 9)  && (abtRx[5] == 0xda) && (abtRx[6] == 0xbc)
                && (abtRx[7] == 0x19) && (abtRx[8] == 0x10)) {
              magic2 = true;
            }
          }
          //printf("Guessing size: seems to be a %i-byte card\n", (uiBlocks + 1) * 16);

          if (bUseKeyFile) {
            FILE *pfKeys = fopen(argv[4], "rb");
            if (pfKeys == null) {
              //printf("Could not open keys file: %s\n", argv[4]);
              exit(EXIT_FAILURE);
            }
            if (fread(&mtKeys, 1, (uiBlocks + 1) * sizeof(mifare_classic_block), pfKeys) != (uiBlocks + 1) * sizeof(mifare_classic_block)) {
              //printf("Could not read keys file: %s\n", argv[4]);
              fclose(pfKeys);
              exit(EXIT_FAILURE);
            }
            fclose(pfKeys);
          }

          if (atAction == ACTION_READ) {
            memset(&mtDump, 0x00, sizeof(mtDump));
          } else {
            FILE *pfDump = fopen(argv[3], "rb");

            if (pfDump == null) {
              //printf("Could not open dump file: %s\n", argv[3]);
              exit(EXIT_FAILURE);

            }

            if (fread(&mtDump, 1, (uiBlocks + 1) * sizeof(mifare_classic_block), pfDump) != (uiBlocks + 1) * sizeof(mifare_classic_block)) {
              //printf("Could not read dump file: %s\n", argv[3]);
              fclose(pfDump);
              exit(EXIT_FAILURE);
            }
            fclose(pfDump);
          }
        // //printf("Successfully opened required files\n");

          if (atAction == ACTION_READ) {
            if (read_card(unlock)) {
              //printf("Writing data to file: %s ...", argv[3]);
              fflush(stdout);
              FILE *pfDump = fopen(argv[3], "wb");
              if (pfDump == null) {
                //printf("Could not open dump file: %s\n", argv[3]);
                NFC.nfc_close(pnd);
                NFC.nfc_exit(context);
                exit(EXIT_FAILURE);
              }
              if (fwrite(&mtDump, 1, (uiBlocks + 1) * sizeof(mifare_classic_block), pfDump) != ((uiBlocks + 1) * sizeof(mifare_classic_block))) {
                //printf("\nCould not write to file: %s\n", argv[3]);
                fclose(pfDump);
                NFC.nfc_close(pnd);
                NFC.nfc_exit(context);
                exit(EXIT_FAILURE);
              }
              //printf("Done.\n");
              fclose(pfDump);
            }
          } else if (atAction == ACTION_WRITE) {
            write_card(unlock);
          }

          NFC.nfc_close(pnd);
          NFC.nfc_exit(context);
          exit(EXIT_SUCCESS);
        }*/

    }
}
