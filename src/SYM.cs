using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Gen2TASTool
{
	public class SYM
	{
		public enum Gen2Game
		{
			Gold,
			Silver,
			Crystal
		}

		private readonly Gen2Game Game;

		public bool IsGold => Game is Gen2Game.Gold;
		public bool IsSilver => Game is Gen2Game.Silver;
		public bool IsGS => Game is Gen2Game.Gold or Gen2Game.Silver;
		public bool IsCrys => Game is Gen2Game.Crystal;

		private static readonly uint[] bankSizes = new uint[16]
		{
			/*ROM*/ 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x2000, 0x2000,
			/*SRAM*/ 0x2000, 0x2000,
			/*WRAM*/ 0, 0x1000,
			/*Echo-OAM-HRAM*/ 0, 0,
		};

		private static readonly uint[] domOffsets = new uint[16]
		{
			/*ROM*/ 0, 0, 0, 0, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x8000, 0x8000,
			/*SRAM*/ 0xA000, 0xA000,
			/*WRAM*/ 0xC000, 0xD000,
			/*Echo-OAM-HRAM*/ 0, 0xFF80,
		};

		private static readonly string[] domains = new string[16]
		{
			/*ROM*/ "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM",
			/*VRAM*/ "VRAM", "VRAM",
			/*SRAM*/ "SRAM", "SRAM",
			/*WRAM*/ "WRAM", "WRAM",
			/*Echo-OAM-HRAM*/ "", "HRAM",
		};

		private class SYMEntry
		{
			public uint Bank;
			public uint SystemBusAddress;
			public string Domain;
			public uint DomainAddress;

			public SYMEntry(string line)
			{
				Bank = uint.Parse(line.Substring(0, 2), NumberStyles.HexNumber);
				SystemBusAddress = uint.Parse(line.Substring(3, 4), NumberStyles.HexNumber);
				var index = SystemBusAddress >> 12;
				Domain = domains[index];
				DomainAddress = SystemBusAddress + Bank * bankSizes[index] - domOffsets[index];
			}
		}

		private readonly Dictionary<string, SYMEntry> SymEntries = new();
		private Gen2TASToolForm.MessageCallback MessageCb { get; }

		public SYM(Gen2Game game, Gen2TASToolForm.MessageCallback messageCb)
		{
			MessageCb = messageCb;
			Game = game;

			string file = game switch
			{
				Gen2Game.Gold => "Gen2TASTool.pokegold.sym",
				Gen2Game.Silver => "Gen2TASTool.pokesilver.sym",
				Gen2Game.Crystal => "Gen2TASTool.pokecrystal.sym",
				_ => throw new Exception()
			};

			var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(file));
			reader.ReadLine(); // skip first line
			while (true)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrEmpty(line))
				{
					break;
				}
				SymEntries[line.Remove(0, 8)] = new SYMEntry(line);
			}
		}

		public uint GetSYMBank(string symbol)
		{
			try
			{
				return SymEntries[symbol].Bank;
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting bank for symbol {symbol}");
				return 0;
			}
		}

		public uint GetSYMSysBusAddr(string symbol)
		{
			try
			{
				return SymEntries[symbol].SystemBusAddress;
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting system bus address for symbol {symbol}");
				return 0;
			}
		}

		public string GetSYMDomain(string symbol)
		{
			try
			{
				return SymEntries[symbol].Domain;
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting domain for symbol {symbol}");
				return "";
			}
		}

		public uint GetSYMDomAddr(string symbol)
		{
			try
			{
				return SymEntries[symbol].DomainAddress;
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting domain address for symbol {symbol}");
				return 0;
			}
		}
	}
}
