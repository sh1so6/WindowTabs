using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripFormFactory
    {
        private readonly ManagedGroupStripDisplayStateService displayStateService;
        private readonly ManagedGroupStripFormStateService formStateService;
        private readonly ManagedGroupStripDropController stripDropController;
        private readonly ManagedGroupStripButtonVisualService buttonVisualService;
        private readonly ManagedGroupStripControlBindingService controlBindingService;
        private readonly ManagedGroupStripPlacementService stripPlacementService;
        private readonly IDragDrop dragDrop;

        public ManagedGroupStripFormFactory(
            ManagedGroupStripDisplayStateService displayStateService,
            ManagedGroupStripFormStateService formStateService,
            ManagedGroupStripDropController stripDropController,
            ManagedGroupStripButtonVisualService buttonVisualService,
            ManagedGroupStripControlBindingService controlBindingService,
            ManagedGroupStripPlacementService stripPlacementService,
            IDragDrop dragDrop)
        {
            this.displayStateService = displayStateService ?? throw new ArgumentNullException(nameof(displayStateService));
            this.formStateService = formStateService ?? throw new ArgumentNullException(nameof(formStateService));
            this.stripDropController = stripDropController ?? throw new ArgumentNullException(nameof(stripDropController));
            this.buttonVisualService = buttonVisualService ?? throw new ArgumentNullException(nameof(buttonVisualService));
            this.controlBindingService = controlBindingService ?? throw new ArgumentNullException(nameof(controlBindingService));
            this.stripPlacementService = stripPlacementService ?? throw new ArgumentNullException(nameof(stripPlacementService));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
        }

        public ManagedGroupStripForm Create()
        {
            return new ManagedGroupStripForm(
                displayStateService,
                formStateService,
                stripDropController,
                buttonVisualService,
                controlBindingService,
                stripPlacementService,
                dragDrop);
        }
    }
}
