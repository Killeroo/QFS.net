using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QFS
{
	/// <summary>
	/// Implementation of QFS/RefPack/LZ77 decompression. This compression is used on larger entries inside saves
	/// </summary>
	/// <remarks>
	/// Note that this implementaiton contains control characters and other changes specific to SimCity 4.
	/// You can read about other game specifics at thsi specification for QFS spec http://wiki.niotso.org/RefPack.
	///
	/// Ported from https://github.com/wouanagaine/SC4Mapper-2013/blob/db29c9bf88678a144dd1f9438e63b7a4b5e7f635/Modules/qfs.c#L25 and https://github.com/0xC0000054/DBPFSharp/blob/main/src/DBPFSharp/QfsCompression.cs
	///
	/// More information on file specification:
	/// - https://www.wiki.sc4devotion.com/index.php?title=DBPF_Compression
	/// - http://wiki.niotso.org/RefPack#Naming_notes
	/// </remarks>
	public class QFS
    {
        /// <summary>
        /// All QFS compressed items will contain this signature in the file header.
        /// </summary>
        private const ushort QFS_Signature = 0xFB10;
		/// <summary>
		/// Minimum byte size that can be compressed.
		/// </summary>
		/// <remarks>
		/// This is an optimization to skip very small files as the SC4 QFS format uses a 9 byte header.
		/// </remarks>
		public const int MinUncompresedSize = 10;
		/// <summary>
		/// Maximum byte size that can be compressed.
		/// </summary>
		/// <remarks>
		/// The SC4 QFS format represents the uncompressed length using a 3 byte unsigned int.
		/// </remarks>
		public const int MaxUncompressedSize = 16777215;



		/// <summary>
		/// Check if the data is compressed.
		/// </summary>
		/// <param name="entryData">Data to check</param>
		/// <returns>TRUE if data is compressed; FALSE otherwise</returns>
		public static bool IsCompressed(byte[] entryData)
        {
            if (entryData.Length > 6)
            {
                if (BitConverter.ToUInt16(entryData, 4) == QFS_Signature)
                {
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
        public static uint GetDecompressedSize(byte[] cData)
        {
            if (IsCompressed(cData))
            {
                //First 4 bytes is always the size of header + compressed data

                // Read 5 byte header
                byte[] header = new byte[5];
                for (int idx = 0; idx < 5; idx++)
                {
                    header[idx] = cData[idx + 4];
                }

                // After QFS identifier, next 3 bytes are the decompressed size ... byte shift most significant byte to least
                return Convert.ToUInt32((header[2] << 16) + (header[3] << 8) + header[4]);
            }
            else
            {
                return (uint)cData.Length;
            }
        }

        /// <summary>
        /// Decompress data compressed with QFS/RefPack compression.
        /// </summary>
        /// <param name="data">Compressed data array</param>
        /// <returns>Decompressed data array</returns>
        /// <example>
        /// <c>
        /// // Load save game
        /// SC4SaveFile savegame = new SC4SaveFile(@"C:\Path\To\Save\Game.sc4");
        ///
        /// // Read raw data for Region View Subfile from save
        /// byte[] data = sc4Save.LoadIndexEntryRaw(REGION_VIEW_SUBFILE_TGI);
        ///
        /// // Decompress data (This file will normally be compressed, should idealy check before decompressing)
        /// byte[] decompressedData = QFS.UncompressData(data);
        /// </c>
        /// </example>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when the compression algorithm tries to access an element that is out of bounds in the array
        /// </exception>
        public static byte[] Decompress(byte[] data)
        {
            byte[] sourceBytes = data;
            byte[] destinationBytes;
            int sourcePosition = 0;
            int destinationPosition = 0;

            // Check first 4 bytes (size of header + compressed data)
            uint compressedSize = BitConverter.ToUInt32(sourceBytes, 0);

            // Next read the 5 byte header
            byte[] header = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                header[i] = sourceBytes[i + 4];
            }

            // First 2 bytes should be the QFS identifier
            // Next 3 bytes should be the uncompressed size of file
            // (we do this by byte shifting (from most significant byte to least))
            // the last 3 bytes of the header to make a number)
            uint uncompressedSize = Convert.ToUInt32((long)(header[2] << 16) + (header[3] << 8) + header[4]); ;

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
            sourcePosition = 9;

            // In QFS the control character tells us what type of decompression operation we are going to perform (there are 4)
            // Most involve using the bytes proceeding the control byte to determine the amount of data that should be copied from what
            // offset. These bytes are labled a, b and c. Some operations only use 1 proceeding byte, others can use 3
            byte ctrlByte1;
            byte ctrlByte2;
            byte ctrlByte3;
            byte ctrlByte4;
            int length;
            int offset;

            // Main decoding loop. Keep decoding while sourcePosition is in source array and position isn't 0xFC
            while ((sourcePosition < sourceBytes.Length) && (sourceBytes[sourcePosition] < 0xFC))
            {
                // Read our packcode/control character
                ctrlByte1 = sourceBytes[sourcePosition];

                // Read bytes proceeding packcode
                ctrlByte2 = sourceBytes[sourcePosition + 1];
                ctrlByte3 = sourceBytes[sourcePosition + 2];

				// Check which packcode type we are dealing with
				// Control Characters 0 to 127 (2 byte length CC)
				if ((ctrlByte1 & 0x80) == 0)
                {
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
				else if ((ctrlByte1 & 0x40) == 0)
                {
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
				else if ((ctrlByte1 & 0x20) == 0)
                {
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
			if ((sourcePosition < sourceBytes.Length) && (destinationPosition < destinationBytes.Length))
            {
                LZCompliantCopy(ref sourceBytes, sourcePosition + 1, ref destinationBytes, destinationPosition, sourceBytes[sourcePosition] & 3);
                destinationPosition += sourceBytes[sourcePosition] & 3;
            }

            if (destinationPosition != destinationBytes.Length)
            {
                //Logger.Log(LogLevel.Warning, "QFS bad length, {0} instead of {1}", destinationPosition, destinationBytes.Length);
            }

            return destinationBytes;
		}



		/// <summary>
		/// Compress data using QFS/RefPack compression
		/// </summary>
		/// <param name="dData"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method has been adapted from deflate.c in zlib version 1.2.3. It can produce smaller files than the FSHTool QFS compression code. null45 is fairly certain the idea was borrowed from another QFS implementation, but the original source is unknown. https://community.simtropolis.com/forums/topic/762189-simcity-4-open-access-repository-of-modding-tools/?do=findComment&comment=1777854
		/// </remarks>
		public static byte[] Compress(byte[] dData) {
			if (dData.Length < MinUncompresedSize || dData.Length > MaxUncompressedSize) {
				return dData;
			}
			return new QFSCompress(dData).Compress();
		}





		/// <summary>
		/// A helper class specifically for the compression algorithm to condense all of the fields together in a central location and not clutter the main QFS class.
		/// </summary>
		private class QFSCompress {
			//https://en.wikipedia.org/wiki/LZ77_and_LZ78#LZ77
			//https://www.wiki.sc4devotion.com/index.php?title=DBPF_Compression
			//LZ77 algorithms achieve compression by replacing repeated occurrences of data with references to a single copy of that data existing earlier in the uncompressed data stream. A match is encoded by a pair of numbers called a length-distance pair, which is equivalent to the statement "each of the next length characters is equal to the characters exactly distance characters behind it in the uncompressed stream". (The distance is sometimes called the offset instead.) For example, if the word "heureka" occurs twice in a file, the second occurrence would be encoded by pointing to the first, thus lowering the size of the file. 
			//To spot matches, the encoder must keep track of some amount of the most recent data. The structure in which this data is held is called a sliding window, which is why LZ77 is sometimes called sliding-window compression. The encoder needs to keep this data to look for matches, and the decoder needs to keep this data to interpret the matches the encoder refers to. The larger the sliding window is, the longer back the encoder may search for creating references.
			//The compression is done by defining control characters that tell three things: 1) How many characters of plain text that follow should be appended to the output. 2) How many characters should be read from the already decoded text (and appended to the output). 3) Where to read the characters from in the already decoded text.
			//Pseudocode:
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

			private const int LiteralRunMaxLength = 112;
			private const int MaxWindowSize = 131072;
			private const int MaxHashSize = 65536;
			private const int GoodLength = 32;
			private const int MaxLazy = 258;
			private const int NiceLength = 258;
			private const int MaxChain = 4096;
			private const int MinMatch = 3;
			private const int MaxMatch = 1028;

			private readonly byte[] dData; //Decompressed data = input data
			private byte[] cData; //Compressed data = output data
			private int cPos; //position we're at in the output data
			private int readPos; //aka dPos or position we're at in the input data
			private int lastWritePos;
			private int remaining; //number of bytes left to be read from dData

			private int hash;
			private readonly int[] head;
			private readonly int[] prev;

			private readonly int windowSize;
			private readonly int windowMask;
			private readonly int maxWindowOffset;
			private readonly int hashSize;
			private readonly int hashMask;
			private readonly int hashShift;

			private int matchStart;
			private int matchLength; //number of characters to use for determining a match with previously compressed data
			private int prevLength;

			public QFSCompress(byte[] data) {
				dData = data;
				cData = new byte[dData.Length-1];

				if (dData.Length < MaxWindowSize) {
					windowSize = 1 << BitOperations.Log2((uint) dData.Length);
					hashSize = Math.Max(windowSize / 2, 32);
					hashShift = (BitOperations.TrailingZeroCount(hashSize) + MinMatch - 1) / MinMatch;
				} else {
					windowSize = MaxWindowSize;
					hashSize = MaxHashSize;
					hashShift = 6;
				}
				maxWindowOffset = windowSize - 1;
				windowMask = maxWindowOffset;
				hashMask = hashSize - 1;

				hash = 0;
				head = new int[hashSize];
				prev = new int[windowSize];

				readPos = 0;
				remaining = dData.Length;
				cPos = 5; //QFS header size is 5 bytes
				lastWritePos = 0;
				Array.Fill(head, -1);
			}


			/// <summary>
			/// Compresses this instance.
			/// </summary>
			/// <returns></returns>
			/// <remarks>
			/// This method has been adapted from deflate.c in zlib version 1.2.3.
			/// </remarks>
			public byte[] Compress() {
				hash = ((hash << hashShift) ^ dData[1]) & hashMask;
				int lastMatch = dData.Length - MinMatch;

				while (remaining > 0) {
					matchLength = MinMatch - 1;
					prevLength = matchLength;
					int prev_match = matchStart;

					int hash_head = -1;

					// Insert the string window[readPosition .. readPosition+2] in the
					// dictionary, and set hash_head to the head of the hash chain:
					if (remaining >= MinMatch) {
						hash = ((hash << hashShift) ^ dData[readPos + MinMatch - 1]) & hashMask;

						hash_head = head[hash];
						prev[readPos & windowMask] = hash_head;
						head[hash] = readPos;
					}


					if (hash_head >= 0 && prevLength < MaxLazy && readPos - hash_head <= windowSize) {
						int bestLength = LongestMatch(hash_head);

						if (bestLength >= MinMatch) {
							int bestOffset = readPos - matchStart;

							if (bestOffset <= 1024 ||
								bestOffset <= 16384 && bestLength >= 4 ||
								bestOffset <= windowSize && bestLength >= 5) {
								matchLength = bestLength;
							}
						}
					}

					// If there was a match at the previous step and the current
					// match is not better, output the previous match:
					if (prevLength >= MinMatch && matchLength <= prevLength) {
						if (!WriteCompressedData(prev_match)) {
							return null;
						}

						// Insert in hash table all strings up to the end of the match.
						// readPosition-1 and readPosition are already inserted. If there is not
						// enough lookahead, the last two strings are not inserted in
						// the hash table.

						remaining -= (prevLength - 1);
						prevLength -= 2;

						do {
							readPos++;

							if (readPos < lastMatch) {
								hash = ((hash << hashShift) ^ dData[readPos + MinMatch - 1]) & hashMask;

								hash_head = head[hash];
								prev[readPos & windowMask] = hash_head;
								head[hash] = readPos;
							}
							prevLength--;
						}
						while (prevLength > 0);

						matchLength = MinMatch - 1;
						readPos++;
					} else {
						readPos++;
						remaining--;
					}
				}

				if (!WriteTrailingBytes()) {
					return null;
				}

				// Write the compressed data header.
				cData[0] = 0x10;
				cData[1] = 0xFB;
				cData[2] = (byte) ((dData.Length >> 16) & 0xff);
				cData[3] = (byte) ((dData.Length >> 8) & 0xff);
				cData[4] = (byte) (dData.Length & 0xff);

				// Trim the output array to its actual size.
				int finalLength = cPos + 4;
				if (finalLength >= dData.Length) {
					return null;
				}

				byte[] temp = new byte[finalLength];

				// Write the compressed data length in little endian byte order.
				temp[0] = (byte) (cPos & 0xff);
				temp[1] = (byte) ((cPos >> 8) & 0xff);
				temp[2] = (byte) ((cPos >> 16) & 0xff);
				temp[3] = (byte) ((cPos >> 24) & 0xff);

				Buffer.BlockCopy(cData, 0, temp, 4, cPos);
				cData = temp;
				

				return cData;
			}


			/// <summary>
			/// Writes the compressed data.
			/// </summary>
			/// <param name="startOffset">The start offset.</param>
			/// <returns>
			/// <see langword="true"/> if the data was compressed; otherwise, <see langword="false"/>.
			/// </returns>
			private bool WriteCompressedData(int startOffset) {
				int endOffset = readPos - 1;
				int run = endOffset - lastWritePos;

				while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
				{
					int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);

					if ((cPos + blockLength + 1) >= cData.Length) {
						return false; // data did not compress
					}

					cData[cPos] = (byte) (0xE0 + ((blockLength / 4) - 1));
					cPos++;

					// A for loop is faster than Buffer.BlockCopy for data less than or equal to 32 bytes.
					if (blockLength <= 32) {
						for (int i = 0; i < blockLength; i++) {
							cData[cPos] = dData[lastWritePos];
							lastWritePos++;
							cPos++;
						}
					} else {
						Buffer.BlockCopy(dData, lastWritePos, cData, cPos, blockLength);
						lastWritePos += blockLength;
						cPos += blockLength;
					}

					run -= blockLength;
				}

				int copyLength = this.prevLength;
				// Subtract one before encoding the copy offset, the QFS decompression algorithm adds it back when decoding.
				int copyOffset = endOffset - startOffset - 1;

				if (copyLength <= 10 && copyOffset < 1024) // 2 byte op code  0x00 - 0x7f
				{
					if ((cPos + run + 2) >= cData.Length) {
						return false; // data did not compress
					}

					cData[cPos] = (byte) ((((copyOffset >> 8) << 5) + ((copyLength - 3) << 2)) + run);
					cData[cPos + 1] = (byte) (copyOffset & 0xff);
					cPos += 2;
				} else if (copyLength <= 67 && copyOffset < 16384)  // 3 byte op code 0x80 - 0xBF
				  {
					if ((cPos + run + 3) >= cData.Length) {
						return false; // data did not compress
					}

					cData[cPos] = (byte) (0x80 + (copyLength - 4));
					cData[cPos + 1] = (byte) ((run << 6) + (copyOffset >> 8));
					cData[cPos + 2] = (byte) (copyOffset & 0xff);
					cPos += 3;
				} else // 4 byte op code 0xC0 - 0xDF
				  {
					if ((cPos + run + 4) >= cData.Length) {
						return false; // data did not compress
					}

					cData[cPos] = (byte) (((0xC0 + ((copyOffset >> 16) << 4)) + (((copyLength - 5) >> 8) << 2)) + run);
					cData[cPos + 1] = (byte) ((copyOffset >> 8) & 0xff);
					cData[cPos + 2] = (byte) (copyOffset & 0xff);
					cData[cPos + 3] = (byte) ((copyLength - 5) & 0xff);
					cPos += 4;
				}

				for (int i = 0; i < run; i++) {
					cData[cPos] = dData[lastWritePos];
					lastWritePos++;
					cPos++;
				}
				lastWritePos += copyLength;

				return true;
			}

			/// <summary>
			/// Writes the trailing bytes after the last compressed block.
			/// </summary>
			/// <returns>
			/// <see langword="true"/> if the data was compressed; otherwise, <see langword="false"/>.
			/// </returns>
			private bool WriteTrailingBytes() {
				int run = readPos - lastWritePos;

				while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
				{
					int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);

					if ((cPos + blockLength + 1) >= cData.Length) {
						return false; // data did not compress
					}

					cData[cPos] = (byte) (0xE0 + ((blockLength / 4) - 1));
					cPos++;

					// A for loop is faster than Buffer.BlockCopy for data less than or equal to 32 bytes.
					if (blockLength <= 32) {
						for (int i = 0; i < blockLength; i++) {
							cData[cPos] = dData[lastWritePos];
							lastWritePos++;
							cPos++;
						}
					} else {
						Buffer.BlockCopy(dData, lastWritePos, cData, cPos, blockLength);
						lastWritePos += blockLength;
						cPos += blockLength;
					}
					run -= blockLength;
				}

				if ((cPos + run + 1) >= cData.Length) {
					return false;
				}

				cData[cPos] = (byte) (0xFC + run);
				cPos++;

				for (int i = 0; i < run; i++) {
					cData[cPos] = dData[lastWritePos];
					lastWritePos++;
					cPos++;
				}

				return true;
			}



			/// <summary>
			/// Finds the longest the run of data to compress.
			/// </summary>
			/// <param name="currentMatch">The current match length.</param>
			/// <returns>The longest the run of data to compress.</returns>
			/// <remarks>
			/// This method has been adapted from deflate.c in zlib version 1.2.3.
			/// </remarks>
			private int LongestMatch(int currentMatch) {
				int chainLength = MaxChain;
				int scan = readPos;
				int bestLength = prevLength;

				if (bestLength >= remaining) {
					return remaining;
				}

				byte scanEnd1 = dData[scan + bestLength - 1];
				byte scanEnd = dData[scan + bestLength];

				// Do not waste too much time if we already have a good match:
				if (prevLength >= GoodLength) {
					chainLength >>= 2;
				}
				int niceLength = NiceLength;

				// Do not look for matches beyond the end of the input. This is necessary
				// to make deflate deterministic.
				if (niceLength > remaining) {
					niceLength = remaining;
				}

				int maxLength = Math.Min(remaining, MaxMatch);
				int limit = readPos > maxWindowOffset ? readPos - maxWindowOffset : 0;

				do {
					int match = currentMatch;

					// Skip to next match if the match length cannot increase
					// or if the match length is less than 2:
					if (dData[match + bestLength] != scanEnd ||
						dData[match + bestLength - 1] != scanEnd1 ||
						dData[match] != dData[scan] ||
						dData[match + 1] != dData[scan + 1]) {
						continue;
					}

					int len = 2;
					do {
						len++;
					}
					while (len < maxLength && dData[scan + len] == dData[match + len]);

					if (len > bestLength) {
						this.matchStart = currentMatch;
						bestLength = len;
						if (len >= niceLength) {
							break;
						}
						scanEnd1 = dData[scan + bestLength - 1];
						scanEnd = dData[scan + bestLength];
					}
				}
				while ((currentMatch = prev[currentMatch & windowMask]) >= limit && --chainLength > 0);

				return bestLength;
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
		/// With QFS (LZ77) we require an LZ compatible copy method between arrays, what this means practically is that we need to copy
		/// stuff one byte at a time from arrays. This is, because with LZ compatible algorithms, it is complete legal to copy over data that overruns
		/// the currently filled position in the destination array. In other words it is more than likely the we will be asked to copy over data that hasn't
		/// been copied yet. It's confusing, so we copy things one byte at a time.
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