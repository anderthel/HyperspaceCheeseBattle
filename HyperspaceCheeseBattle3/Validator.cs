using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InputValidatorLib
{
	static class Validator
	{
		// Input Validator - Returns validated input based on settings
		public static dynamic InputValidator(
			string prompt,                      // The prompt message to display to the user
			dynamic expected = null,            // The expected input values or type
			int tries = 5,                      // The number of tries allowed (-1 for infinite)
			bool caseSensitive = false,         // Whether the comparison is case sensitive
			bool allowEmpty = false,            // Whether empty input is allowed
			bool validate = false,              // Whether to validate the input with a confirmation
			bool hardExit = false,              // Whether to hard exit if tries are exhausted
			bool numRange = false,              // Whether to validate input within a numeric range
			bool stringLengthRange = false,     // Whether to validate input with string length range
			int numMin = 0,                     // The minimum numeric value (for numRange/stringLengthRange)
			int numMax = 100,                   // The maximum numeric value (for numRange/stringLengthRange)
			dynamic strTest = null,             // The string test to perform (e.g., isNumeric)
			bool printAcceptedValues = true     // Lists out the accepted values
		)
		{
			while (tries > 0 || tries == -1)
			{
				// Define values
				dynamic returnValue = null;             // Variable to store the validated input
				string errorMessage = string.Empty;     // Variable to store error messages

				// Get input
				Console.Write(prompt);
				string inputText = Console.ReadLine();

				// Reduce tries count if not infinite
				if (tries != -1)
				{
					tries--;
				}

				// Test if empty and not allowed
				if (string.IsNullOrEmpty(inputText))
				{
					if (allowEmpty)
					{
						returnValue = inputText;        // Set return value if allowEmpty
					}
					else
					{
						errorMessage = "Error: input cannot be empty.";     // Set error message if allowEmpty False
					}
				}

				// Test if expected is a list of values
				else if (expected is IEnumerable<string>)
				{
					// Marker for if found in expected
					bool found = false;

					// Check each element in the expected list
					foreach (string item in expected)
					{
						// Case Sensitive
						if (caseSensitive && inputText == item)
						{
							returnValue = inputText;
							break;
						}
						// Non Case Sensitive
						else if (!caseSensitive && inputText.Equals(item, StringComparison.OrdinalIgnoreCase))
						{
							returnValue = item;
							break;
						}
					}

					// Handle if not found
					if (!found)
					{
						errorMessage = $"Error: '{inputText}' not in the expected list";
						if (caseSensitive)
						{
							errorMessage += " (case sensitive).";
						}
						else
						{
							errorMessage += ".";
						}
					}
				}

				// Test if expected is dictionary
				else if (expected is IDictionary<string, string>)
				{
					// if Case Sensitive
					if (expected.ContainsKey(inputText))
					{
						returnValue = expected[inputText];
					}
					// if not case sensitive
					else if (!caseSensitive)
					{
						// loop through each key and check with ignore case
						foreach (var key in expected.Keys)
						{
							if (key.Equals(inputText, StringComparison.OrdinalIgnoreCase))
							{
								returnValue = expected[key];
								break;
							}
						}
					}
					// Else if not found set errorMessage
					else
					{
						errorMessage = $"Error: '{inputText}' not found in the expected dictionary keys";
						if (caseSensitive)
						{
							errorMessage += " (case sensitive).";
						}
						else
						{
							errorMessage += ".";
						}
					}
				}

				// Try to convert to enum
				else if (expected is Type && expected.IsEnum)
				{
					// Try to parse
					try
					{
						returnValue = Enum.Parse(expected, inputText, !caseSensitive);
					}
					catch (ArgumentException)
					{
						errorMessage = $"Error: input '{inputText}' is not a valid value for the enum '{expected.Name}'.";
					}
				}

				// Try to convert to expected type
				else if (expected is Type)
				{
					try
					{
						// Convert return value to given type
						returnValue = Convert.ChangeType(inputText, expected, CultureInfo.InvariantCulture);

						// if numRange validate is within expected range
						if (numRange && (expected == typeof(int) || expected == typeof(double)))
						{
							// if less then min or greater than max
							if (returnValue < numMin || returnValue > numMax)
							{
								errorMessage = $"Error: '{returnValue}' is out of range ({numMin}-{numMax}).";
								returnValue = null;
							}
						}

						// if stringLengthRange validated is within length range
						else if (stringLengthRange && (returnValue.Length < numMin || returnValue.Length > numMax))
						{
							errorMessage = $"Error: Length of '{returnValue}' ({returnValue.Length}) is out of range ({numMin}-{numMax}).";
							returnValue = null;
						}

						// Validate input with specified string test
						else if (strTest != null && !StringTest(inputText, strTest))
						{
							errorMessage = $"Error: '{inputText}' does not pass '{strTest}' test.";
							returnValue = null;
						}

					}
					catch (Exception ex)
					{
						errorMessage = $"Error: input '{inputText}' cannot be converted to '{expected.Name}'. Exception: {ex.Message}";
					}
				}
				else
				{
					errorMessage = $"Error: '{inputText}' is not a valid input.";
				}

				// Validate the input if required
				if (returnValue != null)
				{
					if (validate)
					{
						string confirmation = InputValidator($"Please confirm that '{returnValue}' is correct. [y/n]: ", new string[] { "y", "n" });
						if (confirmation == "y")
						{
							return returnValue;             // Return validated input
						}
						else
						{
							Console.WriteLine("Value was incorrect. Try again.");
							if (tries != -1) tries++;       // Restore the try if input is incorrect
						}
					}
					else
					{
						return returnValue;                 // Return input
					}
				}
				else
				{
					// Append tries left
					if (tries >= 0)
					{
						errorMessage += " Tries left: " + tries;
					}

					// Append accepted values
					if (printAcceptedValues && !numRange)
					{
						// Accepted values
						List<string> values = new List<string>();

						// Get accepted values
						if (expected.GetType().IsEnum)
						{
							values = new List<string>(Enum.GetNames(expected));
						}
						else if (expected is IDictionary)
						{
							values = new List<string>(expected.Keys);
						} else if (expected is IEnumerable)
						{
							foreach (var item in expected)
							{
								values.Add(item.ToString());
							}
						}

						// Append values
						if (values.Any())
						{
							errorMessage += $" Possible values: {string.Join(", ", values.ConvertAll(v => $"'{v}'"))}.";
						}
						else
						{
							errorMessage += $" Possible values could not be determined.";
						}
					}

					// Print error message
					Console.WriteLine(errorMessage);
				}
			}

			// If ran out of tries
			Console.Write("Ran out of tries. Press enter key to exit.");
			Console.ReadLine();

			// Exit
			if (hardExit)
			{
				Environment.Exit(13);       // Exit the application with status code 13
			}

			return null;
		}

		// Overflow to allow for single string tests
		public static bool StringTest(string input, string test)
		{
			return StringTest(input, new List<string> { test });
		}

		// Function to preform string tests
		public static bool StringTest(string input, List<string> tests)
		{
			// Run each test
			foreach (string test in tests)
			{
				// Convert test to lower
				switch (test.ToLower())
				{
					// All chars are numeric
					case "isnumeric":
						if (!input.All(char.IsDigit)) return false;
						break;

					// All chars are ascii
					case "isascii":
						if (!input.All(c => c <= 127)) return false;
						break;

					// All chars are letters
					case "isletter":
						if (!input.All(char.IsLetter)) return false;
						break;

					// All chars are lower
					case "islower":
						if (!input.All(char.IsLower)) return false;
						break;

					// All chars are upper
					case "isupper":
						if (!input.All(char.IsUpper)) return false;
						break;

					// All chars are whitespace
					case "iswhitespace":
						if (!input.All(char.IsWhiteSpace)) return false;
						break;

					// All chars are punctuation
					case "ispunctuation":
						if (!input.All(char.IsPunctuation)) return false;
						break;

					// All chars are letters or digits
					case "isalphanumeric":
						if (!input.All(char.IsLetterOrDigit)) return false;
						break;

					// All chars are control characters
					case "iscontrol":
						if (!input.All(char.IsControl)) return false;
						break;

					// All chars are separators
					case "isseparator":
						if (!input.All(char.IsSeparator)) return false;
						break;

					// All chars are symbols
					case "issymbol":
						if (!input.All(char.IsSymbol)) return false;
						break;

					// Error for unrecognized test
					default:
						return false;
				}
			}

			// All tests passed
			return true;
		}
	}
}
