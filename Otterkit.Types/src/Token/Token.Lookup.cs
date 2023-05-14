﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Otterkit.Types;

public sealed partial record Token
{
    private static bool IsReservedWord(string value)
    {
        ref var reserved = ref CollectionsMarshal.GetValueRefOrNullRef(ReservedLookup, value);

        return !Unsafe.IsNullRef(ref reserved);
    }

    private static bool IsStandardClause(string value)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrNullRef(ContextLookup, value);

        return !Unsafe.IsNullRef(ref context) && context is 1;
    }

    private static bool IsStandardStatement(string value)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrNullRef(ContextLookup, value);

        return !Unsafe.IsNullRef(ref context) && context is 2;
    }

    private static bool IsOtterkitDevice(string value)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrNullRef(ContextLookup, value);

        return !Unsafe.IsNullRef(ref context) && context is 3;
    }

    private static bool IsFigurativeLiteral(string value)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrNullRef(ContextLookup, value);

        return !Unsafe.IsNullRef(ref context) && context is 4;
    }

    private static bool IsStandardSymbol(string value)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrNullRef(ContextLookup, value);

        return !Unsafe.IsNullRef(ref context) && context is 5;
    }

    private static bool IsStandardIntrinsic(string value)
    {
        ref var intrinsics = ref CollectionsMarshal.GetValueRefOrNullRef(IntrinsicLookup, value);

        return !Unsafe.IsNullRef(ref intrinsics);
    }

    private static readonly Dictionary<string, int> ReservedLookup = new(400, StringComparer.OrdinalIgnoreCase)
    {
        {"ACCEPT",                          0},
        {"ACCESS",                          0},
        {"ACTIVE-CLASS",                    0},
        {"ADD",                             0},
        {"ADDRESS",                         0},
        {"ADVANCING",                       0},
        {"AFTER",                           0},
        {"ALIGNED",                         0},
        {"ALLOCATE",                        0},
        {"ALPHABET",                        0},
        {"ALPHABETIC",                      0},
        {"ALPHABETIC-LOWER",                0},
        {"ALPHABETIC-UPPER",                0},
        {"ALPHANUMERIC",                    0},
        {"ALPHANUMERIC-EDITED",             0},
        {"ALSO",                            0},
        {"ALTERNATE",                       0},
        {"AND",                             0},
        {"ANY",                             0},
        {"ANYCASE",                         0},
        {"ARE",                             0},
        {"AREA",                            0},
        {"AREAS",                           0},
        {"AS",                              0},
        {"ASCENDING",                       0},
        {"ASSIGN",                          0},
        {"AT",                              0},
        {"B-AND",                           0},
        {"B-NOT",                           0},
        {"B-OR",                            0},
        {"B-SHIFT",                         0},
        {"B-SHIFT-LC",                      0},
        {"B-SHIFT-RC",                      0},
        {"BY",                              0},
        {"B-XOR",                           0},
        {"BASED",                           0},
        {"BEFORE",                          0},
        {"BINARY",                          0},
        {"BINARY-CHAR",                     0},
        {"BINARY-DOUBLE",                   0},
        {"BINARY-LONG",                     0},
        {"BINARY-SHORT",                    0},
        {"BIT",                             0},
        {"BLANK",                           0},
        {"BLOCK",                           0},
        {"BOOLEAN",                         0},
        {"BOTTOM",                          0},
        {"CALL",                            0},
        {"CANCEL",                          0},
        {"CF",                              0},
        {"CH",                              0},
        {"CHARACTER",                       0},
        {"CHARACTERS",                      0},
        {"CLASS",                           0},
        {"CLASS-ID",                        0},
        {"CLOSE",                           0},
        {"CODE",                            0},
        {"CODE-SET",                        0},
        {"COL",                             0},
        {"COLLATING",                       0},
        {"COLS",                            0},
        {"COLUMN",                          0},
        {"COLUMNS",                         0},
        {"COMMA",                           0},
        {"COMMIT",                          0},
        {"COMMON",                          0},
        {"COMP",                            0},
        {"COMPUTATIONAL",                   0},
        {"COMPUTE",                         0},
        {"CONFIGURATION",                   0},
        {"CONSTANT",                        0},
        {"CONTAINS",                        0},
        {"CONTENT",                         0},
        {"CONTINUE",                        0},
        {"CONTROL",                         0},
        {"CONTROLS",                        0},
        {"CONVERTING",                      0},
        {"COPY",                            0},
        {"CORR",                            0},
        {"CORRESPONDING",                   0},
        {"COUNT",                           0},
        {"CRT",                             0},
        {"CURRENCY",                        0},
        {"CURSOR",                          0},
        {"DATA",                            0},
        {"DATA-POINTER",                    0},
        {"DATE",                            0},
        {"DAY",                             0},
        {"DAY-OF-WEEK",                     0},
        {"DE",                              0},
        {"DECIMAL-POINT",                   0},
        {"DECLARATIVES",                    0},
        {"DEFAULT",                         0},
        {"DELETE",                          0},
        {"DELIMITED",                       0},
        {"DELIMITER",                       0},
        {"DEPENDING",                       0},
        {"DESCENDING",                      0},
        {"DESTINATION",                     0},
        {"DETAIL",                          0},
        {"DISPLAY",                         0},
        {"DIVIDE",                          0},
        {"DIVISION",                        0},
        {"DOWN",                            0},
        {"DUPLICATES",                      0},
        {"DYNAMIC",                         0},
        {"EC",                              0},
        {"EDITING",                         0},
        {"ELSE",                            0},
        {"EMI",                             0},
        {"END",                             0},
        {"END-ACCEPT",                      0},
        {"END-ADD",                         0},
        {"END-CALL",                        0},
        {"END-COMPUTE",                     0},
        {"END-DELETE",                      0},
        {"END-DISPLAY",                     0},
        {"END-DIVIDE",                      0},
        {"END-EVALUATE",                    0},
        {"END-IF",                          0},
        {"END-MULTIPLY",                    0},
        {"END-OF-PAGE",                     0},
        {"END-PERFORM",                     0},
        {"END-RECEIVE",                     0},
        {"END-READ",                        0},
        {"END-RETURN",                      0},
        {"END-REWRITE",                     0},
        {"END-SEARCH",                      0},
        {"END-SEND",                        0},
        {"END-START",                       0},
        {"END-STRING",                      0},
        {"END-SUBTRACT",                    0},
        {"END-UNSTRING",                    0},
        {"END-WRITE",                       0},
        {"ENVIRONMENT",                     0},
        {"EOL",                             0},
        {"EOP",                             0},
        {"EQUAL",                           0},
        {"ERROR",                           0},
        {"EVALUATE",                        0},
        {"EXCEPTION",                       0},
        {"EXCEPTION-OBJECT",                0},
        {"EXCLUSIVE-OR",                    0},
        {"EXIT",                            0},
        {"EXTEND",                          0},
        {"EXTERNAL",                        0},
        {"FACTORY",                         0},
        {"FARTHEST-FROM-ZERO",              0},
        {"FALSE",                           0},
        {"FD",                              0},
        {"FILE",                            0},
        {"FILE-CONTROL",                    0},
        {"FILLER",                          0},
        {"FINAL",                           0},
        {"FINALLY",                         0},
        {"FIRST",                           0},
        {"FLOAT-BINARY-32",                 0},
        {"FLOAT-BINARY-64",                 0},
        {"FLOAT-BINARY-128",                0},
        {"FLOAT-DECIMAL-16",                0},
        {"FLOAT-DECIMAL-34",                0},
        {"FLOAT-EXTENDED",                  0},
        {"FLOAT-INFINITY",                  0},
        {"FLOAT-LONG",                      0},
        {"FLOAT-NOT-A-NUMBER",              0},
        {"FLOAT-NOT-A-NUMBER-QUIET",        0},
        {"FLOAT-NOT-A-NUMBER-SIGNALING",    0},
        {"FOOTING",                         0},
        {"FOR",                             0},
        {"FORMAT",                          0},
        {"FREE",                            0},
        {"FROM",                            0},
        {"FUNCTION",                        0},
        {"FUNCTION-ID",                     0},
        {"FUNCTION-POINTER",                0},
        {"GENERATE",                        0},
        {"GET",                             0},
        {"GIVING",                          0},
        {"GLOBAL",                          0},
        {"GO",                              0},
        {"GOBACK",                          0},
        {"GREATER",                         0},
        {"GROUP",                           0},
        {"GROUP-USAGE",                     0},
        {"HEADING",                         0},
        {"I-O",                             0},
        {"I-O-CONTROL",                     0},
        {"IDENTIFICATION",                  0},
        {"IF",                              0},
        {"IN",                              0},
        {"IN-ARITHMETIC-RANGE",             0},
        {"INDEX",                           0},
        {"INDEXED",                         0},
        {"INDICATE",                        0},
        {"INHERITS",                        0},
        {"INITIAL",                         0},
        {"INITIALIZE",                      0},
        {"INITIALIZED",                     0},
        {"INITIATE",                        0},
        {"INPUT",                           0},
        {"INPUT-OUTPUT",                    0},
        {"INSPECT",                         0},
        {"INTERFACE",                       0},
        {"INTERFACE-ID",                    0},
        {"INTO",                            0},
        {"INVALID",                         0},
        {"INVOKE",                          0},
        {"IS",                              0},
        {"JUST",                            0},
        {"JUSTIFIED",                       0},
        {"KEY",                             0},
        {"LAST",                            0},
        {"LEADING",                         0},
        {"LEFT",                            0},
        {"LENGTH",                          0},
        {"LESS",                            0},
        {"LIMIT",                           0},
        {"LIMITS",                          0},
        {"LINAGE",                          0},
        {"LINAGE-COUNTER",                  0},
        {"LINE",                            0},
        {"LINE-COUNTER",                    0},
        {"LINES",                           0},
        {"LINKAGE",                         0},
        {"LOCAL-STORAGE",                   0},
        {"LOCALE",                          0},
        {"LOCATION",                        0},
        {"LOCK",                            0},
        {"MERGE",                           0},
        {"MESSAGE-TAG",                     0},
        {"METHOD",                          0},
        {"METHOD-ID",                       0},
        {"MINUS",                           0},
        {"MODE",                            0},
        {"MOVE",                            0},
        {"MULTIPLY",                        0},
        {"NATIONAL",                        0},
        {"NATIONAL-EDITED",                 0},
        {"NATIVE",                          0},
        {"NEAREST-TO-ZERO",                 0},
        {"NESTED",                          0},
        {"NEXT",                            0},
        {"NO",                              0},
        {"NOT",                             0},
        {"NULL",                            0},
        {"NUMBER",                          0},
        {"NUMERIC",                         0},
        {"NUMERIC-EDITED",                  0},
        {"OBJECT",                          0},
        {"OBJECT-COMPUTER",                 0},
        {"OBJECT-REFERENCE",                0},
        {"OCCURS",                          0},
        {"OF",                              0},
        {"OFF",                             0},
        {"OMITTED",                         0},
        {"ON",                              0},
        {"OPEN",                            0},
        {"OPTIONAL",                        0},
        {"OPTIONS",                         0},
        {"OR",                              0},
        {"ORDER",                           0},
        {"ORGANIZATION",                    0},
        {"OTHER",                           0},
        {"OUTPUT",                          0},
        {"OVERFLOW",                        0},
        {"OVERRIDE",                        0},
        {"PACKED-DECIMAL",                  0},
        {"PAGE",                            0},
        {"PAGE-COUNTER",                    0},
        {"PERFORM",                         0},
        {"PF",                              0},
        {"PH",                              0},
        {"PIC",                             0},
        {"PICTURE",                         0},
        {"PLUS",                            0},
        {"POINTER",                         0},
        {"POSITIVE",                        0},
        {"PRESENT",                         0},
        {"PRINTING",                        0},
        {"PROCEDURE",                       0},
        {"PROGRAM",                         0},
        {"PROGRAM-ID",                      0},
        {"PROGRAM-POINTER",                 0},
        {"PROPERTY",                        0},
        {"PROTOTYPE",                       0},
        {"RAISE",                           0},
        {"RAISING",                         0},
        {"RANDOM",                          0},
        {"RD",                              0},
        {"READ",                            0},
        {"RECEIVE",                         0},
        {"RECORD",                          0},
        {"RECORDS",                         0},
        {"REDEFINES",                       0},
        {"REEL",                            0},
        {"REF",                             0},
        {"REFERENCE",                       0},
        {"RELATIVE",                        0},
        {"RELEASE",                         0},
        {"REMAINDER",                       0},
        {"REMOVAL",                         0},
        {"RENAMES",                         0},
        {"REPLACE",                         0},
        {"REPLACING",                       0},
        {"REPORT",                          0},
        {"REPORTING",                       0},
        {"REPORTS",                         0},
        {"REPOSITORY",                      0},
        {"RESERVE",                         0},
        {"RESET",                           0},
        {"RESUME",                          0},
        {"RETRY",                           0},
        {"RETURN",                          0},
        {"RETURNING",                       0},
        {"REWIND",                          0},
        {"REWRITE",                         0},
        {"RF",                              0},
        {"RH",                              0},
        {"RIGHT",                           0},
        {"ROLLBACK",                        0},
        {"ROUNDED",                         0},
        {"RUN",                             0},
        {"SAME",                            0},
        {"SCREEN",                          0},
        {"SD",                              0},
        {"SEARCH",                          0},
        {"SECONDS",                         0},
        {"SECTION",                         0},
        {"SELECT",                          0},
        {"SEND",                            0},
        {"SELF",                            0},
        {"SENTENCE",                        0},
        {"SEPARATE",                        0},
        {"SEQUENCE",                        0},
        {"SEQUENTIAL",                      0},
        {"SET",                             0},
        {"SHARING",                         0},
        {"SIGN",                            0},
        {"SIZE",                            0},
        {"SORT",                            0},
        {"SORT-MERGE",                      0},
        {"SOURCE",                          0},
        {"SOURCE-COMPUTER",                 0},
        {"SOURCES",                         0},
        {"SPECIAL-NAMES",                   0},
        {"STANDARD",                        0},
        {"STANDARD-1",                      0},
        {"STANDARD-2",                      0},
        {"START",                           0},
        {"STATUS",                          0},
        {"STOP",                            0},
        {"STRING",                          0},
        {"SUBTRACT",                        0},
        {"SUM",                             0},
        {"SUPER",                           0},
        {"SUPPRESS",                        0},
        {"SYMBOLIC",                        0},
        {"SYNC",                            0},
        {"SYNCHRONIZED",                    0},
        {"SYSTEM-DEFAULT",                  0},
        {"TABLE",                           0},
        {"TALLYING",                        0},
        {"TERMINATE",                       0},
        {"TEST",                            0},
        {"THAN",                            0},
        {"THEN",                            0},
        {"THROUGH",                         0},
        {"THRU",                            0},
        {"TIME",                            0},
        {"TIMES",                           0},
        {"TO",                              0},
        {"TOP",                             0},
        {"TRAILING",                        0},
        {"TRUE",                            0},
        {"TYPE",                            0},
        {"TYPEDEF",                         0},
        {"UNIT",                            0},
        {"UNIVERSAL",                       0},
        {"UNLOCK",                          0},
        {"UNSTRING",                        0},
        {"UNTIL",                           0},
        {"UP",                              0},
        {"UPON",                            0},
        {"USAGE",                           0},
        {"USE",                             0},
        {"USER-DEFAULT",                    0},
        {"USING",                           0},
        {"VAL-STATUS",                      0},
        {"VALID",                           0},
        {"VALIDATE",                        0},
        {"VALIDATE-STATUS",                 0},
        {"VALUE",                           0},
        {"VALUES",                          0},
        {"VARYING",                         0},
        {"WHEN",                            0},
        {"WITH",                            0},
        {"WORKING-STORAGE",                 0},
        {"WRITE",                           0},
        {"XOR",                             0},
    };

    private static readonly Dictionary<string, int> ContextLookup = new(168, StringComparer.OrdinalIgnoreCase)
    {
        // TokenContext
        // None         = 0
        // IsClause     = 1
        // IsStatement  = 2
        // IsDevice     = 3
        // IsFigurative = 4
        // IsSymbol     = 5
        // IsEOF        = 6

        // DATA DIVISION CLAUSES
        {"ALIGNED",             1},
        {"ANY",                 1},
        {"LENGTH",              1},
        {"AUTO",                1},
        {"BACKGROUND-COLOR",    1},
        {"BASED",               1},
        {"BELL",                1},
        {"BLANK",               1},
        {"WHEN",                1},
        {"BLINK",               1},
        {"BLOCK",               1},
        {"CONTAINS",            1},
        {"CLASS",               1},
        {"CODE",                1},
        {"CODE-SET",            1},
        {"COLUMN",              1},
        {"CONSTANT",            1},
        {"CONTROL",             1},
        {"DEFAULT",             1},
        {"DESTINATION",         1},
        {"DYNAMIC",             1},
        {"ERASE",               1},
        {"EXTERNAL",            1},
        {"FOREGROUND-COLOR",    1},
        {"FORMAT",              1},
        {"FROM",                1},
        {"FULL",                1},
        {"GLOBAL",              1},
        {"GROUP",               1},
        {"INDICATE",            1},
        {"GROUP-USAGE",         1},
        {"HIGHLIGHT",           1},
        {"INVALID",             1},
        {"JUSTIFIED",           1},
        {"LINAGE",              1},
        {"LINE",                1},
        {"LOWLIGHT",            1},
        {"NEXT",                1},
        {"OCCURS",              1},
        {"PAGE",                1},
        {"PICTURE",             1},
        {"PIC",                 1},
        {"IS",                  1},
        {"PRESENT",             1},
        {"PROPERTY",            1},
        {"RECORD",              1},
        {"REDEFINES",           1},
        {"RENAMES",             1},
        {"REPORT",              1},
        {"REQUIRED",            1},
        {"REVERSE-VIDEO",       1},
        {"SAME",                1},
        {"AS",                  1},
        {"SECURE",              1},
        {"SELECT",              1},
        {"SIGN",                1},
        {"SOURCE",              1},
        {"SUM",                 1},
        {"SYNCHRONIZED",        1},
        {"TO",                  1},
        {"TYPE",                1},
        {"TYPEDEF",             1},
        {"UNDERLINE",           1},
        {"USAGE",               1},
        {"USING",               1},
        {"VALIDATE-STATUS",     1},
        {"VALUE",               1},
        {"VARYING",             1},

        // FILE-CONTROL CLAUSES
        {"ACCESS",              1},
        {"ALTERNATE",           1},
        {"FILE",                1},
        {"STATUS",              1},
        {"LOCK",                1},
        {"ORGANIZATION",        1},
        {"RELATIVE",            1},
        {"RESERVE",             1},
        {"SHARING",             1},
        {"COLLATING",           1},
        {"SEQUENCE",            1},

        // PROCEDURE DIVISION STATEMENTS
        {"ACCEPT",              2},
        {"ADD",                 2},
        {"ALLOCATE",            2},
        {"CALL",                2},
        {"CANCEL",              2},
        {"CLOSE",               2},
        {"COMMIT",              2},
        {"COMPUTE",             2},
        {"CONTINUE",            2},
        {"DELETE",              2},
        {"DISPLAY",             2},
        {"DIVIDE",              2},
        {"EVALUATE",            2},
        {"EXIT",                2},
        {"FREE",                2},
        {"GENERATE",            2},
        {"GOBACK",              2},
        {"GO",                  2},
        {"IF",                  2},
        {"INITIALIZE",          2},
        {"INITIATE",            2},
        {"INSPECT",             2},
        {"INVOKE",              2},
        {"MERGE",               2},
        {"MOVE",                2},
        {"MULTIPLY",            2},
        {"OPEN",                2},
        {"PERFORM",             2},
        {"RAISE",               2},
        {"READ",                2},
        {"RECEIVE",             2},
        {"RELEASE",             2},
        {"RESUME",              2},
        {"RETURN",              2},
        {"REWRITE",             2},
        {"ROLLBACK",            2},
        {"SEARCH",              2},
        {"SEND",                2},
        {"SET",                 2},
        {"SORT",                2},
        {"START",               2},
        {"STOP",                2},
        {"STRING",              2},
        {"SUBTRACT",            2},
        {"SUPPRESS",            2},
        {"TERMINATE",           2},
        {"UNLOCK",              2},
        {"UNSTRING",            2},
        {"USE",                 2},
        {"VALIDATE",            2},
        {"WRITE",               2},

        // SYSTEM DEVICE NAMES
        {"STANDARD-OUTPUT",     3},
        {"STANDARD-INPUT",      3},
        {"COMMAND-LINE",        3},
        {"SINGLE-KEY",          3},

        // FIGURATIVE LITERALS
        {"ZERO",                4},
        {"ZEROES",              4},
        {"ZEROS",               4},
        {"SPACE",               4},
        {"SPACES",              4},
        {"HIGH-VALUE",          4},
        {"HIGH-VALUES",         4},
        {"LOW-VALUE",           4},
        {"LOW-VALUES",          4},
        {"QUOTE",               4},
        {"QUOTES",              4},
        {"ALL",                 4},

        // Standard COBOL symbols
        {"+",                   5},
        {"-",                   5},
        {"**",                  5},
        {"*",                   5},
        {"=",                   5},
        {"/",                   5},
        {"$",                   5},
        {",",                   5},
        {";",                   5},
        {"::",                  5},
        {".",                   5},
        {"(",                   5},
        {")",                   5},
        {">>",                  5},
        {"<>",                  5},
        {">=",                  5},
        {"<=",                  5},
        {">",                   5},
        {"<",                   5},
        {"&",                   5},
        {"_",                   5},
    };

    private static readonly Dictionary<string, int> IntrinsicLookup = new(95, StringComparer.OrdinalIgnoreCase)
    {
        {"ABS",                         0},
        {"ACOS",                        0},
        {"ANNUITY",                     0},
        {"ASIN",                        0},
        {"ATAN",                        0},
        {"BASECONVERT",                 0},
        {"BOOLEAN-OF-INTEGER",          0},
        {"BYTE-LENGTH",                 0},
        {"CHAR",                        0},
        {"CHAR-NATIONAL",               0},
        {"COMBINED-DATETIME",           0},
        {"CONCAT",                      0},
        {"CONVERT",                     0},
        {"COS",                         0},
        {"CURRENT-DATE",                0},
        {"DATE-OF-INTEGER",             0},
        {"DATE-TO-YYYYMMDD",            0},
        {"DAY-OF-INTEGER",              0},
        {"DAY-TO-YYYYDDD",              0},
        {"DISPLAY-OF",                  0},
        {"E",                           0},
        {"EXCEPTION-FILE",              0},
        {"EXCEPTION-FILE-N",            0},
        {"EXCEPTION-LOCATION",          0},
        {"EXCEPTION-LOCATION-N",        0},
        {"EXCEPTION-STATEMENT",         0},
        {"EXCEPTION-STATUS",            0},
        {"EXP",                         0},
        {"EXP10",                       0},
        {"FACTORIAL",                   0},
        {"FIND-STRING",                 0},
        {"FORMATTED-CURRENT-DATE",      0},
        {"FORMATTED-DATE",              0},
        {"FORMATTED-DATETIME",          0},
        {"FORMATTED-TIME",              0},
        {"FRACTION-PART",               0},
        {"HIGHEST-ALGEBRAIC",           0},
        {"INTEGER",                     0},
        {"INTEGER-OF-BOOLEAN",          0},
        {"INTEGER-OF-DATE",             0},
        {"INTEGER-OF-DAY",              0},
        {"INTEGER-OF-FORMATTED-DATE",   0},
        {"INTEGER-PART",                0},
        {"LENGTH",                      0},
        {"LOCALE-COMPARE",              0},
        {"LOCALE-DATE",                 0},
        {"LOCALE-TIME",                 0},
        {"LOCALE-TIME-FROM-SECONDS",    0},
        {"LOG",                         0},
        {"LOG10",                       0},
        {"LOWER-CASE",                  0},
        {"LOWEST-ALGEBRAIC",            0},
        {"MAX",                         0},
        {"MEAN",                        0},
        {"MEDIAN",                      0},
        {"MIDRANGE",                    0},
        {"MIN",                         0},
        {"MOD",                         0},
        {"MODULE-NAME",                 0},
        {"NATIONAL-OF",                 0},
        {"NUMVAL",                      0},
        {"NUMVAL-C",                    0},
        {"NUMVAL-F",                    0},
        {"ORD",                         0},
        {"ORD-MAX",                     0},
        {"ORD-MIN",                     0},
        {"PI",                          0},
        {"PRESENT-VALUE",               0},
        {"RANDOM",                      0},
        {"RANGE",                       0},
        {"REM",                         0},
        {"REVERSE",                     0},
        {"SECONDS-FROM-FORMATTED-TIME", 0},
        {"SECONDS-PAST-MIDNIGHT",       0},
        {"SIGN",                        0},
        {"SIN",                         0},
        {"SMALLEST-ALGEBRAIC",          0},
        {"SQRT",                        0},
        {"STANDARD-COMPARE",            0},
        {"STANDARD-DEVIATION",          0},
        {"SUBSTITUTE",                  0},
        {"SUM",                         0},
        {"TAN",                         0},
        {"TEST-DATE-YYYYMMDD",          0},
        {"TEST-DAY-YYYYDDD",            0},
        {"TEST-FORMATTED-DATETIME",     0},
        {"TEST-NUMVAL",                 0},
        {"TEST-NUMVAL-C",               0},
        {"TEST-NUMVAL-F",               0},
        {"TRIM",                        0},
        {"UPPER-CASE",                  0},
        {"VARIANCE",                    0},
        {"WHEN-COMPILED",               0},
        {"YEAR-TO-YYYY",                0},
    };
}
