using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using HyperspaceCheeseBattle3.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HyperspaceCheeseBattle3
{
	// Main settings
	public class Settings
	{
		public General General { get; set; } = new General();
		public Cheese Cheese { get; set; } = new Cheese();
		public MapGeneration MapGeneration { get; set; } = new MapGeneration();
		public Display Display { get; set; } = new Display();
	}

	// General settings
	public class General
	{
		// Map String
		public string MapString { get; set; } = "TopLeft:8:8:Up,Up,Up,Up,Up,Up,Up,Up,Right,Right,Up,Down,Up,Up,Left,Left,Right,Right,Up,Right,Left,Right,Left,Left,Right,Right,Up,Right,Up,Up,Left,Left,Right,Right,Right,Right,Up,Up,Left,Left,Right,Right,Right,Right,Up,Up,Left,Left,Right,Right,Up,Down,Up,Right,Left,Left,Down,Right,Right,Right,Right,Right,Down,Win:0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0";

		// Set dice mode
		public DiceMode DiceMode { get; set; } = DiceMode.Standard;

		// Set max num of players
		public int MaxPlayers { get; set; } = 4;

		// Set max bounces
		public int MaxBounce { get; set; } = 5;

		// Stuck Settings:
		// StuckPlayer toggle
		public bool ToggleStuckPlayerFix { get; set; } = false;

		// Max time to be stuck before allowing sublight
		public int MaxStuckTime { get; set; } = 2;

		// Cool down turns after emergency jump
		public int CoolDownTurns { get; set; } = 1;
	}

	// Cheese Powers
	public class Cheese
	{
		// Toggles Sub category
		public CheeseToggles Toggles { get; set; } = new CheeseToggles();
		public class CheeseToggles
		{
			public bool Deathray { get; set; } = true;
			public bool SecondTurn { get; set; } = true;

			// Method to get a list of all toggles that are true (as enum)
			public List<CheesePowers> GetTrueToggles()
			{
				var trueToggles = new List<CheesePowers>();
				var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach (var property in properties)
				{
					bool value = (bool)property.GetValue(this);
					if (value)
					{
						trueToggles.Add((CheesePowers)Enum.Parse(typeof(CheesePowers), property.Name));
					}
				}
				return trueToggles;
			}
		}
	}

	// Map generation settings
	public class MapGeneration
	{
		// Map generation toggle
		public bool GenerateMap { get; set; } = false;

		// Map Size
		public int MapX { get; set; } = 10;
		public int MapY { get; set; } = 10;

		// Important spots:
		// Winning tile location
		public SpotLocation WinningSpot { get; set; } = SpotLocation.BottomRight;

		// Starting location
		public SpotLocation StartingSpot { get; set; } = SpotLocation.TopLeft;

		// Cheese Placement:
		// Cheese distribution mode
		public CheeseDistributionMode CheeseMode { get; set; } = CheeseDistributionMode.Count;

		// Cheese tiles to place
		public int CheeseCount { get; set; } = 5;

		// Percentage chance for tile to be cheese
		public double CheeseChance { get; set; } = 10.0;

		// Percentage of board to be cheese
		public double CheesePercentage { get; set; } = 10.0;
	}

	// Display Settings
	public class Display
	{
		// Set line char
		public char LineChar { get; set; } = '=';

		// Set display characters
		public DirectionChars Directions { get; set; } = new DirectionChars();

		public class DirectionChars
		{
			public char Up { get; set; } = '↑';
			public char Down { get; set; } = '↓';
			public char Left { get; set; } = '←';
			public char Right { get; set; } = '→';
			public char Win { get; set; } = '♕';

			public Dictionary<Data.Directions, char> GetDirectionCharacters()
			{
				var temp = new Dictionary<Data.Directions, char>
				{
					{ Data.Directions.Up, Up },
					{ Data.Directions.Down, Down },
					{ Data.Directions.Left, Left },
					{ Data.Directions.Right, Right },
					{ Data.Directions.Win, Win }
				};
				return temp;
			}
		}

		// Toggle text output
		public bool OutputGame { get; set; } = true;

		// Set AutoRun - skip enter to continue
		public bool AutoRun { get; set; } = false;

		// Colors container
		public DisplayColors Colors { get; set; } = new DisplayColors();

		public class DisplayColors
		{
			// Color toggles
			public bool Black { get; set; } = false;
			public bool DarkBlue { get; set; } = true;
			public bool DarkGreen { get; set; } = true;
			public bool DarkCyan { get; set; } = true;
			public bool DarkRed { get; set; } = true;
			public bool DarkMagenta { get; set; } = true;
			public bool DarkYellow { get; set; } = false;
			public bool Gray { get; set; } = false;
			public bool DarkGray { get; set; } = true;
			public bool Blue { get; set; } = true;
			public bool Green { get; set; } = false;
			public bool Cyan { get; set; } = false;
			public bool Red { get; set; } = true;
			public bool Magenta { get; set; } = false;
			public bool Yellow { get; set; } = false;
			public bool White { get; set; } = false;

			// Method to get enabled colors
			public List<ConsoleColor> GetEnabledColors()
			{
				var enabledColors = new List<ConsoleColor>();
				var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach (var property in properties)
				{
					if ((bool)property.GetValue(this))
					{
						enabledColors.Add((ConsoleColor)Enum.Parse(typeof(ConsoleColor), property.Name));
					}
				}
				return enabledColors;
			}
		}
	}



	// Settings Manager
	public static class SettingsManager
	{
		// Save
		public static void Save(Settings settings, string filePath)
		{
			try
			{
				// Serializer
				var serializer = new SerializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();

				// Convert to yaml
				var yaml = serializer.Serialize(settings);

				// Save to file
				File.WriteAllText(filePath, yaml);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred while saving settings: {ex.Message}");
			}
		}

		// Load
		public static Settings Load(string filePath)
		{
			try
			{
				// Check for file
				if (!File.Exists(filePath))
				{
					Console.WriteLine($"Settings file not found: {filePath}. Loading default settings.");

					// Load default settings
					Settings settings = new Settings();

					// Save default settings to file
					Save(settings, filePath);

					// Return default settings if the file doesn't exist
					return new Settings();
				}

				// Deserializer
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();

				// Read file in
				var yaml = File.ReadAllText(filePath);

				// Return converted yaml as Settings
				return deserializer.Deserialize<Settings>(yaml);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred while loading settings: {ex.Message}");
				// Return default settings
				return new Settings();
			}
		}
	}
}