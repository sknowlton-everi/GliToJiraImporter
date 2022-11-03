namespace GliToJiraImporter.Models
{
    public interface IOriginator
    {
        //IOriginator(string state);
        //void DoSomething();
        //void Parse(IEntity entity);
        IMemento Save();

        void Restore(IMemento memento);
    }
}