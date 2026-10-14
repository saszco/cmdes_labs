namespace Lab2;

// Імітація за принципом найближчої події
public class Model
{
    private readonly List<Element> elements;
    public double Tcurr { get; private set; }
    public bool Verbose { get; set; } = true; // друкувати протокол подій

    public Model(List<Element> elements)
    {
        this.elements = elements;
        Tcurr = 0.0;
    }

    public void Simulate(double time)
    {
        while (Tcurr < time)
        {
            // 1. Шукаємо найближчу подію
            Element nextEvent = elements.MinBy(e => e.Tnext);
            double tnext = nextEvent.Tnext;

            if (Verbose)
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

            if (Verbose) PrintInfo();
        }

        if (Verbose) PrintResult();
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
        {
            if (e is Process p) p.PrintResult(Tcurr);
            else e.PrintResult();
        }
    }
}
