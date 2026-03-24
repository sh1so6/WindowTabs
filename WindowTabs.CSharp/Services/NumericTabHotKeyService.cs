using System;
using System.Runtime.InteropServices;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class NumericTabHotKeyService : IDisposable
    {
        private readonly SettingsSession settingsSession;
        private readonly AppBehaviorState appBehaviorState;
        private readonly GroupWindowActivationService groupWindowActivationService;
        private readonly NativeKeyboardApi.HookProc hookProc;
        private IntPtr hookHandle;
        private bool initialized;
        private bool disposed;

        public NumericTabHotKeyService(
            SettingsSession settingsSession,
            AppBehaviorState appBehaviorState,
            GroupWindowActivationService groupWindowActivationService)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            this.groupWindowActivationService = groupWindowActivationService ?? throw new ArgumentNullException(nameof(groupWindowActivationService));
            hookProc = HookProc;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            hookHandle = NativeKeyboardApi.SetLowLevelKeyboardHook(hookProc);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (hookHandle != IntPtr.Zero)
            {
                NativeKeyboardApi.UnhookWindowsHook(hookHandle);
                hookHandle = IntPtr.Zero;
            }
        }

        private int HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (code >= 0
                    && !appBehaviorState.IsDisabled
                    && settingsSession.Current.EnableCtrlNumberHotKey
                    && (wParam.ToInt32() == NativeKeyboardApi.WmKeyDown || wParam.ToInt32() == NativeKeyboardApi.WmSysKeyDown))
                {
                    var data = Marshal.PtrToStructure<NativeKeyboardApi.KeyboardHookStruct>(lParam);
                    var tabIndex = TryGetTabIndex(data.VirtualKeyCode);
                    if (tabIndex.HasValue && NativeKeyboardApi.IsKeyPressed(NativeKeyboardApi.VkControl))
                    {
                        groupWindowActivationService.TryActivateForegroundIndex(tabIndex.Value);
                    }
                }
            }
            catch
            {
            }

            return NativeKeyboardApi.CallNextHook(hookHandle, code, wParam, lParam);
        }

        private static int? TryGetTabIndex(int virtualKey)
        {
            var index = virtualKey - 0x31;
            return index >= 0 && index < 9 ? index : (int?)null;
        }
    }
}
