using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpamBot.Services;

internal static class KeystrokeSender
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private const ushort VirtualKeyReturn = 0x0D;

    static KeystrokeSender()
    {
        // SendInput Rejects The Call On A Mismatched Size
        Debug.Assert(Marshal.SizeOf<KeyboardInput>() == 40, "Wrong KeyboardInput Layout");
    }

    public static void SendLine(string text)
    {
        KeyboardInput[] inputs = BuildInputs(text);

        if (
            SendInput((uint)inputs.Length, ref inputs[0], Marshal.SizeOf<KeyboardInput>())
            != inputs.Length
        )
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static KeyboardInput[] BuildInputs(string text)
    {
        List<KeyboardInput> inputs = new(text.Length * 2 + 2);

        foreach (char character in text)
        {
            switch (character)
            {
                // Carriage Return Rides Along With The Newline
                case '\r':
                    break;
                case '\n':
                    AddKeyPress(inputs, VirtualKeyReturn, isUnicode: false);
                    break;
                default:
                    AddKeyPress(inputs, character, isUnicode: true);
                    break;
            }
        }

        AddKeyPress(inputs, VirtualKeyReturn, isUnicode: false);
        return [.. inputs];
    }

    private static void AddKeyPress(List<KeyboardInput> inputs, ushort key, bool isUnicode)
    {
        uint flags = isUnicode ? KeyEventUnicode : 0;
        inputs.Add(CreateKeyEvent(key, flags, isUnicode));
        inputs.Add(CreateKeyEvent(key, flags | KeyEventKeyUp, isUnicode));
    }

    private static KeyboardInput CreateKeyEvent(ushort key, uint flags, bool isUnicode) =>
        new()
        {
            Type = InputKeyboard,
            VirtualKey = isUnicode ? (ushort)0 : key,
            ScanCode = isUnicode ? key : (ushort)0,
            Flags = flags,
        };

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct KeyboardInput
    {
        // Win32 INPUT On 64-Bit, Union Starts At Offset 8
        [FieldOffset(0)]
        public uint Type;

        [FieldOffset(8)]
        public ushort VirtualKey;

        [FieldOffset(10)]
        public ushort ScanCode;

        [FieldOffset(12)]
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, ref KeyboardInput inputs, int size);
}
