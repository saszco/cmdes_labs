namespace Lab3;

// Універсальний алгоритм імітації мережі МО: просування часу до найближчої
// події, зміна стану моделі за подіями
public class Model
{
    private readonly List<Element> elements;
    public double Tcurr { get; private set; }
    public bool Verbose { get; set; } = true;   // друкувати протокол подій
    public double ProtocolFrom { get; set; }    // друкувати протокол лише з цього моменту

    // Дії після кожної події — правила, що не вкладаються у формалізм мережі МО
    // (наприклад, зміна черги клієнтом). Повертають опис зміни або null.
    public List<Func<string>> AfterEvent { get; } = new();

    public Model(params Element[] elements) => this.elements = elements.ToList();

    public void Simulate(double time)
    {
        while (true)
        {
            // 1. Шукаємо найближчу подію
            Element nextEvent = elements.MinBy(e => e.Tnext);
            double tnext = nextEvent.Tnext;
            if (tnext > time)
            {
                // подій до кінця моделювання більше немає: статистика до моменту time
                foreach (var e in elements) e.DoStatistics(time - Tcurr);
                Tcurr = time;
                break;
            }

            bool print = Verbose && tnext >= ProtocolFrom;
            if (print)
                Console.WriteLine($"\nIt's time for event in {nextEvent.Name}, time = {tnext:F4}");

            // 2. Збираємо статистику за інтервал до цієї події
            foreach (var e in elements)
                e.DoStatistics(tnext - Tcurr);

            // 3. Просуваємо час
            Tcurr = tnext;
            foreach (var e in elements)
                e.Tcurr = Tcurr;

            // 4. Виконуємо події всіх елементів, у яких вона настала саме зараз
            foreach (var e in elements)
                if (e.Tnext == Tcurr)
                    e.OutAct();

            // 5. Додаткові правила зміни стану
            foreach (var rule in AfterEvent)
            {
                string change = rule();
                if (print && change != null) Console.WriteLine($">>> {change}");
            }

            if (print) PrintInfo();
        }
    }

    public void PrintInfo()
    {
        foreach (var e in elements)
            e.PrintInfo();
    }

    public void PrintResult()
    {
        Console.WriteLine("\n-------------RESULTS-------------");
        foreach (var e in elements)
            e.PrintResult(Tcurr);
    }
}
