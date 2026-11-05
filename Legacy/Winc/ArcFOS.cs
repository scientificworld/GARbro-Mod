//! \file       ArcFOS.cs
//! \date       2026-11-01
//! \brief      WINC resource archive.
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
using System.Linq;
using GameRes.Compression;

namespace GameRes.Formats.Winc
{
    [Export(typeof(ArchiveFormat))]
    public class FosOpener : ArchiveFormat
    {
        public override string         Tag { get { return "FOS"; } }
        public override string Description { get { return "WINC resource archive"; } }
        public override uint     Signature { get { return 0x31534F46; } } // 'FOS1'
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            var dir = new List<Entry> ();
            var folders = new List<string> ();
            var sizes = new List<uint> ();

            uint index_offset = 8;
            uint index_size = file.View.ReadUInt32 (4);
            if (index_size > file.MaxOffset)
                return null;
            sizes.Add (index_size);
            while (sizes[0] > 0)
            {
                uint name_size = file.View.ReadByte (index_offset);
                bool is_folder = file.View.ReadByte (index_offset + 1) == 0;
                uint consumed = name_size + 4;
                if (is_folder)
                {
                    var name = file.View.ReadString (index_offset + 12, name_size);
                    index_size = file.View.ReadUInt32 (index_offset + 4);
                    consumed += 8;
                    index_size += consumed;
                    if (index_size > sizes.Last())
                        return null;
                    folders.Add (name);
                    sizes.Add (index_size);
                }
                else
                {
                    var name = file.View.ReadString (index_offset + 16, name_size);
                    name = Path.Combine (Path.Combine (folders.ToArray()), name);
                    var entry = Create<PackedEntry> (name);
                    entry.UnpackedSize = file.View.ReadUInt32 (index_offset + 4);
                    entry.Size = file.View.ReadUInt32 (index_offset + 8);
                    entry.Offset = file.View.ReadUInt32 (index_offset + 12);
                    if (!entry.CheckPlacement (file.MaxOffset))
                        return null;
                    consumed += 12;
                    dir.Add (entry);
                }
                index_offset += consumed;
                for (int i = 0; i < sizes.Count(); i++)
                    sizes[i] -= consumed;
                for (int i = sizes.Count() - 1; i > 0 && sizes[i] <= 0; i--)
                {
                    folders.RemoveAt (i - 1);
                    sizes.RemoveAt (i);
                }
            }
            if (dir.Count == 0)
                return null;

            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var input = arc.File.CreateStream (entry.Offset, entry.Size);
            return new LzssStream (input);
        }
    }
}
