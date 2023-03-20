using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QFS.net {
    public static class QFS_FSHLib {
        public static byte[] Compress(byte[] data, bool incLen) {
            int windowsize = 131072;
            int windowmask = windowsize - 1;
            int maxIterations = 50;
            int[,] rev_last = new int[256, 256];
            int[] rev_similar = new int[windowsize];
            int num3 = 0;

            for (int index1 = 0; index1 < 256; ++index1) {
                for (int index2 = 0; index2 < 256; ++index2)
                    rev_last[index1, index2] = -1;
            }
            Array.Fill(rev_similar, -1);

            int inputLength = data.Length;
            byte[] outData = new byte[inputLength + 1028];
            Array.Copy(data, 0, outData, 0, inputLength);
            byte[] numArray4 = new byte[inputLength];
            numArray4[0] = 16;
            numArray4[1] = 251;
            numArray4[2] = (byte) (inputLength >> 16);
            numArray4[3] = (byte) (inputLength >> 8 & byte.MaxValue);
            numArray4[4] = (byte) (inputLength & byte.MaxValue);
            int outPos = 5;
            int currPos = 0;
            int sourceIndex = 0;


            while (currPos < inputLength) {
                int num5 = rev_last[outData[currPos], outData[currPos + 1]];
                int num6 = rev_similar[currPos & windowmask] = num5;
                rev_last[outData[currPos], outData[currPos + 1]] = currPos;
                if (currPos < sourceIndex) {
                    ++currPos;
                } else {
                    int num7 = 0;
                    for (int index4 = 0; num6 >= 0 && currPos - num6 < windowsize && index4++ < maxIterations; num6 = rev_similar[num6 & windowmask]) {
                        int num8 = 2;
                        while (outData[currPos + num8] == outData[num6 + num8] && num8 < 1028) {
                            ++num8;
                        }
                        if (num8 > num7) {
                            num7 = num8;
                            num3 = currPos - num6;
                        }
                    }
                    if (num7 > inputLength - currPos) { num7 = currPos - inputLength; }
                    if (num7 <= 2) { num7 = 0; }
                    if (num7 == 3 && num3 > 1024) { num7 = 0; }
                    if (num7 == 4 && num3 > 16384) { num7 = 0; }
                    if (num7 > 0) {
                        while (currPos - sourceIndex >= 4) {
                            int num9 = (currPos - sourceIndex) / 4 - 1;
                            if (num9 > 27)
                                num9 = 27;
                            byte[] numArray5 = numArray4;
                            int index5 = outPos;
                            int destinationIndex = index5 + 1;
                            int num10 = (byte) (224 + num9);
                            numArray5[index5] = (byte) num10;
                            int length3 = 4 * num9 + 4;
                            Array.Copy(outData, sourceIndex, numArray4, destinationIndex, length3);
                            sourceIndex += length3;
                            outPos = destinationIndex + length3;
                        }

                        int num11 = currPos - sourceIndex;
                        if (num7 <= 10 && num3 <= 1024) {
                            byte[] numArray6 = numArray4;
                            int index6 = outPos;
                            int num12 = index6 + 1;
                            int num13 = (byte) ((num3 - 1 >> 8 << 5) + (num7 - 3 << 2) + num11);
                            numArray6[index6] = (byte) num13;
                            byte[] numArray7 = numArray4;
                            int index7 = num12;
                            outPos = index7 + 1;
                            int num14 = (byte) (num3 - 1 & byte.MaxValue);
                            numArray7[index7] = (byte) num14;
                            while (num11-- > 0)
                                numArray4[outPos++] = outData[sourceIndex++];
                            sourceIndex += num7;
                        } 
                        
                        else if (num7 <= 67 && num3 <= 16384) {
                            byte[] numArray8 = numArray4;
                            int index8 = outPos;
                            int num15 = index8 + 1;
                            int num16 = (byte) (128 + (num7 - 4));
                            numArray8[index8] = (byte) num16;
                            byte[] numArray9 = numArray4;
                            int index9 = num15;
                            int num17 = index9 + 1;
                            int num18 = (byte) ((num11 << 6) + (num3 - 1 >> 8));
                            numArray9[index9] = (byte) num18;
                            byte[] numArray10 = numArray4;
                            int index10 = num17;
                            outPos = index10 + 1;
                            int num19 = (byte) (num3 - 1 & byte.MaxValue);
                            numArray10[index10] = (byte) num19;
                            while (num11-- > 0)
                                numArray4[outPos++] = outData[sourceIndex++];
                            sourceIndex += num7;
                        } 
                        
                        else if (num7 <= 1028 && num3 < windowsize) {
                            --num3;
                            byte[] numArray11 = numArray4;
                            int index11 = outPos;
                            int num20 = index11 + 1;
                            int num21 = (byte) (192 + (num3 >> 16 << 4) + (num7 - 5 >> 8 << 2) + num11);
                            numArray11[index11] = (byte) num21;
                            byte[] numArray12 = numArray4;
                            int index12 = num20;
                            int num22 = index12 + 1;
                            int num23 = (byte) (num3 >> 8 & byte.MaxValue);
                            numArray12[index12] = (byte) num23;
                            byte[] numArray13 = numArray4;
                            int index13 = num22;
                            int num24 = index13 + 1;
                            int num25 = (byte) (num3 & byte.MaxValue);
                            numArray13[index13] = (byte) num25;
                            byte[] numArray14 = numArray4;
                            int index14 = num24;
                            outPos = index14 + 1;
                            int num26 = (byte) (num7 - 5 & byte.MaxValue);
                            numArray14[index14] = (byte) num26;
                            while (num11-- > 0)
                                numArray4[outPos++] = outData[sourceIndex++];
                            sourceIndex += num7;
                        }
                    }
                    ++currPos;
                }
            }

            int num27 = inputLength;
            while (num27 - sourceIndex >= 4) {
                int num28 = (num27 - sourceIndex) / 4 - 1;
                if (num28 > 27)
                    num28 = 27;
                byte[] numArray15 = numArray4;
                int index15 = outPos;
                int destinationIndex = index15 + 1;
                int num29 = (byte) (224 + num28);
                numArray15[index15] = (byte) num29;
                int length4 = 4 * num28 + 4;
                Array.Copy(outData, sourceIndex, numArray4, destinationIndex, length4);
                sourceIndex += length4;
                outPos = destinationIndex + length4;
            }

            int num30 = num27 - sourceIndex;
            byte[] numArray16 = numArray4;
            int index16 = outPos;
            int length5 = index16 + 1;
            int num31 = (byte) (252 + num30);
            numArray16[index16] = (byte) num31;
            while (num30-- > 0)
                numArray4[length5++] = outData[sourceIndex++];
            byte[] destinationArray1 = new byte[length5];
            Array.Copy(numArray4, 0, destinationArray1, 0, length5);
            byte[] sourceArray = destinationArray1;
            if (incLen) {
                byte[] destinationArray2 = new byte[sourceArray.Length + 4];
                Array.Copy(sourceArray, 0, destinationArray2, 4, sourceArray.Length);
                byte[] bytes = BitConverter.GetBytes(destinationArray2.Length);
                destinationArray2[0] = bytes[0];
                destinationArray2[1] = bytes[1];
                destinationArray2[2] = bytes[2];
                destinationArray2[3] = bytes[3];
                sourceArray = destinationArray2;
            }
            return sourceArray;
        }
    }
}
