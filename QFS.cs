using System;
using System.Numerics;


namespace QFS {
	/// <summary>
	/// Implementation of QFS/RefPack/LZ77 decompression. This compression is used on larger entries inside saves
	/// </summary>
	/// <remarks>
	/// Note that this implementation contains control characters and other changes specific to SimCity 4.
	/// You can read about other game specifics at this specification for QFS spec http://wiki.niotso.org/RefPack.
	///
	/// Ported from https://github.com/wouanagaine/SC4Mapper-2013/blob/db29c9bf88678a144dd1f9438e63b7a4b5e7f635/Modules/qfs.c#L25 and https://github.com/0xC0000054/DBPFSharp/blob/main/src/DBPFSharp/QfsCompression.cs
	///
	/// More information on file specification:
	/// - https://www.wiki.sc4devotion.com/index.php?title=DBPF_Compression
	/// - http://wiki.niotso.org/RefPack#Naming_notes
	/// </remarks>
	public class QFS {
		/// <summary>
		/// All QFS compressed items will contain this signature in the file header.
		/// </summary>
		private const ushort QFS_Signature = 0xFB10;



		/// <summary>
		/// Check if the data is compressed.
		/// </summary>
		/// <param name="entryData">Data to check</param>
		/// <returns>TRUE if data is compressed; FALSE otherwise</returns>
		public static bool IsCompressed(byte[] entryData) {
			if (entryData.Length > 6) {
				if (BitConverter.ToUInt16(entryData, 4) == QFS_Signature) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns data's decompressed length in bytes.
		/// </summary>
		/// <param name="cData">Data to check</param>
		/// <returns>Size of decompressed data. If data is not compressed, the raw size is returned.</returns>
		public static uint GetDecompressedSize(byte[] cData) {
			if (IsCompressed(cData)) {
				//First 4 bytes is always the size of header + compressed data

				// Read 5 byte header
				byte[] header = new byte[5];
				for (int idx = 0; idx < 5; idx++) {
					header[idx] = cData[idx + 4];
				}

				// After QFS identifier, next 3 bytes are the decompressed size ... byte shift most significant byte to least
				return Convert.ToUInt32((header[2] << 16) + (header[3] << 8) + header[4]);
			} else {
				return (uint) cData.Length;
			}
		}

		/// <summary>
		/// Decompress data compressed with QFS/RefPack compression.
		/// </summary>
		/// <param name="sourceBytes">Compressed data array</param>
		/// <returns>Decompressed data array</returns>
		/// <example>
		/// <c>
		/// // Load save game
		/// SC4SaveFile savegame = new SC4SaveFile(@"C:\Path\To\Save\Game.sc4");
		///
		/// // Read raw data for Region View Subfile from save
		/// byte[] data = sc4Save.LoadIndexEntryRaw(REGION_VIEW_SUBFILE_TGI);
		///
		/// // Decompress data (This file will normally be compressed, should ideally check before decompressing)
		/// byte[] decompressedData = QFS.UncompressData(data);
		/// </c>
		/// </example>
		/// <exception cref="System.IndexOutOfRangeException">
		/// Thrown when the compression algorithm tries to access an element that is out of bounds in the array
		/// </exception>
		public static byte[] Decompress(byte[] sourceBytes) {
			if (!IsCompressed(sourceBytes)) {
				return sourceBytes;
			}

			byte[] destinationBytes;
			int destinationPosition = 0;

			// Check first 4 bytes (size of header + compressed data)
			uint compressedSize = BitConverter.ToUInt32(sourceBytes, 0);

			// Next read the 5 byte header
			byte[] header = new byte[5];
			for (int i = 0; i < 5; i++) {
				header[i] = sourceBytes[i + 4];
			}

			// First 2 bytes should be the QFS identifier
			// Next 3 bytes should be the uncompressed size of file
			// (we do this by byte shifting (from most significant byte to least))
			// the last 3 bytes of the header to make a number)
			uint uncompressedSize = Convert.ToUInt32((long) (header[2] << 16) + (header[3] << 8) + header[4]); ;

			// Create our destination array
			destinationBytes = new byte[uncompressedSize];

			// Next set our position in the file
			// (The if checks if the first 4 bytes are the size of the file
			// if so our start position is 4 bytes + 5 byte header if not then our
			// offset is just the header (5 bytes))
			//if ((sourceBytes[0] & 0x01) != 0)
			//{
			//    sourcePosition = 9;//8;
			//}
			//else
			//{
			//    sourcePosition = 5;
			//}

			// Above code is redundant for SimCity 4 saves as the QFS compressed files all have the same header length
			// (Check was throwing off start position and caused decompression to get buggered)
			int sourcePosition = 9;

			// In QFS the control character tells us what type of decompression operation we are going to perform (there are 4)
			// Most involve using the bytes proceeding the control byte to determine the amount of data that should be copied from what
			// offset. These bytes are labeled a, b and c. Some operations only use 1 proceeding byte, others can use 3
			byte ctrlByte1;
			byte ctrlByte2;
			byte ctrlByte3;
			byte ctrlByte4;
			int length;
			int offset;

			// Main decoding loop. Keep decoding while sourcePosition is in source array and position isn't 0xFC
			while ((sourcePosition < sourceBytes.Length) && (sourceBytes[sourcePosition] < 0xFC)) {
				// Read our packcode/control character
				ctrlByte1 = sourceBytes[sourcePosition];

				// Read bytes proceeding packcode
				ctrlByte2 = sourceBytes[sourcePosition + 1];
				ctrlByte3 = sourceBytes[sourcePosition + 2];

				// Check which packcode type we are dealing with
				// Control Characters 0 to 127 (2 byte length CC)
				if ((ctrlByte1 & 0x80) == 0) {
					// First we copy from the source array to the destination array
					length = ctrlByte1 & 3;
					LZCompliantCopy(ref sourceBytes, sourcePosition + 2, ref destinationBytes, destinationPosition, length);

					// Then we copy characters already in the destination array to our current position in the destination array
					sourcePosition += length + 2;
					destinationPosition += length;
					length = ((ctrlByte1 & 0x1C) >> 2) + 3;
					offset = ((ctrlByte1 >> 5) << 8) + ctrlByte2 + 1;
					LZCompliantCopy(ref destinationBytes, destinationPosition - offset, ref destinationBytes, destinationPosition, length);

					destinationPosition += length;
				}

				// Control Characters 128 to 191 (3 byte length CC)
				else if ((ctrlByte1 & 0x40) == 0) {
					length = (ctrlByte2 >> 6) & 3;
					LZCompliantCopy(ref sourceBytes, sourcePosition + 3, ref destinationBytes, destinationPosition, length);

					sourcePosition += length + 3;
					destinationPosition += length;
					length = (ctrlByte1 & 0x3F) + 4;
					offset = (ctrlByte2 & 0x3F) * 256 + ctrlByte3 + 1;
					LZCompliantCopy(ref destinationBytes, destinationPosition - offset, ref destinationBytes, destinationPosition, length);

					destinationPosition += length;
				}

				// Control Characters 192 to 223 (4 byte length CC)
				else if ((ctrlByte1 & 0x20) == 0) {
					ctrlByte4 = sourceBytes[sourcePosition + 3];

					length = ctrlByte1 & 3;
					LZCompliantCopy(ref sourceBytes, sourcePosition + 4, ref destinationBytes, destinationPosition, length);

					sourcePosition += length + 4;
					destinationPosition += length;
					length = ((ctrlByte1 >> 2) & 3) * 256 + ctrlByte4 + 5;
					offset = ((ctrlByte1 & 0x10) << 12) + 256 * ctrlByte2 + ctrlByte3 + 1;
					LZCompliantCopy(ref destinationBytes, destinationPosition - offset, ref destinationBytes, destinationPosition, length);

					destinationPosition += length;
				}

				// Control Characters 224 to 251 (1 byte length CC)
				else {
					length = (ctrlByte1 & 0x1F) * 4 + 4;
					LZCompliantCopy(ref sourceBytes, sourcePosition + 1, ref destinationBytes, destinationPosition, length);

					sourcePosition += length + 1;
					destinationPosition += length;
				}
			}

			// Add trailing bytes
			// Control Characters 252 to 255 (1 byte length CC)
			if ((sourcePosition < sourceBytes.Length) && (destinationPosition < destinationBytes.Length)) {
				LZCompliantCopy(ref sourceBytes, sourcePosition + 1, ref destinationBytes, destinationPosition, sourceBytes[sourcePosition] & 3);
				destinationPosition += sourceBytes[sourcePosition] & 3;
			}

			if (destinationPosition != destinationBytes.Length) {
				//Logger.Log(LogLevel.Warning, "QFS bad length, {0} instead of {1}", destinationPosition, destinationBytes.Length);
			}

			return destinationBytes;
		}





        //https://en.wikipedia.org/wiki/LZ77_and_LZ78#LZ77
        //https://www.wiki.sc4devotion.com/index.php?title=DBPF_Compression
        //LZ77 algorithms achieve compression by replacing repeated occurrences of data with references to a single copy of that data existing earlier in the uncompressed data stream. A match is encoded by a pair of numbers called a length-distance pair, which is equivalent to the statement "each of the next length characters is equal to the characters exactly distance characters behind it in the uncompressed stream". (The distance is sometimes called the offset instead.) For example, if the word "heureka" occurs twice in a file, the second occurrence would be encoded by pointing to the first, thus lowering the size of the file. 
        //To spot matches, the encoder must keep track of some amount of the most recent data. The structure in which this data is held is called a sliding window, which is why LZ77 is sometimes called sliding-window compression. The encoder needs to keep this data to look for matches, and the decoder needs to keep this data to interpret the matches the encoder refers to. The larger the sliding window is, the longer back the encoder may search for creating references.
        //The compression is done by defining control characters that tell three things: 1) How many characters of plain text that follow should be appended to the output. 2) How many characters should be read from the already decoded text (and appended to the output). 3) Where to read the characters from in the already decoded text.
        //Pseudo code:
        /* 
		 * while input is not empty do
		 *		match := longest repeated occurrence of input that begins in window
		 *		
		 *		if match exists then
		 *			d := distance to start of match
		 *			l := length of match
		 *			c := char following match in input
		 *		else
		 *			d := 0
		 *			l := 0
		 *			c := first char of input
		 *		end if
		 *		
		 *		output (d, l, c)
		 *		
		 *		discard l + 1 chars from front of window
		 *		s := pop l + 1 chars from front of input
		 *		append s to back of window
		 *	repeat
		 */
        /*https://www.youtube.com/watch?v=EFUYNoFRHQI
		 * 
		 * 
		 * 
		 * 
		 * 
		 * 
		 */



        //basic components: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-drsr/2ea1ab25-99b4-489b-a71c-61e4c196b561
        /* input stream: dData
         * coding position: position of input stream (aka start of lookahead buffer)
         * lookahead buffer: sequence from coding position to end of input
         * window: buffer of number of bytes from coding position backwards. these are all processed bytes
         * pointer: info about the beginning of the match in the window and its length
         * match: string used to find a match of the byte sequence between lookahead buffer and window
         * 
         */


        /// <summary>
        /// Compress data with QFS/RefPack compression.
        /// </summary>
        /// <param name="dData">Data to compress</param>
        /// <returns>Compressed data array</returns>
        /// <exception cref="Exception">If error occurred during compression</exception>
        public static byte[] Compress(byte[] dData) {
			if (IsCompressed(dData)) {
				return dData;
			}

			// Static Buffers
			const int QFS_MAXITER = 50;
			const int WINDOW_SIZE = 1 << 17; // 2 ^ 17) '131072
			const int WINDOW_MASK = WINDOW_SIZE - 1;

			//these are the occurrence tables.
			int[] rev_similar = new int[WINDOW_SIZE];    // indexes are file position - auto initialized to zero				 
			int[,] rev_last = new int[256, 256]; //64kB indexes are successive input char bytes

            int dPos;
			byte[] cData = new byte[dData.Length + 1028]; // for now

			//First 4 bytes are size of next 5 bytes + compressed data. we don't know this yet so for now start with the QFS identifier and shift everything 4 bytes at the very end once the size is calculated.
			//5 byte header: 10FB then uncompressed file size
			cData[0] = 0x10;
			cData[1] = 0xFB;
			cData[2] = (byte) ((dData.Length >> 16) & 0xff); //convert to big endian. only get last 8 bytes of the value
			cData[3] = (byte) ((dData.Length >> 8) & 0xff);
			cData[4] = (byte) (dData.Length & 0xff);
			int cPos = 5;

			int bestlength, blen, offset, bestoffset = default;
			int lastwrote = 0;
			int x, idx;
			int tmpx, tmpy;

			// main encoding loop. process one byte at a time
			for (dPos = 0; dPos < dData.Length-1; dPos++) {
				// adjust occurrence tables
				x = rev_last[dData[dPos], dData[dPos + 1]] - 1; //just reduce by one before using
				rev_similar[dPos & WINDOW_MASK] = x + 1; //and increment by one on storing (wrap around if >than WINDOW SIZE

				tmpx = dData[dPos];
				tmpy = dData[dPos + 1];
				//rev_last is a matrix storing the most recent position a given set of two characters (denoted from the x,y coordinates) is at
				//e.g. dPos = 0, dData[0]=114 ("r"), dData[1]=101 ("e"), rev_last[114,101]=1
				rev_last[tmpx, tmpy] = dPos + 1; //now the init can be zero (the default)
				offset = x;

				// if this has already been compressed, skip ahead
				
				if (dPos >= lastwrote) {
					// look for a redundancy (repeat)
					bestlength = 0;
					idx = 0;

					//this will skip until the first repetition is encountered
					while (offset >= 0 & dPos - offset < WINDOW_SIZE & idx < QFS_MAXITER) {
						blen = 2;

						//this loop figures the length of the matching sequence, writing to blen
						while (dData[dPos + blen] == dData[offset + blen] & blen < 1028) { // !!! this is why Buffer needs to be 1028> than actual data
							blen++; // do while ((*(incmp++)==*(inref++)) and (blen<1028))
						}
						if (blen > bestlength) {
							bestlength = blen;
							bestoffset = dPos - offset;
						}
						offset = rev_similar[offset & WINDOW_MASK] - 1; // just reduce by one before using
						idx++;
					}

					// check if (repeat) redundancy is good enough
					if (bestlength > dData.Length - dPos)
						bestlength = 0; // was (InPos - InLen) effectively - best length
					if (bestlength <= 2) {
						bestlength = 0;
					} else if (bestlength == 3 & bestoffset > 1024) {
						bestlength = 0;
					} else if (bestlength == 4 & bestoffset > 16384) {
						bestlength = 0;
					}

					// update compressed data
					if (bestlength != 0) {

						//write the number of plain text at the very start that will never have a match
						while (dPos - lastwrote >= 4) {
							//write the control character (blen) to the data
							blen = (dPos - lastwrote) / 4 - 1;
							if (blen > 0x1B)
								blen = 0x1B;
							cData[cPos] = (byte) (0xE0 + blen);
							cPos++;
							blen = 4 * blen + 4;
							SlowMemCopy(cData, cPos, dData, lastwrote, blen);
							lastwrote += blen; //last position of the dData wrote
							cPos += blen;
						}

						//now write the matches
						//blen is now the length of plain text
						blen = dPos - lastwrote;

						//2 byte control character
						if (bestlength <= 10 && bestoffset <= 1024) {
							cData[cPos] = (byte) ((bestoffset - 1) / 256 * 32 + (bestlength - 3) * 4 + blen);
							cPos++;
							cData[cPos] = (byte) (bestoffset - 1 & 0xFF);
							cPos++;
							SlowMemCopy(cData, cPos, dData, lastwrote, blen); //write the length of plain text
							lastwrote += blen;
							cPos += blen;
							lastwrote += bestlength; //update the last write length to skip over the matched bytes
						} 
						

						else if (bestlength <= 67 && bestoffset <= 16384) {
							cData[cPos] = (byte) (0x80 + (bestlength - 4));
							cPos++;
							cData[cPos] = (byte) (blen * 64 + (bestoffset - 1) / 256);
							cPos++;
							cData[cPos] = (byte) (bestoffset - 1 & 0xFF);
							cPos++;
							SlowMemCopy(cData, cPos, dData, lastwrote, blen);
							lastwrote += blen;
							cPos += blen;
							lastwrote += bestlength;
						} 
						

						else if (bestlength <= 1028 && bestoffset < WINDOW_SIZE) {
							bestoffset--;
							cData[cPos] = (byte) (0xC0 + bestoffset / 65536 * 16 + (bestlength - 5) / 256 * 4 + blen);
							cPos++;
							cData[cPos] = (byte) (bestoffset / 256 & 0xFF);
							cPos++;
							cData[cPos] = (byte) (bestoffset & 0xFF);
							cPos++;
							cData[cPos] = (byte) (bestlength - 5 & 0xFF);
							cPos++;
							SlowMemCopy(cData, cPos, dData, lastwrote, blen);
							lastwrote += blen;
							cPos += blen;
							lastwrote += bestlength;
						}
					}
				}
			}

			// end stuff
			dPos = dData.Length;
			while (dPos - lastwrote >= 4) {
				blen = (dPos - lastwrote) / 4 - 1;
				if (blen > 0x1B) {
                    blen = 0x1B;
                }
				cData[cPos] = (byte) (0xE0 + blen);
				cPos++;
				blen = 4 * blen + 4;
				SlowMemCopy(cData, cPos, dData, lastwrote, blen);
				lastwrote += blen;
				cPos += blen;
			}

			blen = dPos - lastwrote;
			cData[cPos] = (byte) (0xFC + blen);  // end marker
			cPos++;
			SlowMemCopy(cData, cPos, dData, lastwrote, blen);
			lastwrote += blen;
			cPos += blen;

			if (lastwrote != dData.Length) {
				throw new Exception("Something strange happened at the end of QFS compression!");
			}
            Array.Resize(ref cData, cPos);

			//Add the final byte size to the very start
			byte[] output = new byte[cData.Length+4];
			Array.Copy(cData, 0, output, 4, cData.Length);
			byte[] size = BitConverter.GetBytes(output.Length);
			Array.Copy(size, 0, output, 0, 4);
			return output;
		}

		private static void SlowMemCopy(byte[] dst, int dstptr, byte[] src, int srcptr, int nbytes) {
			// NOTE:: DO NOT change this into a system call, the nature of QFS means that it MUST work byte for byte (internal overlaps possible) and in any case this is fast
			while (nbytes > 0) {
				dst[dstptr] = src[srcptr];
				dstptr++;
				srcptr++;
				nbytes--;
			}
		}



		/// <summary>
		/// Method that implements LZ compliant copying of data between arrays
		/// </summary>
		/// <param name="source">Array to copy from</param>
		/// <param name="sourceOffset">Position in array to copy from</param>
		/// <param name="destination">Array to copy to</param>
		/// <param name="destinationOffset">Position in array to copy to</param>
		/// <param name="length">Amount of data to copy</param>
		/// <remarks>
		/// With QFS (LZ77) we require an LZ compatible copy method between arrays, meaning we copy stuff one byte at a time between arrays. With LZ compatible algorithms, it is completely legal to copy over data that overruns the currently filled position in the destination array. In other words it is more than likely the we will be asked to copy over data that hasn't been copied yet. It's confusing, so we copy things one byte at a time.
		/// </remarks>
		/// <exception cref="System.IndexOutOfRangeException">
		/// Thrown when the copy method tries to access an element that is out of bounds in the array
		/// </exception>
		private static void LZCompliantCopy(ref byte[] source, int sourceOffset, ref byte[] destination, int destinationOffset, int length) {
			if (length != 0) {
				for (int i = 0; i < length; i++) {
					Buffer.BlockCopy(source, sourceOffset, destination, destinationOffset, 1);
					sourceOffset++;
					destinationOffset++;
				}
			}
		}
	}
}