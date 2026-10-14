namespace Lab2;

// Базовий елемент моделі (Create, Process, Dispose успадковують його)
public class Element
{
    public string Name { get; set; }
    public double Tnext { get; set; }        // момент наступної події елемента
    public double Tcurr { get; set; }        // поточний модельний час
    public double DelayMean { get; set; }    // середня затримка (для unif — мінімум)
    public double DelayDev { get; set; }     // відхилення (для unif — максимум)
    public string Distribution { get; set; } // "exp", "norm", "unif" або "" (стала затримка)
    public int Quantity { get; protected set; } // кількість вимог, що вийшли з елемента

    // Маршрут: наступні елементи та ймовірності переходу до них
    private readonly List<Element> nextElements = new();
    private readonly List<double> probabilities = new();

    public Element(string name, double delay)
    {
        Name = name;
        DelayMean = delay;
        Distribution = "exp";
        Tnext = double.MaxValue;
        Tcurr = 0.0;
    }

    // Додати наступний елемент маршруту з ймовірністю переходу
    public void AddNext(Element next, double probability = 1.0)
    {
        nextElements.Add(next);
        probabilities.Add(probability);
    }

    // Вибір наступного елемента за ймовірностями (null — вимога залишає модель)
    public Element ChooseNext()
    {
        if (nextElements.Count == 0) return null;
        if (nextElements.Count == 1) return nextElements[0];

        double r = FunRand.Uniform01() * probabilities.Sum();
        double sum = 0;
        for (int i = 0; i < nextElements.Count; i++)
        {
            sum += probabilities[i];
            if (r <= sum) return nextElements[i];
        }
        return nextElements[^1];
    }

    public double GetDelay()
    {
        switch (Distribution.ToLower())
        {
            case "exp": return FunRand.Exp(DelayMean);
            case "norm": return FunRand.Norm(DelayMean, DelayDev);
            case "unif": return FunRand.Unif(DelayMean, DelayDev);
            default: return DelayMean;
        }
    }

    // Вхід вимоги в елемент
    public virtual void InAct() { }

    // Вихід вимоги з елемента
    public virtual void OutAct() => Quantity++;

    // Накопичення статистики за інтервал delta між подіями
    public virtual void DoStatistics(double delta) { }

    public virtual void PrintInfo() =>
        Console.WriteLine($"{Name} quantity = {Quantity} tnext = {FormatTime(Tnext)}");

    public virtual void PrintResult() => Console.WriteLine($"{Name} quantity = {Quantity}");

    protected static string FormatTime(double t) => t == double.MaxValue ? "inf" : t.ToString("F4");
}
