//! \file       ImageV12.cs
//! \date       2026-11-03
//! \brief      WINC image format.
//
// Copyright (C) 2026 by morkt
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
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;

namespace GameRes.Formats.Winc
{
    internal class V12MetaData : ImageMetaData
    {
        public int DataOffset;
    }

    [Export(typeof(ImageFormat))]
    public class V12Format : ImageFormat
    {
        public override string         Tag { get { return "V12"; } }
        public override string Description { get { return "WINC image format"; } }
        public override uint     Signature { get { return 0x52475F4D; } } // 'M_GRPVr1'
        public override bool      CanWrite { get { return false; } }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            var header = stream.ReadHeader (0x10);
            if (!header.AsciiEqual (4, "PVr1"))
                return null;
            return new V12MetaData {
                Width = header.ToUInt16 (8),
                Height = header.ToUInt16 (0xA),
                BPP = 24,
                DataOffset = ((int)header.ToUInt16 (0xC) << 2) + 0x10 // XXX non-zero first offset
            };
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            var meta = info as V12MetaData;
            int stride = meta.iWidth * 3;
            var pixels = new byte[stride * meta.iHeight];
            stream.Position = meta.DataOffset;

            int dst = 0;
            for (int i = 0; i < meta.iHeight; i++)
            {
                int run_type = stream.ReadByte();

                while (dst < stride * (i + 1))
                {
                    int len = ((meta.iWidth > 0xFF) ? stream.ReadUInt16() : stream.ReadUInt8()) * 3;

                    if (run_type != 0)
                    {
                        stream.Read (pixels, dst, len);
                    }
                    run_type ^= 1;
                    dst += len;
                }
            }
            return ImageData.CreateFlipped (info, PixelFormats.Bgr24, null, pixels, stride);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotImplementedException ("V12Format.Write not implemented");
        }
    }
}
