namespace CalcEngine.Core;

/// <summary>A single cell address. Value type: two ints, no allocation.</summary>
public readonly record struct CellRef(int Row, int Column)
{
    public static CellRef Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new FormatException("Cell reference is empty.");

        s = s.Trim().ToUpperInvariant();

        int i = 0;
        while (i < s.Length && s[i] >= 'A' && s[i] <= 'Z') i++;

        if (i == 0)          throw new FormatException($"'{s}' has no column letters.");
        if (i == s.Length)   throw new FormatException($"'{s}' has no row number.");

        // column: bijective base-26.  A=1 ... Z=26, AA=27
        int col = 0;
        for (int k = 0; k < i; k++)
            col = col * 26 + (s[k] - 'A' + 1);

        // row: every remaining character must be a digit
        int row = 0;
        for (int k = i; k < s.Length; k++)
        {
            if (s[k] < '0' || s[k] > '9')
                throw new FormatException($"'{s}' has an unexpected character at position {k}.");
            row = row * 10 + (s[k] - '0');
        }

        if (row < 1) throw new FormatException($"'{s}' has row 0; rows start at 1.");

        return new CellRef(row, col);
    }

    public string ToA1()
    {
        var buf = new Stack<char>();
        int n = Column;
        while (n > 0)
        {
            n--;                                  // shift to 0-based for the modulo
            buf.Push((char)('A' + n % 26));
            n /= 26;
        }
        return new string(buf.ToArray()) + Row;
    }

    public override string ToString() => ToA1();
}