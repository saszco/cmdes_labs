namespace Lab3;

// Дуга мережі МО: маршрут слідування вимоги до наступного елемента.
// За замовчуванням ймовірність слідування 1 і пріоритет 1.
public class Route
{
    public Element Target { get; }
    public double Probability { get; }
    public int Priority { get; }              // більше значення — вищий пріоритет

    // Блокування маршруту: маршрут закритий, якщо B(S, t) = 1
    public Func<bool> Block { get; }

    // Які типи вимог можуть слідувати цим маршрутом
    public Func<Item, bool> Accepts { get; }

    // Дія з вимогою при проходженні дуги (наприклад, зміна типу)
    public Action<Item> OnPass { get; }

    public Route(Element target, double probability, int priority,
                 Func<bool> block, Func<Item, bool> accepts, Action<Item> onPass)
    {
        Target = target;
        Probability = probability;
        Priority = priority;
        Block = block ?? (() => false);
        Accepts = accepts ?? (_ => true);
        OnPass = onPass;
    }

    public bool IsOpen(Item item) => Accepts(item) && !Block();
}
