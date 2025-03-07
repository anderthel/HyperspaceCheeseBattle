using HyperspaceCheeseBattle3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperspaceCheeseBattle3.Utilities
{
	static class MapString
	{
		// Method to convert boards to a string format
		public static string BoardsToString(ref SpotLocation startingLoc, ref Directions[,] movementBoard, ref int[,] cheeseBoard)
		{
			// Create string builder instance
			StringBuilder sb = new StringBuilder();

			// Append start location
			sb.Append(startingLoc.ToString());
			sb.Append(":");

			// Append X and Y
			sb.Append(movementBoard.GetLength(1).ToString()); // X
			sb.Append(":");
			sb.Append(movementBoard.GetLength(0).ToString()); // Y
			sb.Append(":");

			// Convert movementBoard to string
			for (int i = 0; i < movementBoard.GetLength(0); i++)
			{
				for (int j = 0; j < movementBoard.GetLength(1); j++)
				{
					// Append the char
					sb.Append(movementBoard[i, j]);
					if (j < movementBoard.GetLength(1) - 1 || i < movementBoard.GetLength(0) - 1)
					{
						// Append delimiter
						sb.Append(",");
					}
				}
			}

			// Separator between arrays
			sb.Append(":");

			// Convert cheeseBoard to string
			for (int i = 0; i < cheeseBoard.GetLength(0); i++)
			{
				for (int j = 0; j < cheeseBoard.GetLength(1); j++)
				{
					// Append the char
					sb.Append(cheeseBoard[i, j]);
					if (j < cheeseBoard.GetLength(1) - 1 || i < cheeseBoard.GetLength(0) - 1)
					{
						// Append delimiter
						sb.Append(",");
					}
				}
			}

			// Return
			return sb.ToString();
		}

		// Method to load boards from a string format
		public static void StringToBoards(string data, ref SpotLocation startingLoc, ref Directions[,] movementBoard, ref int[,] cheeseBoard)
		{
			// Split the input data into parts
			var parts = data.Split(':');

			// Parse the starting location
			startingLoc = (SpotLocation)Enum.Parse(typeof(SpotLocation), parts[0]);

			// Parse X and Y
			int x = int.Parse(parts[1]);
			int y = int.Parse(parts[2]);

			// Initialize boards
			movementBoard = new Directions[y, x];
			cheeseBoard = new int[y, x];

			// Fill the boards
			var movementData = parts[3].Split(',');
			var cheeseData = parts[4].Split(',');
			for (int i = 0; i < y; i++)
			{
				for (int j = 0; j < x; j++)
				{
					movementBoard[i, j] = (Directions)Enum.Parse(typeof(Directions), movementData[i * x + j]);           // Convert string to char
					cheeseBoard[i, j] = int.Parse(cheeseData[i * x + j]);
				}
			}
		}
	}
}
