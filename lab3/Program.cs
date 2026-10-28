using System.Globalization;
using Lab3;

// десяткова крапка у виводі незалежно від мови системи
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Запуск: dotnet run -- <номер завдання 1..3>, без аргументу — меню
string choice = args.Length > 0 ? args[0] : Ask();

switch (choice)
{
    case "1": Task1(); break;
    case "2": Task2(); break;
    case "3": Task3(); break;
    default: Console.WriteLine("Невідомий номер завдання"); break;
}

static string Ask()
{
    Console.Write("Номер завдання (1-3): ");
    return Console.ReadLine()?.Trim();
}

// ---------------- Завдання 1: універсальний алгоритм імітації мережі МО ----------------
static void Task1()
{
    Console.WriteLine("=== Завдання 1: вибір маршруту за пріоритетом (протокол, перші 6 одиниць часу) ===");
    Console.WriteLine("MAIN — пріоритет 2; RESERVE1, RESERVE2 — пріоритет 1, ймовірності 0.5 / 0.5");
    FunRand.SetSeed(1);
    var (model, main, r1, r2, c) = BuildPriorityNet();
    model.Simulate(6);

    Console.WriteLine("\n=== Завдання 1: вибір маршруту за пріоритетом (час 10000) ===");
    FunRand.SetSeed(1);
    (model, main, r1, r2, c) = BuildPriorityNet();
    model.Verbose = false;
    model.Simulate(10000);
    Console.WriteLine($"{"СМО",-10} {"надійшло",9} {"обслуг.",8} {"відмов",7} {"завант.",8}");
    foreach (var p in new[] { main, r1, r2 })
        Console.WriteLine($"{p.Name,-10} {p.Arrivals,9} {p.Quantity,8} {p.Failure,7} {p.MeanLoad(model.Tcurr),8:F3}");
    Console.WriteLine($"Створено вимог: {c.Quantity}");

    Console.WriteLine("\n=== Завдання 1: тестова мережа МО з результатами аналітичного розрахунку (час 100000) ===");
    Console.WriteLine("Порівняння з результатами аналітичного розрахунку");
    double[] lqTheory = { 1.786, 0.003, 0.004, 0.00001 };
    double[] busyTheory = { 0.714, 0.054, 0.062, 0.036 };
    Console.WriteLine($"{"СМО",-6} | {"Lq імітація",12} {"Lq теорія",10} | {"зайнятість",11} {"теорія",7} | {"обслуг.",8}");
    FunRand.SetSeed(1);
    var (testModel, smo) = BuildLectureNet();
    testModel.Verbose = false;
    testModel.Simulate(100000);
    double t = testModel.Tcurr;
    for (int i = 0; i < smo.Length; i++)
        Console.WriteLine($"{smo[i].Name,-6} | {smo[i].MeanQueue(t),12:F5} {lqTheory[i],10:F5} | " +
                          $"{smo[i].MeanBusy(t),11:F3} {busyTheory[i],7:F3} | {smo[i].Quantity,8}");
    Console.WriteLine("зайнятість — середня кількість зайнятих пристроїв СМО");

    Console.WriteLine("\n=== Завдання 1: тестова мережа, 5 прогонів з різним seed (час 100000) ===");
    Console.WriteLine($"{"seed",4} | {"Lq1",7} {"Lq2",7} {"Lq3",7} | {"зайн.1",7} {"зайн.2",7} {"зайн.3",7} {"зайн.4",7}");
    var lqSum = new double[4];
    var busySum = new double[4];
    for (int seed = 1; seed <= 5; seed++)
    {
        FunRand.SetSeed(seed);
        var (m, s) = BuildLectureNet();
        m.Verbose = false;
        m.Simulate(100000);
        for (int i = 0; i < 4; i++)
        {
            lqSum[i] += s[i].MeanQueue(m.Tcurr) / 5;
            busySum[i] += s[i].MeanBusy(m.Tcurr) / 5;
        }
        Console.WriteLine($"{seed,4} | {s[0].MeanQueue(m.Tcurr),7:F3} {s[1].MeanQueue(m.Tcurr),7:F4} {s[2].MeanQueue(m.Tcurr),7:F4} | " +
                          $"{s[0].MeanBusy(m.Tcurr),7:F3} {s[1].MeanBusy(m.Tcurr),7:F3} {s[2].MeanBusy(m.Tcurr),7:F3} {s[3].MeanBusy(m.Tcurr),7:F3}");
    }
    Console.WriteLine($"{"сер.",4} | {lqSum[0],7:F3} {lqSum[1],7:F4} {lqSum[2],7:F4} | " +
                      $"{busySum[0],7:F3} {busySum[1],7:F3} {busySum[2],7:F3} {busySum[3],7:F3}");
    Console.WriteLine($"{"теор",4} | {lqTheory[0],7:F3} {lqTheory[1],7:F4} {lqTheory[2],7:F4} | " +
                      $"{busyTheory[0],7:F3} {busyTheory[1],7:F3} {busyTheory[2],7:F3} {busyTheory[3],7:F3}");
}

// Основний пристрій і два резервні: резервні отримують вимогу лише тоді,
// коли основний не може її прийняти; між резервними — за ймовірністю
static (Model, Process, Process, Process, Create) BuildPriorityNet()
{
    var c = new Create("CREATOR", () => FunRand.Exp(1.0));
    var main = new Process("MAIN", () => FunRand.Exp(1.5)) { MaxQueue = 1 };
    var r1 = new Process("RESERVE1", () => FunRand.Exp(1.5)) { MaxQueue = 1 };
    var r2 = new Process("RESERVE2", () => FunRand.Exp(1.5)) { MaxQueue = 1 };
    var d = new Dispose("DISPOSE");

    c.AddRoute(main, priority: 2);
    c.AddRoute(r1, probability: 0.5, priority: 1);
    c.AddRoute(r2, probability: 0.5, priority: 1);
    main.AddRoute(d);
    r1.AddRoute(d);
    r2.AddRoute(d);
    return (new Model(c, main, r1, r2, d), main, r1, r2, c);
}

// Мережа МО з результатами аналітичного розрахунку: усі черги
// необмежені, усі затримки експоненційні, маршрут лише за ймовірностями
static (Model, Process[]) BuildLectureNet()
{
    var c = new Create("CREATOR", () => FunRand.Exp(2.0));
    var k1 = new Process("СМО1", () => FunRand.Exp(0.6));
    var k2 = new Process("СМО2", () => FunRand.Exp(0.3));
    var k3 = new Process("СМО3", () => FunRand.Exp(0.4));
    var k4 = new Process("СМО4", () => FunRand.Exp(0.1), channels: 2);
    var d = new Dispose("DISPOSE");

    c.AddRoute(k1);
    k1.AddRoute(k2, 0.15);
    k1.AddRoute(k3, 0.13);
    k1.AddRoute(k4, 0.3);
    k1.AddRoute(d, 0.42);
    k2.AddRoute(k1);
    k3.AddRoute(k1);
    k4.AddRoute(k1);
    return (new Model(c, k1, k2, k3, k4, d), new[] { k1, k2, k3, k4 });
}

// ---------------- Завдання 2: банк для автомобілістів ----------------
static void Task2()
{
    Console.WriteLine("=== Завдання 2: протокол подій (перші 4 одиниці часу) ===");
    FunRand.SetSeed(1);
    var bank = new BankModel();
    Console.WriteLine("\nПочатковий стан:");
    bank.Model.PrintInfo();
    bank.Model.Simulate(4);

    Console.WriteLine("\n=== Завдання 2: прогін на 1000 одиниць часу ===");
    FunRand.SetSeed(1);
    bank = new BankModel();
    bank.Model.Verbose = false;
    bank.Model.Simulate(1000);
    bank.PrintResult();

    Console.WriteLine("\n=== Завдання 2: 5 прогонів з різним seed (час 1000) ===");
    Console.WriteLine($"{"seed",4} | {"load1",6} {"load2",6} | {"N банк",7} {"інт.від'їзд",11} {"T банк",7} | " +
                      $"{"Lq1",6} {"Lq2",6} | {"відм.,%",7} {"змін",5}");
    for (int seed = 1; seed <= 5; seed++)
    {
        FunRand.SetSeed(seed);
        var b = new BankModel();
        b.Model.Verbose = false;
        b.Model.Simulate(1000);
        double t = b.Model.Tcurr;
        Console.WriteLine($"{seed,4} | {b.Cashier1.MeanLoad(t),6:F3} {b.Cashier2.MeanLoad(t),6:F3} | " +
                          $"{b.TotalTimeInBank / t,7:F3} {b.Exit.MeanArrivalInterval,11:F3} {b.TotalTimeInBank / b.Exit.Quantity,7:F3} | " +
                          $"{b.Cashier1.MeanQueue(t),6:F3} {b.Cashier2.MeanQueue(t),6:F3} | " +
                          $"{100.0 * b.Refused / b.Clients.Quantity,7:F2} {b.LaneChanges,5}");
    }
}

// ---------------- Завдання 3: лікарня ----------------
static void Task3()
{
    Console.WriteLine("=== Завдання 3: фрагмент протоколу подій (620–665 хв) ===");
    Console.WriteLine("Для черг і каналів у дужках наведено типи хворих");
    FunRand.SetSeed(1);
    var h = new HospitalModel();
    h.Model.ProtocolFrom = 620;
    h.Model.Simulate(665);

    Console.WriteLine("\n=== Завдання 3: прогін на 100000 хвилин ===");
    FunRand.SetSeed(1);
    h = new HospitalModel();
    h.Model.Verbose = false;
    h.Model.Simulate(100000);
    h.PrintResult();

    Console.WriteLine("\n=== Завдання 3: 5 прогонів з різним seed (час 100000 хв) ===");
    Console.WriteLine($"{"seed",4} | {"T тип 1",8} {"T тип 2",8} {"T тип 3",8} | {"інт. в лаб.",11} | {"завант. лікарів",15}");
    for (int seed = 1; seed <= 5; seed++)
    {
        FunRand.SetSeed(seed);
        var m = new HospitalModel();
        m.Model.Verbose = false;
        m.Model.Simulate(100000);
        Console.WriteLine($"{seed,4} | {m.MeanTimeInSystem(1),8:F2} {m.MeanTimeInSystem(2),8:F2} {m.MeanTimeInSystem(3),8:F2} | " +
                          $"{m.LabRegistry.MeanArrivalInterval,11:F2} | {m.Reception.MeanLoad(m.Model.Tcurr),15:F3}");
    }
}
