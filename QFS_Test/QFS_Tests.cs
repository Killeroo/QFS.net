using Microsoft.VisualStudio.TestTools.UnitTesting;
using QFSdotnet;

namespace QFS_Test {
	[TestClass]
	public class QFS_Tests {
		#region SampleData
		//Sample data from z_DataView - Parks Aura.dat --- in BINARY encoding ---
		public static byte[] notcompresseddata_b = new byte[] { 0x14, 0x00, 0x00, 0x10, 0x50, 0x00, 0x61, 0x00, 0x72, 0x00, 0x6B, 0x00, 0x73, 0x00, 0x20, 0x00, 0x41, 0x00, 0x75, 0x00, 0x72, 0x00, 0x61, 0x00, 0x20, 0x00, 0x28, 0x00, 0x62, 0x00, 0x79, 0x00, 0x20, 0x00, 0x43, 0x00, 0x6F, 0x00, 0x72, 0x00, 0x69, 0x00, 0x29, 0x00 };
		public static byte[] compresseddata_b = new byte[] { 0x42, 0x01, 0x00, 0x00, 0x10, 0xFB, 0x00, 0x01, 0xBE, 0xE5, 0x45, 0x51, 0x5A, 0x42, 0x31, 0x23, 0x23, 0x23, 0x61, 0x28, 0x34, 0x05, 0x3F, 0x69, 0x0F, 0x69, 0x00, 0x67, 0x0B, 0x4A, 0x0F, 0x00, 0x00, 0x00, 0x01, 0x03, 0x10, 0x02, 0x03, 0x00, 0x03, 0x01, 0x03, 0x23, 0x05, 0x0C, 0x20, 0xE0, 0x0C, 0x80, 0x00, 0x00, 0x01, 0x07, 0x14, 0xE5, 0x44, 0x61, 0x74, 0x61, 0x56, 0x69, 0x65, 0x77, 0x3A, 0x20, 0x50, 0x61, 0x72, 0x6B, 0x73, 0x20, 0x41, 0x75, 0x72, 0x61, 0xE0, 0x47, 0x0B, 0x4A, 0x02, 0x20, 0x00, 0x03, 0x05, 0x29, 0x08, 0x9B, 0x00, 0x00, 0x05, 0x2C, 0xE1, 0x01, 0x08, 0x0B, 0x16, 0x09, 0x01, 0xE2, 0x0A, 0x40, 0x00, 0xE3, 0x10, 0x20, 0x15, 0x4D, 0xE4, 0x19, 0x33, 0x1C, 0x03, 0x05, 0x99, 0x70, 0x01, 0xE0, 0x3C, 0x53, 0xBC, 0x70, 0x11, 0x07, 0x0C, 0x01, 0x07, 0x0D, 0xE0, 0x79, 0x8C, 0xD9, 0x70, 0x11, 0x07, 0x46, 0x01, 0x07, 0x7F, 0xE0, 0xBA, 0xC5, 0xF0, 0x70, 0x00, 0x36, 0xE0, 0x00, 0xFF, 0xFF, 0xFF, 0x02, 0x07, 0x70, 0x81, 0xE0, 0xDD, 0xF1, 0xE2, 0x70, 0x01, 0x07, 0xB8, 0xE0, 0xBB, 0xE3, 0xC5, 0x70, 0x01, 0x07, 0xB9, 0xE0, 0x9A, 0xD4, 0xA8, 0x70, 0x05, 0x2F, 0xF2, 0xE0, 0xC6, 0x8A, 0x70, 0xF3, 0x00, 0x07, 0xE0, 0x58, 0xB7, 0x6A, 0x70, 0x01, 0x07, 0xFE, 0xE0, 0x36, 0xA8, 0x46, 0x70, 0x09, 0x66, 0xFF, 0x17, 0x89, 0x00, 0x70, 0xE5, 0x04, 0x68, 0x19, 0xAA, 0xE7, 0x15, 0x16, 0xE9, 0x01, 0x03, 0x06, 0x15, 0x0C, 0xEC, 0x01, 0x03, 0x64, 0x19, 0xBA, 0xEF, 0x00, 0x76, 0x15, 0xBA, 0xF2, 0x0D, 0xB6, 0x09, 0xE2, 0x99, 0x3B, 0x55, 0xBA, 0x99, 0x64, 0x83, 0xC8, 0x99, 0x7C, 0xA3, 0xC6, 0x01, 0x1F, 0x99, 0xE2, 0x99, 0x81, 0xB6, 0xB4, 0x99, 0x72, 0xBA, 0x94, 0x99, 0x4E, 0xB1, 0x65, 0x01, 0x6E, 0x99, 0x88, 0x80, 0x30, 0x99, 0xF3, 0xE8, 0x33, 0x72, 0x3E, 0x1C, 0x98, 0x3C, 0xAB, 0xB5, 0x9F, 0xDE, 0xD7, 0xC2, 0x88, 0xB1, 0xD1, 0x84, 0xFA, 0x44, 0x28, 0x16, 0xF1, 0x9F, 0x73, 0xB1, 0x5D, 0x99, 0x4E, 0x4F, 0x43, 0xD1, 0x97, 0x14, 0xA1, 0x35, 0x54, 0xF6, 0x15, 0x6E, 0xF4, 0xE0, 0xB4, 0x75, 0xCA, 0x39, 0xFC };
		public static byte[] decompresseddata_b = new byte[] { 0x45, 0x51, 0x5A, 0x42, 0x31, 0x23, 0x23, 0x23, 0x61, 0x28, 0x34, 0x05, 0x3F, 0x69, 0x0F, 0x69, 0x00, 0x67, 0x0B, 0x4A, 0x0F, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x80, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x44, 0x61, 0x74, 0x61, 0x56, 0x69, 0x65, 0x77, 0x3A, 0x20, 0x50, 0x61, 0x72, 0x6B, 0x73, 0x20, 0x41, 0x75, 0x72, 0x61, 0xE0, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x80, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE1, 0x47, 0x0B, 0x4A, 0x00, 0x0B, 0x00, 0x00, 0x00, 0x01, 0xE2, 0x47, 0x0B, 0x4A, 0x00, 0x0B, 0x00, 0x00, 0x00, 0x00, 0xE3, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE4, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x80, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99, 0x70, 0x01, 0x00, 0x00, 0x00, 0x3C, 0x53, 0xBC, 0x70, 0x0C, 0x00, 0x00, 0x00, 0x3C, 0x53, 0xBC, 0x70, 0x0D, 0x00, 0x00, 0x00, 0x79, 0x8C, 0xD9, 0x70, 0x46, 0x00, 0x00, 0x00, 0x79, 0x8C, 0xD9, 0x70, 0x7F, 0x00, 0x00, 0x00, 0xBA, 0xC5, 0xF0, 0x70, 0x80, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x70, 0x81, 0x00, 0x00, 0x00, 0xDD, 0xF1, 0xE2, 0x70, 0xB8, 0x00, 0x00, 0x00, 0xBB, 0xE3, 0xC5, 0x70, 0xB9, 0x00, 0x00, 0x00, 0x9A, 0xD4, 0xA8, 0x70, 0xF2, 0x00, 0x00, 0x00, 0x79, 0xC6, 0x8A, 0x70, 0xF3, 0x00, 0x00, 0x00, 0x58, 0xB7, 0x6A, 0x70, 0xFE, 0x00, 0x00, 0x00, 0x36, 0xA8, 0x46, 0x70, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x99, 0x00, 0x70, 0xE5, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0xE7, 0x47, 0x0B, 0x4A, 0x00, 0x0B, 0x00, 0x00, 0x00, 0x01, 0xE9, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0xEC, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0xEF, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xF2, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x80, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99, 0x99, 0x3B, 0x55, 0xBA, 0x99, 0x64, 0x83, 0xC8, 0x99, 0x7C, 0xA3, 0xC6, 0x99, 0xFF, 0xFF, 0xFF, 0x99, 0x81, 0xB6, 0xB4, 0x99, 0x72, 0xBA, 0x94, 0x99, 0x4E, 0xB1, 0x65, 0x99, 0x00, 0x99, 0x00, 0x99, 0xF3, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x80, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x33, 0x72, 0x3E, 0x1C, 0x98, 0x3C, 0xAB, 0xB5, 0x9F, 0xDE, 0xD7, 0xC2, 0x88, 0xB1, 0xD1, 0x84, 0xFA, 0x44, 0x28, 0x16, 0xF1, 0x9F, 0x73, 0xB1, 0x5D, 0x99, 0x4E, 0x4F, 0x43, 0xD1, 0x97, 0x14, 0xA1, 0x35, 0x54, 0xF6, 0xF4, 0x47, 0x0B, 0x4A, 0x00, 0x03, 0x00, 0x00, 0x00, 0xB4, 0x75, 0xCA, 0x39 };

		//Sample data from b62-albertsons_60s v 1.1-0x6534284a-0xd3a3e650-0xd4ebfbfa.SC4Desc --- in TEXT encoding ---
		public static byte[] notcompresseddata_t = { 0x45, 0x51, 0x5A, 0x54, 0x31, 0x23, 0x23, 0x23, 0x0D, 0x0A, 0x50, 0x61, 0x72, 0x65, 0x6E, 0x74, 0x43, 0x6F, 0x68, 0x6F, 0x72, 0x74, 0x3D, 0x4B, 0x65, 0x79, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x7D, 0x0D, 0x0A, 0x50, 0x72, 0x6F, 0x70, 0x43, 0x6F, 0x75, 0x6E, 0x74, 0x3D, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x38, 0x0D, 0x0A, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x3A, 0x7B, 0x22, 0x45, 0x78, 0x65, 0x6D, 0x70, 0x6C, 0x61, 0x72, 0x20, 0x54, 0x79, 0x70, 0x65, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x32, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x3A, 0x7B, 0x22, 0x45, 0x78, 0x65, 0x6D, 0x70, 0x6C, 0x61, 0x72, 0x20, 0x4E, 0x61, 0x6D, 0x65, 0x22, 0x7D, 0x3D, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3A, 0x31, 0x3A, 0x7B, 0x22, 0x42, 0x36, 0x32, 0x2D, 0x43, 0x53, 0x24, 0x5F, 0x41, 0x6C, 0x62, 0x65, 0x72, 0x74, 0x73, 0x6F, 0x6E, 0x73, 0x5F, 0x36, 0x30, 0x73, 0x5F, 0x47, 0x72, 0x6F, 0x63, 0x65, 0x72, 0x79, 0x20, 0x76, 0x20, 0x31, 0x2E, 0x31, 0x22, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x30, 0x39, 0x39, 0x41, 0x46, 0x41, 0x43, 0x44, 0x3A, 0x7B, 0x22, 0x42, 0x75, 0x6C, 0x6C, 0x64, 0x6F, 0x7A, 0x65, 0x20, 0x43, 0x6F, 0x73, 0x74, 0x22, 0x7D, 0x3D, 0x53, 0x69, 0x6E, 0x74, 0x36, 0x34, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x41, 0x39, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x31, 0x30, 0x3A, 0x7B, 0x22, 0x4F, 0x63, 0x63, 0x75, 0x70, 0x61, 0x6E, 0x74, 0x20, 0x53, 0x69, 0x7A, 0x65, 0x22, 0x7D, 0x3D, 0x46, 0x6C, 0x6F, 0x61, 0x74, 0x33, 0x32, 0x3A, 0x33, 0x3A, 0x7B, 0x38, 0x31, 0x2E, 0x35, 0x38, 0x39, 0x37, 0x39, 0x37, 0x39, 0x37, 0x2C, 0x31, 0x33, 0x2E, 0x39, 0x34, 0x37, 0x32, 0x39, 0x39, 0x39, 0x36, 0x2C, 0x33, 0x39, 0x2E, 0x34, 0x34, 0x32, 0x35, 0x30, 0x31, 0x30, 0x37, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x31, 0x31, 0x3A, 0x7B, 0x22, 0x55, 0x6E, 0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x22, 0x7D, 0x3D, 0x46, 0x6C, 0x6F, 0x61, 0x74, 0x33, 0x32, 0x3A, 0x31, 0x3A, 0x7B, 0x30, 0x2E, 0x35, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x32, 0x31, 0x3A, 0x7B, 0x22, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x4B, 0x65, 0x79, 0x54, 0x79, 0x70, 0x65, 0x31, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x33, 0x3A, 0x7B, 0x30, 0x78, 0x35, 0x41, 0x44, 0x30, 0x45, 0x38, 0x31, 0x37, 0x2C, 0x30, 0x78, 0x42, 0x32, 0x44, 0x36, 0x44, 0x45, 0x42, 0x45, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x33, 0x30, 0x30, 0x30, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x33, 0x32, 0x3A, 0x7B, 0x22, 0x57, 0x65, 0x61, 0x6C, 0x74, 0x68, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x31, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x33, 0x33, 0x3A, 0x7B, 0x22, 0x50, 0x75, 0x72, 0x70, 0x6F, 0x73, 0x65, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x32, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x33, 0x34, 0x3A, 0x7B, 0x22, 0x43, 0x61, 0x70, 0x61, 0x63, 0x69, 0x74, 0x79, 0x20, 0x53, 0x61, 0x74, 0x69, 0x73, 0x66, 0x69, 0x65, 0x64, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x32, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x33, 0x31, 0x31, 0x30, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x32, 0x45, 0x46, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x35, 0x31, 0x3A, 0x7B, 0x22, 0x50, 0x6F, 0x6C, 0x6C, 0x75, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x61, 0x74, 0x20, 0x63, 0x65, 0x6E, 0x74, 0x72, 0x65, 0x22, 0x7D, 0x3D, 0x53, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x34, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x37, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x36, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x37, 0x38, 0x31, 0x32, 0x38, 0x35, 0x34, 0x3A, 0x7B, 0x22, 0x50, 0x6F, 0x77, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6E, 0x73, 0x75, 0x6D, 0x65, 0x64, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x39, 0x32, 0x34, 0x34, 0x44, 0x42, 0x35, 0x3A, 0x7B, 0x22, 0x46, 0x6C, 0x61, 0x6D, 0x6D, 0x61, 0x62, 0x69, 0x6C, 0x69, 0x74, 0x79, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x32, 0x44, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x41, 0x34, 0x39, 0x39, 0x46, 0x38, 0x35, 0x3A, 0x7B, 0x22, 0x51, 0x75, 0x65, 0x72, 0x79, 0x20, 0x65, 0x78, 0x65, 0x6D, 0x70, 0x6C, 0x61, 0x72, 0x20, 0x47, 0x55, 0x49, 0x44, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x43, 0x41, 0x35, 0x36, 0x37, 0x38, 0x33, 0x41, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x32, 0x43, 0x38, 0x46, 0x38, 0x37, 0x34, 0x36, 0x3A, 0x7B, 0x22, 0x45, 0x78, 0x65, 0x6D, 0x70, 0x6C, 0x61, 0x72, 0x20, 0x43, 0x61, 0x74, 0x65, 0x67, 0x6F, 0x72, 0x79, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x38, 0x43, 0x38, 0x46, 0x42, 0x42, 0x43, 0x43, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x34, 0x39, 0x39, 0x41, 0x46, 0x41, 0x33, 0x38, 0x3A, 0x7B, 0x22, 0x43, 0x6F, 0x6E, 0x73, 0x74, 0x72, 0x75, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x54, 0x69, 0x6D, 0x65, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x31, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x34, 0x39, 0x42, 0x45, 0x44, 0x41, 0x33, 0x31, 0x3A, 0x7B, 0x22, 0x4D, 0x61, 0x78, 0x46, 0x69, 0x72, 0x65, 0x53, 0x74, 0x61, 0x67, 0x65, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x34, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x36, 0x38, 0x45, 0x45, 0x39, 0x37, 0x36, 0x34, 0x3A, 0x7B, 0x22, 0x50, 0x6F, 0x6C, 0x6C, 0x75, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x52, 0x61, 0x64, 0x69, 0x75, 0x73, 0x22, 0x7D, 0x3D, 0x46, 0x6C, 0x6F, 0x61, 0x74, 0x33, 0x32, 0x3A, 0x34, 0x3A, 0x7B, 0x35, 0x2C, 0x35, 0x2C, 0x30, 0x2C, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x38, 0x41, 0x31, 0x43, 0x33, 0x45, 0x37, 0x32, 0x3A, 0x7B, 0x22, 0x57, 0x6F, 0x72, 0x74, 0x68, 0x22, 0x7D, 0x3D, 0x53, 0x69, 0x6E, 0x74, 0x36, 0x34, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x41, 0x39, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x38, 0x43, 0x42, 0x33, 0x35, 0x31, 0x31, 0x46, 0x3A, 0x7B, 0x22, 0x4F, 0x63, 0x63, 0x75, 0x70, 0x61, 0x6E, 0x74, 0x20, 0x54, 0x79, 0x70, 0x65, 0x73, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x31, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x33, 0x31, 0x31, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x41, 0x41, 0x31, 0x44, 0x44, 0x33, 0x39, 0x36, 0x3A, 0x7B, 0x22, 0x4F, 0x63, 0x63, 0x75, 0x70, 0x61, 0x6E, 0x74, 0x47, 0x72, 0x6F, 0x75, 0x70, 0x73, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x36, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x30, 0x31, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x31, 0x33, 0x31, 0x31, 0x30, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x30, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x31, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x32, 0x2C, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x33, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x41, 0x41, 0x31, 0x44, 0x44, 0x33, 0x39, 0x37, 0x3A, 0x7B, 0x22, 0x53, 0x46, 0x58, 0x3A, 0x51, 0x75, 0x65, 0x72, 0x79, 0x20, 0x53, 0x6F, 0x75, 0x6E, 0x64, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x32, 0x41, 0x38, 0x39, 0x31, 0x36, 0x41, 0x42, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x41, 0x41, 0x38, 0x33, 0x35, 0x35, 0x38, 0x46, 0x3A, 0x7B, 0x22, 0x43, 0x72, 0x61, 0x6E, 0x65, 0x20, 0x48, 0x69, 0x6E, 0x74, 0x73, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x38, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x43, 0x38, 0x45, 0x44, 0x32, 0x44, 0x38, 0x34, 0x3A, 0x7B, 0x22, 0x57, 0x61, 0x74, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6E, 0x73, 0x75, 0x6D, 0x65, 0x64, 0x22, 0x7D, 0x3D, 0x55, 0x69, 0x6E, 0x74, 0x33, 0x32, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x39, 0x38, 0x7D, 0x0D, 0x0A, 0x30, 0x78, 0x45, 0x39, 0x31, 0x41, 0x30, 0x42, 0x35, 0x46, 0x3A, 0x7B, 0x22, 0x42, 0x75, 0x69, 0x6C, 0x64, 0x69, 0x6E, 0x67, 0x20, 0x76, 0x61, 0x6C, 0x75, 0x65, 0x22, 0x7D, 0x3D, 0x53, 0x69, 0x6E, 0x74, 0x36, 0x34, 0x3A, 0x30, 0x3A, 0x7B, 0x30, 0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x36, 0x37, 0x38, 0x7D, 0x0D, 0x0A, 0x31, 0x2E, 0x35, 0x38, 0x39, 0x37, 0x39, 0x42, 0x2B, 0xF2, 0x55, 0xBF, 0xB7, 0x55 };
		#endregion SampleData

		[TestMethod]
		public void Test_020_QFS_IsCompressed() {
			Assert.IsTrue(QFS.IsCompressed(compresseddata_b));
			Assert.IsFalse(QFS.IsCompressed(notcompresseddata_b));
			Assert.IsFalse(QFS.IsCompressed(decompresseddata_b));

			Assert.IsFalse(QFS.IsCompressed(notcompresseddata_t));

		}

		[TestMethod]
		public void Test_021_QFS_GetDecompressedSize() {
			Assert.AreEqual((uint) 44, QFS.GetDecompressedSize(notcompresseddata_b));
			Assert.AreEqual((uint) 446, QFS.GetDecompressedSize(compresseddata_b));
			Assert.AreEqual((uint) 446, QFS.GetDecompressedSize(decompresseddata_b)); //BUG - figure out why this returns 318 when read from the index below
		}

		[TestMethod]
		public void Test_025_QFS_Decompress() {
			CollectionAssert.AreEquivalent(notcompresseddata_b, QFS.UncompressData(notcompresseddata_b));
			CollectionAssert.AreEquivalent(decompresseddata_b, QFS.UncompressData(compresseddata_b));
		}
	}
}
