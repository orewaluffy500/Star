using star.error;
using star.value;

namespace star.memory;


public class Memory
{
    public StValue[] RegisterFile = new StValue[64];

    public Memory()
    {
        SourcePos pos = new("<register_file>");
        for (int i = 0; i < RegisterFile.Length; i++)
        {
            RegisterFile[i] = new StInt(0).Repos(pos);
        }
    }

    public BaseError? SetRegister(int index, StValue value, SourcePos site)
    {
        GetRegister(index, site, out var error);
        if (error is not null) return error;

        RegisterFile[index] = value;
        return null;
    }

    public StValue GetRegister(int index, SourcePos site, out BaseError? error)
    {
        if (index < 0 || index >= RegisterFile.Length)
        {
            error = new StRuntimeError($"out of bounds register! {index}", site);
            return new StInt(0).Repos(site);
        }

        error = null;
        return RegisterFile[index].Copy();
    }
}