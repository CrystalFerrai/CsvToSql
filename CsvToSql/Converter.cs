// Copyright 2024 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text;

namespace CsvToSql
{
	/// <summary>
	/// Helper for performing CSV to SQL conversions
	/// </summary>
	internal static class Converter
	{
		/// <summary>
		/// Run the converter
		/// </summary>
		/// <param name="config">Config containing information about the conversion</param>
		/// <param name="logWriter">For logging status updates</param>
		/// <exception cref="ConverterException">An error occurred during conversion</exception>
		public static void Run(Config config, TextWriter logWriter)
		{
			using FileStream outFile = File.Create(config.OutPath);
			using StreamWriter writer = new(outFile, Encoding.UTF8) { NewLine = "\n" };

			writer.WriteLine("set names utf8mb4;");
			writer.WriteLine("start transaction;");
			
			foreach (InputFile inputFile in config.InputFiles)
			{
				logWriter.WriteLine($"Converting {Path.GetFileName(inputFile.Path)}...");

				writer.WriteLine();

				using FileStream inFile = File.OpenRead(inputFile.Path);
				using StreamReader reader = new(inFile, Encoding.UTF8);

				// Skip first line
				if (reader.ReadLine() is null)
				{
					throw new ConverterException($"Input file \"{Path.GetFileName(inputFile.Path)}\" has no content.");
				}

				writer.WriteLine($"truncate {inputFile.TableName};");

				int lineNumber = 1;
				int n = 0;
				string? line;
				List<string> fields = new();
				while ((line = reader.ReadLine()) is not null)
				{
					fields.Clear();

					if (line.IndexOf('"') < 0)
					{
						// Simple case - no enclosures
						fields.AddRange(line.Split(','));
					}
					else
					{
						fields.AddRange(ParseCsvRow(line, reader));
					}

					if (fields.Count > inputFile.FieldTypes.Count)
					{
						throw new ConverterException($"Row {lineNumber} contains {fields.Count} fields, but only {inputFile.FieldTypes.Count} field types were specified on the command line.");
					}

					if (n == 0)
					{
						writer.WriteLine($"insert into {inputFile.TableName} values");
					}

					writer.Write($"(");
					for (int i = 0; i < fields.Count - 1; ++i)
					{
						WriteField(writer, fields[i], inputFile.FieldTypes[i]);
						writer.Write(", ");
					}
					if (fields.Count > 0)
					{
						WriteField(writer, fields[fields.Count - 1], inputFile.FieldTypes[fields.Count - 1]);
					}

					if (n == 999)
					{
						writer.WriteLine($");");
						n = 0;
					}
					else
					{
						writer.WriteLine($"),");
						++n; 
					}

					++lineNumber;
				}

				// Replace final comma + newline with semicolon + newline
				// (The final line may already have a semicolon, but this is still fine in that case)
				writer.Flush();
				outFile.Seek(-2, SeekOrigin.Current);
				writer.WriteLine($";");
			}

			writer.WriteLine();
			writer.WriteLine("commit;");
		}

		private static void WriteField(TextWriter writer, string field, string fieldType)
		{
			switch (fieldType.ToLowerInvariant())
			{
				case "string":
					writer.Write(Sanitize(field));
					break;
				case "int":
					{
						if (int.TryParse(field, out int result))
						{
							writer.Write(result);
						}
						else
						{
							writer.Write("null");
						}
					}
					break;
				case "float":
				case "double":
					{
						if (double.TryParse(field, out double result))
						{
							writer.Write(result);
						}
						else
						{
							writer.Write("null");
						}
					}
					break;
				case "bool":
					{
						if (bool.TryParse(field, out bool result))
						{
							writer.Write(result);
						}
						else
						{
							writer.Write("null");
						}
					}
					break;
				default:
					throw new ConverterException($"Field type {fieldType} is not implemented");
			}
		}

		private static string Sanitize(string field)
		{
			return $"'{field.Replace("'", "''")}'";
		}

		private static IEnumerable<string> ParseCsvRow(string line, TextReader reader)
		{
			int fieldStart = 0;
			ParseState state = ParseState.None;

			int i = 0;

		ProcessLine:
			for (; i < line.Length; ++i)
			{
				switch (state)
				{
					case ParseState.None:
						if (i == fieldStart && line[i] == '"')
						{
							state = ParseState.InEnclosure;
							fieldStart = i + 1;
						}
						else if (line[i] == ',')
						{
							yield return line.Substring(fieldStart, i - fieldStart).Replace("\"\"", "\"");
							fieldStart = i + 1;
						}
						break;
					case ParseState.InEnclosure:
						if (line[i] == '"')
						{
							bool escapedQuote = i < line.Length - 1 && line[i + 1] == '"';
							if (i == line.Length - 1 || !escapedQuote)
							{
								yield return line.Substring(fieldStart, i - fieldStart).Replace("\"\"", "\"");

								state = ParseState.None;
								for (; i < line.Length; ++i)
								{
									if (line[i] == ',')
									{
										fieldStart = i + 1;
										break;
									}
								}
							}
							else if (escapedQuote)
							{
								++i;
							}
						}
						break;
				}
			}
			if (state == ParseState.InEnclosure)
			{
				// Row continues to another line
				string? nextLine = reader.ReadLine();
				if (nextLine is null)
				{
					throw new ConverterException("Reached end of file without finishing a row. Verify input file is valid csv.");
				}

				line += "<br />" + nextLine;
				goto ProcessLine;
			}
			else if (line[line.Length - 1] != '"')
			{
				yield return line.Substring(fieldStart, line.Length - fieldStart).Replace("\"\"", "\"");
			}
		}

		private enum ParseState
		{
			None,
			InEnclosure
		}
	}

	/// <summary>
	/// Exception thrown by <see cref="Converter"/> when an error occurs
	/// </summary>
	internal class ConverterException : Exception
	{
		public ConverterException()
		{
		}

		public ConverterException(string? message)
			: base(message)
		{
		}

		public ConverterException(string? message, Exception? innerException)
			: base(message, innerException)
		{
		}
	}
}
