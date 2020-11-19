using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;

namespace DalamudVox
{
    class GameFunctions
    {
        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);
        private delegate void EasierProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private readonly GetUIModuleDelegate getUiModule;
        private readonly EasierProcessChatBoxDelegate easierProcessChatBox;

        private readonly IntPtr uiModulePtr;

        public GameFunctions(SigScanner scanner) {

            var getUiModulePtr = scanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
            var easierProcessChatBoxPtr = scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9");
            this.uiModulePtr = scanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??");
            this.getUiModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUiModulePtr);
            this.easierProcessChatBox = Marshal.GetDelegateForFunctionPointer<EasierProcessChatBoxDelegate>(easierProcessChatBoxPtr);
        }

        public void ProcessChatBox(string message) {
            IntPtr uiModule = this.getUiModule(Marshal.ReadIntPtr(this.uiModulePtr));

            if (uiModule == IntPtr.Zero) {
                throw new ApplicationException("uiModule was null");
            }

            using var payload = new ChatPayload(message);
            IntPtr mem1 = Marshal.AllocHGlobal(400);
            Marshal.StructureToPtr(payload, mem1, false);

            this.easierProcessChatBox(uiModule, mem1, IntPtr.Zero, 0);

            Marshal.FreeHGlobal(mem1);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    readonly struct ChatPayload : IDisposable {
        [FieldOffset(0)] readonly IntPtr textPtr;
        [FieldOffset(16)] readonly ulong textLen;

        [FieldOffset(8)] readonly ulong unk1;
        [FieldOffset(24)] readonly ulong unk2;

        internal ChatPayload(string text) {
            byte[] stringBytes = Encoding.UTF8.GetBytes(text);
            this.textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
            Marshal.Copy(stringBytes, 0, this.textPtr, stringBytes.Length);
            Marshal.WriteByte(this.textPtr + stringBytes.Length, 0);

            this.textLen = (ulong)(stringBytes.Length + 1);

            this.unk1 = 64;
            this.unk2 = 0;
        }

        public void Dispose() {
            Marshal.FreeHGlobal(this.textPtr);
        }
    }
}
