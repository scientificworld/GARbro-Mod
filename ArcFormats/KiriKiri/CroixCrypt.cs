//! \file       CroixCrypt.cs
//! \date       2017 Dec 27
//! \brief      Croix encryption algorithm for KiriKiri.
//
// Copyright (C) 2017 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System;

namespace GameRes.Formats.KiriKiri
{
    [Serializable]
    public class CroixCrypt : ICrypt
    {
        public byte[] KeyTable;

        public CroixCrypt (ulong checksum)
        {
            KeyTable = new byte[0x3D];

            checksum &= 0x1FFFFFFFFFFFFFFF;
            for (int i = 0; i < 0x3D; ++i)
            {
                KeyTable[i] = (byte)checksum;
                checksum = checksum >> 8 | (checksum & 0xFF) << 53;
            }
        }

        public override void Decrypt (Xp3Entry entry, long offset, byte[] data, int pos, int count)
        {
            uint hash = entry.Hash & 0x7FFFFFFF;
            var v83 = new byte[0x1F];
            for (int i = 0; i < 0x1F; ++i)
            {
                v83[i] = (byte)hash;
                hash = hash >> 8 | (hash & 0xFF) << 23;
            }
            for (int i = 0; i < count; ++i)
            {
                long v63 = offset + i;
                data[pos+i] ^= v83[v63 % 0x1F];
                data[pos+i] += KeyTable[v63 % 0x3D];
            }
        }

        public override void Encrypt (Xp3Entry entry, long offset, byte[] data, int pos, int count)
        {
            uint hash = entry.Hash & 0x7FFFFFFF;
            var v83 = new byte[0x1F];
            for (int i = 0; i < 0x1F; ++i)
            {
                v83[i] = (byte)hash;
                hash = hash >> 8 | (hash & 0xFF) << 23;
            }
            for (int i = 0; i < count; ++i)
            {
                long v63 = offset + i;
                data[pos+i] -= KeyTable[v63 % 0x3D];
                data[pos+i] ^= v83[v63 % 0x1F];
            }
        }
    }
}
