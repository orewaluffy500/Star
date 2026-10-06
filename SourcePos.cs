namespace star;


public struct SourcePos(string fileName)
{
    public string FileName = fileName;
    public int Index = -1;
    public int Row = 1;
    public int Col = 0;

    public void Next(bool isNewline)
    {
        Index++;
        Col++;
        if (isNewline)
        {
            Col = 0;
            Row++;
        }
    }
}