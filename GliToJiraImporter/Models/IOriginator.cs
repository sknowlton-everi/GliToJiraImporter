namespace GliToJiraImporter.Models
{
    public interface IOriginator
    {
        //TODO This constructor is required by all, but I don't think it's possible...
        //IOriginator(string state);

        IMemento Save();

        void Restore(IMemento memento);
    }
}