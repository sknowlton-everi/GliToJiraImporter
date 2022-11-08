namespace GliToJiraImporter.Models
{
    public interface IMemento
    {
        string GetName();//TODO Should this be renamed because their names aren't called Name?

        IMemento GetState();

        bool IsValid();

        bool IsEmpty();
    }
}