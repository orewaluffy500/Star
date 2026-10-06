using star.error;
using star.value;

namespace star.memory;


public class Memory
{
    public StInt[] RegisterFile = new StInt[64];

    public Memory()
    {
        SourcePos pos = new("<register_file>");
        for (int i = 0; i < RegisterFile.Length; i++)
        {
            RegisterFile[i] = (StInt) new StInt(0).Repos(pos);
        }
    }

    public BaseError? SetRegister(int index, int value, SourcePos site)
    {
        GetRegister(index, site, out var error);
        if (error is not null) return error;

        RegisterFile[index].Value = value;
        return null;
    }

    public StInt GetRegister(int index, SourcePos site, out BaseError? error)
    {
        if (index < 0 || index >= RegisterFile.Length)
        {
            error = new StRuntimeError($"out of bounds register! {index}", site);
            return (StInt) new StInt(0).Repos(site);
        }

        error = null;
        return (StInt) new StInt(RegisterFile[index].Value).Repos(site);
    }
}