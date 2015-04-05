using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibNFC4CSharp.driver
{
    abstract class nfc_io
    {
        abstract public int send(NFC.nfc_device pnd, byte[] pbtData, int szData, int timeout);
        abstract public int receive(NFC.nfc_device pnd, out byte[] pbtData, int szDataLen, int timeout);
    }
}
