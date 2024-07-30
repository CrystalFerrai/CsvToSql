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

namespace CsvToSql
{
	/// <summary>
	/// Converter configuration options
	/// </summary>
	internal class Config
	{
		/// <summary>
		/// The path to output the resulting SQL file
		/// </summary>
		public string OutPath { get; }

		/// <summary>
		/// Paths to input CSV files
		/// </summary>
		public IReadOnlyList<InputFile> InputFiles { get; }

		private Config(string outPath, IReadOnlyList<InputFile> inputFiles)
		{
			OutPath = outPath;
			InputFiles = inputFiles;
		}

		/// <summary>
		/// Loads a config from a file
		/// </summary>
		/// <param name="path">The config file path</param>
		/// <returns>The loaded config</returns>
		/// <exception cref="ArgumentException">There is an issue with a parameter</exception>
		/// <exception cref="ConfigException">An error occurred while parsing the config file</exception>
		/// <exception cref="FileNotFoundException">The config specifies an input file that does not exist</exception>
		public static Config Load(string path)
		{
			string fullPath = Path.GetFullPath(path);
			string baseDir = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException($"Unable to parse \"{path}\" as a path.", nameof(path));

			using (FileStream file = File.OpenRead(fullPath))
			using (StreamReader reader = new(file))
			{
				int lineNumber = 1;

				string? outPath = ReadLine(reader);
				if (outPath is null) throw new ConfigException("Config file not long enough.");

				string outFullPath = Path.Combine(baseDir, outPath);

				string? outDir = Path.GetDirectoryName(outFullPath);
				if (outDir is null)
				{
					throw new ConfigException($"Could not parse output path: {outPath}");
				}
				if (!Directory.Exists(outDir))
				{
					try
					{
						Directory.CreateDirectory(outDir);
					}
					catch (Exception ex)
					{
						throw new ConfigException($"Error creating output directory \"{outDir}\". [{ex.GetType().FullName}] {ex.Message}");
					}
				}

				++lineNumber;

				List<InputFile> inputFiles = new();
				while (!reader.EndOfStream)
				{
					string inputLine = ReadLine(reader)!;

					string[] inputLineParts = inputLine.Split('|');
					if (inputLineParts.Length != 2) throw new ConfigException($"Unable to parse config line {lineNumber}.");

					string inPath = inputLineParts[0].Trim();
					string inFullPath = Path.Combine(baseDir, inPath);

					if (!File.Exists(inFullPath))
					{
						throw new FileNotFoundException("Unable to find input file specified in config.", inFullPath);
					}

					string table;
					string[] fieldTypes;
					{
						string schema = inputLineParts[1];
						int sep = schema.IndexOf('(');
						if (sep < 0)
						{
							throw new ConfigException($"Unable to parse table schema on config line {lineNumber}. Expected '('");
						}
						table = schema.Substring(0, sep);

						if (!schema.EndsWith(')'))
						{
							throw new ConfigException($"Unable to parse table schema on config line {lineNumber}. Expected ')'");
						}
						string types = schema.Substring(sep + 1, schema.Length - sep - 2);
						fieldTypes = types.Split(',');
						for (int i = 0; i < fieldTypes.Length; ++i)
						{
							fieldTypes[i] = fieldTypes[i].Trim();
						}
					}

					inputFiles.Add(new() { Path = inFullPath, TableName = table, FieldTypes = fieldTypes });

					++lineNumber;
				}

				return new(outFullPath, inputFiles);
			}
		}

		private static string? ReadLine(TextReader reader)
		{
			string? line = null;
			do
			{
				line = reader.ReadLine()?.Trim();
			} while (line is not null && (line.Length == 0 || line.StartsWith('#')));
			return line;
		}
	}

	/// <summary>
	/// Information about a covnersion input file
	/// </summary>
	internal struct InputFile
	{
		public string Path;
		public string TableName;
		public IReadOnlyList<string> FieldTypes;

		public override string ToString()
		{
			return Path;
		}
	}

	/// <summary>
	/// Exception thrown by <see cref="Config"/> when an error occurs
	/// </summary>
	internal class ConfigException : Exception
	{
		public ConfigException()
		{
		}

		public ConfigException(string? message)
			: base(message)
		{
		}

		public ConfigException(string? message, Exception? innerException)
			: base(message, innerException)
		{
		}
	}
}
