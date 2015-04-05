using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibNFC4CSharp.driver;

namespace LibNFC4CSharp
{
    class NFC : NFCInternal
    {
        ///pn532_uart is being used,  if multiple driver can be used, then use nfc-driver as abstract driver.
        
        /* Library initialization/deinitialization */
        /// <summary>
        /// nfc_init
        /// </summary>
        /// <param name="context"></param>
        public static void nfc_init(ref nfc_context context)
        {
            context = nfc_context_new();
            //TODO Init Driver
        }

        public static void nfc_exit(nfc_context context)
        {
            return;
        }
        public static int nfc_register_driver(nfc_driver driver)
        {
            //Todo  implements driver
            driver = pn532_uart.getInstance();
            return 0;
        }

        /* NFC Device/Hardware manipulation */
        /// <summary>
        /// nfc_open
        /// </summary>
        /// <param name="context"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static nfc_device nfc_open(nfc_context context, string port)
        {
            nfc_device dev = pn532_uart.getInstance().open(context, port);
            if (null == dev)
            {//log error handle
            }
            else
            {
            }
            return dev;
        }

        /// <summary>
        /// nfc_close
        /// </summary>
        /// <param name="pnd"></param>
        public static void nfc_close(nfc_device pnd)
        {
            pn532_uart.dispose();
            pnd.last_error = 0;
            pnd.driver.close(pnd);
        }

        /** @ingroup dev
         * @brief Abort current running command
         * @return Returns 0 on success, otherwise returns libnfc's error code.
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         *
         * Some commands (ie. nfc_target_init()) are blocking functions and will return only in particular conditions (ie. external initiator request).
         * This function attempt to abort the current running command.
         *
         * @note The blocking function (ie. nfc_target_init()) will failed with DEABORT error.
         */
        public static int nfc_abort_command(nfc_device pnd){
            pnd.last_error = 0;
            return pnd.driver.abort_command(pnd);
        }

        //public static int nfc_list_devices(nfc_context context, nfc_connstring connstrings[], int connpublic static strings_len){}

        /** @ingroup dev
         * @brief Turn NFC device in idle mode
         * @return Returns 0 on success, otherwise returns libnfc's error code.
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         *
         * This function switch the device in idle mode.
         * In initiator mode, the RF field is turned off and the device is set to low power mode (if avaible);
         * In target mode, the emulation is stoped (no target available from external initiator) and the device is set to low power mode (if avaible).
         */
        public static int nfc_idle(nfc_device pnd){
            pnd.last_error = 0;
            return pnd.driver.idle(pnd);
        }

        /* NFC initiator: act as "reader" */
        public static int nfc_initiator_init(nfc_device pnd){
            int res = 0;
            // Drop the field for a while
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACTIVATE_FIELD, false)) < 0)
                return res;
            // Enable field so more power consuming cards can power themselves up
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACTIVATE_FIELD, true)) < 0)
                return res;
            // Let the device try forever to find a target/tag
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_INFINITE_SELECT, true)) < 0)
                return res;
            // Activate auto ISO14443-4 switching by default
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_AUTO_ISO14443_4, true)) < 0)
                return res;
            // Force 14443-A mode
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_FORCE_ISO14443_A, true)) < 0)
                return res;
            // Force speed at 106kbps
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_FORCE_SPEED_106, true)) < 0)
                return res;
            // Disallow invalid frame
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACCEPT_INVALID_FRAMES, false)) < 0)
                return res;
            // Disallow multiple frames
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACCEPT_MULTIPLE_FRAMES, false)) < 0)
                return res;
            //HAL
            pnd.last_error = 0;
            return pnd.driver.initiator_init(pnd);
        }
        public static int nfc_initiator_init_secure_element(nfc_device pnd){
            pnd.last_error = 0;
            return pnd.driver.initiator_init_secure_element(pnd);
        }
        public static int nfc_initiator_select_passive_target(nfc_device pnd, nfc_modulation nm, byte[] pbtInitData, int szInitData,/*out*/ nfc_target pnt)
        {
            byte[] abtInit = new byte[Math.Max(12,pbtInitData.Length)];
            // byte abtTmpInit[MAX(12, szInitData)];
            int szInit = 0;
            if (pbtInitData == null || szInitData == 0)
            {
                // Provide default values, if any
                abtInit = prepare_initiator_data(nm);
                szInit = abtInit.Length;
            }
            else if (nm.nmt == nfc_modulation_type.NMT_ISO14443A)
            {
                //abtInit = abtTmpInit;
                iso14443_cascade_uid(pbtInitData, szInitData, ref abtInit, out szInit);
            }
            else
            {
                abtInit = pbtInitData;
                szInit = szInitData;
            }

            //HAL(initiator_select_passive_target, pnd, nm, abtInit, szInit, pnt);
            pnd.last_error = 0;
            return pnd.driver.initiator_select_passive_target(pnd, nm, abtInit, szInit, /*out*/ pnt);
        }

        public static int nfc_initiator_list_passive_targets(nfc_device pnd, nfc_modulation nm, ref nfc_target[] ant/*, int szTargets*/)
        {
            nfc_target nt = new nfc_target();
            int szTargetFound = 0;
            byte[] pbtInitData = null;
            //int szInitDataLen = 0;
            int res = 0;

            pnd.last_error = 0;

            // Let the reader only try once to find a tag
            bool bInfiniteSelect = pnd.bInfiniteSelect;
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_INFINITE_SELECT, false)) < 0)
            {
                return res;
            }

            pbtInitData = prepare_initiator_data(nm);
            //szInitDataLen = pbtInitData.Length;
            while (nfc_initiator_select_passive_target(pnd, nm, pbtInitData, pbtInitData.Length, /*out*/ nt) > 0)
            {
                int i;
                bool seen = false;
                // Check if we've already seen this tag
                for (i = 0; i < szTargetFound; i++)
                {
                    if (MiscTool.CompareType<nfc_target>(ant[i], nt))
                    {
                        seen = true;
                    }
                }
                if (seen)
                {
                    break;
                }

                ant[szTargetFound] = nt;
                szTargetFound++;
                if (ant.Length == szTargetFound)
                {
                    break;
                }
                nfc_initiator_deselect_target(pnd);
                // deselect has no effect on FeliCa and Jewel cards so we'll stop after one...
                // ISO/IEC 14443 B' cards are polled at 100% probability so it's not possible to detect correctly two cards at the same time
                if ((nm.nmt == nfc_modulation_type.NMT_FELICA)
                    || (nm.nmt == nfc_modulation_type.NMT_JEWEL)
                    || (nm.nmt == nfc_modulation_type.NMT_ISO14443BI)
                    || (nm.nmt == nfc_modulation_type.NMT_ISO14443B2SR)
                    || (nm.nmt == nfc_modulation_type.NMT_ISO14443B2CT))
                {
                    break;
                }
            }
            if (bInfiniteSelect)
            {
                if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_INFINITE_SELECT, true)) < 0)
                {
                    return res;
                }
            }
            return szTargetFound;
        }





        public static int nfc_initiator_poll_target(nfc_device pnd, nfc_modulation[] pnmTargetTypes, int szModulations, byte uiPollNr, byte uiPeriod, out nfc_target pnt)
        {
            //HAL(initiator_poll_target, pnd, pnmModulations, szModulations, uiPollNr, uiPeriod, pnt);
            pnd.last_error = 0;
            return pnd.driver.initiator_poll_target(pnd, pnmTargetTypes, szModulations, uiPollNr, uiPeriod, out pnt);
        }
/** @ingroup initiator
 * @brief Poll a target and request active or passive mode for D.E.P. (Data Exchange Protocol)
 * @return Returns selected D.E.P targets count on success, otherwise returns libnfc's error code (negative value).
 *
 * @param pnd \a nfc_device struct pointer that represent currently used device
 * @param ndm desired D.E.P. mode (\a NDM_ACTIVE or \a NDM_PASSIVE for active, respectively passive mode)
 * @param nbr desired baud rate
 * @param ndiInitiator pointer \a nfc_dep_info struct that contains \e NFCID3 and \e General \e Bytes to set to the initiator device (optionnal, can be \e NULL)
 * @param[out] pnt is a \a nfc_target struct pointer where target information will be put.
 * @param timeout in milliseconds
 *
 * The NFC device will try to find an available D.E.P. target. The standards
 * (ISO18092 and ECMA-340) describe the modulation that can be used for reader
 * to passive communications.
 *
 * @note \a nfc_dep_info will be returned when the target was acquired successfully.
 */
public static int
nfc_initiator_poll_dep_target( nfc_device pnd,nfc_dep_mode ndm,  nfc_baud_rate nbr,nfc_dep_info pndiInitiator,  nfc_target pnt, int timeout)
{
  const int period = 300;
  int remaining_time = timeout;
  int res;
  int result = 0;
  bool bInfiniteSelect = pnd.bInfiniteSelect;
  pnt = null;
  if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_INFINITE_SELECT, true)) < 0)
    return res;
  while (remaining_time > 0) {
    if ((res = nfc_initiator_select_dep_target(pnd, ndm, nbr, pndiInitiator,ref pnt, period)) < 0) {
      if (res != NFC_ETIMEOUT) {
        result = res;
        break;
      }
    }
    if (res == 1) {
      result = res;
      break;
    }
    remaining_time -= period;
  }
  if (! bInfiniteSelect) {
    if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_INFINITE_SELECT, false)) < 0) {
      return res;
    }
  }
  return result;
}
/** @ingroup initiator
 * @brief Select a target and request active or passive mode for D.E.P. (Data Exchange Protocol)
 * @return Returns selected D.E.P targets count on success, otherwise returns libnfc's error code (negative value).
 *
 * @param pnd \a nfc_device struct pointer that represent currently used device
 * @param ndm desired D.E.P. mode (\a NDM_ACTIVE or \a NDM_PASSIVE for active, respectively passive mode)
 * @param nbr desired baud rate
 * @param ndiInitiator pointer \a nfc_dep_info struct that contains \e NFCID3 and \e General \e Bytes to set to the initiator device (optionnal, can be \e NULL)
 * @param[out] pnt is a \a nfc_target struct pointer where target information will be put.
 * @param timeout in milliseconds
 *
 * The NFC device will try to find an available D.E.P. target. The standards
 * (ISO18092 and ECMA-340) describe the modulation that can be used for reader
 * to passive communications.
 *
 * @note \a nfc_dep_info will be returned when the target was acquired successfully.
 *
 * If timeout equals to 0, the function blocks indefinitely (until an error is raised or function is completed)
 * If timeout equals to -1, the default timeout will be used
 */
        public static int
        nfc_initiator_select_dep_target(nfc_device pnd, nfc_dep_mode ndm, nfc_baud_rate nbr, nfc_dep_info pndiInitiator, ref nfc_target pnt, int timeout)
        {
            pnd.last_error = 0;
            return pnd.driver.initiator_select_dep_target(pnd, ndm, nbr, pndiInitiator, ref pnt, timeout);
        }
        /** @ingroup initiator
         * @brief Deselect a selected passive or emulated tag
         * @return Returns 0 on success, otherwise returns libnfc's error code (negative value).
         * @param pnd \a nfc_device struct pointer that represents currently used device
         *
         * After selecting and communicating with a passive tag, this function could be
         * used to deactivate and release the tag. This is very useful when there are
         * multiple tags available in the field. It is possible to use the \fn
         * nfc_initiator_select_passive_target() function to select the first available
         * tag, test it for the available features and support, deselect it and skip to
         * the next tag until the correct tag is found.
         */
       public static int
        nfc_initiator_deselect_target(nfc_device pnd)
        {
            pnd.last_error = 0;
            return pnd.driver.initiator_deselect_target(pnd);
        }

        /** @ingroup initiator
 * @brief Send data to target then retrieve data from target
 * @return Returns received bytes count on success, otherwise returns libnfc's error code
 *
 * @param pnd \a nfc_device struct pointer that represents currently used device
 * @param pbtTx contains a byte array of the frame that needs to be transmitted.
 * @param szTx contains the length in bytes.
 * @param[out] pbtRx response from the target
 * @param szRx size of \a pbtRx (Will return NFC_EOVFLOW if RX exceeds this size)
 * @param timeout in milliseconds
 *
 * The NFC device (configured as initiator) will transmit the supplied bytes (\a pbtTx) to the target.
 * It waits for the response and stores the received bytes in the \a pbtRx byte array.
 *
 * If \a NP_EASY_FRAMING option is disabled the frames will sent and received in raw mode: \e PN53x will not handle input neither output data.
 *
 * The parity bits are handled by the \e PN53x chip. The CRC can be generated automatically or handled manually.
 * Using this function, frames can be communicated very fast via the NFC initiator to the tag.
 *
 * Tests show that on average this way of communicating is much faster than using the regular driver/middle-ware (often supplied by manufacturers).
 *
 * @warning The configuration option \a NP_HANDLE_PARITY must be set to \c true (the default value).
 *
 * @note When used with MIFARE Classic, NFC_EMFCAUTHFAIL error is returned if authentication command failed. You need to re-select the tag to operate with.
 *
 * If timeout equals to 0, the function blocks indefinitely (until an error is raised or function is completed)
 * If timeout equals to -1, the default timeout will be used
 */

        public static int nfc_initiator_transceive_bytes(nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, int timeout){
            pnd.last_error = 0;
            return  pnd.driver.initiator_transceive_bytes(pnd, pbtTx, szTx, pbtRx, szRx, timeout);
        }

/** @ingroup initiator
 * @brief Transceive raw bit-frames to a target
 * @return Returns received bits count on success, otherwise returns libnfc's error code
 *
 * @param pnd \a nfc_device struct pointer that represents currently used device
 * @param pbtTx contains a byte array of the frame that needs to be transmitted.
 * @param szTxBits contains the length in bits.
 *
 * @note For example the REQA (0x26) command (first anti-collision command of
 * ISO14443-A) must be precise 7 bits long. This is not possible by using
 * nfc_initiator_transceive_bytes(). With that function you can only
 * communicate frames that consist of full bytes. When you send a full byte (8
 * bits + 1 parity) with the value of REQA (0x26), a tag will simply not
 * respond. More information about this can be found in the anti-collision
 * example (\e nfc-anticol).
 *
 * @param pbtTxPar parameter contains a byte array of the corresponding parity bits needed to send per byte.
 *
 * @note For example if you send the SELECT_ALL (0x93, 0x20) = [ 10010011,
 * 00100000 ] command, you have to supply the following parity bytes (0x01,
 * 0x00) to define the correct odd parity bits. This is only an example to
 * explain how it works, if you just are sending two bytes with ISO14443-A
 * compliant parity bits you better can use the
 * nfc_initiator_transceive_bytes() function.
 *
 * @param[out] pbtRx response from the target
 * @param szRx size of \a pbtRx (Will return NFC_EOVFLOW if RX exceeds this size)
 * @param[out] pbtRxPar parameter contains a byte array of the corresponding parity bits
 *
 * The NFC device (configured as \e initiator) will transmit low-level messages
 * where only the modulation is handled by the \e PN53x chip. Construction of
 * the frame (data, CRC and parity) is completely done by libnfc.  This can be
 * very useful for testing purposes. Some protocols (e.g. MIFARE Classic)
 * require to violate the ISO14443-A standard by sending incorrect parity and
 * CRC bytes. Using this feature you are able to simulate these frames.
 */

        public static int nfc_initiator_transceive_bits(nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar,  byte[] pbtRx, int szRx,  byte[] pbtRxPar)
        {
            if (0 == szRx) throw new Exception("Invalid argument: szRx=0");
            pnd.last_error = 0;
            return pnd.driver.initiator_transceive_bits(pnd, pbtTx, szTxBits, pbtTxPar,  pbtRx, szRx,  pbtRxPar);
        }

        /** @ingroup initiator
         * @brief Send data to target then retrieve data from target
         * @return Returns received bytes count on success, otherwise returns libnfc's error code.
         *
         * @param pnd \a nfc_device struct pointer that represents currently used device
         * @param pbtTx contains a byte array of the frame that needs to be transmitted.
         * @param szTx contains the length in bytes.
         * @param[out] pbtRx response from the target
         * @param szRx size of \a pbtRx (Will return NFC_EOVFLOW if RX exceeds this size)
         *
         * This function is similar to nfc_initiator_transceive_bytes() with the following differences:
         * - A precise cycles counter will indicate the number of cycles between emission & reception of frames.
         * - It only supports mode with \a NP_EASY_FRAMING option disabled.
         * - Overall communication with the host is heavier and slower.
         *
         * Timer control:
         * By default timer configuration tries to maximize the precision, which also limits the maximum
         * cycles count before saturation/timeout.
         * E.g. with PN53x it can count up to 65535 cycles, so about 4.8ms, with a precision of about 73ns.
         * - If you're ok with the defaults, set *cycles = 0 before calling this function.
         * - If you need to count more cycles, set *cycles to the maximum you expect but don't forget
         *   you'll loose in precision and it'll take more time before timeout, so don't abuse!
         *
         * @warning The configuration option \a NP_EASY_FRAMING must be set to \c false.
         * @warning The configuration option \a NP_HANDLE_PARITY must be set to \c true (the default value).
         */
        public static int nfc_initiator_transceive_bytes_timed(nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, ref UInt32 cycles){

            pnd.last_error = 0;
            return pnd.driver.initiator_transceive_bytes_timed(pnd, pbtTx, szTx,  pbtRx, szRx,ref cycles);
        }

        /** @ingroup initiator
         * @brief Transceive raw bit-frames to a target
         * @return Returns received bits count on success, otherwise returns libnfc's error code
         *
         * This function is similar to nfc_initiator_transceive_bits() with the following differences:
         * - A precise cycles counter will indicate the number of cycles between emission & reception of frames.
         * - It only supports mode with \a NP_EASY_FRAMING option disabled and CRC must be handled manually.
         * - Overall communication with the host is heavier and slower.
         *
         * Timer control:
         * By default timer configuration tries to maximize the precision, which also limits the maximum
         * cycles count before saturation/timeout.
         * E.g. with PN53x it can count up to 65535 cycles, so about 4.8ms, with a precision of about 73ns.
         * - If you're ok with the defaults, set *cycles = 0 before calling this function.
         * - If you need to count more cycles, set *cycles to the maximum you expect but don't forget
         *   you'll loose in precision and it'll take more time before timeout, so don't abuse!
         *
         * @warning The configuration option \a NP_EASY_FRAMING must be set to \c false.
         * @warning The configuration option \a NP_HANDLE_CRC must be set to \c false.
         * @warning The configuration option \a NP_HANDLE_PARITY must be set to \c true (the default value).
         */
        public static int nfc_initiator_transceive_bits_timed(nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar, byte[] pbtRx, int szRx,  byte[] pbtRxPar, ref UInt32 cycles){
            if (0 == szRx) throw new Exception("Invalid argument: szRx=0");
            pnd.last_error = 0;
            return pnd.driver.initiator_transceive_bits_timed(pnd, pbtTx, szTxBits, pbtTxPar, pbtRx, szRx, pbtRxPar, ref cycles);
        }

        /** @ingroup initiator
         * @brief Check target presence
         * @return Returns 0 on success, otherwise returns libnfc's error code.
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pnt a \a nfc_target struct pointer where desired target information was stored (optionnal, can be \e NULL).
         * This function tests if \a nfc_target (or last selected tag if \e NULL) is currently present on NFC device.
         * @warning The target have to be selected before check its presence
         * @warning To run the test, one or more commands will be sent to target
        */
        public static int nfc_initiator_target_is_present(nfc_device pnd, nfc_target pnt){
            pnd.last_error = 0;
           return pnd.driver.initiator_target_is_present(pnd, pnt);
        }

        /* NFC target: act as tag (i.e. MIFARE Classic) or NFC target device. */

        /** @ingroup target
         * @brief Initialize NFC device as an emulated tag
         * @return Returns received bytes count on success, otherwise returns libnfc's error code
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pnt pointer to \a nfc_target struct that represents the wanted emulated target
         *
         * @note \a pnt can be updated by this function: if you set NBR_UNDEFINED
         * and/or NDM_UNDEFINED (ie. for DEP mode), these fields will be updated.
         *
         * @param[out] pbtRx Rx buffer pointer
         * @param[out] szRx received bytes count
         * @param timeout in milliseconds
         *
         * This function initializes NFC device in \e target mode in order to emulate a
         * tag using the specified \a nfc_target_mode_t.
         * - Crc is handled by the device (NP_HANDLE_CRC = true)
         * - Parity is handled the device (NP_HANDLE_PARITY = true)
         * - Cryto1 cipher is disabled (NP_ACTIVATE_CRYPTO1 = false)
         * - Auto-switching in ISO14443-4 mode is enabled (NP_AUTO_ISO14443_4 = true)
         * - Easy framing is disabled (NP_EASY_FRAMING = false)
         * - Invalid frames are not accepted (NP_ACCEPT_INVALID_FRAMES = false)
         * - Multiple frames are not accepted (NP_ACCEPT_MULTIPLE_FRAMES = false)
         * - RF field is dropped
         *
         * @warning Be aware that this function will wait (hang) until a command is
         * received that is not part of the anti-collision. The RATS command for
         * example would wake up the emulator. After this is received, the send and
         * receive functions can be used.
         *
         * If timeout equals to 0, the function blocks indefinitely (until an error is raised or function is completed)
         * If timeout equals to -1, the default timeout will be used
         */
        public static int nfc_target_init(nfc_device pnd, nfc_target pnt,ref byte[] pbtRx,  int szRx, int timeout){

            int res = 0;
            // Disallow invalid frame
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACCEPT_INVALID_FRAMES, false)) < 0)
                return res;
            // Disallow multiple frames
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACCEPT_MULTIPLE_FRAMES, false)) < 0)
                return res;
            // Make sure we reset the CRC and parity to chip handling.
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_HANDLE_CRC, true)) < 0)
                return res;
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_HANDLE_PARITY, true)) < 0)
                return res;
            // Activate auto ISO14443-4 switching by default
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_AUTO_ISO14443_4, true)) < 0)
                return res;
            // Activate "easy framing" feature by default
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_EASY_FRAMING, true)) < 0)
                return res;
            // Deactivate the CRYPTO1 cipher, it may could cause problems when still active
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACTIVATE_CRYPTO1, false)) < 0)
                return res;
            // Drop explicitely the field
            if ((res = nfc_device_set_property_bool(pnd, nfc_property.NP_ACTIVATE_FIELD, false)) < 0)
                return res;
            pnd.last_error = 0;
            return pnd.driver.target_init(pnd, pnt,ref pbtRx,  szRx, timeout);
        }

        /** @ingroup target
         * @brief Send bytes and APDU frames
         * @return Returns sent bytes count on success, otherwise returns libnfc's error code
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pbtTx pointer to Tx buffer
         * @param szTx size of Tx buffer
         * @param timeout in milliseconds
         *
         * This function make the NFC device (configured as \e target) send byte frames
         * (e.g. APDU responses) to the \e initiator.
         *
         * If timeout equals to 0, the function blocks indefinitely (until an error is raised or function is completed)
         * If timeout equals to -1, the default timeout will be used
         */
        public static int nfc_target_send_bytes(nfc_device pnd, byte[] pbtTx, int szTx, int timeout){
            pnd.last_error = 0;
            return pnd.driver.target_send_bytes(pnd, pbtTx, szTx, timeout);
        }

        /** @ingroup target
         * @brief Receive bytes and APDU frames
         * @return Returns received bytes count on success, otherwise returns libnfc's error code
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pbtRx pointer to Rx buffer
         * @param szRx size of Rx buffer
         * @param timeout in milliseconds
         *
         * This function retrieves bytes frames (e.g. ADPU) sent by the \e initiator to the NFC device (configured as \e target).
         *
         * If timeout equals to 0, the function blocks indefinitely (until an error is raised or function is completed)
         * If timeout equals to -1, the default timeout will be used
         */
        public static int nfc_target_receive_bytes(nfc_device pnd,ref byte[] pbtRx, int szRx, int timeout){
            pnd.last_error = 0;
            return pnd.driver.target_receive_bytes(pnd, ref pbtRx, szRx, timeout);
        }

        /** @ingroup target
         * @brief Send raw bit-frames
         * @return Returns sent bits count on success, otherwise returns libnfc's error code.
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pbtTx pointer to Tx buffer
         * @param szTxBits size of Tx buffer
         * @param pbtTxPar parameter contains a byte array of the corresponding parity bits needed to send per byte.
         * This function can be used to transmit (raw) bit-frames to the \e initiator
         * using the specified NFC device (configured as \e target).
         */
        public static int nfc_target_send_bits(nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar){
            pnd.last_error = 0;
            return pnd.driver.target_send_bits(pnd, pbtTx, szTxBits, pbtTxPar);
        }

        /** @ingroup target
         * @brief Receive bit-frames
         * @return Returns received bits count on success, otherwise returns libnfc's error code
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param pbtRx pointer to Rx buffer
         * @param szRx size of Rx buffer
         * @param[out] pbtRxPar parameter contains a byte array of the corresponding parity bits
         *
         * This function makes it possible to receive (raw) bit-frames.  It returns all
         * the messages that are stored in the FIFO buffer of the \e PN53x chip.  It
         * does not require to send any frame and thereby could be used to snoop frames
         * that are transmitted by a nearby \e initiator.  @note Check out the
         * NP_ACCEPT_MULTIPLE_FRAMES configuration option to avoid losing transmitted
         * frames.
         */
        public static int nfc_target_receive_bits(nfc_device pnd, ref byte[] pbtRx, int szRx, byte[] pbtRxPar){
            pnd.last_error = 0;
            return pnd.driver.target_receive_bits(pnd,ref pbtRx, szRx, pbtRxPar);
        }
    
        /* Error reporting */

        /** @ingroup error
         * @brief Return the last error string
         * @return Returns a string
         *
         * @param pnd \a nfc_device struct pointer that represent currently used device
         */
        public static string nfc_strerror(nfc_device pnd){
            return NFC.ErrorMessage(pnd.last_error);
        }
        public static int nfc_strerror_r(nfc_device pnd, string buf, int buflen) { return 1; }
        public static void nfc_perror(nfc_device pnd, string s){}
        public static int nfc_device_get_last_error(nfc_device pnd) { return pnd.last_error; }

        /* Special data accessors */
        public static string nfc_device_get_name(nfc_device pnd) { return pnd.name; }
        public static string nfc_device_get_connstring(nfc_device pnd) { return pnd.connstring; }

        /** @ingroup data
         * @brief Get supported modulations.
         * @return Returns 0 on success, otherwise returns libnfc's error code (negative value)
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param mode \a nfc_mode.
         * @param supported_mt pointer of \a nfc_modulation_type array.
         *
         */
        public static int nfc_device_get_supported_modulation(nfc_device pnd, nfc_mode mode,out nfc_modulation_type[] supported_mt){
            pnd.last_error = 0;
            return pnd.driver.get_supported_modulation(pnd, mode, out supported_mt);
        }

        /** @ingroup data
         * @brief Get supported baud rates.
         * @return Returns 0 on success, otherwise returns libnfc's error code (negative value)
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param nmt \a nfc_modulation_type.
         * @param supported_br pointer of \a nfc_baud_rate array.
         *
         */
        public static int nfc_device_get_supported_baud_rate(nfc_device pnd, nfc_modulation_type nmt,out nfc_baud_rate[] supported_br){
            pnd.last_error = 0;
            return pnd.driver.get_supported_baud_rate(pnd, nmt, out supported_br);
        }

        /* Properties accessors */
        /** @ingroup properties
         * @brief Set a device's integer-property value
         * @return Returns 0 on success, otherwise returns libnfc's error code (negative value)
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param property \a nfc_property which will be set
         * @param value integer value
         *
         * Sets integer property.
         *
         * @see nfc_property enum values
         */
        public static int nfc_device_set_property_int(nfc_device pnd, nfc_property property, int value){
            pnd.last_error = 0;
            return pnd.driver.device_set_property_int(pnd, property, value);
        }

        /** @ingroup properties
         * @brief Set a device's boolean-property value
         * @return Returns 0 on success, otherwise returns libnfc's error code (negative value)
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param property \a nfc_property which will be set
         * @param bEnable boolean to activate/disactivate the property
         *
         * Configures parameters and registers that control for example timing,
         * modulation, frame and error handling.  There are different categories for
         * configuring the \e PN53X chip features (handle, activate, infinite and
         * accept).
         */
        public static int nfc_device_set_property_bool(nfc_device pnd, nfc_property property, bool bEnable){
            pnd.last_error = 0;
            return pnd.driver.device_set_property_bool(pnd, property, bEnable);
        }

        /* Misc. functions 
        public static void iso14443a_crc(byte[]  pbtData, int szLen, byte[]  pbtCrc){}
        public static void iso14443a_crc_append(byte[]  pbtData, int szLen){}
        public static void iso14443b_crc(byte[]  pbtData, int szLen, byte[]  pbtCrc){}
        public static void iso14443b_crc_append(byte[]  pbtData, int szLen){}
        //byte[]  iso14443a_locate_historical_bytes(byte[]  pbtAts, int szAts, out int pszTk){}
        */
        //public static void nfc_free(public static void *p){}
        public static string nfc_version() { return "1.7.1"; }

        /** @ingroup misc
         * @brief Print information about NFC device
         * @return Upon successful return, this function returns the number of characters printed (excluding the null byte used to end output to strings), otherwise returns libnfc's error code (negative value)
         * @param pnd \a nfc_device struct pointer that represent currently used device
         * @param buf pointer where string will be allocated, then information printed
         *
         * @warning *buf must be freed using nfc_free()
         */
        public static int nfc_device_get_information_about(nfc_device pnd, out string buf){
            pnd.last_error = 0;
            return pnd.driver.device_get_information_about(pnd, out buf);
        }

        /* public static string converter functions */
        public static string str_nfc_modulation_type(nfc_modulation_type nmt){

            switch (nmt)
            {
                case nfc_modulation_type.NMT_ISO14443A:
                    return "ISO/IEC 14443A";
                    
                case nfc_modulation_type.NMT_ISO14443B:
                    return "ISO/IEC 14443-4B";
                    
                case nfc_modulation_type.NMT_ISO14443BI:
                    return "ISO/IEC 14443-4B'";
                    
                case nfc_modulation_type.NMT_ISO14443B2CT:
                    return "ISO/IEC 14443-2B ASK CTx";
                    
                case nfc_modulation_type.NMT_ISO14443B2SR:
                    return "ISO/IEC 14443-2B ST SRx";
                    
                case nfc_modulation_type.NMT_FELICA:
                    return "FeliCa";
                    
                case nfc_modulation_type.NMT_JEWEL:
                    return "Innovision Jewel";
                    
                case nfc_modulation_type.NMT_DEP:
                    return "D.E.P.";
                    
                default :
                    return "Unknown modulation";
            }
        }
        public static string str_nfc_baud_rate(nfc_baud_rate nbr){

            switch (nbr)
            {
                case nfc_baud_rate.NBR_UNDEFINED:
                    return "undefined baud rate";
                    
                case nfc_baud_rate.NBR_106:
                    return "106 kbps";
                    
                case nfc_baud_rate.NBR_212:
                    return "212 kbps";
                    
                case nfc_baud_rate.NBR_424:
                    return "424 kbps";
                    
                case nfc_baud_rate.NBR_847:
                    return "847 kbps";
                    
                default:
                    return "Unkwown baud rate";
            }
        }
        //public static int str_nfc_target(out string buf, nfc_target pnt, bool verbose){}

    }
}
