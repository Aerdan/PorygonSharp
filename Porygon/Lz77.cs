/*
 * Lz77 is an implementation of the LZ77 compression algorithm.
 *
 * From https://gist.github.com/mjs3339/c5c26638a7485afaaed6ed171f1bfb0d
 */

using System;
using System.Collections.Generic;
using System.Linq;

public static class Lz77 {
    private const int RingBufferSize = 4096;
    private const int UpperMatchLength = 18;
    private const int LowerMatchLength = 2;
    private const int None = RingBufferSize;
    private static readonly int[] Parent = new int[RingBufferSize + 1];
    private static readonly int[] LeftChild = new int[RingBufferSize + 1];
    private static readonly int[] RightChild = new int[RingBufferSize + 257];
    private static readonly ushort[] Buffer = new ushort[RingBufferSize + UpperMatchLength - 1];
    private static int matchPosition, matchLength;
    /// <summary>
    ///     Size of the compressed code during and after compression
    /// </summary>
    public static int CompressedSize { get; set; }
    /// <summary>
    ///     Size of the original code packet while decompressing
    /// </summary>
    public static int DeCompressedSize { get; set; }
    public static double Ratio => (double) CompressedSize / DeCompressedSize * 100.0;

    public static byte[] Lz77Decompress(this byte[] ins) {
        if (ins == null) {
            throw new Exception("Input buffer is null.");
        }
        if (ins.Length == 0) {
            throw new Exception("Input buffer is empty.");
        }
        var outa = new GArray<byte>();
        var ina = new GArray<byte>(ins);

        CompressedSize = 0;
        DeCompressedSize = BitConverter.ToInt32(ina.Read(4));

        for (var i = 0; i < RingBufferSize - UpperMatchLength; i++) {
            Buffer[i] = 0;
        }
        var r = RingBufferSize - UpperMatchLength;
        uint flags = 7;
        var z = 7;

        while (true) {
            flags <<= 1;
            z++;
            if (z == 8) {
                if (ina.ReadEnd) {
                    break;
                }
                flags = ina.Read();
                z = 0;
            }

            if ((flags & 0x80) == 0) {
                if (ina.ReadEnd) {
                    break;
                }
                var c = ina.Read();
                if (CompressedSize < DeCompressedSize)
                    outa.Write(c);

                Buffer[r++] = c;
                r &= RingBufferSize - 1;
                CompressedSize++;
            } else {
                if (ina.ReadEnd) {
                    break;
                }
                int i = ina.Read();
                if (ina.ReadEnd) {
                    break;
                }
                int j = ina.Read();
                j = j | ((i << 8) & 0xF00);
                i = ((i >> 4) & 0xF) + LowerMatchLength;
                for (var k = 0; k <= i; k++) {
                    var c = Buffer[(r - j - 1) & (RingBufferSize - 1)];
                    if (CompressedSize < DeCompressedSize) {
                        outa.Write((byte) c);
                    }
                    Buffer[r++] = (byte) c;
                    r &= RingBufferSize - 1;
                    CompressedSize++;
                }
            }
        }

        return outa.ToArray();
    }
    /// <summary>
    ///     E:12.5, R:17.3 E:25.0, R:35.9 E:32.3, R:47.7 E:37.5, R:56.5 E:41.5, R:63.0 E:44.8, R:67.6 E:47.6, R:71.7 E:50.0,
    ///     R:75.8 E:52.1, R:79.9 E:54.0, R:83.9 E:55.7, R:87.7
    ///     E:57.3, R:91.0 E:58.8, R:93.9 E:60.1, R:96.6 E:61.3, R:98.8 E:62.5, R:100.6 E:63.6, R:102.1 E:64.6, R:103.5 E:65.6,
    ///     R:104.7 E:66.5, R:105.6 E:67.4, R:106.5
    ///     E:68.2, R:107.2 E:69.0, R:107.8 E:69.8, R:108.3 E:70.5, R:108.7
    /// </summary>
    public static byte[] Lz77Compress(this byte[] ins, bool TestForCompressibility = false) {
        if (ins == null) {
            throw new Exception("Input buffer is null.");
        }
        if (ins.Length == 0) {
            throw new Exception("Input buffer is empty.");
        }
        if (TestForCompressibility) {
            if ((int) Entropy(ins) > 61) {
                throw new Exception("Input buffer Cannot be compressed.");
            }
        }
        matchLength = 0;
        matchPosition = 0;
        CompressedSize = 0;
        DeCompressedSize = ins.Length;
        int length;
        var codeBuffer = new int[UpperMatchLength - 1];
        var outa = new GArray<byte>();
        var ina = new GArray<byte>(ins);
        outa.Write(BitConverter.GetBytes((int)DeCompressedSize));
        InitTree();
        codeBuffer[0] = 0;
        var codeBufferPointer = 1;
        var mask = 0x80;
        var s = 0;
        var r = RingBufferSize - UpperMatchLength;
        for (var i = s; i < r; i++)
            Buffer[i] = 0xFFFF;
        for (length = 0; length < UpperMatchLength && !ina.ReadEnd; length++)
            Buffer[r + length] = ina.Read();
        if (length == 0)
            throw new Exception("No Data to Compress.");
        for (var i = 1; i <= UpperMatchLength; i++)
            InsertNode(r - i);
        InsertNode(r);
        do {
            if (matchLength > length) {
                matchLength = length;
            }
            if (matchLength <= LowerMatchLength) {
                matchLength = 1;
                codeBuffer[codeBufferPointer++] = Buffer[r];
            } else {
                codeBuffer[0] |= mask;
                codeBuffer[codeBufferPointer++] = (byte) (((r - matchPosition - 1) >> 8) & 0xF) | ((matchLength - (LowerMatchLength + 1)) << 4);
                codeBuffer[codeBufferPointer++] = (byte) ((r - matchPosition - 1) & 0xFF);
            }

            if ((mask >>= 1) == 0) {
                for (var i = 0; i < codeBufferPointer; i++) {
                    outa.Write((byte) codeBuffer[i]);
                }
                CompressedSize += codeBufferPointer;
                codeBuffer[0] = 0;
                codeBufferPointer = 1;
                mask = 0x80;
            }

            var lastMatchLength = matchLength;
            var ii = 0;
            for (ii = 0; ii < lastMatchLength && !ina.ReadEnd; ii++) {
                DeleteNode(s);
                var c = ina.Read();
                Buffer[s] = c;
                if (s < UpperMatchLength - 1) {
                    Buffer[s + RingBufferSize] = c;
                }
                s = (s + 1) & (RingBufferSize - 1);
                r = (r + 1) & (RingBufferSize - 1);
                InsertNode(r);
            }

            while (ii++ < lastMatchLength) {
                DeleteNode(s);
                s = (s + 1) & (RingBufferSize - 1);
                r = (r + 1) & (RingBufferSize - 1);
                if (--length != 0) {
                    InsertNode(r);
                }
            }
        } while (length > 0);

        if (codeBufferPointer > 1) {
            for (var i = 0; i < codeBufferPointer; i++) {
                outa.Write((byte) codeBuffer[i]);
            }
            CompressedSize += codeBufferPointer;
        }

        if (CompressedSize % 4 != 0) {
            for (var i = 0; i < 4 - CompressedSize % 4; i++) {
                outa.Write(0);
            }
        }
        return outa.ToArray();
    }

    private static void InitTree() {
        for (var i = RingBufferSize + 1; i <= RingBufferSize + 256; i++) {
            RightChild[i] = None;
        }
        for (var i = 0; i < RingBufferSize; i++) {
            Parent[i] = None;
        }
    }
    private static void InsertNode(int r) {
        var cmp = 1;
        var p = RingBufferSize + 1 + (Buffer[r] == 0xFFFF ? 0 : Buffer[r]);
        RightChild[r] = LeftChild[r] = None;
        matchLength = 0;
        while (true) {
            if (cmp >= 0) {
                if (RightChild[p] != None) {
                    p = RightChild[p];
                } else {
                    RightChild[p] = r;
                    Parent[r] = p;
                    return;
                }
            } else {
                if (LeftChild[p] != None) {
                    p = LeftChild[p];
                } else {
                    LeftChild[p] = r;
                    Parent[r] = p;
                    return;
                }
            }

            int i;
            for (i = 1; i < UpperMatchLength; i++) {
                if ((cmp = Buffer[r + i] - Buffer[p + i]) != 0) {
                    break;
                }
            }
            if (i > matchLength) {
                matchPosition = p;
                if ((matchLength = i) >= UpperMatchLength) {
                    break;
                }
            }
        }

        Parent[r] = Parent[p];
        LeftChild[r] = LeftChild[p];
        RightChild[r] = RightChild[p];
        Parent[LeftChild[p]] = r;
        Parent[RightChild[p]] = r;

        if (RightChild[Parent[p]] == p) {
            RightChild[Parent[p]] = r;
        } else {
            LeftChild[Parent[p]] = r;
        }
        Parent[p] = None;
    }
    private static void DeleteNode(int p) {
        int q;
        if (Parent[p] == None) {
            return;
        }
        if (RightChild[p] == None) {
            q = LeftChild[p];
        } else if (LeftChild[p] == None) {
            q = RightChild[p];
        } else {
            q = LeftChild[p];
            if (RightChild[q] != None) {
                do {
                    q = RightChild[q];
                } while (RightChild[q] != None);

                RightChild[Parent[q]] = LeftChild[q];
                Parent[LeftChild[q]] = Parent[q];
                LeftChild[q] = LeftChild[p];
                Parent[LeftChild[p]] = q;
            }

            RightChild[q] = RightChild[p];
            Parent[RightChild[p]] = q;
        }

        Parent[q] = Parent[p];
        if (RightChild[Parent[p]] == p) {
            RightChild[Parent[p]] = q;
        } else {
            LeftChild[Parent[p]] = q;
        }
        Parent[p] = None;
    }
    private static double Entropy(byte[] ba) {
        var map = new Dictionary<byte, int>();
        foreach (var c in ba) {
            if (!map.ContainsKey(c)) {
                map.Add(c, 1);
            } else {
                map[c]++;
            }
        }
        double Len = ba.Length;
        var re = map.Select(item => item.Value / Len)
            .Aggregate(0.0, (current, frequency) =>
                current - frequency * (Math.Log(frequency) / Math.Log(2)));
        return re / 8.00D * 100D;
    }
}