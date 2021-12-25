using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace PokemonGBTASTool
{
	public abstract class SYM
	{
		private static uint[] BankSizes { get; } = new uint[16]
		{
			/*ROM*/ 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x2000, 0x2000,
			/*SRAM*/ 0x2000, 0x2000,
			/*WRAM*/ 0, 0x1000,
			/*Echo-OAM-HRAM*/ 0, 0,
		};

		private static uint[] DomOffsets { get; } = new uint[16]
		{
			/*ROM*/ 0, 0, 0, 0, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x8000, 0x8000,
			/*SRAM*/ 0xA000, 0xA000,
			/*WRAM*/ 0xC000, 0xD000,
			/*Echo-OAM-HRAM*/ 0, 0xFF80,
		};

		private static string[] Domains { get; } = new string[16]
		{
			/*ROM*/ "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM",
			/*VRAM*/ "VRAM", "VRAM",
			/*SRAM*/ "CartRAM", "CartRAM",
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
				Domain = Domains[index];
				DomainAddress = SystemBusAddress + Bank * BankSizes[index] - DomOffsets[index];
			}
		}

		private Dictionary<string, SYMEntry> SymEntries { get; } = new();
		private Action<string> MessageCb { get; }

		private string Which { get; }

		public bool IsGen2 { get; }

		public SYM(string sym, Action<string> messageCb, string which, bool isGen2)
		{
			MessageCb = messageCb;
			Which = which;
			IsGen2 = isGen2;

			var reader = new StreamReader(new GZipStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(sym), CompressionMode.Decompress));
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
				return Which + SymEntries[symbol].Domain;
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

	public sealed class RedSYM : SYM
	{
		public RedSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokered.sym.gz", messageCb, which, false)
		{
		}
	}

	public sealed class BlueSYM : SYM
	{
		public BlueSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokeblue.sym.gz", messageCb, which, false)
		{
		}
	}

	public sealed class YellowSYM : SYM
	{
		public YellowSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokeyellow.sym.gz", messageCb, which, false)
		{
		}
	}

	public sealed class GoldSYM : SYM
	{
		public GoldSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokegold.sym.gz", messageCb, which, true)
		{
		}
	}

	public sealed class SilverSYM : SYM
	{
		public SilverSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokesilver.sym.gz", messageCb, which, true)
		{
		}
	}

	public sealed class CrystalSYM : SYM
	{
		public CrystalSYM(Action<string> messageCb, string which)
			: base("PokemonGBTASTool.res.pokecrystal.sym.gz", messageCb, which, true)
		{
		}
	}
}
