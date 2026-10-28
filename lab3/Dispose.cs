namespace Lab3;

// Вихід з мережі: рахує вимоги, що завершили обслуговування (за типами)
public class Dispose : Element
{
    private readonly Dictionary<int, int> byType = new();

    public Dispose(string name) : base(name) { }

    public override void InAct(Item item)
    {
        RegisterArrival();
        Quantity++;
        byType[item.InitialType] = CountOfType(item.InitialType) + 1;
    }

    public int CountOfType(int initialType) => byType.GetValueOrDefault(initialType);
}
