// Based on the EEPROM from Bitcraze licensed until MIT.
// Notice below.
//
// Changes by Tiny Labs Inc for T24CxxA EEPROM.
//
// Copyright (c) 2021 Bitcraze
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Antmicro.Renode.Exceptions;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class EEPROM_24CxxA : II2CPeripheral
    {
        // Backing store
        private byte[] storage;
        private int size;

        // Device state
        private byte highAddress;
        private byte lowAddress;

        public EEPROM_24CxxA(int size = 256)
        {
            this.size = size;
            storage = new byte[size];
        }

        public byte[] Read(int count = 1)
        {
            // Currently no way to know how many bytes are read and thus
            // Sequential Read cannot properly increase the address counter
            // DO NOT use current address read after sequential read!
            this.Log(LogLevel.Noisy, "Reading 0x{0:X} bytes from EEPROM at address 0x{1:X}", count, ((ushort)highAddress<<8)+lowAddress);
            byte[] result = new byte[storage.Length];

            var ap = ((ushort)highAddress<<8)+lowAddress;
            Array.Copy(storage, ap, result, 0, storage.Length - ap);
            Array.Copy(storage, 0, result, storage.Length - ap, ap);

            lowAddress++;
            if(lowAddress == 0)
            {
                highAddress++;
                highAddress &= 0x1F;
            }
            return result;
        }

        public void Write(byte[] packet)
        {
            // Implemented: Byte and Page Write
            if(packet.Length < 2)
            {
                this.Log(LogLevel.Error, "Tried to write less than two bytes, i.e. missing address bytes.");
                return;
            }
            if(packet.Length > 34)
            {
                this.Log(LogLevel.Warning, "Page overflow. Trying to write {0} bytes of data and new data will be overwritten!", packet.Length-2);
            }
            if((packet[0] & ~0x1F) != 0)
            {
                this.Log(LogLevel.Warning, "Unused bits: 0x{0:X} for high address ignored!", packet[0] & ~0x1F);
            }
            highAddress = (byte)(packet[0] & 0x1F);
            lowAddress = packet[1];
            byte inPageAddress = (byte)(lowAddress & 0x1F);
            ushort pageAddress = (ushort)(((ushort)highAddress << 8) + (lowAddress & 0xE0));
            this.Log(LogLevel.Noisy, "inPageAddress: 0x{0:X}, pageAddress: 0x{1:X}, packet length: {2}",inPageAddress, pageAddress, packet.Length);
            //FIXME The I2C seems to send one packet too much of transmited data, workaround by ignoring the final byte (i.e. -3 instead of -2)?
            for(int i = 0; i < packet.Length - 3; ++i)
            {
                storage[pageAddress + ((inPageAddress+i)%0x20)] = packet[i+2]; // Page overflow
                this.Log(LogLevel.Noisy, "Trying to write data byte {0} (value: 0x{1:X})", i, packet[i+2]);
            }
            //TODO Handle the write delay!
        }

        public void FinishTransmission()
        {
           this.Log(LogLevel.Noisy, "FinishTransmission in EEPROM!");
        }

        public void Reset()
        {
        }

        public void LoadBinary(string fileName)
        {
            try
            {
                using(var reader = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var read = reader.Read(this.storage, 0, this.size);
                    if(read != this.size)
                    {
                        throw new RecoverableException($"Error while loading file {fileName}: file is too small");
                    }
                }
            }
            catch(IOException e)
            {
                throw new RecoverableException(string.Format("Exception while loading file {0}: {1}", fileName, e.Message));
            }
        }

    }
}
