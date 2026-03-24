using System;
using System.Runtime.InteropServices;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class NumericTabHotKeyService : IDisposable
    {
        private readonly SettingsSession settingsSession;
        private readonly AppBehaviorState appBehaviorState;
        private readonly GroupWindowActivationService groupWindowActivationService;
        private readonly HOOKPROC hookProc;
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
            hookHandle = WinUserApi.SetWindowsHookEx(
                WindowHookTypes.WH_KEYBOARD_LL,
                hookProc,
                WinBaseApi.GetModuleHandle(IntPtr.Zero),
                0);
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
                WinUserApi.UnhookWindowsHookEx(hookHandle);
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
                    && (wParam.ToInt32() == WindowMessages.WM_KEYDOWN || wParam.ToInt32() == WindowMessages.WM_SYSKEYDOWN))
                {
                    var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var tabIndex = TryGetTabIndex(data.vkCode);
                    if (tabIndex.HasValue && Win32Helper.IsKeyPressed(VirtualKeyCodes.VK_CONTROL))
                    {
                        groupWindowActivationService.TryActivateForegroundIndex(tabIndex.Value);
                    }
                }
            }
            catch
            {
            }

            return WinUserApi.CallNextHookEx(hookHandle, code, wParam, lParam);
        }

        private static int? TryGetTabIndex(int virtualKey)
        {
            var index = virtualKey - 0x31;
            return index >= 0 && index < 9 ? index : (int?)null;
        }
    }
}
