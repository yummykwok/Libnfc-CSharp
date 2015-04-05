using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp.chips
{
    abstract class pn53x_internal
    {

        // Miscellaneous
        public const byte Diagnose = 0x00;
        public const byte GetFirmwareVersion = 0x02;
        public const byte GetGeneralStatus = 0x04;
        public const byte ReadRegister = 0x06;
        public const byte WriteRegister = 0x08;
        public const byte ReadGPIO = 0x0C;
        public const byte WriteGPIO = 0x0E;
        public const byte SetSerialBaudRate = 0x10;
        public const byte SetParameters = 0x12;
        public const byte SAMConfiguration = 0x14;
        public const byte PowerDown = 0x16;
        public const byte AlparCommandForTDA = 0x18;
        // RC-S360 has another command 0x18 for reset &..?

        // RF communication
        public const byte RFConfiguration = 0x32;
        public const byte RFRegulationTest = 0x58;

        // Initiator
        public const byte InJumpForDEP = 0x56;
        public const byte InJumpForPSL = 0x46;
        public const byte InListPassiveTarget = 0x4A;
        public const byte InATR = 0x50;
        public const byte InPSL = 0x4E;
        public const byte InDataExchange = 0x40;
        public const byte InCommunicateThru = 0x42;
        public const byte InQuartetByteExchange = 0x38;
        public const byte InDeselect = 0x44;
        public const byte InRelease = 0x52;
        public const byte InSelect = 0x54;
        public const byte InActivateDeactivatePaypass = 0x48;
        public const byte InAutoPoll = 0x60;

        // Target
        public const byte TgInitAsTarget = 0x8C;
        public const byte TgSetGeneralBytes = 0x92;
        public const byte TgGetData = 0x86;
        public const byte TgSetData = 0x8E;
        public const byte TgSetDataSecure = 0x96;
        public const byte TgSetMetaData = 0x94;
        public const byte TgSetMetaDataSecure = 0x98;
        public const byte TgGetInitiatorCommand = 0x88;
        public const byte TgResponseToInitiator = 0x90;
        public const byte TgGetTargetStatus = 0x8A;

        /** @note PN53x's normal frame:
         *
         *   .-- Start
         *   |   .-- Packet length
         *   |   |  .-- Length checksum
         *   |   |  |  .-- Direction (D4 Host to PN, D5 PN to Host)
         *   |   |  |  |  .-- Code
         *   |   |  |  |  |  .-- Packet checksum
         *   |   |  |  |  |  |  .-- Postamble
         *   V   |  |  |  |  |  |
         * ----- V  V  V  V  V  V
         * 00 FF 02 FE D4 02 2A 00
         */

        /** @note PN53x's extended frame:
         *
         *   .-- Start
         *   |     .-- Fixed to FF to enable extended frame
         *   |     |     .-- Packet length
         *   |     |     |   .-- Length checksum
         *   |     |     |   |  .-- Direction (D4 Host to PN, D5 PN to Host)
         *   |     |     |   |  |  .-- Code
         *   |     |     |   |  |  |  .-- Packet checksum
         *   |     |     |   |  |  |  |  .-- Postamble
         *   V     V     V   |  |  |  |  |
         * ----- ----- ----- V  V  V  V  V
         * 00 FF FF FF 00 02 FE D4 02 2A 00
         */

        /**
         * Start bytes, packet length, length checksum, direction, packet checksum and postamble are overhead
         */
        // The TFI is considered part of the overhead
        public const int PN53x_NORMAL_FRAME__DATA_MAX_LEN = 254;
        public const int PN53x_NORMAL_FRAME__OVERHEAD = 8;
        public const int PN53x_EXTENDED_FRAME__DATA_MAX_LEN = 264;
        public const int PN53x_EXTENDED_FRAME__OVERHEAD = 11;
        public const int PN53x_ACK_FRAME__LEN = 6;

        public struct pn53x_command
        {
            byte ui8Code;
            byte ui8CompatFlags;
            string abtCommandText;
        } ;

        public class pn53x_type
        {
            public const byte PN53X = 0x00; // Unknown PN53x chip type
            public const byte PN531 = 0x01;
            public const byte PN532 = 0x02;
            public const byte PN533 = 0x04;
            public const byte RCS360 = 0x08;
        } ;

        public static bool is_cmd_supported(byte chip_type, byte cmd)
        {
            return (get_compat_flags(cmd) & chip_type)!=0 ;
        }

        public static byte get_compat_flags(byte cmd)
        {
            switch (cmd)
            {
                // Miscellaneous
                case Diagnose: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case GetFirmwareVersion: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case GetGeneralStatus: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case ReadRegister: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case WriteRegister: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case ReadGPIO: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case WriteGPIO: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case SetSerialBaudRate: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case SetParameters: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case SAMConfiguration: return pn53x_type.PN531 | pn53x_type.PN532;
                case PowerDown: return pn53x_type.PN531 | pn53x_type.PN532;
                case AlparCommandForTDA: return pn53x_type.PN533 | pn53x_type.RCS360;    // Has another usage on RC-S360...

                // RF communication
                case RFConfiguration: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case RFRegulationTest: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;

                // Initiator
                case InJumpForDEP: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case InJumpForPSL: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case InListPassiveTarget: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case InATR: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case InPSL: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case InDataExchange: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case InCommunicateThru: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case InQuartetByteExchange: return pn53x_type.PN533;
                case InDeselect: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case InRelease: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533 | pn53x_type.RCS360;
                case InSelect: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case InAutoPoll: return pn53x_type.PN532;
                case InActivateDeactivatePaypass: return pn53x_type.PN533;

                // Target
                case TgInitAsTarget: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgSetGeneralBytes: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgGetData: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgSetData: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgSetDataSecure: return pn53x_type.PN533;
                case TgSetMetaData: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgSetMetaDataSecure: return pn53x_type.PN533;
                case TgGetInitiatorCommand: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgResponseToInitiator: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                case TgGetTargetStatus: return pn53x_type.PN531 | pn53x_type.PN532 | pn53x_type.PN533;
                default: return 0x00;
            }
        }
        // SFR part
        public int _BV(int x) { return 1 << x; }


        public const int P30 = 0;
        public const int P31 = 1;
        public const int P32 = 2;
        public const int P33 = 3;
        public const int P34 = 4;
        public const int P35 = 5;

        // Registers part

        struct pn53x_register
        {
            public UInt16 ui16Address;
            public string abtRegisterText;
            public string abtRegisterDescription;
        } ;




        // Register addresses
        public const int PN53X_REG_Control_switch_rng = 0x6106;
        public const int PN53X_REG_CIU_Mode = 0x6301;
        public const int PN53X_REG_CIU_TxMode = 0x6302;
        public const int PN53X_REG_CIU_RxMode = 0x6303;
        public const int PN53X_REG_CIU_TxControl = 0x6304;
        public const int PN53X_REG_CIU_TxAuto = 0x6305;
        public const int PN53X_REG_CIU_TxSel = 0x6306;
        public const int PN53X_REG_CIU_RxSel = 0x6307;
        public const int PN53X_REG_CIU_RxThreshold = 0x6308;
        public const int PN53X_REG_CIU_Demod = 0x6309;
        public const int PN53X_REG_CIU_FelNFC1 = 0x630A;
        public const int PN53X_REG_CIU_FelNFC2 = 0x630B;
        public const int PN53X_REG_CIU_MifNFC = 0x630C;
        public const int PN53X_REG_CIU_ManualRCV = 0x630D;
        public const int PN53X_REG_CIU_TypeB = 0x630E;
        // public const int  PN53X_REG_- 0x630F
        // public const int  PN53X_REG_- 0x6310
        public const int PN53X_REG_CIU_CRCResultMSB = 0x6311;
        public const int PN53X_REG_CIU_CRCResultLSB = 0x6312;
        public const int PN53X_REG_CIU_GsNOFF = 0x6313;
        public const int PN53X_REG_CIU_ModWidth = 0x6314;
        public const int PN53X_REG_CIU_TxBitPhase = 0x6315;
        public const int PN53X_REG_CIU_RFCfg = 0x6316;
        public const int PN53X_REG_CIU_GsNOn = 0x6317;
        public const int PN53X_REG_CIU_CWGsP = 0x6318;
        public const int PN53X_REG_CIU_ModGsP = 0x6319;
        public const int PN53X_REG_CIU_TMode = 0x631A;
        public const int PN53X_REG_CIU_TPrescaler = 0x631B;
        public const int PN53X_REG_CIU_TReloadVal_hi = 0x631C;
        public const int PN53X_REG_CIU_TReloadVal_lo = 0x631D;
        public const int PN53X_REG_CIU_TCounterVal_hi = 0x631E;
        public const int PN53X_REG_CIU_TCounterVal_lo = 0x631F;
        // public const int  PN53X_REG_- 0x6320
        public const int PN53X_REG_CIU_TestSel1 = 0x6321;
        public const int PN53X_REG_CIU_TestSel2 = 0x6322;
        public const int PN53X_REG_CIU_TestPinEn = 0x6323;
        public const int PN53X_REG_CIU_TestPinValue = 0x6324;
        public const int PN53X_REG_CIU_TestBus = 0x6325;
        public const int PN53X_REG_CIU_AutoTest = 0x6326;
        public const int PN53X_REG_CIU_Version = 0x6327;
        public const int PN53X_REG_CIU_AnalogTest = 0x6328;
        public const int PN53X_REG_CIU_TestDAC1 = 0x6329;
        public const int PN53X_REG_CIU_TestDAC2 = 0x632A;
        public const int PN53X_REG_CIU_TestADC = 0x632B;
        // public const int  PN53X_REG_- 0x632C
        // public const int  PN53X_REG_- 0x632D
        // public const int  PN53X_REG_- 0x632E
        public const int PN53X_REG_CIU_RFlevelDet = 0x632F;
        public const int PN53X_REG_CIU_SIC_CLK_en = 0x6330;
        public const int PN53X_REG_CIU_Command = 0x6331;
        public const int PN53X_REG_CIU_CommIEn = 0x6332;
        public const int PN53X_REG_CIU_DivIEn = 0x6333;
        public const int PN53X_REG_CIU_CommIrq = 0x6334;
        public const int PN53X_REG_CIU_DivIrq = 0x6335;
        public const int PN53X_REG_CIU_Error = 0x6336;
        public const int PN53X_REG_CIU_Status1 = 0x6337;
        public const int PN53X_REG_CIU_Status2 = 0x6338;
        public const int PN53X_REG_CIU_FIFOData = 0x6339;
        public const int PN53X_REG_CIU_FIFOLevel = 0x633A;
        public const int PN53X_REG_CIU_WaterLevel = 0x633B;
        public const int PN53X_REG_CIU_Control = 0x633C;
        public const int PN53X_REG_CIU_BitFraming = 0x633D;
        public const int PN53X_REG_CIU_Coll = 0x633E;

        public const int PN53X_SFR_P3 = 0xFFB0;

        public const int PN53X_SFR_P3CFGA = 0xFFFC;
        public const int PN53X_SFR_P3CFGB = 0xFFFD;
        public const int PN53X_SFR_P7CFGA = 0xFFF4;
        public const int PN53X_SFR_P7CFGB = 0xFFF5;
        public const int PN53X_SFR_P7 = 0xFFF7;

        /* PN53x specific errors */
        public const int ETIMEOUT = 0x01;
        public const int ECRC = 0x02;
        public const int EPARITY = 0x03;
        public const int EBITCOUNT = 0x04;
        public const int EFRAMING = 0x05;
        public const int EBITCOLL = 0x06;
        public const int ESMALLBUF = 0x07;
        public const int EBUFOVF = 0x09;
        public const int ERFTIMEOUT = 0x0a;
        public const int ERFPROTO = 0x0b;
        public const int EOVHEAT = 0x0d;
        public const int EINBUFOVF = 0x0e;
        public const int EINVPARAM = 0x10;
        public const int EDEPUNKCMD = 0x12;
        public const int EINVRXFRAM = 0x13;
        public const int EMFAUTH = 0x14;
        public const int ENSECNOTSUPP = 0x18;	// PN533 only
        public const int EBCC = 0x23;
        public const int EDEPINVSTATE = 0x25;
        public const int EOPNOTALL = 0x26;
        public const int ECMD = 0x27;
        public const int ETGREL = 0x29;
        public const int ECID = 0x2a;
        public const int ECDISCARDED = 0x2b;
        public const int ENFCID3 = 0x2c;
        public const int EOVCURRENT = 0x2d;
        public const int ENAD = 0x2e;

        public static string get_register_text(UInt16 ui16Address)
        {
            switch (ui16Address)
            {
                case PN53X_REG_CIU_Mode: return "Defines general modes for transmitting and receiving";
                case PN53X_REG_CIU_TxMode: return "Defines the transmission data rate and framing during transmission";
                case PN53X_REG_CIU_RxMode: return "Defines the transmission data rate and framing during receiving";
                case PN53X_REG_CIU_TxControl: return "Controls the logical behaviour of the antenna driver pins TX1 and TX2";
                case PN53X_REG_CIU_TxAuto: return "Controls the settings of the antenna driver";
                case PN53X_REG_CIU_TxSel: return "Selects the internal sources for the antenna driver";
                case PN53X_REG_CIU_RxSel: return "Selects internal receiver settings";
                case PN53X_REG_CIU_RxThreshold: return "Selects thresholds for the bit decoder";
                case PN53X_REG_CIU_Demod: return "Defines demodulator settings";
                case PN53X_REG_CIU_FelNFC1: return "Defines the length of the valid range for the received frame";
                case PN53X_REG_CIU_FelNFC2: return "Defines the length of the valid range for the received frame";
                case PN53X_REG_CIU_MifNFC: return "Controls the communication in ISO/IEC 14443/MIFARE and NFC target mode at 106 kbit/s";
                case PN53X_REG_CIU_ManualRCV: return "Allows manual fine tuning of the internal receiver";
                case PN53X_REG_CIU_TypeB: return "Configure the ISO/IEC 14443 type B";
                //  PNREG (PN53X_REG_-: return "Reserved";
                //  PNREG (PN53X_REG_-: return "Reserved";
                case PN53X_REG_CIU_CRCResultMSB: return "Shows the actual MSB values of the CRC calculation";
                case PN53X_REG_CIU_CRCResultLSB: return "Shows the actual LSB values of the CRC calculation";
                case PN53X_REG_CIU_GsNOFF: return "Selects the conductance of the antenna driver pins TX1 and TX2 for load modulation when own RF field is switched OFF";
                case PN53X_REG_CIU_ModWidth: return "Controls the setting of the width of the Miller pause";
                case PN53X_REG_CIU_TxBitPhase: return "Bit synchronization at 106 kbit/s";
                case PN53X_REG_CIU_RFCfg: return "Configures the receiver gain and RF level";
                case PN53X_REG_CIU_GsNOn: return "Selects the conductance of the antenna driver pins TX1 and TX2 for modulation: return when own RF field is switched ON";
                case PN53X_REG_CIU_CWGsP: return "Selects the conductance of the antenna driver pins TX1 and TX2 when not in modulation phase";
                case PN53X_REG_CIU_ModGsP: return "Selects the conductance of the antenna driver pins TX1 and TX2 when in modulation phase";
                case PN53X_REG_CIU_TMode: return "Defines settings for the internal timer";
                case PN53X_REG_CIU_TPrescaler: return "Defines settings for the internal timer";
                case PN53X_REG_CIU_TReloadVal_hi: return "Describes the 16-bit long timer reload value (Higher 8 bits)";
                case PN53X_REG_CIU_TReloadVal_lo: return "Describes the 16-bit long timer reload value (Lower 8 bits)";
                case PN53X_REG_CIU_TCounterVal_hi: return "Describes the 16-bit long timer actual value (Higher 8 bits)";
                case PN53X_REG_CIU_TCounterVal_lo: return "Describes the 16-bit long timer actual value (Lower 8 bits)";
                //  PNREG (PN53X_REG_-: return "Reserved";
                case PN53X_REG_CIU_TestSel1: return "General test signals configuration";
                case PN53X_REG_CIU_TestSel2: return "General test signals configuration and PRBS control";
                case PN53X_REG_CIU_TestPinEn: return "Enables test signals output on pins.";
                case PN53X_REG_CIU_TestPinValue: return "Defines the values for the 8-bit parallel bus when it is used as I/O bus";
                case PN53X_REG_CIU_TestBus: return "Shows the status of the internal test bus";
                case PN53X_REG_CIU_AutoTest: return "Controls the digital self-test";
                case PN53X_REG_CIU_Version: return "Shows the CIU version";
                case PN53X_REG_CIU_AnalogTest: return "Controls the pins AUX1 and AUX2";
                case PN53X_REG_CIU_TestDAC1: return "Defines the test value for the TestDAC1";
                case PN53X_REG_CIU_TestDAC2: return "Defines the test value for the TestDAC2";
                case PN53X_REG_CIU_TestADC: return "Show the actual value of ADC I and Q";
                //  PNREG (PN53X_REG_-: return "Reserved for tests";
                //  PNREG (PN53X_REG_-: return "Reserved for tests";
                //  PNREG (PN53X_REG_-: return "Reserved for tests";
                case PN53X_REG_CIU_RFlevelDet: return "Power down of the RF level detector";
                case PN53X_REG_CIU_SIC_CLK_en: return "Enables the use of secure IC clock on P34 / SIC_CLK";
                case PN53X_REG_CIU_Command: return "Starts and stops the command execution";
                case PN53X_REG_CIU_CommIEn: return "Control bits to enable and disable the passing of interrupt requests";
                case PN53X_REG_CIU_DivIEn: return "Controls bits to enable and disable the passing of interrupt requests";
                case PN53X_REG_CIU_CommIrq: return "Contains common CIU interrupt request flags";
                case PN53X_REG_CIU_DivIrq: return "Contains miscellaneous interrupt request flags";
                case PN53X_REG_CIU_Error: return "Error flags showing the error status of the last command executed";
                case PN53X_REG_CIU_Status1: return "Contains status flags of the CRC: return Interrupt Request System and FIFO buffer";
                case PN53X_REG_CIU_Status2: return "Contain status flags of the receiver: return transmitter and Data Mode Detector";
                case PN53X_REG_CIU_FIFOData: return "In- and output of 64 byte FIFO buffer";
                case PN53X_REG_CIU_FIFOLevel: return "Indicates the number of bytes stored in the FIFO";
                case PN53X_REG_CIU_WaterLevel: return "Defines the thresholds for FIFO under- and overflow warning";
                case PN53X_REG_CIU_Control: return "Contains miscellaneous control bits";
                case PN53X_REG_CIU_BitFraming: return "Adjustments for bit oriented frames";
                case PN53X_REG_CIU_Coll: return "Defines the first bit collision detected on the RF interface";

                // SFR
                case PN53X_SFR_P3CFGA: return "Port 3 configuration";
                case PN53X_SFR_P3CFGB: return "Port 3 configuration";
                case PN53X_SFR_P3: return "Port 3 value";
                case PN53X_SFR_P7CFGA: return "Port 7 configuration";
                case PN53X_SFR_P7CFGB: return "Port 7 configuration";
                case PN53X_SFR_P7: return "Port 7 value";
                default: return "Unkwown register";
            }
        }
    }
}
