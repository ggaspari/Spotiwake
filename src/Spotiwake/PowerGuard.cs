using System.Runtime.InteropServices;

namespace Spotiwake;

/// <summary>
/// Controla o estado de energia do sistema via SetThreadExecutionState.
/// O estado é associado à thread que faz a chamada, portanto esta classe
/// deve ser usada sempre a partir da mesma thread (a thread de UI).
/// </summary>
internal sealed class PowerGuard : IDisposable
{
    [Flags]
    private enum ExecutionState : uint
    {
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
        Continuous = 0x80000000,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    public bool IsActive { get; private set; }

    /// <summary>Impede a suspensão do sistema (e opcionalmente o desligamento da tela).</summary>
    public void KeepAwake(bool keepDisplayOn)
    {
        var flags = ExecutionState.Continuous | ExecutionState.SystemRequired;
        if (keepDisplayOn)
        {
            flags |= ExecutionState.DisplayRequired;
        }

        SetThreadExecutionState(flags);
        IsActive = true;
    }

    /// <summary>Devolve o controle de suspensão ao sistema.</summary>
    public void Release()
    {
        if (!IsActive)
        {
            return;
        }

        SetThreadExecutionState(ExecutionState.Continuous);
        IsActive = false;
    }

    public void Dispose() => Release();
}
