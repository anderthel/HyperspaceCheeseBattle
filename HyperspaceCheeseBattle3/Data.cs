using System;

namespace HyperspaceCheeseBattle3.Data
{
	// Player struct
	public struct Player
	{
		public string Name { get; set; }             // Name
		public int ID { get; set; }                  // Index
		public int X { get; set; }                   // X
		public int Y { get; set; }                   // Y
		public bool Bot { get; set; }                // Are they a bot
		public ConsoleColor Color { get; set; }      // Player color
		public int StuckTime { get; set; }           // Turns without movement
	}


	// Dice Modes
	public enum DiceMode
	{
		Ones,
		Sequence,
		Standard,
	}

	// Spots on board
	public enum SpotLocation
	{
		BottomRight = 0,
		BottomLeft = 1,
		TopRight = 2,
		TopLeft = 3,
		Random = 4,
		Middle = 5
	}

	// Cheese spots
	public enum CheeseDistributionMode
	{
		Count,
		Chance,
		Percentage
	}

	// Map Directions
	public enum Directions
	{
		Up,
		Down,
		Left,
		Right,
		Win
	}

	// Powers
	public enum CheesePowers
	{
		Deathray,
		SecondTurn,

	}
}
