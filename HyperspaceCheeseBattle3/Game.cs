using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static InputValidatorLib.Validator;
using HyperspaceCheeseBattle3.Data;
using static HyperspaceCheeseBattle3.Utilities.Printer;
using static HyperspaceCheeseBattle3.Utilities.MapString;
using System.Reflection;


namespace HyperspaceCheeseBattle3
{
	public class Game
	{
		// Save location
		static readonly string SaveLocation = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "settings.yaml"));

		// Settings
		Settings settings;

		// Movement board (lookups are y then x)
		// Table is upside down
		Directions[,] movementBoard;

		// Cheese board (lookups are y then x)
		// Table is upside down
		// 0 = Empty 1 = Cheese
		int[,] cheeseBoard;

		// Starting pos
		SpotLocation StartingLoc = SpotLocation.TopLeft;

		// Player info array
		Player[] players;

		// Random static
		static readonly Random rand = new Random();

		// GameOver detector
		bool gameOver = false;

		// Suppress output
		bool output;

		// For Type 2 Dice - Set sequence
		int diceValuePos = 0;

		// stuckPlayers - if nobody moves on a turn this increases
		int stuckPlayers;

		// Holder of enabled powers
		List<CheesePowers> enabledPowers;

		// Holder for allowed colors
		List<ConsoleColor> availableColors;

		// Holder for direction chars
		Dictionary<Directions, char> directionChars;

		// Holder for changed settings
		bool changedSettings = false;

		public static void Main()
		{
			// Auto start game
			Game game = new Game();
			game.MainMenu();

			// Do not exit on end of game
			Console.WriteLine("Why are you here?");
			Console.ReadLine();
		}

		void LoadSettings()
		{
			// Required for fancy tiles
			Console.OutputEncoding = Encoding.UTF8;

			// Attempt to load settings
			if (settings == null)
			{
				settings = SettingsManager.Load(SaveLocation);
			}

			// Set if output is enabled
			output = settings.Display.OutputGame;

			// Load maps - will reload already loaded
			if (movementBoard == null)
			{
				StringToBoards(settings.General.MapString, ref StartingLoc, ref movementBoard, ref cheeseBoard);
			}

			// Load list of enabled powers - will reload already loaded
			enabledPowers = settings.Cheese.Toggles.GetTrueToggles();

			// Load list of allowed colors - will reload already loaded
			availableColors = settings.Display.Colors.GetEnabledColors();

			// Load list of directions - will reload already loaded
			directionChars = settings.Display.Directions.GetDirectionCharacters();
		}

		// Setup for each game
		void GameSetup()
		{
			// Get maxPlayers
			int maxPlayers = settings.General.MaxPlayers;

			// Get player count
			int realPlayers = InputValidator($"Please enter the number of players who want to play [0-{maxPlayers}]: ", typeof(int), numRange: true, numMin: 0, numMax: maxPlayers, tries: -1);

			// Get number of bots
			int botPlayers = 0;
			if (realPlayers < maxPlayers)
			{
				int minBots = realPlayers == 0 ? 1 : 0;
				botPlayers = InputValidator($"Please enter the number of players who want to play [{minBots}-{maxPlayers - realPlayers}]: ", typeof(int), numRange: true, numMin: minBots, numMax: maxPlayers - realPlayers, tries: -1);
			}

			// Set total player count
			int playerCount = botPlayers + realPlayers;

			// Setup player info array
			players = new Player[playerCount];

			// Setup player details
			for (int i = 0; i < playerCount; i++)
			{
				// For each real player get name
				if (i < realPlayers)
				{
					// Get name
					players[i].Name = InputValidator($"Please enter name for player {i + 1}: ", typeof(string), stringLengthRange: true, numMin: 1, numMax: 20, tries: -1);
					players[i].Bot = false;

					// Print color options
					PrintLine("Color options:", output);
					foreach (var color in availableColors)
					{
						PrintEmpty(output);
						Console.BackgroundColor = color;
						Print($"{availableColors.IndexOf(color) + 1}) {color}", output);
						Console.ResetColor();
						PrintEmpty(output);
					}
					PrintEmpty(output);

					// Get choice
					int colorChoice = InputValidator($"Please select color choice [1-{availableColors.Count}]: ", typeof(int), tries: -1, numRange: true, numMin: 1, numMax: availableColors.Count) - 1;

					// Set color and remove choice from options
					players[i].Color = availableColors[colorChoice];
					availableColors.RemoveAt(colorChoice);
				}
				else
				{
					// Set bot names
					players[i].Name = $"Bot {i - realPlayers + 1}";
					players[i].Bot = true;

					// Set bot color at random
					int colorChoice = rand.Next(availableColors.Count);
					players[i].Color = availableColors[colorChoice];
					availableColors.RemoveAt(colorChoice);
				}

				// Set initial location
				players[i].X = 0;
				players[i].Y = 0;

				// Set ID
				players[i].ID = i;
			}
		}

		// reads in the player information for a new game
		void ResetGame()
		{
			// Generate new map
			if (settings.MapGeneration.GenerateMap)
			{
				MapGenerator(settings.MapGeneration);
			}

			// Reset gameOver
			gameOver = false;

			// Reset stuckPlayers
			stuckPlayers = 0;

			// Reset player locations
			for (int i = 0; i < players.Length; i++)
			{
				// Reset stuck timer
				players[i].StuckTime = 0;

				// Set starting location
				switch (StartingLoc)
				{
					// bottom right
					case SpotLocation.BottomRight:
						players[i].X = movementBoard.GetLength(1) - 1;
						players[i].Y = movementBoard.GetLength(0) - 1;
						break;

					// bottom left
					case SpotLocation.BottomLeft:
						players[i].X = 0;
						players[i].Y = movementBoard.GetLength(0) - 1;
						break;

					// top right
					case SpotLocation.TopRight:
						players[i].X = movementBoard.GetLength(1) - 1;
						players[i].Y = 0;
						break;

					// top left
					case SpotLocation.TopLeft:
						players[i].X = 0;
						players[i].Y = 0;
						break;

					// random
					case SpotLocation.Random:
						RandomSpace(ref players[i]);
						break;

					// middle
					case SpotLocation.Middle:
						players[i].X = (movementBoard.GetLength(1) - 1) / 2;
						players[i].Y = (movementBoard.GetLength(0) - 1) / 2;
						break;
				}
			}
		}

		// returns the value of the next dice throw
		public int DiceThrow(DiceMode mode)
		{
			switch (mode)
			{
				// always return ones
				case DiceMode.Ones:
					return 1;

				// Return value based on sequence
				case DiceMode.Sequence:
					int[] diceValues = new int[] { 2, 2, 3, 3 };

					int spots = diceValues[diceValuePos];
					diceValuePos = diceValuePos++;
					if (diceValuePos == diceValues.Length)
					{
						diceValuePos = 0;
					}
					return spots;

				// Return a standard D6
				case DiceMode.Standard:
					return rand.Next(1, 7);

				// Unknown mode
				default:
					return 0;
			}
		}

		// makes a move for the player given in playerNo
		void PlayerTurn(int playerNo)
		{
			// Get player info
			ref var player = ref players[playerNo];

			// Roll
			int roll = DiceThrow(settings.General.DiceMode);

			// Define current square
			int[] newSquare = new int[2] { player.X, player.Y };

			// Detect if player stuck
			if (settings.General.ToggleStuckPlayerFix && player.StuckTime > settings.General.MaxStuckTime)
			{
				PrintLine("Warp tunnel anomaly detected!", output);
				PrintLine("Initiating emergency jump!", output);

				RandomSpace(ref player);

				PrintLine("Jump successful!", output);
				PrintLine($"Arrived at new coordinates ({player.X}, {player.Y}).", output);
				PrintLine($"Ship on cooldown for {settings.General.CoolDownTurns} turns.", output);
				player.StuckTime = -settings.General.CoolDownTurns;
				return;
			}

			// Detect if player on cooldown
			if (settings.General.ToggleStuckPlayerFix && player.StuckTime < 0)
			{
				PrintLine($"Ship on cooldown for {-player.StuckTime} turn(s)", output);
				return;
			}

			// Handler to find new square
			void move(int distance, Directions direction)
			{
				// Get new square
				switch (direction)
				{
					// Up
					case Directions.Up:
						newSquare[1] += distance;
						break;

					// Down
					case Directions.Down:
						newSquare[1] -= distance;
						break;

					// Left
					case Directions.Left:
						newSquare[0] -= distance;
						break;

					// Right
					case Directions.Right:
						newSquare[0] += distance;
						break;
				}
			}

			// Print roll
			PrintLine($"{player.Name} has rolled {roll}!", output);

			// Move according to roll
			move(roll, movementBoard[player.Y, player.X]);

			// Test if new space is occupied
			bool bounce;
			int bounceCount = 0;
			do
			{
				bounce = RocketInSquare(newSquare[0], newSquare[1]);

				// if bounce move 1
				if (bounce)
				{
					bounceCount++;
					PrintLine("Jump space occupied! Recalculating!", output);
					move(1, movementBoard[newSquare[1], newSquare[0]]);
				}
			} while (bounce && bounceCount < settings.General.MaxBounce);

			// Move back to starting location if too many bounces
			if (bounceCount == settings.General.MaxBounce)
			{
				PrintLine("Recursion detected in the space time continuum!", output);
				PrintLine("Traveling to before recursion detected!", output);
				PrintLine("Jump Failed!", output);
				player.StuckTime++;
				stuckPlayers++;
				return;
			}
			// Check if off map - Return if off map
			else if (newSquare[0] < 0 || newSquare[0] > movementBoard.GetLength(1) - 1 || newSquare[1] < 0 || newSquare[1] > movementBoard.GetLength(0) - 1)
			{
				PrintLine("No existing jump lanes found!", output);
				PrintLine("Jump Failed!", output);
				player.StuckTime++;
				stuckPlayers++;
				return;
			}
			else
			{
				// Move player to new pos
				player.X = newSquare[0];
				player.Y = newSquare[1];
				player.StuckTime = 0;
				PrintLine($"Jump Successful! Jumped to ({player.X}, {player.Y})", output);
			}

			// Check for cheese at new location
			if (cheeseBoard[player.Y, player.X] > 0)
			{
				// Run Cheese Power Logic
				CheesePower(ref player);
			}

			// Check for winner
			if (movementBoard[player.Y, player.X] == Directions.Win)
			{
				gameOver = true;
				//PrintLine($"{player.Name} has won!", output);
			}
		}

		// Get random space for player
		void RandomSpace(ref Player player)
		{
			// Define variables
			const int MaxTries = 100;       // Max tries
			int tries = 0;                  // Tries
			int newX, newY;                 // new XY holders
			do
			{
				tries++;

				// Get new random pos
				newX = rand.Next(movementBoard.GetLength(1));
				newY = rand.Next(movementBoard.GetLength(0));

				// if tries too many just accept new pos
				if (tries > MaxTries)
				{
					break;
				}
			} while (movementBoard[newY, newX] == Directions.Win || RocketInSquare(newX, newY));

			// Set new coordinates
			player.X = newX;
			player.Y = newY;
		}

		// Check if rocket on square
		bool RocketInSquare(int X, int Y)
		{
			// Check each players pos
			for (int i = 0; i < players.Length; i++)
			{
				if (players[i].X == X && players[i].Y == Y)
				{
					return true;
				}
			}

			// if no player on pos
			return false;
		}

		// Show current game status
		void ShowStatus()
		{
			// Load settings
			char lineChar = settings.Display.LineChar;
			int lineSize = Console.WindowWidth;
			bool outputGame = settings.Display.OutputGame;
			bool autoRun = settings.Display.AutoRun;

			// Check if not outputGame
			if (!outputGame)
			{
				return;
			}

			PrintBreak(lineChar, lineSize, output);
			PrintCentered("Hyperspace Cheese Battle Status Report", lineSize, output);
			PrintBreak(lineChar, lineSize, output);
			PrintCentered("Current Map", lineSize, output);
			VisualizeBoard();
			PrintBreak(lineChar, lineSize, output);

			PrintLine("Player position(s):", output);
			for (int i = 0; i < players.Length; i++)
			{
				Console.BackgroundColor = players[i].Color;
				Print(players[i].Name, output);
				Console.ResetColor();
				PrintLine($" is on square ({players[i].X}, {players[i].Y}) ", output);
			}
			PrintBreak(lineChar, lineSize, output);

			// Print extra line on gameOver
			if (gameOver)
			{
				for (int i = 0; i < players.Length; i++)
				{
					// Find winning player
					if (movementBoard[players[i].X, players[i].Y] == Directions.Win)
					{
						Console.BackgroundColor = players[i].Color;
						PrintCentered($"{settings.Display.Directions.Win} {players[i].Name} has won! {settings.Display.Directions.Win}", lineSize, output);
						Console.ResetColor();
						PrintBreak(lineChar, lineSize, output);
						break;
					}
				}
			}

			// if not autorun wait for input
			if (!autoRun && !gameOver)
			{
				PrintCentered("Press enter to end continue.", lineSize, output);
				PrintBreak(lineChar, lineSize, output);
				Console.ReadLine();
			}
		}

		// Logic for each round
		void RunRound()
		{
			// Run each players turn
			for (int i = 0; i < players.Length; i++)
			{
				// Run players turn
				PlayerTurn(i);

				// Check for game over
				if (gameOver)
				{
					break;
				}
			}

			// Print end of turn status
			ShowStatus();
		}

		// Print board to console
		void VisualizeBoard()
		{
			// Load settings
			int lineSize = Console.WindowWidth;

			// Print top line
			Console.WriteLine(new string('-', movementBoard.GetLength(1) + 2).PadLeft(((lineSize - movementBoard.GetLength(1) - 2) / 2) + movementBoard.GetLength(1) + 2));

			for (int y = movementBoard.GetLength(0) - 1; y >= 0; y--)
			{
				// Print left side line
				Console.Write("|".PadLeft(((lineSize - movementBoard.GetLength(1) - 2) / 2) + 1));

				for (int x = 0; x < movementBoard.GetLength(1); x++)
				{
					// Set color if player on square
					SquareColor(x, y);

					// Write the square value
					Console.Write(directionChars[movementBoard[y, x]]);

					// Reset color
					Console.ResetColor();

					// Write end of line
					if (x == movementBoard.GetLength(1) - 1)
					{
						Console.WriteLine("|");
					}
				}
			}

			// Print bottom line
			Console.WriteLine(new string('-', movementBoard.GetLength(1) + 2).PadLeft(((lineSize - movementBoard.GetLength(1) - 2) / 2) + movementBoard.GetLength(1) + 2));
		}

		// Set color of board square
		void SquareColor(int X, int Y)
		{
			// Loop through each player to check if any player on this square
			foreach (var player in players)
			{
				// if player at given x and y
				if (player.X == X && player.Y == Y)
				{
					Console.BackgroundColor = player.Color;

					// Break since only one player can be on a square at a given time
					break;
				}
			}

			// Check if this square is cheese
			if (cheeseBoard[Y, X] == 1)
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
			}
		}

		// Generate new map
		static void MapGenerator(MapGeneration settings)
		{
			// Generate new arrays
			Directions[,] movementBoard = new Directions[settings.MapY, settings.MapX];
			int[,] cheeseBoard = new int[settings.MapY, settings.MapX];

			// Get directions
			List<Directions> directions = new List<Directions>
			{
				Directions.Up,
				Directions.Down,
				Directions.Left,
				Directions.Right
			};

			// Random tile placement
			for (int i = 0; i < settings.MapY; i++)
			{
				for (int j = 0; j < settings.MapX; j++)
				{
					movementBoard[i, j] = directions[rand.Next(directions.Count)];
				}
			}

			// Random cheese placement
			switch (settings.CheeseMode)
			{
				// cheeseCount
				case CheeseDistributionMode.Count:
					for (int i = 0; i < settings.CheeseCount; i++)
					{
						// Loop until new square is picked
						while (true)
						{
							// Random Square
							int randX = rand.Next(settings.MapX);
							int randY = rand.Next(settings.MapY);

							if (cheeseBoard[randY, randX] == 0)
							{
								cheeseBoard[randY, randX] = 1;
								break;
							}
						}
					}
					break;

				// cheeseChance
				case CheeseDistributionMode.Chance:
					for (int i = 0; i < settings.MapY; i++)
					{
						for (int j = 0; j < settings.MapX; j++)
						{
							// Random number
							double randNum = rand.NextDouble() * 100;

							// if randNum is under percentage it triggers
							if (randNum < settings.CheeseChance)
							{
								cheeseBoard[i, j] = 1;
							}
						}
					}
					break;

				// cheesePercentage
				case CheeseDistributionMode.Percentage:
					int cheeseGoal = (int)(settings.CheesePercentage * (settings.MapX * settings.MapY));

					for (int i = 0; i < cheeseGoal; i++)
					{
						// Loop until new square is picked
						while (true)
						{
							// Random Square
							int randX = rand.Next(settings.MapX);
							int randY = rand.Next(settings.MapY);

							if (cheeseBoard[randY, randX] == 0)
							{
								cheeseBoard[randY, randX] = 1;
								break;
							}
						}
					}
					break;

			}

			// Place winning tile
			switch (settings.WinningSpot)
			{
				// bottom right
				case SpotLocation.BottomRight:
					movementBoard[settings.MapY - 1, settings.MapX - 1] = Directions.Win;
					break;

				// bottom left
				case SpotLocation.BottomLeft:
					movementBoard[settings.MapY - 1, 0] = Directions.Win;
					break;

				// top right
				case SpotLocation.TopRight:
					movementBoard[0, settings.MapX - 1] = Directions.Win;
					break;

				// top left
				case SpotLocation.TopLeft:
					movementBoard[0, 0] = Directions.Win;
					break;

				// random
				case SpotLocation.Random:
					movementBoard[rand.Next(settings.MapY), rand.Next(settings.MapX)] = Directions.Win;
					break;

				// middle
				case SpotLocation.Middle:
					movementBoard[settings.MapY / 2, settings.MapX / 2] = Directions.Win;
					break;
			}
		}

		// Run the game
		public void RunGame()
		{
			// Setup game
			GameSetup();

			// Game loop
			do
			{
				ResetGame();

				// Loop through rounds until game over
				while (!gameOver)
				{
					RunRound();

					// Stuck player logic
					if (settings.General.ToggleStuckPlayerFix)
					{
						// Check if not all players stuck
						if (stuckPlayers < players.Length)
						{
							stuckPlayers = 0;
						}

						// if all players stuck for more than x turns or turn count = maxTurns
						else if (stuckPlayers >= players.Length * (settings.General.MaxStuckTime / 2))
						{
							if (settings.Display.AutoRun)
							{
								break;
							}

							// Ask to quit
							if (InputValidator($"Quit this game? y/n: ", new string[] { "y", "n" }, tries: -1) == "y")
							{
								break;
							}
						}
					}
				}
				// Ask to play again
			} while (InputValidator("Play again? [y/n]: ", new string[] { "y", "n" }, tries: -1) == "y");

			// if exit return to main menu
			MainMenu();
		}

		// Cheese Power Logic
		void CheesePower(ref Player player)
		{
			// Flavor text
			PrintLine("Scanners detect high concentration of Cheese Power!", output);
			PrintLine("Deploying Cheese Power Vacuum!", output);
			PrintLine("Determine what to do with the excess Cheese Power:", output);

			// Print Choices
			foreach (var power in enabledPowers)
			{
				PrintLine($"{enabledPowers.IndexOf(power) + 1}) {Powers.Descriptions[power]}", output);
			}

			// Get Choice
			int chosenPower;
			if (!player.Bot)
			{
				chosenPower = InputValidator($"Please enter choice [1-{enabledPowers.Count}]: ", typeof(int), tries: -1, numRange: true, numMin: 1, numMax: enabledPowers.Count) - 1;
			}
			else
			{
				chosenPower = rand.Next(0, enabledPowers.Count);
			}

			// Powers
			switch (enabledPowers[chosenPower])
			{
				// Cheese Deathray
				case CheesePowers.Deathray:
					Powers.DeathRay(output, ref players, ref player, rand, movementBoard.GetLength(1));
					break;

				// Engine Recharge (Extra turn)
				case CheesePowers.SecondTurn:
					Powers.SecondJump(output);
					PlayerTurn(player.ID);          // Move to powers
					break;
			}
		}

		// Main menu logic
		void MainMenu()
		{
			// Load settings
			LoadSettings();

			// Settings
			int lineSize = Console.WindowWidth;
			char lineChar = settings.Display.LineChar;

			// Cheese ASCII from https://ascii.co.uk/art/cheese
			string line1 = "    _--\"-.            ";
			string line2 = " .-\"      \"-.         ";
			string line3 = "|\"\"--..      '-.      ";
			string line4 = "|      \"\"--..   '-.   ";
			string line5 = "|.-. .-\".    \"\"--..\". ";
			string line6 = "|'./  -_'  .-.      | ";
			string line7 = "|      .-. '.-'   .-' ";
			string line8 = "'--..  '.'    .-  -.  ";
			string line9 = "     \"\"--..   '_'   : ";
			string line10 = "           \"\"--..   | ";
			string line11 = "                 \"\"-' ";

			// Print
			PrintEmpty(output);
			PrintLine(line1, output);
			Print(line2, output);
			PrintCentered(new string(lineChar, 28), lineSize - line2.Length, output);
			Print(line3, output);
			PrintCentered("| Hyperspace Cheese Battle |", lineSize - line3.Length, output);
			Print(line4, output);
			PrintCentered(new string(lineChar, 28), lineSize - line4.Length, output);
			Print(line5, output);
			PrintCentered("|     By: Jared Nelson     |", lineSize - line7.Length, output);
			Print(line6, output);
			PrintCentered(new string(lineChar, 28), lineSize - line6.Length, output);
			Print(line7, output);
			PrintCentered("| 1) Play Game".PadRight(27) + "|", lineSize - line7.Length, output);
			Print(line8, output);
			PrintCentered("| 2) Settings".PadRight(27) + "|", lineSize - line8.Length, output);
			Print(line9, output);
			PrintCentered("| 3) Exit".PadRight(27) + "|", lineSize - line9.Length, output);
			Print(line10, output);
			PrintCentered(new string(lineChar, 28), lineSize - line10.Length, output);
			Print(line11, output);
			PrintEmpty(output);
			PrintEmpty(output);

			// Get input
			int selection = InputValidator($"Please enter selection [1-3]: ", typeof(int), numRange: true, numMin: 1, numMax: 3, tries: -1);

			// Play Game
			if (selection == 1)
			{
				// Run the game
				RunGame();
			}

			// Settings Menu
			else if (selection == 2)
			{
				// Open settings menu (will recall main menu after)
				SettingsMenu(settings, lineSize, lineChar);
			}

			// Exit
			else
			{
				// Exit with code 0
				Environment.Exit(0);
			}
		}

		// Menu to display and edit settings
		void SettingsMenu(object localSettings, int lineSize, char lineChar)
		{
			// Print blank line
			PrintEmpty(output);
			PrintBreak(lineChar, lineSize, output);

			// Get title of current settings location
			string titleString = $"| {localSettings.GetType().Name} |";

			// Print title in a block format
			PrintEmpty(output);
			PrintCentered(new string(lineChar, titleString.Length), lineSize, output);
			PrintCentered(titleString, lineSize, output);
			PrintCentered(new string(lineChar, titleString.Length), lineSize, output);
			PrintEmpty(output);

			// Print warning
			if (localSettings.GetType().Name == "Settings")
			{
				PrintCentered("Warning!", lineSize, output);
				PrintCentered("Settings has ZERO validation!", lineSize, output);
				PrintCentered("It is recommended to use default settings!", lineSize, output);
				PrintEmpty(output);
			}

			// Get properties of settings
			PropertyInfo[] categories = localSettings.GetType().GetProperties();

			// Sort properties
			List<PropertyInfo> references = new List<PropertyInfo>();
			List<PropertyInfo> values = new List<PropertyInfo>();

			foreach (PropertyInfo property in categories)
			{
				if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
				{
					values.Add(property);
				}
				else
				{
					references.Add(property);
				}
			}

			// Create merged list with categories first
			List<PropertyInfo> merged = new List<PropertyInfo>();
			merged.AddRange(references);
			merged.AddRange(values);

			// Print lists
			int i = 0;

			// Print categories (if any)
			if (references.Any())
			{
				PrintLine("Categories:", output);

				// Print each category
				foreach (PropertyInfo property in references)
				{
					PrintLine($"{++i}) {property.Name}", output);
				}

				// Print a spacer if there is settings too
				if (values.Any())
				{
					PrintEmpty(output);
				}
			}

			// Print settings (if any) and current values
			if (values.Any())
			{
				PrintLine("Settings:", output);

				// Print each setting and current value
				foreach (PropertyInfo property in values)
				{
					PrintLine($"{++i}) {property.Name} = {property.GetValue(localSettings)}", output);
				}
			}

			// Add return to main menu/settings
			if (localSettings.GetType().Name == "Settings")
			{
				// Return to main menu
				PrintLine($"{++i}) Return to main menu", output);

				// Reset settings
				PrintLine($"{++i}) Reset to default settings", output);

				// Save changes
				if (changedSettings)
				{
					PrintLine($"{++i}) Save changes", output);
				}
			}
			else
			{
				PrintLine($"{++i}) Return to main settings", output);
			}

			// Print space between options and input
			PrintEmpty(output);

			// Get selection
			int selection = InputValidator($"Select an option [1-{i}]: ", typeof(int), numRange: true, numMin: 1, numMax: i, tries: -1);

			// Handle selection
			if (selection == merged.Count + 3)      // Save changes
			{
				SettingsManager.Save(settings, SaveLocation);
				changedSettings = false;
				LoadSettings();
				SettingsMenu(settings, lineSize, lineChar);
			}

			else if (selection == merged.Count + 2)		// Reset to default
			{
				settings = new Settings();
				changedSettings = true;
				SettingsMenu(settings, lineSize, lineChar);
			}

			else if (selection == merged.Count + 1)
			{
				// if main settings menu return to main menu
				if (localSettings.GetType().Name == "Settings")
				{
					// Print split line
					PrintEmpty(output);
					PrintBreak(lineChar, lineSize, output);
					PrintEmpty(output);

					MainMenu();
				}

				// else return to main settings menu
				else
				{
					SettingsMenu(settings, lineSize, lineChar);
				}
			}

			// else if the setting is a value set new value
			else if (merged[selection - 1].PropertyType.IsValueType || merged[selection - 1].PropertyType == typeof(string))
			{
				// Print split line
				PrintEmpty(output);
				PrintBreak(lineChar, lineSize, output);
				PrintEmpty(output);

				// Define setting to change
				var setting = merged[selection - 1];

				// Print current value
				PrintLine("Current Value:", output);
				PrintLine($"{setting.Name}={setting.GetValue(localSettings)}", output);
				PrintEmpty(output);

				// if bool invert
				if (setting.PropertyType == typeof(bool))
				{
					setting.SetValue(localSettings, !(bool)setting.GetValue(localSettings));
				}

				// else get value
				else
				{
					var newValue = InputValidator($"New Value: ", setting.PropertyType, tries: -1);

					// Set new value
					setting.SetValue(localSettings, newValue);
				}

				// Print new value
				PrintLine("New Value:", output);
				PrintLine($"{setting.Name}={setting.GetValue(localSettings)}", output);
				PrintEmpty(output);

				// Changed setting
				changedSettings = true;

				// Reopen this settings menu to show updated
				SettingsMenu(localSettings, lineSize, lineChar);
			}

			// else open new settings menu
			else
			{
				// Load Sub Category
				SettingsMenu(merged[selection - 1].GetValue(localSettings), lineSize, lineChar);
			}
		}
	}
}


// TODO:
// UI
// - Update main menu to be more dynamic
// - Improve settings menu (validation)

// Gameplay
// - Add more dice options
// - Change max player count
// - Add cheese randomizer (option for move each time someone lands on it)
// - Add option that cheese disappears when used up
// - Random cheese placements

// Powers Changes
// - Add Cheese Power Deathray range
// - Add consequences for firing deathray in single player
// - Add cheese deathray accuracy
// - Add second jump failure rate
// - Cheese deathray target square blocking passage for x turns
// - Option to include target XY in death ray (so you can see who is closest to winning)
// - Maybe store cheese for use?

// New Powers
// - Quantum Leap: Lets the player teleport to a random tile on the board. It could be a great shortcut or throw them into trouble!
// - Tile Sabotage: Allows the player to booby-trap a tile for the next player who lands on it. The trap could cause movement penalties or redirect their ship to a different tile.
// - Directional Override: Lets the player temporarily disable or change the direction of arrows on a chosen tile, causing unpredictable results for others.
// - Cheese Storm: Causes a random rain of cheesy debris on the board, changing a random subset of tiles' properties (e.g., flipping arrow directions or temporarily disabling tile effects).
// - Lets player pick direction of travel

// Bot types
// - Random (current bots)
// - Smart (Picks based on who is winning)
// - Aggressive (Always targets player)

// Display
// - Slow down game for reading
// - Change displayed x and y to start at 1,1
// - Game stats for bot mode
