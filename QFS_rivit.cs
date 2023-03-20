using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

//Converted from rivit's Visual Basic code by a converter. Code in this file is unmodified.

public sealed partial class QFSCompressor  // static
{

	private QFSCompressor() {
	}

	public static byte[] DecompressBlock(byte[] InBlock) {

		if (InBlock is null)
			throw new ArgumentNullException("InBlock", "QFS no Input to decompress");

		var tmp = new byte[1];

		if ((InBlock[0] & 0xFE) == 0x10 & InBlock[1] == 0xFB)        // 10 FB xx xx .. = QFS signature...
		{
			if (0 == mQFSCompression.QFS_Uncompress(InBlock, 0, InBlock.Length, ref tmp)) {
				return tmp;
			}
		} else if ((InBlock[4] & 0xFE) == 0x10 & InBlock[5] == 0xFB)    // zz zz zz zz 10 FB xx xx .. = filesize QFS signature
		  {
			if (0 == mQFSCompression.QFS_Uncompress(InBlock, 4, InBlock.Length, ref tmp)) {
				return tmp;
			}
		}

		return null;

	}

	public static byte[] CompressBlock(byte[] InBlock) {

		if (InBlock is null)
			throw new ArgumentNullException("InBlock", "QFS no Input to compress");

		int buflen = InBlock.Length;              // known data length
		var tmpInput = new byte[InBlock.Length + 1031 + 1];
		Array.Copy(InBlock, tmpInput, buflen);               // datablock+padding
		var tmpOutput = new byte[1];                            // destination compressed data

		if (mQFSCompression.QFS_Compress(ref tmpOutput, tmpInput, 0, ref buflen) == 0)
			return tmpOutput;

		return null;

	}

}

internal static partial class mQFSCompression {
	// mQFSCompression.bas a component of rvtFSHLib a FSH File Manager Library for SimCity4 Modding Tools
	// 
	// ©2009-2021 R.van Tilburg, Brainscrambler Products
	// Bug Reports to rivit@tpg.com.au, please include sufficient info to recreate issue.
	// Jun 2021 - optimised speed of Compress by changing the initialisations of arrays
	// 
	// This code is free software; you can redistribute it and/or
	// modify it under the terms of the GNU General Public License
	// as published by the Free Software Foundation; either version 2
	// of the License, or (at your option) any later version.
	// 
	// This program is distributed in the hope that it will be useful,
	// but WITHOUT ANY WARRANTY; without even the implied warranty of
	// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	// GNU General Public License for more details.
	// 
	// You should have received a copy of the GNU General Public License
	// along with this program; if not, write to the Free Software
	// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
	// ****************************************************************************
	private static void SlowMemCopy(byte[] dst, int dstptr, byte[] src, int srcptr, int nbytes) {

		// NOTE:: DO NOT change this into a system call, the nature of QFS means that it MUST work byte for byte (internal overlaps possible) and in any case this is fast
		while (nbytes > 0) {
			dst[dstptr] = src[srcptr];
			dstptr = dstptr + 1;
			srcptr = srcptr + 1;
			nbytes = nbytes - 1;
		}

	}

	// compressing a QFS file */
	// note: inbuf should have at least 1028 bytes beyond buflen - this is guaranteed from above QFSCompression.CompressBlock, but not if called directly

	// 0=OK,<0 ERROR
	public static int QFS_Compress(ref byte[] Outbuf, byte[] Inbuf, int Offset, ref int Buflen) {

		// Static Buffers
		const int QFS_MAXITER = 50;

		const int WINDOW_LEN = 1 << 17; // 2 ^ 17) '128K
		const int WINDOW_MASK = WINDOW_LEN - 1;

		var rev_similar = new int[131072];    // 128kB indices are file position - these are auto initialized to zero
		var rev_last = new int[256, 256];     // 64kB indices are successive input char bytes

		int InLen = Buflen;
		int InPos = Offset;

		Outbuf = new byte[Information.UBound(Inbuf) + 1028 + 1]; // for now
		Outbuf[0] = 0x10;
		Outbuf[1] = 0xFB;
		Outbuf[2] = (byte) (InLen / 65536);         // HIGH Endian!! max compressed length = 2^24-1 = 1MB
		Outbuf[3] = (byte) (InLen / 256 & 0xFF);
		Outbuf[4] = (byte) (InLen & 0xFF);

		int OutPos = 5;

		int bestlen, blen, offs, bestoffs = default;
		int lastwrot = 0;
		int x, i;

		try {
			// main encoding loop
			var loopTo = InLen - 1;
			for (InPos = 0; InPos <= loopTo; InPos++) {
				// adjust occurrence tables
				x = rev_last[Inbuf[InPos], Inbuf[InPos + 1]] - 1;     // just reduce by one before using
				rev_similar[InPos & WINDOW_MASK] = x + 1;           // and increment by one on storing (wrap around if >than WINDOW SIZE
				rev_last[Inbuf[InPos], Inbuf[InPos + 1]] = InPos + 1; // now the init can be zero (the default)
				offs = x;

				// if this has already been compressed, skip ahead
				if (InPos >= lastwrot) // look for a redundancy (repeat)
				{
					bestlen = 0;
					i = 0;
					while (offs >= 0 & InPos - offs < WINDOW_LEN & i < QFS_MAXITER) {
						blen = 2;
						while (Inbuf[InPos + blen] == Inbuf[offs + blen] & blen < 1028)     // !!! this is why Buffer needs to be 1028> than actual data
							blen = blen + 1;                                                       // do while ((*(incmp++)==*(inref++)) and (blen<1028))
						if (blen > bestlen) {
							bestlen = blen;
							bestoffs = InPos - offs;
						}
						offs = rev_similar[offs & WINDOW_MASK] - 1;        // just reduce by one before using
						i = i + 1;
					}

					// check if (repeat) redundancy is good enough
					if (bestlen > InLen - InPos)
						bestlen = 0; // was (InPos - InLen) effectively -bestlen
					if (bestlen <= 2) {
						bestlen = 0;
					} else if (bestlen == 3 & bestoffs > 1024) {
						bestlen = 0;
					} else if (bestlen == 4 & bestoffs > 16384) {
						bestlen = 0;
					}

					// update compressed data
					if (bestlen != 0) {

						while (InPos - lastwrot >= 4) {
							blen = (InPos - lastwrot) / 4 - 1;
							if (blen > 0x1B)
								blen = 0x1B;
							Outbuf[OutPos] = (byte) (0xE0 + blen);
							OutPos = OutPos + 1;
							blen = 4 * blen + 4;
							SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
							lastwrot = lastwrot + blen;
							OutPos = OutPos + blen;
						}

						blen = InPos - lastwrot;

						if (bestlen <= 10 && bestoffs <= 1024) {
							Outbuf[OutPos] = (byte) ((bestoffs - 1) / 256 * 32 + (bestlen - 3) * 4 + blen);
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (bestoffs - 1 & 0xFF);
							OutPos = OutPos + 1;
							SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
							lastwrot = lastwrot + blen;
							OutPos = OutPos + blen;
							lastwrot = lastwrot + bestlen;
						} else if (bestlen <= 67 && bestoffs <= 16384) {
							Outbuf[OutPos] = (byte) (0x80 + (bestlen - 4));
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (blen * 64 + (bestoffs - 1) / 256);
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (bestoffs - 1 & 0xFF);
							OutPos = OutPos + 1;
							SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
							lastwrot = lastwrot + blen;
							OutPos = OutPos + blen;
							lastwrot = lastwrot + bestlen;
						} else if (bestlen <= 1028 && bestoffs < WINDOW_LEN) {
							bestoffs = bestoffs - 1;
							Outbuf[OutPos] = (byte) (0xC0 + bestoffs / 65536 * 16 + (bestlen - 5) / 256 * 4 + blen);
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (bestoffs / 256 & 0xFF);
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (bestoffs & 0xFF);
							OutPos = OutPos + 1;
							Outbuf[OutPos] = (byte) (bestlen - 5 & 0xFF);
							OutPos = OutPos + 1;
							SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
							lastwrot = lastwrot + blen;
							OutPos = OutPos + blen;
							lastwrot = lastwrot + bestlen;
						}

					}
				}
			}

			// end stuff
			InPos = InLen;
			while (InPos - lastwrot >= 4) {
				blen = (InPos - lastwrot) / 4 - 1;
				if (blen > 0x1B)
					blen = 0x1B;
				Outbuf[OutPos] = (byte) (0xE0 + blen);
				OutPos = OutPos + 1;
				blen = 4 * blen + 4;
				SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
				lastwrot = lastwrot + blen;
				OutPos = OutPos + blen;
			}

			blen = InPos - lastwrot;
			Outbuf[OutPos] = (byte) (0xFC + blen);  // end marker
			OutPos = OutPos + 1;
			SlowMemCopy(Outbuf, OutPos, Inbuf, lastwrot, blen);
			lastwrot = lastwrot + blen;
			OutPos = OutPos + blen;

			if (lastwrot != InLen) {
				Interaction.MsgBox("Something strange happened at the end of QFS compression!");
				return -1;
			}

			Array.Resize(ref Outbuf, OutPos);     // finally
			Buflen = OutPos; // length of the new compressed buffer
			return 0;
		}
		catch {
		}
		return -1;

	}

	// /* uncompressing a QFS file */
	// Compressed passed in with INBuf() and returned with OutBuf(): DOffset may be 0 or 4 if 4 first 4bytes are length
	// Outbuf is explicitly reallocated
	public static int QFS_Uncompress(byte[] Inbuf, int Doffset, int InLen, ref byte[] Outbuf) // =0=OK
	{

		int PackCode, CodeA, CodeB, CodeC;
		int nbytes, InPos, outlen, OutPos, Offset;

		// /* length of data */
		outlen = Inbuf[2 + Doffset] * 65536 + Inbuf[3 + Doffset] * 256 + Inbuf[4 + Doffset];
		Outbuf = new byte[outlen];

		// /* position in file */
		if ((Inbuf[Doffset] & 0x1) != 0)
			InPos = Doffset + 8;
		else
			InPos = Doffset + 5;

		OutPos = 0;
		try {
			// /* main decoding loop */
			while (InPos < InLen) {
				if (Inbuf[InPos] >= 0xFC)
					break;

				PackCode = Inbuf[InPos];
				CodeA = Inbuf[InPos + 1];
				CodeB = Inbuf[InPos + 2];

				if ((PackCode & 0x80) == 0)                               // 0.xx.yyy.nn aaaaaaaa
				{
					nbytes = PackCode & 0x3;
					SlowMemCopy(Outbuf, OutPos, Inbuf, InPos + 2, nbytes);
					InPos = InPos + nbytes + 2;
					OutPos = OutPos + nbytes;

					nbytes = (PackCode & 0x1C) / 4 + 3;
					Offset = PackCode / 32 * 256 + CodeA + 1;
					SlowMemCopy(Outbuf, OutPos, Outbuf, OutPos - Offset, nbytes);
					OutPos = OutPos + nbytes;
				} else if ((PackCode & 0x40) == 0)                           // 10.yyyyyy nn.aaaaaa bbbbbbbb
				  {
					nbytes = CodeA / 64 & 0x3;
					SlowMemCopy(Outbuf, OutPos, Inbuf, InPos + 3, nbytes);
					InPos = InPos + nbytes + 3;
					OutPos = OutPos + nbytes;

					nbytes = (short) (PackCode & 0x3F) + 4;
					Offset = (short) (CodeA & 0x3F) * 256 + CodeB + 1;
					SlowMemCopy(Outbuf, OutPos, Outbuf, OutPos - Offset, nbytes);
					OutPos = OutPos + nbytes;
				} else if ((PackCode & 0x20) == 0)                           // 110.z.ww.nn aaaaaaaa*256 bbbbbbbb 256ww+cccccccc+5
				  {
					CodeC = Inbuf[InPos + 3];
					nbytes = PackCode & 0x3;
					SlowMemCopy(Outbuf, OutPos, Inbuf, InPos + 4, nbytes);
					InPos = InPos + nbytes + 4;
					OutPos = OutPos + nbytes;

					nbytes = (short) (PackCode / 4 & 0x3) * 256 + CodeC + 5;
					Offset = (short) (PackCode & 0x10) * 4096 + 256 * CodeA + CodeB + 1;
					SlowMemCopy(Outbuf, OutPos, Outbuf, OutPos - Offset, nbytes);
					OutPos = OutPos + nbytes;
				} else // literal copy of 4k+4  0..128 bytes                     '111xxxxx
				  {
					nbytes = (short) (PackCode & 0x1F) * 4 + 4;
					SlowMemCopy(Outbuf, OutPos, Inbuf, InPos + 1, nbytes);
					InPos = InPos + nbytes + 1;
					OutPos = OutPos + nbytes;
				}
			}

			// /* trailing bytes */
			if (InPos < InLen & OutPos < outlen) {
				nbytes = Inbuf[InPos] & 0x3;
				SlowMemCopy(Outbuf, OutPos, Inbuf, InPos + 1, nbytes);
				OutPos = OutPos + nbytes;
			}

			if (OutPos != outlen) {
				Interaction.MsgBox("Warning: bad length ? " + OutPos + " instead of " + outlen);
				return -1;
			}
			return 0;
		}
		catch {
		}

		Interaction.MsgBox("Strange problems with array bounds in QFS decompress");
		return -1;

	}

}