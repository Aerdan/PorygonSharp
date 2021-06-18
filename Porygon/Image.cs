/*
 * Porygon.Image - Manipulation and metadata for a single image.
 *
 * Copyright (C) 2021 Síle Ekaterin Aman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Tommy;
using LibPixelPet;

namespace Porygon {
    class Image {
        public string Name { get; }
        public Bitmap Bitmap { get; }
        public BitmapFormat Format { get; }
        public ColorFormat Colors { get; }
        public long Graphic { get; }
        public int GraphicSize { get; }
        public long Palette { get; }
        public int PaletteSize { get; }
        public long Target { get; }
        private string BaseFilename;
        private static Dictionary<string, ColorFormat> ColorFormats = new Dictionary<string, ColorFormat> {
            {"RGB555", ColorFormat.RGB555},
            {"BGR888", ColorFormat.BGR888},
            {"RGBA5551", ColorFormat.RGBA5551},
            {"BGRA8888", ColorFormat.BGRA8888},
            {"Grayscale2BPP", ColorFormat.Grayscale2BPP},
            {"Grayscale4BPP", ColorFormat.Grayscale4BPP},
            {"Grayscale8BPP", ColorFormat.Grayscale8BPP},
            {"Gameboy", ColorFormat.GameBoy},
        };

        public Image(TomlTable cfg) {
            string[] frags;
            Name = cfg["name"];
            
            if (cfg["format"] == "GBA-4BPP") {
                Format = BitmapFormat.GBA4BPP;
            } else if (cfg["format"] == "GB") {
                Format = BitmapFormat.GB;
            } else if (cfg["format"] == "GBA-8BPP") {
                Format = BitmapFormat.GBA8BPP;
            } else if (cfg["format"] == "NDSTEX5") {
                Format = BitmapFormat.NDSTEX5;
            } else {
                Console.WriteLine("Warning: unrecognized target format; ignoring.");
            }
            if (ColorFormats.ContainsKey(cfg["palette_format"])) {
                Colors = ColorFormats[cfg["palette_format"]];
            }

            Graphic     = cfg["graphic"];
            GraphicSize = cfg["graphic_size"];
            Palette     = cfg["palette"];
            PaletteSize = cfg["palette_size"];
            Target      = cfg["target"];

            BaseFilename = cfg["filename"];
            frags = BaseFilename.Split(".");

            Bitmap = new Bitmap(BaseFilename);
            BaseFilename = String.Join(".", frags.SkipLast(1));
        }

        public Palette GetPalette() {
            TileCutter cutter = new TileCutter(8, 8);
            Palette pal = new Palette(Colors, PaletteSize);

            foreach (Tile tile in cutter.CutTiles(Bitmap)) {
                List<int> colors = tile.EnumerateTile().Distinct().ToList();

                foreach (int color in colors.Where(c => !pal.Contains(c))) {
                    pal.Add(color);
                }
            }

            return pal;
        }

        public Tileset GetTileset(Palette pal, bool deduplicate) {
            Tilemap tm = new Tilemap(Format);
            Tileset ts = new Tileset(8, 8);
            PaletteSet ps = new PaletteSet();
            ps.Add(pal);
            if (Format.IsIndexed) {
                tm.AddBitmapIndexed(Bitmap, ts, ps, Format, deduplicate);
            } else {
                tm.AddBitmap(Bitmap, ts, Format, deduplicate);
            }
            return ts;
        }
        
        public byte[] Serialize(Tileset tiles, int offset = 0) {
            List<byte> data = new List<byte>();
            int bpp = Format.Bits;
            int ppb = 8 / bpp;
            int pi, b;
            long co;

            foreach (Tile tile in tiles) {
                pi = 0;
                b  = 0;
                foreach (int c in tile.EnumerateTile()) {
                    co = c + offset;
                    b |= (int)(co << (pi * bpp));

                    if (++pi >= ppb) {
                        data.Add((byte)b);
                        pi = 0;
                        b = 0;
                    }
                }
                if (pi > 0) {
                    data.Add((byte)b);
                }
            }

            return data.ToArray();
        }
    }
}
