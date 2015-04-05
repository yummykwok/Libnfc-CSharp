using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp.driver
{
    interface nfc_driver
    {
        string name { get;  }
        NFC.scan_type_enum scan_type { get;  }
        int scan(NFC.nfc_context context, string[] connstrings, int connstrings_len);
        NFC.nfc_device open(NFC.nfc_context context, string connstring);
        void close(NFC.nfc_device pnd);
        /*[[ pn53x_io */
        int send(NFC.nfc_device pnd, byte[] pbtData, int szData, int timeout);
        int receive(NFC.nfc_device pnd, ref byte[] pbtData, int szDataLen, int timeout);
        /*]] pn53x_io*/
        string strerror(NFC.nfc_device pnd);

        int initiator_init(NFC.nfc_device pnd);
        int initiator_init_secure_element(NFC.nfc_device pnd);
        int initiator_select_passive_target(NFC.nfc_device pnd, NFC.nfc_modulation nm, byte[] pbtInitData, int szInitData, /*out*/ NFC.nfc_target pnt);
        int initiator_poll_target(NFC.nfc_device pnd, NFC.nfc_modulation[] pnmModulations, int szModulations, byte uiPollNr, byte btPeriod, out NFC.nfc_target pnt);
        int initiator_select_dep_target(NFC.nfc_device pnd, NFC.nfc_dep_mode ndm, NFC.nfc_baud_rate nbr, NFC.nfc_dep_info pndiInitiator, ref NFC.nfc_target pnt, int timeout);
        int initiator_deselect_target(NFC.nfc_device pnd);
        int initiator_transceive_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, int timeout);
        int initiator_transceive_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar, byte[] pbtRx, int szRx, byte[] pbtRxPar);
        int initiator_transceive_bytes_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTx, byte[] pbtRx, int szRx, ref UInt32 cycles);
        int initiator_transceive_bits_timed(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar, byte[] pbtRx, int szRx, byte[] pbtRxPar, ref UInt32 cycles);
        int initiator_target_is_present(NFC.nfc_device pnd, NFC.nfc_target pnt);
        int target_init(NFC.nfc_device pnd, NFC.nfc_target pnt, ref byte[] pbtRx, int szRx, int timeout);
        int target_send_bytes(NFC.nfc_device pnd, byte[] pbtTx, int szTx, int timeout);
        int target_receive_bytes(NFC.nfc_device pnd, ref byte[] pbtRx, int szRxLen, int timeout);
        int target_send_bits(NFC.nfc_device pnd, byte[] pbtTx, int szTxBits, byte[] pbtTxPar);
        int target_receive_bits(NFC.nfc_device pnd, ref byte[] pbtRx, int szRxLen, byte[] pbtRxPar);
        int device_set_property_bool(NFC.nfc_device pnd, NFC.nfc_property property, bool bEnable);
        int device_set_property_int(NFC.nfc_device pnd, NFC.nfc_property property, int value);
        int get_supported_modulation(NFC.nfc_device pnd, NFC.nfc_mode mode, out NFC.nfc_modulation_type[] supported_mt);
        int get_supported_baud_rate(NFC.nfc_device pnd, NFC.nfc_modulation_type nmt, out NFC.nfc_baud_rate[] supported_br);
        int device_get_information_about(NFC.nfc_device pnd, out string info);

        int abort_command(NFC.nfc_device pnd);
        int idle(NFC.nfc_device pnd);
        int powerdown(NFC.nfc_device pnd);
    }
}
