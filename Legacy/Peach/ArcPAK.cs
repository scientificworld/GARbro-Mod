//! \file       ArcPAK.cs
//! \date       2026-09-18
//! \brief      PEACH resource archive.
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;

namespace GameRes.Formats.Peach
{
    [Export(typeof(ArchiveFormat))]
    public class RqfOpener : ArchiveFormat
    {
        public override string         Tag { get { return "PAK/RQF"; } }
        public override string Description { get { return "PEACH resource archive"; } }
        public override uint     Signature { get { return 0; } }
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.View.AsciiEqual (0, "Rqf"))
                return null;

            int count = file.View.ReadUInt16 (4);
            if (!IsSaneCount (count))
                return null;

            var base_name = Path.GetFileNameWithoutExtension (file.Name);
            var parent_dir = new DirectoryInfo (VFS.Top.CurrentDirectory).Name.ToLowerInvariant();
            uint index_offset = 6;

            var dir = new List<Entry> (count);
            for (int i = 0; i < count; ++i)
            {
                var entry = new Entry {
                    Name = string.Format ("{0}#{1:D4}", base_name, i),
                    Offset = file.View.ReadUInt32 (index_offset),
                };
                dir.Add (entry);
                index_offset += 4;
            }
            for (int i = 0; i < count; ++i)
            {
                long next_offset = (i == count - 1) ? file.MaxOffset : dir[i + 1].Offset;
                dir[i].Size = (uint)(next_offset - dir[i].Offset);
                if (!dir[i].CheckPlacement (file.MaxOffset))
                    return null;
                if (parent_dir == "cg")
                    dir[i].Type = "image";
                else if (parent_dir == "snd")
                    dir[i].Type = "audio";
            }

            return new ArcFile (file, this, dir);
        }

        public override IImageDecoder OpenImage (ArcFile arc, Entry entry)
        {
            var input = arc.OpenBinaryEntry (entry);
            return new RqfBitmapDecoder (input);
        }
    }

    internal class RqfBitmapDecoder : BinaryImageDecoder
    {
        public RqfBitmapDecoder (IBinaryStream input) : base (input)
        {
            Info = new ImageMetaData {
                Width  = m_input.ReadUInt16(),
                Height = m_input.ReadUInt16(),
            };
        }

        protected override ImageData GetImageData ()
        {
            try
            {
                return Get24BitImageData();
            }
            catch
            {
                return Get8BitImageData();
            }
        }

        ImageData Get24BitImageData ()
        {
            m_input.Position = 4;
            int strike = Info.iWidth * 3;
            var pixels = new byte[strike * Info.iHeight];
            int dst = 0;
            while (dst < pixels.Length)
            {
                int v = ReadByte();
                if (v == 0)
                {
                    v = ReadByte();
                    for (int i = 0; i <= v; i++)
                    {
                        pixels[dst++] = ReadByte();
                        pixels[dst++] = ReadByte();
                        pixels[dst++] = ReadByte();
                    }
                }
                else
                {
                    byte b = ReadByte();
                    byte g = ReadByte();
                    byte r = ReadByte();
                    for (int i = 0; i <= v; i++)
                    {
                        pixels[dst++] = b;
                        pixels[dst++] = g;
                        pixels[dst++] = r;
                    }
                }
            }
            Info.BPP = 24;
            return ImageData.CreateFlipped (Info, PixelFormats.Bgr24, null, pixels, strike);
        }

        ImageData Get8BitImageData ()
        {
            m_input.Position = 4;
            int strike = Info.iWidth;
            var pixels = new byte[strike * Info.iHeight];
            int dst = 0;
            while (dst < pixels.Length)
            {
                int v = ReadByte();
                if (v == 0)
                {
                    v = ReadByte();
                    for (int i = 0; i <= v; i++)
                    {
                        byte b = ReadByte();
                        pixels[dst++] = b;
                    }
                }
                else
                {
                    byte b = ReadByte();
                    for (int i = 0; i <= v; i++)
                    {
                        pixels[dst++] = b;
                    }
                }
            }
            Info.BPP = 8;
            return ImageData.CreateFlipped (Info, PixelFormats.Gray8, null, pixels, strike);
        }

        byte ReadByte ()
        {
            int b = m_input.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            return (byte)b;
        }
    }
}
