namespace Lab3;

// Базовий елемент мережі МО (Create, Process, Dispose успадковують його)
public abstract class Element
{
    public string Name { get; }
    public double Tnext { get; set; } = double.MaxValue; // момент наступної події елемента
    public double Tcurr { get; set; }                     // поточний модельний час
    public int Quantity { get; protected set; }           // кількість вимог, що вийшли з елемента
    public int RouteFailures { get; protected set; }      // вимоги, для яких усі маршрути закриті

    private readonly List<Route> routes = new();

    // Статистика надходжень: середній інтервал між надходженнями в елемент
    public int Arrivals { get; private set; }
    private double firstArrival, lastArrival;

    protected Element(string name) => Name = name;

    public Route AddRoute(Element target, double probability = 1.0, int priority = 1,
                          Func<bool> block = null, Func<Item, bool> accepts = null,
                          Action<Item> onPass = null)
    {
        var r = new Route(target, probability, priority, block, accepts, onPass);
        routes.Add(r);
        return r;
    }

    // Чи може елемент прийняти вимогу зараз (є вільний пристрій або місце в черзі)
    public virtual bool CanAccept => true;

    // Вхід вимоги в елемент
    public abstract void InAct(Item item);

    // Вихід вимоги з елемента
    public virtual void OutAct() { }

    // Накопичення статистики за інтервал delta між подіями
    public virtual void DoStatistics(double delta) { }

    protected void RegisterArrival()
    {
        if (Arrivals == 0) firstArrival = Tcurr;
        lastArrival = Tcurr;
        Arrivals++;
    }

    // Сума інтервалів між послідовними надходженнями дорівнює lastArrival - firstArrival
    public double MeanArrivalInterval => Arrivals > 1 ? (lastArrival - firstArrival) / (Arrivals - 1) : 0;

    // Передати вимогу за маршрутом. false — усі маршрути закриті, вимога отримує відмову
    protected bool Pass(Item item)
    {
        Route r = ChooseRoute(item);
        if (r == null) return false;
        r.OnPass?.Invoke(item);
        r.Target.InAct(item);
        return true;
    }

    // Вибір маршруту за пріоритетом:
    // спочатку маршрути з найвищим пріоритетом; якщо слідування ними неможливе
    // (маршрут заблоковано або наступна СМО не може прийняти вимогу), береться
    // нижчий пріоритет. Серед дуг з однаковим пріоритетом — за ймовірністю.
    // На найнижчому рівні наповненість СМО не перевіряється: вимога, що не
    // знайшла місця, отримує відмову в самій СМО.
    public Route ChooseRoute(Item item)
    {
        var open = routes.Where(r => r.IsOpen(item)).ToList();
        if (open.Count == 0) return null;

        var levels = open.Select(r => r.Priority).Distinct().OrderByDescending(p => p).ToList();
        for (int i = 0; i < levels.Count; i++)
        {
            var candidates = open.Where(r => r.Priority == levels[i]).ToList();
            if (i < levels.Count - 1)
                candidates = candidates.Where(r => r.Target.CanAccept).ToList();
            if (candidates.Count > 0)
                return ByProbability(candidates);
        }
        return null;
    }

    private static Route ByProbability(List<Route> candidates)
    {
        if (candidates.Count == 1) return candidates[0];
        // множення на суму нормує ймовірності, якщо частину дуг відкинуто
        double r = FunRand.Uniform01() * candidates.Sum(c => c.Probability);
        double sum = 0;
        foreach (var c in candidates)
        {
            sum += c.Probability;
            if (r <= sum) return c;
        }
        return candidates[^1];
    }

    public virtual void PrintInfo() =>
        Console.WriteLine($"{Name} quantity = {Quantity} tnext = {FormatTime(Tnext)}");

    public virtual void PrintResult(double totalTime) =>
        Console.WriteLine($"{Name} quantity = {Quantity}");

    protected static string FormatTime(double t) => t == double.MaxValue ? "inf" : t.ToString("F4");
}
