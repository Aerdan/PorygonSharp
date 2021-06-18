using System.IO;

using LibPixelPet;

namespace Porygon {
    public static class LibPixelPetExtensions {
        public static byte[] Serialize(this LibPixelPet.Palette p) {
            MemoryStream mem = new MemoryStream();
            int colors = 0;
            int width = p.Count;
            int bpc = (p.Format.Bits + 7) / 8;

            for (int i = 0; i < width; i++) {
                int c = i < p.Count ? p[i] : 0;

                for (int j = 0; j < bpc; j++) {
                    mem.WriteByte((byte)(c & 0xFF));
                    c >>= 8;
                }
                colors++;
            }
            return mem.ToArray();
        }
    }
}
