using System;
using System.Drawing;
using System.Windows.Forms;
using Bemo;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyProgramBridge : IProgram
    {
        private readonly LegacyProgramLifecycle lifecycle;
        private readonly LegacyAppWindowCatalog appWindowCatalog;
        private readonly LegacyTabAppearanceCatalog tabAppearanceCatalog;
        private readonly LegacyNewWindowLauncher windowLauncher;
        private readonly LegacyProgramSettingsFacade programSettingsFacade;
        private readonly LegacyWindowPresentationAdapter windowPresentationAdapter;
        private readonly FSharpEvent<Unit> newVersionEvent = new FSharpEvent<Unit>();
        private readonly FSharpEvent<Tuple<int, IntPtr>> llMouseEvent = new FSharpEvent<Tuple<int, IntPtr>>();

        public LegacyProgramBridge(
            LegacyProgramLifecycle lifecycle,
            LegacyAppWindowCatalog appWindowCatalog,
            LegacyTabAppearanceCatalog tabAppearanceCatalog,
            LegacyNewWindowLauncher windowLauncher,
            LegacyProgramSettingsFacade programSettingsFacade,
            LegacyWindowPresentationAdapter windowPresentationAdapter)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.appWindowCatalog = appWindowCatalog ?? throw new ArgumentNullException(nameof(appWindowCatalog));
            this.tabAppearanceCatalog = tabAppearanceCatalog ?? throw new ArgumentNullException(nameof(tabAppearanceCatalog));
            this.windowLauncher = windowLauncher ?? throw new ArgumentNullException(nameof(windowLauncher));
            this.programSettingsFacade = programSettingsFacade ?? throw new ArgumentNullException(nameof(programSettingsFacade));
            this.windowPresentationAdapter = windowPresentationAdapter ?? throw new ArgumentNullException(nameof(windowPresentationAdapter));
        }

        public string version => "WindowTabs.CSharp.LegacyBridge";

        public bool isUpgrade => false;

        public bool isFirstRun => tabAppearanceCatalog.IsFirstRun;

        public FSharpOption<int> tabLimit => null;

        public List2<IntPtr> appWindows => appWindowCatalog.GetAppWindows();

        public TabAppearanceInfo tabAppearanceInfo => tabAppearanceCatalog.Current;

        public TabAppearanceInfo defaultTabAppearanceInfo => tabAppearanceCatalog.Default;

        public TabAppearanceInfo darkModeTabAppearanceInfo => tabAppearanceCatalog.DarkMode;

        public TabAppearanceInfo darkModeBlueTabAppearanceInfo => tabAppearanceCatalog.DarkModeBlue;

        public TabAppearanceInfo lightMonoTabAppearanceInfo => tabAppearanceCatalog.LightMono;

        public TabAppearanceInfo darkMonoTabAppearanceInfo => tabAppearanceCatalog.DarkMono;

        public TabAppearanceInfo darkRedFrameTabAppearanceInfo => tabAppearanceCatalog.DarkRedFrame;

        public IEvent<FSharpHandler<Unit>, Unit> newVersion => newVersionEvent.Publish;

        public IEvent<FSharpHandler<Tuple<int, IntPtr>>, Tuple<int, IntPtr>> llMouse => llMouseEvent.Publish;

        public bool isDisabled => lifecycle.IsDisabled;

        public bool isShuttingDown => lifecycle.IsShuttingDown;

        public void SetRefreshAction(Action<string> refreshAction)
        {
            lifecycle.SetRefreshAction(refreshAction);
        }

        public void refresh()
        {
            lifecycle.Refresh();
        }

        public void shutdown()
        {
            lifecycle.RequestShutdown(Application.Exit);
        }

        public void setWindowNameOverride(Tuple<IntPtr, FSharpOption<string>> value)
        {
            windowPresentationAdapter.SetWindowNameOverride(value);
        }

        public FSharpOption<string> getWindowNameOverride(IntPtr hwnd)
        {
            return windowPresentationAdapter.GetWindowNameOverride(hwnd);
        }

        public void setWindowFillColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationAdapter.SetWindowFillColor(hwnd, color);
        }

        public FSharpOption<Color> getWindowFillColor(IntPtr hwnd)
        {
            return windowPresentationAdapter.GetWindowFillColor(hwnd);
        }

        public void setWindowUnderlineColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationAdapter.SetWindowUnderlineColor(hwnd, color);
        }

        public FSharpOption<Color> getWindowUnderlineColor(IntPtr hwnd)
        {
            return windowPresentationAdapter.GetWindowUnderlineColor(hwnd);
        }

        public void setWindowBorderColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationAdapter.SetWindowBorderColor(hwnd, color);
        }

        public FSharpOption<Color> getWindowBorderColor(IntPtr hwnd)
        {
            return windowPresentationAdapter.GetWindowBorderColor(hwnd);
        }

        public void setWindowPinned(IntPtr hwnd, bool pinned)
        {
            windowPresentationAdapter.SetWindowPinned(hwnd, pinned);
        }

        public bool isWindowPinned(IntPtr hwnd)
        {
            return windowPresentationAdapter.IsWindowPinned(hwnd);
        }

        public void setWindowAlignment(IntPtr hwnd, FSharpOption<TabAlign> alignment)
        {
            windowPresentationAdapter.SetWindowAlignment(hwnd, alignment);
        }

        public FSharpOption<TabAlign> getWindowAlignment(IntPtr hwnd)
        {
            return windowPresentationAdapter.GetWindowAlignment(hwnd);
        }

        public bool getAutoGroupingEnabled(string procPath)
        {
            return programSettingsFacade.GetAutoGroupingEnabled(procPath);
        }

        public void setAutoGroupingEnabled(string procPath, bool enabled)
        {
            programSettingsFacade.SetAutoGroupingEnabled(procPath, enabled);
            refresh();
        }

        public bool getCategoryEnabled(string procPath, int categoryNum)
        {
            return programSettingsFacade.GetCategoryEnabled(procPath, categoryNum);
        }

        public void setCategoryEnabled(string procPath, int categoryNum, bool enabled)
        {
            programSettingsFacade.SetCategoryEnabled(procPath, categoryNum, enabled);
        }

        public void ping()
        {
        }

        public void setHotKey(string key, int value)
        {
            programSettingsFacade.SetHotKey(key, value);
        }

        public int getHotKey(string key)
        {
            return programSettingsFacade.GetHotKey(key);
        }

        public void notifyNewVersion()
        {
            newVersionEvent.Trigger(default(Unit));
        }

        public void suspendTabMonitoring()
        {
            lifecycle.SuspendTabMonitoring();
        }

        public void resumeTabMonitoring()
        {
            lifecycle.ResumeTabMonitoring();
        }

        public void setDisabled(bool value)
        {
            lifecycle.SetDisabled(value);
        }

        public void saveTabGroupsBeforeExit()
        {
        }

        public void launchNewWindow(IntPtr groupHwnd, IntPtr invokerHwnd, string processPath)
        {
            windowLauncher.Launch(groupHwnd, invokerHwnd, processPath);
        }

        public List2<string> getAllConfiguredProcessPaths()
        {
            return programSettingsFacade.GetAllConfiguredProcessPaths();
        }

        public void removeProcessSettings(string procPath)
        {
            programSettingsFacade.RemoveProcessSettings(procPath);
            refresh();
        }
    }
}
