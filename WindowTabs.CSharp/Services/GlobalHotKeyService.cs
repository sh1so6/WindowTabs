using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Bemo;
using Bemo.Win32;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GlobalHotKeyService : IDisposable
    {
        private readonly HotKeySettingsStore hotKeySettingsStore;
        private readonly AppBehaviorState appBehaviorState;
        private readonly GroupWindowActivationService groupWindowActivationService;
        private readonly Dictionary<string, HotKeyBinding> bindings = new Dictionary<string, HotKeyBinding>(StringComparer.OrdinalIgnoreCase);
        private readonly HotKeyWindow hotKeyWindow;
        private bool isInitialized;
        private bool isDisposed;

        public GlobalHotKeyService(
            HotKeySettingsStore hotKeySettingsStore,
            AppBehaviorState appBehaviorState,
            GroupWindowActivationService groupWindowActivationService)
        {
            this.hotKeySettingsStore = hotKeySettingsStore ?? throw new ArgumentNullException(nameof(hotKeySettingsStore));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            this.groupWindowActivationService = groupWindowActivationService ?? throw new ArgumentNullException(nameof(groupWindowActivationService));
            hotKeyWindow = new HotKeyWindow(HandleHotKeyMessage);

            bindings["prevTab"] = new HotKeyBinding(1, false);
            bindings["nextTab"] = new HotKeyBinding(2, true);
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            hotKeySettingsStore.Changed += OnHotKeySettingsChanged;
            RegisterConfiguredHotKeys();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            hotKeySettingsStore.Changed -= OnHotKeySettingsChanged;
            UnregisterAll();
            hotKeyWindow.Dispose();
        }

        private void OnHotKeySettingsChanged(object sender, EventArgs e)
        {
            if (!isInitialized)
            {
                return;
            }

            RegisterConfiguredHotKeys();
        }

        private void RegisterConfiguredHotKeys()
        {
            foreach (var binding in bindings.Values)
            {
                hotKeyWindow.Unregister(binding.Id);
            }

            foreach (var pair in bindings)
            {
                var shortcutCode = hotKeySettingsStore.Get(pair.Key);
                if (shortcutCode == 0)
                {
                    continue;
                }

                var shortcut = new HotKeyShortcut
                {
                    HotKeyControlCode = unchecked((short)shortcutCode)
                };

                hotKeyWindow.Register(
                    pair.Value.Id,
                    shortcut.RegisterHotKeyModifierFlags,
                    shortcut.RegisterHotKeyVirtualKeyCode);
            }
        }

        private void UnregisterAll()
        {
            foreach (var binding in bindings.Values)
            {
                hotKeyWindow.Unregister(binding.Id);
            }
        }

        private void HandleHotKeyMessage(int hotKeyId)
        {
            if (appBehaviorState.IsDisabled)
            {
                return;
            }

            foreach (var binding in bindings.Values)
            {
                if (binding.Id != hotKeyId)
                {
                    continue;
                }

                groupWindowActivationService.TryActivateForegroundRelative(binding.MoveNext);
                break;
            }
        }

        private sealed class HotKeyBinding
        {
            public HotKeyBinding(int id, bool moveNext)
            {
                Id = id;
                MoveNext = moveNext;
            }

            public int Id { get; }

            public bool MoveNext { get; }
        }

        private sealed class HotKeyWindow : NativeWindow, IDisposable
        {
            private readonly Action<int> onHotKey;
            private bool isDisposed;

            public HotKeyWindow(Action<int> onHotKey)
            {
                this.onHotKey = onHotKey ?? throw new ArgumentNullException(nameof(onHotKey));
                CreateHandle(new CreateParams
                {
                    Caption = "WindowTabs.CSharp.HotKeyWindow"
                });
            }

            public void Register(int id, int modifiers, int virtualKey)
            {
                if (virtualKey == 0)
                {
                    return;
                }

                var finalModifiers = modifiers | HotKeyConstants.MOD_NOREPEAT;
                WinUserApi.RegisterHotKey(Handle, id, finalModifiers, virtualKey);
            }

            public void Unregister(int id)
            {
                if (Handle != IntPtr.Zero)
                {
                    WinUserApi.UnregisterHotKey(Handle, id);
                }
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                DestroyHandle();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WindowMessages.WM_HOTKEY)
                {
                    onHotKey(m.WParam.ToInt32());
                }

                base.WndProc(ref m);
            }
        }
    }
}
