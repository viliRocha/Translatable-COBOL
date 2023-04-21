using static Otterkit.Types.TokenHandling;

namespace Otterkit.Types;

public partial class DataEntry
{
    public Option<DataEntry> Parent;
    public Option<Token> Identifier;
    public Option<string> ExternalizedName;
    public EntryType EntryType;

    public CurrentScope Section;
    public UsageType Usage;
    public int LevelNumber;

    public bool IsGroup;
    public bool IsConstant;

    private ulong ClauseBitField;
    public int ClauseDeclaration;

    public DataEntry(Token identifier, EntryType entryType) 
    {
        Identifier = identifier;
        EntryType = entryType;
    }

    public bool this[DataClause clauseName]
    {
        get => GetClauseBit(clauseName);

        set => SetClauseBit(clauseName, value);
    }

    private void SetClauseBit(DataClause clause, bool bit)
    {
        var mask = 1UL << (int)clause - 1;

        if (bit)
        {
            ClauseBitField |= mask;
            return;
        }

        ClauseBitField &= ~mask;
    }

    private bool GetClauseBit(DataClause clause)
    {
        var position = (int)clause - 1;

        var bit = (ClauseBitField >> position) & 1;

        return bit == 1UL;
    }

    public bool FetchTypedef()
    {
        if (!this[DataClause.Typedef])
        {
            throw new NullReferenceException("NOTE: Always check if clause is present before running this method.");
        }

        var currentIndex = TokenHandling.Index;

        TokenHandling.Index = ClauseDeclaration;

        var isStrong = false;

        while (CurrentEquals(TokenContext.IsClause))
        {
            if (CurrentEquals("TYPEDEF") && LookaheadEquals(1, "STRONG"))
            {
                isStrong = true;
                break;
            }

            Continue();
        }

        TokenHandling.Index = currentIndex;

        return isStrong;
    }

    public Token FetchType()
    {
        if (!this[DataClause.Type])
        {
            throw new NullReferenceException("NOTE: Always check if clause is present before running this method.");
        }

        var currentIndex = TokenHandling.Index;

        TokenHandling.Index = ClauseDeclaration;

        while (CurrentEquals(TokenContext.IsClause))
        {
            if (CurrentEquals("TYPE") && LookaheadEquals(1, TokenType.Identifier))
            {
                Continue();
                break;
            }

            Continue();
        }

        var type = Current();

        TokenHandling.Index = currentIndex;

        return type;
    }
}
