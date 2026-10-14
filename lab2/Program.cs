using Lab2;

// Запуск: dotnet run -- <номер завдання 1..6>, без аргументу — меню
string choice = args.Length > 0 ? args[0] : Ask();

switch (choice)
{
    case "1": Task1(); break;
    case "2": Task2(); break;
    case "3": Task3(); break;
    case "4": Task4(); break;
    case "5": Task5(); break;
    case "6": Task6(); break;
    default: Console.WriteLine("Невідомий номер завдання"); break;
}

static string Ask()
{
    Console.Write("Номер завдання (1-6): ");
    return Console.ReadLine()?.Trim();
}

// ---------------- Завдання 1: CREATE -> PROCESS -> DISPOSE ----------------
static void Task1()
{
    Console.WriteLine("=== Завдання 1: протокол подій (перші 20 одиниць часу) ===");
    var (model, _) = BuildSimple(delayCreate: 2.0, delayProcess: 1.0, maxQueue: 5);
    model.Simulate(20);

    Console.WriteLine("\n=== Завдання 1: прогін на 1000 одиниць часу ===");
    (model, _) = BuildSimple(2.0, 1.0, 5);
    model.Verbose = false;
    model.Simulate(1000);
    model.PrintResult();

    Console.WriteLine("\n=== Завдання 1: вплив зміни параметрів ===");
    Console.WriteLine($"{"delayCr",8} {"delayPr",8} {"maxQ",5} | {"served",7} {"failure",8} {"meanQ",7} {"Pfail",7}");
    foreach (var (dc, dp, mq) in new[] { (2.0, 1.0, 5), (1.0, 1.0, 5), (2.0, 2.0, 5), (2.0, 1.0, 1), (4.0, 1.0, 5) })
    {
        var (m, p) = BuildSimple(dc, dp, mq);
        m.Verbose = false;
        m.Simulate(1000);
        Console.WriteLine($"{dc,8} {dp,8} {mq,5} | {p.Quantity,7} {p.Failure,8} {p.MeanQueue(m.Tcurr),7:F3} {p.FailureProbability(),7:F3}");
    }
}

// ---------------- Завдання 2: середнє завантаження пристрою ----------------
static void Task2()
{
    Console.WriteLine("=== Завдання 2: середнє завантаження пристрою ===");
    Console.WriteLine("Без обмеження черги теоретично завантаження = delayPr / delayCr\n");
    Console.WriteLine($"{"delayCr",8} {"delayPr",8} | {"load",7} {"theory",7} {"meanQ",7}");
    foreach (var (dc, dp) in new[] { (2.0, 1.0), (4.0, 1.0), (2.0, 1.5), (1.0, 0.5), (5.0, 4.0) })
    {
        var (m, p) = BuildSimple(dc, dp, int.MaxValue);
        m.Verbose = false;
        m.Simulate(10000);
        Console.WriteLine($"{dc,8} {dp,8} | {p.MeanLoad(m.Tcurr),7:F3} {dp / dc,7:F3} {p.MeanQueue(m.Tcurr),7:F3}");
    }
}

static (Model, Process) BuildSimple(double delayCreate, double delayProcess, int maxQueue)
{
    var c = new Create("CREATOR", delayCreate);
    var p = new Process("PROCESSOR", delayProcess) { MaxQueue = maxQueue };
    var d = new Dispose("DISPOSE");
    c.AddNext(p);   // CREATE -> PROCESS
    p.AddNext(d);   // PROCESS -> DISPOSE
    return (new Model(new List<Element> { c, p, d }), p);
}

// ---------------- Завдання 3: CREATE -> P1 -> P2 -> P3 -> DISPOSE ----------------
static void Task3()
{
    Console.WriteLine("=== Завдання 3: протокол подій (перші 10 одиниць часу) ===");
    var chain = BuildChain(ChainParams.Base);
    chain.Model.Simulate(10);

    Console.WriteLine("\n=== Завдання 3: прогін на 1000 одиниць часу ===");
    chain = BuildChain(ChainParams.Base);
    chain.Model.Verbose = false;
    chain.Model.Simulate(1000);
    chain.Model.PrintResult();
}

// ---------------- Завдання 4: верифікація моделі із завдання 3 ----------------
static void Task4()
{
    Console.WriteLine("=== Завдання 4: верифікація моделі (час моделювання 10000) ===");
    Console.WriteLine("У кожному прогоні змінено лише один параметр відносно базового (прогін 1)\n");

    var b = ChainParams.Base;
    var runs = new List<ChainParams>
    {
        b,
        b with { DelayCreate = 1.0 },  // вдвічі частіші надходження
        b with { DelayCreate = 4.0 },  // вдвічі рідші надходження
        b with { Delay1 = 2.0 },
        b with { Delay2 = 2.0 },
        b with { Delay3 = 0.5 },
        b with { MaxQ1 = 1 },
        b with { MaxQ2 = 10 },
        b with { MaxQ3 = 0 },
    };

    Console.WriteLine($"{"#",2} | {"dCr",4} {"d1",4} {"d2",4} {"d3",4} {"q1",3} {"q2",3} {"q3",3} | " +
                      $"{"created",7} {"out",6} | {"fail1",5} {"fail2",5} {"fail3",5} | " +
                      $"{"Lq1",5} {"Lq2",5} {"Lq3",5} | {"load1",5} {"load2",5} {"load3",5}");
    Console.WriteLine(new string('-', 120));

    for (int i = 0; i < runs.Count; i++)
    {
        var r = runs[i];
        var ch = BuildChain(r);
        ch.Model.Verbose = false;
        ch.Model.Simulate(10000);
        double t = ch.Model.Tcurr;
        var p = ch.Processes;

        Console.WriteLine($"{i + 1,2} | {r.DelayCreate,4} {r.Delay1,4} {r.Delay2,4} {r.Delay3,4} {r.MaxQ1,3} {r.MaxQ2,3} {r.MaxQ3,3} | " +
                          $"{ch.Create.Quantity,7} {ch.Dispose.Quantity,6} | " +
                          $"{p[0].Failure,5} {p[1].Failure,5} {p[2].Failure,5} | " +
                          $"{p[0].MeanQueue(t),5:F2} {p[1].MeanQueue(t),5:F2} {p[2].MeanQueue(t),5:F2} | " +
                          $"{p[0].MeanLoad(t),5:F2} {p[1].MeanLoad(t),5:F2} {p[2].MeanLoad(t),5:F2}");
    }
}

static Chain BuildChain(ChainParams prm)
{
    var c = new Create("CREATOR", prm.DelayCreate);
    var p1 = new Process("PROCESS1", prm.Delay1) { MaxQueue = prm.MaxQ1 };
    var p2 = new Process("PROCESS2", prm.Delay2) { MaxQueue = prm.MaxQ2 };
    var p3 = new Process("PROCESS3", prm.Delay3) { MaxQueue = prm.MaxQ3 };
    var d = new Dispose("DISPOSE");

    // Зв'язування елементів через nextElement
    c.AddNext(p1);
    p1.AddNext(p2);
    p2.AddNext(p3);
    p3.AddNext(d);

    var model = new Model(new List<Element> { c, p1, p2, p3, d });
    return new Chain(model, c, new[] { p1, p2, p3 }, d);
}

// ---------------- Завдання 5: багатоканальний PROCESS ----------------
static void Task5()
{
    Console.WriteLine("=== Завдання 5: протокол подій для 3-канального пристрою (перші 5 одиниць часу) ===");
    Console.WriteLine("state — стан кожного каналу, напр. 101 = зайняті 1-й і 3-й\n");
    var (model, _) = BuildMulti(delayCreate: 1.0, delayProcess: 2.5, channels: 3, maxQueue: 5);
    model.Simulate(5);

    Console.WriteLine("\n=== Завдання 5: вплив кількості каналів (delayCr = 1, delayPr = 2.5, maxQ = 5, час 10000) ===");
    Console.WriteLine($"{"channels",8} | {"served",7} {"failure",8} {"Pfail",6} {"meanQ",6} {"wait",6} {"busy",6} {"load",6}");
    for (int n = 1; n <= 5; n++)
    {
        var (m, p) = BuildMulti(1.0, 2.5, n, 5);
        m.Verbose = false;
        m.Simulate(10000);
        double t = m.Tcurr;
        Console.WriteLine($"{n,8} | {p.Quantity,7} {p.Failure,8} {p.FailureProbability(),6:F3} {p.MeanQueue(t),6:F3} " +
                          $"{p.MeanWait(),6:F3} {p.MeanBusy(t),6:F3} {p.MeanLoad(t),6:F3}");
    }
    Console.WriteLine("busy — середня кількість зайнятих каналів, load = busy / channels");
}

static (Model, Process) BuildMulti(double delayCreate, double delayProcess, int channels, int maxQueue)
{
    var c = new Create("CREATOR", delayCreate);
    var p = new Process("PROCESSOR", delayProcess, channels) { MaxQueue = maxQueue };
    var d = new Dispose("DISPOSE");
    c.AddNext(p);
    p.AddNext(d);
    return (new Model(new List<Element> { c, p, d }), p);
}

// ---------------- Завдання 6: розгалуження маршруту з поверненням ----------------
//
//   CREATE -> P1 --0.7--> P2 --(1-pBack)--> DISPOSE
//              |           \--pBack--> P1   (повернення у попередній блок)
//              \--0.3--> P3 --0.9--> DISPOSE
//                          \--0.1--> P3     (повернення в себе)
static void Task6()
{
    Console.WriteLine("=== Завдання 6: протокол подій (перші 10 одиниць часу) ===");
    var net = BuildNetwork(pBack: 0.2);
    net.Model.Simulate(10);

    Console.WriteLine("\n=== Завдання 6: прогін на 1000 одиниць часу ===");
    net = BuildNetwork(0.2);
    net.Model.Verbose = false;
    net.Model.Simulate(1000);
    net.Model.PrintResult();

    Console.WriteLine("\n=== Завдання 6: вплив ймовірності повернення P2 -> P1 (час 10000) ===");
    Console.WriteLine($"{"pBack",5} | {"created",7} {"out",6} | {"in P1",6} {"in P2",6} {"in P3",6} | " +
                      $"{"Lq1",5} {"Lq2",5} {"Lq3",5} | {"load1",5} {"load2",5} {"load3",5} | {"fail",5}");
    foreach (double pBack in new[] { 0.0, 0.2, 0.4, 0.6 })
    {
        var n = BuildNetwork(pBack);
        n.Model.Verbose = false;
        n.Model.Simulate(10000);
        double t = n.Model.Tcurr;
        var p = n.Processes;
        int fail = p.Sum(x => x.Failure);
        Console.WriteLine($"{pBack,5} | {n.Create.Quantity,7} {n.Dispose.Quantity,6} | " +
                          $"{p[0].Quantity,6} {p[1].Quantity,6} {p[2].Quantity,6} | " +
                          $"{p[0].MeanQueue(t),5:F2} {p[1].MeanQueue(t),5:F2} {p[2].MeanQueue(t),5:F2} | " +
                          $"{p[0].MeanLoad(t),5:F2} {p[1].MeanLoad(t),5:F2} {p[2].MeanLoad(t),5:F2} | {fail,5}");
    }
    Console.WriteLine("in Pi — кількість обслуговувань у Pi (з урахуванням повторних проходів)");
}

static Chain BuildNetwork(double pBack)
{
    var c = new Create("CREATOR", 1.0);
    var p1 = new Process("PROCESS1", 0.6, channels: 2) { MaxQueue = 5 };
    var p2 = new Process("PROCESS2", 1.0, channels: 2) { MaxQueue = 5 };
    var p3 = new Process("PROCESS3", 1.5) { MaxQueue = 3 };
    var d = new Dispose("DISPOSE");

    c.AddNext(p1);

    p1.AddNext(p2, 0.7);
    p1.AddNext(p3, 0.3);

    p2.AddNext(d, 1 - pBack);
    p2.AddNext(p1, pBack);  // повернення у попередній блок

    p3.AddNext(d, 0.9);
    p3.AddNext(p3, 0.1);    // повернення в цей самий блок

    var model = new Model(new List<Element> { c, p1, p2, p3, d });
    return new Chain(model, c, new[] { p1, p2, p3 }, d);
}

// Параметри моделі із завдання 3: інтервал надходження, затримки та обмеження черг пристроїв
record ChainParams(double DelayCreate, double Delay1, double Delay2, double Delay3, int MaxQ1, int MaxQ2, int MaxQ3)
{
    public static ChainParams Base => new(2.0, 1.0, 1.0, 1.0, 5, 5, 5);
}

record Chain(Model Model, Create Create, Process[] Processes, Dispose Dispose);
