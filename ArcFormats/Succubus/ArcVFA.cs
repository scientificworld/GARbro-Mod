//! \file       ArcVFA.cs
//! \date       2026-11-03
//! \brief      OVERDRIVE resource archive.
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
using System.Text;

namespace GameRes.Formats.Succubus
{
    [Export(typeof(ArchiveFormat))]
    public class VfaOpener : ArchiveFormat
    {
        public override string         Tag { get { return "VFA"; } }
        public override string Description { get { return "OVERDRIVE resource archive"; } }
        public override uint     Signature { get { return 0; } } // 'RIFF'
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            // 'RIFF' isn't included into signature to avoid auto-detection of the WAV files as archives.
            if (file.View.ReadUInt32 (0) != 0x46464952)
                return null;
            if (file.View.ReadUInt32 (8) != 0x31414656) // 'VFA1'
                return null;

            List<Entry> dir = null;
            uint offset = 0xC;
            uint data_offset = 0;
            while (offset < file.MaxOffset)
            {
                uint fourcc = file.View.ReadUInt32 (offset);
                uint size = file.View.ReadUInt32 (offset + 4);
                offset += 8;
                if (fourcc == 0x746E6564) // 'dent'
                    dir = ReadIndex (file, offset, size);
                else if (fourcc == 0x61746164) // 'data'
                    data_offset = offset;
                offset += size;
            }

            if (dir == null || dir.Count == 0 || data_offset == 0)
                return null;
            for (int i = 0; i < dir.Count; i++)
            {
                dir[i].Offset += data_offset;
                if (!dir[i].CheckPlacement (file.MaxOffset))
                    return null;
            }

            return new ArcFile (file, this, dir);
        }

        List<Entry> ReadIndex (ArcView file, uint offset, uint size)
        {
            var dir = new List<Entry>();
            uint end = offset + size;
            string cur_folder = "";
            while (offset < end)
            {
                uint fourcc = file.View.ReadUInt32 (offset);
                uint entry_size = file.View.ReadUInt32 (offset + 4);
                offset += 8;
                if (fourcc == 0x20726964) // 'dir '
                {
                    cur_folder = file.View.ReadString (offset, entry_size, Encoding.Unicode);
                }
                else if (fourcc == 0x656C6966) // 'file'
                {
                    var name = file.View.ReadString (offset, entry_size - 0x10, Encoding.Unicode);
                    name = Path.Combine (cur_folder, name);
                    var entry = Create<Entry> (name);
                    entry.Offset = file.View.ReadUInt32 (offset + entry_size - 0x10);
                    entry.Size = file.View.ReadUInt32 (offset + entry_size - 0xC);
                    dir.Add (entry);
                }
                offset += entry_size;
            }
            return dir;
        }
    }
}
