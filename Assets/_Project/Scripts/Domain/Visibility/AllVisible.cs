namespace StructureViewer.Domain.Visibility
{
    public sealed class AllVisible : IVisibility
    {
        public static readonly AllVisible Instance = new AllVisible();

        private AllVisible()
        {
        }

        public bool IsVisible(int index) => true;
    }
}
