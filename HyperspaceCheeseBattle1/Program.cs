using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperspaceCheeseBattle1
{
	internal class Program
	{
		// Movement board
		// D = Down U = Up R = Right L = Left W = Win
		static readonly char[,] movementBoard = new char[,] {
			{'U','U','U','U','U','U','U','U'},  // row 0
			{'R','R','U','D','U','U','L','L'},  // row 1
			{'R','R','U','R','L','R','L','L'},  // row 2
			{'R','R','U','R','U','U','L','L'},  // row 3
			{'R','R','R','R','U','U','L','L'},  // row 4
			{'R','R','R','R','U','U','L','L'},  // row 5
			{'R','R','U','D','U','R','L','L'},  // row 6
			{'D','R','R','R','R','R','D','W'}   // row 7
		};

		// Player struct
		struct Player
		{
			public string Name;
			public int X;
			public int Y;
		}

		// Player info array
		static Player[] players;

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
			}
		}

		// returns the value of the next dice throw
		static int DiceThrow()
		{
			return 1;
		}

		// makes a move for the player given in playerNo
		private static void PlayerTurn(int playerNo)
		{
			// Get player info
			ref var player = ref players[playerNo];

			// Roll
			int roll = DiceThrow();

			// Define current square
			int[] newSquare = new int[2] { player.X, player.Y };

			// Handler to find new square
			void move(int distance, char direction)
			{
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
			bool bounce;
			do
			{
				bounce = RocketInSquare(newSquare[0], newSquare[1]);

				// if bounce move 1
				if (bounce)
				{
					Console.WriteLine("Jump space occupied! Recalculating!");
					move(1, movementBoard[newSquare[1], newSquare[0]]);
				}
			} while (bounce);

			// Check if off map - Return if off map
			if (newSquare[0] < 0 || newSquare[0] > 7 || newSquare[1] < 0 || newSquare[1] > 7)
			{
				Console.WriteLine("No existing jump lanes found! Jump failed!");
			}
			else
			{
				// Move player to new pos
				player.X = newSquare[0];
				player.Y = newSquare[1];
				Console.WriteLine($"Jump Successful! Jumped to ({player.X}, {player.Y})");
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

		static void Main()
		{
			ResetGame();

			for (int i = 0; i < players.Length; i++)
			{
				PlayerTurn(i);
			}

			Console.Read();
		}
	}
}
