using System;
using System.Collections.Generic;
using static HyperspaceCheeseBattle3.Utilities.Printer;
using HyperspaceCheeseBattle3.Data;
using static InputValidatorLib.Validator;

namespace HyperspaceCheeseBattle3
{
	public static class Powers
	{
		// Power Descriptions
		public static readonly IReadOnlyDictionary<CheesePowers, string> Descriptions = new Dictionary<CheesePowers, string>
		{
			{ CheesePowers.Deathray, "Charge the Cheese Death ray and fire at a competitor!" },
			{ CheesePowers.SecondTurn, "Supercharge engines and perform a second jump!" }
		};

		// Deathray
		public static void DeathRay(bool output, ref Player[] players, ref Player player, Random rand, int boardSize)
		{
			// if single player
			if (players.Length == 1)
			{
				PrintLine("Error! No hostile ships detected!", output);
				PrintLine("Warning! Cheese Deathray already charged!", output);
				PrintLine("Firing at random to prevent self destruction!", output);
				PrintLine("Hopefully nothing important was hit...", output);
			}
			else
			{
				// Print valid targets
				PrintLine("Target options:", output);
				for (int i = 0; i < players.Length; i++)
				{
					PrintLine($"{i + 1}) {players[i].Name}", output);
				}

				// Get input
				int targetNo;
				if (!player.Bot)
				{
					targetNo = InputValidator("Please select the target: ", typeof(int), tries: -1, numRange: true, numMin: 1, numMax: players.Length) - 1;
				}
				else
				{
					targetNo = rand.Next(0, players.Length);
				}

				ref var target = ref players[targetNo];

				if (target.ID == player.ID)
				{
					PrintLine($"Error! Selected target is {player.Name}!", output);
					PrintLine("Oh No! The computer accepted the target!", output);
				}

				// Fire at target
				PrintLine($"{player.Name} fires at {target.Name}!", output);
				PrintLine($"{target.Name} was hit and forced out of the jump lanes!", output);
				PrintLine($"Under emergency power {target.Name}'s ship limps back to make repairs!", output);
				PrintLine($"After quick repairs {target.Name}'s ship is back underway!", output);
				target.Y = 0;
				if (!target.Bot)
				{
					target.X = InputValidator($"{target.Name} please select starting location [0-{boardSize - 1}]: ", typeof(int), tries: -1, numRange: true, numMin: 0, numMax: boardSize - 1);
				}
				else
				{
					target.X = rand.Next(0, boardSize - 1);
				}

				// Reset stuck time
				target.StuckTime = 0;

				PrintLine($"{target.Name} is now at sector ({target.X}, {target.Y})", output);
			}
		}

		// Second Jump
		public static void SecondJump(bool output)
		{
			PrintLine("Engines overcharged! Preparing for second jump!", output);
		}
	}
}
