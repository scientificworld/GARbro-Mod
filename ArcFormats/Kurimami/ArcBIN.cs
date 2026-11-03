//! \file       ArcBIN.cs
//! \date       2026-11-02
//! \brief      Atelier Kurimami resource archive.
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

namespace GameRes.Formats.Kurimami
{
    [Export(typeof(ArchiveFormat))]
    public class BinOpener : ArchiveFormat
    {
        public override string         Tag { get { return "BIN/KURIMAMI"; } }
        public override string Description { get { return "Atelier Kurimami resource archive"; } }
        public override uint     Signature { get { return 0; } }
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.Name.HasExtension (".bin"))
                return null;

            uint index_offset = 0;
            var dir = new List<Entry> ();
            while (index_offset < 0x4000)
            {
                var name = file.View.ReadString (index_offset, 0x10);
                if (string.IsNullOrEmpty (name))
                    break;
                dir.Add (Create<Entry> (name));
                index_offset += 0x10;
            }

            if (dir.Count == 0)
                return null;
            index_offset = 0x4000;
            for (int i = 0; i < dir.Count; i++)
            {
                uint offset = file.View.ReadUInt32 (index_offset);
                uint next_offset = file.View.ReadUInt32 (index_offset + 4);
                dir[i].Offset = offset;
                dir[i].Size = next_offset - offset;
                if (!dir[i].CheckPlacement (file.MaxOffset))
                    return null;
                index_offset += 4;
            }

            return new ArcFile (file, this, dir);
        }
    }
}
