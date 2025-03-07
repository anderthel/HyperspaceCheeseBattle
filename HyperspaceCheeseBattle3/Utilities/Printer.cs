using System;

namespace HyperspaceCheeseBattle3.Utilities
{
    static class Printer
	{
		// Printer - Print Line
		public static void PrintLine(string text, bool output)
		{
			// if outputGame false suppress printing
			if (!output)
			{
				return;
			}

			// write line
			Console.WriteLine(text);
		}

		// Printer - Print
		public static void Print(string text, bool output)
		{
			// if outputGame false suppress printing
			if (!output)
			{
				return;
			}

			// write
			Console.Write(text);
		}

		// Print text centered
		public static void PrintCentered(string text, int lineSize, bool output)
		{
			int padding = (lineSize - text.Length) / 2;
			PrintLine(text.PadLeft(padding + text.Length).PadRight(lineSize), output);
		}

		// Print break line
		public static void PrintBreak(char c, int lineSize, bool output)
		{
			PrintLine(new string(c, lineSize), output);
		}

		// Print empty line
		public static void PrintEmpty(bool output)
		{
			PrintLine(string.Empty, output);
		}
	}
}
