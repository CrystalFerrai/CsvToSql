Command line program to convert a set of CSV files into a SQL file to populate MySQL database tables.

## Purpose

For when you need to import CSV into a MySQL database and do not have the necessary permissions to import it directly via `load data infile`. If you do have import permisisons, then you probably don't need this program.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/CsvToSql/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

You will need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if you do not already have it.

## Usage

First you should have one or more CSV files that each follow the schema of one of your database tables in terms of column counts and types. Basically, one CSV file will become one table in the database.

### Configuration file

Once you have your input data ready, the next step is to create a configuration file for CsvToSql. The file should be a plain text file where each line represents a parameter for the program. The name of the file does not matter.

Here is an example file.

MyConfig.cfg
```
# The path to the output file should be the first line
update.sql

# All subsequent lines represent input files and table schemas
Data/MyTable.csv|my_table(int,string,string,bool,string)
Data/OtherTable.csv|other_table(string,float,string)
```

Empty lines and lines beginning with a `#` are ignored by the program.

**All paths listed in the configuration file are relative to the configuration file itself unless you specify full paths.**

The first line in the file is the path where the program should output the resulting SQL file.

Subsequent lines each represent an input file path and table schema, separated by a pipe (`|`) character. The table schema consists of the name of the table, which should match the name of the table in your database where the data will be imported, followed by the type of each column in the table within parenthesis and comma separated. Note that column type names are simplified and differ from actual database column types. The available types are as follows:

* `string` - A string of characters, aka text. Use for columns such as `varchar`, `text`, `tinytext`, etc.
* `int` - An integer value. Use for columns such as `int`, `smallint`, `bigint`, etc.
* `float` (or `double`) - A floating point value. Use for `float` or `double` columns. Can also be used for `decimal` columns, but be warned the output value may have precision/rounding errors.
* `bool` - A boolean value. Use for `bool` columns.

If you need support for other column types or configuration options, feel free to file an issue or a pull request.

### Running the program

Once you have assembled your input files and built your configuration file, you can run the program, passing the path to the configuration file on the command line.

```
CsvToSql MyConfig.cfg
```

Once complete, you should find the output SQL file at the location you specified in your configuration file.

Example output
```sql
set names utf8mb4;
start transaction;

truncate my_table;
insert into my_table values (1, 'Lorem impsum', 'dolor', true, 'sit amet');
insert into my_table values (2, 'consectetur', 'adipiscing', false, 'elit');

truncate other_table;
insert into other_table ('Hello', 7.9, 'World');
insert into other_table ('Meaning of life?', 42, '');
insert into other_table ('e', 2.718281828459, 'number');

commit;
```

Note that the resulting file expects the tables to already exist int he database, and that it will clear them of all data before inserting new data.

## Building

Clone the repository.
```
git clone  https://github.com/CrystalFerrai/CsvToSql.git
```

You can then open and build CsvToSql.sln.

To publish a build for release, run this command from the directory containing the SLN.
```
dotnet publish -p:DebugType=None -r win-x64 -c Release --self-contained false
```

The resulting build can be located at `CsvToSql\bin\Release\net8.0\win-x64\publish`.