namespace Lab3;

// Система масового обслуговування: один або кілька ідентичних пристроїв
// з однією чергою перед ними. channels <= 0 — необмежена
// кількість пристроїв (чиста затримка, наприклад час слідування).
public class Process : Element
{
    private class Channel
    {
        public Item Item;
        public double Tnext = double.MaxValue;
    }

    // Правила вибору вимоги з черги: повертають індекс вимоги, яку бере пристрій
    public static readonly Func<List<Item>, int> Fifo = q => 0;
    public static readonly Func<List<Item>, int> Lifo = q => q.Count - 1;

    private readonly Func<Item, double> delay;   // час обслуговування може залежати від типу
    private readonly List<Channel> channels = new();
    private readonly List<Item> queue = new();
    private readonly bool unlimited;

    public int MaxQueue { get; set; } = int.MaxValue;              // max — обмеження черги
    public Func<List<Item>, int> QueueRule { get; set; } = Fifo;
    public int Failure { get; private set; }                        // відмови: немає місця в черзі
    public bool ShowTypes { get; set; }                             // друкувати типи вимог у протоколі

    private double queueSum;   // ∑ L_i·Δt_i — для середньої довжини черги
    private double busySum;    // ∑ (зайняті пристрої)·Δt_i — для завантаження
    private readonly Dictionary<int, double> timeByType = new(); // ∑ (вимоги типу k у СМО)·Δt_i

    public Process(string name, Func<Item, double> delay, int channels = 1) : base(name)
    {
        this.delay = delay;
        unlimited = channels <= 0;
        for (int i = 0; i < Math.Max(channels, 0); i++)
            this.channels.Add(new Channel());
    }

    public Process(string name, Func<double> delay, int channels = 1) : this(name, _ => delay(), channels) { }

    public int Channels => unlimited ? -1 : channels.Count;
    public int Queue => queue.Count;
    public int Busy => channels.Count(c => c.Item != null);
    public int Count => Queue + Busy;          // кількість вимог у СМО

    public override bool CanAccept => unlimited || FreeChannel() != null || Queue < MaxQueue;

    private Channel FreeChannel() => channels.FirstOrDefault(c => c.Item == null);

    public override void InAct(Item item)
    {
        RegisterArrival();
        if (unlimited || FreeChannel() != null)
            Start(item, Tcurr + delay(item));
        else if (Queue < MaxQueue)
            queue.Add(item);
        else
            Failure++;
    }

    // Вимога займає перший вільний пристрій (пристрої переглядаються по порядку)
    private void Start(Item item, double releaseTime)
    {
        Channel c = FreeChannel();
        if (c == null)
        {
            c = new Channel();      // лише для необмеженої кількості пристроїв
            channels.Add(c);
        }
        c.Item = item;
        c.Tnext = releaseTime;
        UpdateTnext();
    }

    // Початковий стан мережі: зайнятий пристрій і вимоги в черзі
    public void PreloadChannel(Item item, double releaseTime) => Start(item, releaseTime);
    public void PreloadQueue(Item item) => queue.Add(item);

    public override void OutAct()
    {
        var finished = channels.Where(c => c.Tnext == Tcurr).ToList();
        foreach (var c in finished)
        {
            Item item = c.Item;
            c.Item = null;
            c.Tnext = double.MaxValue;
            Quantity++;

            // спочатку пристрій бере наступну вимогу з черги...
            if (queue.Count > 0)
            {
                int idx = QueueRule(queue);
                Item next = queue[idx];
                queue.RemoveAt(idx);
                c.Item = next;
                c.Tnext = Tcurr + delay(next);
            }

            // ...потім обслужена вимога слідує далі за маршрутом
            if (!Pass(item)) RouteFailures++;
        }
        UpdateTnext();
    }

    // Остання вимога в черзі залишає її (потрібно для зміни черги)
    public Item TakeLastFromQueue()
    {
        Item item = queue[^1];
        queue.RemoveAt(queue.Count - 1);
        return item;
    }

    // Tnext СМО = найближче звільнення серед усіх пристроїв
    private void UpdateTnext() => Tnext = channels.Count == 0 ? double.MaxValue : channels.Min(c => c.Tnext);

    public override void DoStatistics(double delta)
    {
        queueSum += Queue * delta;
        busySum += Busy * delta;
        foreach (var item in queue.Concat(channels.Where(c => c.Item != null).Select(c => c.Item)))
            timeByType[item.InitialType] = timeByType.GetValueOrDefault(item.InitialType) + delta;
    }

    public double QueueTime => queueSum;     // загальний час очікування в черзі
    public double BusyTime => busySum;       // загальний час роботи пристроїв
    public double TimeOfType(int initialType) => timeByType.GetValueOrDefault(initialType);

    public double MeanQueue(double totalTime) => queueSum / totalTime;
    public double MeanBusy(double totalTime) => busySum / totalTime;                 // середня кількість зайнятих пристроїв
    public double MeanLoad(double totalTime) => busySum / totalTime / Channels;      // завантаження одного пристрою
    public double MeanWait() => Quantity == 0 ? 0 : queueSum / Quantity;             // середнє очікування в черзі
    public double FailureProbability() => Arrivals == 0 ? 0 : Failure / (double)Arrivals;

    public override void PrintInfo()
    {
        string state = unlimited ? Busy.ToString() : string.Join("", channels.Select(c => c.Item == null ? "0" : "1"));
        string q = ShowTypes ? $"[{string.Join(",", queue.Select(i => i.Type))}]" : Queue.ToString();
        string inService = ShowTypes && Busy > 0
            ? $" types = [{string.Join(",", channels.Where(c => c.Item != null).Select(c => c.Item.Type))}]" : "";
        Console.WriteLine($"{Name} state = {state}{inService} queue = {q} quantity = {Quantity} " +
                          $"failure = {Failure} tnext = {FormatTime(Tnext)}");
    }

    public override void PrintResult(double totalTime)
    {
        Console.WriteLine($"{Name} quantity = {Quantity}");
        Console.WriteLine($"  mean length of queue = {MeanQueue(totalTime):F4}");
        if (unlimited)
            Console.WriteLine($"  mean busy channels   = {MeanBusy(totalTime):F4} (необмежена кількість)");
        else
            Console.WriteLine($"  mean load            = {MeanLoad(totalTime):F4}" +
                              (Channels > 1 ? $" (mean busy channels = {MeanBusy(totalTime):F4} of {Channels})" : ""));
        Console.WriteLine($"  mean waiting time    = {MeanWait():F4}");
        Console.WriteLine($"  failure              = {Failure}");
        Console.WriteLine($"  failure probability  = {FailureProbability():F4}");
    }
}
