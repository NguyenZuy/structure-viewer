namespace StructureViewer.Presentation.Shell
{
    public enum SheetHeight
    {
        Closed,
        Half,
        Full
    }

    public static class SheetSnap
    {
        // Where the sheet settles after the handle is released. A tap toggles half/full; a swipe up opens fully;
        // a swipe down steps full → half → closed.
        public static SheetHeight AfterDrag(SheetHeight current, float draggedUp, float threshold)
        {
            if (draggedUp < threshold && draggedUp > -threshold)
                return current == SheetHeight.Full ? SheetHeight.Half : SheetHeight.Full;
            if (draggedUp > 0f)
                return SheetHeight.Full;
            return current == SheetHeight.Full ? SheetHeight.Half : SheetHeight.Closed;
        }
    }
}
