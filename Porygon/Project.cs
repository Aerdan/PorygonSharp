/*
 * Porygon.Project - Management for a single set of image insertions.
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
using System.Drawing;
using System.Collections.Generic;
using System.IO;

using LibPixelPet;
using Tommy;

namespace Porygon {
    enum Compression {
        None,
        Lz77,
    }
    enum Platform {
        GameBoy,
        GameBoyAdvance,
    }
    class Project {
        public string Name { get; }
        public string TargetFilename { get; }
        public List<Image> Images { get; }
        public Compression Compression { get; }
        public Platform System { get; }

        public Project(TomlTable cfg) {
            Name = cfg["name"];
            Images = new List<Image>();
            TargetFilename = cfg["target"];

            if (cfg["compress"] == "lz77") {
                Compression = Compression.Lz77;
            } else {
                Compression = Compression.None;
            }

            string system = cfg["system"];
            if ((String.Compare(system, "gba", true) == 0) ||
                (String.Compare(system, "gameboy advance", true) == 0) ||
                (String.Compare(system, "game boy advance", true) == 0) ||
                (String.Compare(system, "gameboyadvance", true) == 0))
            {
                System = Platform.GameBoyAdvance;
            }

            foreach (TomlTable image in cfg["image"]) {
                if (!image.HasKey("palette_format")) { image["palette_format"] = cfg["palette_format"]; }
                if (!image.HasKey("graphic_size"))   { image["graphic_size"]   = cfg["graphic_size"]; }
                if (!image.HasKey("graphic_format")) { image["graphic_format"] = cfg["graphic_format"]; }
                if (!image.HasKey("palette_size"))   { image["palette_size"]   = cfg["palette_size"]; }
                if (!image.HasKey("palette_format")) { image["palette_format"] = cfg["palette_format"]; }
                Images.Add(new Image(image));
            }
        }

        private byte Bits(int n) {
            byte c = 0;
            while (n > 0) {
                c += (byte) (n & 1);
                n >>= 1;
            }
            return c;
        }

        private byte[] ToInt24LE(int n) {
            byte[] c = new byte[3];
            c[0] = (byte) (n & 0xFF);
            c[1] = (byte)((n >> 8) & 0xFF);
            c[2] = (byte)((n >> 16) & 0xFF);
            return c;
        }

        public void Process(string filename) {
            var data = File.ReadAllBytes(filename);
            var output = File.OpenWrite(TargetFilename);

            output.Write(data);
            output.Flush();

            foreach (Image image in Images) {
                Palette pal = image.GetPalette();
                Tileset ts = image.GetTileset(pal, false);
                byte[] pal_data = pal.Serialize();
                byte[] base_data = image.Serialize(ts, 0);
                byte[] image_data = new byte[base_data.Length + 8];

                if (Compression == Compression.Lz77) {
                    if (System == Platform.GameBoyAdvance) {
                        byte[] lol = BitConverter.GetBytes((int)1);
                        byte[] temp;
                        if (!BitConverter.IsLittleEndian) {
                            Array.Reverse(lol);
                        }
                        int size = image_data.Length / (8*8 / (Bits(image.PaletteSize - 1) / 8));
                        temp = BitConverter.GetBytes(size);
                        if (!BitConverter.IsLittleEndian) {
                            Array.Reverse(temp);
                        }
                        Array.Copy(lol, 0, image_data, 0, 4);
                        Array.Copy(temp, 0, image_data, 4, 4);
                        Array.Copy(base_data, 0, image_data, 8, base_data.Length);
                    }
                    image_data = Lz77.Lz77Compress(image_data);
                }

                long seek;
                SeekOrigin origin;
                if (image.Target < 0) {
                    seek = Math.Abs(image.Target) - 1;
                    origin = SeekOrigin.End;
                } else {
                    seek = image.Target;
                    origin = SeekOrigin.Begin;
                }

                output.Seek(seek, origin);
                byte[] address = ToInt24LE((int)output.Position);
                output.Write(image_data);
                
                output.Seek(image.Graphic, SeekOrigin.Begin);
                output.Write(address);
                
                output.Seek(image.Palette, SeekOrigin.Begin);
                output.Write(pal_data);
                
                output.Flush();
            }
            output.Close();
        }
    }
}
