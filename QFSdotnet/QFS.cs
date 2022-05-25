using System;

namespace QFSdotnet {
    /// <summary>
    /// Implementation of QFS/RefPack/LZ77 decompression. This compression is used on larger entries inside saves.
    /// </summary>
    /// <remarks>
    /// Note that this implementation contains control characters and other changes specific to SimCity 4.
    /// You can read about other game specifics at this specification for QFS spec http://wiki.niotso.org/RefPack.
    /// 
    /// Ported from https://github.com/wouanagaine/SC4Mapper-2013/blob/db29c9bf88678a144dd1f9438e63b7a4b5e7f635/Modules/qfs.ctrlByte4#L25
    /// 
    /// More information on file specification:
    /// - https://www.wiki.sc4devotion.com/index.php?title=DBPF_Compression
    /// - http://wiki.niotso.org/RefPack#Naming_notes
    /// </remarks>
    public class QFS {
        private const ushort QFS_Constant = 0xFB10;



        /// <summary>
		/// Check if the data is compressed.
		/// </summary>
		/// <param name="entryData">Data to check</param>
		/// <returns>TRUE if data is compressed; FALSE otherwise</returns>
		public static bool IsCompressed(byte[] entryData) {
            if (entryData.Length > 6) {

                ushort signature = BitConverter.ToUInt16(entryData, 4); //ToUint32(entryData,2) would otherwise return 0xFB10 0000, but we're only interested in 0xFB10
                if (signature == QFS_Constant) {
                    //Memo's message: "there is an s3d file in SC1.dat which would otherwise return true on uncompressed data; this workaround is not fail proof"
                    //https://github.com/memo33/jDBPFX/blob/fa2535c51de80df48a7f62b79a376e25274998c0/src/jdbpfx/util/DBPFPackager.java#L54
                    //string fileType = ByteArrayHelper.ToAString(entryData, 0, 4);
                    //if (fileType.Equals("3DMD")) { //3DMD = 0x3344 4D44 = 860114244
                    //return false;
                    //}
                    return true;
                }
            }
            return false;
        }



        /// <summary>
		/// Returns the length of the data array in bytes. If data is compressed, the uncompressed size is returned. If data is not compressed, the raw size is returned.
		/// </summary>
		/// <param name="cData">Data to check</param>
		/// <returns>Size of data</returns>
		public static uint GetDecompressedSize(byte[] cData) {
            if (IsCompressed(cData)) {
                uint compressedSize = BitConverter.ToUInt32(cData, 0); //first 4 bytes is always the size of header + compressed data

                //read 5 byte header
                byte[] header = new byte[5];
                for (int idx = 0; idx < 5; idx++) {
                    header[idx] = cData[idx + 4];
                }

                //After QFS identifier, next 3 bytes are the decompressed size ... byte shift most significant byte to least
                uint decompressedSize = Convert.ToUInt32((header[2] << 16) + (header[3] << 8) + header[4]);
                return decompressedSize;

            } else {
                return (uint) cData.Length;
            }
        }



        /// <summary>
        /// Uncompress data using QFS/RefPak and return uncompressed array of uncompressed data
        /// </summary>
        /// <param name="data">Compressed array of data</param>
        /// <returns>Uncompressed data array</returns>
        /// <example>
        /// <ctrlByte4>
        /// // Load save game
        /// SC4SaveFile savegame = new SC4SaveFile(@"C:\Path\To\Save\Game.sc4");
        /// 
        /// // Read raw data for Region View Subfile from save
        /// byte[] data = sc4Save.LoadIndexEntryRaw(REGION_VIEW_SUBFILE_TGI);
        /// 
        /// // Decompress data (This file will normally be compressed, should ideally check before decompressing)
        /// byte[] decompressedData = QFS.UncompressData(data);
        /// </ctrlByte4>
        /// </example>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when the compression algorithm tries to access an element that is out of bounds in the array
        /// </exception>
        public static byte[] UncompressData(byte[] data) {
            //If data is not compressed do not run it through the algorithm otherwise it will return junk
            if (!IsCompressed(data)) {
                return data;
            }


            byte[] sourceBytes = data;
            byte[] destinationBytes;
            int sourcePosition = 0;
            int destinationPosition = 0;

            // Check first 4 bytes (size of header + compressed data)
            uint compressedSize = BitConverter.ToUInt32(sourceBytes, 0);

            // Next read the 5 byte header
            byte[] header = new byte[5];
            for (int i = 0; i < 5; i++) {
                header[i] = sourceBytes[i + 4];
            }

            // Create our destination array
            destinationBytes = new byte[GetDecompressedSize(data)];

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
            // Bytes 0-1 are header identifier, 2-4 are uncompressed size, 5-8 are unused by SC4
            // (Check was throwing off start position and caused decompression to get buggered)
            sourcePosition = 9;

            // In QFS the control character (CC) tells us what type of decompression operation we are going to perform (there are 4)
            // Most involve using the bytes proceeding the control byte to determine the amount of data that should be copied from what
            // offset. The CC can be 1 to 4 bytes, determined from byte1 of the CC
            byte ctrlByte1 = 0; //ctrlByte1
            byte ctrlByte2 = 0; //ctrlByte2, set if required
            byte ctrlByte3 = 0; //ctrlByte3, set if required
            byte ctrlByte4 = 0; //ctrlByte4, set if required
            int length = 0;
            int offset = 0;

            // Main decoding loop. Keep decoding while sourcePosition is in source array and position isn't 0xFC?
            while ((sourcePosition < sourceBytes.Length) && (sourceBytes[sourcePosition] < 0xFC)) {
                // Read our packcode/control character
                ctrlByte1 = sourceBytes[sourcePosition];

                // Read bytes proceeding packcode
                ctrlByte2 = sourceBytes[sourcePosition + 1];
                ctrlByte3 = sourceBytes[sourcePosition + 2];

                // Control Characters 0 to 127 (2 byte length CC)
                if (ctrlByte1 >= 0x00 && ctrlByte1 <= 0x7F)
                //if ((ctrlByte1 & 0x80) == 0)
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
                else if (ctrlByte1 >= 0x80 && ctrlByte1 <= 0xBF)
                //else if ((ctrlByte1 & 0x40) == 0)
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
                else if (ctrlByte1 >= 0xC0 && ctrlByte1 <= 0xDF)
                //else if ((ctrlByte1 & 0x20) == 0)
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
            if ((sourcePosition < sourceBytes.Length) && (destinationPosition < destinationBytes.Length)) {
                LZCompliantCopy(ref sourceBytes, sourcePosition + 1, ref destinationBytes, destinationPosition, sourceBytes[sourcePosition] & 3);
                destinationPosition += sourceBytes[sourcePosition] & 3;
            }

            if (destinationPosition != destinationBytes.Length) {
                //Logger.Log(LogLevel.Warning, "QFS bad length, {0} instead of {1}", destinationPosition, destinationBytes.Length);
            }

            return destinationBytes;
        }



        public static void CompressData() {

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
        /// stuff one byte at ctrlByte2 time from arrays. This is, because with LZ compatible algorithms, it is complete legal to copy over data that overruns
        /// the currently filled position in the destination array. In other words it is more than likely the we will be asked to copy over data that hasn't
        /// been copied yet. It's confusing, so we copy things one byte at ctrlByte2 time.
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
