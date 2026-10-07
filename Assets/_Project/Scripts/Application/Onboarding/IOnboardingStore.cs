namespace StructureViewer.Application.Onboarding
{
    // Remembers that the controls hint was dismissed. Storage may be unavailable (private browsing): then it just forgets.
    public interface IOnboardingStore
    {
        bool HasSeenHint { get; }
        void MarkHintSeen();
    }
}
