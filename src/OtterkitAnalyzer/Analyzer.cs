using System.Diagnostics;
using System.Text;

namespace Otterkit;

/// <summary>
/// Otterkit COBOL Syntax and Semantic Analyzer
/// <para>This parser was built to be easily extensible, with some reusable COBOL parts.</para>
/// <para>It requires a List of Tokens generated from the Lexer and the Token Classifier.</para>
/// </summary>
public static partial class Analyzer
{
    /// <summary>
    /// String <c>FileName</c> is used in the parser as a parameter for the <c>ErrorHandler</c> method.
    /// <para>The error handler will use this to fetch the file and get the correct line and column when displaying the error message.</para>
    /// </summary>
    private static string FileName = string.Empty;

    /// <summary>
    /// Stack string <c>SourceId</c> is used in the parser whenever it needs to know the name of the current source unit (The identifier after PROGRAM-ID).
    /// <para>This is used when checking if a variable already exists in the current source unit, and when adding them to the DataItemSymbolTable class's variable table.
    /// The DataItemSymbolTable class is then used to simplify the codegen process of generating data items.</para>
    /// </summary>
    private static readonly Stack<string> SourceId = new();

    /// <summary>
    /// Stack SourceUnit <c>SourceType</c> is used in the parser whenever it needs to know which <c>-ID</c> it is currently parsing.
    /// <para>This is used when handling certain syntax rules for different <c>-ID</c>s, like the <c>"RETURNING data-name"</c> being required for every <c>FUNCTION-ID</c> source unit.</para>
    /// </summary>
    private static readonly Stack<SourceUnit> SourceType = new();

    /// <summary>
    /// Stack int <c>LevelStack</c> is used in the parser whenever it needs to know which data item level it is currently parsing.
    /// <para>This is used when handling the level number syntax rules, like which clauses are allowed for a particular level number or group item level number rules</para>
    /// </summary>
    private static readonly Stack<int> LevelStack = new();

    /// <summary>
    /// CurrentScope <c>CurrentSection</c> is used in the parser whenever it needs to know which section it is currently parsing (WORKING-STORAGE and LOCAL-STORAGE for example).
    /// <para>This is used when handling certain syntax rules for different sections and to add extra context needed for the SymbolTable class's variable table.
    /// This will also be used by the SymbolTable class during codegen to simplify the process to figuring out if a variable is static or not.</para>
    /// </summary>
    private static CurrentScope CurrentSection;

    /// <summary>
    /// List of Tokens <c>TokenList</c>: This is the main data structure that the parser will be iterating through.
    /// <para>The parser expects a list of already preprocessed and classified COBOL tokens in the form of full COBOL words (CALL, END-IF, COMPUTE for example)</para>
    /// </summary>
    private static List<Token> TokenList = new();

    /// <summary>
    /// Int <c>Index</c>: This is the index of the current token, used by most helper methods including Continue, Current and Lookahead.
    /// <para>The index should only move forwards, but if the previous token is needed you can use the Lookahead and LookaheadEquals methods with a negative integer parameter</para>
    /// </summary>
    private static int Index;

    /// <summary>
    /// Int <c>FileIndex</c>: This is the index of the current file name, used by the parser to point to the correct file name when showing error messages.
    /// <para>The file index should only move forwards.</para>
    /// </summary>
    private static int FileIndex;

    /// <summary>
    /// Otterkit COBOL Syntax Analyzer
    /// <para>This parser was built to be easily extensible, with some reusable COBOL parts.</para>
    /// <para>It requires a List of Tokens generated from the Lexer and the Token Classifier.</para>
    /// </summary>
    public static List<Token> Analyze(List<Token> tokenList, string fileName)
    {
        FileName = fileName;
        TokenList = tokenList;

        // Call the parser's main method
        // This should only return when the parser reaches the true EOF token
        Source();

        // If a parsing error has occured, terminate the compilation process.
        // We do not want the compiler to continue when the source code is not valid.
        if (ErrorHandler.Error) ErrorHandler.Terminate("parsing");

        // Return parsed list of tokens.
        return TokenList;

        // Source() is the main method of the parser.
        // It's responsible for parsing COBOL divisions until the EOF token.
        // If EOF was not returned as the last Token in the list then,
        // the parser has not finished reading through the list of tokens correctly.
        void Source()
        {
            IDENTIFICATION();

            if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

            if (CurrentEquals("DATA")) DATA();

            bool notClassOrInterface = SourceType.Peek() switch
            {
                SourceUnit.Class => false,
                SourceUnit.Interface => false,
                _ => true
            };

            if (notClassOrInterface)
            {
                if (CurrentEquals("PROCEDURE")) PROCEDURE();
            }
            else if (SourceType.Peek() == SourceUnit.Class)
            {
                FactoryObject();
            }
            else if (SourceType.Peek() == SourceUnit.Interface)
            {
                InterfaceProcedure();
            }

            EndMarker();

            if (CurrentEquals("IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID"))
            {
                Source();
            }

            if (CurrentEquals("EOF") && Index < TokenList.Count - 1)
            {
                FileName = OtterkitCompiler.Options.FileNames[FileIndex++];

                Continue();
                Source();
            }
        }


        // Method responsible for parsing the IDENTIFICATION DIVISION.
        // That includes PROGRAM-ID, FUNCTION-ID, CLASS-ID, METHOD-ID, INTERFACE-ID, OBJECT, FACTORY and OPTIONS paragraphs.
        // It is also responsible for showing appropriate error messages when an error occurs in the IDENTIFICATION DIVISION.
        void IDENTIFICATION()
        {
            if (CurrentEquals("IDENTIFICATION"))
            {
                Expected("IDENTIFICATION");
                Expected("DIVISION");

                Expected(".", """
                Missing separator period at the end of this IDENTIFICATION DIVISION header, every division header must end with a separator period
                """, -1, "PROGRAM-ID", "FUNCTION-ID", "ENVIRONMENT", "DATA", "PROCEDURE");
            }

            if (!CurrentEquals("PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "METHOD-ID", "INTERFACE-ID", "OBJECT", "FACTORY"))
            {
                Expected("PROGRAM-ID", """
                Missing source unit ID name (PROGRAM-ID, FUNCTION-ID, CLASS-ID...), the identification division header is optional but every source unit must still have an ID.
                """, 0, "OPTIONS", "ENVIRONMENT", "DATA", "PROCEDURE");
            }

            if (CurrentEquals("PROGRAM-ID"))
            {
                ProgramId();
            }

            else if (CurrentEquals("FUNCTION-ID"))
            {
                FunctionId();
            }

            else if (CurrentEquals("CLASS-ID"))
            {
                ClassId();
            }

            else if (CurrentEquals("INTERFACE-ID"))
            {
                InterfaceId();
            }

            else if (SourceType.Peek() is SourceUnit.Class && CurrentEquals("FACTORY"))
            {
                Factory();
            }

            else if (SourceType.Peek() is SourceUnit.Class && CurrentEquals("OBJECT"))
            {
                Object();
            }

            else if (SourceType.Peek() is SourceUnit.Object or SourceUnit.Factory or SourceUnit.Interface && CurrentEquals("METHOD-ID"))
            {
                MethodId();
            }

            if (CurrentEquals("OPTIONS"))
            {
                Options();
            }
        }

        void Options()
        {
            bool shouldHavePeriod = false;

            Expected("OPTIONS");
            Expected(".");

            if (CurrentEquals("ARITHMETIC"))
            {
                Expected("ARITHMETIC");
                Optional("IS");
                Choice("NATIVE", "STANDARD-BINARY", "STANDARD-DECIMAL");

                shouldHavePeriod = true;
            }

            if (CurrentEquals("DEFAULT"))
            {
                Expected("DEFAULT");
                Expected("ROUNDED");
                Optional("MODE");
                Optional("IS");
                Choice(
                    "AWAY-FROM-ZERO", "NEAREST-AWAY-FROM-ZERO",
                    "NEAREST-EVEN", "NEAREST-TOWARD-ZERO",
                    "PROHIBITED", "TOWARD-GREATER",
                    "TOWARD-LESSER", "TRUNCATION"
                );

                shouldHavePeriod = true;
            }

            if (CurrentEquals("ENTRY-CONVENTION"))
            {
                Expected("ENTRY-CONVENTION");
                Optional("IS");
                Expected("COBOL");

                shouldHavePeriod = true;
            }

            if (shouldHavePeriod) Expected(".");
        }


        // The following methods are responsible for parsing the -ID paragraph.
        // That includes the program, user-defined function, method, class, interface, factory or object identifier that should be specified right after.
        // This is where SourceId and SourceType get their values for a COBOL source unit.
        void ProgramId()
        {
            Token ProgramIdentifier;

            Expected("PROGRAM-ID");
            Expected(".");

            ProgramIdentifier = Current();
            SourceId.Push(ProgramIdentifier.value);
            SourceType.Push(SourceUnit.Program);
            CurrentSection = CurrentScope.ProgramId;

            Identifier();
            if (CurrentEquals("AS"))
            {
                Expected("AS");
                SourceId.Pop();
                String();
                SourceId.Push(Lookahead(-1).value);
            }

            if (CurrentEquals("IS", "COMMON", "INITIAL", "RECURSIVE", "PROTOTYPE"))
            {
                bool isCommon = false;
                bool isInitial = false;
                bool isPrototype = false;
                bool isRecursive = false;

                Optional("IS");

                while (CurrentEquals("COMMON", "INITIAL", "RECURSIVE", "PROTOTYPE"))
                {
                    if (CurrentEquals("COMMON"))
                    {
                        Expected("COMMON");
                        isCommon = true;
                    }

                    if (CurrentEquals("INITIAL"))
                    {
                        Expected("INITIAL");
                        isInitial = true;
                    }

                    if (CurrentEquals("RECURSIVE"))
                    {
                        Expected("RECURSIVE");
                        isRecursive = true;
                    }

                    if (CurrentEquals("PROTOTYPE"))
                    {
                        Expected("PROTOTYPE");
                        SourceType.Pop();
                        SourceType.Push(SourceUnit.ProgramPrototype);
                        isPrototype = true;
                    }
                }

                if (isPrototype && (isCommon || isInitial || isRecursive))
                {
                    ErrorHandler.Parser.Report(FileName, ProgramIdentifier, ErrorType.General, """
                    Invalid prototype. Program prototypes cannot be defined as common, initial or recursive.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, ProgramIdentifier);
                }

                if (isInitial && isRecursive)
                {
                    ErrorHandler.Parser.Report(FileName, ProgramIdentifier, ErrorType.General, """
                    Invalid program definition. Initial programs cannot be defined as recursive.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, ProgramIdentifier);
                }

                if (!isPrototype) Optional("PROGRAM");
            }

            SymbolTable.AddSymbol($"{SourceId.Peek()}", SymbolType.SourceUnitSignature);
            Expected(".", """
            Missing separator period at the end of this program definition
            """, -1, "OPTION", "ENVIRONMENT", "DATA", "PROCEDURE");
        }

        void FunctionId()
        {
            Expected("FUNCTION-ID");
            Expected(".");

            SourceId.Push(Current().value);
            SourceType.Push(SourceUnit.Function);
            CurrentSection = CurrentScope.FunctionId;

            Identifier();

            if (CurrentEquals("AS"))
            {
                Expected("AS");
                String();
            }

            if (CurrentEquals("IS", "PROTOTYPE"))
            {
                Optional("IS");
                Expected("PROTOTYPE");
                SourceType.Pop();
                SourceType.Push(SourceUnit.FunctionPrototype);
            }

            SymbolTable.AddSymbol($"{SourceId.Peek()}", SymbolType.SourceUnitSignature);
            Expected(".", """
            Missing separator period at the end of this function definition
            """, -1, "OPTION", "ENVIRONMENT", "DATA", "PROCEDURE");
        }

        void ClassId()
        {
            Expected("CLASS-ID");
            Expected(".");

            SourceId.Push(Current().value);
            SourceType.Push(SourceUnit.Class);
            CurrentSection = CurrentScope.ClassId;

            Identifier();

            if (CurrentEquals("AS"))
            {
                Expected("AS");
                String();
            }

            if (CurrentEquals("IS", "FINAL"))
            {
                Optional("IS");
                Expected("FINAL");
            }

            if (CurrentEquals("INHERITS"))
            {
                Expected("INHERITS");
                Optional("FROM");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The INHERITS FROM clause must contain at least one class or object name.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();
            }

            if (CurrentEquals("USING"))
            {
                Expected("USING");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The USING clause must contain at least one parameter.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();
            }

            SymbolTable.AddSymbol($"{SourceId.Peek()}", SymbolType.SourceUnitSignature);
            Expected(".", """
            Missing separator period at the end of this class definition
            """, -1, "OPTION", "ENVIRONMENT", "DATA", "FACTORY", "OBJECT");
        }

        void InterfaceId()
        {
            Expected("INTERFACE-ID");
            Expected(".");

            SourceId.Push(Current().value);
            SourceType.Push(SourceUnit.Interface);
            CurrentSection = CurrentScope.InterfaceId;

            Identifier();

            if (CurrentEquals("AS"))
            {
                Expected("AS");
                String();
            }

            if (CurrentEquals("INHERITS"))
            {
                Expected("INHERITS");
                Optional("FROM");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The INHERITS FROM clause must contain at least one class or object name.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();
            }

            if (CurrentEquals("USING"))
            {
                Expected("USING");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The USING clause must contain at least one parameter.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();
            }

            SymbolTable.AddSymbol($"{SourceId.Peek()}", SymbolType.SourceUnitSignature);
            Expected(".", """
            Missing separator period at the end of this interface definition
            """, -1, "OPTION", "ENVIRONMENT", "DATA", "FACTORY", "OBJECT");
        }

        void MethodId()
        {
            Expected("METHOD-ID");
            Expected(".");

            CurrentSection = CurrentScope.MethodId;
            var currentSource = SourceType.Peek();
            var currentId = SourceId.Peek();

            if (currentSource != SourceUnit.Interface && CurrentEquals("GET"))
            {
                Expected("GET");
                Expected("PROPERTY");
                SourceId.Push($"GET {Current().value}");
                SourceType.Push(SourceUnit.MethodGetter);

                SymbolTable.AddSymbol($"{currentId}->{SourceId.Peek()}", SymbolType.SourceUnitSignature);

                Identifier();

            }
            else if (currentSource != SourceUnit.Interface && CurrentEquals("SET"))
            {
                Expected("SET");
                Expected("PROPERTY");

                SourceId.Push($"SET {Current().value}");
                SourceType.Push(SourceUnit.MethodSetter);

                SymbolTable.AddSymbol($"{currentId}->{SourceId.Peek()}", SymbolType.SourceUnitSignature);

                Identifier();
            }
            else // If not a getter or a setter
            {
                Identifier();
                if (CurrentEquals("AS"))
                {
                    Expected("AS");
                    String();
                }
                SourceId.Push(Lookahead(-1).value);

                if (currentSource == SourceUnit.Interface)
                {
                    SourceType.Push(SourceUnit.MethodPrototype);
                }
                else
                {
                    SourceType.Push(SourceUnit.Method);
                }

                SymbolTable.AddSymbol($"{currentId}->{SourceId.Peek()}", SymbolType.SourceUnitSignature);
            }

            if (CurrentEquals("OVERRIDE")) Expected("OVERRIDE");

            if (CurrentEquals("IS", "FINAL"))
            {
                Optional("IS");
                Expected("FINAL");
            }

            Expected(".", """
            Missing separator period at the end of this method definition
            """, -1, "OPTION", "ENVIRONMENT", "DATA", "PROCEDURE");
        }

        void Factory()
        {
            Expected("FACTORY");
            Expected(".");

            SourceType.Push(SourceUnit.Factory);

            if (CurrentEquals("IMPLEMENTS"))
            {
                Expected("IMPLEMENTS");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The IMPLEMENTS clause must contain at least one interface name.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();

                Expected(".");
            }
        }

        void Object()
        {
            Expected("OBJECT");
            Expected(".");

            SourceType.Push(SourceUnit.Object);

            if (CurrentEquals("IMPLEMENTS"))
            {
                Expected("IMPLEMENTS");
                if (!CurrentEquals(TokenType.Identifier))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    The IMPLEMENTS clause must contain at least one interface name.
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                Identifier();
                while (CurrentEquals(TokenType.Identifier)) Identifier();

                Expected(".");
            }
        }

        void FactoryObject()
        {
            if (CurrentEquals("FACTORY") || CurrentEquals("IDENTIFICATION") && LookaheadEquals(3, "FACTORY"))
            {
                IDENTIFICATION();

                if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

                if (CurrentEquals("DATA")) DATA();
                Expected("PROCEDURE");
                Expected("DIVISION");
                Expected(".");

                while (CurrentEquals("METHOD-ID") || CurrentEquals("IDENTIFICATION") && LookaheadEquals(3, "METHOD-ID"))
                {
                    IDENTIFICATION();
                    if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

                    if (CurrentEquals("DATA")) DATA();

                    PROCEDURE();

                    EndMarker();
                }

                EndMarker();
            }

            if (CurrentEquals("OBJECT") || CurrentEquals("IDENTIFICATION") && LookaheadEquals(3, "OBJECT"))
            {
                IDENTIFICATION();

                if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

                if (CurrentEquals("DATA")) DATA();

                Expected("PROCEDURE");
                Expected("DIVISION");
                Expected(".");

                while (CurrentEquals("METHOD-ID") || (CurrentEquals("IDENTIFICATION") && LookaheadEquals(3, "METHOD-ID")))
                {
                    IDENTIFICATION();
                    if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

                    if (CurrentEquals("DATA")) DATA();

                    PROCEDURE();

                    EndMarker();
                }

                EndMarker();
            }
        }

        void InterfaceProcedure()
        {
            if (CurrentEquals("PROCEDURE"))
            {
                Expected("PROCEDURE");
                Expected("DIVISION");
                Expected(".", """
                Missing separator period at the end of this PROCEDURE DIVISION header, every division header must end with a separator period
                """, -1, "METHOD-ID", "END");

                while (CurrentEquals("METHOD-ID") || CurrentEquals("IDENTIFICATION") && LookaheadEquals(3, "METHOD-ID"))
                {
                    IDENTIFICATION();

                    if (CurrentEquals("ENVIRONMENT")) ENVIRONMENT();

                    if (CurrentEquals("DATA")) DATA();

                    if (CurrentEquals("PROCEDURE")) PROCEDURE();

                    EndMarker();
                }
            }
        }

        // Method responsible for parsing the ENVIRONMENT DIVISION.
        // That includes the CONFIGURATION and the INPUT-OUTPUT sections.
        // It is also responsible for showing appropriate error messages when an error occurs in the ENVIRONMENT DIVISION.
        void ENVIRONMENT()
        {
            Expected("ENVIRONMENT");
            Expected("DIVISION");
            CurrentSection = CurrentScope.EnvironmentDivision;

            Expected(".", """
            Missing separator period at the end of this ENVIRONMENT DIVISION header, every division header must end with a separator period
            """, -1, "DATA", "PROCEDURE", "PROGRAM-ID", "FUNCTION-ID");

            if (CurrentEquals("CONFIGURATION"))
            {
                Expected("CONFIGURATION");
                Expected("SECTION");
                Expected(".", """
                Missing separator period at the end of this CONFIGURATION SECTION header, every section must end with a separator period
                """, -1, "REPOSITORY", "DATA", "PROCEDURE", "PROGRAM-ID", "FUNCTION-ID");

                if (CurrentEquals("REPOSITORY")) REPOSITORY();
            }
        }

        void REPOSITORY()
        {
            Expected("REPOSITORY");
            CurrentSection = CurrentScope.Repository;

            Expected(".", """
            Missing separator period at the end of this REPOSITORY paragraph header, every paragraph must end with a separator period
            """, -1, "CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "DATA", "PROCEDURE");

            while (CurrentEquals("CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY"))
            {
                if (CurrentEquals("CLASS"))
                {
                    Expected("CLASS");
                    Identifier();

                    if (CurrentEquals("AS"))
                    {
                        Expected("AS");
                        String();
                    }

                    if (CurrentEquals("EXPANDS"))
                    {
                        Expected("EXPANDS");
                        Identifier();
                        Expected("USING");
                        if (!CurrentEquals(TokenType.Identifier))
                        {
                            ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                            The USING clause must contain at least one class, object or interface name.
                            """);
                            ErrorHandler.Parser.PrettyError(FileName, Current());
                        }

                        if (!CurrentEquals(TokenType.Identifier) && !LookaheadEquals(1, TokenType.Identifier))
                        {
                            AnchorPoint("CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "DATA", "PROCEDURE");
                        }

                        Identifier();
                        while (CurrentEquals(TokenType.Identifier)) Identifier();
                    }
                }

                if (CurrentEquals("INTERFACE"))
                {
                    Expected("INTERFACE");
                    Identifier();

                    if (CurrentEquals("AS"))
                    {
                        Expected("AS");
                        String();
                    }

                    if (CurrentEquals("EXPANDS"))
                    {
                        Expected("EXPANDS");
                        Identifier();
                        Expected("USING");
                        if (!CurrentEquals(TokenType.Identifier))
                        {
                            ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                            The USING clause must contain at least one class, object or interface name.
                            """);
                            ErrorHandler.Parser.PrettyError(FileName, Current());
                        }

                        if (!CurrentEquals(TokenType.Identifier) && !LookaheadEquals(1, TokenType.Identifier))
                        {
                            AnchorPoint("CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "DATA", "PROCEDURE");
                        }

                        Identifier();
                        while (CurrentEquals(TokenType.Identifier)) Identifier();
                    }
                }

                if (CurrentEquals("FUNCTION"))
                {
                    Expected("FUNCTION");
                    if (CurrentEquals("ALL"))
                    {
                        Expected("ALL");
                        Expected("INTRINSIC");
                    }
                    else if (CurrentEquals(TokenType.IntrinsicFunction))
                    {
                        Continue();
                        while (CurrentEquals(TokenType.IntrinsicFunction) || CurrentEquals("RANDOM"))
                        {
                            Continue();
                        }

                        Expected("INTRINSIC");

                        if (!CurrentEquals("CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "."))
                        {
                            AnchorPoint("CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "DATA", "PROCEDURE");
                        }
                    }
                    else
                    {
                        Identifier();
                        if (CurrentEquals("AS"))
                        {
                            Expected("AS");
                            String();
                        }
                    }
                }

                if (CurrentEquals("PROGRAM"))
                {
                    Expected("PROGRAM");
                    Identifier();
                    if (CurrentEquals("AS"))
                    {
                        Expected("AS");
                        String();
                    }
                }

                if (CurrentEquals("PROPERTY"))
                {
                    Expected("PROPERTY");
                    Identifier();
                    if (CurrentEquals("AS"))
                    {
                        Expected("AS");
                        String();
                    }
                }
            }

            Expected(".", """
            Missing separator period at the end of this REPOSITORY paragraph body, the last definition in the REPOSITORY paragraph must end with a period
            """, -1, "CLASS", "INTERFACE", "FUNCTION", "PROGRAM", "PROPERTY", "DATA", "PROCEDURE");
        }


        // Method responsible for parsing the DATA DIVISION.
        // That includes the FILE, WORKING-STORAGE, LOCAL-STORAGE, LINKAGE, REPORT and SCREEN sections.
        // It is also responsible for showing appropriate error messages when an error occurs in the DATA DIVISION.
        void DATA()
        {
            Expected("DATA", "data division");
            Expected("DIVISION");
            CurrentSection = CurrentScope.DataDivision;

            Expected(".", """
            Missing separator period at the end of this DATA DIVISION header, every division header must end with a separator period
            """, -1, "WORKING-STORAGE", "LOCAL-STORAGE", "LINKAGE", "PROCEDURE");

            if (CurrentEquals("WORKING-STORAGE"))
                WorkingStorage();

            if (CurrentEquals("LOCAL-STORAGE"))
                LocalStorage();

            if (CurrentEquals("LINKAGE"))
                LinkageSection();

            if (!CurrentEquals("PROCEDURE"))
            {
                ErrorHandler.Parser.Report(FileName, Current(), ErrorType.Expected, "Data Division data items and sections");
                ErrorHandler.Parser.PrettyError(FileName, Current());
                Continue();
            }
        }


        // The following methods are responsible for parsing the DATA DIVISION sections
        // They are technically only responsible for parsing the section header, 
        // the Entries() method handles parsing the actual data items in their correct sections.
        void WorkingStorage()
        {
            Expected("WORKING-STORAGE");
            Expected("SECTION");
            CurrentSection = CurrentScope.WorkingStorage;

            Expected(".");
            while (Current().type == TokenType.Numeric)
                Entries();
        }

        void LocalStorage()
        {
            Expected("LOCAL-STORAGE");
            Expected("SECTION");
            CurrentSection = CurrentScope.LocalStorage;

            Expected(".");
            while (Current().type is TokenType.Numeric)
                Entries();
        }

        void LinkageSection()
        {
            Expected("LINKAGE");
            Expected("SECTION");
            CurrentSection = CurrentScope.LinkageSection;

            Expected(".");
            while (Current().type is TokenType.Numeric)
                Entries();
        }


        // The following methods are responsible for parsing the DATA DIVISION data items
        // The Entries() method is responsible for identifying which kind of data item to 
        // parse based on it's level number.

        // The RecordEntry(), BaseEntry(), and ConstantEntry() are then responsible for correctly
        // parsing each data item, or in the case of the RecordEntry() a group item or 01-level elementary item.
        void Entries()
        {
            if (CurrentEquals("77"))
                BaseEntry();

            if ((CurrentEquals("01") || CurrentEquals("1")) && !LookaheadEquals(2, "CONSTANT"))
                RecordEntry();

            if (LookaheadEquals(2, "CONSTANT"))
                ConstantEntry();
        }

        void RecordEntry()
        {
            BaseEntry();
            _ = int.TryParse(Current().value, out int outInt);
            while (outInt > 1 && outInt < 50)
            {
                BaseEntry();
                _ = int.TryParse(Current().value, out outInt);
            }

            LevelStack.Clear();
        }

        void BaseEntry()
        {
            string dataType;
            int LevelNumber = int.Parse(Current().value);
            CheckLevelNumber(LevelNumber);
            Number();

            Token DataItem = Current();
            string DataName = DataItem.value;
            Identifier();

            string DataItemHash = $"{SourceId.Peek()}#{DataName}";
            if (SymbolTable.SymbolExists(DataItemHash))
            {
                DataItemInfo originalItem = SymbolTable.GetDataItem(DataItemHash);

                ErrorHandler.Parser.Report(FileName, DataItem, ErrorType.General, $"""
                A data item with this name already exists in this source unit, data items must have a unique name.
                The original {originalItem.Identifier} data item can be found at line {originalItem.Line}. 
                """);
                ErrorHandler.Parser.PrettyError(FileName, DataItem);
            }
            else
            {
                SymbolTable.AddSymbol(DataItemHash, SymbolType.DataItem);
            }

            DataItemInfo dataItem = SymbolTable.GetDataItem(DataItemHash);

            dataItem.Identifier = DataName;
            dataItem.LevelNumber = LevelNumber;
            dataItem.Section = CurrentSection;

            if (!CurrentEquals(TokenContext.IsClause) && !CurrentEquals("."))
            {
                ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, $"""
                Expected data division clauses or a separator period after this data item's identifier.
                Token found ("{Current().value}") was not a data division clause reserved word.
                """);
                ErrorHandler.Parser.PrettyError(FileName, Current());
            }

            while (CurrentEquals(TokenContext.IsClause))
            {
                if (CurrentEquals("IS") && !LookaheadEquals(1, "EXTERNAL", "GLOBAL", "TYPEDEF"))
                {
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    Missing clause or possible clause mismatch, in this context the "IS" word must be followed by the EXTERNAL, GLOBAL or TYPEDEF clauses only (IS TYPEDEF), or must be in the middle of the PICTURE clause (PIC IS ...) 
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }

                if ((CurrentEquals("IS") && LookaheadEquals(1, "EXTERNAL")) || CurrentEquals("EXTERNAL"))
                {
                    Optional("IS");
                    Expected("EXTERNAL");
                    if (CurrentEquals("AS"))
                    {
                        Expected("AS");
                        dataItem.IsExternal = true;
                        dataItem.ExternalName = Current().value;

                        String("""
                        Missing externalized name, the "AS" word on the EXTERNAL clause must be followed by an alphanumeric or national literal
                        """, -1);
                    }

                    if (!CurrentEquals("AS"))
                    {
                        dataItem.IsExternal = true;
                        dataItem.ExternalName = Current().value;
                    }
                }

                if ((CurrentEquals("IS") && LookaheadEquals(1, "GLOBAL")) || CurrentEquals("GLOBAL"))
                {
                    Optional("IS");
                    Expected("GLOBAL");
                    dataItem.IsGlobal = true;
                }

                if ((CurrentEquals("IS") && LookaheadEquals(1, "TYPEDEF")) || CurrentEquals("TYPEDEF"))
                {
                    Optional("IS");
                    Expected("TYPEDEF");
                    dataItem.IsTypedef = true;

                    if (CurrentEquals("STRONG")) Expected("STRONG");
                }

                if (CurrentEquals("REDEFINES"))
                {
                    Expected("REDEFINES");
                    Identifier();
                    dataItem.IsRedefines = true;
                }

                if (CurrentEquals("ALIGNED")) Expected("ALIGNED");

                if (CurrentEquals("ANY") && LookaheadEquals(1, "LENGTH"))
                {
                    Expected("ANY");
                    Expected("LENGTH");
                    dataItem.IsAnyLength = true;
                }

                if (CurrentEquals("BASED")) Expected("BASED");

                if (CurrentEquals("BLANK"))
                {
                    Expected("BLANK");
                    Optional("WHEN");
                    Expected("ZERO");
                    dataItem.IsBlank = true;
                }

                if (CurrentEquals("CONSTANT") && LookaheadEquals(1, "RECORD"))
                {
                    Expected("CONSTANT");
                    Expected("RECORD");
                    dataItem.IsConstantRecord = true;
                }

                if (CurrentEquals("DYNAMIC"))
                {
                    Expected("DYNAMIC");
                    Optional("LENGTH");
                    dataItem.IsDynamicLength = true;

                    if (CurrentEquals(TokenType.Identifier)) Identifier();

                    if (CurrentEquals("LIMIT"))
                    {
                        Expected("LIMIT");
                        Optional("IS");
                        Number();
                    }
                }

                if (CurrentEquals("GROUP-USAGE"))
                {
                    Expected("GROUP-USAGE");
                    Optional("IS");
                    Choice("BIT", "NATIONAL");
                }

                if (CurrentEquals("JUSTIFIED", "JUST"))
                {
                    Choice("JUSTIFIED", "JUST");
                    Optional("RIGHT");
                }

                if (CurrentEquals("SYNCHRONIZED", "SYNC"))
                {
                    Choice("SYNCHRONIZED", "SYNC");
                    if (CurrentEquals("LEFT")) Expected("LEFT");

                    else if (CurrentEquals("RIGHT")) Expected("RIGHT");
                }

                if (CurrentEquals("PROPERTY"))
                {
                    Expected("PROPERTY");
                    dataItem.IsProperty = true;
                    if (CurrentEquals("WITH", "NO"))
                    {
                        Optional("WITH");
                        Expected("NO");
                        Choice("GET", "SET");
                    }

                    if (CurrentEquals("IS", "FINAL"))
                    {
                        Optional("IS");
                        Expected("FINAL");
                    }
                }

                if (CurrentEquals("SAME"))
                {
                    Expected("SAME");
                    Expected("AS");
                    Identifier();
                }

                if (CurrentEquals("TYPE"))
                {
                    Expected("TYPE");
                    Identifier();
                }

                if (CurrentEquals("PIC", "PICTURE"))
                {
                    Choice("PIC", "PICTURE");
                    Optional("IS");
                    dataType = Current().value switch
                    {
                        "S9" => "S9",
                        "9" => "9",
                        "X" => "X",
                        "A" => "A",
                        "N" => "N",
                        "1" => "1",
                        _ => "Error"
                    };

                    if (dataType == "Error")
                    {
                        ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                        Unrecognized type, PICTURE type must be S9, 9, X, A, N or 1. These are Signed Numeric, Unsigned Numeric, Alphanumeric, Alphabetic, National and Boolean respectively
                        """);
                        ErrorHandler.Parser.PrettyError(FileName, Current());
                    }

                    dataItem.Type = dataType;
                    dataItem.IsPicture = true;
                    Choice("S9", "9", "X", "A", "N", "1");

                    Expected("(");
                    string DataLength = Current().value;
                    Number();
                    Expected(")");
                    if (CurrentEquals("V9") && dataType != "S9" && dataType != "9")
                    {
                        ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, "V9 cannot be used with non-numeric types");
                        ErrorHandler.Parser.PrettyError(FileName, Current());
                    }

                    if (CurrentEquals("V9"))
                    {
                        Expected("V9");
                        Expected("(");
                        DataLength += $"V{Current().value}";
                        Number();
                        Expected(")");
                    }

                    dataItem.PictureLength = DataLength;
                }

                if (CurrentEquals("VALUE"))
                {
                    Expected("VALUE");

                    if (!CurrentEquals(TokenType.String, TokenType.Numeric))
                    {
                        ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                        The only tokens allowed after a VALUE clause are type literals, like an Alphanumeric literal ("Hello, World!") or a Numeric literal (123.456).
                        """);
                        ErrorHandler.Parser.PrettyError(FileName, Current());
                    }

                    if (CurrentEquals(TokenType.String))
                    {
                        dataItem.DefaultValue = Current().value;
                        String();
                    }

                    if (CurrentEquals(TokenType.Numeric))
                    {
                        dataItem.DefaultValue = Current().value;
                        Number();
                    }
                }

                if (CurrentEquals("USAGE"))
                {
                    UsageClause(dataItem);
                }

            }

            if (CurrentEquals(".") && LookaheadEquals(1, TokenType.Numeric))
            {
                if (LevelStack.Count == 0)
                {
                    dataItem.IsElementary = true;
                }
                else
                {
                    _ = int.TryParse(Lookahead(1).value, out int outInt);
                    var currentLevel = LevelStack.Peek();

                    if (currentLevel == 1 && outInt >= 2 && outInt <= 49 || outInt >= 2 && outInt <= 49 && outInt > currentLevel)
                    {
                        dataItem.IsGroup = true;
                    }
                    else
                    {
                        dataItem.IsElementary = true;
                    }
                }
            }

            CheckClauses(DataItemHash, DataItem);

            Expected(".", """
            Missing separator period at the end of this data item definition, each data item must end with a separator period
            """, -1, "PROCEDURE");
        }

        void ConstantEntry()
        {
            if (!CurrentEquals("01") && !CurrentEquals("1"))
            {
                ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                Invalid level number for this data item, CONSTANT data items must have a level number of 1 or 01
                """);
                ErrorHandler.Parser.PrettyError(FileName, Current());
            }

            var LevelNumber = int.Parse(Current().value);
            Number();

            var DataName = Current().value;
            Identifier();

            var DataItemHash = $"{SourceId.Peek()}#{DataName}";
            if (SymbolTable.SymbolExists(DataItemHash))
            {
                var originalItem = SymbolTable.GetDataItem(DataItemHash);

                ErrorHandler.Parser.Report(FileName, Lookahead(-1), ErrorType.General, $"""
                A data item with this name already exists in this program, data items in a program must have a unique name.
                The original {originalItem.Identifier} data item can be found on line {originalItem.Line}. 
                """);
                ErrorHandler.Parser.PrettyError(FileName, Lookahead(-1));
            }
            else
            {
                SymbolTable.AddSymbol(DataItemHash, SymbolType.DataItem);
            }

            DataItemInfo dataItem = SymbolTable.GetDataItem(DataItemHash);

            dataItem.Identifier = DataName;
            dataItem.LevelNumber = LevelNumber;
            dataItem.Section = CurrentSection;
            dataItem.IsConstant = true;

            Expected("CONSTANT");
            if (CurrentEquals("IS") || CurrentEquals("GLOBAL"))
            {
                Optional("IS");
                Expected("GLOBAL");
                 dataItem.IsGlobal = true;
            }

            if (CurrentEquals("FROM"))
            {
                Expected("FROM");
                Identifier();
            }
            else
            {
                Optional("AS");
                switch (Current().type)
                {
                    case TokenType.String:
                        String();
                        break;

                    case TokenType.Numeric:
                        Number();
                        break;

                    case TokenType.FigurativeLiteral:
                        FigurativeLiteral();
                        break;
                }

                if (CurrentEquals("LENGTH"))
                {
                    Expected("LENGTH");
                    Optional("OF");
                    Identifier();
                }

                if (CurrentEquals("BYTE-LENGTH"))
                {
                    Expected("BYTE-LENGTH");
                    Optional("OF");
                    Identifier();
                }

            }

            Expected(".");
        }

        void CheckLevelNumber(int level)
        {
            if (level is 77) return;

            if (level is 1)
            {
                LevelStack.Push(level);
                return;
            }

            var currentLevel = LevelStack.Peek();

            if (level == currentLevel) return;

            if (level > currentLevel && level <= 49)
            {
                LevelStack.Push(level);
                return;
            }

            if (level < currentLevel)
            {
                var current = LevelStack.Pop();
                var lowerLevel = LevelStack.Peek();
                if (level == lowerLevel) return;

                if (level != lowerLevel)
                {
                    LevelStack.Push(current);
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                    All data items that are immediate members of a group item must have equal level numbers, and it should be greater than the level number used for that group item. 
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current());
                }
            }
        }

        void CheckClauses(string dataItemHash, Token itemToken)
        {
            DataItemInfo dataItem = SymbolTable.GetDataItem(dataItemHash);

            bool usageCannotHavePicture = dataItem.UsageType switch
            {
                UsageType.BinaryChar => true,
                UsageType.BinaryDouble => true,
                UsageType.BinaryLong => true,
                UsageType.BinaryShort => true,
                UsageType.FloatShort => true,
                UsageType.FloatLong => true,
                UsageType.FloatExtended => true,
                UsageType.Index => true,
                UsageType.MessageTag => true,
                UsageType.ObjectReference => true,
                UsageType.DataPointer => true,
                UsageType.FunctionPointer => true,
                UsageType.ProgramPointer => true,
                _ => false
            };

            if (usageCannotHavePicture && dataItem.IsPicture)
            {
                ErrorHandler.Parser.Report(FileName, itemToken, ErrorType.General, $"""
                Data items defined with USAGE {dataItem.UsageType} cannot contain a PICTURE clause
                """);
                ErrorHandler.Parser.PrettyError(FileName, itemToken);
            }

            if (!usageCannotHavePicture && dataItem.IsElementary && !dataItem.IsPicture && !dataItem.IsValue)
            {
                ErrorHandler.Parser.Report(FileName, itemToken, ErrorType.General, """
                Elementary data items must contain a PICTURE clause. Except when an alphanumeric, boolean, or national literal is defined in the VALUE clause 
                """);
                ErrorHandler.Parser.PrettyError(FileName, itemToken);
            }

            if (dataItem.IsGroup && dataItem.IsPicture)
            {
                ErrorHandler.Parser.Report(FileName, itemToken, ErrorType.General, """
                Group items must not contain a PICTURE clause. The PICTURE clause can only be specified on elementary data items
                """);
                ErrorHandler.Parser.PrettyError(FileName, itemToken);
            }

            if (dataItem.IsRenames && dataItem.IsPicture)
            {
                ErrorHandler.Parser.Report(FileName, itemToken, ErrorType.General, """
                Data items with a RENAMES clause must not contain a PICTURE clause
                """);
                ErrorHandler.Parser.PrettyError(FileName, itemToken);
            }

            bool usageCannotHaveValue = dataItem.UsageType switch
            {
                UsageType.Index => true,
                UsageType.MessageTag => true,
                UsageType.ObjectReference => true,
                UsageType.DataPointer => true,
                UsageType.FunctionPointer => true,
                UsageType.ProgramPointer => true,
                _ => false
            };

            if (usageCannotHaveValue && dataItem.IsValue)
            {
                ErrorHandler.Parser.Report(FileName, itemToken, ErrorType.General, $"""
                Data items defined with USAGE {dataItem.UsageType} cannot contain a VALUE clause
                """);
                ErrorHandler.Parser.PrettyError(FileName, itemToken);
            }

        }

        void UsageClause(DataItemInfo dataitem)
        {
            Expected("USAGE");
            Optional("IS");
            switch (Current().value)
            {
                
                case "BINARY":
                    Expected("BINARY");
                    dataitem.UsageType = UsageType.Binary;
                    break;

                case "BINARY-CHAR":
                case "BINARY-SHORT":
                case "BINARY-LONG":
                case "BINARY-DOUBLE":
                    Expected(Current().value);
                    if (CurrentEquals("SIGNED"))
                    {
                        Expected("SIGNED");
                    }
                    else if (CurrentEquals("UNSIGNED"))
                    {
                        Expected("UNSIGNED");
                    }
                    break;

                case "BIT":
                    Expected("BIT");
                    dataitem.UsageType = UsageType.Bit;
                    break;

                case "COMP":
                case "COMPUTATIONAL":
                    Expected(Current().value);
                    dataitem.UsageType = UsageType.Computational;
                    break;

                case "DISPLAY":
                    Expected("DISPLAY");
                    dataitem.UsageType = UsageType.Display;
                    break;

                case "FLOAT-BINARY-32":
                    Expected("FLOAT-BINARY-32");
                    Choice("HIGH-ORDER-LEFT", "HIGH-ORDER-RIGHT");
                    break;

                case "FLOAT-BINARY-64":
                    Expected("FLOAT-BINARY-64");
                    Choice("HIGH-ORDER-LEFT", "HIGH-ORDER-RIGHT");
                    break;

                case "FLOAT-BINARY-128":
                    Expected("FLOAT-BINARY-128");
                    Choice("HIGH-ORDER-LEFT", "HIGH-ORDER-RIGHT");
                    break;

                case "FLOAT-DECIMAL-16":
                    Expected("FLOAT-DECIMAL-16");
                    EncodingEndianness();
                    break;

                case "FLOAT-DECIMAL-32":
                    Expected("FLOAT-DECIMAL-32");
                    EncodingEndianness();
                    break;

                case "FLOAT-EXTENDED":
                    Expected("FLOAT-EXTENDED");
                    break;

                case "FLOAT-LONG":
                    Expected("FLOAT-LONG");
                    break;

                case "FLOAT-SHORT":
                    Expected("FLOAT-SHORT");
                    break;

                case "INDEX":
                    Expected("INDEX");
                    dataitem.UsageType = UsageType.Index;
                    break;

                case "MESSAGE-TAG":
                    Expected("MESSAGE-TAG");
                    dataitem.UsageType = UsageType.MessageTag;
                    break;

                case "NATIONAL":
                    Expected("NATIONAL");
                    break;

                case "OBJECT":
                    Expected("OBJECT");
                    Expected("REFERENCE");
                    var isFactory = false;
                    var isStronglyTyped = false;
                    // Need implement identifier resolution first
                    // To parse the rest of this using clause
                    dataitem.UsageType = UsageType.ObjectReference;
                    if (CurrentEquals("Factory"))
                    {
                        Expected("FACTORY");
                        Optional("OF");
                        isFactory = true;
                    }

                    if (CurrentEquals("ACTIVE-CLASS"))
                    {
                        Expected("ACTIVE-CLASS");
                        break;
                    }

                    Continue();

                    if (CurrentEquals("ONLY"))
                    {
                        Expected("ONLY");
                        isStronglyTyped = true;
                    }

                    break;

                case "PACKED-DECIMAL":
                    Expected("PACKED-DECIMAL");
                    if (CurrentEquals("WITH", "NO"))
                    {
                        Optional("WITH");
                        Expected("NO");
                        Expected("SIGN");
                    }
                    break;

                case "POINTER":
                    Expected("POINTER");
                    if (CurrentEquals("TO") || CurrentEquals(TokenType.Identifier))
                    {
                        Optional("TO");
                        dataitem.UsageType = UsageType.DataPointer;
                        dataitem.UsageContext = Current().value;
                        Identifier();
                    }
                    else
                    {
                        dataitem.UsageType = UsageType.DataPointer;
                    }
                    break;

                case "FUNCTION-POINTER":
                    Expected("FUNCTION-POINTER");
                    Optional("TO");
                    dataitem.UsageType = UsageType.FunctionPointer;
                    dataitem.UsageContext = Current().value;
                    Identifier();
                    break;

                case "PROGRAM-POINTER":
                    Expected("PROGRAM-POINTER");
                    if (CurrentEquals("TO") || CurrentEquals(TokenType.Identifier))
                    {
                        Optional("TO");
                        dataitem.UsageType = UsageType.ProgramPointer;
                        dataitem.UsageContext = Current().value;
                        Identifier();
                    }
                    else
                    {
                        dataitem.UsageType = UsageType.ProgramPointer;
                    }
                    break;

                default:
                    ErrorHandler.Parser.Report(FileName, Current(), ErrorType.Recovery, """
                    Unrecognized USAGE clause. This could be due to an unsupported third-party extension. 
                    """);
                    ErrorHandler.Parser.PrettyError(FileName, Current(), ConsoleColor.Blue);

                    AnchorPoint(TokenContext.IsClause);
                    break;
            }
        }

        // Method responsible for parsing the PROCEDURE DIVISION.
        // That includes the user-defined paragraphs, sections and declaratives
        // or when parsing OOP COBOL code, it's responsible for parsing COBOL methods, objects and factories. 
        // It is also responsible for showing appropriate error messages when an error occurs in the PROCEDURE DIVISION.
        void PROCEDURE()
        {
            Expected("PROCEDURE");
            Expected("DIVISION");
            CurrentSection = CurrentScope.ProcedureDivision;
            var currentSource = SourceType.Peek();

            if (CurrentEquals("USING"))
            {
                Expected("USING");

                while (CurrentEquals("BY", "REFERENCE", "VALUE"))
                {
                    if (CurrentEquals("BY") && !LookaheadEquals(1, "VALUE", "REFERENCE"))
                    {
                        ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                        The USING BY clause in the procedure division header must be followed by "VALUE" or "REFERENCE"
                        """);
                        ErrorHandler.Parser.PrettyError(FileName, Current());

                        CombinedAnchorPoint(TokenContext.IsStatement, "RETURNING", ".");
                    }

                    if (CurrentEquals("REFERENCE") || CurrentEquals("BY") && LookaheadEquals(1, "REFERENCE"))
                    {
                        var optional = false;
                        Optional("BY");
                        Expected("REFERENCE");

                        if (CurrentEquals("OPTIONAL"))
                        {
                            optional = true;
                            Expected("OPTIONAL");
                        }

                        if (!CurrentEquals(TokenType.Identifier))
                        {
                            ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                            The USING BY REFERENCE clause must contain at least one data item name.
                            """);
                            ErrorHandler.Parser.PrettyError(FileName, Current());
                        }
                        
                        SourceUnitSignature signature;

                        if (SourceType.Peek() is SourceUnit.Method or SourceUnit.MethodPrototype or SourceUnit.MethodGetter or SourceUnit.MethodSetter)
                        {
                            var currentId = SourceId.Pop();
                            signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}->{currentId}");
                            SourceId.Push(currentId);
                        }
                        else
                        {
                            signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}");
                        }

                        signature.Parameters.Add(Current().value);
                        signature.IsOptional.Add(optional);
                        signature.IsByRef.Add(true);

                        Identifier();
                        while (CurrentEquals(TokenType.Identifier) || CurrentEquals("OPTIONAL"))
                        {
                            optional = false;
                            if (CurrentEquals("OPTIONAL"))
                            {
                                optional = true;
                                Expected("OPTIONAL");
                            }
                            signature.Parameters.Add(Current().value);
                            signature.IsOptional.Add(optional);
                            signature.IsByRef.Add(true);
                            Identifier();
                        }
                    }

                    if (CurrentEquals("VALUE") || CurrentEquals("BY") && LookaheadEquals(1, "VALUE"))
                    {
                        Optional("BY");
                        Expected("VALUE");
                        if (!CurrentEquals(TokenType.Identifier))
                        {
                            ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                            The USING BY VALUE clause must contain at least one data item name.
                            """);
                            ErrorHandler.Parser.PrettyError(FileName, Current());
                        }
                        
                        SourceUnitSignature signature;

                        if (SourceType.Peek() is SourceUnit.Method or SourceUnit.MethodPrototype or SourceUnit.MethodGetter or SourceUnit.MethodSetter)
                        {
                            var currentId = SourceId.Pop();
                            signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}->{currentId}");
                            SourceId.Push(currentId);
                        }
                        else
                        {
                            signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}");
                        }

                        signature.Parameters.Add(Current().value);
                        signature.IsOptional.Add(false);
                        signature.IsByRef.Add(false);
                        Identifier();
                        while (CurrentEquals(TokenType.Identifier))
                        {
                            signature.Parameters.Add(Current().value);
                            signature.IsOptional.Add(false);
                            signature.IsByRef.Add(false);
                            Identifier();
                        }
                    }
                }
            }

            if (SourceType.Peek() is SourceUnit.Function or SourceUnit.FunctionPrototype)
            {
                Expected("RETURNING");
                ReturningDataName();
            }
            else if (CurrentEquals("RETURNING"))
            {
                Expected("RETURNING");
                ReturningDataName();
            }

            Expected(".", """
            Missing separator period at the end of this PROCEDURE DIVISION header, every division header must end with a separator period
            """, -1, TokenContext.IsStatement);

            bool isProcedureDeclarative = CurrentEquals("DECLARATIVES")
                || CurrentEquals(TokenType.Identifier) && LookaheadEquals(1, "SECTION");

            bool canContainStatements = currentSource switch
            {
                SourceUnit.FunctionPrototype => false,
                SourceUnit.ProgramPrototype => false,
                SourceUnit.MethodPrototype => false,
                _ => true
            };

            if (canContainStatements && !isProcedureDeclarative) ParseStatements();

            if (canContainStatements && isProcedureDeclarative) DeclarativeProcedure();

            if (!canContainStatements && (CurrentEquals(TokenContext.IsStatement) || CurrentEquals(TokenType.Identifier)))
            {
                ErrorHandler.Parser.Report(FileName, Current(), ErrorType.General, """
                The procedure division of function, program and method prototypes must not contain any statements, sections or paragraphs
                """);
                ErrorHandler.Parser.PrettyError(FileName, Current());

                AnchorPoint("END");
            }

        }

        // This method is part of the PROCEDURE DIVISION parsing. It's used to parse the "RETURNING" data item specified in
        // the PROCEDURE DIVISION header. It's separate from the previous method because its code is needed more than once.
        // COBOL user-defined functions should always return a data item.
        void ReturningDataName()
        {
            if (!CurrentEquals(TokenType.Identifier))
            {
                ErrorHandler.Parser.Report(FileName, Lookahead(-1), ErrorType.General, """
                Missing returning data item after this RETURNING definition.
                """);
                ErrorHandler.Parser.PrettyError(FileName, Lookahead(-1));
                return;
            }

            SourceUnitSignature signature;

            if (SourceType.Peek() is SourceUnit.Method or SourceUnit.MethodPrototype or SourceUnit.MethodGetter or SourceUnit.MethodSetter)
            {
                var currentId = SourceId.Pop();
                signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}->{currentId}");
                SourceId.Push(currentId);
            }
            else
            {
                signature = SymbolTable.GetSourceUnit($"{SourceId.Peek()}");
            }

            signature.Returning = Current().value;
            Identifier();
        }

        void EndMarker()
        {
            SourceUnit currentSource = SourceType.Peek();

            if (currentSource != SourceUnit.Program && !CurrentEquals("END") || currentSource == SourceUnit.Program && (!CurrentEquals(TokenType.EOF) && !CurrentEquals("END")))
            {
                string errorMessageChoice = currentSource switch
                {
                    SourceUnit.Program or SourceUnit.ProgramPrototype => """
                    Missing END PROGRAM marker. If another source unit is present after the end of a program or program prototype, the program must contain an END marker.
                    """,

                    SourceUnit.Function or SourceUnit.FunctionPrototype => """
                    Missing END FUNCTION marker. User-defined functions and function prototypes must always end with an END FUNCTION marker.
                    """,

                    SourceUnit.Method or SourceUnit.MethodPrototype or SourceUnit.MethodGetter or SourceUnit.MethodSetter => """
                    Missing END METHOD marker. Method definitions and property getter/setter must always end with an END METHOD marker.
                    """,

                    SourceUnit.Class => """
                    Missing END CLASS marker. Class definitions must always end with an END CLASS marker.
                    """,

                    SourceUnit.Interface => """
                    Missing END INTERFACE marker. Interface definitions must always end with an END INTERFACE marker.
                    """,

                    SourceUnit.Factory or SourceUnit.Object => """
                    Missing END FACTORY and END OBJECT marker. Factory and object definitions must always end with an END FACTORY and END OBJECT marker.
                    """,

                    _ => throw new UnreachableException()
                };

                ErrorHandler.Parser.Report(FileName, Lookahead(-1), ErrorType.General, errorMessageChoice);
                ErrorHandler.Parser.PrettyError(FileName, Lookahead(-1));
                return;
            }

            if (currentSource == SourceUnit.Program && CurrentEquals("EOF"))
            {
                SourceType.Pop();
                return;
            }

            switch (currentSource)
            {
                case SourceUnit.Program:
                case SourceUnit.ProgramPrototype:
                    SourceType.Pop();

                    Expected("END");
                    Expected("PROGRAM");
                    Identifier(SourceId.Pop());
                    Expected(".", """
                    Missing separator period at the end of this END PROGRAM definition
                    """, -1, "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

                case SourceUnit.Function:
                case SourceUnit.FunctionPrototype:
                    SourceType.Pop();

                    Expected("END");
                    Expected("FUNCTION");
                    Identifier(SourceId.Pop());
                    Expected(".", """
                    Missing separator period at the end of this END FUNCTION definition
                    """, -1, "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

                case SourceUnit.Method:
                case SourceUnit.MethodPrototype:
                case SourceUnit.MethodGetter:
                case SourceUnit.MethodSetter:
                    SourceType.Pop();

                    Expected("END");
                    Expected("METHOD");
                    if (currentSource is SourceUnit.Method or SourceUnit.MethodPrototype)
                        Identifier(SourceId.Pop());

                    if (currentSource is SourceUnit.MethodGetter or SourceUnit.MethodSetter)
                        SourceId.Pop();

                    Expected(".", """
                    Missing separator period at the end of this END METHOD definition
                    """, -1, "IDENTIFICATION", "METHOD-ID", "OBJECT", "FACTORY");
                    break;

                case SourceUnit.Class:
                    SourceType.Pop();

                    Expected("END");
                    Expected("CLASS");
                    Identifier(SourceId.Pop());
                    Expected(".", """
                    Missing separator period at the end of this END CLASS definition
                    """, -1, "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

                case SourceUnit.Interface:
                    SourceType.Pop();

                    Expected("END");
                    Expected("INTERFACE");
                    Identifier(SourceId.Pop());
                    Expected(".", """
                    Missing separator period at the end of this END INTERFACE definition
                    """, -1, "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

                case SourceUnit.Factory:
                    SourceType.Pop();

                    Expected("END");
                    Expected("FACTORY");
                    Expected(".", """
                    Missing separator period at the end of this END FACTORY definition
                    """, -1, "OBJECT", "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

                case SourceUnit.Object:
                    SourceType.Pop();

                    Expected("END");
                    Expected("OBJECT");
                    Expected(".", """
                    Missing separator period at the end of this END FACTORY definition
                    """, -1, "END", "IDENTIFICATION", "PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID");
                    break;

            }
        }
    }
}
