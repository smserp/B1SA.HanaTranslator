B1SA.HanaTranslator
===================

SMS Fork
--------
- The main focus of our changes were easy consumption of the SQL string translation feature as a NuGet package
- This library is based on the [_Query Migration Tool for Microsoft SQL Server to SAP HANA_](https://github.com/B1SA/HanaTranslator-Src), which is marked as being _archived_
  - fork date: `2017-11-10`
  - git revision: `e53f0ecce5fabdac2c5787ae1311437a148552e5`
- Like the original code the changes are licensed under the MIT license as well
  - The used ANTLR libraries are licensed under the BSD license

### Changes
- Bumped version to `2.x.x` to distinguish from the original code
- Targeting _.NET Standard 2.1_ (compiled with VS 2022)
- ANTLR
  - Based on version `3.5.2-rc1`, which is marked as a _Release Candidate_, but works as expected and seems to stay the last release of the version 3 branch
  - Integrated & adapted _Antlr3.Runtime_ source to also target _.NET Standard 2.1_
  - Use official _Antlr3 (Generator)_ package instead of local one. It only supports _.NET Standard 2.0_, but is solely used during build-time to generate the parser and lexer code, which takes a few moments
- Removed UI project that was just a consumer of the library
- Removed DB connectivity and with it the automatic case fixing
- Repository clean up & streamlining for packaging
- Replaced static file `config.txt` with a non-static `Config` object
- Moved to the more unique and origin honouring namespace `B1SA.HanaTranslator`
- Added rudimentary unit tests
- And many more small improvements...

### Usage
````csharp
using B1SA.HanaTranslator;

var translator = new Translator(new Config() {
    TranslationComments = true,
    FormatOutput = true
});

var hanaSql = translator.Translate(
    """
    SELECT "Item", ISNULL("Quantity", 0) AS "Quantity" FROM "Table"
    """,
    out string summary,
    out int statements,
    out int errors
);
````


The original README
-------------------
### Query Migration Tool for Microsoft SQL Server to SAP HANA
The Query Migration Tool for Microsoft SQL Server to SAP HANA is a semi-automatic tool that helps convert most of the data-definition language (DDL) and data-manipulation language (DML).
It will help you to convert structured query language (SQL) in the Microsoft SQL Server database (using T-SQL grammar) to SQL that can be used in the SAP HANA™ database (using ANSI-SQL grammar).

After the conversion, you must check whether the converted version is correct according to your needs. 

This tool supports most of the official T-SQL grammar, and some well-known and widely-used undocumented feature. For more information about the official T-SQL grammar, see the MSDN Library. 

If SAP HANA does not support certain SQL, this tool will do the following:

* Find equivalents in the SAP HANA database and convert the SQL.
* Delete the SQL in the input file and display relevant comments in the output file.
* Leave the SQL in the input file as it is, for example, the WITH statement.

In order to find out more details please follow the details on the SAP community blog https://blogs.sap.com/2013/04/10/how-to-convert-sql-from-ms-sql-server-to-sap-hana/.

#### Prerequisites
The provided source code is a .NET solution. You will need Microsoft Visual Studio installed in your own environment to be able to recompile the provided source code.

#### License
* There is no guarantee or support on the provided source code.
* The provided source code might use external frameworks and libraries, pay attention if you are building a product that you have the required licenses.
* The Query Migration Tool for Microsoft SQL Server to SAP HANA is released under the terms of the MIT license. See LICENSE for more information or see https://opensource.org/licenses/MIT.

#### Special thanks
Thanks to the SAP Business One development team for his collaboration on getting this tool implemented and published here.
