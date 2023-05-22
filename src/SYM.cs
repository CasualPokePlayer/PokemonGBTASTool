using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using BizHawk.Emulation.Common;

namespace PokemonGBTASTool;

public abstract class SYM
{
	private static uint[] BankSizes { get; } =
	{
		/*ROM*/ 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000,
		/*VRAM*/ 0x2000, 0x2000,
		/*SRAM*/ 0x2000, 0x2000,
		/*WRAM*/ 0, 0x1000,
		/*Echo-OAM-HRAM*/ 0, 0,
	};

	private static uint[] DomOffsets { get; } =
	{
		/*ROM*/ 0, 0, 0, 0, 0x4000, 0x4000, 0x4000, 0x4000,
		/*VRAM*/ 0x8000, 0x8000,
		/*SRAM*/ 0xA000, 0xA000,
		/*WRAM*/ 0xC000, 0xD000,
		/*Echo-OAM-HRAM*/ 0, 0xFF80,
	};

	private static string[] Domains { get; } =
	{
		/*ROM*/ "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM",
		/*VRAM*/ "VRAM", "VRAM",
		/*SRAM*/ "CartRAM", "CartRAM",
		/*WRAM*/ "WRAM", "WRAM",
		/*Echo-OAM-HRAM*/ "", "HRAM",
	};

	private readonly struct SYMEntry
	{
		public readonly uint Bank;
		public readonly uint SystemBusAddress;
		public readonly string Domain;
		public readonly uint DomainAddress;

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

	public string Which { get; }

	protected SYM(string sym, Action<string> messageCb, string which)
	{
		MessageCb = messageCb;
		Which = which;

		using var compressedSymStream = typeof(SYM).Assembly.GetManifestResourceStream(sym)!;
		using var decompressedSymStream = Zstd.DecompressZstdStream(compressedSymStream);
		decompressedSymStream.Seek(0, SeekOrigin.Begin);
		using var reader = new StreamReader(decompressedSymStream);
		reader.ReadLine(); // skip first line
		while (true)
		{
			var line = reader.ReadLine();
			if (string.IsNullOrEmpty(line))
			{
				break;
			}
			SymEntries[line.Remove(0, 8)] = new(line);
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

	// ReSharper disable once MemberCanBeProtected.Global
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
