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
	internal class Program
	{
		/// <summary>
		/// Applicaiton entry point
		/// </summary>
		private static int Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.Out.WriteLine("Usage: CsvToSql [config path]. See readme.md for config format.");
				return OnExit(0);
			}

			try
			{
				Config config = Config.Load(args[0]);
				Converter.Run(config, Console.Out);
			}
			catch (Exception ex)
			{
				ConsoleColor color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;

				Console.Error.WriteLine($"An error occured. [{ex.GetType().FullName}] {ex.Message}");

				Console.ForegroundColor = color;

				return OnExit(1);
			}

			Console.Out.WriteLine("Done.");

            return OnExit(0);
		}

		private static int OnExit(int code)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.Out.WriteLine("Press a key to exit");
				Console.ReadKey();
			}
			return code;
		}

	}
}
