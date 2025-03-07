using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperspaceCheeseBattle2
{
	internal class Program
	{
		// Movement board (lookups are y then x)
		// Table is upside down
		// D = Down U = Up R = Right L = Left W = Win
		static char[,] movementBoard = new char[,] {
			{'U','U','U','U','U','U','U','U'},  // row 0
			{'R','R','U','D','U','U','L','L'},  // row 1
			{'R','R','U','R','L','R','L','L'},  // row 2
			{'R','R','U','R','U','U','L','L'},  // row 3
			{'R','R','R','R','U','U','L','L'},  // row 4
			{'R','R','R','R','U','U','L','L'},  // row 5
			{'R','R','U','D','U','R','L','L'},  // row 6
			{'D','R','R','R','R','R','D','W'}   // row 7
		};

		// Chese board (lookups are y then x)
		// Table is upside down
		// 0 = Empty 1 = Cheese
		static int[,] cheeseBoard = new int[,] {
			{0,0,0,0,0,0,0,0},  // row 0
			{0,0,0,0,1,0,0,0},  // row 1
			{0,0,0,0,0,0,0,0},  // row 2
			{1,0,0,0,0,0,0,0},  // row 3
			{0,0,0,0,0,0,1,0},  // row 4
			{0,0,0,1,0,0,0,0},  // row 5
			{0,0,0,0,0,0,0,0},  // row 6
			{0,0,0,0,0,0,0,0}   // row 7
		};

		// Player struct
		struct Player
		{
			public string Name;
			public int ID;
			public int X;
			public int Y;
		}

		// Player info array
		static Player[] players;

		// Random static
		static Random rand = new Random();

		// GameOver detector
		static bool gameOver = false;

		// For Type 2 Dice - Set sequence
		static int[] diceValues = new int[] { 2, 2, 3, 3 };
		static int diceValuePos = 0;

		static int diceMode = 3;

		// reads in the player information for a new game
		static void ResetGame()
		{
			// Get players
			Console.Write("Please enter the number of players who want to play [1-4]: ");
			int playerCount = int.Parse(Console.ReadLine());

			// Reset player info array
			players = new Player[playerCount];

			// Get player names and set locations
			for (int i = 0; i < playerCount; i++)
			{
				// Get name
				Console.Write($"Please enter name for player {i + 1}: ");
				players[i].Name = Console.ReadLine();

				// Set location
				players[i].X = 0;
				players[i].Y = 0;

				// Set ID
				players[i].ID = i;
			}
		}

		// returns the value of the next dice throw
		static int DiceThrow(int type)
		{
			// Type 1 - Always roll 1
			if (type == 1)
			{
				return 1;
			}

			// Type 2 - Set sequence of rolls
			else if (type == 2)
			{
				int spots = diceValues[diceValuePos];
				diceValuePos = diceValuePos + 1;
				if (diceValuePos == diceValues.Length)
				{
					diceValuePos = 0;
				}
				return spots;
			}

			// Type 3 - Random
			else if (type == 3)
			{
				return rand.Next(1, 7);
			} else
			{
				return 0;
			}
		}

		// makes a move for the player given in playerNo
		private static void PlayerTurn(int playerNo)
		{
			// Get player info
			ref var player = ref players[playerNo];

			// Roll
			int roll = DiceThrow(diceMode);

			// Define current square
			int[] newSquare = new int[2] { player.X, player.Y };

			// Handler to find new square
			void move(int distance, char direction)
			{
				//Console.WriteLine($"Moving: {direction}, {distance}");
				// Get new square
				switch (direction)
				{
					// Up
					case 'U':
						newSquare[1] += distance;
						break;

					// Down
					case 'D':
						newSquare[1] -= distance;
						break;

					// Left
					case 'L':
						newSquare[0] -= distance;
						break;

					// Right
					case 'R':
						newSquare[0] += distance;
						break;
				}
			}

			// Print roll
			Console.WriteLine($"Player: {player.Name} has rolled {roll}!");

			// Move according to roll
			move(roll, movementBoard[player.Y, player.X]);

			// Test if new space is occupied
			bool bounce = false;
			do
			{
				bounce = RocketInSquare(newSquare[0], newSquare[1]);

				// if bounce move 1
				if (bounce)
				{
					Console.WriteLine("Jump space occupied! Recalculating!");
					//Console.WriteLine($"Direction: {newSquare[0]} {newSquare[1]}");
					move(1, movementBoard[newSquare[1], newSquare[0]]);
				}
			} while (bounce);

			// Check if off map - Return if off map
			if (newSquare[0] < 0 || newSquare[0] > 7 || newSquare[1] < 0 || newSquare[1] > 7)
			{
				Console.WriteLine("No existing jump lanes found! Jump failed!");
				return;
			}
			else
			{
				// Move player to new pos
				player.X = newSquare[0];
				player.Y = newSquare[1];
				Console.WriteLine($"Jump Successful! Jumped to ({player.X}, {player.Y})");
			}

			// Check for cheese at new location
			if (cheeseBoard[player.Y, player.X] == 1)
			{
				// Run Cheese Power Logic
				CheesePower(ref player);
			}

			// Check for winner
			if (player.X == 7 && player.Y == 7)
			{
				gameOver = true;
				Console.WriteLine($"{player.Name} has won!");
			}
		}

		// returns true if there is a rocket in the specified square
		static bool RocketInSquare(int X, int Y)
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
		static void ShowStatus()
		{
			Console.WriteLine("======================================");
			Console.WriteLine("Hyperspace Cheese Battle Status Report");
			Console.WriteLine("======================================");
			if (players.Length == 1)
			{
				Console.WriteLine($"There is {players.Length} player in the game");
			} else
			{
				Console.WriteLine($"There are {players.Length} players in the game");
			}
			for (int i = 0; i < players.Length; i++)
			{
				Console.WriteLine($"{players[i].Name} is on square ({players[i].X}, {players[i].Y}) ");
			}
			Console.WriteLine("======================================");
			Console.WriteLine("     Press enter to end continue.     ");
			Console.WriteLine("======================================");
			Console.ReadLine();
		}

		// Run through one round
		static void RunRound()
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

		// Cheese Power Logic
		static void CheesePower(ref Player player)
		{
			// Get choice
			Console.WriteLine("Scanners detect high concentration of Cheese Power!");
			Console.WriteLine("Deploying Cheese Power Vacuum!");
			Console.WriteLine("Determine what to do with the excese Cheese Power:");
			Console.WriteLine("1) Charge the Cheese Deathray and fire at a compeditor!");
			Console.WriteLine("2) Supercharge engines and preform a second jump!");
			Console.Write("Please enter choice: ");
			int power = int.Parse(Console.ReadLine());

			// Powers
			switch (power)
			{
				// Cheese Deathray
				case 1:
					// if single player
					if (players.Length == 1)
					{
						Console.WriteLine("Error! No hostile ships detected!");
						Console.WriteLine("Warning! Cheese Deathray already charged!");
						Console.WriteLine("Firing at random to prevent self destruction!");
						Console.WriteLine("Hopefully nothing important was hit...");
					} else
					{
						// Print valid targets
						Console.WriteLine("Target options:");
						for (int i = 0; i < players.Length; i++)
						{
							Console.WriteLine($"{i + 1}) {players[i].Name}");
						}

						// Get input
						Console.Write("Please select the target: ");
						int targetNo = int.Parse(Console.ReadLine()) - 1;
						ref var target = ref players[targetNo];

						if (target.ID == player.ID)
						{
							Console.WriteLine($"Error! Selected target is {player.Name}!");
							Console.WriteLine("Oh No! The computer accepted the target!");
						}

						// Fire at target
						Console.WriteLine($"{player.Name} fires at {target.Name}!");
						Console.WriteLine($"{target.Name} was hit and forced out of the jump lanes!");
						Console.WriteLine($"Under emergany power {target.Name}'s ship limps back to make repairs!");
						Console.WriteLine($"After quick repairs {target.Name}'s ship is back underway!");
						Console.Write($"{target.Name} please select starting location [0-7]: ");
						target.Y = 0;
						target.X = int.Parse(Console.ReadLine());
						Console.WriteLine($"{target.Name} is now at sector ({target.X}, {target.Y})");
					}
					break;

				// Engine Recharge (Extra turn)
				case 2:
					Console.WriteLine("Engines overcharged! Preparing for second jump!");
					PlayerTurn(player.ID);
					break;
			}
		}

		static void Main(string[] args)
		{
			ResetGame();

			// Loop through rounds until game over
			while (!gameOver)
			{
				RunRound();
			}

			// Do not exit on end of game
			Console.WriteLine("Why are you here?");
			Console.ReadLine();
		}
	}
}
