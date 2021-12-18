using System;

namespace Gen2TASTool
{
	public class PokemonData
	{
		private static readonly string[] PokemonMoves = new string[256]
		{
			"No Move",  "Pound", "Karate Chop", "DoubleSlap", "Comet Punch", "Mega Punch", "Pay Day",
			"Fire Punch", "Ice Punch", "ThunderPunch", "Scratch", "ViceGrip", "Guillotine",
			"Razor Wind", "Swords Dance", "Cut", "Gust", "Wing Attack", "Whirlwind", "Fly",
			"Bind", "Slam", "Vine Whip", "Stomp", "Double Kick", "Mega Kick", "Jump Kick",
			"Rolling Kick", "Sand-Attack", "Headbutt", "Horn Attack", "Fury Attack",
			"Horn Drill", "Tackle", "Body Slam", "Wrap", "Take Down", "Thrash", "Double-Edge",
			"Tail Whip", "Poison Sting", "Twineedle", "Pin Missile", "Leer", "Bite", "Growl",
			"Roar", "Sing", "Supersonic", "SonicBoom", "Disable", "Acid", "Ember",
			"Flamethrower", "Mist", "Water Gun", "Hydro Pump", "Surf", "Ice Beam", "Blizzard",
			"Psybeam", "BubbleBeam", "Aurora Beam", "Hyper Beam", "Peck", "Drill Peck",
			"Submission", "Low Kick", "Counter", "Seismic Toss", "Strength", "Absorb",
			"Mega Drain", "Leech Seed", "Growth", "Razor Leaf", "SolarBeam", "PoisonPowder",
			"Stun Spore", "Sleep Powder", "Petal Dance", "String Shot", "Dragon Rage",
			"Fire Spin", "ThunderShock", "Thunderbolt", "Thunder Wave", "Thunder", "Rock Throw",
			"Earthquake", "Fissure", "Dig", "Toxic", "Confusion", "Psychic", "Hypnosis",
			"Meditate", "Agility", "Quick Attack", "Rage", "Teleport", "Night Shade", "Mimic",
			"Screech", "Double Team", "Recover", "Harden", "Minimize", "SmokeScreen",
			"Confuse Ray", "Withdraw", "Defense Curl", "Barrier", "Light Screen", "Haze",
			"Reflect", "Focus Energy", "Bide", "Metronome", "Mirror Move", "Selfdestruct",
			"Egg Bomb", "Lick", "Smog", "Sludge", "Bone Club", "Fire Blast", "Waterfall",
			"Clamp", "Swift", "Skull Bash", "Spike Cannon", "Constrict", "Amnesia", "Kinesis",
			"Softboiled", "Hi Jump Kick", "Glare", "Dream Eater", "Poison Gas", "Barrage",
			"Leech Life", "Lovely Kiss", "Sky Attack", "Transform", "Bubble", "Dizzy Punch",
			"Spore", "Flash", "Psywave", "Splash", "Acid Armor", "Crabhammer", "Explosion",
			"Fury Swipes", "Bonemerang", "Rest", "Rock Slide", "Hyper Fang", "Sharpen",
			"Conversion", "Tri Attack", "Super Fang", "Slash", "Substitute", "Struggle",
			"Sketch", "Triple Kick", "Thief", "Spider Web", "Mind Reader", "Nightmare",
			"Flame Wheel", "Snore", "Curse", "Flail", "Conversion 2", "Aeroblast",
			"Cotton Spore", "Reversal", "Spite", "Powder Snow", "Protect", "Mach Punch",
			"Scary Face", "Faint Attack", "Sweet Kiss", "Belly Drum", "Sludge Bomb",
			"Mud-Slap", "Octazooka", "Spikes", "Zap Cannon", "Foresight", "Destiny Bond",
			"Perish Song", "Icy Wind", "Detect", "Bone Rush", "Lock-On", "Outrage", "Sandstorm",
			"Giga Drain", "Endure", "Charm", "Rollout", "False Swipe", "Swagger", "Milk Drink",
			"Spark", "Fury Cutter", "Steel Wing", "Mean Look", "Attract", "Sleep Talk",
			"Heal Bell", "Return", "Present", "Frustration", "Safeguard", "Pain Split",
			"Sacred Fire", "Magnitude", "DynamicPunch", "Megahorn", "DragonBreath",
			"Baton Pass", "Encore", "Pursuit", "Rapid Spin", "Sweet Scent", "Iron Tail",
			"Metal Claw", "Vital Throw", "Morning Sun", "Synthesis", "Moonlight", "Hidden Power",
			"Cross Chop", "Twister", "Rain Dance", "Sunny Day", "Crunch", "Mirror Coat",
			"Psych Up", "ExtremeSpeed", "AncientPower", "Shadow Ball", "Future Sight",
			"Rock Smash", "Whirlpool", "Beat Up",

			"Move 0xFC", "Move 0xFD", "Move 0xFE", "Move 0xFF"
		};

		private static readonly string[] PokemonSpecies = new string[256]
		{
			"No Pokemon", "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard",
			"Squirtle", "Wartortle", "Blastoise", "Caterpie", "Metapod", "Butterfree",
			"Weedle", "Kakuna", "Beedrill", "Pidgey", "Pidgeotto", "Pidgeot", "Rattata", "Raticate",
			"Spearow", "Fearow", "Ekans", "Arbok", "Pikachu", "Raichu", "Sandshrew", "Sandslash",
			"Nidoran F", "Nidorina", "Nidoqueen", "Nidoran M", "Nidorino", "Nidoking",
			"Clefairy", "Clefable", "Vulpix", "Ninetales", "Jigglypuff", "Wigglytuff",
			"Zubat", "Golbat", "Oddish", "Gloom", "Vileplume", "Paras", "Parasect", "Venonat", "Venomoth",
			"Diglett", "Dugtrio", "Meowth", "Persian", "Psyduck", "Golduck", "Mankey", "Primeape",
			"Growlithe", "Arcanine", "Poliwag", "Poliwhirl", "Poliwrath", "Abra", "Kadabra", "Alakazam",
			"Machop", "Machoke", "Machamp", "Bellsprout", "Weepinbell", "Victreebel", "Tentacool", "Tentacruel",
			"Geodude", "Graveler", "Golem", "Ponyta", "Rapidash", "Slowpoke", "Slowbro",
			"Magnemite", "Magneton", "Farfetch'd", "Doduo", "Dodrio", "Seel", "Dewgong", "Grimer", "Muk",
			"Shellder", "Cloyster", "Gastly", "Haunter", "Gengar", "Onix", "Drowzee", "Hypno",
			"Krabby", "Kingler", "Voltorb", "Electrode", "Exeggcute", "Exeggutor", "Cubone", "Marowak",
			"Hitmonlee", "Hitmonchan", "Lickitung", "Koffing", "Weezing", "Rhyhorn", "Rhydon", "Chansey",
			"Tangela", "Kangaskhan", "Horsea", "Seadra", "Goldeen", "Seaking", "Staryu", "Starmie",
			"Mr. Mime", "Scyther", "Jynx", "Electabuzz", "Magmar", "Pinsir", "Tauros", "Magikarp", "Gyarados",
			"Lapras", "Ditto", "Eevee", "Vaporeon", "Jolteon", "Flareon", "Porygon", "Omanyte", "Omastar",
			"Kabuto", "Kabutops", "Aerodactyl", "Snorlax", "Articuno", "Zapdos", "Moltres",
			"Dratini", "Dragonair", "Dragonite", "Mewtwo", "Mew",

			"Chikorita", "Bayleef", "Meganium", "Cyndaquil", "Quilava", "Typhlosion",
			"Totodile", "Croconaw", "Feraligatr", "Sentret", "Furret", "Hoothoot", "Noctowl",
			"Ledyba", "Ledian", "Spinarak", "Ariados", "Crobat", "Chinchou", "Lanturn", "Pichu", "Cleffa",
			"Igglybuff", "Togepi", "Togetic", "Natu", "Xatu", "Mareep", "Flaaffy", "Ampharos", "Bellossom",
			"Marill", "Azumarill", "Sudowoodo", "Politoed", "Hoppip", "Skiploom", "Jumpluff", "Aipom",
			"Sunkern", "Sunflora", "Yanma", "Wooper", "Quagsire", "Espeon", "Umbreon", "Murkrow", "Slowking",
			"Misdreavus", "Unown", "Wobbuffet", "Girafarig", "Pineco", "Forretress", "Dunsparce", "Gligar",
			"Steelix", "Snubbull", "Granbull", "Qwilfish", "Scizor", "Shuckle", "Heracross", "Sneasel",
			"Teddiursa", "Ursaring", "Slugma", "Magcargo", "Swinub", "Piloswine", "Corsola", "Remoraid", "Octillery",
			"Delibird", "Mantine", "Skarmory", "Houndour", "Houndoom", "Kingdra", "Phanpy", "Donphan",
			"Porygon2", "Stantler", "Smeargle", "Tyrogue", "Hitmontop", "Smoochum", "Elekid", "Magby", "Miltank",
			"Blissey", "Raikou", "Entei", "Suicune", "Larvitar", "Pupitar", "Tyranitar", "Lugia", "Ho-Oh", "Celebi",

			"Mon 0xFC", "Mon 0xFD", "Mon 0xFE", "Mon 0xFF"
		};

		private Gen2TASToolForm.MessageCallback MessageCb { get; }

		public PokemonData(Gen2TASToolForm.MessageCallback messageCb)
		{
			MessageCb = messageCb;
		}

		public string GetPokemonMoveName(byte index)
		{
			try
			{
				return PokemonMoves[index];
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting pokemon move name for index {index}");
				return "";
			}
		}

		public string GetPokemonSpeciesName(byte index)
		{
			try
			{
				return PokemonSpecies[index];
			}
			catch (Exception ex)
			{
				MessageCb($"Caught {ex.GetType().FullName} while getting pokemon species name for index {index}");
				return "";
			}
		}
	}
}
